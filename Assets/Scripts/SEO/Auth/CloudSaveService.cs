using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Jinyou 통합 세이브(PlayerPrefs의 단일 JSON 블롭)를 Firestore에 계정(UID)별로 동기화한다.
/// 설계:
/// - 로컬 우선(Local-first): PlayerPrefs는 그대로 두고, 체크포인트마다 클라우드로 푸시한다(디바운스).
/// - 로그인 시 풀(pull): 클라우드/로컬을 lastSavedUnixTime으로 비교해 더 최신본을 채택한다(자동 충돌 해결).
/// - 문서 경로: users/{uid} (Phase 1 — Jinyou 통합 세이브 한정).
/// 비로그인/오프라인/Firestore 미준비 시에는 조용히 no-op 하고 로컬 저장만 동작한다.
/// </summary>
[DisallowMultipleComponent]
public class CloudSaveService : MonoBehaviour
{
    private const string UsersCollection = "users";
    private const string SaveField = "save";
    private const string TimestampField = "lastSaved";
    private const string VersionField = "version";
    private const string UpdatedAtField = "updatedAt";
    private const float PushDebounceSeconds = 3f;

    private static CloudSaveService instance;

    /// <summary>지연 생성되는 싱글턴. 씬에 배치하지 않아도 첫 접근 시 자동 생성된다.</summary>
    public static CloudSaveService Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("[CloudSaveService]");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<CloudSaveService>();
            }

            return instance;
        }
    }

    private FirebaseFirestore firestore;
    private readonly Dictionary<string, Task> syncing = new Dictionary<string, Task>();
    private readonly HashSet<string> pendingPushKeys = new HashSet<string>();
    private float nextPushTime;
    private bool pushing;

    [Serializable]
    private class SaveMeta
    {
        public long lastSavedUnixTime;
        public int version;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (pushing || pendingPushKeys.Count == 0)
        {
            return;
        }

        if (Time.unscaledTime >= nextPushTime)
        {
            FlushNow();
        }
    }

    // ───────── 공개 API ─────────

    /// <summary>클라우드와 로컬을 비교해 최신본을 로컬 PlayerPrefs[key]에 맞춘다(필요 시 클라우드로 업로드). 같은 키 동시 호출은 합쳐진다.</summary>
    public Task EnsureSyncedAsync(string playerPrefsKey)
    {
        if (string.IsNullOrEmpty(playerPrefsKey))
        {
            return Task.CompletedTask;
        }

        if (syncing.TryGetValue(playerPrefsKey, out Task running) && !running.IsCompleted)
        {
            return running;
        }

        Task task = SyncAsync(playerPrefsKey);
        syncing[playerPrefsKey] = task;
        return task;
    }

    /// <summary>저장 직후 호출. 디바운스되어 잠시 뒤 클라우드로 업로드된다.</summary>
    public void RequestPush(string playerPrefsKey)
    {
        if (string.IsNullOrEmpty(playerPrefsKey) || !IsReady())
        {
            return;
        }

        pendingPushKeys.Add(playerPrefsKey);
        nextPushTime = Time.unscaledTime + PushDebounceSeconds;
    }

    /// <summary>일시정지/종료 등 Update가 멈추는 상황에서 즉시 업로드한다(대기 중인 키 전부).</summary>
    public void FlushNow()
    {
        if (pushing || pendingPushKeys.Count == 0 || !IsReady())
        {
            return;
        }

        List<string> keys = new List<string>(pendingPushKeys);
        pendingPushKeys.Clear();
        _ = PushManyAsync(keys);
    }

    // ───────── 내부 구현 ─────────

    private async Task SyncAsync(string key)
    {
        if (!IsReady())
        {
            return;
        }

        try
        {
            DocumentSnapshot snap = await Doc().GetSnapshotAsync();
            long localTs = GetLocalTimestamp(key);

            if (snap.Exists
                && snap.TryGetValue(SaveField, out string cloudJson)
                && !string.IsNullOrEmpty(cloudJson))
            {
                long cloudTs = snap.TryGetValue(TimestampField, out long t) ? t : 0L;

                if (cloudTs > localTs)
                {
                    // 클라우드가 최신 → 로컬에 반영(다음 LoadUnifiedGame이 이 값을 읽는다).
                    PlayerPrefs.SetString(key, cloudJson);
                    PlayerPrefs.Save();
                    Debug.Log($"[Cloud] 클라우드 세이브 채택 (cloud {cloudTs} > local {localTs}).");
                }
                else if (localTs > cloudTs)
                {
                    await PushAsync(key);
                    Debug.Log($"[Cloud] 로컬 세이브 업로드 (local {localTs} > cloud {cloudTs}).");
                }
            }
            else if (localTs >= 0)
            {
                await PushAsync(key);
                Debug.Log("[Cloud] 클라우드에 첫 세이브 업로드.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] 동기화 실패(로컬 유지): {e.Message}");
        }
    }

    private async Task PushManyAsync(List<string> keys)
    {
        pushing = true;
        try
        {
            foreach (string key in keys)
            {
                await PushAsync(key);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] 업로드 실패: {e.Message}");
        }
        finally
        {
            pushing = false;
        }
    }

    private async Task PushAsync(string key)
    {
        if (!IsReady())
        {
            return;
        }

        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SaveMeta meta = ParseMeta(json);
        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { SaveField, json },
            { TimestampField, meta != null ? meta.lastSavedUnixTime : 0L },
            { VersionField, meta != null ? meta.version : 0 },
            { UpdatedAtField, FieldValue.ServerTimestamp },
        };

        await Doc().SetAsync(payload, SetOptions.MergeAll);
    }

    private DocumentReference Doc()
    {
        return firestore.Collection(UsersCollection).Document(FirebaseAuthManager.Instance.Uid);
    }

    private bool IsReady()
    {
        FirebaseAuthManager auth = FirebaseAuthManager.Instance;
        if (auth == null || !auth.IsReady || !auth.IsSignedIn)
        {
            return false;
        }

        if (firestore == null)
        {
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Cloud] Firestore 초기화 실패: {e.Message}");
                return false;
            }
        }

        return firestore != null;
    }

    private static long GetLocalTimestamp(string key)
    {
        SaveMeta meta = ParseMeta(PlayerPrefs.GetString(key, string.Empty));
        return meta != null ? meta.lastSavedUnixTime : -1L;
    }

    private static SaveMeta ParseMeta(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SaveMeta>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

/// <summary>
/// 구글 ID 토큰을 받아 Firebase 인증으로 교환하고, 로그인 상태/UID를 관리한다.
/// 로그인 방식(C 웹 / B 네이티브)은 <see cref="IGoogleSignIn"/> 구현 교체로만 바꾼다.
/// ※ Firebase Auth Unity SDK가 import 되어 있어야 컴파일된다.
/// </summary>
[DisallowMultipleComponent]
public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private IGoogleSignIn googleSignIn;

    public bool IsReady { get; private set; }
    public bool IsSignedIn => auth != null && auth.CurrentUser != null;
    public string Uid => auth != null && auth.CurrentUser != null ? auth.CurrentUser.UserId : null;

    /// <summary>구글 프로필 표시 이름(없으면 null). 환영 문구 등에 사용.</summary>
    public string DisplayName => auth != null && auth.CurrentUser != null ? auth.CurrentUser.DisplayName : null;

    /// <summary>준비 완료/로그인 상태 변화 시 호출.</summary>
    public event Action OnAuthStateChanged;

    /// <summary>플레이어에게 보여줄 접속/로그인 진행 단계.</summary>
    public enum ConnectionState
    {
        Connecting,    // Firebase 초기화/네트워크 확인 중
        Ready,         // 준비 완료, 로그인 대기
        SigningIn,     // 구글 로그인 진행 중
        SignedIn,      // 로그인 완료
        SignInFailed,  // 로그인 실패(취소 제외)
        ConnectFailed, // Firebase 초기화/네트워크 실패
    }

    /// <summary>현재 진행 단계. 변경 시 <see cref="OnAuthStateChanged"/>가 호출된다.</summary>
    public ConnectionState State { get; private set; } = ConnectionState.Connecting;

    private void SetState(ConnectionState state)
    {
        State = state;
        OnAuthStateChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ▼ 교체 지점: 네이티브(B). 웹(C)로 되돌리려면 new WebGoogleSignIn()으로만 바꾸면 됨.
        googleSignIn = new NativeGoogleSignIn();
    }

    private async void Start()
    {
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"[Auth] Firebase 의존성 사용 불가: {status}");
            SetState(ConnectionState.ConnectFailed);
            return;
        }

        auth = FirebaseAuth.DefaultInstance;
        IsReady = true;
        // 자동 로그인(이전 세션 CurrentUser)도 여기서 반영됨.
        SetState(IsSignedIn ? ConnectionState.SignedIn : ConnectionState.Ready);
    }

    /// <summary>구글 로그인 → Firebase 자격증명 교환. 성공 시 true.</summary>
    public async Task<bool> SignInWithGoogleAsync()
    {
        if (!IsReady)
        {
            Debug.LogWarning("[Auth] 아직 Firebase 준비 전입니다.");
            return false;
        }

        SetState(ConnectionState.SigningIn);

        string idToken = await googleSignIn.SignInAsync();
        if (string.IsNullOrEmpty(idToken))
        {
            SetState(ConnectionState.Ready); // 사용자 취소 → 다시 대기 상태로
            return false;
        }

        try
        {
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            await auth.SignInWithCredentialAsync(credential);
            SetState(ConnectionState.SignedIn);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] Firebase 로그인 실패: {e.Message}");
            SetState(ConnectionState.SignInFailed);
            return false;
        }
    }

    public void SignOut()
    {
        googleSignIn?.SignOut();
        auth?.SignOut();
        SetState(ConnectionState.Ready);
    }

    /// <summary>계정별 세이브 키. 로그인 시 baseKey_UID, 비로그인 시 baseKey 그대로.</summary>
    public string ScopedSaveKey(string baseKey)
    {
        return IsSignedIn ? $"{baseKey}_{Uid}" : baseKey;
    }
}

// Play Games 플러그인의 PlayGamesPlatform은 #if UNITY_ANDROID로 가려져 있어, Android 타깃에서만 컴파일한다.
// (비-Android 타깃에선 FirebaseAuthManager가 이 클래스를 참조하지 않으므로 비어 있어도 무방.)
#if UNITY_ANDROID
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

/// <summary>
/// Android Google Play Games(v2)로 로그인하고, Firebase에 넘길 server auth code를 받는다.
/// Firebase는 <see cref="PlayGamesAuthProvider"/>로 이 코드를 자격증명으로 교환한다.
/// ※ 동작 전제: Play Console PGS 구성 + Firebase 'Play Games' 공급자 활성화 + SHA-1 등록 + 테스터 등록.
/// </summary>
public class PlayGamesSignIn : IGoogleSignIn
{
    private static bool activated;
    private TaskCompletionSource<string> pending;

    public PlayGamesSignIn()
    {
        if (!activated)
        {
            PlayGamesPlatform.Activate(); // 1회. 자동 로그인 시도를 시작한다.
            activated = true;
        }
    }

    // 버튼(수동): 로그인 UI를 띄운다.
    public Task<string> SignInAsync()
    {
        return AuthenticateThenServerCode(manual: true);
    }

    // 시작 시 무음 자동 연결: 이미 Play Games에 로그인돼 있으면 UI 없이 성공, 아니면 실패(null).
    public Task<string> TrySilentSignInAsync()
    {
        return AuthenticateThenServerCode(manual: false);
    }

    private Task<string> AuthenticateThenServerCode(bool manual)
    {
        if (pending != null && !pending.Task.IsCompleted)
        {
            return pending.Task;
        }

        pending = new TaskCompletionSource<string>();
        Task<string> task = pending.Task;

        Action<SignInStatus> onAuth = status =>
        {
            if (status != SignInStatus.Success)
            {
                Debug.LogWarning($"[PlayGames] {(manual ? "수동" : "무음")} 인증 실패/취소: {status}");
                Complete(null);
                return;
            }

            // 서버 교환용 auth code 요청 → Firebase PlayGamesAuthProvider로 전달.
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
            {
                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogWarning("[PlayGames] server auth code가 비어 있습니다.");
                    Complete(null);
                    return;
                }

                Complete(code);
            });
        };

        // 무음(Authenticate)은 UI를 띄우지 않고, 수동(ManuallyAuthenticate)은 로그인 UI를 띄운다.
        if (manual)
        {
            PlayGamesPlatform.Instance.ManuallyAuthenticate(onAuth);
        }
        else
        {
            PlayGamesPlatform.Instance.Authenticate(onAuth);
        }

        return task;
    }

    public Credential CreateCredential(string serverAuthCode)
    {
        return PlayGamesAuthProvider.GetCredential(serverAuthCode);
    }

    public void SignOut()
    {
        // PGS v2는 앱 주도 SignOut을 제공하지 않는다(시스템이 자동 로그인 관리). Firebase 측에서만 SignOut한다.
    }

    private void Complete(string code)
    {
        TaskCompletionSource<string> tcs = pending;
        pending = null;
        tcs?.TrySetResult(string.IsNullOrEmpty(code) ? null : code);
    }
}
#endif

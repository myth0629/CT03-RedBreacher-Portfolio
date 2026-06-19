using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 흐름: (로그인 버튼) → 구글 로그인 → 성공 시 로그인 버튼 숨김 + Tap to Start 활성화.
/// Tap to Start 그룹에는 <see cref="TapToStartGame"/>가 있어, 활성화된 뒤 탭하면 게임 씬으로 전환된다.
/// 이전 세션에서 이미 로그인돼 있으면(자동 로그인) 바로 Tap to Start 상태로 넘어간다.
/// </summary>
[DisallowMultipleComponent]
public class TitleScreenController : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private GameObject loginGroup;       // 숨길 로그인 묶음(미지정 시 버튼 오브젝트 사용)

    [Header("Tap to Start")]
    [SerializeField] private GameObject tapToStartGroup;   // 로그인 후 활성화할 'tap' 오브젝트

    private bool busy;

    private void Awake()
    {
        // 초기: 로그인만 보이고 Tap to Start는 숨김.
        SetActiveSafe(tapToStartGroup, false);
        SetActiveSafe(LoginVisual(), true);
    }

    private void Start()
    {
        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.AddListener(OnLoginClicked);
        }

        // FirebaseAuthManager.Awake는 이 시점 이전에 실행됨(같은 씬). 준비/상태 변화를 구독.
        FirebaseAuthManager manager = FirebaseAuthManager.Instance;
        if (manager != null)
        {
            manager.OnAuthStateChanged += HandleAuthState;
            HandleAuthState(); // 자동 로그인 즉시 반영(준비 전이면 이후 콜백으로)
        }
        else
        {
            Debug.LogWarning("[Title] 씬에 FirebaseAuthManager가 없습니다.");
        }
    }

    private void OnDestroy()
    {
        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.RemoveListener(OnLoginClicked);
        }

        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnAuthStateChanged -= HandleAuthState;
        }
    }

    private void HandleAuthState()
    {
        FirebaseAuthManager manager = FirebaseAuthManager.Instance;
        if (manager != null && manager.IsSignedIn)
        {
            ShowTapToStart();
        }
    }

    private async void OnLoginClicked()
    {
        if (busy)
        {
            return;
        }

        FirebaseAuthManager manager = FirebaseAuthManager.Instance;
        if (manager == null || !manager.IsReady)
        {
            Debug.LogWarning("[Title] 인증이 아직 준비되지 않았습니다.");
            return;
        }

        busy = true;
        googleLoginButton.interactable = false;

        bool success = await manager.SignInWithGoogleAsync();

        busy = false;
        if (success)
        {
            ShowTapToStart();
        }
        else
        {
            googleLoginButton.interactable = true; // 실패/취소 시 재시도 허용
        }
    }

    private void ShowTapToStart()
    {
        SetActiveSafe(LoginVisual(), false);
        SetActiveSafe(tapToStartGroup, true);
    }

    private GameObject LoginVisual()
    {
        if (loginGroup != null)
        {
            return loginGroup;
        }

        return googleLoginButton != null ? googleLoginButton.gameObject : null;
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}

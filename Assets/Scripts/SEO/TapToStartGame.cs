using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 타이틀 화면에서 화면 아무 곳이나 탭/클릭하면 게임 씬을 로드한다.
/// EventSystem 없이도 동작하도록 신형 Input System의 Pointer(마우스/터치 공용)를 직접 폴링한다.
/// </summary>
public class TapToStartGame : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Myth";
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Optional - 'Tap to Start' 깜빡임")]
    [SerializeField] private TMP_Text tapText;
    [SerializeField] private float blinkSpeed = 2f;

    private bool starting;

    private void Update()
    {
        BlinkTapText();

        if (starting)
        {
            return;
        }

        if (WasTapped())
        {
            starting = true;
            if (startDelay > 0f)
            {
                Invoke(nameof(LoadGameScene), startDelay);
            }
            else
            {
                LoadGameScene();
            }
        }
    }

    private void LoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogWarning("[TapToStart] gameSceneName이 비어 있습니다.");
            return;
        }

        // 페이드 아웃 → 비동기 로드 → 페이드 인으로 전환.
        ScreenFader.Instance.LoadScene(gameSceneName, fadeDuration);
    }

    private void BlinkTapText()
    {
        if (tapText == null)
        {
            return;
        }

        Color color = tapText.color;
        color.a = Mathf.Lerp(0.35f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * blinkSpeed * Mathf.PI));
        tapText.color = color;
    }

    private static bool WasTapped()
    {
        // Pointer.current는 마우스/터치/펜을 모두 포괄한다(가장 최근 사용 포인터).
        Pointer pointer = Pointer.current;
        return pointer != null && pointer.press.wasPressedThisFrame;
    }
}

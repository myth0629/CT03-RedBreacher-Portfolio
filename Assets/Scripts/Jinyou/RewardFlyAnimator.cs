using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 클레임 시 클릭 위치에서 상단 재화 아이콘으로 여러 개의 아이콘이 분출했다가
/// 포물선을 그리며 빨려들어가는 연출을 재생한다. 아이콘이 도착하는 시점에 재화 숫자가
/// 카운트업되도록 <see cref="PlayerStatusHud"/>에 hold를 건다. 코루틴 기반(외부 트윈 불필요).
/// </summary>
public class RewardFlyAnimator : MonoBehaviour
{
    private const int IconCount = 5;
    private const float TravelDuration = 0.55f;
    private const float StaggerPerIcon = 0.05f;

    private static RewardFlyAnimator instance;

    private RectTransform overlayRoot;
    private RectTransform creditIcon;
    private RectTransform crystalIcon;
    private PlayerStatusHud hud;

    public static RewardFlyAnimator Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("RewardFlyAnimator");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<RewardFlyAnimator>();
            }

            return instance;
        }
    }

    public void PlayReward(Vector3 sourceWorldPosition, CurrencyType currency, int amount, float iconSize = 56f)
    {
        if (amount <= 0 || !EnsureReferences())
        {
            return;
        }

        RectTransform target = currency == CurrencyType.CoreCrystals ? crystalIcon : creditIcon;
        if (target == null)
        {
            return;
        }

        Sprite sprite = target.GetComponent<Image>() != null ? target.GetComponent<Image>().sprite : null;

        // 도착 시점에 숫자가 오르도록 비행 시간만큼 표시값을 잡아둔다.
        if (hud != null)
        {
            hud.HoldCurrencyDisplay(currency, TravelDuration);
        }

        StartCoroutine(SpawnBurst(sprite, sourceWorldPosition, target, Mathf.Max(32f, iconSize)));
    }

    private IEnumerator SpawnBurst(Sprite sprite, Vector3 startWorld, RectTransform target, float iconSize)
    {
        for (int i = 0; i < IconCount; i++)
        {
            Vector3 endWorld = target != null ? target.position : startWorld;
            StartCoroutine(FlyOne(sprite, startWorld, endWorld, iconSize, i));
            yield return new WaitForSecondsRealtime(StaggerPerIcon);
        }

        yield return new WaitForSecondsRealtime(TravelDuration);
        if (target != null)
        {
            StartCoroutine(Pulse(target));
        }
    }

    private IEnumerator FlyOne(Sprite sprite, Vector3 startWorld, Vector3 endWorld, float iconSize, int index)
    {
        GameObject go = new GameObject("FlyIcon", typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(overlayRoot, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        rect.sizeDelta = new Vector2(iconSize, iconSize);

        Vector3 start = overlayRoot.InverseTransformPoint(startWorld);
        Vector3 end = overlayRoot.InverseTransformPoint(endWorld);

        // 분출: 시작 지점에서 링 형태로 튕겨 나갔다가 타깃으로 모이는 제어점.
        float angle = (index / (float)IconCount) * Mathf.PI * 2f;
        Vector2 burst = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (iconSize * (1.1f + 0.35f * index));
        Vector3 control = start + (Vector3)burst + (end - start) * 0.35f;

        rect.localPosition = start;
        rect.localScale = Vector3.one * 1.3f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / TravelDuration;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            rect.localPosition = Quadratic(start, control, end, e);
            float s = Mathf.Lerp(1.3f, 0.65f, e);
            rect.localScale = new Vector3(s, s, 1f);
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.8f, e));
            yield return null;
        }

        Destroy(go);
    }

    private IEnumerator Pulse(RectTransform target)
    {
        float duration = 0.18f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            target.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private bool EnsureReferences()
    {
        if (overlayRoot != null && creditIcon != null && crystalIcon != null)
        {
            return true;
        }

        GameObject canvasGo = GameObject.Find("UI_Canvas_Game");
        if (canvasGo == null)
        {
            return false;
        }

        if (overlayRoot == null)
        {
            GameObject go = new GameObject("RewardFlyOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayRoot = (RectTransform)go.transform;
            overlayRoot.SetParent(canvasGo.GetComponent<RectTransform>(), false);
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            overlayRoot.localScale = Vector3.one;
            overlayRoot.SetAsLastSibling();
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        Transform t = canvasGo.transform;
        creditIcon = t.Find("Credits_Panel/Credit/Icon") as RectTransform;
        crystalIcon = t.Find("Credits_Panel/Core Crystal/Icon") as RectTransform;
        hud = FindFirstObjectByType<PlayerStatusHud>(FindObjectsInactive.Include);

        return overlayRoot != null && creditIcon != null && crystalIcon != null;
    }

    private static Vector3 Quadratic(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossEncounterHud : MonoBehaviour
{
    [SerializeField] private GameObject bossHudPanel;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private float resultDisplaySeconds = 4f;

    private BossEnemyConfig config;
    private CombatHealth bossHealth;
    private float resultHideTime;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (resultHideTime > 0f && Time.unscaledTime >= resultHideTime)
        {
            Hide();
        }

        Refresh();
    }

    public void Show(BossEnemyConfig bossConfig, CombatHealth health)
    {
        config = bossConfig;
        bossHealth = health;
        resultHideTime = 0f;
        if (bossHudPanel != null)
        {
            bossHudPanel.SetActive(true);
        }

        Refresh();
    }

    public void Hide()
    {
        config = null;
        bossHealth = null;
        resultHideTime = 0f;
        if (bossHudPanel != null)
        {
            bossHudPanel.SetActive(false);
        }
    }

    public void ShowResult(string title, string detail, bool success)
    {
        config = null;
        bossHealth = null;
        resultHideTime = Time.unscaledTime + Mathf.Max(1f, resultDisplaySeconds);
        if (bossHudPanel != null)
        {
            bossHudPanel.SetActive(true);
        }

        // 별도 결과 프리팹 없이 기존 보스 HUD를 결과 표시에도 사용한다.
        if (bossNameText != null)
        {
            bossNameText.text = title;
            bossNameText.color = success
                ? new Color(0.4f, 1f, 0.5f)
                : new Color(1f, 0.35f, 0.35f);
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = detail;
        }

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(false);
        }
    }

    private void Refresh()
    {
        if (config == null || bossHealth == null)
        {
            return;
        }

        float maxHealth = Mathf.Max(1f, bossHealth.MaxHealth);
        float healthRate = Mathf.Clamp01(bossHealth.CurrentHealth / maxHealth);
        if (bossNameText != null)
        {
            bossNameText.text = config.DisplayName;
            bossNameText.color = Color.white;
        }

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(true);
            bossHealthSlider.value = healthRate;
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = $"{bossHealth.CurrentHealth:0} / {maxHealth:0}";
        }
    }
}

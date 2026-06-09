using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossEncounterHud : MonoBehaviour
{
    [SerializeField] private GameObject bossHudPanel;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TMP_Text bossHealthText;

    private BossEnemyConfig config;
    private CombatHealth bossHealth;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        Refresh();
    }

    public void Show(BossEnemyConfig bossConfig, CombatHealth health)
    {
        config = bossConfig;
        bossHealth = health;
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
        if (bossHudPanel != null)
        {
            bossHudPanel.SetActive(false);
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
        }

        if (bossHealthSlider != null)
        {
            bossHealthSlider.value = healthRate;
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = $"{bossHealth.CurrentHealth:0} / {maxHealth:0}";
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusHud : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerController player;
    [SerializeField] private EnemySpawnManager spawnManager;

    [Header("Text")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text statPointText;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image experienceFillImage;

    private void Awake()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<EnemySpawnManager>();
        }
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<EnemySpawnManager>();
        }

        if (spawnManager != null)
        {
            SetText(stageText, $"스테이지 {spawnManager.CurrentStage}");
        }

        if (player == null)
        {
            return;
        }

        CombatHealth health = player.Health;
        PlayerProgression progression = player.Progression;

        // UI 프리팹에서 필요한 필드만 연결해도 동작하도록 null 체크로 갱신한다.
        SetText(nameText, player.DisplayName);

        if (progression != null)
        {
            SetText(levelText, $"LV. {progression.Level}");
            SetText(experienceText, $"{progression.CurrentExperience:0} / {progression.ExperienceToNextLevel:0}");
            SetText(statPointText, $"{progression.StatPoints}");
            SetSlider(experienceSlider, progression.ExperienceProgress01);
            SetFill(experienceFillImage, progression.ExperienceProgress01);
        }

        if (health != null)
        {
            float maxHealth = Mathf.Max(1f, health.MaxHealth);
            float healthRate = Mathf.Clamp01(health.CurrentHealth / maxHealth);
            SetText(healthText, $"{health.CurrentHealth:0} / {maxHealth:0}");
            SetSlider(healthSlider, healthRate);
            SetFill(healthFillImage, healthRate);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetSlider(Slider target, float value)
    {
        if (target != null)
        {
            target.minValue = 0f;
            target.maxValue = 1f;
            target.value = value;
        }
    }

    private static void SetFill(Image target, float value)
    {
        if (target != null)
        {
            target.fillAmount = value;
        }
    }
}

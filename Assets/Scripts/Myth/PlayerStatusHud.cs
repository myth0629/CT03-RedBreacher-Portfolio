using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerStatusHud : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerController player;
    [SerializeField] private EnemySpawnManager spawnManager;
    [SerializeField] private PlayerCurrencyWallet currencyWallet;

    [Header("Text")]
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private TMP_Text coreCrystalsText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text statPointText;

    [Header("Tank Popup")]
    [SerializeField] private TMP_Text tankPopupStageText;
    [SerializeField] private TMP_Text tankPopupNameText;
    [SerializeField] private TMP_Text tankPopupLevelText;
    [SerializeField] private TMP_Text tankPopupHealthText;
    [SerializeField] private TMP_Text tankPopupDpsText;
    [SerializeField] private TMP_Text tankPopupMoveSpeedText;
    [SerializeField] private TMP_Text tankPopupCritText;
    [SerializeField] private TMP_Text tankPopupCritChanceText;
    [SerializeField] private TMP_Text tankPopupCritMultiplierText;
    [SerializeField] private TMP_Text tankPopupWeaponNameText;
    [SerializeField] private TMP_Text tankPopupWeaponCategoryText;
    [SerializeField] private TMP_Text tankPopupWeaponDamageText;
    [SerializeField] private TMP_Text tankPopupWeaponRangeText;
    [SerializeField] private TMP_Text tankPopupWeaponFireIntervalText;
    [SerializeField] private TMP_Text tankPopupWeaponSpeedText;
    [SerializeField] private TMP_Text tankPopupWeaponLifetimeText;
    [SerializeField] private TMP_Text tankPopupWeaponKnockbackText;

    [Header("Stat Upgrade Popup")]
    [SerializeField] private TMP_Text statUpgradePointText;
    [SerializeField] private TMP_Text attackUpgradeLevelText;
    [SerializeField] private TMP_Text healthUpgradeLevelText;
    [SerializeField] private TMP_Text critChanceUpgradeLevelText;
    [SerializeField] private TMP_Text critMultiplierUpgradeLevelText;
    [SerializeField] private TMP_Text attackUpgradeAmountText;
    [SerializeField] private TMP_Text healthUpgradeAmountText;
    [SerializeField] private TMP_Text critChanceUpgradeAmountText;
    [SerializeField] private TMP_Text critMultiplierUpgradeAmountText;
    [SerializeField] private Button attackUpgradeButton;
    [SerializeField] private Button healthUpgradeButton;
    [SerializeField] private Button critChanceUpgradeButton;
    [SerializeField] private Button critMultiplierUpgradeButton;
    [SerializeField] private TMP_Text stattankPopupHealthText;
    [SerializeField] private TMP_Text stattankPopupDpsText;
    [SerializeField] private TMP_Text stattankPopupMoveSpeedText;
    [SerializeField] private TMP_Text stattankPopupCritChanceText;
    [SerializeField] private TMP_Text stattankPopupCritMultiplierText;
    
    [Header("GameQuit Popup")]
    [SerializeField] private GameObject gameQuitPopup;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image experienceFillImage;
    [SerializeField] private Image roundProgressFillImage;

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

        if (currencyWallet == null)
        {
            currencyWallet = ResolveCurrencyWallet();
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
            string stageValue = $"스테이지 {spawnManager.CurrentStage}";
            string roundValue = $"{spawnManager.CurrentRoundInStage}/{spawnManager.RoundsPerStage}";
            float roundProgress = spawnManager.RoundsPerStage > 0
                ? Mathf.Clamp01((float)spawnManager.CurrentRoundInStage / spawnManager.RoundsPerStage)
                : 0f;

            SetText(stageText, stageValue);
            SetText(roundText, roundValue);
            SetText(tankPopupStageText, stageValue);
            SetFill(roundProgressFillImage, roundProgress);
        }

        if (currencyWallet == null)
        {
            currencyWallet = ResolveCurrencyWallet();
        }

        if (currencyWallet != null)
        {
            SetText(creditsText, $"{currencyWallet.Credits}");
            SetText(coreCrystalsText, $"{currencyWallet.CoreCrystals}");
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

        RefreshTankPopup(health, progression);
        RefreshStatUpgradePopup(progression);

        if (gameQuitPopup != null)
        {
            if (WasGameQuitPopupRequested() && !gameQuitPopup.activeSelf)
            {
                gameQuitPopup.SetActive(true);
            }
        }
    }

    // PC버전의 ESC키나 스마트폰 버전의 백버튼을 누르면 게임종료 팝업창을 활성화
    private bool WasGameQuitPopupRequested()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    public void GameQuit()
    {
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void RefreshTankPopup(CombatHealth health, PlayerProgression progression)
    {
        if (player == null)
        {
            return;
        }

        ProjectileConfig weapon = player.WeaponConfig;

        // 탱크 팝업은 연결된 텍스트만 선택적으로 갱신한다.
        SetText(tankPopupNameText, player.DisplayName);
        SetText(tankPopupLevelText, progression != null ? $"[Lv. {progression.Level}]" : "[1]");
        SetText(tankPopupHealthText, health != null ? $"{health.CurrentHealth:0}" : "0");
        SetText(tankPopupDpsText, $"{player.EstimatedDamagePerSecond:0.##}");
        SetText(tankPopupMoveSpeedText, $"{player.MoveSpeed:0.##}");
        SetText(tankPopupCritText, $"{player.CritChance * 100f:0.#}% / {player.CritMultiplier:0.##}x");
        SetText(tankPopupCritChanceText, $"{player.CritChance * 100f:0.#}%");
        SetText(tankPopupCritMultiplierText, $"{player.CritMultiplier:0.##}x");
        SetText(tankPopupWeaponNameText, weapon != null ? weapon.DisplayName : "장착한 무기 없음");
        SetText(tankPopupWeaponCategoryText, weapon != null ? weapon.WeaponCategory : "무기 카테고리");
        SetText(tankPopupWeaponDamageText, $"{player.WeaponAttackDamage:0.##}");
        SetText(tankPopupWeaponRangeText, $"{player.AttackRange:0.##}");
        SetText(tankPopupWeaponFireIntervalText, $"{player.AttackInterval:0.##}");
        SetText(tankPopupWeaponSpeedText, $"{player.ProjectileSpeed:0.##}");
        SetText(tankPopupWeaponLifetimeText, $"{player.ProjectileLifetime:0.##}");
        SetText(tankPopupWeaponKnockbackText, $"{player.KnockbackForce:0.##}");
        SetText(stattankPopupHealthText, health != null ? $"{health.CurrentHealth:0}" : "0");
        SetText(stattankPopupDpsText, $"{player.EstimatedDamagePerSecond:0.##}");
        SetText(stattankPopupMoveSpeedText, $"{player.MoveSpeed:0.##}");
        SetText(stattankPopupCritChanceText, $"{player.CritChance * 100f:0.#}%");
        SetText(stattankPopupCritMultiplierText, $"{player.CritMultiplier:0.##}x");
    }

    private void RefreshStatUpgradePopup(PlayerProgression progression)
    {
        PlayerStatAllocator allocator = player != null ? player.StatAllocator : null;
        if (allocator == null)
        {
            return;
        }

        // 미투자 상태를 UI에서는 Lv.1로 표시한다.
        SetText(statUpgradePointText, progression != null ? $"보유 중인 포인트 : {progression.StatPoints}" : "보유 중인 포인트 : 0");
        SetText(attackUpgradeLevelText, $"Lv.{allocator.AttackDisplayLevel}");
        SetText(healthUpgradeLevelText, $"Lv.{allocator.HealthDisplayLevel}");
        SetText(critChanceUpgradeLevelText, $"Lv.{allocator.CritChanceDisplayLevel}");
        SetText(critMultiplierUpgradeLevelText, $"Lv.{allocator.CritMultiplierDisplayLevel}");
        SetText(attackUpgradeAmountText, $"공격력 +{allocator.AttackBonusPercent * 100f:0.##}%");
        SetText(healthUpgradeAmountText, $"최대 체력 +{allocator.HealthBonusPercent * 100f:0.##}%");
        SetText(critChanceUpgradeAmountText, $"치명타 확률 +{allocator.CritChanceBonus * 100f:0.##}%");
        SetText(critMultiplierUpgradeAmountText, $"치명타 피해 +{allocator.CritMultiplierBonus * 100f:0.##}%");

        bool hasPoint = progression != null && progression.StatPoints > 0;
        SetButtonInteractable(attackUpgradeButton, hasPoint && allocator.CanUpgradeAttack);
        SetButtonInteractable(healthUpgradeButton, hasPoint && allocator.CanUpgradeHealth);
        SetButtonInteractable(critChanceUpgradeButton, hasPoint && allocator.CanUpgradeCritChance);
        SetButtonInteractable(critMultiplierUpgradeButton, hasPoint && allocator.CanUpgradeCritMultiplier);
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

    private static void SetButtonInteractable(Button target, bool interactable)
    {
        if (target != null)
        {
            target.interactable = interactable;
        }
    }

    private PlayerCurrencyWallet ResolveCurrencyWallet()
    {
        if (player != null)
        {
            PlayerCurrencyWallet playerWallet = player.GetComponent<PlayerCurrencyWallet>();
            if (playerWallet != null)
            {
                return playerWallet;
            }
        }

        if (BaseCampManager.Instance != null)
        {
            return BaseCampManager.Instance.CurrencyWallet;
        }

        return FindFirstObjectByType<PlayerCurrencyWallet>();
    }
}

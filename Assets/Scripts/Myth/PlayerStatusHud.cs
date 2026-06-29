using DG.Tweening;
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
    [SerializeField] private BossEncounterManager bossEncounterManager;

    [Header("inGamePanels")]
    [SerializeField] private GameObject stagePanel;
    
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
    [SerializeField] private Image tankPopupEquipWeaponIcon;
    [SerializeField] private RawImage tankPopupEquipDroneIcon;
    private Image tankPopupEquipDronePlaceholderImage;
    private PlayerDroneController droneController;
    private GameObject renderedTankPopupDronePrefab;

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
    
    [Header("TraitPointFacility")]
    [SerializeField] private TMP_Text statPointText_base;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image experienceFillImage;
    [SerializeField] private Image roundProgressFillImage;

    // 재화 표시 애니메이터(DOTween 카운트업). 보상 흡수 연출 도착(hold) 이후에 숫자가 올라간다.
    private readonly CurrencyDisplay creditsDisplay = new CurrencyDisplay();
    private readonly CurrencyDisplay coreCrystalsDisplay = new CurrencyDisplay();
    private float creditHoldUntil;
    private float coreCrystalHoldUntil;

    // 바(체력/경험치/라운드) 채움을 DOTween으로 부드럽게 보간하는 애니메이터.
    private readonly BarAnimator healthSliderAnim = new BarAnimator();
    private readonly BarAnimator experienceSliderAnim = new BarAnimator();
    private readonly BarAnimator healthFillAnim = new BarAnimator();
    private readonly BarAnimator experienceFillAnim = new BarAnimator();
    private readonly BarAnimator roundProgressAnim = new BarAnimator();

    /// <summary>지정 재화의 표시 숫자 갱신을 잠시 멈춘다(보상 비행 동안 숫자 고정 → 도착 시 카운트업).</summary>
    public void HoldCurrencyDisplay(CurrencyType currency, float seconds)
    {
        float until = Time.unscaledTime + Mathf.Max(0f, seconds);
        if (currency == CurrencyType.CoreCrystals)
        {
            coreCrystalHoldUntil = until;
        }
        else
        {
            creditHoldUntil = until;
        }
    }

    // 재화 표시값을 DOTween으로 카운트업한다. 소비(감소)는 즉시, 획득(증가)은 hold 이후 트윈.
    private class CurrencyDisplay
    {
        private const float CountUpDuration = 0.4f;

        private long shown = -1;
        private long animatingTo;
        private Tween tween;

        public void Apply(TMP_Text text, long real, float holdUntil)
        {
            if (shown < 0 || real < shown)
            {
                // 최초 표시 또는 소비(감소)는 즉시 반영한다.
                tween?.Kill();
                tween = null;
                shown = real;
                animatingTo = real;
            }
            else if (real > shown && real != animatingTo && Time.unscaledTime >= holdUntil)
            {
                // 획득(증가)은 hold가 끝난 뒤 현재값에서 실제값까지 카운트업한다.
                animatingTo = real;
                tween?.Kill();
                tween = DOTween.To(() => shown, value => shown = value, real, CountUpDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }

            if (text != null)
            {
                text.text = shown.ToString();
            }
        }

        public void Kill()
        {
            tween?.Kill();
            tween = null;
        }
    }

    // 채움 게이지를 목표값이 바뀔 때만 DOTween으로 보간한다(매 프레임 트윈 생성 방지).
    private class BarAnimator
    {
        private const float FillDuration = 0.25f;

        private float target = float.NaN;
        private Tween tween;

        public void ApplyFill(Image image, float value)
        {
            if (image == null)
            {
                return;
            }

            if (!Mathf.Approximately(target, value))
            {
                target = value;
                tween?.Kill();
                tween = image.DOFillAmount(value, FillDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }

        public void ApplySlider(Slider slider, float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            if (!Mathf.Approximately(target, value))
            {
                target = value;
                tween?.Kill();
                tween = slider.DOValue(value, FillDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }

        public void Kill()
        {
            tween?.Kill();
            tween = null;
        }
    }

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

        ResolveBossEncounterManager();
        ResolveTankPopupIconReferences();
        ResolveDroneController();
    }

    private void OnEnable()
    {
        SubscribeBossEncounter();
        RefreshStagePanelVisibility();
    }

    private void OnDisable()
    {
        if (bossEncounterManager != null)
        {
            bossEncounterManager.EncounterStarted -= HandleBossEncounterStarted;
            bossEncounterManager.EncounterEnded -= HandleBossEncounterEnded;
        }
    }

    private void OnDestroy()
    {
        // 파괴된 UI를 대상으로 트윈이 남지 않도록 모두 정리한다.
        creditsDisplay.Kill();
        coreCrystalsDisplay.Kill();
        healthSliderAnim.Kill();
        experienceSliderAnim.Kill();
        healthFillAnim.Kill();
        experienceFillAnim.Kill();
        roundProgressAnim.Kill();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        RefreshStagePanelVisibility();

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
            roundProgressAnim.ApplyFill(roundProgressFillImage, roundProgress);
        }

        if (currencyWallet == null)
        {
            currencyWallet = ResolveCurrencyWallet();
        }

        if (currencyWallet != null)
        {
            creditsDisplay.Apply(creditsText, currencyWallet.Credits, creditHoldUntil);
            coreCrystalsDisplay.Apply(coreCrystalsText, currencyWallet.CoreCrystals, coreCrystalHoldUntil);
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
            experienceSliderAnim.ApplySlider(experienceSlider, progression.ExperienceProgress01);
            experienceFillAnim.ApplyFill(experienceFillImage, progression.ExperienceProgress01);
        }

        if (health != null)
        {
            float maxHealth = Mathf.Max(1f, health.MaxHealth);
            float healthRate = Mathf.Clamp01(health.CurrentHealth / maxHealth);
            SetText(healthText, $"{health.CurrentHealth:0} / {maxHealth:0}");
            healthSliderAnim.ApplySlider(healthSlider, healthRate);
            healthFillAnim.ApplyFill(healthFillImage, healthRate);
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

    // PC버전(에디터)의 ESC키나 안드로이드 버전의 백버튼을 누르면 게임종료 팝업창을 활성화
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
        ResolveTankPopupIconReferences();
        ResolveDroneController();
        DroneConfig drone = droneController != null ? droneController.DroneConfig : null;

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
        SetIcon(tankPopupEquipWeaponIcon, weapon != null ? weapon.Icon : null);
        SetDronePreview(tankPopupEquipDroneIcon, drone);
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
        SetText(statPointText_base, progression != null ? $"보유 포인트 : {progression.StatPoints}" : "보유 포인트 : 0");
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

    private static void SetIcon(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
        target.preserveAspect = true;
    }

    private void SetDronePreview(RawImage target, DroneConfig drone)
    {
        if (target == null)
        {
            return;
        }

        GameObject prefab = drone != null ? drone.DronePrefab : null;
        if (prefab == null)
        {
            target.texture = null;
            target.color = Color.clear;
            target.enabled = false;
            if (tankPopupEquipDronePlaceholderImage != null)
            {
                tankPopupEquipDronePlaceholderImage.enabled = true;
            }
            renderedTankPopupDronePrefab = null;
            return;
        }

        if (renderedTankPopupDronePrefab == prefab && target.texture != null)
        {
            return;
        }

        // 드론은 프리팹을 장착 정보로 쓰기 때문에 유닛 프리뷰 렌더러를 그대로 재사용한다.
        RenderTexture preview = UnitPreviewRenderer.Instance.GetPreview(prefab);
        target.texture = preview;
        target.color = preview != null ? Color.white : Color.clear;
        target.enabled = preview != null;
        if (tankPopupEquipDronePlaceholderImage != null)
        {
            tankPopupEquipDronePlaceholderImage.enabled = preview == null;
        }
        renderedTankPopupDronePrefab = preview != null ? prefab : null;
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

    private void ResolveDroneController()
    {
        if (droneController != null)
        {
            return;
        }

        droneController = player != null
            ? player.GetComponent<PlayerDroneController>()
            : FindFirstObjectByType<PlayerDroneController>(FindObjectsInactive.Include);
    }

    private void ResolveTankPopupIconReferences()
    {
        if (tankPopupEquipWeaponIcon == null)
        {
            Transform tankWeaponRoot = FindChildTransformByName(transform, "TankWeapon");
            tankPopupEquipWeaponIcon = FindChildComponentByName<Image>(tankWeaponRoot, "Weapon_Icon");
            tankPopupEquipWeaponIcon ??= FindChildComponentByName<Image>(transform, "Weapon_Icon");
        }

        if (tankPopupEquipDroneIcon != null)
        {
            return;
        }

        Transform tankDroneRoot = FindChildTransformByName(transform, "TankDrone");
        tankPopupEquipDroneIcon = FindChildComponentByName<RawImage>(tankDroneRoot, "Drone_Icon");
        if (tankPopupEquipDroneIcon != null)
        {
            return;
        }

        Image droneImage = FindChildComponentByName<Image>(tankDroneRoot, "Drone_Icon");
        if (droneImage == null)
        {
            droneImage = FindChildComponentByName<Image>(transform, "Drone_Icon");
        }

        if (droneImage == null)
        {
            return;
        }

        // 기존 프리팹은 Image 슬롯이라, 같은 위치에 런타임 프리뷰용 RawImage 자식을 만든다.
        tankPopupEquipDronePlaceholderImage = droneImage;
        tankPopupEquipDroneIcon = droneImage.GetComponentInChildren<RawImage>(true);
        tankPopupEquipDroneIcon ??= CreateDronePreviewImage(droneImage.rectTransform);
    }

    private static RawImage CreateDronePreviewImage(RectTransform parent)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject previewObject = new GameObject("Drone_Icon_Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.SetParent(parent, false);
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;
        previewRect.localScale = Vector3.one;

        RawImage previewImage = previewObject.GetComponent<RawImage>();
        previewImage.raycastTarget = false;
        previewImage.color = Color.clear;
        previewImage.enabled = false;
        return previewImage;
    }

    private static Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == childName)
            {
                return component;
            }
        }

        return null;
    }

    private void SubscribeBossEncounter()
    {
        ResolveBossEncounterManager();
        if (bossEncounterManager == null)
        {
            return;
        }

        bossEncounterManager.EncounterStarted -= HandleBossEncounterStarted;
        bossEncounterManager.EncounterEnded -= HandleBossEncounterEnded;
        bossEncounterManager.EncounterStarted += HandleBossEncounterStarted;
        bossEncounterManager.EncounterEnded += HandleBossEncounterEnded;
    }

    private void ResolveBossEncounterManager()
    {
        bossEncounterManager ??= FindFirstObjectByType<BossEncounterManager>();
    }

    private void RefreshStagePanelVisibility()
    {
        ResolveBossEncounterManager();
        bool shouldHideForBossHud = bossEncounterManager != null
            && (bossEncounterManager.IsEncounterActive || bossEncounterManager.IsBossHudVisible);
        SetActive(stagePanel, !shouldHideForBossHud);
    }

    private void HandleBossEncounterEnded(bool cleared)
    {
        RefreshStagePanelVisibility();
    }

    private void HandleBossEncounterStarted()
    {
        RefreshStagePanelVisibility();
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}

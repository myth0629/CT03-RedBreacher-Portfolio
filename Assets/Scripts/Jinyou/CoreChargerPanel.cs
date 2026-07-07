using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerPanel : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeRemainingText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image coinIcon;

    [Header("Panels")] 
    [SerializeField] private GameObject tankUnitSubPanel;
    [SerializeField] private GameObject droneUnlockSubPanel;
    
    [Header("TankUnit subPanel")]
    [SerializeField] private Button enhanceUnitButton;
    [SerializeField] private TMP_Text enhanceUnitButtonStateText;
    [SerializeField] private TMP_Text enhanceUnitEmptyText;
    [SerializeField] private RawImage currentUnitPreviewImage;
    [SerializeField] private TMP_Text currentUnitText;
    [SerializeField] private RawImage enhanceUnitPreviewImage;
    [SerializeField] private TMP_Text enhanceUnitText;

    [Header("TankUnit subPanel Status")]
    [SerializeField] private TMP_Text enhanceUnitHealthText;
    [SerializeField] private TMP_Text enhanceUnitDamageText;
    [SerializeField] private TMP_Text enhanceUnitSpeedText;
    [SerializeField] private TMP_Text enhanceUnitCritChanceText;

    [Header("TankUnit DetailStatus")]
    [SerializeField] private TMP_Text unitStatusDetailText;

    [Header("DroneUnlock subPanel")]
    [SerializeField] private Button unlockDroneButton;
    [SerializeField] private TMP_Text unlockDroneButtonStateText;
    [SerializeField] private RawImage unlockDronePreviewImage;
    [SerializeField] private TMP_Text unlockDroneText;
    [SerializeField] private TMP_Text unlockDroneEmptyText;

    [Header("DroneUnlock subPanel")]
    [SerializeField] private TMP_Text unlockDroneCountText;
    [SerializeField] private TMP_Text unlockDroneEquipWeaponText;
    [SerializeField] private TMP_Text unlockDroneDamageText;
    [SerializeField] private TMP_Text unlockDroneProjSpeedText;
    [SerializeField] private TMP_Text unlockDroneMoveSpeedText;
    
    [Header("Drone DetailStatus")]
    [SerializeField] private TMP_Text droneStatusDetailText;

    private CoreCharger coreCharger;
    private InventoryFacility inventory;
    private PlayerController player;
    private float observedUpgradeDuration;

    // 상점의 무기/스킬 서브패널처럼 감춰야 할 패널을 감추지 못하고
    // 다른 패널이 나오는 꼬이는 문제를 방지하기 위해 스크립트로 강제제어
    void Start()
    {
        tankUnitSubPanel.SetActive(true);
        droneUnlockSubPanel.SetActive(false);
    }
    
    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeCoreCharger);
        enhanceUnitButton?.onClick.AddListener(ConvertCurrentUnit);
        unlockDroneButton?.onClick.AddListener(UnlockNextDrone);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeCoreCharger);
        enhanceUnitButton?.onClick.RemoveListener(ConvertCurrentUnit);
        unlockDroneButton?.onClick.RemoveListener(UnlockNextDrone);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button upgrade,
        TMP_Text level,
        TMP_Text upgradeLabel,
        TMP_Text selectedUnit)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        levelText = level;
        upgradeText = upgradeLabel;
        currentUnitText = selectedUnit;
        Refresh();
    }

    private void ConvertCurrentUnit()
    {
        baseCampManager?.ConvertSelectedCoreUnit();
        Refresh();
    }

    private void UpgradeCoreCharger()
    {
        baseCampManager?.UpgradeCoreCharger();
        Refresh();
    }

    private void UnlockNextDrone()
    {
        ResolveReferences();
        coreCharger?.TryUnlockNextDrone(inventory);
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (coreCharger == null)
        {
            SetActive(coinIcon != null ? coinIcon.gameObject : null, false);
            SetUpgradeRemainingText(upgradeRemainingText, false, 0f);
            SetActive(upgradeText != null ? upgradeText.gameObject : null, true);
            SetText(enhanceUnitButtonStateText, "코어 차저가 연결되지 않았습니다.");
            SetEnhanceUnitConditionText(string.Empty);
            SetText(currentUnitText, string.Empty);
            SetText(enhanceUnitText, string.Empty);
            RefreshEnhanceUnitStatTexts(null);
            SetUnitPreview(currentUnitPreviewImage, null);
            SetUnitPreview(enhanceUnitPreviewImage, null);
            RefreshDroneUnlockPanel();
            SetInteractable(upgradeButton, false);
            SetInteractable(enhanceUnitButton, false);
            return;
        }

        int playerLevel = GetPlayerLevel();
        CoreCharger.UnitConversionStage stage = coreCharger.CurrentConversionStage;

        int researchLabLevel = baseCampManager?.CommandCenter != null
            ? baseCampManager.CommandCenter.Level
            : 1;

        SetText(levelText, $"Lv. {coreCharger.Level}");
        BaseCampUpgradeButtonText.Set(
            upgradeText,
            upgradeCostText,
            "기지 업그레이드",
            coreCharger.UpgradeCost,
            !coreCharger.IsUpgrading && coreCharger.Level < coreCharger.MaxLevel);
        SetUpgradeRemainingText(
            upgradeRemainingText,
            coreCharger.IsUpgrading,
            coreCharger.UpgradeRemainingSeconds);
        SetActive(upgradeText != null ? upgradeText.gameObject : null, !coreCharger.IsUpgrading);
        SetActive(coinIcon != null ? coinIcon.gameObject : null, !coreCharger.IsUpgrading);
        SetText(currentUnitText, stage != null ? FormatUnitName(stage.currentUnit) : "모든 변환 완료");
        SetText(enhanceUnitText, stage != null ? FormatUnitName(stage.nextUnit) : string.Empty);
        SetUnitPreview(currentUnitPreviewImage, stage?.currentUnit);
        SetUnitPreview(enhanceUnitPreviewImage, stage?.nextUnit);
        SetText(unitStatusDetailText, BuildUnitDetailStatusText(stage));
        RefreshEnhanceUnitStatTexts(stage);

        bool canConvert = coreCharger.CanConvertCurrentUnit(inventory, player, playerLevel);
        SetInteractable(enhanceUnitButton, canConvert);
        SetText(enhanceUnitButtonStateText, BuildEnhanceUnitButtonStateText(stage, playerLevel));
        SetEnhanceUnitConditionText(BuildEnhanceUnitConditionText(stage, playerLevel));
        SetEnhanceUnitButtonLabel(stage != null ? "유닛 강화" : "완료");
        RefreshDroneUnlockPanel();

        BaseCampUpgradeStatus.SetUpgradeProgress(
            upgradeProgressFill,
            coreCharger,
            ref observedUpgradeDuration);
        SetInteractable(upgradeButton, coreCharger.CanStartUpgrade(
            baseCampManager != null ? baseCampManager.Credits : 0,
            baseCampManager != null ? baseCampManager.CommanderLevel : 1,
            researchLabLevel));
    }

    private string BuildEnhanceUnitButtonStateText(CoreCharger.UnitConversionStage stage, int playerLevel)
    {
        if (coreCharger == null)
        {
            return "코어 차저가 연결되지 않았습니다.";
        }

        if (stage == null)
        {
            return coreCharger.ConversionStages.Count == 0
                ? "유닛 강화 단계가 설정되지 않았습니다."
                : "모든 유닛 강화가 완료되었습니다.";
        }

        if (!stage.IsConfigured)
        {
            return "현재 탱크와 다음 탱크 데이터가 필요합니다.";
        }

        int requiredCoreLevel = coreCharger.GetRequiredCoreChargerLevel(coreCharger.CurrentStageIndex);
        bool ownsCurrentUnit = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool hasCurrentUnitEquipped = player != null && player.UnitConfig == stage.currentUnit;

        if (playerLevel >= stage.requiredPlayerLevel
            && coreCharger.Level >= requiredCoreLevel
            && (ownsCurrentUnit || hasCurrentUnitEquipped))
        {
            return "강화 가능";
        }

        return string.Empty;
    }

    private string BuildEnhanceUnitConditionText(CoreCharger.UnitConversionStage stage, int playerLevel)
    {
        if (coreCharger == null || stage == null || !stage.IsConfigured)
        {
            return string.Empty;
        }

        int requiredCoreLevel = coreCharger.GetRequiredCoreChargerLevel(coreCharger.CurrentStageIndex);
        bool ownsCurrentUnit = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool hasCurrentUnitEquipped = player != null && player.UnitConfig == stage.currentUnit;

        string message = string.Empty;
        if (playerLevel < stage.requiredPlayerLevel)
        {
            message += $"- 플레이어 Lv.{stage.requiredPlayerLevel} 필요 (현재 Lv.{playerLevel})";
        }

        if (coreCharger.Level < requiredCoreLevel)
        {
            message += $"{(message.Length > 0 ? "\n" : string.Empty)}"
                + $"- 코어 차저 Lv.{requiredCoreLevel} 필요 (현재 Lv.{coreCharger.Level})";
        }

        if (!ownsCurrentUnit && !hasCurrentUnitEquipped)
        {
            message += $"{(message.Length > 0 ? "\n" : string.Empty)}"
                + $"- {stage.currentUnit.DisplayName} 보유 또는 장착 필요";
        }

        return message;
    }

    private void SetEnhanceUnitConditionText(string message)
    {
        SetText(enhanceUnitEmptyText, message);
        SetActive(
            enhanceUnitEmptyText != null ? enhanceUnitEmptyText.gameObject : null,
            !string.IsNullOrEmpty(message));
    }

    private void RefreshDroneUnlockPanel()
    {
        if (coreCharger == null)
        {
            SetInteractable(unlockDroneButton, false);
            SetText(unlockDroneButtonStateText, "해금 불가");
            SetUnlockDroneConditionText("코어 차저가 연결되지 않았습니다.");
            SetText(unlockDroneText, string.Empty);
            SetText(unlockDroneCountText, string.Empty);
            SetText(unlockDroneEquipWeaponText, string.Empty);
            SetDronePreview(unlockDronePreviewImage, null);
            RefreshUnlockDroneStatTexts(null);
            SetText(droneStatusDetailText, string.Empty);
            return;
        }

        CoreCharger.DroneUnlock nextUnlock = coreCharger.GetNextLockedDroneUnlock(inventory);
        DroneConfig nextDrone = nextUnlock?.droneConfig;

        SetInteractable(unlockDroneButton, coreCharger.CanUnlockNextDrone(inventory));
        SetText(unlockDroneButtonStateText, BuildUnlockDroneButtonStateText(nextUnlock));
        SetUnlockDroneConditionText(BuildUnlockDroneConditionText(nextUnlock));
        SetText(unlockDroneText, nextDrone != null ? nextDrone.DisplayName : "모든 드론 해금 완료");
        SetText(unlockDroneCountText, nextDrone != null ? $"{nextDrone.DroneCount}마리" : string.Empty);
        SetText(unlockDroneEquipWeaponText, nextDrone?.ProjectileConfig != null
            ? nextDrone.ProjectileConfig.DisplayName
            : string.Empty);
        SetDronePreview(unlockDronePreviewImage, nextDrone);
        RefreshUnlockDroneStatTexts(nextDrone);
        SetText(droneStatusDetailText, BuildDroneDetailStatusText(nextDrone));
    }

    /// <summary>
    /// unlockDroneButtonStateText를 통해서 탱크 유닛을 강화할 때 필요한 조건이 무엇인지 확인할 수 있으며,
    /// 현재로는 코어 차저 레벨에 따라 드론 해금하기로 조건을 걸어놨음.
    /// 예: 드론 해금 조건 \n 코어 차저 Lv. 3 필요 (현재 Lv. 1)
    /// </summary>
    /// <param name="nextUnlock"></param>
    /// <returns></returns>
    private string BuildUnlockDroneButtonStateText(CoreCharger.DroneUnlock nextUnlock)
    {
        return string.IsNullOrEmpty(BuildUnlockDroneConditionText(nextUnlock))
            ? $"{nextUnlock.droneConfig.DisplayName} 해금하기"
            : string.Empty;
    }

    private string BuildUnlockDroneConditionText(CoreCharger.DroneUnlock nextUnlock)
    {
        if (coreCharger == null)
        {
            return "코어 차저가 연결되지 않았습니다.";
        }

        if (inventory == null)
        {
            return "인벤토리가 연결되지 않았습니다.";
        }

        if (nextUnlock?.droneConfig == null)
        {
            return coreCharger.DroneUnlocks == null || coreCharger.DroneUnlocks.Count == 0
                ? "드론 해금 목록이 설정되지 않았습니다."
                : "모든 드론이 해금되었습니다.";
        }

        int requiredLevel = Mathf.Max(1, nextUnlock.requiredCoreChargerLevel);
        return coreCharger.Level < requiredLevel
            ? $"코어 차저 Lv.{requiredLevel} 필요 (현재 Lv.{coreCharger.Level})"
            : string.Empty;
    }

    private void SetUnlockDroneConditionText(string message)
    {
        SetText(unlockDroneEmptyText, message);
        SetActive(
            unlockDroneEmptyText != null ? unlockDroneEmptyText.gameObject : null,
            !string.IsNullOrEmpty(message));
    }

    // UnlockDrone 스텟 관련 텍스트에 연결
    private void RefreshUnlockDroneStatTexts(DroneConfig drone)
    {
        if (drone == null)
        {
            SetText(unlockDroneDamageText, string.Empty);
            SetText(unlockDroneProjSpeedText, string.Empty);
            SetText(unlockDroneMoveSpeedText, string.Empty);
            return;
        }

        SetText(unlockDroneDamageText, $"{drone.AttackDamage:0.##}");
        SetText(unlockDroneProjSpeedText, $"{drone.ProjectileSpeed:0.##}");
        SetText(unlockDroneMoveSpeedText, $"{drone.FollowSpeed:0.##}");
    }

    // TankUnit subPanel Status에 연결
    private static string BuildDroneDetailStatusText(DroneConfig drone)
    {
        if (drone == null)
        {
            return string.Empty;
        }

        string weaponName = drone.ProjectileConfig != null
            ? drone.ProjectileConfig.DisplayName
            : string.Empty;
        return $"{drone.DroneCount}마리\n"
            + $"{weaponName}\n"
            + $"{drone.AttackDamage:0.##}\n"
            + $"{drone.AttackRange:0.##}\n"
            + $"{drone.AttackInterval:0.##}\n"
            + $"{drone.FollowSpeed:0.##}\n"
            + $"{drone.FollowRadius:0.##}";
    }

    private void RefreshEnhanceUnitStatTexts(CoreCharger.UnitConversionStage stage)
    {
        if (stage == null || stage.currentUnit == null || stage.nextUnit == null)
        {
            SetText(enhanceUnitHealthText, string.Empty);
            SetText(enhanceUnitDamageText, string.Empty);
            SetText(enhanceUnitSpeedText, string.Empty);
            SetText(enhanceUnitCritChanceText, string.Empty);
            return;
        }

        PlayerUnitConfig current = stage.currentUnit;
        PlayerUnitConfig next = stage.nextUnit;
        SetText(enhanceUnitHealthText, FormatDodgeCooldownSecChange(current.MaxHealth, next.MaxHealth));
        SetText(enhanceUnitDamageText, FormatDodgeCooldownSecChange(current.AttackDamage, next.AttackDamage));
        SetText(enhanceUnitSpeedText, FormatDodgeCooldownSecChange(current.MoveSpeed, next.MoveSpeed));
        SetText(enhanceUnitCritChanceText, FormatPlainPercentChange(current.CritChance, next.CritChance));
    }

    // TankUnit DetailStatus에 연결
    private static string BuildUnitDetailStatusText(CoreCharger.UnitConversionStage stage)
    {
        if (stage == null)
        {
            return "대기 중인 유닛 SO 변환 없음";
        }

        if (stage.currentUnit == null || stage.nextUnit == null)
        {
            return $"변환 전 SO: {FormatUnitName(stage.currentUnit)}\n"
                + $"변환 후 SO: {FormatUnitName(stage.nextUnit)}";
        }

        PlayerUnitConfig current = stage.currentUnit;
        PlayerUnitConfig next = stage.nextUnit;
        // 순서 => 내구도, 피해량, 사거리, 발사간격, 이동 속도, 회전속도, 치명타 확률, 치명타 피해
        return $"{FormatStatChange(current.MaxHealth, next.MaxHealth)}\n"
            + $"{FormatStatChange(current.AttackDamage, next.AttackDamage)}\n"
            + $"{FormatStatChange(current.AttackRange, next.AttackRange)}\n"
            + $"{FormatStatChange(current.AttackInterval, next.AttackInterval)}\n"
            + $"{FormatStatChange(current.MoveSpeed, next.MoveSpeed)}\n"
            + $"{FormatStatChange(current.RotationSpeed, next.RotationSpeed)}\n"
            + $"{FormatPercentChange(current.CritChance, next.CritChance)}\n"
            + $"{FormatPercentChange(current.CritMultiplier, next.CritMultiplier)}\n"
            + $"{FormatDodgeCooldownSecChange(current.BossDodgeCooldown, next.BossDodgeCooldown)}\n";
    }

    // 탱크 유닛 유/무 판별
    private static string FormatUnitName(PlayerUnitConfig unitConfig)
    {
        return unitConfig != null
            ? unitConfig.DisplayName
            : "미지정";
    }

    private static string FormatStatChange(float current, float next)
    {
        return $"{current:0.##} -> <color=#4AD787>{next:0.##} ({next - current:+0.##;-0.##;0})</color>";
    }

    private static string FormatDodgeCooldownSecChange(float current, float next)
    {
        return $"{current:0.##}초 > <color=#4AD787>{next:0.##}초</color>";
    }

    private static string FormatPlainPercentChange(float current, float next)
    {
        return $"{current * 100f:0.##}% > <color=#4AD787>{next * 100f:0.##}%</color>";
    }

    private static string FormatPercentChange(float current, float next)
    {
        float currentPercent = current * 100f;
        float nextPercent = next * 100f;
        return $"{currentPercent:0.##}% -> <color=#4AD787>{nextPercent:0.##}% "
            + $"({nextPercent - currentPercent:+0.##;-0.##;0}%p)</color>";
    }

    // 탱크 유닛강화 조건을 담아내기 위해 플레이어 레벨 가져오기
    private int GetPlayerLevel()
    {
        if (baseCampManager?.PlayerProgression != null)
        {
            return baseCampManager.PlayerProgression.Level;
        }

        return baseCampManager != null ? baseCampManager.CommanderLevel : 1;
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        coreCharger = baseCampManager != null ? baseCampManager.CoreCharger : FindFirstObjectByType<CoreCharger>();
        inventory = baseCampManager != null ? baseCampManager.Inventory : InventoryFacility.FindAny();
        player ??= FindFirstObjectByType<PlayerController>();
    }

    private static void SetInteractable(Button button, bool value)
    {
        if (button != null)
        {
            button.gameObject.SetActive(true);
            button.interactable = value;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private static void SetUpgradeRemainingText(TMP_Text target, bool isUpgrading, float remainingSeconds)
    {
        SetText(target, isUpgrading ? $"완료까지 {remainingSeconds:0}초" : string.Empty);
        SetActive(target != null ? target.gameObject : null, isUpgrading);
    }

    private void SetEnhanceUnitButtonLabel(string value)
    {
        if (enhanceUnitButton == null)
        {
            return;
        }

        TMP_Text[] labels = enhanceUnitButton.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label != null
                && label != enhanceUnitButtonStateText
                && label != enhanceUnitEmptyText)
            {
                label.text = value;
                return;
            }
        }
    }

    // 탱크 Raw이미지 연동
    private static void SetUnitPreview(RawImage target, PlayerUnitConfig unitConfig)
    {
        if (target == null)
        {
            return;
        }

        GameObject prefab = unitConfig != null ? unitConfig.UnitPrefab : null;
        if (prefab == null)
        {
            target.texture = null;
            target.color = Color.clear;
            target.gameObject.SetActive(false);
            return;
        }

        RenderTexture preview = UnitPreviewRenderer.Instance.GetPreview(prefab);
        target.texture = preview;
        target.color = preview != null ? Color.white : Color.clear;
        target.gameObject.SetActive(preview != null);
    }

    // 드론 Raw이미지 연동
    private static void SetDronePreview(RawImage target, DroneConfig droneConfig)
    {
        if (target == null)
        {
            return;
        }

        GameObject prefab = droneConfig != null ? droneConfig.DronePrefab : null;
        if (prefab == null)
        {
            target.texture = null;
            target.color = Color.clear;
            target.gameObject.SetActive(false);
            return;
        }

        RenderTexture preview = UnitPreviewRenderer.Instance.GetPreview(prefab);
        target.texture = preview;
        target.color = preview != null ? Color.white : Color.clear;
        target.gameObject.SetActive(preview != null);
    }
}

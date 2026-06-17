using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerPanel : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text levelText;
    
    [Header("TankUnit subPanel")]
    [SerializeField] private Button enhanceUnitButton;
    [SerializeField] private TMP_Text enhanceUnitButtonStateText;
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

    private CoreCharger coreCharger;
    private InventoryFacility inventory;
    private PlayerController player;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeCoreCharger);
        enhanceUnitButton?.onClick.AddListener(ConvertCurrentUnit);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeCoreCharger);
        enhanceUnitButton?.onClick.RemoveListener(ConvertCurrentUnit);
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

    private void Refresh()
    {
        ResolveReferences();

        if (coreCharger == null)
        {
            SetText(enhanceUnitButtonStateText, "코어 충전소가 연결되지 않았습니다.");
            SetText(currentUnitText, string.Empty);
            SetText(enhanceUnitText, string.Empty);
            RefreshEnhanceUnitStatTexts(null);
            SetUnitPreview(currentUnitPreviewImage, null);
            SetUnitPreview(enhanceUnitPreviewImage, null);
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
        SetText(upgradeText, coreCharger.IsUpgrading
            ? $"완료까지 {coreCharger.UpgradeRemainingSeconds:0}초"
            : $"기지 업그레이드 ({coreCharger.UpgradeCost} 크레딧)");
        SetText(currentUnitText, stage != null ? FormatUnitName(stage.currentUnit) : "모든 변환 완료");
        SetText(enhanceUnitText, stage != null ? FormatUnitName(stage.nextUnit) : string.Empty);
        SetUnitPreview(currentUnitPreviewImage, stage?.currentUnit);
        SetUnitPreview(enhanceUnitPreviewImage, stage?.nextUnit);
        SetText(unitStatusDetailText, BuildUnitDetailStatusText(stage));
        RefreshEnhanceUnitStatTexts(stage);

        bool canConvert = coreCharger.CanConvertCurrentUnit(inventory, player, playerLevel);
        SetInteractable(enhanceUnitButton, canConvert);
        SetText(enhanceUnitButtonStateText, BuildEnhanceUnitButtonStateText(stage, playerLevel));
        SetEnhanceUnitButtonLabel(stage != null ? "유닛 강화" : "완료");

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
            return "코어 충전소가 연결되지 않았습니다.";
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

        string message = "강화 조건";
        if (playerLevel < stage.requiredPlayerLevel)
        {
            message += $"\n- 플레이어 Lv.{stage.requiredPlayerLevel} 필요 (현재 Lv.{playerLevel})";
        }

        if (coreCharger.Level < requiredCoreLevel)
        {
            message += $"\n- 코어 충전소 Lv.{requiredCoreLevel} 필요 (현재 Lv.{coreCharger.Level})";
        }

        if (!ownsCurrentUnit && !hasCurrentUnitEquipped)
        {
            message += $"\n- {stage.currentUnit.DisplayName} 보유 또는 장착 필요";
        }

        return message;
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
        SetText(enhanceUnitHealthText, FormatPlainStatChange(current.MaxHealth, next.MaxHealth));
        SetText(enhanceUnitDamageText, FormatPlainStatChange(current.AttackDamage, next.AttackDamage));
        SetText(enhanceUnitSpeedText, FormatPlainStatChange(current.MoveSpeed, next.MoveSpeed));
        SetText(enhanceUnitCritChanceText, FormatPlainPercentChange(current.CritChance, next.CritChance));
    }

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
        // 순서 => 체력, 공격력, 공격범위, 공격간격, 이동 속도, 회전속도, 치명타 확률, 치명타 피해
        return $"{FormatStatChange(current.MaxHealth, next.MaxHealth)}\n"
            + $"{FormatStatChange(current.AttackDamage, next.AttackDamage)}\n"
            + $"{FormatStatChange(current.AttackRange, next.AttackRange)}\n"
            + $"{FormatStatChange(current.AttackInterval, next.AttackInterval)}\n"
            + $"{FormatStatChange(current.MoveSpeed, next.MoveSpeed)}\n"
            + $"{FormatStatChange(current.RotationSpeed, next.RotationSpeed)}\n"
            + $"{FormatPercentChange(current.CritChance, next.CritChance)}\n";
    }

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

    private static string FormatPlainStatChange(float current, float next)
    {
        return $"{current:0.##} > <color=#4AD787>{next:0.##}</color>";
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

    private void SetEnhanceUnitButtonLabel(string value)
    {
        if (enhanceUnitButton == null)
        {
            return;
        }

        TMP_Text[] labels = enhanceUnitButton.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label != null && label != enhanceUnitButtonStateText)
            {
                label.text = value;
                return;
            }
        }
    }

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

}

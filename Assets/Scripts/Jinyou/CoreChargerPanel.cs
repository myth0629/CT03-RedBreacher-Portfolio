using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button enhanceUnitButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private Image currentUnitPreviewImage;
    [SerializeField] private TMP_Text selectedUnitText;
    [SerializeField] private TMP_Text unitSoTransitionText;
    [SerializeField] private TMP_Text unitStateText;

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
        TMP_Text selectedUnit,
        TMP_Text unitState)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        levelText = level;
        upgradeText = upgradeLabel;
        selectedUnitText = selectedUnit;
        unitStateText = unitState;
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
            SetText(upgradeConditionText, "코어 충전소가 연결되지 않았습니다.");
            SetUnitPreview(currentUnitPreviewImage, null);
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
        SetText(selectedUnitText, stage != null ? stage.DisplayName : "모든 변환 완료");
        SetUnitPreview(currentUnitPreviewImage, stage?.currentUnit);
        SetText(unitSoTransitionText, BuildUnitSoTransitionText(stage));
        SetText(upgradeConditionText, BuildUpgradeConditionText(
            coreCharger,
            baseCampManager != null ? baseCampManager.Credits : 0,
            baseCampManager != null ? baseCampManager.CommanderLevel : 1,
            researchLabLevel));

        string conversionState = BuildConversionStateText(stage, playerLevel);
        SetText(unitStateText, conversionState);

        bool canConvert = coreCharger.CanConvertCurrentUnit(inventory, player, playerLevel);
        SetInteractable(enhanceUnitButton, canConvert);
        SetText(enhanceUnitButton != null
            ? enhanceUnitButton.GetComponentInChildren<TMP_Text>(true)
            : null, stage != null ? "유닛 강화" : "완료");

        BaseCampUpgradeStatus.SetUpgradeProgress(
            upgradeProgressFill,
            coreCharger,
            ref observedUpgradeDuration);
        SetInteractable(upgradeButton, coreCharger.CanStartUpgrade(
            baseCampManager != null ? baseCampManager.Credits : 0,
            baseCampManager != null ? baseCampManager.CommanderLevel : 1,
            researchLabLevel));
    }

    private string BuildConversionStateText(CoreCharger.UnitConversionStage stage, int playerLevel)
    {
        if (stage == null)
        {
            return coreCharger.ConversionStages.Count == 0
                ? "유닛 SO 변환 단계가 설정되지 않았습니다."
                : "모든 유닛 SO 변환이 완료되었습니다.";
        }

        if (!stage.IsConfigured)
        {
            return "현재 및 다음 유닛 SO를 지정하세요.";
        }

        int requiredCoreLevel = coreCharger.GetRequiredCoreChargerLevel(coreCharger.CurrentStageIndex);
        string stageText = $"단계 {coreCharger.CurrentStageIndex + 1}/{coreCharger.ConversionStages.Count}"
            + $" | 플레이어 레벨 {stage.requiredPlayerLevel}"
            + $" | 코어 충전소 레벨 {requiredCoreLevel}";

        if (playerLevel < stage.requiredPlayerLevel)
        {
            return $"{stageText}\n플레이어 레벨 {stage.requiredPlayerLevel} 필요";
        }

        if (coreCharger.Level < requiredCoreLevel)
        {
            return $"{stageText}\n코어 충전소 레벨 {requiredCoreLevel} 필요";
        }

        bool ownsCurrent = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool equippedCurrent = player != null && player.UnitConfig == stage.currentUnit;
        if (!ownsCurrent && !equippedCurrent)
        {
            return $"{stageText}\n{stage.currentUnit.DisplayName} 필요";
        }

        return $"{stageText}\n변환 준비 완료";
    }

    private static string BuildUnitSoTransitionText(CoreCharger.UnitConversionStage stage)
    {
        if (stage == null)
        {
            return "대기 중인 유닛 SO 변환 없음";
        }

        if (stage.currentUnit == null || stage.nextUnit == null)
        {
            return $"변환 전 SO: {FormatUnitSo(stage.currentUnit)}\n"
                + $"변환 후 SO: {FormatUnitSo(stage.nextUnit)}";
        }

        PlayerUnitConfig current = stage.currentUnit;
        PlayerUnitConfig next = stage.nextUnit;
        return $"체력: {FormatStatChange(current.MaxHealth, next.MaxHealth)}\n"
            + $"공격력: {FormatStatChange(current.AttackDamage, next.AttackDamage)}\n"
            + $"공격 범위: {FormatStatChange(current.AttackRange, next.AttackRange)}\n"
            + $"공격 간격: {FormatStatChange(current.AttackInterval, next.AttackInterval)}\n"
            + $"이동 속도: {FormatStatChange(current.MoveSpeed, next.MoveSpeed)}\n"
            + $"회전 속도: {FormatStatChange(current.RotationSpeed, next.RotationSpeed)}\n"
            + $"치명타 확률: {FormatPercentChange(current.CritChance, next.CritChance)}\n"
            + $"치명타 피해: {FormatMultiplierChange(current.CritMultiplier, next.CritMultiplier)}";
    }

    private static string FormatUnitSo(PlayerUnitConfig unitConfig)
    {
        return unitConfig != null
            ? unitConfig.DisplayName
            : "미지정";
    }

    private static string BuildUpgradeConditionText(
        IBaseCampFacility facility,
        int credits,
        int commanderLevel,
        int researchLabLevel)
    {
        if (facility == null)
        {
            return "시설이 연결되지 않았습니다.";
        }

        if (facility.IsUpgrading)
        {
            return $"업그레이드 중... {facility.UpgradeRemainingSeconds:0}초 남음";
        }

        if (researchLabLevel < facility.RequiredResearchLabLevel)
        {
            return $"연구소 레벨 {facility.RequiredResearchLabLevel} 필요";
        }

        int levelLimit = facility.GetLevelLimit(researchLabLevel);
        if (facility.Level >= levelLimit && facility.Level < facility.MaxLevel)
        {
            return $"현재 최대 레벨: {levelLimit}. 연구소를 업그레이드하세요.";
        }

        if (facility.Level >= facility.MaxLevel)
        {
            return "최대 레벨에 도달했습니다.";
        }

        if (credits < facility.UpgradeCost)
        {
            return $"{facility.UpgradeCost - credits} 크레딧 부족";
        }

        if (commanderLevel < facility.RequiredCommanderLevel)
        {
            return $"지휘관 Lv.{facility.RequiredCommanderLevel} 필요";
        }

        if (facility.CanStartUpgrade(credits, commanderLevel, researchLabLevel))
        {
            return "업그레이드 가능";
        }

        return "최대 레벨에 도달했습니다.";
    }

    private static string FormatStatChange(float current, float next)
    {
        return $"{current:0.##} -> <color=#4AD787>{next:0.##} ({next - current:+0.##;-0.##;0})</color>";
    }

    private static string FormatPercentChange(float current, float next)
    {
        float currentPercent = current * 100f;
        float nextPercent = next * 100f;
        return $"{currentPercent:0.##}% -> <color=#4AD787>{nextPercent:0.##}% "
            + $"({nextPercent - currentPercent:+0.##;-0.##;0}%p)</color>";
    }

    private static string FormatMultiplierChange(float current, float next)
    {
        return $"x{current:0.##} -> <color=#4AD787>x{next:0.##} ({next - current:+0.##;-0.##;0})</color>";
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

    private static void SetUnitPreview(Image target, PlayerUnitConfig unitConfig)
    {
        if (target == null)
        {
            return;
        }

        Sprite sprite = null;
        if (unitConfig != null && unitConfig.UnitPrefab != null)
        {
            SpriteRenderer spriteRenderer =
                unitConfig.UnitPrefab.GetComponentInChildren<SpriteRenderer>(true);
            sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        }

        target.sprite = sprite;
        target.preserveAspect = true;
        target.enabled = sprite != null;
    }

}

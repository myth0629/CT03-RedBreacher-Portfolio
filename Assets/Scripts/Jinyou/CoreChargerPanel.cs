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
            SetText(upgradeConditionText, "Core Charger is not connected.");
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

        SetText(levelText, $"Core Charger Lv. {coreCharger.Level}");
        SetText(upgradeText, coreCharger.IsUpgrading
            ? $"Upgrading... {coreCharger.UpgradeRemainingSeconds:0}s"
            : $"Upgrade ({coreCharger.UpgradeCost} Credits)");
        SetText(selectedUnitText, stage != null ? stage.DisplayName : "All conversions complete");
        SetUnitPreview(currentUnitPreviewImage, stage?.currentUnit);
        SetText(unitSoTransitionText, BuildUnitSoTransitionText(stage));
        SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
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
            : null, stage != null ? "Enhance Unit" : "Complete");

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
                ? "No unit SO conversion stages configured."
                : "All unit SO conversions are complete.";
        }

        if (!stage.IsConfigured)
        {
            return "Assign the current and next Unit SO.";
        }

        int requiredCoreLevel = coreCharger.GetRequiredCoreChargerLevel(coreCharger.CurrentStageIndex);
        string stageText = $"Stage {coreCharger.CurrentStageIndex + 1}/{coreCharger.ConversionStages.Count}"
            + $" | Player Lv.{stage.requiredPlayerLevel}"
            + $" | Core Charger Lv.{requiredCoreLevel}";

        if (playerLevel < stage.requiredPlayerLevel)
        {
            return $"{stageText}\nRequires Player Lv. {stage.requiredPlayerLevel}";
        }

        if (coreCharger.Level < requiredCoreLevel)
        {
            return $"{stageText}\nUpgrade Core Charger to Lv. {requiredCoreLevel}";
        }

        bool ownsCurrent = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool equippedCurrent = player != null && player.UnitConfig == stage.currentUnit;
        if (!ownsCurrent && !equippedCurrent)
        {
            return $"{stageText}\nRequires {stage.currentUnit.DisplayName}";
        }

        return $"{stageText}\nReady to convert";
    }

    private static string BuildUnitSoTransitionText(CoreCharger.UnitConversionStage stage)
    {
        if (stage == null)
        {
            return "No pending Unit SO conversion";
        }

        if (stage.currentUnit == null || stage.nextUnit == null)
        {
            return $"Before SO: {FormatUnitSo(stage.currentUnit)}\n"
                + $"After SO: {FormatUnitSo(stage.nextUnit)}";
        }

        PlayerUnitConfig current = stage.currentUnit;
        PlayerUnitConfig next = stage.nextUnit;
        return $"{FormatUnitSo(current)} -> {FormatUnitSo(next)}\n"
            + $"HP: {FormatStatChange(current.MaxHealth, next.MaxHealth)}\n"
            + $"Attack: {FormatStatChange(current.AttackDamage, next.AttackDamage)}\n"
            + $"Attack Range: {FormatStatChange(current.AttackRange, next.AttackRange)}\n"
            + $"Attack Interval: {FormatStatChange(current.AttackInterval, next.AttackInterval)}\n"
            + $"Move Speed: {FormatStatChange(current.MoveSpeed, next.MoveSpeed)}\n"
            + $"Rotation Speed: {FormatStatChange(current.RotationSpeed, next.RotationSpeed)}\n"
            + $"Crit Chance: {FormatPercentChange(current.CritChance, next.CritChance)}\n"
            + $"Crit Damage: {FormatMultiplierChange(current.CritMultiplier, next.CritMultiplier)}";
    }

    private static string FormatUnitSo(PlayerUnitConfig unitConfig)
    {
        return unitConfig != null
            ? $"{unitConfig.name} ({unitConfig.DisplayName})"
            : "Unassigned";
    }

    private static string FormatStatChange(float current, float next)
    {
        return $"{current:0.##} -> {next:0.##} ({next - current:+0.##;-0.##;0})";
    }

    private static string FormatPercentChange(float current, float next)
    {
        float currentPercent = current * 100f;
        float nextPercent = next * 100f;
        return $"{currentPercent:0.##}% -> {nextPercent:0.##}% "
            + $"({nextPercent - currentPercent:+0.##;-0.##;0}%p)";
    }

    private static string FormatMultiplierChange(float current, float next)
    {
        return $"x{current:0.##} -> x{next:0.##} ({next - current:+0.##;-0.##;0})";
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

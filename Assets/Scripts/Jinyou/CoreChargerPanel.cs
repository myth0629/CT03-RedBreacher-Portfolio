using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button firstUnitButton;
    [SerializeField] private Button secondUnitButton;
    [SerializeField] private Button thirdUnitButton;
    [SerializeField] private Button enhanceUnitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text selectedUnitText;
    [SerializeField] private TMP_Text unitStateText;

    private CoreCharger coreCharger;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeCharger);
        firstUnitButton?.onClick.AddListener(SelectFirstUnit);
        secondUnitButton?.onClick.AddListener(SelectSecondUnit);
        thirdUnitButton?.onClick.AddListener(SelectThirdUnit);
        enhanceUnitButton?.onClick.AddListener(EnhanceSelectedUnit);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeCharger);
        closeButton?.onClick.RemoveListener(ClosePanel);
        firstUnitButton?.onClick.RemoveListener(SelectFirstUnit);
        secondUnitButton?.onClick.RemoveListener(SelectSecondUnit);
        thirdUnitButton?.onClick.RemoveListener(SelectThirdUnit);
        enhanceUnitButton?.onClick.RemoveListener(EnhanceSelectedUnit);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button upgrade,
        Button firstUnit,
        Button secondUnit,
        Button thirdUnit,
        Button close,
        TMP_Text level,
        TMP_Text upgradeLabel,
        TMP_Text selectedUnit,
        TMP_Text unitState)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        firstUnitButton = firstUnit;
        secondUnitButton = secondUnit;
        thirdUnitButton = thirdUnit;
        closeButton = close;
        levelText = level;
        upgradeText = upgradeLabel;
        selectedUnitText = selectedUnit;
        unitStateText = unitState;
        Refresh();
    }

    public void SelectUnit(PlayerUnitConfig unitConfig)
    {
        baseCampManager?.SelectCoreUnit(unitConfig);
        Refresh();
    }

    public void SelectUnitByIndex(int unitIndex)
    {
        baseCampManager?.SelectCoreUnit(unitIndex);
        Refresh();
    }

    private void UpgradeCharger()
    {
        baseCampManager?.UpgradeCoreCharger();
        Refresh();
    }

    private void SelectFirstUnit()
    {
        SelectUnitByIndex(0);
    }

    private void SelectSecondUnit()
    {
        SelectUnitByIndex(1);
    }

    private void SelectThirdUnit()
    {
        SelectUnitByIndex(2);
    }

    private void EnhanceSelectedUnit()
    {
        baseCampManager?.EnhanceCoreUnit();
        Refresh();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        ResolveReferences();

        if (coreCharger == null)
        {
            return;
        }

        SetText(levelText, $"Lv. {coreCharger.Level}");
        SetText(upgradeText, coreCharger.IsUpgrading
            ? $"Upgrading {coreCharger.UpgradeRemainingSeconds:0}s"
            : $"Upgrade Cost {coreCharger.UpgradeCost}");
        SetText(currencyText, baseCampManager != null ? $"Credits {baseCampManager.Credits}" : "Credits --");
        SetText(selectedUnitText, BuildSelectedUnitText());
        SetText(unitStateText, BuildUnitSummary());

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.ResearchLab != null ? baseCampManager.ResearchLab.Level : 1;
            upgradeButton.interactable = coreCharger.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
            SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
                coreCharger,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, coreCharger, ref observedUpgradeDuration);

        if (enhanceUnitButton != null && baseCampManager != null)
        {
            enhanceUnitButton.interactable = coreCharger.CanEnhanceSelectedUnit(baseCampManager.Credits);
        }

        SetUnitButton(firstUnitButton, 0);
        SetUnitButton(secondUnitButton, 1);
        SetUnitButton(thirdUnitButton, 2);
    }

    private string BuildSelectedUnitText()
    {
        CoreCharger.UnitEnhancement selectedUnit = coreCharger.SelectedUnitEnhancement;
        if (selectedUnit == null)
        {
            return "No Unit Selected";
        }

        if (selectedUnit.IsMaxLevel)
        {
            return $"{selectedUnit.DisplayName} Lv.MAX {BuildStatBonusSummary(selectedUnit)}";
        }

        return $"{selectedUnit.DisplayName} Lv.{selectedUnit.enhanceLevel}/{selectedUnit.MaxEnhanceLevel} {BuildStatBonusSummary(selectedUnit)} / Cost {selectedUnit.NextEnhanceCost} / Next {BuildNextStatIncreaseSummary(selectedUnit)}";
    }

    private string BuildUnitSummary()
    {
        string summary = string.Empty;
        foreach (CoreCharger.UnitEnhancement unitEnhancement in coreCharger.UnitEnhancements)
        {
            if (unitEnhancement == null)
            {
                continue;
            }

            string selected = unitEnhancement == coreCharger.SelectedUnitEnhancement ? " *" : string.Empty;
            summary += $"{unitEnhancement.DisplayName}{selected}: {BuildStatBonusSummary(unitEnhancement)} (Lv.{unitEnhancement.enhanceLevel}/{unitEnhancement.MaxEnhanceLevel})\n";
        }

        return summary.TrimEnd();
    }

    private static string BuildStatBonusSummary(CoreCharger.UnitEnhancement unitEnhancement)
    {
        if (unitEnhancement == null)
        {
            return string.Empty;
        }

        string summary = string.Empty;
        foreach (CoreCharger.UnitEnhancementStat stat in System.Enum.GetValues(typeof(CoreCharger.UnitEnhancementStat)))
        {
            float bonus = unitEnhancement.GetStatBonus(stat);
            if (Mathf.Approximately(bonus, 0f))
            {
                continue;
            }

            summary += $"{CoreCharger.GetStatDisplayName(stat)} {FormatSigned(bonus)} ";
        }

        return string.IsNullOrWhiteSpace(summary) ? "No Bonus" : summary.TrimEnd();
    }

    private static string BuildNextStatIncreaseSummary(CoreCharger.UnitEnhancement unitEnhancement)
    {
        CoreCharger.UnitEnhancementLevel nextLevel = unitEnhancement?.GetEnhancementLevel(unitEnhancement.enhanceLevel);
        if (nextLevel == null || nextLevel.statIncreases == null || nextLevel.statIncreases.Count == 0)
        {
            return "No Bonus";
        }

        string summary = string.Empty;
        foreach (CoreCharger.UnitStatIncrease statIncrease in nextLevel.statIncreases)
        {
            if (statIncrease == null || Mathf.Approximately(statIncrease.amount, 0f))
            {
                continue;
            }

            summary += $"{CoreCharger.GetStatDisplayName(statIncrease.stat)} {FormatSigned(statIncrease.amount)} ";
        }

        return string.IsNullOrWhiteSpace(summary) ? "No Bonus" : summary.TrimEnd();
    }

    private void SetUnitButton(Button button, int unitIndex)
    {
        if (button != null)
        {
            button.interactable = coreCharger.UnitEnhancements != null && unitIndex >= 0 && unitIndex < coreCharger.UnitEnhancements.Count;
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        coreCharger = baseCampManager != null ? baseCampManager.CoreCharger : null;
    }

    private static string FormatSigned(float value)
    {
        return value >= 0f ? $"+{value:0.##}" : $"{value:0.##}";
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

using System.Collections.Generic;
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
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private GameObject unitInventoryArea;
    [SerializeField] private RectTransform unitInventoryContentRoot;
    [SerializeField] private Button inventoryUnitButtonPrefab;
    [SerializeField] private TMP_Text inventoryUnitListText;

    private CoreCharger coreCharger;
    private InventoryFacility inventory;
    private readonly List<Button> spawnedUnitButtons = new List<Button>();
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventoryEvents();
        upgradeButton?.onClick.AddListener(UpgradeCharger);
        firstUnitButton?.onClick.AddListener(OpenInventoryUnitSelection);
        secondUnitButton?.onClick.AddListener(SelectSecondUnit);
        thirdUnitButton?.onClick.AddListener(SelectThirdUnit);
        enhanceUnitButton?.onClick.AddListener(EnhanceSelectedUnit);
        closeButton?.onClick.AddListener(ClosePanel);
        SetActive(unitInventoryArea, false);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvents();
        upgradeButton?.onClick.RemoveListener(UpgradeCharger);
        closeButton?.onClick.RemoveListener(ClosePanel);
        firstUnitButton?.onClick.RemoveListener(OpenInventoryUnitSelection);
        secondUnitButton?.onClick.RemoveListener(SelectSecondUnit);
        thirdUnitButton?.onClick.RemoveListener(SelectThirdUnit);
        enhanceUnitButton?.onClick.RemoveListener(EnhanceSelectedUnit);
        ClearUnitButtons();
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
        RebuildInventoryUnitButtons();
        Refresh();
    }

    public void SelectUnitByIndex(int unitIndex)
    {
        baseCampManager?.SelectCoreUnit(unitIndex);
        RebuildInventoryUnitButtons();
        Refresh();
    }

    private void UpgradeCharger()
    {
        baseCampManager?.UpgradeCoreCharger();
        Refresh();
    }

    private void SelectFirstUnit()
    {
        OpenInventoryUnitSelection();
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
        SetText(inventoryUnitListText, BuildInventoryUnitListText());
        SetActive(unitInventoryArea, inventoryPanel == null);

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.CommandCenter != null ? baseCampManager.CommandCenter.Level : 1;
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

    private string BuildInventoryUnitListText()
    {
        if (inventory == null)
        {
            return "Inventory not connected";
        }

        if (inventory.UnitConfigs.Count == 0)
        {
            return "No Inventory Units";
        }

        string summary = string.Empty;
        for (int i = 0; i < inventory.UnitConfigs.Count; i++)
        {
            PlayerUnitConfig unit = inventory.UnitConfigs[i];
            if (unit == null)
            {
                continue;
            }

            string selected = unit == coreCharger.SelectedUnitConfig ? " *" : string.Empty;
            string configured = coreCharger.HasUnitEnhancement(unit) ? string.Empty : " (No Enhance Data)";
            summary += $"{i + 1}. {unit.DisplayName}{selected}{configured}\n";
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

    private void RebuildInventoryUnitButtons()
    {
        ClearUnitButtons();

        if (unitInventoryContentRoot == null || inventoryUnitButtonPrefab == null || inventory == null)
        {
            return;
        }

        foreach (PlayerUnitConfig unit in inventory.UnitConfigs)
        {
            if (unit == null)
            {
                continue;
            }

            Button button = Instantiate(inventoryUnitButtonPrefab, unitInventoryContentRoot);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                string selected = unit == coreCharger.SelectedUnitConfig ? " *" : string.Empty;
                string configured = coreCharger.HasUnitEnhancement(unit) ? string.Empty : " (No Data)";
                label.text = $"{unit.DisplayName}{selected}{configured}";
            }

            PlayerUnitConfig capturedUnit = unit;
            button.interactable = coreCharger.HasUnitEnhancement(capturedUnit);
            button.onClick.AddListener(() => SelectUnit(capturedUnit));
            spawnedUnitButtons.Add(button);
        }
    }

    private void ClearUnitButtons()
    {
        foreach (Button button in spawnedUnitButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawnedUnitButtons.Clear();
    }

    private void SubscribeInventoryEvents()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged.AddListener(HandleInventoryChanged);
        }
    }

    private void UnsubscribeInventoryEvents()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged.RemoveListener(HandleInventoryChanged);
        }
    }

    private void HandleInventoryChanged()
    {
        RebuildInventoryUnitButtons();
        Refresh();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        coreCharger = baseCampManager != null ? baseCampManager.CoreCharger : null;
        inventory = baseCampManager != null ? baseCampManager.Inventory : FindFirstObjectByType<InventoryFacility>();
        inventoryPanel ??= FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
    }

    private void OpenInventoryUnitSelection()
    {
        ResolveReferences();

        if (inventoryPanel == null || coreCharger == null)
        {
            SetActive(unitInventoryArea, true);
            RebuildInventoryUnitButtons();
            return;
        }

        inventoryPanel.OpenUnitSelectMode(
            SelectUnit,
            unit => coreCharger.HasUnitEnhancement(unit));
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

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null)
        {
            target.SetActive(value);
        }
    }
}

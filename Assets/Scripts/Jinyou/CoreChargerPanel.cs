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
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text selectedUnitText;
    [SerializeField] private TMP_Text unitStateText;
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private GameObject unitInventoryArea;
    [SerializeField] private RectTransform unitInventoryContentRoot;
    [SerializeField] private Button inventoryUnitButtonPrefab;
    [SerializeField] private TMP_Text inventoryUnitListText;

    private CoreCharger coreCharger;
    private InventoryFacility inventory;
    private PlayerController player;

    private void OnEnable()
    {
        ResolveReferences();
        firstUnitButton?.onClick.AddListener(SelectFirstStage);
        secondUnitButton?.onClick.AddListener(SelectSecondStage);
        thirdUnitButton?.onClick.AddListener(SelectThirdStage);
        enhanceUnitButton?.onClick.AddListener(ConvertSelectedUnit);
        SetActive(unitInventoryArea, false);
        Refresh();
    }

    private void OnDisable()
    {
        firstUnitButton?.onClick.RemoveListener(SelectFirstStage);
        secondUnitButton?.onClick.RemoveListener(SelectSecondStage);
        thirdUnitButton?.onClick.RemoveListener(SelectThirdStage);
        enhanceUnitButton?.onClick.RemoveListener(ConvertSelectedUnit);
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
        levelText = level;
        upgradeText = upgradeLabel;
        selectedUnitText = selectedUnit;
        unitStateText = unitState;
        Refresh();
    }

    public void SelectUnit(PlayerUnitConfig unitConfig)
    {
        if (coreCharger == null || unitConfig == null)
        {
            return;
        }

        for (int i = 0; i < coreCharger.ConversionStages.Count; i++)
        {
            CoreCharger.UnitConversionStage stage = coreCharger.ConversionStages[i];
            if (stage != null && stage.currentUnit == unitConfig)
            {
                SelectUnitByIndex(i);
                return;
            }
        }
    }

    public void SelectUnitByIndex(int unitIndex)
    {
        coreCharger?.TrySelectConversionStage(unitIndex);
        Refresh();
    }

    private void SelectFirstStage()
    {
        SelectUnitByIndex(0);
    }

    private void SelectSecondStage()
    {
        SelectUnitByIndex(1);
    }

    private void SelectThirdStage()
    {
        SelectUnitByIndex(2);
    }

    private void ConvertSelectedUnit()
    {
        baseCampManager?.ConvertSelectedCoreUnit();
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        if (coreCharger == null)
        {
            return;
        }

        int playerLevel = GetPlayerLevel();
        CoreCharger.UnitConversionStage stage = coreCharger.SelectedConversionStage;

        SetText(levelText, $"Player Lv. {playerLevel}");
        SetText(upgradeText, "Unit Conversion");
        SetText(upgradeConditionText, BuildConditionText(stage, playerLevel));
        SetText(selectedUnitText, stage != null ? stage.DisplayName : "No conversion configured");
        SetText(unitStateText, BuildStageSummary(playerLevel));
        SetText(inventoryUnitListText, BuildStageSummary(playerLevel));

        SetActive(upgradeProgressFill != null ? upgradeProgressFill.gameObject : null, false);
        SetActive(unitInventoryArea, false);

        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
            upgradeButton.gameObject.SetActive(false);
        }

        if (enhanceUnitButton != null)
        {
            enhanceUnitButton.interactable = coreCharger.CanConvertSelectedUnit(inventory, player, playerLevel);
            TMP_Text buttonText = enhanceUnitButton.GetComponentInChildren<TMP_Text>(true);
            SetText(buttonText, "Convert Unit");
        }

        SetStageButton(firstUnitButton, 0);
        SetStageButton(secondUnitButton, 1);
        SetStageButton(thirdUnitButton, 2);
    }

    private string BuildConditionText(CoreCharger.UnitConversionStage stage, int playerLevel)
    {
        if (stage == null || !stage.IsConfigured)
        {
            return "Configure a conversion stage on CoreCharger.";
        }

        if (coreCharger.IsConversionCompleted(coreCharger.SelectedUnitIndex))
        {
            return "Conversion completed";
        }

        if (playerLevel < stage.requiredPlayerLevel)
        {
            return $"Requires Player Lv. {stage.requiredPlayerLevel}";
        }

        bool ownsCurrent = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool equippedCurrent = player != null && player.UnitConfig == stage.currentUnit;
        if (!ownsCurrent && !equippedCurrent)
        {
            return $"Requires {stage.currentUnit.DisplayName}";
        }

        return "Ready to convert";
    }

    private string BuildStageSummary(int playerLevel)
    {
        if (coreCharger.ConversionStages.Count == 0)
        {
            return "No conversion stages";
        }

        string summary = string.Empty;
        for (int i = 0; i < coreCharger.ConversionStages.Count; i++)
        {
            CoreCharger.UnitConversionStage stage = coreCharger.ConversionStages[i];
            if (stage == null)
            {
                continue;
            }

            string state = coreCharger.IsConversionCompleted(i)
                ? "Completed"
                : playerLevel >= stage.requiredPlayerLevel ? "Unlocked" : "Locked";
            string selected = i == coreCharger.SelectedUnitIndex ? " *" : string.Empty;
            summary += $"{stage.DisplayName} / Lv.{stage.requiredPlayerLevel} / {state}{selected}\n";
        }

        return summary.TrimEnd();
    }

    private void SetStageButton(Button button, int stageIndex)
    {
        if (button == null)
        {
            return;
        }

        bool exists = stageIndex >= 0 && stageIndex < coreCharger.ConversionStages.Count;
        button.interactable = exists;
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (exists)
        {
            CoreCharger.UnitConversionStage stage = coreCharger.ConversionStages[stageIndex];
            SetText(buttonText, stage != null ? stage.DisplayName : $"Stage {stageIndex + 1}");
        }
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

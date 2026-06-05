using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerOptionButton : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private CoreChargerPanel coreChargerPanel;
    [SerializeField] private int unitIndex;
    [SerializeField] private Button optionButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image lockedOverlay;
    [SerializeField] private Image selectedFrame;

    private CoreCharger coreCharger;

    public int UnitIndex => unitIndex;

    private void OnEnable()
    {
        ResolveReferences();
        optionButton?.onClick.AddListener(SelectUnit);
        Refresh();
    }

    private void OnDisable()
    {
        optionButton?.onClick.RemoveListener(SelectUnit);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(BaseCampManager manager, CoreChargerPanel panel, int targetUnitIndex)
    {
        baseCampManager = manager;
        coreChargerPanel = panel;
        unitIndex = targetUnitIndex;
        Refresh();
    }

    public void SelectUnit()
    {
        ResolveReferences();
        coreChargerPanel?.SelectUnitByIndex(unitIndex);
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        CoreCharger.UnitEnhancement unitEnhancement = GetUnitEnhancement();
        if (unitEnhancement == null)
        {
            SetText(titleText, $"Unit {unitIndex + 1}");
            SetText(stateText, "Not connected");
            SetInteractable(false);
            SetActive(lockedOverlay, true);
            SetActive(selectedFrame, false);
            return;
        }

        bool selected = unitIndex == coreCharger.SelectedUnitIndex;
        SetText(titleText, unitEnhancement.DisplayName);
        SetText(stateText, unitEnhancement.IsMaxLevel
            ? $"Lv.MAX / Cost --"
            : $"Lv.{unitEnhancement.enhanceLevel}/{unitEnhancement.MaxEnhanceLevel} / Cost {unitEnhancement.NextEnhanceCost}");
        SetInteractable(true);
        SetActive(lockedOverlay, false);
        SetActive(selectedFrame, selected);
    }

    private CoreCharger.UnitEnhancement GetUnitEnhancement()
    {
        if (coreCharger == null || unitIndex < 0 || unitIndex >= coreCharger.UnitEnhancements.Count)
        {
            return null;
        }

        return coreCharger.UnitEnhancements[unitIndex];
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        coreChargerPanel ??= FindFirstObjectByType<CoreChargerPanel>();
        coreCharger = baseCampManager != null ? baseCampManager.CoreCharger : null;
        optionButton ??= GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
    }

    private void SetInteractable(bool value)
    {
        if (optionButton != null)
        {
            optionButton.interactable = value;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetActive(Component target, bool value)
    {
        if (target != null)
        {
            target.gameObject.SetActive(value);
        }
    }
}

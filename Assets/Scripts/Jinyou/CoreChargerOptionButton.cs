using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerOptionButton : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private CoreChargerPanel coreChargerPanel;
    [SerializeField] private string optionId;
    [SerializeField] private Button optionButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image lockedOverlay;
    [SerializeField] private Image selectedFrame;

    private CoreCharger coreCharger;
    public string OptionId => optionId;

    private void OnEnable()
    {
        ResolveReferences();
        optionButton?.onClick.AddListener(SelectOption);
        Refresh();
    }

    private void OnDisable()
    {
        optionButton?.onClick.RemoveListener(SelectOption);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        CoreChargerPanel panel,
        string targetOptionId,
        Button button,
        TMP_Text title,
        TMP_Text state)
    {
        baseCampManager = manager;
        coreChargerPanel = panel;
        optionId = targetOptionId;
        optionButton = button;
        titleText = title;
        stateText = state;
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        CoreChargerPanel panel,
        string targetOptionId)
    {
        baseCampManager = manager;
        coreChargerPanel = panel;
        optionId = targetOptionId;
        Refresh();
    }

    public void SelectOption()
    {
        ResolveReferences();
        coreChargerPanel?.SelectOption(optionId);
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (coreCharger == null || !coreCharger.TryGetOption(optionId, out CoreCharger.CoreRoute route, out CoreCharger.CoreRouteOption option))
        {
            SetText(titleText, string.IsNullOrEmpty(optionId) ? "Option" : optionId);
            SetText(stateText, "Not connected");
            SetInteractable(false);
            SetActive(lockedOverlay, true);
            SetActive(selectedFrame, false);
            return;
        }

        bool unlocked = coreCharger.IsOptionUnlocked(route, option);
        bool selected = option.optionId == coreCharger.SelectedOptionId;
        int maxPoints = coreCharger.GetOptionMaxPoints(option);
        float bonus = coreCharger.GetOptionBonus(option);
        float bonusPerPoint = coreCharger.GetCurrentOptionTierBonusPerPoint(option);

        SetText(titleText, option.displayName);
        SetText(stateText, unlocked
            ? $"{option.investedPoints}/{maxPoints} {option.statId} +{bonus:0.##} (+{bonusPerPoint:0.##}/pt)"
            : $"Locked: route {option.requiredRoutePoints}");
        SetInteractable(unlocked);
        SetActive(lockedOverlay, !unlocked);
        SetActive(selectedFrame, selected);
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

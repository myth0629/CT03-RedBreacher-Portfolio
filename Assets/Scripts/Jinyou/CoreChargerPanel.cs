using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button armorRouteButton;
    [SerializeField] private Button shieldRouteButton;
    [SerializeField] private Button pierceDefenseRouteButton;
    [SerializeField] private Button survivalRouteButton;
    [SerializeField] private Button investRouteButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text traitPointText;
    [SerializeField] private TMP_Text selectedRouteText;
    [SerializeField] private TMP_Text routeStateText;

    private CoreCharger coreCharger;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeCharger);
        armorRouteButton?.onClick.AddListener(SelectArmorRoute);
        shieldRouteButton?.onClick.AddListener(SelectShieldRoute);
        pierceDefenseRouteButton?.onClick.AddListener(SelectPierceDefenseRoute);
        survivalRouteButton?.onClick.AddListener(SelectSurvivalRoute);
        investRouteButton?.onClick.AddListener(InvestSelectedRoute);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeCharger);
        closeButton?.onClick.RemoveListener(ClosePanel);
        armorRouteButton?.onClick.RemoveListener(SelectArmorRoute);
        shieldRouteButton?.onClick.RemoveListener(SelectShieldRoute);
        pierceDefenseRouteButton?.onClick.RemoveListener(SelectPierceDefenseRoute);
        survivalRouteButton?.onClick.RemoveListener(SelectSurvivalRoute);
        investRouteButton?.onClick.RemoveListener(InvestSelectedRoute);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button upgrade,
        Button armor,
        Button shield,
        Button pierceDefense,
        Button survival,
        Button close,
        TMP_Text level,
        TMP_Text upgradeLabel,
        TMP_Text selectedRoute,
        TMP_Text routeState)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        armorRouteButton = armor;
        shieldRouteButton = shield;
        pierceDefenseRouteButton = pierceDefense;
        survivalRouteButton = survival;
        closeButton = close;
        levelText = level;
        upgradeText = upgradeLabel;
        selectedRouteText = selectedRoute;
        routeStateText = routeState;
        Refresh();
    }

    private void UpgradeCharger()
    {
        baseCampManager?.UpgradeCoreCharger();
        Refresh();
    }

    private void SelectArmorRoute()
    {
        SelectRoute("health");
    }

    private void SelectShieldRoute()
    {
        SelectRoute("attack");
    }

    private void SelectPierceDefenseRoute()
    {
        SelectRoute("mobility");
    }

    private void SelectSurvivalRoute()
    {
        SelectRoute("critical");
    }

    private void SelectRoute(string routeId)
    {
        baseCampManager?.SelectCoreRoute(routeId);
        Refresh();
    }

    private void InvestSelectedRoute()
    {
        if (coreCharger == null)
        {
            return;
        }

        baseCampManager?.InvestCoreRoute(coreCharger.SelectedRouteId);
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
        SetText(traitPointText, baseCampManager != null && baseCampManager.PlayerProgression != null
            ? $"Trait Points {baseCampManager.PlayerProgression.TraitPoints}"
            : "Trait Points --");
        SetText(selectedRouteText, string.IsNullOrEmpty(coreCharger.SelectedRouteId) ? "No Route Selected" : $"Selected: {coreCharger.SelectedRouteId}");
        SetText(routeStateText, BuildRouteSummary());

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

        if (investRouteButton != null)
        {
            investRouteButton.interactable = coreCharger.CanInvestRoute(coreCharger.SelectedRouteId);
        }

        SetRouteButton(armorRouteButton, "health");
        SetRouteButton(shieldRouteButton, "attack");
        SetRouteButton(pierceDefenseRouteButton, "mobility");
        SetRouteButton(survivalRouteButton, "critical");
    }

    private string BuildRouteSummary()
    {
        string summary = string.Empty;

        foreach (CoreCharger.CoreRoute route in coreCharger.Routes)
        {
            string state = route.unlocked ? "OPEN" : $"Charger Lv.{route.requiredChargerLevel}";
            float bonus = route.investedPoints * route.bonusPerPoint;
            summary += $"{route.displayName}: {state} / {route.investedPoints}/{route.maxPoints} {route.statType} +{bonus:0.##}\n";
        }

        return summary.TrimEnd();
    }

    private void SetRouteButton(Button button, string routeId)
    {
        if (button != null)
        {
            button.interactable = coreCharger.IsRouteUnlocked(routeId);
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        coreCharger = baseCampManager != null ? baseCampManager.CoreCharger : null;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

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
    [SerializeField] private Button investRouteButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text traitPointText;
    [SerializeField] private TMP_Text selectedRouteText;
    [SerializeField] private TMP_Text routeStateText;
    [SerializeField] private CoreChargerTreeBuilder treeBuilder;

    private CoreCharger coreCharger;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeCharger);
        armorRouteButton?.onClick.AddListener(SelectArmorRoute);
        shieldRouteButton?.onClick.AddListener(SelectShieldRoute);
        pierceDefenseRouteButton?.onClick.AddListener(SelectPierceDefenseRoute);
        investRouteButton?.onClick.AddListener(InvestSelectedRoute);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeCharger);
        armorRouteButton?.onClick.RemoveListener(SelectArmorRoute);
        shieldRouteButton?.onClick.RemoveListener(SelectShieldRoute);
        pierceDefenseRouteButton?.onClick.RemoveListener(SelectPierceDefenseRoute);
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
        SelectRoute("critical");
    }

    private void SelectRoute(string routeId)
    {
        baseCampManager?.SelectCoreRoute(routeId);
        treeBuilder?.RebuildRoute(routeId);
        Refresh();
    }

    public void SelectOption(string optionId)
    {
        baseCampManager?.SelectCoreOption(optionId);
        Refresh();
    }

    public void InvestOption(string optionId)
    {
        baseCampManager?.InvestCoreOption(optionId);
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
            ? $"Stat Points {baseCampManager.PlayerProgression.StatPoints}"
            : "Stat Points --");
        SetText(selectedRouteText, string.IsNullOrEmpty(coreCharger.SelectedRouteId)
            ? "No Route Selected"
            : $"Selected: {coreCharger.SelectedRouteId}/{coreCharger.SelectedOptionId}");
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
        SetRouteButton(pierceDefenseRouteButton, "critical");
    }

    private string BuildRouteSummary()
    {
        string summary = string.Empty;

        foreach (CoreCharger.CoreRoute route in coreCharger.Routes)
        {
            string state = route.unlocked ? "OPEN" : $"Charger Lv.{route.requiredChargerLevel}";
            summary += $"{route.displayName}: {state} / Route Points {coreCharger.GetRouteInvestedPoints(route)}\n";

            if (route.options == null)
            {
                continue;
            }

            foreach (CoreCharger.CoreRouteOption option in route.options)
            {
                if (option == null)
                {
                    continue;
                }

                int maxPoints = coreCharger.GetOptionMaxPoints(option);
                float bonus = coreCharger.GetOptionBonus(option);
                float currentTierBonus = coreCharger.GetCurrentOptionTierBonusPerPoint(option);
                string selected = option.optionId == coreCharger.SelectedOptionId ? " *" : string.Empty;
                string optionState = coreCharger.IsOptionUnlocked(route, option)
                    ? "OPEN"
                    : $"LOCKED Need {option.requiredRoutePoints}";
                summary += $"  {option.displayName}{selected}: {optionState} / {coreCharger.GetOptionTierLabel(option)} / {option.investedPoints}/{maxPoints} {option.statId} +{bonus:0.##} (+{currentTierBonus:0.##}/pt)\n";
            }
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

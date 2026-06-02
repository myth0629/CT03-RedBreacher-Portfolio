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
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text selectedRouteText;
    [SerializeField] private TMP_Text routeStateText;

    private CoreCharger coreCharger;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeCharger);
        armorRouteButton?.onClick.AddListener(SelectArmorRoute);
        shieldRouteButton?.onClick.AddListener(SelectShieldRoute);
        pierceDefenseRouteButton?.onClick.AddListener(SelectPierceDefenseRoute);
        survivalRouteButton?.onClick.AddListener(SelectSurvivalRoute);
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
        SelectRoute("armor");
    }

    private void SelectShieldRoute()
    {
        SelectRoute("shield");
    }

    private void SelectPierceDefenseRoute()
    {
        SelectRoute("pierce_defense");
    }

    private void SelectSurvivalRoute()
    {
        SelectRoute("survival");
    }

    private void SelectRoute(string routeId)
    {
        baseCampManager?.SelectCoreRoute(routeId);
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
        SetText(selectedRouteText, string.IsNullOrEmpty(coreCharger.SelectedRouteId) ? "No Route Selected" : $"Selected: {coreCharger.SelectedRouteId}");
        SetText(routeStateText, BuildRouteSummary());

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.ResearchLab != null ? baseCampManager.ResearchLab.Level : 1;
            upgradeButton.interactable = coreCharger.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
        }

        SetRouteButton(armorRouteButton, "armor");
        SetRouteButton(shieldRouteButton, "shield");
        SetRouteButton(pierceDefenseRouteButton, "pierce_defense");
        SetRouteButton(survivalRouteButton, "survival");
    }

    private string BuildRouteSummary()
    {
        string summary = string.Empty;

        foreach (CoreCharger.CoreRoute route in coreCharger.Routes)
        {
            summary += $"{route.displayName}: {(route.unlocked ? "OPEN" : $"Lv.{route.requiredChargerLevel}")}\n";
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

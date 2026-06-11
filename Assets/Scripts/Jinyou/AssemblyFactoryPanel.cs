using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssemblyFactoryPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button skillMenuButton;
    [SerializeField] private Button partsMenuButton;
    [SerializeField] private Button weaponEnhanceButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text weaponEnhanceText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text selectedMenuText;
    [SerializeField] private TMP_Text menuStateText;
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private GameObject weaponInventoryArea;
    [SerializeField] private RectTransform weaponInventoryContentRoot;
    [SerializeField] private Button inventoryWeaponButtonPrefab;
    [SerializeField] private TMP_Text inventoryWeaponListText;
    [SerializeField] private PlayerLoadoutSelectionPanel loadoutSelectionPanel;

    private AssemblyFactory assemblyFactory;
    private PlayerLoadoutSelectionPanel loadoutSelectionTemplate;
    private GameObject independentLoadoutPanelObject;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeFactory);
        skillMenuButton?.onClick.AddListener(OpenWeaponSelection);
        partsMenuButton?.onClick.AddListener(OpenDroneSelection);
        weaponEnhanceButton?.onClick.AddListener(EnhanceSelected);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeFactory);
        skillMenuButton?.onClick.RemoveListener(OpenWeaponSelection);
        partsMenuButton?.onClick.RemoveListener(OpenDroneSelection);
        weaponEnhanceButton?.onClick.RemoveListener(EnhanceSelected);
        closeButton?.onClick.RemoveListener(ClosePanel);
    }

    private void OnDestroy()
    {
        if (independentLoadoutPanelObject != null)
        {
            Destroy(independentLoadoutPanelObject);
        }
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button upgrade,
        Button skill,
        Button parts,
        Button close,
        TMP_Text level,
        TMP_Text upgradeLabel,
        TMP_Text selectedMenu,
        TMP_Text menuState)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        skillMenuButton = skill;
        partsMenuButton = parts;
        closeButton = close;
        levelText = level;
        upgradeText = upgradeLabel;
        selectedMenuText = selectedMenu;
        menuStateText = menuState;
        Refresh();
    }

    public void SelectWeapon(ProjectileConfig weaponConfig)
    {
        baseCampManager?.SelectAssemblyWeapon(weaponConfig);
        Refresh();
    }

    public void SelectDrone(DroneConfig droneConfig)
    {
        baseCampManager?.SelectAssemblyDrone(droneConfig);
        Refresh();
    }

    public void SelectWeaponByIndex(int weaponIndex)
    {
        baseCampManager?.SelectAssemblyWeapon(weaponIndex);
        Refresh();
    }

    public void OpenWeaponSelection()
    {
        ResolveReferences();
        if (loadoutSelectionPanel != null)
        {
            loadoutSelectionPanel.OpenWeaponsForSelection(SelectWeapon);
        }
    }

    public void OpenDroneSelection()
    {
        ResolveReferences();
        if (loadoutSelectionPanel != null)
        {
            loadoutSelectionPanel.OpenDronesForSelection(SelectDrone);
        }
    }

    private void EnhanceSelected()
    {
        if (assemblyFactory == null)
        {
            return;
        }

        if (assemblyFactory.SelectedMenuId == "drone")
        {
            baseCampManager?.EnhanceAssemblyDrone();
        }
        else
        {
            baseCampManager?.EnhanceAssemblyWeapon();
        }

        Refresh();
    }

    private void UpgradeFactory()
    {
        baseCampManager?.UpgradeAssemblyFactory();
        Refresh();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        ResolveReferences();
        if (assemblyFactory == null)
        {
            return;
        }

        bool droneMode = assemblyFactory.SelectedMenuId == "drone";
        SetText(levelText, $"Factory Lv.{assemblyFactory.Level}");
        SetText(upgradeText, droneMode
            ? BuildSelectedDroneHeader()
            : BuildSelectedWeaponHeader());
        SetText(selectedMenuText, droneMode ? "Selected Drone SO" : "Selected Weapon SO");
        SetText(weaponEnhanceText, droneMode ? BuildDroneText() : BuildWeaponText());
        SetText(menuStateText, BuildSummary());
        if (baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.CommandCenter != null
                ? baseCampManager.CommandCenter.Level
                : 1;
            SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
                assemblyFactory,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
        }

        SetButtonLabel(skillMenuButton, assemblyFactory.SelectedWeaponConfig != null
            ? $"Weapon: {assemblyFactory.SelectedWeaponConfig.DisplayName}"
            : "Select Weapon");
        SetButtonLabel(partsMenuButton, assemblyFactory.SelectedDroneConfig != null
            ? $"Drone: {assemblyFactory.SelectedDroneConfig.DisplayName}"
            : "Select Drone");
        SetButtonLabel(weaponEnhanceButton, droneMode
            ? BuildDroneEnhanceButtonText()
            : BuildWeaponEnhanceButtonText());
        SetButtonLabel(upgradeButton, assemblyFactory.IsUpgrading
            ? $"Upgrading {assemblyFactory.UpgradeRemainingSeconds:0}s"
            : assemblyFactory.Level >= assemblyFactory.MaxLevel
                ? "Factory MAX"
                : $"Upgrade Factory ({assemblyFactory.UpgradeCost})");

        SetActive(weaponEnhanceButton != null ? weaponEnhanceButton.gameObject : null, true);
        SetActive(upgradeButton != null ? upgradeButton.gameObject : null, true);
        SetActive(skillMenuButton != null ? skillMenuButton.gameObject : null, true);
        SetActive(partsMenuButton != null ? partsMenuButton.gameObject : null, true);
        SetActive(weaponInventoryArea, false);

        if (skillMenuButton != null)
        {
            skillMenuButton.interactable = true;
        }

        if (partsMenuButton != null)
        {
            partsMenuButton.interactable = true;
        }

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.CommandCenter != null
                ? baseCampManager.CommandCenter.Level
                : 1;
            upgradeButton.interactable = assemblyFactory.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(
            upgradeProgressFill,
            assemblyFactory,
            ref observedUpgradeDuration);

        if (weaponEnhanceButton != null && baseCampManager != null)
        {
            weaponEnhanceButton.interactable = droneMode
                ? assemblyFactory.CanEnhanceSelectedDrone(baseCampManager.Credits)
                : assemblyFactory.CanEnhanceSelectedWeapon(baseCampManager.Credits);
        }
    }

    private string BuildWeaponText()
    {
        AssemblyFactory.WeaponEnhancement enhancement = assemblyFactory.SelectedWeaponEnhancement;
        ProjectileConfig weapon = enhancement?.weaponConfig;
        if (enhancement == null || weapon == null)
        {
            return "Select a weapon.";
        }

        float attackBonus = enhancement.GetStatBonus(AssemblyFactory.WeaponEnhancementStat.AttackDamage);
        float currentAttack = weapon.AttackDamage + attackBonus;
        float nextIncrease = GetNextWeaponAttackIncrease(enhancement);
        float nextAttack = currentAttack + nextIncrease;

        if (enhancement.IsMaxLevel)
        {
            return $"{weapon.DisplayName} SO\n"
                + $"Enhance Lv.MAX\n"
                + $"Base Attack {weapon.AttackDamage:0.##}\n"
                + $"Enhance Bonus +{attackBonus:0.##}\n"
                + $"Applied Attack {currentAttack:0.##}";
        }

        return $"{weapon.DisplayName} SO\n"
            + $"Enhance Lv.{enhancement.enhanceLevel}/{enhancement.MaxEnhanceLevel}\n"
            + $"Base Attack {weapon.AttackDamage:0.##}\n"
            + $"Current {currentAttack:0.##}  ->  Next {nextAttack:0.##}\n"
            + $"Next Increase +{nextIncrease:0.##} / Cost {enhancement.NextEnhanceCost}";
    }

    private string BuildDroneText()
    {
        AssemblyFactory.DroneEnhancement enhancement = assemblyFactory.SelectedDroneEnhancement;
        DroneConfig drone = enhancement?.droneConfig;
        if (enhancement == null || drone == null)
        {
            return "Select a drone.";
        }

        float currentAttack = drone.AttackDamage + enhancement.AttackDamageBonus;
        float nextAttack = currentAttack + enhancement.attackDamagePerLevel;

        if (enhancement.IsMaxLevel)
        {
            return $"{drone.DisplayName} SO\n"
                + $"Enhance Lv.MAX\n"
                + $"Base Attack {drone.AttackDamage:0.##}\n"
                + $"Enhance Bonus +{enhancement.AttackDamageBonus:0.##}\n"
                + $"Applied Attack {currentAttack:0.##}";
        }

        return $"{drone.DisplayName} SO\n"
            + $"Enhance Lv.{enhancement.enhanceLevel}/{enhancement.maxEnhanceLevel}\n"
            + $"Base Attack {drone.AttackDamage:0.##}\n"
            + $"Current {currentAttack:0.##}  ->  Next {nextAttack:0.##}\n"
            + $"Next Increase +{enhancement.attackDamagePerLevel:0.##} / Cost {enhancement.costPerEnhancement}";
    }

    private string BuildSummary()
    {
        string weaponName = assemblyFactory.SelectedWeaponConfig != null
            ? assemblyFactory.SelectedWeaponConfig.DisplayName
            : "None";
        string droneName = assemblyFactory.SelectedDroneConfig != null
            ? assemblyFactory.SelectedDroneConfig.DisplayName
            : "None";
        string activeTarget = assemblyFactory.SelectedMenuId == "drone" ? droneName : weaponName;
        return $"Enhancing SO: {activeTarget}\nWeapon SO: {weaponName}\nDrone SO: {droneName}";
    }

    private string BuildSelectedWeaponHeader()
    {
        ProjectileConfig weapon = assemblyFactory.SelectedWeaponConfig;
        return weapon != null ? $"Enhancing Weapon SO: {weapon.DisplayName}" : "Select a Weapon SO";
    }

    private string BuildSelectedDroneHeader()
    {
        DroneConfig drone = assemblyFactory.SelectedDroneConfig;
        return drone != null ? $"Enhancing Drone SO: {drone.DisplayName}" : "Select a Drone SO";
    }

    private string BuildWeaponEnhanceButtonText()
    {
        AssemblyFactory.WeaponEnhancement enhancement = assemblyFactory.SelectedWeaponEnhancement;
        if (enhancement?.weaponConfig == null)
        {
            return "Select Weapon First";
        }

        if (enhancement.IsMaxLevel)
        {
            return $"{enhancement.weaponConfig.DisplayName} MAX";
        }

        float increase = GetNextWeaponAttackIncrease(enhancement);
        return $"Enhance {enhancement.weaponConfig.DisplayName} +{increase:0.##}";
    }

    private string BuildDroneEnhanceButtonText()
    {
        AssemblyFactory.DroneEnhancement enhancement = assemblyFactory.SelectedDroneEnhancement;
        if (enhancement?.droneConfig == null)
        {
            return "Select Drone First";
        }

        return enhancement.IsMaxLevel
            ? $"{enhancement.droneConfig.DisplayName} MAX"
            : $"Enhance {enhancement.droneConfig.DisplayName} +{enhancement.attackDamagePerLevel:0.##}";
    }

    private static float GetNextWeaponAttackIncrease(AssemblyFactory.WeaponEnhancement enhancement)
    {
        AssemblyFactory.WeaponEnhancementLevel nextLevel =
            enhancement?.GetEnhancementLevel(enhancement.enhanceLevel);
        if (nextLevel?.statIncreases == null)
        {
            return 0f;
        }

        float increase = 0f;
        foreach (AssemblyFactory.WeaponStatIncrease statIncrease in nextLevel.statIncreases)
        {
            if (statIncrease != null
                && statIncrease.stat == AssemblyFactory.WeaponEnhancementStat.AttackDamage)
            {
                increase += statIncrease.amount;
            }
        }

        return increase;
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        assemblyFactory = baseCampManager != null
            ? baseCampManager.AssemblyFactory
            : FindFirstObjectByType<AssemblyFactory>();
        EnsureIndependentLoadoutPanel();
    }

    private void EnsureIndependentLoadoutPanel()
    {
        if (independentLoadoutPanelObject != null && loadoutSelectionPanel != null)
        {
            return;
        }

        loadoutSelectionTemplate ??= loadoutSelectionPanel != null
            ? loadoutSelectionPanel
            : FindFirstObjectByType<PlayerLoadoutSelectionPanel>(FindObjectsInactive.Include);
        if (loadoutSelectionTemplate == null)
        {
            return;
        }

        Canvas rootCanvas = loadoutSelectionTemplate.GetComponentInParent<Canvas>(true);
        Transform parent = rootCanvas != null ? rootCanvas.transform : transform.root;
        independentLoadoutPanelObject = Instantiate(loadoutSelectionTemplate.gameObject, parent, false);
        independentLoadoutPanelObject.name = "AssemblyFactory_LoadoutSelectionPanel";
        independentLoadoutPanelObject.transform.SetAsLastSibling();
        loadoutSelectionPanel = independentLoadoutPanelObject.GetComponent<PlayerLoadoutSelectionPanel>();
        independentLoadoutPanelObject.SetActive(false);
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button != null)
        {
            SetText(button.GetComponentInChildren<TMP_Text>(true), value);
        }
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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssemblyFactoryPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button weaponMenuButton;
    [SerializeField] private Button droneMenuButton;
    [SerializeField] private Button weaponEnhanceButton;
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
        weaponMenuButton?.onClick.AddListener(OpenWeaponSelection);
        droneMenuButton?.onClick.AddListener(OpenDroneSelection);
        weaponEnhanceButton?.onClick.AddListener(EnhanceSelected);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeFactory);
        weaponMenuButton?.onClick.RemoveListener(OpenWeaponSelection);
        droneMenuButton?.onClick.RemoveListener(OpenDroneSelection);
        weaponEnhanceButton?.onClick.RemoveListener(EnhanceSelected);
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
        TMP_Text level,
        TMP_Text upgradeLabel,
        TMP_Text selectedMenu,
        TMP_Text menuState)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        weaponMenuButton = skill;
        droneMenuButton = parts;
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
        SetText(levelText, $"Lv. {assemblyFactory.Level}");
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

        SetButtonLabel(weaponMenuButton, assemblyFactory.SelectedWeaponConfig != null
            ? $"{assemblyFactory.SelectedWeaponConfig.DisplayName}"
            : "무기 선택");
        SetButtonLabel(droneMenuButton, assemblyFactory.SelectedDroneConfig != null
            ? $"{assemblyFactory.SelectedDroneConfig.DisplayName}"
            : "드론 선택");
        SetButtonLabel(weaponEnhanceButton, droneMode
            ? BuildDroneEnhanceButtonText()
            : BuildWeaponEnhanceButtonText());
        SetButtonLabel(upgradeButton, assemblyFactory.IsUpgrading
            ? $"완료까지 {assemblyFactory.UpgradeRemainingSeconds:0}초"
            : assemblyFactory.Level >= assemblyFactory.MaxLevel
                ? "최대레벨"
                : $"기지 업그레이드 ({assemblyFactory.UpgradeCost} 크레딧)");

        SetActive(weaponEnhanceButton != null ? weaponEnhanceButton.gameObject : null, true);
        SetActive(upgradeButton != null ? upgradeButton.gameObject : null, true);
        SetActive(weaponMenuButton != null ? weaponMenuButton.gameObject : null, true);
        SetActive(droneMenuButton != null ? droneMenuButton.gameObject : null, true);
        SetActive(weaponInventoryArea, false);

        if (weaponMenuButton != null)
        {
            weaponMenuButton.interactable = true;
        }

        if (droneMenuButton != null)
        {
            droneMenuButton.interactable = true;
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
            return "강화하고자 하는 무기/드론을 선택하세요.";
        }

        float attackBonus = enhancement.GetStatBonus(AssemblyFactory.WeaponEnhancementStat.AttackDamage);
        float currentAttack = weapon.AttackDamage + attackBonus;
        float nextIncrease = GetNextWeaponAttackIncrease(enhancement);
        float nextAttack = currentAttack + nextIncrease;

        if (enhancement.IsMaxLevel)
        {
            return $"{weapon.DisplayName} (Lv.최대)\n"
                + $"피해량 {weapon.AttackDamage:0.##} (강화 보너스 +{attackBonus:0.##})\n"
                + $"종합 피해량 {currentAttack:0.##}";
        }

        return $"{weapon.DisplayName} (Lv.{enhancement.enhanceLevel}/{enhancement.MaxEnhanceLevel})\n"
            + $"피해량 {weapon.AttackDamage:0.##} (강화 전 {currentAttack:0.##}  ->  강화 후 {nextAttack:0.##})\n"
            + $"다음 강화 피해량 +{nextIncrease:0.##} / {enhancement.NextEnhanceCost} 크레딧";
    }

    private string BuildDroneText()
    {
        AssemblyFactory.DroneEnhancement enhancement = assemblyFactory.SelectedDroneEnhancement;
        DroneConfig drone = enhancement?.droneConfig;
        if (enhancement == null || drone == null)
        {
            return "드론을 선택하세요.";
        }

        float currentAttack = drone.AttackDamage + enhancement.AttackDamageBonus;
        float nextAttack = currentAttack + enhancement.attackDamagePerLevel;

        if (enhancement.IsMaxLevel)
        {
            return $"{drone.DisplayName}\n"
                + $"강화 Lv.최대\n"
                + $"피해량 {drone.AttackDamage:0.##}\n"
                + $"강화 보너스 +{enhancement.AttackDamageBonus:0.##}\n"
                + $"종합 피해량 {currentAttack:0.##}";
        }

        return $"{drone.DisplayName}\n"
            + $"강화 Lv.{enhancement.enhanceLevel}/{enhancement.maxEnhanceLevel}\n"
            + $"피해량 {drone.AttackDamage:0.##}\n"
            + $"강화 전 {currentAttack:0.##}  ->  강화 후 {nextAttack:0.##}\n"
            + $"다음 강화 피해량 +{enhancement.attackDamagePerLevel:0.##} / {enhancement.costPerEnhancement} 크래딧";
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
            return "강화하기";
        }

        if (enhancement.IsMaxLevel)
        {
            return $"{enhancement.weaponConfig.DisplayName} 모두 완료됨";
        }

        float increase = GetNextWeaponAttackIncrease(enhancement);
        return $"강화하기 {enhancement.weaponConfig.DisplayName} +{increase:0.##}";
    }

    private string BuildDroneEnhanceButtonText()
    {
        AssemblyFactory.DroneEnhancement enhancement = assemblyFactory.SelectedDroneEnhancement;
        if (enhancement?.droneConfig == null)
        {
            return "드론을 먼저 선택하십시오.";
        }

        return enhancement.IsMaxLevel
            ? $"{enhancement.droneConfig.DisplayName} 모두 완료됨"
            : $"강화하기 {enhancement.droneConfig.DisplayName} +{enhancement.attackDamagePerLevel:0.##}";
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

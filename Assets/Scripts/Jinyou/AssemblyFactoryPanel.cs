using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssemblyFactoryPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button weaponMenuButton;
    [SerializeField] private Button mechMenuButton;
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

    private AssemblyFactory assemblyFactory;
    private InventoryFacility inventory;
    private readonly List<Button> spawnedWeaponButtons = new List<Button>();
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventoryEvents();
        upgradeButton?.onClick.AddListener(UpgradeFactory);
        weaponMenuButton?.onClick.AddListener(SelectWeaponMenu);
        mechMenuButton?.onClick.AddListener(SelectMechMenu);
        skillMenuButton?.onClick.AddListener(SelectSkillMenu);
        partsMenuButton?.onClick.AddListener(SelectPartsMenu);
        weaponEnhanceButton?.onClick.AddListener(EnhanceWeapon);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvents();
        upgradeButton?.onClick.RemoveListener(UpgradeFactory);
        closeButton?.onClick.RemoveListener(ClosePanel);
        weaponMenuButton?.onClick.RemoveListener(SelectWeaponMenu);
        mechMenuButton?.onClick.RemoveListener(SelectMechMenu);
        skillMenuButton?.onClick.RemoveListener(SelectSkillMenu);
        partsMenuButton?.onClick.RemoveListener(SelectPartsMenu);
        weaponEnhanceButton?.onClick.RemoveListener(EnhanceWeapon);
        ClearWeaponButtons();
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button upgrade,
        Button weapon,
        Button mech,
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
        weaponMenuButton = weapon;
        mechMenuButton = mech;
        skillMenuButton = skill;
        partsMenuButton = parts;
        closeButton = close;
        levelText = level;
        upgradeText = upgradeLabel;
        selectedMenuText = selectedMenu;
        menuStateText = menuState;
        Refresh();
    }

    private void UpgradeFactory()
    {
        baseCampManager?.UpgradeAssemblyFactory();
        Refresh();
    }

    private void EnhanceWeapon()
    {
        baseCampManager?.EnhanceAssemblyWeapon();
        Refresh();
    }

    public void SelectWeapon(ProjectileConfig weaponConfig)
    {
        baseCampManager?.SelectAssemblyWeapon(weaponConfig);
        Refresh();
    }

    public void SelectWeaponByIndex(int weaponIndex)
    {
        baseCampManager?.SelectAssemblyWeapon(weaponIndex);
        Refresh();
    }

    private void SelectWeaponMenu()
    {
        SelectMenu("weapon");
        OpenInventoryWeaponSelection();
    }

    private void SelectMechMenu()
    {
        SelectMenu("mech");
        SetActive(weaponInventoryArea, false);
    }

    private void SelectSkillMenu()
    {
        SelectMenu("skill");
        SetActive(weaponInventoryArea, false);
    }

    private void SelectPartsMenu()
    {
        SelectMenu("parts");
        SetActive(weaponInventoryArea, false);
    }

    private void SelectMenu(string menuId)
    {
        baseCampManager?.SelectAssemblyMenu(menuId);
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

        SetText(levelText, $"Lv. {assemblyFactory.Level}");
        SetText(upgradeText, assemblyFactory.IsUpgrading
            ? $"Upgrading {assemblyFactory.UpgradeRemainingSeconds:0}s"
            : $"Upgrade Cost {assemblyFactory.UpgradeCost}");
        SetText(weaponEnhanceText, BuildSelectedWeaponEnhancementText());
        SetText(selectedMenuText, string.IsNullOrEmpty(assemblyFactory.SelectedMenuId) ? "No Menu Selected" : $"Selected: {assemblyFactory.SelectedMenuId}");
        SetText(menuStateText, BuildMenuSummary());
        SetText(inventoryWeaponListText, BuildInventoryWeaponListText());
        SetActive(weaponInventoryArea, inventoryPanel == null && assemblyFactory.SelectedMenuId == "weapon");

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.ResearchLab != null ? baseCampManager.ResearchLab.Level : 1;
            upgradeButton.interactable = assemblyFactory.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
            SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
                assemblyFactory,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, assemblyFactory, ref observedUpgradeDuration);

        SetMenuButton(weaponMenuButton, "weapon");
        SetMenuButton(mechMenuButton, "mech");
        SetMenuButton(skillMenuButton, "skill");
        SetMenuButton(partsMenuButton, "parts");

        if (weaponEnhanceButton != null && baseCampManager != null)
        {
            weaponEnhanceButton.interactable = assemblyFactory.CanEnhanceSelectedWeapon(baseCampManager.Credits);
        }
    }

    private string BuildMenuSummary()
    {
        string summary = string.Empty;

        foreach (AssemblyFactory.AssemblyMenu menu in assemblyFactory.Menus)
        {
            summary += $"{menu.displayName}: {(menu.unlocked ? "OPEN" : $"Lv.{menu.requiredFactoryLevel}")}\n";
        }

        foreach (AssemblyFactory.WeaponEnhancement weaponEnhancement in assemblyFactory.WeaponEnhancements)
        {
            if (weaponEnhancement == null)
            {
                continue;
            }

            summary += $"{weaponEnhancement.DisplayName}: {BuildStatBonusSummary(weaponEnhancement)} (Lv.{weaponEnhancement.enhanceLevel}/{weaponEnhancement.MaxEnhanceLevel})\n";
        }

        return summary.TrimEnd();
    }

    private string BuildInventoryWeaponListText()
    {
        if (inventory == null)
        {
            return "Inventory not connected";
        }

        if (inventory.WeaponConfigs.Count == 0)
        {
            return "No Inventory Weapons";
        }

        string summary = string.Empty;
        for (int i = 0; i < inventory.WeaponConfigs.Count; i++)
        {
            ProjectileConfig weapon = inventory.WeaponConfigs[i];
            if (weapon == null)
            {
                continue;
            }

            string selected = weapon == assemblyFactory.SelectedWeaponConfig ? " *" : string.Empty;
            string configured = assemblyFactory.HasWeaponEnhancement(weapon) ? string.Empty : " (No Enhance Data)";
            summary += $"{i + 1}. {weapon.DisplayName}{selected}{configured}\n";
        }

        return summary.TrimEnd();
    }

    private string BuildSelectedWeaponEnhancementText()
    {
        AssemblyFactory.WeaponEnhancement selectedWeapon = assemblyFactory.SelectedWeaponEnhancement;
        if (selectedWeapon == null)
        {
            return "No Weapon Selected";
        }

        if (selectedWeapon.IsMaxLevel)
        {
            return $"{selectedWeapon.DisplayName} Lv.MAX {BuildStatBonusSummary(selectedWeapon)}";
        }

        return $"{selectedWeapon.DisplayName} Lv.{selectedWeapon.enhanceLevel}/{selectedWeapon.MaxEnhanceLevel} {BuildStatBonusSummary(selectedWeapon)} / Cost {selectedWeapon.NextEnhanceCost} / Next {BuildNextStatIncreaseSummary(selectedWeapon)}";
    }

    private static string BuildStatBonusSummary(AssemblyFactory.WeaponEnhancement weaponEnhancement)
    {
        if (weaponEnhancement == null)
        {
            return string.Empty;
        }

        string summary = string.Empty;
        foreach (AssemblyFactory.WeaponEnhancementStat stat in System.Enum.GetValues(typeof(AssemblyFactory.WeaponEnhancementStat)))
        {
            float bonus = weaponEnhancement.GetStatBonus(stat);
            if (bonus <= 0f)
            {
                continue;
            }

            summary += $"{AssemblyFactory.GetStatDisplayName(stat)} +{bonus:0.#} ";
        }

        return string.IsNullOrWhiteSpace(summary) ? "No Bonus" : summary.TrimEnd();
    }

    private static string BuildNextStatIncreaseSummary(AssemblyFactory.WeaponEnhancement weaponEnhancement)
    {
        AssemblyFactory.WeaponEnhancementLevel nextLevel = weaponEnhancement?.GetEnhancementLevel(weaponEnhancement.enhanceLevel);
        if (nextLevel == null || nextLevel.statIncreases == null || nextLevel.statIncreases.Count == 0)
        {
            return "No Bonus";
        }

        string summary = string.Empty;
        foreach (AssemblyFactory.WeaponStatIncrease statIncrease in nextLevel.statIncreases)
        {
            if (statIncrease == null || statIncrease.amount <= 0f)
            {
                continue;
            }

            summary += $"{AssemblyFactory.GetStatDisplayName(statIncrease.stat)} +{statIncrease.amount:0.#} ";
        }

        return string.IsNullOrWhiteSpace(summary) ? "No Bonus" : summary.TrimEnd();
    }

    private void SetMenuButton(Button button, string menuId)
    {
        if (button != null)
        {
            button.interactable = assemblyFactory.IsMenuUnlocked(menuId);
        }
    }

    private void RebuildInventoryWeaponButtons()
    {
        ClearWeaponButtons();

        if (weaponInventoryContentRoot == null || inventoryWeaponButtonPrefab == null || inventory == null)
        {
            return;
        }

        foreach (ProjectileConfig weapon in inventory.WeaponConfigs)
        {
            if (weapon == null)
            {
                continue;
            }

            Button button = Instantiate(inventoryWeaponButtonPrefab, weaponInventoryContentRoot);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                string selected = weapon == assemblyFactory.SelectedWeaponConfig ? " *" : string.Empty;
                string configured = assemblyFactory.HasWeaponEnhancement(weapon) ? string.Empty : " (No Data)";
                label.text = $"{weapon.DisplayName}{selected}{configured}";
            }

            ProjectileConfig capturedWeapon = weapon;
            button.interactable = assemblyFactory.HasWeaponEnhancement(capturedWeapon);
            button.onClick.AddListener(() => SelectInventoryWeapon(capturedWeapon));
            spawnedWeaponButtons.Add(button);
        }
    }

    private void SelectInventoryWeapon(ProjectileConfig weapon)
    {
        SelectWeapon(weapon);
        RebuildInventoryWeaponButtons();
    }

    private void ClearWeaponButtons()
    {
        foreach (Button button in spawnedWeaponButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawnedWeaponButtons.Clear();
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
        RebuildInventoryWeaponButtons();
        Refresh();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        assemblyFactory = baseCampManager != null ? baseCampManager.AssemblyFactory : null;
        inventory = baseCampManager != null ? baseCampManager.Inventory : FindFirstObjectByType<InventoryFacility>();
        inventoryPanel ??= FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
    }

    private void OpenInventoryWeaponSelection()
    {
        ResolveReferences();

        if (inventoryPanel == null || assemblyFactory == null)
        {
            SetActive(weaponInventoryArea, true);
            RebuildInventoryWeaponButtons();
            return;
        }

        inventoryPanel.OpenWeaponSelectMode(
            SelectWeapon,
            weapon => assemblyFactory.HasWeaponEnhancement(weapon));
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

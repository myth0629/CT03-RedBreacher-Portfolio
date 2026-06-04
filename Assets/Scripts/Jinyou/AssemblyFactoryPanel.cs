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
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text selectedMenuText;
    [SerializeField] private TMP_Text menuStateText;

    private AssemblyFactory assemblyFactory;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeFactory);
        weaponMenuButton?.onClick.AddListener(SelectWeaponMenu);
        mechMenuButton?.onClick.AddListener(SelectMechMenu);
        skillMenuButton?.onClick.AddListener(SelectSkillMenu);
        partsMenuButton?.onClick.AddListener(SelectPartsMenu);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeFactory);
        weaponMenuButton?.onClick.RemoveListener(SelectWeaponMenu);
        mechMenuButton?.onClick.RemoveListener(SelectMechMenu);
        skillMenuButton?.onClick.RemoveListener(SelectSkillMenu);
        partsMenuButton?.onClick.RemoveListener(SelectPartsMenu);
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

    private void SelectWeaponMenu()
    {
        SelectMenu("weapon");
    }

    private void SelectMechMenu()
    {
        SelectMenu("mech");
    }

    private void SelectSkillMenu()
    {
        SelectMenu("skill");
    }

    private void SelectPartsMenu()
    {
        SelectMenu("parts");
    }

    private void SelectMenu(string menuId)
    {
        baseCampManager?.SelectAssemblyMenu(menuId);
        Refresh();
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
        SetText(selectedMenuText, string.IsNullOrEmpty(assemblyFactory.SelectedMenuId) ? "No Menu Selected" : $"Selected: {assemblyFactory.SelectedMenuId}");
        SetText(menuStateText, BuildMenuSummary());

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
    }

    private string BuildMenuSummary()
    {
        string summary = string.Empty;

        foreach (AssemblyFactory.AssemblyMenu menu in assemblyFactory.Menus)
        {
            summary += $"{menu.displayName}: {(menu.unlocked ? "OPEN" : $"Lv.{menu.requiredFactoryLevel}")}\n";
        }

        return summary.TrimEnd();
    }

    private void SetMenuButton(Button button, string menuId)
    {
        if (button != null)
        {
            button.interactable = assemblyFactory.IsMenuUnlocked(menuId);
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        assemblyFactory = baseCampManager != null ? baseCampManager.AssemblyFactory : null;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

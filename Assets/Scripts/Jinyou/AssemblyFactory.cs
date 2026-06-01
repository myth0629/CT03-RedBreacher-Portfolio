using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyFactory : MonoBehaviour, IBaseCampFacility
{
    [Serializable]
    public class AssemblyMenu
    {
        public string menuId;
        public string displayName;
        public int requiredFactoryLevel = 1;
        public bool unlocked;
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 350;
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 1;
    [SerializeField] private float upgradeDurationSeconds = 10f;

    [Header("Placeholder Menus")]
    [SerializeField] private List<AssemblyMenu> menus = new List<AssemblyMenu>
    {
        new AssemblyMenu { menuId = "weapon", displayName = "Weapon Upgrade", requiredFactoryLevel = 1, unlocked = true },
        new AssemblyMenu { menuId = "mech", displayName = "Mech Upgrade", requiredFactoryLevel = 1, unlocked = true },
        new AssemblyMenu { menuId = "skill", displayName = "Skill Upgrade", requiredFactoryLevel = 2 }
    };

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<string> OnMenuUnlocked = new UnityEvent<string>();
    public UnityEvent<string> OnMenuSelected = new UnityEvent<string>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private string selectedMenuId;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;

    public int Level => level;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => requiredResearchLabLevel;
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public string SelectedMenuId => selectedMenuId;
    public IReadOnlyList<AssemblyMenu> Menus => menus;

    private void Start()
    {
        RefreshUnlocks();
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
    }

    public bool IsMenuUnlocked(string menuId)
    {
        AssemblyMenu menu = menus.Find(item => item.menuId == menuId);
        return menu != null && menu.unlocked;
    }

    public bool TrySelectMenu(string menuId)
    {
        if (!IsMenuUnlocked(menuId))
        {
            return false;
        }

        selectedMenuId = menuId;
        OnMenuSelected.Invoke(menuId);
        return true;
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= upgradeCost && commanderLevel >= requiredCommanderLevel;
    }

    public bool CanStartUpgrade(int availableCredits, int commanderLevel, int researchLabLevel)
    {
        return CanUpgrade(availableCredits, commanderLevel) && researchLabLevel >= requiredResearchLabLevel;
    }

    public bool TryStartUpgrade(ref int availableCredits, int commanderLevel, int researchLabLevel)
    {
        if (!CanStartUpgrade(availableCredits, commanderLevel, researchLabLevel))
        {
            return false;
        }

        availableCredits -= upgradeCost;
        StartUpgradeTimer();
        return true;
    }

    public void Upgrade()
    {
        if (isUpgrading)
        {
            CompleteUpgrade();
            return;
        }

        if (level >= maxLevel)
        {
            return;
        }

        OnUpgradeStarted.Invoke();
        CompleteUpgrade();
    }

    public void CompleteUpgradeImmediately()
    {
        Upgrade();
    }

    private void StartUpgradeTimer()
    {
        OnUpgradeStarted.Invoke();

        if (upgradeDurationSeconds <= 0f)
        {
            CompleteUpgrade();
            return;
        }

        isUpgrading = true;
        upgradeRemainingSeconds = upgradeDurationSeconds;
    }

    private void TickUpgrade(float deltaTime)
    {
        if (!isUpgrading)
        {
            return;
        }

        upgradeRemainingSeconds -= deltaTime;

        if (upgradeRemainingSeconds <= 0f)
        {
            CompleteUpgrade();
        }
    }

    private void CompleteUpgrade()
    {
        if (level >= maxLevel)
        {
            isUpgrading = false;
            upgradeRemainingSeconds = 0f;
            return;
        }

        isUpgrading = false;
        upgradeRemainingSeconds = 0f;
        level++;
        upgradeCost = Mathf.RoundToInt(upgradeCost * 1.35f);
        requiredCommanderLevel++;
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private void RefreshUnlocks()
    {
        foreach (AssemblyMenu menu in menus)
        {
            if (!menu.unlocked && level >= menu.requiredFactoryLevel)
            {
                menu.unlocked = true;
                OnMenuUnlocked.Invoke(menu.menuId);
            }
        }
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
    }
}

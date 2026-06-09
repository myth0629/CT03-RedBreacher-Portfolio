using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyFactory : MonoBehaviour, IBaseCampFacility
{
    private const string WeaponEnhancementSaveKey = "AssemblyFactory.WeaponEnhancements";

    [Serializable]
    private class WeaponEnhancementSaveEntry
    {
        public string weaponId;
        public int level;
    }

    [Serializable]
    private class WeaponEnhancementSaveData
    {
        public string selectedWeaponId;
        public List<WeaponEnhancementSaveEntry> weapons = new List<WeaponEnhancementSaveEntry>();
    }

    public enum WeaponEnhancementStat
    {
        AttackDamage,
        Speed,
        Lifetime,
        CollisionRadius,
        KnockbackForce
    }

    [Serializable]
    public class AssemblyMenu
    {
        public string menuId;
        public string displayName;
        public int requiredFactoryLevel = 1;
        public bool unlocked;
        public int developmentLevel;
        public int maxDevelopmentLevel = 5;
        public int developmentCost = 150;
        public List<int> developmentCostByLevel = new List<int>();
        public float powerBonusPerLevel = 0.02f;

        public int MaxDevelopmentLevel => Mathf.Max(1, maxDevelopmentLevel);
        public bool IsMaxDevelopmentLevel => developmentLevel >= MaxDevelopmentLevel;
        public int NextDevelopmentCost => GetDevelopmentCost(developmentLevel);
        public float PowerBonus => Mathf.Max(0, developmentLevel) * Mathf.Max(0f, powerBonusPerLevel);

        public int GetDevelopmentCost(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= MaxDevelopmentLevel)
            {
                return 0;
            }

            if (developmentCostByLevel != null && levelIndex < developmentCostByLevel.Count)
            {
                return Mathf.Max(0, developmentCostByLevel[levelIndex]);
            }

            return Mathf.Max(0, developmentCost);
        }

        public void Normalize()
        {
            requiredFactoryLevel = Mathf.Max(1, requiredFactoryLevel);
            maxDevelopmentLevel = Mathf.Max(1, maxDevelopmentLevel);
            developmentLevel = Mathf.Clamp(developmentLevel, 0, maxDevelopmentLevel);
            developmentCost = Mathf.Max(0, developmentCost);
            powerBonusPerLevel = Mathf.Max(0f, powerBonusPerLevel);
            developmentCostByLevel ??= new List<int>();

            while (developmentCostByLevel.Count < maxDevelopmentLevel)
            {
                developmentCostByLevel.Add(developmentCost);
            }

            for (int i = 0; i < developmentCostByLevel.Count; i++)
            {
                developmentCostByLevel[i] = Mathf.Max(0, developmentCostByLevel[i]);
            }
        }
    }

    [Serializable]
    public class WeaponStatIncrease
    {
        public WeaponEnhancementStat stat = WeaponEnhancementStat.AttackDamage;
        public float amount = 5f;

        public void Normalize()
        {
            amount = Mathf.Max(0f, amount);
        }
    }

    [Serializable]
    public class WeaponEnhancementLevel
    {
        public int cost = 100;
        public List<WeaponStatIncrease> statIncreases = new List<WeaponStatIncrease>
        {
            new WeaponStatIncrease { stat = WeaponEnhancementStat.AttackDamage, amount = 5f }
        };

        public void Normalize()
        {
            cost = Mathf.Max(0, cost);
            if (statIncreases == null)
            {
                statIncreases = new List<WeaponStatIncrease>();
            }

            foreach (WeaponStatIncrease statIncrease in statIncreases)
            {
                statIncrease?.Normalize();
            }
        }
    }

    [Serializable]
    public class WeaponEnhancement
    {
        public ProjectileConfig weaponConfig;
        public string displayNameOverride;
        public int enhanceLevel;
        public List<WeaponEnhancementLevel> enhancementLevels = new List<WeaponEnhancementLevel>
        {
            new WeaponEnhancementLevel()
        };

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayNameOverride))
                {
                    return displayNameOverride;
                }

                return weaponConfig != null ? weaponConfig.DisplayName : "Unassigned Weapon";
            }
        }

        public int MaxEnhanceLevel => enhancementLevels != null ? enhancementLevels.Count : 0;
        public int NextEnhanceCost => GetEnhancementLevel(enhanceLevel)?.cost ?? 0;
        public bool IsMaxLevel => enhanceLevel >= MaxEnhanceLevel;

        public float GetStatBonus(WeaponEnhancementStat stat)
        {
            float bonus = 0f;
            for (int i = 0; i < enhanceLevel; i++)
            {
                WeaponEnhancementLevel level = GetEnhancementLevel(i);
                if (level == null || level.statIncreases == null)
                {
                    continue;
                }

                foreach (WeaponStatIncrease statIncrease in level.statIncreases)
                {
                    if (statIncrease != null && statIncrease.stat == stat)
                    {
                        bonus += statIncrease.amount;
                    }
                }
            }

            return bonus;
        }

        public WeaponEnhancementLevel GetEnhancementLevel(int levelIndex)
        {
            if (enhancementLevels == null || levelIndex < 0 || levelIndex >= enhancementLevels.Count)
            {
                return null;
            }

            return enhancementLevels[levelIndex];
        }

        public void Normalize()
        {
            enhanceLevel = Mathf.Max(0, enhanceLevel);

            if (enhancementLevels == null)
            {
                enhancementLevels = new List<WeaponEnhancementLevel>();
            }

            foreach (WeaponEnhancementLevel level in enhancementLevels)
            {
                level?.Normalize();
            }

            enhanceLevel = Mathf.Min(enhanceLevel, MaxEnhanceLevel);
        }
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 350;
    [SerializeField] private List<int> upgradeCostByLevel = new List<int>();
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private List<int> requiredCommanderLevelByLevel = new List<int>();
    [SerializeField] private int requiredResearchLabLevel = 2;
    [SerializeField] private List<int> requiredResearchLabLevelByLevel = new List<int>();
    [SerializeField] private float upgradeDurationSeconds = 10f;
    [SerializeField] private List<float> upgradeDurationSecondsByLevel = new List<float>();

    [Header("Assembly Menus")]
    [SerializeField] private List<AssemblyMenu> menus = new List<AssemblyMenu>
    {
        new AssemblyMenu { menuId = "weapon", displayName = "Weapon Upgrade", requiredFactoryLevel = 1, unlocked = true, developmentCost = 150, powerBonusPerLevel = 0.01f },
        new AssemblyMenu { menuId = "mech", displayName = "Mech Upgrade", requiredFactoryLevel = 1, unlocked = true, developmentCost = 200, powerBonusPerLevel = 0.03f },
        new AssemblyMenu { menuId = "skill", displayName = "Skill Upgrade", requiredFactoryLevel = 2, developmentCost = 250, powerBonusPerLevel = 0.025f },
        new AssemblyMenu { menuId = "parts", displayName = "Parts Crafting", requiredFactoryLevel = 3, developmentCost = 300, powerBonusPerLevel = 0.02f }
    };

    [Header("Weapon Enhancement")]
    [SerializeField] private List<WeaponEnhancement> weaponEnhancements = new List<WeaponEnhancement>();
    [SerializeField] private int selectedWeaponIndex;

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<string> OnMenuUnlocked = new UnityEvent<string>();
    public UnityEvent<string> OnMenuSelected = new UnityEvent<string>();
    public UnityEvent<ProjectileConfig> OnWeaponSelected = new UnityEvent<ProjectileConfig>();
    public UnityEvent<ProjectileConfig, int> OnWeaponEnhanced = new UnityEvent<ProjectileConfig, int>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private string selectedMenuId;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int UpgradeCost => GetUpgradeCostForCurrentLevel();
    public int RequiredCommanderLevel => GetRequiredCommanderLevelForCurrentUpgrade();
    public int RequiredResearchLabLevel => GetRequiredResearchLabLevelForCurrentUpgrade();
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public string SelectedMenuId => selectedMenuId;
    public AssemblyMenu SelectedMenu => GetMenu(selectedMenuId);
    public IReadOnlyList<AssemblyMenu> Menus => menus;
    public IReadOnlyList<WeaponEnhancement> WeaponEnhancements => weaponEnhancements;
    public WeaponEnhancement SelectedWeaponEnhancement => GetWeaponEnhancementAt(selectedWeaponIndex);
    public ProjectileConfig SelectedWeaponConfig => SelectedWeaponEnhancement?.weaponConfig;
    public int SelectedWeaponIndex => selectedWeaponIndex;

    private void Awake()
    {
        NormalizeUpgradeCosts();
        NormalizeCommanderRequirements();
        NormalizeMenus();
        NormalizeWeaponEnhancements();
        LoadWeaponEnhancements();
        RefreshUnlocks();
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
    }

    public bool IsMenuUnlocked(string menuId)
    {
        AssemblyMenu menu = GetMenu(menuId);
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

    public bool CanDevelopSelectedMenu(int credits)
    {
        AssemblyMenu selectedMenu = SelectedMenu;
        return selectedMenu != null
            && selectedMenu.unlocked
            && !selectedMenu.IsMaxDevelopmentLevel
            && credits >= selectedMenu.NextDevelopmentCost;
    }

    public bool TryDevelopSelectedMenu(ref int availableCredits)
    {
        if (!CanDevelopSelectedMenu(availableCredits))
        {
            return false;
        }

        AssemblyMenu selectedMenu = SelectedMenu;
        availableCredits -= selectedMenu.NextDevelopmentCost;
        selectedMenu.developmentLevel++;
        OnMenuSelected.Invoke(selectedMenu.menuId);
        return true;
    }

    public float GetMenuPowerBonus(string menuId)
    {
        AssemblyMenu menu = GetMenu(menuId);
        return menu != null ? menu.PowerBonus : 0f;
    }

    public float GetTotalMenuPowerBonus()
    {
        float totalBonus = 0f;
        foreach (AssemblyMenu menu in menus)
        {
            if (menu != null && menu.unlocked)
            {
                totalBonus += menu.PowerBonus;
            }
        }

        return totalBonus;
    }

    public bool TrySelectWeapon(int index)
    {
        if (!IsMenuUnlocked("weapon") || index < 0 || index >= weaponEnhancements.Count)
        {
            return false;
        }

        selectedWeaponIndex = index;
        SaveWeaponEnhancements();
        OnWeaponSelected.Invoke(SelectedWeaponConfig);
        return true;
    }

    public bool TrySelectWeapon(ProjectileConfig weaponConfig)
    {
        if (weaponConfig == null)
        {
            return false;
        }

        for (int i = 0; i < weaponEnhancements.Count; i++)
        {
            if (weaponEnhancements[i].weaponConfig == weaponConfig)
            {
                return TrySelectWeapon(i);
            }
        }

        return false;
    }

    public bool HasWeaponEnhancement(ProjectileConfig weaponConfig)
    {
        return FindWeaponEnhancement(weaponConfig) != null;
    }

    public JinyouAssemblyFactorySaveData CaptureState()
    {
        JinyouAssemblyFactorySaveData data = new JinyouAssemblyFactorySaveData
        {
            level = level,
            selectedMenuId = selectedMenuId,
            selectedWeaponIndex = selectedWeaponIndex,
            isUpgrading = isUpgrading,
            upgradeRemainingSeconds = upgradeRemainingSeconds,
            currentUpgradeDurationSeconds = currentUpgradeDurationSeconds
        };

        foreach (AssemblyMenu menu in menus)
        {
            if (menu == null)
            {
                continue;
            }

            data.menus.Add(new JinyouMenuSaveData
            {
                menuId = menu.menuId,
                developmentLevel = menu.developmentLevel,
                unlocked = menu.unlocked
            });
        }

        foreach (WeaponEnhancement weaponEnhancement in weaponEnhancements)
        {
            data.weaponEnhanceLevels.Add(weaponEnhancement != null ? weaponEnhancement.enhanceLevel : 0);
        }

        return data;
    }

    public void RestoreState(JinyouAssemblyFactorySaveData data)
    {
        if (data == null)
        {
            return;
        }

        level = Mathf.Clamp(data.level, 1, maxLevel);
        selectedMenuId = data.selectedMenuId;
        selectedWeaponIndex = data.selectedWeaponIndex;
        isUpgrading = data.isUpgrading;
        upgradeRemainingSeconds = Mathf.Max(0f, data.upgradeRemainingSeconds);
        currentUpgradeDurationSeconds = Mathf.Max(0f, data.currentUpgradeDurationSeconds);

        if (data.menus != null)
        {
            foreach (JinyouMenuSaveData menuData in data.menus)
            {
                if (menuData == null)
                {
                    continue;
                }

                AssemblyMenu menu = GetMenu(menuData.menuId);
                if (menu == null)
                {
                    continue;
                }

                menu.developmentLevel = menuData.developmentLevel;
                menu.unlocked = menuData.unlocked;
                menu.Normalize();
            }
        }

        if (data.weaponEnhanceLevels != null)
        {
            int count = Mathf.Min(data.weaponEnhanceLevels.Count, weaponEnhancements.Count);
            for (int i = 0; i < count; i++)
            {
                if (weaponEnhancements[i] != null)
                {
                    weaponEnhancements[i].enhanceLevel = data.weaponEnhanceLevels[i];
                    weaponEnhancements[i].Normalize();
                }
            }
        }

        NormalizeMenus();
        NormalizeWeaponEnhancements();
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
    }

    public bool CanEnhanceSelectedWeapon(int credits)
    {
        WeaponEnhancement selectedWeapon = SelectedWeaponEnhancement;
        return IsMenuUnlocked("weapon")
            && selectedWeapon != null
            && !selectedWeapon.IsMaxLevel
            && credits >= selectedWeapon.NextEnhanceCost;
    }

    public bool TryEnhanceSelectedWeapon(ref int availableCredits)
    {
        if (!CanEnhanceSelectedWeapon(availableCredits))
        {
            return false;
        }

        availableCredits -= SelectedWeaponEnhancement.NextEnhanceCost;
        EnhanceSelectedWeaponAttackDamage();
        return true;
    }

    public void EnhanceSelectedWeaponAttackDamage()
    {
        WeaponEnhancement selectedWeapon = SelectedWeaponEnhancement;
        if (selectedWeapon == null || selectedWeapon.IsMaxLevel)
        {
            return;
        }

        selectedWeapon.enhanceLevel++;
        SaveWeaponEnhancements();
        OnWeaponEnhanced.Invoke(selectedWeapon.weaponConfig, selectedWeapon.enhanceLevel);
    }

    public float GetWeaponStatBonus(ProjectileConfig weaponConfig, WeaponEnhancementStat stat)
    {
        WeaponEnhancement weaponEnhancement = FindWeaponEnhancement(weaponConfig);
        return weaponEnhancement != null ? weaponEnhancement.GetStatBonus(stat) : 0f;
    }

    public static string GetStatDisplayName(WeaponEnhancementStat stat)
    {
        return stat switch
        {
            WeaponEnhancementStat.AttackDamage => "Attack",
            WeaponEnhancementStat.Speed => "Speed",
            WeaponEnhancementStat.Lifetime => "Lifetime",
            WeaponEnhancementStat.CollisionRadius => "Collision",
            WeaponEnhancementStat.KnockbackForce => "Knockback",
            _ => stat.ToString()
        };
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= UpgradeCost && commanderLevel >= requiredCommanderLevel;
    }

    public int GetLevelLimit(int researchLabLevel)
    {
        return Mathf.Min(maxLevel, Mathf.Max(1, researchLabLevel) + 2);
    }

    public bool CanStartUpgrade(int availableCredits, int commanderLevel, int researchLabLevel)
    {
        return CanUpgrade(availableCredits, commanderLevel)
            && researchLabLevel >= RequiredResearchLabLevel
            && level < GetLevelLimit(researchLabLevel);
    }

    public bool TryStartUpgrade(ref int availableCredits, int commanderLevel, int researchLabLevel)
    {
        if (!CanStartUpgrade(availableCredits, commanderLevel, researchLabLevel))
        {
            return false;
        }

        availableCredits -= UpgradeCost;
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

        currentUpgradeDurationSeconds = GetUpgradeDurationForCurrentLevel();

        if (currentUpgradeDurationSeconds <= 0f)
        {
            CompleteUpgrade();
            return;
        }

        isUpgrading = true;
        upgradeRemainingSeconds = currentUpgradeDurationSeconds;
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
        currentUpgradeDurationSeconds = 0f;
        level++;
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private void RefreshUnlocks()
    {
        foreach (AssemblyMenu menu in menus)
        {
            menu?.Normalize();
            if (menu == null)
            {
                continue;
            }

            if (!menu.unlocked && level >= menu.requiredFactoryLevel)
            {
                menu.unlocked = true;
                OnMenuUnlocked.Invoke(menu.menuId);
            }
        }
    }

    private AssemblyMenu GetMenu(string menuId)
    {
        if (string.IsNullOrWhiteSpace(menuId) || menus == null)
        {
            return null;
        }

        return menus.Find(item => item != null && string.Equals(item.menuId, menuId, StringComparison.OrdinalIgnoreCase));
    }

    private float GetUpgradeDurationForCurrentLevel()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < upgradeDurationSecondsByLevel.Count)
        {
            return Mathf.Max(0f, upgradeDurationSecondsByLevel[index]);
        }

        return upgradeDurationSeconds;
    }

    private int GetUpgradeCostForCurrentLevel()
    {
        if (level >= maxLevel)
        {
            return 0;
        }

        int index = Mathf.Max(0, level - 1);
        if (index < upgradeCostByLevel.Count)
        {
            return Mathf.Max(0, upgradeCostByLevel[index]);
        }

        return Mathf.Max(0, upgradeCost);
    }

    private int GetRequiredResearchLabLevelForCurrentUpgrade()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < requiredResearchLabLevelByLevel.Count)
        {
            return Mathf.Max(1, requiredResearchLabLevelByLevel[index]);
        }

        return Mathf.Max(1, requiredResearchLabLevel);
    }

    private int GetRequiredCommanderLevelForCurrentUpgrade()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < requiredCommanderLevelByLevel.Count)
        {
            return Mathf.Max(1, requiredCommanderLevelByLevel[index]);
        }

        return Mathf.Max(1, requiredCommanderLevel);
    }

    private void NormalizeUpgradeDurations()
    {
        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (upgradeDurationSecondsByLevel.Count < targetCount)
        {
            upgradeDurationSecondsByLevel.Add(upgradeDurationSeconds);
        }

        for (int i = 0; i < upgradeDurationSecondsByLevel.Count; i++)
        {
            upgradeDurationSecondsByLevel[i] = Mathf.Max(0f, upgradeDurationSecondsByLevel[i]);
        }
    }

    private void NormalizeResearchLabRequirements()
    {
        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (requiredResearchLabLevelByLevel.Count < targetCount)
        {
            requiredResearchLabLevelByLevel.Add(requiredResearchLabLevel);
        }

        for (int i = 0; i < requiredResearchLabLevelByLevel.Count; i++)
        {
            requiredResearchLabLevelByLevel[i] = Mathf.Max(1, requiredResearchLabLevelByLevel[i]);
        }
    }

    private void NormalizeUpgradeCosts()
    {
        if (upgradeCostByLevel == null)
        {
            upgradeCostByLevel = new List<int>();
        }

        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (upgradeCostByLevel.Count < targetCount)
        {
            upgradeCostByLevel.Add(upgradeCost);
        }

        for (int i = 0; i < upgradeCostByLevel.Count; i++)
        {
            upgradeCostByLevel[i] = Mathf.Max(0, upgradeCostByLevel[i]);
        }
    }

    private void NormalizeCommanderRequirements()
    {
        if (requiredCommanderLevelByLevel == null)
        {
            requiredCommanderLevelByLevel = new List<int>();
        }

        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (requiredCommanderLevelByLevel.Count < targetCount)
        {
            requiredCommanderLevelByLevel.Add(requiredCommanderLevel);
        }

        for (int i = 0; i < requiredCommanderLevelByLevel.Count; i++)
        {
            requiredCommanderLevelByLevel[i] = Mathf.Max(1, requiredCommanderLevelByLevel[i]);
        }
    }

    private void NormalizeMenus()
    {
        menus ??= new List<AssemblyMenu>();
        foreach (AssemblyMenu menu in menus)
        {
            menu?.Normalize();
        }
    }

    private WeaponEnhancement FindWeaponEnhancement(ProjectileConfig weaponConfig)
    {
        if (weaponConfig == null)
        {
            return null;
        }

        return weaponEnhancements.Find(item => item.weaponConfig == weaponConfig);
    }

    private WeaponEnhancement GetWeaponEnhancementAt(int index)
    {
        if (index < 0 || index >= weaponEnhancements.Count)
        {
            return null;
        }

        return weaponEnhancements[index];
    }

    private void NormalizeWeaponEnhancements()
    {
        if (weaponEnhancements == null)
        {
            weaponEnhancements = new List<WeaponEnhancement>();
        }

        foreach (WeaponEnhancement weaponEnhancement in weaponEnhancements)
        {
            weaponEnhancement?.Normalize();
        }

        if (weaponEnhancements.Count == 0)
        {
            selectedWeaponIndex = 0;
            return;
        }

        selectedWeaponIndex = Mathf.Clamp(selectedWeaponIndex, 0, weaponEnhancements.Count - 1);
    }

    private void LoadWeaponEnhancements()
    {
        string json = PlayerPrefs.GetString(WeaponEnhancementSaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            SaveWeaponEnhancements();
            return;
        }

        WeaponEnhancementSaveData saveData = JsonUtility.FromJson<WeaponEnhancementSaveData>(json);
        if (saveData?.weapons != null)
        {
            for (int i = 0; i < saveData.weapons.Count; i++)
            {
                WeaponEnhancementSaveEntry saved = saveData.weapons[i];
                WeaponEnhancement enhancement = weaponEnhancements.Find(
                    item => item?.weaponConfig != null && item.weaponConfig.Id == saved.weaponId);
                if (enhancement == null)
                {
                    continue;
                }

                enhancement.enhanceLevel = Mathf.Clamp(saved.level, 0, enhancement.MaxEnhanceLevel);
            }
        }

        if (!string.IsNullOrWhiteSpace(saveData?.selectedWeaponId))
        {
            int savedIndex = weaponEnhancements.FindIndex(
                item => item?.weaponConfig != null && item.weaponConfig.Id == saveData.selectedWeaponId);
            if (savedIndex >= 0)
            {
                selectedWeaponIndex = savedIndex;
            }
        }
    }

    private void SaveWeaponEnhancements()
    {
        WeaponEnhancementSaveData saveData = new WeaponEnhancementSaveData
        {
            selectedWeaponId = SelectedWeaponConfig != null ? SelectedWeaponConfig.Id : string.Empty
        };

        for (int i = 0; i < weaponEnhancements.Count; i++)
        {
            WeaponEnhancement enhancement = weaponEnhancements[i];
            if (enhancement?.weaponConfig == null)
            {
                continue;
            }

            saveData.weapons.Add(new WeaponEnhancementSaveEntry
            {
                weaponId = enhancement.weaponConfig.Id,
                level = enhancement.enhanceLevel
            });
        }

        PlayerPrefs.SetString(WeaponEnhancementSaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        NormalizeUpgradeCosts();
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        NormalizeCommanderRequirements();
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        NormalizeResearchLabRequirements();
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        NormalizeUpgradeDurations();
        NormalizeMenus();
        NormalizeWeaponEnhancements();
    }
}

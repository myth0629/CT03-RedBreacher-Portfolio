using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyFactory : MonoBehaviour, IBaseCampFacility
{
    private const string FacilityId = "assembly_factory";
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

        public void Normalize()
        {
            requiredFactoryLevel = Mathf.Max(1, requiredFactoryLevel);
        }
    }

    [Serializable]
    public class WeaponStatIncrease
    {
        public WeaponEnhancementStat stat = WeaponEnhancementStat.AttackDamage;
        public float amount = 1f;

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
            new WeaponStatIncrease { stat = WeaponEnhancementStat.AttackDamage, amount = 1f }
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
        [NonSerialized] public int maxEnhanceLevel;
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

        public int MaxEnhanceLevel => enhancementLevels != null
            ? Mathf.Min(maxEnhanceLevel, enhancementLevels.Count)
            : 0;
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

            if (maxEnhanceLevel > 0)
            {
                enhanceLevel = Mathf.Min(enhanceLevel, MaxEnhanceLevel);
            }
        }
    }

    [Serializable]
    public class DroneEnhancement
    {
        public DroneConfig droneConfig;
        [Min(0)] public int enhanceLevel;
        [NonSerialized] public int maxEnhanceLevel;
        [Min(0)] public int costPerEnhancement = 100;
        [Min(0f)] public float attackDamagePerLevel = 2f;

        public string DisplayName => droneConfig != null ? droneConfig.DisplayName : "Unassigned Drone";
        public bool IsMaxLevel => enhanceLevel >= maxEnhanceLevel;
        public float AttackDamageBonus => enhanceLevel * attackDamagePerLevel;

        public void Normalize()
        {
            maxEnhanceLevel = Mathf.Max(0, maxEnhanceLevel);
            enhanceLevel = maxEnhanceLevel > 0
                ? Mathf.Clamp(enhanceLevel, 0, maxEnhanceLevel)
                : Mathf.Max(0, enhanceLevel);
            costPerEnhancement = Mathf.Max(0, costPerEnhancement);
            attackDamagePerLevel = Mathf.Max(0f, attackDamagePerLevel);
        }
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    private int maxLevel = 1;

    [Header("Assembly Menus")]
    [SerializeField] private List<AssemblyMenu> menus = new List<AssemblyMenu>
    {
        new AssemblyMenu { menuId = "weapon", displayName = "Weapon Upgrade", requiredFactoryLevel = 1, unlocked = true },
        new AssemblyMenu { menuId = "drone", displayName = "Drone Enhancement", requiredFactoryLevel = 1, unlocked = true },
        new AssemblyMenu { menuId = "skill", displayName = "Skill Upgrade", requiredFactoryLevel = 2 },
        new AssemblyMenu { menuId = "parts", displayName = "Parts Crafting", requiredFactoryLevel = 3 }
    };

    [Header("Weapon Enhancement")]
    [SerializeField] private List<WeaponEnhancement> weaponEnhancements = new List<WeaponEnhancement>();
    [SerializeField] private int selectedWeaponIndex;
    [SerializeField] private bool saveWeaponEnhancementsToPlayerPrefs = true;
    [SerializeField, Min(0)] private int defaultWeaponEnhanceCost = 100;
    [SerializeField, Min(0f)] private float defaultWeaponAttackIncrease = 1f;

    [Header("Drone Enhancement")]
    [SerializeField] private List<DroneEnhancement> droneEnhancements = new List<DroneEnhancement>();
    [SerializeField] private int selectedDroneIndex;
    [SerializeField, Min(0)] private int defaultDroneEnhanceCost = 100;
    [SerializeField, Min(0f)] private float defaultDroneAttackIncrease = 2f;

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<string> OnMenuUnlocked = new UnityEvent<string>();
    public UnityEvent<string> OnMenuSelected = new UnityEvent<string>();
    public UnityEvent<ProjectileConfig> OnWeaponSelected = new UnityEvent<ProjectileConfig>();
    public UnityEvent<ProjectileConfig, int> OnWeaponEnhanced = new UnityEvent<ProjectileConfig, int>();
    public UnityEvent<DroneConfig> OnDroneSelected = new UnityEvent<DroneConfig>();
    public UnityEvent<DroneConfig, int> OnDroneEnhanced = new UnityEvent<DroneConfig, int>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private string selectedMenuId;
    private bool isUpgrading;
    private bool balanceReady;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;
    private readonly Dictionary<string, int> savedWeaponLevels = new Dictionary<string, int>();
    private readonly Dictionary<string, int> savedDroneLevels = new Dictionary<string, int>();

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
    public IReadOnlyList<DroneEnhancement> DroneEnhancements => droneEnhancements;
    public DroneEnhancement SelectedDroneEnhancement => GetDroneEnhancementAt(selectedDroneIndex);
    public DroneConfig SelectedDroneConfig => SelectedDroneEnhancement?.droneConfig;
    public int SelectedDroneIndex => selectedDroneIndex;
    public int CurrentWeaponEnhanceLevelCap => GetWeaponEnhanceLevelCap();
    public int CurrentDroneEnhanceLevelCap => GetDroneEnhanceLevelCap();

    private void Awake()
    {
        BaseCampBalanceConfig config = BaseCampBalanceConfig.Current;
        string error = "기지 밸런스 설정을 찾을 수 없습니다.";
        if (config != null && config.ValidateFacility(FacilityId, out maxLevel, out error))
        {
            balanceReady = true;
        }
        else
        {
            Debug.LogError($"조립 공장 밸런스 초기화 실패: {error}", this);
        }

        level = Mathf.Clamp(level, 1, maxLevel);
        NormalizeMenus();
        ApplyFactoryEnhanceLevelCaps();
        NormalizeWeaponEnhancements();
        NormalizeDroneEnhancements();
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

        WeaponEnhancement enhancement = FindWeaponEnhancement(weaponConfig) ?? CreateDefaultWeaponEnhancement(weaponConfig);
        selectedWeaponIndex = weaponEnhancements.IndexOf(enhancement);
        selectedMenuId = "weapon";
        SaveWeaponEnhancements();
        OnWeaponSelected.Invoke(weaponConfig);
        return true;
    }

    public bool TrySelectDrone(DroneConfig droneConfig)
    {
        if (!IsMenuUnlocked("drone") || droneConfig == null)
        {
            return false;
        }

        DroneEnhancement enhancement = FindDroneEnhancement(droneConfig) ?? CreateDefaultDroneEnhancement(droneConfig);
        selectedDroneIndex = droneEnhancements.IndexOf(enhancement);
        selectedMenuId = "drone";
        OnDroneSelected.Invoke(droneConfig);
        return true;
    }

    public bool HasWeaponEnhancement(ProjectileConfig weaponConfig)
    {
        return FindWeaponEnhancement(weaponConfig) != null;
    }

    public void SetStandaloneSaveEnabled(bool enabled, bool clearStoredData)
    {
        saveWeaponEnhancementsToPlayerPrefs = enabled;
        if (clearStoredData)
        {
            PlayerPrefs.DeleteKey(WeaponEnhancementSaveKey);
            PlayerPrefs.Save();
        }
    }

    public JinyouAssemblyFactorySaveData CaptureState()
    {
        JinyouAssemblyFactorySaveData data = new JinyouAssemblyFactorySaveData
        {
            level = level,
            selectedMenuId = selectedMenuId,
            selectedWeaponIndex = selectedWeaponIndex,
            selectedWeaponId = SelectedWeaponConfig != null ? SelectedWeaponConfig.Id : string.Empty,
            selectedDroneId = SelectedDroneConfig != null ? SelectedDroneConfig.Id : string.Empty,
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
                unlocked = menu.unlocked
            });
        }

        foreach (WeaponEnhancement weaponEnhancement in weaponEnhancements)
        {
            data.weaponEnhanceLevels.Add(weaponEnhancement != null ? weaponEnhancement.enhanceLevel : 0);
            if (weaponEnhancement?.weaponConfig != null)
            {
                data.weaponEnhancements.Add(new JinyouEnhancementLevelSaveData
                {
                    configId = weaponEnhancement.weaponConfig.Id,
                    level = weaponEnhancement.enhanceLevel
                });
            }
        }

        foreach (DroneEnhancement droneEnhancement in droneEnhancements)
        {
            if (droneEnhancement?.droneConfig == null)
            {
                continue;
            }

            data.droneEnhancements.Add(new JinyouEnhancementLevelSaveData
            {
                configId = droneEnhancement.droneConfig.Id,
                level = droneEnhancement.enhanceLevel
            });
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

        savedWeaponLevels.Clear();
        if (data.weaponEnhancements != null)
        {
            foreach (JinyouEnhancementLevelSaveData saved in data.weaponEnhancements)
            {
                if (saved != null && !string.IsNullOrWhiteSpace(saved.configId))
                {
                    savedWeaponLevels[saved.configId] = Mathf.Max(0, saved.level);
                }
            }

            ApplySavedWeaponLevels();
        }

        savedDroneLevels.Clear();
        if (data.droneEnhancements != null)
        {
            foreach (JinyouEnhancementLevelSaveData saved in data.droneEnhancements)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.configId))
                {
                    continue;
                }

                savedDroneLevels[saved.configId] = Mathf.Max(0, saved.level);
                DroneEnhancement enhancement = droneEnhancements.Find(
                    item => item?.droneConfig != null && item.droneConfig.Id == saved.configId);
                if (enhancement != null)
                {
                    enhancement.enhanceLevel = saved.level;
                    enhancement.Normalize();
                }
            }
        }

        NormalizeMenus();
        NormalizeWeaponEnhancements();
        NormalizeDroneEnhancements();
        ApplyFactoryEnhanceLevelCaps();
        RestoreSelectedEnhancementIds(data.selectedWeaponId, data.selectedDroneId);
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

    public int GetWeaponEnhanceLevel(ProjectileConfig weaponConfig)
    {
        WeaponEnhancement enhancement = FindWeaponEnhancement(weaponConfig);
        if (enhancement != null)
        {
            return enhancement.enhanceLevel;
        }

        return weaponConfig != null && savedWeaponLevels.TryGetValue(weaponConfig.Id, out int savedLevel)
            ? savedLevel
            : 0;
    }

    public bool CanEnhanceSelectedDrone(int credits)
    {
        DroneEnhancement selectedDrone = SelectedDroneEnhancement;
        return selectedDrone != null
            && !selectedDrone.IsMaxLevel
            && credits >= selectedDrone.costPerEnhancement;
    }

    public bool TryEnhanceSelectedDrone(ref int availableCredits)
    {
        if (!CanEnhanceSelectedDrone(availableCredits))
        {
            return false;
        }

        DroneEnhancement selectedDrone = SelectedDroneEnhancement;
        availableCredits -= selectedDrone.costPerEnhancement;
        selectedDrone.enhanceLevel++;
        OnDroneEnhanced.Invoke(selectedDrone.droneConfig, selectedDrone.enhanceLevel);
        return true;
    }

    public float GetDroneAttackDamageBonus(DroneConfig droneConfig)
    {
        DroneEnhancement enhancement = FindDroneEnhancement(droneConfig);
        return enhancement != null ? enhancement.AttackDamageBonus : 0f;
    }

    public int GetDroneEnhanceLevel(DroneConfig droneConfig)
    {
        DroneEnhancement enhancement = FindDroneEnhancement(droneConfig);
        if (enhancement != null)
        {
            return enhancement.enhanceLevel;
        }

        return droneConfig != null && savedDroneLevels.TryGetValue(droneConfig.Id, out int savedLevel)
            ? savedLevel
            : 0;
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
        return balanceReady
            && !isUpgrading
            && level < maxLevel
            && credits >= UpgradeCost
            && commanderLevel >= RequiredCommanderLevel;
    }

    public int GetLevelLimit(int researchLabLevel)
    {
        return maxLevel;
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

    public void AdvanceUpgradeOffline(float elapsedSeconds)
    {
        TickUpgrade(Mathf.Max(0f, elapsedSeconds));
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
        ApplyFactoryEnhanceLevelCaps();
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
        return Mathf.Max(0f, GetCurrentBalance()?.upgradeSeconds ?? 0f);
    }

    private int GetUpgradeCostForCurrentLevel()
    {
        if (level >= maxLevel)
        {
            return 0;
        }

        return Mathf.Max(0, GetCurrentBalance()?.upgradeCost ?? 0);
    }

    private int GetRequiredResearchLabLevelForCurrentUpgrade()
    {
        return GetCurrentBalance()?.requiredCommandCenterLevel ?? int.MaxValue;
    }

    private int GetRequiredCommanderLevelForCurrentUpgrade()
    {
        return GetCurrentBalance()?.requiredCommanderLevel ?? int.MaxValue;
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

    private DroneEnhancement FindDroneEnhancement(DroneConfig droneConfig)
    {
        return droneConfig != null
            ? droneEnhancements.Find(item => item?.droneConfig == droneConfig)
            : null;
    }

    private DroneEnhancement GetDroneEnhancementAt(int index)
    {
        return index >= 0 && index < droneEnhancements.Count ? droneEnhancements[index] : null;
    }

    private WeaponEnhancement CreateDefaultWeaponEnhancement(ProjectileConfig weaponConfig)
    {
        WeaponEnhancement enhancement = new WeaponEnhancement
        {
            weaponConfig = weaponConfig,
            maxEnhanceLevel = GetWeaponEnhanceLevelCap(),
            enhancementLevels = new List<WeaponEnhancementLevel>()
        };

        EnsureWeaponEnhancementLevels(enhancement, GetWeaponEnhanceLevelCap());

        weaponEnhancements.Add(enhancement);
        if (savedWeaponLevels.TryGetValue(weaponConfig.Id, out int savedLevel))
        {
            enhancement.enhanceLevel = Mathf.Clamp(savedLevel, 0, enhancement.MaxEnhanceLevel);
        }
        return enhancement;
    }

    private DroneEnhancement CreateDefaultDroneEnhancement(DroneConfig droneConfig)
    {
        DroneEnhancement enhancement = new DroneEnhancement
        {
            droneConfig = droneConfig,
            maxEnhanceLevel = GetDroneEnhanceLevelCap(),
            costPerEnhancement = defaultDroneEnhanceCost,
            attackDamagePerLevel = defaultDroneAttackIncrease
        };
        enhancement.Normalize();
        if (savedDroneLevels.TryGetValue(droneConfig.Id, out int savedLevel))
        {
            enhancement.enhanceLevel = Mathf.Clamp(savedLevel, 0, enhancement.maxEnhanceLevel);
        }
        droneEnhancements.Add(enhancement);
        return enhancement;
    }

    private void ApplySavedWeaponLevels()
    {
        foreach (WeaponEnhancement enhancement in weaponEnhancements)
        {
            if (enhancement?.weaponConfig != null
                && savedWeaponLevels.TryGetValue(enhancement.weaponConfig.Id, out int savedLevel))
            {
                enhancement.enhanceLevel = Mathf.Clamp(savedLevel, 0, enhancement.MaxEnhanceLevel);
            }
        }
    }

    private void RestoreSelectedEnhancementIds(string weaponId, string droneId)
    {
        if (!string.IsNullOrWhiteSpace(weaponId))
        {
            int weaponIndex = weaponEnhancements.FindIndex(
                item => item?.weaponConfig != null && item.weaponConfig.Id == weaponId);
            if (weaponIndex >= 0)
            {
                selectedWeaponIndex = weaponIndex;
            }
        }

        if (!string.IsNullOrWhiteSpace(droneId))
        {
            int droneIndex = droneEnhancements.FindIndex(
                item => item?.droneConfig != null && item.droneConfig.Id == droneId);
            if (droneIndex >= 0)
            {
                selectedDroneIndex = droneIndex;
            }
        }
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

    private void NormalizeDroneEnhancements()
    {
        droneEnhancements ??= new List<DroneEnhancement>();
        foreach (DroneEnhancement enhancement in droneEnhancements)
        {
            enhancement?.Normalize();
        }

        selectedDroneIndex = droneEnhancements.Count > 0
            ? Mathf.Clamp(selectedDroneIndex, 0, droneEnhancements.Count - 1)
            : 0;
    }

    private void ApplyFactoryEnhanceLevelCaps()
    {
        weaponEnhancements ??= new List<WeaponEnhancement>();
        droneEnhancements ??= new List<DroneEnhancement>();

        int weaponCap = GetWeaponEnhanceLevelCap();
        foreach (WeaponEnhancement enhancement in weaponEnhancements)
        {
            if (enhancement == null)
            {
                continue;
            }

            // 강화 상한은 조립 공장 CSV의 현재 레벨 값만 사용한다.
            enhancement.maxEnhanceLevel = weaponCap;
            EnsureWeaponEnhancementLevels(enhancement, weaponCap);
            enhancement.Normalize();
        }

        int droneCap = GetDroneEnhanceLevelCap();
        foreach (DroneEnhancement enhancement in droneEnhancements)
        {
            if (enhancement == null)
            {
                continue;
            }

            enhancement.maxEnhanceLevel = droneCap;
            enhancement.Normalize();
        }
    }

    private void EnsureWeaponEnhancementLevels(WeaponEnhancement enhancement, int targetCount)
    {
        enhancement.enhancementLevels ??= new List<WeaponEnhancementLevel>();
        while (enhancement.enhancementLevels.Count < targetCount)
        {
            enhancement.enhancementLevels.Add(new WeaponEnhancementLevel
            {
                cost = defaultWeaponEnhanceCost,
                statIncreases = new List<WeaponStatIncrease>
                {
                    new WeaponStatIncrease
                    {
                        stat = WeaponEnhancementStat.AttackDamage,
                        amount = defaultWeaponAttackIncrease
                    }
                }
            });
        }
    }

    private int GetWeaponEnhanceLevelCap()
    {
        return Mathf.Max(0, GetCurrentBalance()?.weaponEnhanceLevelCap ?? 0);
    }

    private int GetDroneEnhanceLevelCap()
    {
        return Mathf.Max(0, GetCurrentBalance()?.droneEnhanceLevelCap ?? 0);
    }

    private BaseCampBalanceConfig.FacilityLevelData GetCurrentBalance()
    {
        return BaseCampBalanceConfig.Current?.GetLevel(FacilityId, level);
    }

    private void LoadWeaponEnhancements()
    {
        if (!saveWeaponEnhancementsToPlayerPrefs)
        {
            return;
        }

        string json = PlayerPrefs.GetString(WeaponEnhancementSaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            SaveWeaponEnhancements();
            return;
        }

        WeaponEnhancementSaveData saveData = JsonUtility.FromJson<WeaponEnhancementSaveData>(json);
        if (saveData?.weapons != null)
        {
            savedWeaponLevels.Clear();
            for (int i = 0; i < saveData.weapons.Count; i++)
            {
                WeaponEnhancementSaveEntry saved = saveData.weapons[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.weaponId))
                {
                    continue;
                }

                savedWeaponLevels[saved.weaponId] = Mathf.Max(0, saved.level);
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
        if (!saveWeaponEnhancementsToPlayerPrefs)
        {
            return;
        }

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
        NormalizeMenus();
        NormalizeWeaponEnhancements();
        defaultWeaponEnhanceCost = Mathf.Max(0, defaultWeaponEnhanceCost);
        defaultWeaponAttackIncrease = Mathf.Max(0f, defaultWeaponAttackIncrease);
        defaultDroneEnhanceCost = Mathf.Max(0, defaultDroneEnhanceCost);
        defaultDroneAttackIncrease = Mathf.Max(0f, defaultDroneAttackIncrease);
        NormalizeDroneEnhancements();
    }
}

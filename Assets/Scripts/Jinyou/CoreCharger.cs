using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CoreCharger : MonoBehaviour, IBaseCampFacility
{
    [Serializable]
    public class CoreRouteTier
    {
        [Tooltip("Tier label shown in the core charger route summary.")]
        public string displayName = "Tier 1";
        [Tooltip("How many stat points can be invested before moving to the next tier.")]
        public int maxPoints = 5;
        [Tooltip("Stat bonus gained for each invested stat point in this tier.")]
        [Min(0f)]
        public float bonusPerPoint = 1f;
    }

    [Serializable]
    public class CoreRouteOption
    {
        [Tooltip("Unique option key used by buttons and save data.")]
        public string optionId;
        [Tooltip("Name shown in the core charger option summary.")]
        public string displayName;
        [Tooltip("Target stat key, such as maxHealth, healthRegen, moveSpeed, attackDamage, attackSpeed, critChance, or critMultiplier.")]
        public string statId;
        [Tooltip("Current invested stat points for this option.")]
        public int investedPoints;
        [Tooltip("Tiered stat growth settings. Tier 1 is used first, then Tier 2, then Tier 3.")]
        public List<CoreRouteTier> tiers = new List<CoreRouteTier>();
    }

    [Serializable]
    public class CoreRoute
    {
        [Tooltip("Unique route key used by buttons and save data.")]
        public string routeId;
        [Tooltip("Name shown in the core charger route summary.")]
        public string displayName;
        [Tooltip("Core Charger level required to unlock this route.")]
        public int requiredChargerLevel = 1;
        [Tooltip("Stat choices inside this route.")]
        public List<CoreRouteOption> options = new List<CoreRouteOption>();
        public bool unlocked;
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 400;
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 3;
    [SerializeField] private List<int> requiredResearchLabLevelByLevel = new List<int>();
    [SerializeField] private float upgradeDurationSeconds = 10f;
    [SerializeField] private List<float> upgradeDurationSecondsByLevel = new List<float>();

    [Header("Core Routes")]
    [SerializeField] private List<CoreRoute> routes = new List<CoreRoute>
    {
        new CoreRoute
        {
            routeId = "health",
            displayName = "Health Route",
            requiredChargerLevel = 1,
            unlocked = true,
            options = new List<CoreRouteOption>
            {
                CreateOption("max_health", "Max Health", "maxHealth", 10f, 20f, 35f),
                CreateOption("health_regen", "Health Regen", "healthRegen", 0.2f, 0.4f, 0.8f),
                CreateOption("move_speed", "Move Speed", "moveSpeed", 0.05f, 0.1f, 0.2f)
            }
        },
        new CoreRoute
        {
            routeId = "attack",
            displayName = "Attack Route",
            requiredChargerLevel = 1,
            unlocked = true,
            options = new List<CoreRouteOption>
            {
                CreateOption("attack_damage", "Attack Damage", "attackDamage", 2f, 4f, 7f),
                CreateOption("attack_speed", "Attack Speed", "attackSpeed", 0.03f, 0.06f, 0.1f)
            }
        },
        new CoreRoute
        {
            routeId = "critical",
            displayName = "Critical Route",
            requiredChargerLevel = 1,
            unlocked = true,
            options = new List<CoreRouteOption>
            {
                CreateOption("crit_chance", "Crit Chance", "critChance", 0.01f, 0.02f, 0.04f),
                CreateOption("crit_multiplier", "Crit Multiplier", "critMultiplier", 0.05f, 0.1f, 0.2f)
            }
        }
    };

    [SerializeField] private PlayerProgression playerProgression;

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<string> OnRouteUnlocked = new UnityEvent<string>();
    public UnityEvent<string> OnRouteSelected = new UnityEvent<string>();
    public UnityEvent<string> OnOptionSelected = new UnityEvent<string>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private string selectedRouteId;
    private string selectedOptionId;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;
    private static readonly string[] DefaultRouteIds = { "health", "attack", "critical" };

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => GetRequiredResearchLabLevelForCurrentUpgrade();
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public string SelectedRouteId => selectedRouteId;
    public string SelectedOptionId => selectedOptionId;
    public IReadOnlyList<CoreRoute> Routes => routes;

    private void Awake()
    {
        NormalizeRouteSet();
    }

    private void Start()
    {
        RefreshUnlocks();
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
    }

    public bool IsRouteUnlocked(string routeId)
    {
        CoreRoute route = routes.Find(item => item.routeId == routeId);
        return route != null && route.unlocked;
    }

    public bool TrySelectRoute(string routeId)
    {
        if (!IsRouteUnlocked(routeId))
        {
            return false;
        }

        selectedRouteId = routeId;
        CoreRoute route = FindRoute(routeId);
        CoreRouteOption option = GetSelectedOptionInRoute(route) ?? GetFirstOption(route);
        selectedOptionId = option != null ? option.optionId : string.Empty;
        OnRouteSelected.Invoke(routeId);
        OnOptionSelected.Invoke(selectedOptionId);
        return true;
    }

    public bool TrySelectOption(string optionId)
    {
        if (!TryFindOption(optionId, out CoreRoute route, out CoreRouteOption option) || !IsRouteUnlocked(route.routeId))
        {
            return false;
        }

        selectedRouteId = route.routeId;
        selectedOptionId = option.optionId;
        OnRouteSelected.Invoke(selectedRouteId);
        OnOptionSelected.Invoke(selectedOptionId);
        return true;
    }

    public bool CanInvestRoute(string routeId)
    {
        CoreRoute route = FindRoute(routeId);
        CoreRouteOption option = GetSelectedOptionInRoute(route) ?? GetFirstOption(route);
        return route != null
            && route.unlocked
            && option != null
            && option.investedPoints < GetOptionMaxPoints(option)
            && ResolvePlayerProgression() != null
            && playerProgression.StatPoints > 0;
    }

    public bool CanInvestOption(string optionId)
    {
        if (!TryFindOption(optionId, out CoreRoute route, out CoreRouteOption option))
        {
            return false;
        }

        return route.unlocked
            && option.investedPoints < GetOptionMaxPoints(option)
            && ResolvePlayerProgression() != null
            && playerProgression.StatPoints > 0;
    }

    public bool TryInvestRoute(string routeId)
    {
        CoreRoute route = FindRoute(routeId);
        CoreRouteOption option = GetSelectedOptionInRoute(route) ?? GetFirstOption(route);
        if (route == null || option == null || !CanInvestRoute(routeId) || !playerProgression.TrySpendStatPoint())
        {
            return false;
        }

        option.investedPoints++;
        selectedRouteId = routeId;
        selectedOptionId = option.optionId;
        Debug.Log($"Core option invested: {route.displayName}/{option.displayName} {GetOptionTierLabel(option)}, {option.statId} +{GetOptionBonus(option):0.##}");
        OnRouteSelected.Invoke(routeId);
        OnOptionSelected.Invoke(selectedOptionId);
        return true;
    }

    public bool TryInvestOption(string optionId)
    {
        if (!TryFindOption(optionId, out CoreRoute route, out CoreRouteOption option)
            || !CanInvestOption(optionId)
            || !playerProgression.TrySpendStatPoint())
        {
            return false;
        }

        option.investedPoints++;
        selectedRouteId = route.routeId;
        selectedOptionId = option.optionId;
        Debug.Log($"Core option invested: {route.displayName}/{option.displayName} {GetOptionTierLabel(option)}, {option.statId} +{GetOptionBonus(option):0.##}");
        OnRouteSelected.Invoke(selectedRouteId);
        OnOptionSelected.Invoke(selectedOptionId);
        return true;
    }

    public float GetStatBonus(string statId)
    {
        float bonus = 0f;
        foreach (CoreRoute route in routes)
        {
            if (route.options == null)
            {
                continue;
            }

            foreach (CoreRouteOption option in route.options)
            {
                if (option != null && option.statId == statId)
                {
                    bonus += GetOptionBonus(option);
                }
            }
        }

        return bonus;
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= upgradeCost && commanderLevel >= requiredCommanderLevel;
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
        upgradeCost = Mathf.RoundToInt(upgradeCost * 1.35f);
        requiredCommanderLevel++;
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private void RefreshUnlocks()
    {
        foreach (CoreRoute route in routes)
        {
            if (!route.unlocked && level >= route.requiredChargerLevel)
            {
                route.unlocked = true;
                OnRouteUnlocked.Invoke(route.routeId);
            }
        }
    }

    private CoreRoute FindRoute(string routeId)
    {
        return routes.Find(item => item.routeId == routeId);
    }

    private CoreRoute FindOrCreateRoute(string routeId)
    {
        CoreRoute route = FindRoute(routeId);
        if (route != null)
        {
            return route;
        }

        route = CreateDefaultRoute(routeId);
        routes.Add(route);
        return route;
    }

    private bool TryFindOption(string optionId, out CoreRoute route, out CoreRouteOption option)
    {
        route = null;
        option = null;

        if (string.IsNullOrEmpty(optionId))
        {
            return false;
        }

        foreach (CoreRoute candidateRoute in routes)
        {
            if (candidateRoute?.options == null)
            {
                continue;
            }

            CoreRouteOption candidateOption = candidateRoute.options.Find(item => item != null && item.optionId == optionId);
            if (candidateOption == null)
            {
                continue;
            }

            route = candidateRoute;
            option = candidateOption;
            return true;
        }

        return false;
    }

    private CoreRouteOption GetSelectedOptionInRoute(CoreRoute route)
    {
        if (route?.options == null || string.IsNullOrEmpty(selectedOptionId))
        {
            return null;
        }

        return route.options.Find(item => item != null && item.optionId == selectedOptionId);
    }

    private CoreRouteOption GetFirstOption(CoreRoute route)
    {
        if (route?.options == null)
        {
            return null;
        }

        return route.options.Find(item => item != null);
    }

    public int GetRouteMaxPoints(CoreRoute route)
    {
        if (route == null || route.options == null)
        {
            return 0;
        }

        int maxPoints = 0;
        foreach (CoreRouteOption option in route.options)
        {
            if (option != null)
            {
                maxPoints += GetOptionMaxPoints(option);
            }
        }

        return maxPoints;
    }

    public int GetOptionMaxPoints(CoreRouteOption option)
    {
        if (option == null || option.tiers == null)
        {
            return 0;
        }

        int maxPoints = 0;
        foreach (CoreRouteTier tier in option.tiers)
        {
            if (tier != null)
            {
                maxPoints += Mathf.Max(1, tier.maxPoints);
            }
        }

        return maxPoints;
    }

    public string GetOptionTierLabel(CoreRouteOption option)
    {
        CoreRouteTier tier = GetCurrentTier(option);
        int tierIndex = GetCurrentTierIndex(option) + 1;
        return tier != null ? $"{tier.displayName} ({tierIndex})" : "Tier --";
    }

    public float GetCurrentOptionTierBonusPerPoint(CoreRouteOption option)
    {
        CoreRouteTier tier = GetCurrentTier(option);
        return tier != null ? tier.bonusPerPoint : 0f;
    }

    public float GetRouteBonus(CoreRoute route)
    {
        if (route == null || route.options == null)
        {
            return 0f;
        }

        float bonus = 0f;
        foreach (CoreRouteOption option in route.options)
        {
            bonus += GetOptionBonus(option);
        }

        return bonus;
    }

    public float GetOptionBonus(CoreRouteOption option)
    {
        if (option == null || option.tiers == null)
        {
            return 0f;
        }

        int remainingPoints = option.investedPoints;
        float bonus = 0f;

        foreach (CoreRouteTier tier in option.tiers)
        {
            if (tier == null || remainingPoints <= 0)
            {
                continue;
            }

            int tierPoints = Mathf.Min(remainingPoints, Mathf.Max(1, tier.maxPoints));
            bonus += tierPoints * Mathf.Max(0f, tier.bonusPerPoint);
            remainingPoints -= tierPoints;
        }

        return bonus;
    }

    private PlayerProgression ResolvePlayerProgression()
    {
        if (playerProgression != null)
        {
            return playerProgression;
        }

        if (BaseCampManager.Instance != null && BaseCampManager.Instance.PlayerProgression != null)
        {
            playerProgression = BaseCampManager.Instance.PlayerProgression;
        }

        playerProgression ??= FindFirstObjectByType<PlayerProgression>();
        return playerProgression;
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

    private int GetRequiredResearchLabLevelForCurrentUpgrade()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < requiredResearchLabLevelByLevel.Count)
        {
            return Mathf.Max(1, requiredResearchLabLevelByLevel[index]);
        }

        return Mathf.Max(1, requiredResearchLabLevel);
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

    private CoreRouteTier GetCurrentTier(CoreRouteOption option)
    {
        if (option == null || option.tiers == null || option.tiers.Count == 0)
        {
            return null;
        }

        return option.tiers[Mathf.Clamp(GetCurrentTierIndex(option), 0, option.tiers.Count - 1)];
    }

    private int GetCurrentTierIndex(CoreRouteOption option)
    {
        if (option == null || option.tiers == null || option.tiers.Count == 0)
        {
            return 0;
        }

        int pointsBeforeTier = 0;
        for (int i = 0; i < option.tiers.Count; i++)
        {
            CoreRouteTier tier = option.tiers[i];
            int tierMaxPoints = tier != null ? Mathf.Max(1, tier.maxPoints) : 1;
            if (option.investedPoints < pointsBeforeTier + tierMaxPoints)
            {
                return i;
            }

            pointsBeforeTier += tierMaxPoints;
        }

        return option.tiers.Count - 1;
    }

    private void NormalizeRoute(CoreRoute route)
    {
        if (route == null)
        {
            return;
        }

        route.requiredChargerLevel = Mathf.Max(1, route.requiredChargerLevel);
        route.options ??= new List<CoreRouteOption>();

        foreach (CoreRouteOption option in route.options)
        {
            NormalizeOption(option);
        }
    }

    private void NormalizeOption(CoreRouteOption option)
    {
        if (option == null)
        {
            return;
        }

        option.tiers ??= new List<CoreRouteTier>();
        while (option.tiers.Count < 3)
        {
            option.tiers.Add(new CoreRouteTier { displayName = $"Tier {option.tiers.Count + 1}" });
        }

        for (int i = 0; i < option.tiers.Count; i++)
        {
            option.tiers[i] ??= new CoreRouteTier();
            if (string.IsNullOrWhiteSpace(option.tiers[i].displayName))
            {
                option.tiers[i].displayName = $"Tier {i + 1}";
            }

            option.tiers[i].maxPoints = Mathf.Max(1, option.tiers[i].maxPoints);
            option.tiers[i].bonusPerPoint = Mathf.Max(0f, option.tiers[i].bonusPerPoint);
        }

        option.investedPoints = Mathf.Clamp(option.investedPoints, 0, GetOptionMaxPoints(option));
    }

    private void NormalizeRouteSet()
    {
        routes ??= new List<CoreRoute>();
        routes.RemoveAll(route => route == null || Array.IndexOf(DefaultRouteIds, route.routeId) < 0);

        foreach (string routeId in DefaultRouteIds)
        {
            CoreRoute route = FindOrCreateRoute(routeId);
            ApplyDefaultRouteIdentity(route, routeId);
            NormalizeRoute(route);
        }
    }

    private CoreRoute CreateDefaultRoute(string routeId)
    {
        CoreRoute route = new CoreRoute();
        ApplyDefaultRouteIdentity(route, routeId);
        return route;
    }

    private void ApplyDefaultRouteIdentity(CoreRoute route, string routeId)
    {
        if (route == null)
        {
            return;
        }

        route.routeId = routeId;
        route.requiredChargerLevel = Mathf.Max(1, route.requiredChargerLevel);
        route.unlocked = route.unlocked || route.requiredChargerLevel <= level;

        switch (routeId)
        {
            case "health":
                route.displayName = string.IsNullOrWhiteSpace(route.displayName) ? "Health Route" : route.displayName;
                EnsureDefaultOption(route, "max_health", "Max Health", "maxHealth", 10f, 20f, 35f);
                EnsureDefaultOption(route, "health_regen", "Health Regen", "healthRegen", 0.2f, 0.4f, 0.8f);
                EnsureDefaultOption(route, "move_speed", "Move Speed", "moveSpeed", 0.05f, 0.1f, 0.2f);
                break;
            case "attack":
                route.displayName = string.IsNullOrWhiteSpace(route.displayName) ? "Attack Route" : route.displayName;
                EnsureDefaultOption(route, "attack_damage", "Attack Damage", "attackDamage", 2f, 4f, 7f);
                EnsureDefaultOption(route, "attack_speed", "Attack Speed", "attackSpeed", 0.03f, 0.06f, 0.1f);
                break;
            case "critical":
                route.displayName = string.IsNullOrWhiteSpace(route.displayName) ? "Critical Route" : route.displayName;
                EnsureDefaultOption(route, "crit_chance", "Crit Chance", "critChance", 0.01f, 0.02f, 0.04f);
                EnsureDefaultOption(route, "crit_multiplier", "Crit Multiplier", "critMultiplier", 0.05f, 0.1f, 0.2f);
                break;
        }
    }

    private void EnsureDefaultOption(
        CoreRoute route,
        string optionId,
        string displayName,
        string statId,
        float tierOneBonus,
        float tierTwoBonus,
        float tierThreeBonus)
    {
        route.options ??= new List<CoreRouteOption>();

        CoreRouteOption option = route.options.Find(item => item != null && item.optionId == optionId);
        if (option == null)
        {
            option = CreateOption(optionId, displayName, statId, tierOneBonus, tierTwoBonus, tierThreeBonus);
            route.options.Add(option);
            return;
        }

        option.displayName = string.IsNullOrWhiteSpace(option.displayName) ? displayName : option.displayName;
        option.statId = string.IsNullOrWhiteSpace(option.statId) ? statId : option.statId;
        EnsureDefaultTiers(option, tierOneBonus, tierTwoBonus, tierThreeBonus);
    }

    private static CoreRouteOption CreateOption(
        string optionId,
        string displayName,
        string statId,
        float tierOneBonus,
        float tierTwoBonus,
        float tierThreeBonus)
    {
        CoreRouteOption option = new CoreRouteOption
        {
            optionId = optionId,
            displayName = displayName,
            statId = statId
        };

        EnsureDefaultTiers(option, tierOneBonus, tierTwoBonus, tierThreeBonus);
        return option;
    }

    private static void EnsureDefaultTiers(CoreRouteOption option, float tierOneBonus, float tierTwoBonus, float tierThreeBonus)
    {
        option.tiers ??= new List<CoreRouteTier>();
        float[] defaultBonuses = { tierOneBonus, tierTwoBonus, tierThreeBonus };

        while (option.tiers.Count < defaultBonuses.Length)
        {
            int tierIndex = option.tiers.Count;
            option.tiers.Add(new CoreRouteTier
            {
                displayName = $"Tier {tierIndex + 1}",
                maxPoints = 5,
                bonusPerPoint = defaultBonuses[tierIndex]
            });
        }
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        NormalizeResearchLabRequirements();
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        NormalizeUpgradeDurations();
        NormalizeRouteSet();
    }
}

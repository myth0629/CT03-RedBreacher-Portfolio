using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CoreCharger : MonoBehaviour, IBaseCampFacility
{
    [Serializable]
    public class CoreRoute
    {
        public string routeId;
        public string displayName;
        public string statId;
        public int requiredChargerLevel = 1;
        public int investedPoints;
        public int maxPoints = 10;
        public float bonusPerPoint = 1f;
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

    [Header("Placeholder Core Routes")]
    [SerializeField] private List<CoreRoute> routes = new List<CoreRoute>
    {
        new CoreRoute { routeId = "health", displayName = "Health Route", statId = "maxHealth", requiredChargerLevel = 1, maxPoints = 10, bonusPerPoint = 10f, unlocked = true },
        new CoreRoute { routeId = "attack", displayName = "Attack Route", statId = "attackDamage", requiredChargerLevel = 1, maxPoints = 10, bonusPerPoint = 2f, unlocked = true },
        new CoreRoute { routeId = "mobility", displayName = "Mobility Route", statId = "moveSpeed", requiredChargerLevel = 2, maxPoints = 5, bonusPerPoint = 0.1f },
        new CoreRoute { routeId = "critical", displayName = "Critical Route", statId = "critChance", requiredChargerLevel = 3, maxPoints = 5, bonusPerPoint = 0.02f }
    };

    [SerializeField] private PlayerProgression playerProgression;

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<string> OnRouteUnlocked = new UnityEvent<string>();
    public UnityEvent<string> OnRouteSelected = new UnityEvent<string>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private string selectedRouteId;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => GetRequiredResearchLabLevelForCurrentUpgrade();
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public string SelectedRouteId => selectedRouteId;
    public IReadOnlyList<CoreRoute> Routes => routes;

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
        OnRouteSelected.Invoke(routeId);
        return true;
    }

    public bool CanInvestRoute(string routeId)
    {
        CoreRoute route = FindRoute(routeId);
        return route != null
            && route.unlocked
            && route.investedPoints < route.maxPoints
            && ResolvePlayerProgression() != null
            && playerProgression.StatPoints > 0;
    }

    public bool TryInvestRoute(string routeId)
    {
        CoreRoute route = FindRoute(routeId);
        if (route == null || !CanInvestRoute(routeId) || !playerProgression.TrySpendStatPoint())
        {
            return false;
        }

        route.investedPoints++;
        selectedRouteId = routeId;
        Debug.Log($"Core route invested: {route.displayName} Lv.{route.investedPoints}, {route.statId} +{GetRouteBonus(route):0.##}");
        OnRouteSelected.Invoke(routeId);
        return true;
    }

    public float GetStatBonus(string statId)
    {
        float bonus = 0f;
        foreach (CoreRoute route in routes)
        {
            if (route.statId == statId)
            {
                bonus += GetRouteBonus(route);
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

    private float GetRouteBonus(CoreRoute route)
    {
        return route != null ? route.investedPoints * route.bonusPerPoint : 0f;
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

        foreach (CoreRoute route in routes)
        {
            if (route == null)
            {
                continue;
            }

            route.requiredChargerLevel = Mathf.Max(1, route.requiredChargerLevel);
            route.maxPoints = Mathf.Max(1, route.maxPoints);
            route.investedPoints = Mathf.Clamp(route.investedPoints, 0, route.maxPoints);
            route.bonusPerPoint = Mathf.Max(0f, route.bonusPerPoint);
        }
    }
}

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
        public int requiredChargerLevel = 1;
        public bool unlocked;
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 400;
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 2;
    [SerializeField] private float upgradeDurationSeconds = 10f;

    [Header("Placeholder Core Routes")]
    [SerializeField] private List<CoreRoute> routes = new List<CoreRoute>
    {
        new CoreRoute { routeId = "armor", displayName = "Armor Route", requiredChargerLevel = 1, unlocked = true },
        new CoreRoute { routeId = "shield", displayName = "Shield Route", requiredChargerLevel = 1, unlocked = true },
        new CoreRoute { routeId = "survival", displayName = "Survival Route", requiredChargerLevel = 2 }
    };

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<string> OnRouteUnlocked = new UnityEvent<string>();
    public UnityEvent<string> OnRouteSelected = new UnityEvent<string>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private string selectedRouteId;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;

    public int Level => level;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => requiredResearchLabLevel;
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
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
        foreach (CoreRoute route in routes)
        {
            if (!route.unlocked && level >= route.requiredChargerLevel)
            {
                route.unlocked = true;
                OnRouteUnlocked.Invoke(route.routeId);
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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StrategyResearchLab : MonoBehaviour, IBaseCampFacility
{
    [Serializable]
    public class FacilityUnlock
    {
        public string facilityId;
        public string displayName;
        public int requiredLabLevel = 1;
        public bool unlocked;
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 500;
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private float upgradeDurationSeconds = 10f;

    [Header("Convenience Bonus")]
    [SerializeField] private float offlineRewardLimitHours = 2f;
    [SerializeField] private float bossTicketChargeSeconds = 1800f;
    [SerializeField] private int bossTicketCapacity = 3;
    [SerializeField] private int bossTickets = 1;

    [Header("Facility Unlocks")]
    [SerializeField] private List<FacilityUnlock> facilityUnlocks = new List<FacilityUnlock>
    {
        new FacilityUnlock { facilityId = "assembly_factory", displayName = "Assembly Factory", requiredLabLevel = 1, unlocked = true },
        new FacilityUnlock { facilityId = "energy_refinery", displayName = "Energy Refinery", requiredLabLevel = 1, unlocked = true },
        new FacilityUnlock { facilityId = "core_charger", displayName = "Core Charger", requiredLabLevel = 2 }
    };

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<int> OnBossTicketsChanged = new UnityEvent<int>();
    public UnityEvent<string> OnFacilityUnlocked = new UnityEvent<string>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private bool isUpgrading;
    private float upgradeRemainingSeconds;

    public int Level => level;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => 1;
    public float OfflineRewardLimitHours => offlineRewardLimitHours;
    public float BossTicketChargeSeconds => bossTicketChargeSeconds;
    public int BossTicketCapacity => bossTicketCapacity;
    public int BossTickets => bossTickets;
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public IReadOnlyList<FacilityUnlock> FacilityUnlocks => facilityUnlocks;

    private void Start()
    {
        RefreshUnlocks();
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
    }

    public bool IsFacilityUnlocked(string facilityId)
    {
        FacilityUnlock facility = facilityUnlocks.Find(item => item.facilityId == facilityId);
        return facility != null && facility.unlocked;
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= upgradeCost && commanderLevel >= requiredCommanderLevel;
    }

    public bool CanStartUpgrade(int availableCredits, int commanderLevel)
    {
        return CanUpgrade(availableCredits, commanderLevel);
    }

    public bool CanStartUpgrade(int availableCredits, int commanderLevel, int researchLabLevel)
    {
        return CanStartUpgrade(availableCredits, commanderLevel);
    }

    public bool TryStartUpgrade(ref int availableCredits, int commanderLevel)
    {
        return TryStartUpgrade(ref availableCredits, commanderLevel, Level);
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

    public bool TryUseBossTicket()
    {
        if (bossTickets <= 0)
        {
            return false;
        }

        bossTickets--;
        OnBossTicketsChanged.Invoke(bossTickets);
        return true;
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
        upgradeCost = Mathf.RoundToInt(upgradeCost * 1.4f);
        requiredCommanderLevel++;
        bossTicketCapacity = Mathf.Max(bossTicketCapacity, level + 2);
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private void RefreshUnlocks()
    {
        foreach (FacilityUnlock facility in facilityUnlocks)
        {
            if (!facility.unlocked && level >= facility.requiredLabLevel)
            {
                facility.unlocked = true;
                OnFacilityUnlocked.Invoke(facility.facilityId);
            }
        }
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        bossTicketCapacity = Mathf.Max(0, bossTicketCapacity);
        bossTickets = Mathf.Clamp(bossTickets, 0, bossTicketCapacity);
    }
}

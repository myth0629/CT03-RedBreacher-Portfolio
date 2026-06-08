using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CommandCenter : MonoBehaviour, IBaseCampFacility
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
    [SerializeField] private List<int> requiredCommanderLevelByLevel = new List<int>();
    [SerializeField] private float upgradeDurationSeconds = 10f;
    [SerializeField] private List<float> upgradeDurationSecondsByLevel = new List<float>();

    [Header("Convenience Bonus")]
    [SerializeField] private float offlineRewardLimitHours = 2f;
    [SerializeField] private float bossTicketProductionDaySeconds = 86400f;
    [SerializeField] private int bossTicketsProducedPerDay = 2;
    [SerializeField] private int bossTicketCapacity = 3;
    [SerializeField] private int bossTickets = 1;

    [Header("Facility Unlocks")]
    [SerializeField] private List<FacilityUnlock> facilityUnlocks = new List<FacilityUnlock>
    {
        new FacilityUnlock { facilityId = "energy_refinery", displayName = "에너지 정제소", requiredLabLevel = 1, unlocked = true },
        new FacilityUnlock { facilityId = "assembly_factory", displayName = "조립 공장", requiredLabLevel = 2 },
        new FacilityUnlock { facilityId = "core_charger", displayName = "코어 충전소", requiredLabLevel = 3 },
        new FacilityUnlock { facilityId = "trait_point_facility", displayName = "스텟 강화소", requiredLabLevel = 1, unlocked = true },
        new FacilityUnlock { facilityId = "boss_dungeon", displayName = "관제탑", requiredLabLevel = 1, unlocked = true }
    };

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<int> OnBossTicketsChanged = new UnityEvent<int>();
    public UnityEvent<string> OnFacilityUnlocked = new UnityEvent<string>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private bool isUpgrading;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;
    private float bossTicketProductionSeconds;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => GetRequiredCommanderLevelForCurrentUpgrade();
    public int RequiredResearchLabLevel => 1;
    public float OfflineRewardLimitHours => offlineRewardLimitHours;
    public float BossTicketChargeSeconds => bossTicketProductionDaySeconds / Mathf.Max(1, bossTicketsProducedPerDay);
    public float BossTicketProductionDaySeconds => bossTicketProductionDaySeconds;
    public int BossTicketsProducedPerDay => bossTicketsProducedPerDay;
    public float BossTicketProductionProgress => BossTicketChargeSeconds > 0f
        ? Mathf.Clamp01(bossTicketProductionSeconds / BossTicketChargeSeconds)
        : 1f;
    public int BossTicketCapacity => bossTicketCapacity;
    public int BossTickets => bossTickets;
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public IReadOnlyList<FacilityUnlock> FacilityUnlocks => facilityUnlocks;

    private void Start()
    {
        RefreshUnlocks();
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
        TickBossTicketProduction(Time.deltaTime);
    }

    public bool IsFacilityUnlocked(string facilityId)
    {
        NormalizeFacilityUnlocks();

        FacilityUnlock facility = facilityUnlocks.Find(item => item.facilityId == facilityId);
        if (facility == null)
        {
            return false;
        }

        facility.unlocked = level >= facility.requiredLabLevel;
        return facility.unlocked;
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= upgradeCost && commanderLevel >= RequiredCommanderLevel;
    }

    public int GetLevelLimit(int researchLabLevel)
    {
        return maxLevel;
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

    public int ProduceBossTicketsOffline(float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f || bossTickets >= bossTicketCapacity)
        {
            return 0;
        }

        int beforeTickets = bossTickets;
        float chargeSeconds = BossTicketChargeSeconds;
        if (chargeSeconds <= 0f)
        {
            bossTickets = bossTicketCapacity;
            bossTicketProductionSeconds = 0f;
        }
        else
        {
            bossTicketProductionSeconds += elapsedSeconds;
            while (bossTicketProductionSeconds >= chargeSeconds && bossTickets < bossTicketCapacity)
            {
                bossTicketProductionSeconds -= chargeSeconds;
                bossTickets++;
            }

            if (bossTickets >= bossTicketCapacity)
            {
                bossTicketProductionSeconds = 0f;
            }
        }

        int addedTickets = bossTickets - beforeTickets;
        if (addedTickets > 0)
        {
            OnBossTicketsChanged.Invoke(bossTickets);
        }

        return addedTickets;
    }

    public JinyouCommandCenterSaveData CaptureState()
    {
        return new JinyouCommandCenterSaveData
        {
            level = level,
            upgradeCost = upgradeCost,
            bossTickets = bossTickets,
            bossTicketCapacity = bossTicketCapacity,
            bossTicketProductionSeconds = bossTicketProductionSeconds,
            isUpgrading = isUpgrading,
            upgradeRemainingSeconds = upgradeRemainingSeconds,
            currentUpgradeDurationSeconds = currentUpgradeDurationSeconds
        };
    }

    public void RestoreState(JinyouCommandCenterSaveData data)
    {
        if (data == null)
        {
            return;
        }

        level = Mathf.Clamp(data.level, 1, maxLevel);
        upgradeCost = Mathf.Max(0, data.upgradeCost);
        bossTicketCapacity = Mathf.Max(0, data.bossTicketCapacity);
        bossTickets = Mathf.Clamp(data.bossTickets, 0, bossTicketCapacity);
        bossTicketProductionSeconds = Mathf.Max(0f, data.bossTicketProductionSeconds);
        isUpgrading = data.isUpgrading;
        upgradeRemainingSeconds = Mathf.Max(0f, data.upgradeRemainingSeconds);
        currentUpgradeDurationSeconds = Mathf.Max(0f, data.currentUpgradeDurationSeconds);
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
        OnBossTicketsChanged.Invoke(bossTickets);
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

    private void TickBossTicketProduction(float deltaTime)
    {
        if (bossTickets >= bossTicketCapacity)
        {
            bossTicketProductionSeconds = 0f;
            return;
        }

        float chargeSeconds = BossTicketChargeSeconds;
        if (chargeSeconds <= 0f)
        {
            AddBossTicket();
            return;
        }

        bossTicketProductionSeconds += deltaTime;

        while (bossTicketProductionSeconds >= chargeSeconds && bossTickets < bossTicketCapacity)
        {
            bossTicketProductionSeconds -= chargeSeconds;
            AddBossTicket();
        }
    }

    private void AddBossTicket()
    {
        int nextTickets = Mathf.Clamp(bossTickets + 1, 0, bossTicketCapacity);
        if (nextTickets == bossTickets)
        {
            return;
        }

        bossTickets = nextTickets;
        OnBossTicketsChanged.Invoke(bossTickets);
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
        upgradeCost = Mathf.RoundToInt(upgradeCost * 1.4f);
        bossTicketCapacity = Mathf.Max(bossTicketCapacity, level + 2);
        RefreshUnlocks();
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private void RefreshUnlocks()
    {
        NormalizeFacilityUnlocks();

        foreach (FacilityUnlock facility in facilityUnlocks)
        {
            bool shouldBeUnlocked = level >= facility.requiredLabLevel;

            if (!facility.unlocked && shouldBeUnlocked)
            {
                facility.unlocked = true;
                OnFacilityUnlocked.Invoke(facility.facilityId);
            }
            else if (facility.unlocked && !shouldBeUnlocked)
            {
                facility.unlocked = false;
            }
        }
    }

    private void NormalizeFacilityUnlocks()
    {
        SetFacilityUnlockRequirement("energy_refinery", "에너지 정제소", 1);
        SetFacilityUnlockRequirement("assembly_factory", "조립 공장", 2);
        SetFacilityUnlockRequirement("core_charger", "코어 충전소", 3);
        SetFacilityUnlockRequirement("trait_point_facility", "스텟 강화소", 1);
        SetFacilityUnlockRequirement("boss_dungeon", "관제탑", 1);
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

    private void NormalizeCommanderLevelRequirements()
    {
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

    private void SetFacilityUnlockRequirement(string facilityId, string displayName, int requiredLabLevel)
    {
        FacilityUnlock facility = facilityUnlocks.Find(item => item.facilityId == facilityId);
        if (facility == null)
        {
            facility = new FacilityUnlock { facilityId = facilityId };
            facilityUnlocks.Add(facility);
        }

        facility.displayName = displayName;
        facility.requiredLabLevel = requiredLabLevel;
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        NormalizeCommanderLevelRequirements();
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        NormalizeUpgradeDurations();
        bossTicketProductionDaySeconds = Mathf.Max(1f, bossTicketProductionDaySeconds);
        bossTicketsProducedPerDay = Mathf.Max(1, bossTicketsProducedPerDay);
        bossTicketCapacity = Mathf.Max(0, bossTicketCapacity);
        bossTickets = Mathf.Clamp(bossTickets, 0, bossTicketCapacity);
        NormalizeFacilityUnlocks();
    }
}

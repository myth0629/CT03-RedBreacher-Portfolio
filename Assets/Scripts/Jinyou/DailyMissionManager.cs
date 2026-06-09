using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum DailyMissionType
{
    CollectCredits,
    UpgradeFacility,
    EnhanceWeapon,
    EnhanceUnit,
    UseBossTicket,
    DrawWeaponGacha,
    ClaimOfflineReward
}

[DisallowMultipleComponent]
public class DailyMissionManager : MonoBehaviour
{
    [Serializable]
    public class DailyMissionEntry
    {
        [SerializeField] private string id;
        [SerializeField] private DailyMissionType missionType;
        [SerializeField] private string title;
        [TextArea]
        [SerializeField] private string description;
        [Min(1)]
        [SerializeField] private int targetAmount = 1;
        [SerializeField] private CurrencyType rewardCurrency = CurrencyType.Credits;
        [Min(0)]
        [SerializeField] private int rewardAmount;
        [SerializeField] private int currentAmount;
        [SerializeField] private bool rewardClaimed;

        public string Id => id;
        public DailyMissionType MissionType => missionType;
        public string Title => title;
        public string Description => description;
        public int TargetAmount => Mathf.Max(1, targetAmount);
        public CurrencyType RewardCurrency => rewardCurrency;
        public int RewardAmount => Mathf.Max(0, rewardAmount);
        public int CurrentAmount => Mathf.Max(0, currentAmount);
        public bool IsCompleted => CurrentAmount >= TargetAmount;
        public bool RewardClaimed => rewardClaimed;
        public float Progress01 => Mathf.Clamp01((float)CurrentAmount / TargetAmount);

        public DailyMissionEntry(
            string id,
            DailyMissionType missionType,
            string title,
            string description,
            int targetAmount,
            CurrencyType rewardCurrency,
            int rewardAmount)
        {
            this.id = id;
            this.missionType = missionType;
            this.title = title;
            this.description = description;
            this.targetAmount = Mathf.Max(1, targetAmount);
            this.rewardCurrency = rewardCurrency;
            this.rewardAmount = Mathf.Max(0, rewardAmount);
        }

        public bool AddProgress(int amount)
        {
            if (rewardClaimed)
            {
                return false;
            }

            int previousAmount = currentAmount;
            currentAmount = Mathf.Clamp(currentAmount + Mathf.Max(0, amount), 0, TargetAmount);
            return currentAmount != previousAmount;
        }

        public bool TryClaim(PlayerCurrencyWallet wallet)
        {
            if (!IsCompleted || rewardClaimed)
            {
                return false;
            }

            rewardClaimed = true;
            if (wallet != null && RewardAmount > 0)
            {
                wallet.Add(rewardCurrency, RewardAmount);
            }

            return true;
        }

        public void Restore(int amount, bool claimed)
        {
            currentAmount = Mathf.Clamp(amount, 0, TargetAmount);
            rewardClaimed = claimed;
        }

        public void ResetProgress()
        {
            currentAmount = 0;
            rewardClaimed = false;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = missionType.ToString();
            }

            targetAmount = Mathf.Max(1, targetAmount);
            rewardAmount = Mathf.Max(0, rewardAmount);
            currentAmount = Mathf.Clamp(currentAmount, 0, targetAmount);
        }
    }

    public static DailyMissionManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool resetByLocalDate = true;

    [Header("Missions")]
    [SerializeField] private List<DailyMissionEntry> missions = new List<DailyMissionEntry>
    {
        new DailyMissionEntry("collect_credits", DailyMissionType.CollectCredits, "Credit Recovery", "Collect 1000 refinery credits", 1000, CurrencyType.Credits, 200),
        new DailyMissionEntry("upgrade_facility", DailyMissionType.UpgradeFacility, "Base Expansion", "Start 1 facility upgrade", 1, CurrencyType.Credits, 300),
        new DailyMissionEntry("enhance_weapon", DailyMissionType.EnhanceWeapon, "Weapon Tuning", "Enhance a weapon 1 time", 1, CurrencyType.Credits, 200),
        new DailyMissionEntry("enhance_unit", DailyMissionType.EnhanceUnit, "Core Calibration", "Enhance a unit 1 time", 1, CurrencyType.Credits, 200),
        new DailyMissionEntry("use_boss_ticket", DailyMissionType.UseBossTicket, "Boss Recon", "Use 1 boss ticket", 1, CurrencyType.CoreCrystals, 5),
        new DailyMissionEntry("draw_weapon", DailyMissionType.DrawWeaponGacha, "Supply Draw", "Draw weapons 1 time", 1, CurrencyType.Credits, 150),
        new DailyMissionEntry("claim_offline", DailyMissionType.ClaimOfflineReward, "Return Bonus", "Claim offline rewards 1 time", 1, CurrencyType.Credits, 150)
    };

    [Header("Events")]
    public UnityEvent OnDailyMissionsChanged = new UnityEvent();
    public UnityEvent<DailyMissionEntry> OnDailyMissionCompleted = new UnityEvent<DailyMissionEntry>();
    public UnityEvent<DailyMissionEntry> OnDailyMissionRewardClaimed = new UnityEvent<DailyMissionEntry>();

    private string dateKey;

    public IReadOnlyList<DailyMissionEntry> Missions => missions;
    public string DateKey => dateKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateMissions();
        ResetForDateIfNeeded(GetCurrentDateKey());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void ReportCreditsCollected(int amount)
    {
        Instance?.AddProgress(DailyMissionType.CollectCredits, amount);
    }

    public static void ReportFacilityUpgraded(int amount = 1)
    {
        Instance?.AddProgress(DailyMissionType.UpgradeFacility, amount);
    }

    public static void ReportWeaponEnhanced(int amount = 1)
    {
        Instance?.AddProgress(DailyMissionType.EnhanceWeapon, amount);
    }

    public static void ReportUnitEnhanced(int amount = 1)
    {
        Instance?.AddProgress(DailyMissionType.EnhanceUnit, amount);
    }

    public static void ReportBossTicketUsed(int amount = 1)
    {
        Instance?.AddProgress(DailyMissionType.UseBossTicket, amount);
    }

    public static void ReportWeaponGachaDrawn(int amount = 1)
    {
        Instance?.AddProgress(DailyMissionType.DrawWeaponGacha, amount);
    }

    public static void ReportOfflineRewardClaimed(int amount = 1)
    {
        Instance?.AddProgress(DailyMissionType.ClaimOfflineReward, amount);
    }

    public void AddProgress(DailyMissionType missionType, int amount)
    {
        ResetForDateIfNeeded(GetCurrentDateKey());

        bool changed = false;
        foreach (DailyMissionEntry mission in missions)
        {
            if (mission == null || mission.MissionType != missionType)
            {
                continue;
            }

            bool wasCompleted = mission.IsCompleted;
            changed = mission.AddProgress(amount) || changed;
            if (!wasCompleted && mission.IsCompleted)
            {
                OnDailyMissionCompleted.Invoke(mission);
            }
        }

        if (changed)
        {
            OnDailyMissionsChanged.Invoke();
        }
    }

    public bool TryClaimReward(string missionId)
    {
        ResetForDateIfNeeded(GetCurrentDateKey());

        DailyMissionEntry mission = missions.Find(item => item != null && item.Id == missionId);
        if (mission == null || !mission.TryClaim(ResolveCurrencyWallet()))
        {
            return false;
        }

        OnDailyMissionRewardClaimed.Invoke(mission);
        OnDailyMissionsChanged.Invoke();
        return true;
    }

    [ContextMenu("Claim All Daily Mission Rewards")]
    public void ClaimAllRewards()
    {
        foreach (DailyMissionEntry mission in missions)
        {
            if (mission != null && mission.TryClaim(ResolveCurrencyWallet()))
            {
                OnDailyMissionRewardClaimed.Invoke(mission);
            }
        }

        OnDailyMissionsChanged.Invoke();
    }

    public JinyouDailyMissionSaveData CaptureState()
    {
        ResetForDateIfNeeded(GetCurrentDateKey());

        JinyouDailyMissionSaveData data = new JinyouDailyMissionSaveData
        {
            dateKey = dateKey
        };

        foreach (DailyMissionEntry mission in missions)
        {
            if (mission == null)
            {
                continue;
            }

            data.entries.Add(new JinyouDailyMissionEntrySaveData
            {
                id = mission.Id,
                currentAmount = mission.CurrentAmount,
                rewardClaimed = mission.RewardClaimed
            });
        }

        return data;
    }

    public void RestoreState(JinyouDailyMissionSaveData data)
    {
        ValidateMissions();

        string currentDateKey = GetCurrentDateKey();
        if (data == null || string.IsNullOrWhiteSpace(data.dateKey) || data.dateKey != currentDateKey)
        {
            ResetForDateIfNeeded(currentDateKey, true);
            OnDailyMissionsChanged.Invoke();
            return;
        }

        dateKey = data.dateKey;
        if (data.entries != null)
        {
            foreach (JinyouDailyMissionEntrySaveData entryData in data.entries)
            {
                if (entryData == null)
                {
                    continue;
                }

                DailyMissionEntry mission = missions.Find(item => item != null && item.Id == entryData.id);
                mission?.Restore(entryData.currentAmount, entryData.rewardClaimed);
            }
        }

        OnDailyMissionsChanged.Invoke();
    }

    [ContextMenu("Reset Daily Missions")]
    public void ResetToday()
    {
        ResetForDateIfNeeded(GetCurrentDateKey(), true);
        OnDailyMissionsChanged.Invoke();
    }

    private void ResetForDateIfNeeded(string currentDateKey, bool force = false)
    {
        if (!force && dateKey == currentDateKey)
        {
            return;
        }

        dateKey = currentDateKey;
        foreach (DailyMissionEntry mission in missions)
        {
            mission?.ResetProgress();
        }
    }

    private string GetCurrentDateKey()
    {
        DateTime now = resetByLocalDate ? DateTime.Now : DateTime.UtcNow;
        return now.ToString("yyyyMMdd");
    }

    private PlayerCurrencyWallet ResolveCurrencyWallet()
    {
        if (BaseCampManager.Instance != null)
        {
            return BaseCampManager.Instance.CurrencyWallet;
        }

        return FindFirstObjectByType<PlayerCurrencyWallet>();
    }

    private void ValidateMissions()
    {
        missions ??= new List<DailyMissionEntry>();
        foreach (DailyMissionEntry mission in missions)
        {
            mission?.Validate();
        }
    }

    private void OnValidate()
    {
        ValidateMissions();
    }
}

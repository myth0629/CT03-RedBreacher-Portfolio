using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum AchievementProgressType
{
    PlayerLevel,
    EnemyKill,
    StageClear,
    WeaponCollect,
    DroneCollect
}

[DisallowMultipleComponent]
public class AchievementManager : MonoBehaviour
{
    [Serializable]
    public class AchievementEntry
    {
        [SerializeField] private string id;
        [SerializeField] private AchievementProgressType progressType;
        [SerializeField] private string title;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite iconSprite;
        [Min(1)]
        [SerializeField] private int targetAmount = 1;
        [SerializeField] private List<int> nextTargetAmounts = new List<int>();
        [Min(1)]
        [SerializeField] private int repeatRequirementAmount = 1;
        [Min(1)]
        [SerializeField] private int progressAmountPerEvent = 1;
        [SerializeField] private CurrencyType rewardCurrency = CurrencyType.Credits;
        [Min(0)]
        [SerializeField] private int rewardAmount;
        [SerializeField] private int currentAmount;
        [SerializeField] private bool completed;
        [SerializeField] private int completedCount;

        public string Id => id;
        public AchievementProgressType ProgressType => progressType;
        public string Title => title;
        public string Description => description;
        public Sprite IconSprite => iconSprite;
        public int TargetAmount => Mathf.Max(1, targetAmount);
        public IReadOnlyList<int> NextTargetAmounts => nextTargetAmounts;
        public int RepeatRequirementAmount => Mathf.Max(1, repeatRequirementAmount);
        public int ProgressAmountPerEvent => Mathf.Max(1, progressAmountPerEvent);
        public CurrencyType RewardCurrency => rewardCurrency;
        public int RewardAmount => Mathf.Max(0, rewardAmount);
        public int CurrentAmount => Mathf.Max(0, currentAmount);
        public bool Completed => completed;
        public int CompletedCount => Mathf.Max(0, completedCount);
        public int PreviousTargetAmount => CompletedCount == 0
            ? 0
            : GetRequiredAmountForCompletion(CompletedCount - 1);
        public int NextTargetAmount => GetRequiredAmountForCompletion(CompletedCount);
        public float Progress01
        {
            get
            {
                int span = Mathf.Max(1, NextTargetAmount - PreviousTargetAmount);
                return Mathf.Clamp01((float)(CurrentAmount - PreviousTargetAmount) / span);
            }
        }

        public AchievementEntry(
            string id,
            AchievementProgressType progressType,
            string title,
            string description,
            int targetAmount,
            int progressAmountPerEvent = 1,
            int repeatRequirementAmount = 1,
            CurrencyType rewardCurrency = CurrencyType.Credits,
            int rewardAmount = 0)
        {
            this.id = id;
            this.progressType = progressType;
            this.title = title;
            this.description = description;
            this.targetAmount = Mathf.Max(1, targetAmount);
            this.progressAmountPerEvent = Mathf.Max(1, progressAmountPerEvent);
            this.repeatRequirementAmount = Mathf.Max(1, repeatRequirementAmount);
            this.rewardCurrency = rewardCurrency;
            this.rewardAmount = Mathf.Max(0, rewardAmount);
        }

        public bool AddProgress(int amount)
        {
            int positiveAmount = Mathf.Max(0, amount);
            if (positiveAmount == 0 || completed)
            {
                return false;
            }

            int previousAmount = currentAmount;
            currentAmount = Mathf.Min(NextTargetAmount, currentAmount + positiveAmount);
            RefreshCompletionState();
            return previousAmount != currentAmount;
        }

        public bool SetProgress(int amount)
        {
            if (completed)
            {
                return false;
            }

            int previousAmount = currentAmount;
            currentAmount = Mathf.Clamp(amount, 0, NextTargetAmount);
            RefreshCompletionState();
            return previousAmount != currentAmount;
        }

        public bool TryClaimReward()
        {
            RefreshCompletionState();
            if (!completed)
            {
                return false;
            }

            // 보상을 받은 뒤에만 다음 업적 단계의 진행을 허용한다.
            completedCount++;
            completed = false;
            return true;
        }

        public void RestoreProgress(int amount, int completedMilestones)
        {
            completedCount = Mathf.Max(0, completedMilestones);
            currentAmount = Mathf.Clamp(amount, 0, NextTargetAmount);
            RefreshCompletionState();
        }

        public void ResetProgress()
        {
            currentAmount = 0;
            completed = false;
            completedCount = 0;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = progressType.ToString();
            }

            targetAmount = Mathf.Max(1, targetAmount);
            nextTargetAmounts ??= new List<int>();
            int previousRequirement = targetAmount;
            for (int i = 0; i < nextTargetAmounts.Count; i++)
            {
                nextTargetAmounts[i] = Mathf.Max(previousRequirement + 1, nextTargetAmounts[i]);
                previousRequirement = nextTargetAmounts[i];
            }

            repeatRequirementAmount = Mathf.Max(1, repeatRequirementAmount);
            progressAmountPerEvent = Mathf.Max(1, progressAmountPerEvent);
            rewardAmount = Mathf.Max(0, rewardAmount);
            completedCount = Mathf.Max(0, completedCount);
            currentAmount = Mathf.Clamp(currentAmount, 0, NextTargetAmount);
            RefreshCompletionState();
        }

        private void RefreshCompletionState()
        {
            completed = currentAmount >= NextTargetAmount;
        }

        private int GetRequiredAmountForCompletion(int completionIndex)
        {
            if (completionIndex <= 0)
            {
                return TargetAmount;
            }

            int nextTargetIndex = completionIndex - 1;
            if (nextTargetAmounts != null && nextTargetIndex < nextTargetAmounts.Count)
            {
                return Mathf.Max(TargetAmount, nextTargetAmounts[nextTargetIndex]);
            }

            int lastConfiguredTarget = TargetAmount;
            if (nextTargetAmounts != null && nextTargetAmounts.Count > 0)
            {
                lastConfiguredTarget = Mathf.Max(TargetAmount, nextTargetAmounts[nextTargetAmounts.Count - 1]);
            }

            int repeatIndex = completionIndex - (nextTargetAmounts != null ? nextTargetAmounts.Count : 0);
            return lastConfiguredTarget + Mathf.Max(0, repeatIndex) * RepeatRequirementAmount;
        }
    }

    private const string CurrentKeySuffix = ".Current";
    private const string CompletedKeySuffix = ".Completed";
    private const string CompletedCountKeySuffix = ".CompletedCount";

    public static AchievementManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool saveToPlayerPrefs = true;
    [SerializeField] private string saveKeyPrefix = "Achievement.";

    [Header("Achievements")]
    [SerializeField] private List<AchievementEntry> achievements = new List<AchievementEntry>
    {
        new AchievementEntry("player_level", AchievementProgressType.PlayerLevel, "\uD50C\uB808\uC774\uC5B4 \uB808\uBCA8 \uB2EC\uC131", "\uD50C\uB808\uC774\uC5B4 \uB808\uBCA8 {0} \uB2EC\uC131", 5, 1, 5, CurrencyType.Credits, 100),
        new AchievementEntry("enemy_kill", AchievementProgressType.EnemyKill, "\uC801 \uCC98\uCE58", "\uC801 {0}\uB9C8\uB9AC \uCC98\uCE58", 10, 1, 10, CurrencyType.Credits, 100),
        new AchievementEntry("stage_clear", AchievementProgressType.StageClear, "\uC2A4\uD14C\uC774\uC9C0 \uD074\uB9AC\uC5B4", "\uC2A4\uD14C\uC774\uC9C0 {0}\uD68C \uD074\uB9AC\uC5B4", 3, 1, 3, CurrencyType.CoreCrystals, 10),
        new AchievementEntry("weapon_collect", AchievementProgressType.WeaponCollect, "\uBB34\uAE30 \uC218\uC9D1", "\uBB34\uAE30 {0}\uAC1C \uC218\uC9D1", 5, 1, 5, CurrencyType.Credits, 100),
        new AchievementEntry("drone_collect", AchievementProgressType.DroneCollect, "\uB4DC\uB860 \uC218\uC9D1", "\uB4DC\uB860 {0}\uAC1C \uC218\uC9D1", 3, 1, 3, CurrencyType.Credits, 100)
    };

    [Header("Events")]
    public UnityEvent OnAchievementsChanged = new UnityEvent();
    public UnityEvent<AchievementEntry> OnAchievementCompleted = new UnityEvent<AchievementEntry>();

    public IReadOnlyList<AchievementEntry> Achievements => achievements;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateAchievements();
        Load();
    }

    public static void ReportPlayerLevelReached(int level)
    {
        Instance?.SetProgress(AchievementProgressType.PlayerLevel, level);
    }

    public static void ReportEnemyKilled(int amount = 1)
    {
        Instance?.AddProgress(AchievementProgressType.EnemyKill, amount);
    }

    public static void ReportStageCleared(int amount = 1)
    {
        Instance?.AddProgress(AchievementProgressType.StageClear, amount);
    }

    public static void ReportWeaponCollected(int amount = 1)
    {
        Instance?.AddProgress(AchievementProgressType.WeaponCollect, amount);
    }

    public static void ReportDroneCollected(int amount = 1)
    {
        Instance?.AddProgress(AchievementProgressType.DroneCollect, amount);
    }

    [ContextMenu("Reset Achievement Progress")]
    public void ResetAllProgress()
    {
        foreach (AchievementEntry achievement in achievements)
        {
            achievement?.ResetProgress();
        }

        Save();
        OnAchievementsChanged.Invoke();
    }

    public JinyouAchievementSaveData CaptureState()
    {
        JinyouAchievementSaveData data = new JinyouAchievementSaveData();
        foreach (AchievementEntry achievement in achievements)
        {
            if (achievement == null)
            {
                continue;
            }

            data.entries.Add(new JinyouAchievementEntrySaveData
            {
                id = achievement.Id,
                currentAmount = achievement.CurrentAmount,
                completedCount = achievement.CompletedCount
            });
        }

        return data;
    }

    public void RestoreState(JinyouAchievementSaveData data)
    {
        if (data?.entries == null)
        {
            return;
        }

        foreach (JinyouAchievementEntrySaveData entryData in data.entries)
        {
            if (entryData == null || string.IsNullOrWhiteSpace(entryData.id))
            {
                continue;
            }

            AchievementEntry achievement = achievements.Find(item => item != null && item.Id == entryData.id);
            achievement?.RestoreProgress(entryData.currentAmount, entryData.completedCount);
        }

        Save();
        OnAchievementsChanged.Invoke();
    }

    public void AddProgress(AchievementProgressType progressType, int eventCount = 1)
    {
        bool changed = false;
        foreach (AchievementEntry achievement in achievements)
        {
            if (achievement == null || achievement.ProgressType != progressType)
            {
                continue;
            }

            int amount = Mathf.Max(0, eventCount) * achievement.ProgressAmountPerEvent;
            bool wasCompleted = achievement.Completed;
            changed = achievement.AddProgress(amount) || changed;
            if (!wasCompleted && achievement.Completed)
            {
                OnAchievementCompleted.Invoke(achievement);
            }
        }

        NotifyIfChanged(changed);
    }

    public void SetProgress(AchievementProgressType progressType, int amount)
    {
        bool changed = false;
        foreach (AchievementEntry achievement in achievements)
        {
            if (achievement == null || achievement.ProgressType != progressType)
            {
                continue;
            }

            bool wasCompleted = achievement.Completed;
            changed = achievement.SetProgress(amount) || changed;
            if (!wasCompleted && achievement.Completed)
            {
                OnAchievementCompleted.Invoke(achievement);
            }
        }

        NotifyIfChanged(changed);
    }

    public bool TryClaimReward(string achievementId)
    {
        AchievementEntry achievement = achievements.Find(item =>
            item != null && item.Id == achievementId);
        if (achievement == null || !achievement.TryClaimReward())
        {
            return false;
        }

        PlayerCurrencyWallet wallet = ResolveCurrencyWallet();
        if (wallet != null && achievement.RewardAmount > 0)
        {
            wallet.Add(achievement.RewardCurrency, achievement.RewardAmount);
        }

        Save();
        OnAchievementsChanged.Invoke();
        return true;
    }

    private PlayerCurrencyWallet ResolveCurrencyWallet()
    {
        if (BaseCampManager.Instance != null)
        {
            return BaseCampManager.Instance.CurrencyWallet;
        }

        return FindFirstObjectByType<PlayerCurrencyWallet>();
    }

    private void NotifyIfChanged(bool changed)
    {
        if (!changed)
        {
            return;
        }

        Save();
        OnAchievementsChanged.Invoke();
    }

    private void Load()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        foreach (AchievementEntry achievement in achievements)
        {
            if (achievement == null)
            {
                continue;
            }

            achievement.RestoreProgress(
                PlayerPrefs.GetInt(GetCurrentKey(achievement), achievement.CurrentAmount),
                PlayerPrefs.GetInt(GetCompletedCountKey(achievement), achievement.CompletedCount));
        }
    }

    private void Save()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        foreach (AchievementEntry achievement in achievements)
        {
            if (achievement == null)
            {
                continue;
            }

            PlayerPrefs.SetInt(GetCurrentKey(achievement), achievement.CurrentAmount);
            PlayerPrefs.SetInt(GetCompletedKey(achievement), achievement.Completed ? 1 : 0);
            PlayerPrefs.SetInt(GetCompletedCountKey(achievement), achievement.CompletedCount);
        }

        PlayerPrefs.Save();
    }

    private string GetCurrentKey(AchievementEntry achievement)
    {
        return saveKeyPrefix + achievement.Id + CurrentKeySuffix;
    }

    private string GetCompletedKey(AchievementEntry achievement)
    {
        return saveKeyPrefix + achievement.Id + CompletedKeySuffix;
    }

    private string GetCompletedCountKey(AchievementEntry achievement)
    {
        return saveKeyPrefix + achievement.Id + CompletedCountKeySuffix;
    }

    private void OnValidate()
    {
        ValidateAchievements();
    }

    private void ValidateAchievements()
    {
        achievements ??= new List<AchievementEntry>();
        foreach (AchievementEntry achievement in achievements)
        {
            achievement?.Validate();
        }
    }
}

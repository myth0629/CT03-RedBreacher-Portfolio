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
        [Min(1)]
        [SerializeField] private int targetAmount = 1;
        [Min(1)]
        [SerializeField] private int progressAmountPerEvent = 1;
        [SerializeField] private int currentAmount;
        [SerializeField] private bool completed;

        public string Id => id;
        public AchievementProgressType ProgressType => progressType;
        public string Title => title;
        public string Description => description;
        public int TargetAmount => Mathf.Max(1, targetAmount);
        public int ProgressAmountPerEvent => Mathf.Max(1, progressAmountPerEvent);
        public int CurrentAmount => Mathf.Clamp(currentAmount, 0, TargetAmount);
        public bool Completed => completed;
        public float Progress01 => TargetAmount > 0 ? Mathf.Clamp01((float)CurrentAmount / TargetAmount) : 0f;

        public AchievementEntry(
            string id,
            AchievementProgressType progressType,
            string title,
            string description,
            int targetAmount,
            int progressAmountPerEvent = 1)
        {
            this.id = id;
            this.progressType = progressType;
            this.title = title;
            this.description = description;
            this.targetAmount = Mathf.Max(1, targetAmount);
            this.progressAmountPerEvent = Mathf.Max(1, progressAmountPerEvent);
        }

        public bool AddProgress(int amount)
        {
            if (completed)
            {
                return false;
            }

            int nextAmount = Mathf.Clamp(currentAmount + Mathf.Max(0, amount), 0, TargetAmount);
            if (nextAmount == currentAmount)
            {
                return false;
            }

            currentAmount = nextAmount;
            completed = currentAmount >= TargetAmount;
            return true;
        }

        public bool SetProgress(int amount)
        {
            int nextAmount = Mathf.Clamp(Mathf.Max(0, amount), 0, TargetAmount);
            bool changed = nextAmount != currentAmount;
            currentAmount = nextAmount;

            bool wasCompleted = completed;
            completed = currentAmount >= TargetAmount;
            return changed || wasCompleted != completed;
        }

        public void ResetProgress()
        {
            currentAmount = 0;
            completed = false;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = progressType.ToString();
            }

            targetAmount = Mathf.Max(1, targetAmount);
            progressAmountPerEvent = Mathf.Max(1, progressAmountPerEvent);
            currentAmount = Mathf.Clamp(currentAmount, 0, targetAmount);
            completed = currentAmount >= targetAmount;
        }
    }

    private const string CurrentKeySuffix = ".Current";
    private const string CompletedKeySuffix = ".Completed";

    public static AchievementManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool saveToPlayerPrefs = true;
    [SerializeField] private string saveKeyPrefix = "Achievement.";

    [Header("Achievements")]
    [SerializeField] private List<AchievementEntry> achievements = new List<AchievementEntry>
    {
        new AchievementEntry("player_level", AchievementProgressType.PlayerLevel, "플레이어 레벨 달성", "플레이어 레벨 {0} 달성", 5),
        new AchievementEntry("enemy_kill", AchievementProgressType.EnemyKill, "적 처치", "적 {0}마리 처치", 10),
        new AchievementEntry("stage_clear", AchievementProgressType.StageClear, "스테이지 클리어", "스테이지 {0}회 클리어", 3),
        new AchievementEntry("weapon_collect", AchievementProgressType.WeaponCollect, "무기 수집", "무기 {0}개 수집", 5),
        new AchievementEntry("drone_collect", AchievementProgressType.DroneCollect, "드론 수집", "드론 {0}개 수집", 3)
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

    public void AddProgress(AchievementProgressType progressType, int eventCount = 1)
    {
        bool changed = false;
        foreach (AchievementEntry achievement in achievements)
        {
            if (achievement == null || achievement.ProgressType != progressType)
            {
                continue;
            }

            bool wasCompleted = achievement.Completed;
            int amount = Mathf.Max(0, eventCount) * achievement.ProgressAmountPerEvent;
            if (!achievement.AddProgress(amount))
            {
                continue;
            }

            changed = true;
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
            if (!achievement.SetProgress(amount))
            {
                continue;
            }

            changed = true;
            if (!wasCompleted && achievement.Completed)
            {
                OnAchievementCompleted.Invoke(achievement);
            }
        }

        NotifyIfChanged(changed);
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

            achievement.SetProgress(PlayerPrefs.GetInt(GetCurrentKey(achievement), achievement.CurrentAmount));
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

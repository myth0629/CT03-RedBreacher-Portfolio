using System;
using System.Collections.Generic;
using UnityEngine;

public class BossTracker : MonoBehaviour
{
    [Serializable]
    public class BossDefinition
    {
        public string bossId;
        public string displayName;
        public Sprite portrait;
        public BossEnemyConfig bossConfig;
    }

    [Serializable]
    public class BossDifficulty
    {
        public string difficultyId;
        public string displayName;
        public int requiredResearchLabLevel = 1;
        public int recommendedPower;
        public string rewardSummary;
        public BossEnemyConfig bossConfig;
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Min(0.01f)] public float moveSpeedMultiplier = 1f;
        [Min(0.01f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float rewardMultiplier = 1f;
        [Min(0)] public int firstClearCreditBonus;
        [Min(0)] public int firstClearCoreCrystalBonus;
    }

    [SerializeField] private CommandCenter cmdCenter;
    [SerializeField] private BossEncounterManager bossEncounterManager;
    [SerializeField] private List<BossDefinition> bosses = new List<BossDefinition>();

    // Kept under the original field name so existing prefab data remains valid.
    [SerializeField] private List<BossDifficulty> difficulties = new List<BossDifficulty>
    {
        new BossDifficulty
        {
            difficultyId = "normal",
            displayName = "일반",
            requiredResearchLabLevel = 1,
            recommendedPower = 1000,
            rewardSummary = "크레딧 / 부품"
        },
        new BossDifficulty
        {
            difficultyId = "hard",
            displayName = "어려움",
            requiredResearchLabLevel = 3,
            recommendedPower = 3000,
            rewardSummary = "고급 부품",
            healthMultiplier = 1.75f,
            moveSpeedMultiplier = 1.1f,
            damageMultiplier = 1.5f,
            rewardMultiplier = 1.75f
        },
        new BossDifficulty
        {
            difficultyId = "elite",
            displayName = "정예",
            requiredResearchLabLevel = 5,
            recommendedPower = 6000,
            rewardSummary = "코어 재료",
            healthMultiplier = 3f,
            moveSpeedMultiplier = 1.2f,
            damageMultiplier = 2.25f,
            rewardMultiplier = 3f
        }
    };

    [SerializeField] private int selectedBossIndex;
    [SerializeField] private int selectedDifficultyIndex;
    [SerializeField] private bool saveRecordsToPlayerPrefs = true;

    private readonly List<JinyouBossRecordSaveData> records = new List<JinyouBossRecordSaveData>();
    private BossDefinition activeBoss;
    private BossDifficulty activeDifficulty;
    private float encounterStartTime;

    public IReadOnlyList<BossDefinition> Bosses => bosses;
    public IReadOnlyList<BossDifficulty> Difficulties => difficulties;
    public CommandCenter CmdCenter => cmdCenter;
    public BossDefinition SelectedBoss => GetBoss(selectedBossIndex);
    public BossDifficulty SelectedDifficulty => GetDifficulty(selectedDifficultyIndex);
    public event Action SelectionChanged;
    public event Action RecordsChanged;

    private void Awake()
    {
        ResolveReferences();
        EnsureValidSelection();
        LoadLegacyRecords();
        SubscribeEncounter();
    }

    private void OnDestroy()
    {
        if (bossEncounterManager != null)
        {
            bossEncounterManager.EncounterEnded -= HandleEncounterEnded;
        }
    }

    public bool IsDifficultyUnlocked(BossDifficulty difficulty)
    {
        ResolveReferences();
        if (difficulty == null)
        {
            return false;
        }

        int researchLevel = cmdCenter != null ? cmdCenter.Level : 1;
        return researchLevel >= difficulty.requiredResearchLabLevel;
    }

    public bool CanEnterSelected()
    {
        return CanEnter(SelectedBoss, SelectedDifficulty);
    }

    public bool CanEnter(BossDefinition boss, BossDifficulty difficulty)
    {
        ResolveReferences();
        BossEnemyConfig config = GetBossConfig(boss, difficulty);
        return cmdCenter != null
            && cmdCenter.BossTickets > 0
            && IsDifficultyUnlocked(difficulty)
            && bossEncounterManager != null
            && bossEncounterManager.CanSummon(config);
    }

    public bool TryEnterSelected()
    {
        BossDefinition boss = SelectedBoss;
        BossDifficulty difficulty = SelectedDifficulty;
        if (!CanEnter(boss, difficulty))
        {
            return false;
        }

        if (!cmdCenter.TryUseBossTicket())
        {
            return false;
        }

        BossEnemyConfig config = GetBossConfig(boss, difficulty);
        SubscribeEncounter();
        bool started = bossEncounterManager.TrySummon(
            config,
            difficulty.healthMultiplier,
            difficulty.moveSpeedMultiplier,
            difficulty.damageMultiplier,
            difficulty.rewardMultiplier);
        if (!started)
        {
            cmdCenter.RefundBossTicket();
            return false;
        }

        activeBoss = boss;
        activeDifficulty = difficulty;
        encounterStartTime = Time.unscaledTime;
        JinyouBossRecordSaveData record = GetOrCreateRecord(boss, difficulty);
        record.attempts++;
        SaveRecord(record);
        return true;
    }

    public bool TryEnter(BossDifficulty difficulty)
    {
        if (difficulty != null)
        {
            int index = difficulties.IndexOf(difficulty);
            if (index >= 0)
            {
                selectedDifficultyIndex = index;
            }
        }

        return TryEnterSelected();
    }

    public void SelectPreviousBoss()
    {
        CycleBoss(-1);
    }

    public void SelectNextBoss()
    {
        CycleBoss(1);
    }

    public void SelectPreviousDifficulty()
    {
        CycleDifficulty(-1);
    }

    public void SelectNextDifficulty()
    {
        CycleDifficulty(1);
    }

    public BossDifficulty GetHighestUnlockedDifficulty()
    {
        BossDifficulty selected = null;
        foreach (BossDifficulty difficulty in difficulties)
        {
            if (IsDifficultyUnlocked(difficulty)
                && (selected == null
                    || difficulty.requiredResearchLabLevel > selected.requiredResearchLabLevel))
            {
                selected = difficulty;
            }
        }

        return selected;
    }

    public string GetRecordSummary(BossDefinition boss, BossDifficulty difficulty)
    {
        if (difficulty == null)
        {
            return string.Empty;
        }

        JinyouBossRecordSaveData record = GetOrCreateRecord(boss, difficulty);
        return record.bestTime > 0f
            ? $"클리어 {record.clears} / 실패 {record.failures} / 최고 {record.bestTime:0.0}초"
            : $"클리어 {record.clears} / 실패 {record.failures}";
    }

    public JinyouBossTrackerSaveData CaptureState()
    {
        BossDefinition boss = SelectedBoss;
        BossDifficulty difficulty = SelectedDifficulty;
        return new JinyouBossTrackerSaveData
        {
            selectedBossId = GetBossId(boss),
            selectedDifficultyId = GetDifficultyId(difficulty),
            records = new List<JinyouBossRecordSaveData>(records)
        };
    }

    public void RestoreState(JinyouBossTrackerSaveData data)
    {
        if (data == null)
        {
            return;
        }

        EnsureBossFallback();
        selectedBossIndex = FindBossIndex(data.selectedBossId, selectedBossIndex);
        selectedDifficultyIndex = FindDifficultyIndex(data.selectedDifficultyId, selectedDifficultyIndex);
        records.Clear();
        if (data.records != null)
        {
            foreach (JinyouBossRecordSaveData record in data.records)
            {
                if (record == null
                    || string.IsNullOrWhiteSpace(record.bossId)
                    || string.IsNullOrWhiteSpace(record.difficultyId))
                {
                    continue;
                }

                records.Add(new JinyouBossRecordSaveData
                {
                    bossId = record.bossId,
                    difficultyId = record.difficultyId,
                    attempts = Mathf.Max(0, record.attempts),
                    clears = Mathf.Max(0, record.clears),
                    failures = Mathf.Max(0, record.failures),
                    bestTime = Mathf.Max(0f, record.bestTime)
                });
            }
        }

        EnsureValidSelection();
        SelectionChanged?.Invoke();
        RecordsChanged?.Invoke();
    }

    public void SetStandaloneSaveEnabled(bool enabled, bool clearStoredData)
    {
        saveRecordsToPlayerPrefs = enabled;
        if (!clearStoredData)
        {
            return;
        }

        foreach (BossDefinition boss in bosses)
        {
            foreach (BossDifficulty difficulty in difficulties)
            {
                PlayerPrefs.DeleteKey(GetRecordKey(boss, difficulty, "Attempts"));
                PlayerPrefs.DeleteKey(GetRecordKey(boss, difficulty, "Clears"));
                PlayerPrefs.DeleteKey(GetRecordKey(boss, difficulty, "Failures"));
                PlayerPrefs.DeleteKey(GetRecordKey(boss, difficulty, "BestTime"));
            }
        }

        PlayerPrefs.Save();
    }

    private void HandleEncounterEnded(bool cleared)
    {
        if (activeDifficulty == null)
        {
            return;
        }

        BossDefinition completedBoss = activeBoss;
        BossDifficulty completedDifficulty = activeDifficulty;
        activeBoss = null;
        activeDifficulty = null;
        float clearTime = Mathf.Max(0f, Time.unscaledTime - encounterStartTime);
        JinyouBossRecordSaveData record = GetOrCreateRecord(completedBoss, completedDifficulty);

        if (!cleared)
        {
            ResolveReferences();
            cmdCenter?.RefundBossTicket();
            record.failures++;
            SaveRecord(record);
            bossEncounterManager.ShowResult(
                "보스전 실패 (티켓 반환)",
                $"{completedDifficulty.displayName}\n{GetRecordSummary(completedBoss, completedDifficulty)}",
                false);
            return;
        }

        bool firstClear = record.clears == 0;
        record.clears++;
        if (record.bestTime <= 0f || clearTime < record.bestTime)
        {
            record.bestTime = clearTime;
        }

        SaveRecord(record);
        if (firstClear)
        {
            PlayerCurrencyWallet wallet = BaseCampManager.Instance != null
                ? BaseCampManager.Instance.CurrencyWallet
                : FindFirstObjectByType<PlayerCurrencyWallet>();
            wallet?.AddCredits(completedDifficulty.firstClearCreditBonus);
            wallet?.AddCoreCrystals(completedDifficulty.firstClearCoreCrystalBonus);
        }

        string firstClearText = firstClear
            ? $"\n최초 클리어 보너스: 크레딧 +{completedDifficulty.firstClearCreditBonus}"
                + $" / 코어 +{completedDifficulty.firstClearCoreCrystalBonus}"
            : string.Empty;
        bossEncounterManager.ShowResult(
            "보스 클리어",
            $"{completedDifficulty.displayName} {clearTime:0.0}초{firstClearText}",
            true);
    }

    private JinyouBossRecordSaveData GetOrCreateRecord(BossDefinition boss, BossDifficulty difficulty)
    {
        string bossId = GetBossId(boss);
        string difficultyId = GetDifficultyId(difficulty);
        foreach (JinyouBossRecordSaveData record in records)
        {
            if (record.bossId == bossId && record.difficultyId == difficultyId)
            {
                return record;
            }
        }

        JinyouBossRecordSaveData created = new JinyouBossRecordSaveData
        {
            bossId = bossId,
            difficultyId = difficultyId
        };
        records.Add(created);
        return created;
    }

    private void SaveRecord(JinyouBossRecordSaveData record)
    {
        if (record == null)
        {
            return;
        }

        if (saveRecordsToPlayerPrefs)
        {
            BossDefinition boss = FindBoss(record.bossId);
            BossDifficulty difficulty = FindDifficulty(record.difficultyId);
            PlayerPrefs.SetInt(GetRecordKey(boss, difficulty, "Attempts"), Mathf.Max(0, record.attempts));
            PlayerPrefs.SetInt(GetRecordKey(boss, difficulty, "Clears"), Mathf.Max(0, record.clears));
            PlayerPrefs.SetInt(GetRecordKey(boss, difficulty, "Failures"), Mathf.Max(0, record.failures));
            PlayerPrefs.SetFloat(GetRecordKey(boss, difficulty, "BestTime"), Mathf.Max(0f, record.bestTime));
            PlayerPrefs.Save();
        }

        RecordsChanged?.Invoke();
        BaseCampManager.Instance?.RequestUnifiedSave();
    }

    private void LoadLegacyRecords()
    {
        foreach (BossDefinition boss in bosses)
        {
            foreach (BossDifficulty difficulty in difficulties)
            {
                int attempts = PlayerPrefs.GetInt(GetRecordKey(boss, difficulty, "Attempts"), 0);
                int clears = PlayerPrefs.GetInt(GetRecordKey(boss, difficulty, "Clears"), 0);
                int failures = PlayerPrefs.GetInt(GetRecordKey(boss, difficulty, "Failures"), 0);
                float bestTime = PlayerPrefs.GetFloat(GetRecordKey(boss, difficulty, "BestTime"), 0f);
                if (attempts <= 0 && clears <= 0 && failures <= 0 && bestTime <= 0f)
                {
                    continue;
                }

                JinyouBossRecordSaveData record = GetOrCreateRecord(boss, difficulty);
                record.attempts = Mathf.Max(0, attempts);
                record.clears = Mathf.Max(0, clears);
                record.failures = Mathf.Max(0, failures);
                record.bestTime = Mathf.Max(0f, bestTime);
            }
        }
    }

    private static string GetRecordKey(
        BossDefinition boss,
        BossDifficulty difficulty,
        string recordName)
    {
        return $"BossDungeon.{GetBossId(boss)}.{GetDifficultyId(difficulty)}.{recordName}";
    }

    private void CycleBoss(int direction)
    {
        if (bosses.Count <= 1)
        {
            return;
        }

        selectedBossIndex = WrapIndex(selectedBossIndex + direction, bosses.Count);
        SelectionChanged?.Invoke();
        BaseCampManager.Instance?.RequestUnifiedSave();
    }

    private void CycleDifficulty(int direction)
    {
        if (difficulties.Count <= 1)
        {
            return;
        }

        selectedDifficultyIndex = WrapIndex(selectedDifficultyIndex + direction, difficulties.Count);
        SelectionChanged?.Invoke();
        BaseCampManager.Instance?.RequestUnifiedSave();
    }

    private BossDefinition GetBoss(int index)
    {
        EnsureBossFallback();
        return bosses.Count > 0 ? bosses[WrapIndex(index, bosses.Count)] : null;
    }

    private BossDifficulty GetDifficulty(int index)
    {
        return difficulties.Count > 0 ? difficulties[WrapIndex(index, difficulties.Count)] : null;
    }

    private BossDefinition FindBoss(string bossId)
    {
        int index = FindBossIndex(bossId, -1);
        return index >= 0 ? bosses[index] : null;
    }

    private BossDifficulty FindDifficulty(string difficultyId)
    {
        int index = FindDifficultyIndex(difficultyId, -1);
        return index >= 0 ? difficulties[index] : null;
    }

    private int FindBossIndex(string bossId, int fallback)
    {
        if (string.IsNullOrWhiteSpace(bossId))
        {
            return fallback;
        }

        for (int i = 0; i < bosses.Count; i++)
        {
            if (GetBossId(bosses[i]) == bossId)
            {
                return i;
            }
        }

        return fallback;
    }

    private int FindDifficultyIndex(string difficultyId, int fallback)
    {
        if (string.IsNullOrWhiteSpace(difficultyId))
        {
            return fallback;
        }

        for (int i = 0; i < difficulties.Count; i++)
        {
            if (GetDifficultyId(difficulties[i]) == difficultyId)
            {
                return i;
            }
        }

        return fallback;
    }

    private static BossEnemyConfig GetBossConfig(BossDefinition boss, BossDifficulty difficulty)
    {
        return boss != null && boss.bossConfig != null
            ? boss.bossConfig
            : difficulty != null ? difficulty.bossConfig : null;
    }

    private void EnsureValidSelection()
    {
        EnsureBossFallback();
        selectedBossIndex = bosses.Count > 0 ? WrapIndex(selectedBossIndex, bosses.Count) : 0;
        selectedDifficultyIndex = difficulties.Count > 0
            ? WrapIndex(selectedDifficultyIndex, difficulties.Count)
            : 0;

        if (!IsDifficultyUnlocked(SelectedDifficulty))
        {
            BossDifficulty highest = GetHighestUnlockedDifficulty();
            int unlockedIndex = difficulties.IndexOf(highest);
            selectedDifficultyIndex = unlockedIndex >= 0 ? unlockedIndex : 0;
        }
    }

    private void EnsureBossFallback()
    {
        if (bosses.Count > 0)
        {
            return;
        }

        foreach (BossDifficulty difficulty in difficulties)
        {
            if (difficulty?.bossConfig == null)
            {
                continue;
            }

            bosses.Add(new BossDefinition
            {
                bossId = difficulty.bossConfig.Id,
                displayName = difficulty.bossConfig.DisplayName,
                portrait = difficulty.bossConfig.Portrait,
                bossConfig = difficulty.bossConfig
            });
            break;
        }
    }

    private void ResolveReferences()
    {
        cmdCenter ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.CommandCenter
            : FindFirstObjectByType<CommandCenter>();
        bossEncounterManager ??= FindFirstObjectByType<BossEncounterManager>();
    }

    private void SubscribeEncounter()
    {
        if (bossEncounterManager == null)
        {
            return;
        }

        bossEncounterManager.EncounterEnded -= HandleEncounterEnded;
        bossEncounterManager.EncounterEnded += HandleEncounterEnded;
    }

    private static string GetBossId(BossDefinition boss)
    {
        return !string.IsNullOrWhiteSpace(boss?.bossId)
            ? boss.bossId
            : !string.IsNullOrWhiteSpace(boss?.displayName)
                ? boss.displayName
                : "default_boss";
    }

    private static string GetDifficultyId(BossDifficulty difficulty)
    {
        return !string.IsNullOrWhiteSpace(difficulty?.difficultyId)
            ? difficulty.difficultyId
            : !string.IsNullOrWhiteSpace(difficulty?.displayName)
                ? difficulty.displayName
                : "normal";
    }

    private static int WrapIndex(int index, int count)
    {
        return count > 0 ? (index % count + count) % count : 0;
    }

    private void OnValidate()
    {
        foreach (BossDifficulty difficulty in difficulties)
        {
            if (difficulty == null)
            {
                continue;
            }

            difficulty.requiredResearchLabLevel = Mathf.Max(1, difficulty.requiredResearchLabLevel);
            difficulty.recommendedPower = Mathf.Max(0, difficulty.recommendedPower);
            difficulty.healthMultiplier = Mathf.Max(0.01f, difficulty.healthMultiplier);
            difficulty.moveSpeedMultiplier = Mathf.Max(0.01f, difficulty.moveSpeedMultiplier);
            difficulty.damageMultiplier = Mathf.Max(0.01f, difficulty.damageMultiplier);
            difficulty.rewardMultiplier = Mathf.Max(0.01f, difficulty.rewardMultiplier);
            difficulty.firstClearCreditBonus = Mathf.Max(0, difficulty.firstClearCreditBonus);
            difficulty.firstClearCoreCrystalBonus = Mathf.Max(0, difficulty.firstClearCoreCrystalBonus);
        }
    }
}

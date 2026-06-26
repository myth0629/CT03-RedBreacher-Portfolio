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
            rewardSummary = "희귀 부품",
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

    private BossDefinition activeBoss;
    private BossDifficulty activeDifficulty;
    private float encounterStartTime;

    public IReadOnlyList<BossDefinition> Bosses => bosses;
    public IReadOnlyList<BossDifficulty> Difficulties => difficulties;
    public CommandCenter CmdCenter => cmdCenter;
    public BossDefinition SelectedBoss => GetBoss(selectedBossIndex);
    public BossDifficulty SelectedDifficulty => GetDifficulty(selectedDifficultyIndex);
    public event Action SelectionChanged;

    private void Awake()
    {
        ResolveReferences();
        EnsureValidSelection();
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
        IncrementRecord(boss, difficulty, "Attempts");
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

        int clears = PlayerPrefs.GetInt(GetRecordKey(boss, difficulty, "Clears"), 0);
        int failures = PlayerPrefs.GetInt(GetRecordKey(boss, difficulty, "Failures"), 0);
        float bestTime = PlayerPrefs.GetFloat(GetRecordKey(boss, difficulty, "BestTime"), 0f);
        return bestTime > 0f
            ? $"클리어 {clears} / 실패 {failures} / 최고 {bestTime:0.0}초"
            : $"클리어 {clears} / 실패 {failures}";
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
        if (!cleared)
        {
            // 방치형: 패배는 "더 강해져"라는 정상 신호 → 시간제 자원인 티켓을 돌려준다.
            ResolveReferences();
            cmdCenter?.RefundBossTicket();
            IncrementRecord(completedBoss, completedDifficulty, "Failures");
            bossEncounterManager.ShowResult(
                "보스전 실패 (티켓 반환)",
                $"{completedDifficulty.displayName}\n{GetRecordSummary(completedBoss, completedDifficulty)}",
                false);
            return;
        }

        string clearKey = GetRecordKey(completedBoss, completedDifficulty, "Clears");
        int previousClears = PlayerPrefs.GetInt(clearKey, 0);
        bool firstClear = previousClears == 0;
        PlayerPrefs.SetInt(clearKey, previousClears + 1);

        string bestTimeKey = GetRecordKey(completedBoss, completedDifficulty, "BestTime");
        float bestTime = PlayerPrefs.GetFloat(bestTimeKey, 0f);
        if (bestTime <= 0f || clearTime < bestTime)
        {
            PlayerPrefs.SetFloat(bestTimeKey, clearTime);
        }

        if (firstClear)
        {
            // 기본 보상은 전투 보상 서비스가 지급하므로 최초 클리어 보너스만 추가한다.
            PlayerCurrencyWallet wallet = BaseCampManager.Instance != null
                ? BaseCampManager.Instance.CurrencyWallet
                : FindFirstObjectByType<PlayerCurrencyWallet>();
            wallet?.AddCredits(completedDifficulty.firstClearCreditBonus);
            wallet?.AddCoreCrystals(completedDifficulty.firstClearCoreCrystalBonus);
        }

        PlayerPrefs.Save();
        string firstClearText = firstClear
            ? $"\n최초 클리어 보너스: 크레딧 +{completedDifficulty.firstClearCreditBonus}"
                + $" / 코어 +{completedDifficulty.firstClearCoreCrystalBonus}"
            : string.Empty;
        bossEncounterManager.ShowResult(
            "보스 클리어",
            $"{completedDifficulty.displayName} {clearTime:0.0}초{firstClearText}",
            true);
    }

    private void IncrementRecord(
        BossDefinition boss,
        BossDifficulty difficulty,
        string recordName)
    {
        string key = GetRecordKey(boss, difficulty, recordName);
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
        PlayerPrefs.Save();
    }

    private static string GetRecordKey(
        BossDefinition boss,
        BossDifficulty difficulty,
        string recordName)
    {
        string bossId = !string.IsNullOrWhiteSpace(boss?.bossId)
            ? boss.bossId
            : boss?.displayName;
        string difficultyId = !string.IsNullOrWhiteSpace(difficulty?.difficultyId)
            ? difficulty.difficultyId
            : difficulty?.displayName;
        return $"BossDungeon.{bossId}.{difficultyId}.{recordName}";
    }

    private void CycleBoss(int direction)
    {
        if (bosses.Count <= 1)
        {
            return;
        }

        selectedBossIndex = WrapIndex(selectedBossIndex + direction, bosses.Count);
        SelectionChanged?.Invoke();
    }

    private void CycleDifficulty(int direction)
    {
        if (difficulties.Count <= 1)
        {
            return;
        }

        selectedDifficultyIndex = WrapIndex(selectedDifficultyIndex + direction, difficulties.Count);
        SelectionChanged?.Invoke();
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

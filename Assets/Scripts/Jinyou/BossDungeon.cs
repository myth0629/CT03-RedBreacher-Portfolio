using System;
using System.Collections.Generic;
using UnityEngine;

public class BossDungeon : MonoBehaviour
{
    [Serializable]
    public class BossDifficulty
    {
        public string difficultyId;
        public string displayName;
        public int requiredResearchLabLevel = 1;
        public int recommendedPower;
        public string rewardSummary;
        public BossEnemyConfig bossConfig;
        [Min(0)] public int clearCreditReward;
        [Min(0)] public int clearCoreCrystalReward;
        [Min(0)] public int firstClearCreditBonus;
        [Min(0)] public int firstClearCoreCrystalBonus;

        private int RewardTier => Mathf.Max(1, requiredResearchLabLevel);
        public int ClearCreditReward => clearCreditReward > 0
            ? clearCreditReward
            : 500 * RewardTier;
        public int ClearCoreCrystalReward => clearCoreCrystalReward > 0
            ? clearCoreCrystalReward
            : 5 * RewardTier;
        public int FirstClearCreditBonus => firstClearCreditBonus > 0
            ? firstClearCreditBonus
            : 500 * RewardTier;
        public int FirstClearCoreCrystalBonus => firstClearCoreCrystalBonus > 0
            ? firstClearCoreCrystalBonus
            : 10 * RewardTier;
    }

    [SerializeField] private CommandCenter cmdCenter;
    [SerializeField] private BossEncounterManager bossEncounterManager;
    [SerializeField] private List<BossDifficulty> difficulties = new List<BossDifficulty>
    {
        new BossDifficulty { difficultyId = "normal", displayName = "Normal Boss", requiredResearchLabLevel = 1, recommendedPower = 1000, rewardSummary = "Credits / Parts" },
        new BossDifficulty { difficultyId = "hard", displayName = "Hard Boss", requiredResearchLabLevel = 3, recommendedPower = 3000, rewardSummary = "Rare Parts" },
        new BossDifficulty { difficultyId = "elite", displayName = "Elite Boss", requiredResearchLabLevel = 5, recommendedPower = 6000, rewardSummary = "Core Materials" }
    };

    public IReadOnlyList<BossDifficulty> Difficulties => difficulties;
    public CommandCenter CmdCenter => cmdCenter;

    private BossDifficulty activeDifficulty;
    private float encounterStartTime;

    private void Awake()
    {
        ResolveReferences();
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

    public bool CanEnter(BossDifficulty difficulty)
    {
        ResolveReferences();
        return cmdCenter != null
            && cmdCenter.BossTickets > 0
            && IsDifficultyUnlocked(difficulty)
            && difficulty != null
            && bossEncounterManager != null
            && bossEncounterManager.CanSummon(difficulty.bossConfig);
    }

    public bool TryEnter(BossDifficulty difficulty)
    {
        if (!CanEnter(difficulty))
        {
            return false;
        }

        SubscribeEncounter();

        // 보스와 스폰 환경을 먼저 검증한 뒤 티켓을 소모하고 실제 전투를 시작한다.
        if (!cmdCenter.TryUseBossTicket())
        {
            return false;
        }

        if (!bossEncounterManager.TrySummon(difficulty.bossConfig))
        {
            cmdCenter.RefundBossTicket();
            return false;
        }

        activeDifficulty = difficulty;
        encounterStartTime = Time.unscaledTime;
        IncrementRecord(difficulty, "Attempts");
        return true;
    }

    public BossDifficulty GetHighestUnlockedDifficulty()
    {
        BossDifficulty selectedDifficulty = null;

        foreach (BossDifficulty difficulty in difficulties)
        {
            if (!IsDifficultyUnlocked(difficulty))
            {
                continue;
            }

            if (selectedDifficulty == null
                || difficulty.requiredResearchLabLevel > selectedDifficulty.requiredResearchLabLevel)
            {
                selectedDifficulty = difficulty;
            }
        }

        return selectedDifficulty;
    }

    public string GetRecordSummary(BossDifficulty difficulty)
    {
        if (difficulty == null)
        {
            return string.Empty;
        }

        int clears = PlayerPrefs.GetInt(GetRecordKey(difficulty, "Clears"), 0);
        int failures = PlayerPrefs.GetInt(GetRecordKey(difficulty, "Failures"), 0);
        float bestTime = PlayerPrefs.GetFloat(GetRecordKey(difficulty, "BestTime"), 0f);
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

        BossDifficulty completedDifficulty = activeDifficulty;
        activeDifficulty = null;
        float clearTime = Mathf.Max(0f, Time.unscaledTime - encounterStartTime);
        if (!cleared)
        {
            IncrementRecord(completedDifficulty, "Failures");
            bossEncounterManager.ShowResult(
                "보스전 실패",
                $"{completedDifficulty.displayName}\n{GetRecordSummary(completedDifficulty)}",
                false);
            return;
        }

        int previousClears = PlayerPrefs.GetInt(GetRecordKey(completedDifficulty, "Clears"), 0);
        bool firstClear = previousClears == 0;
        PlayerPrefs.SetInt(GetRecordKey(completedDifficulty, "Clears"), previousClears + 1);
        float bestTime = PlayerPrefs.GetFloat(GetRecordKey(completedDifficulty, "BestTime"), 0f);
        if (bestTime <= 0f || clearTime < bestTime)
        {
            PlayerPrefs.SetFloat(GetRecordKey(completedDifficulty, "BestTime"), clearTime);
        }

        int credits = completedDifficulty.ClearCreditReward
            + (firstClear ? completedDifficulty.FirstClearCreditBonus : 0);
        int coreCrystals = completedDifficulty.ClearCoreCrystalReward
            + (firstClear ? completedDifficulty.FirstClearCoreCrystalBonus : 0);
        PlayerCurrencyWallet wallet = BaseCampManager.Instance != null
            ? BaseCampManager.Instance.CurrencyWallet
            : FindFirstObjectByType<PlayerCurrencyWallet>();
        wallet?.AddCredits(credits);
        wallet?.AddCoreCrystals(coreCrystals);
        PlayerPrefs.Save();

        string firstClearText = firstClear ? "\n최초 클리어 보너스 포함" : string.Empty;
        bossEncounterManager.ShowResult(
            "보스 클리어",
            $"{completedDifficulty.displayName} {clearTime:0.0}초\n크레딧 +{credits} / 코어 +{coreCrystals}{firstClearText}",
            true);
    }

    private void IncrementRecord(BossDifficulty difficulty, string recordName)
    {
        string key = GetRecordKey(difficulty, recordName);
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
        PlayerPrefs.Save();
    }

    private static string GetRecordKey(BossDifficulty difficulty, string recordName)
    {
        string difficultyId = !string.IsNullOrWhiteSpace(difficulty?.difficultyId)
            ? difficulty.difficultyId
            : difficulty?.displayName;
        return $"BossDungeon.{difficultyId}.{recordName}";
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
            difficulty.clearCreditReward = Mathf.Max(0, difficulty.clearCreditReward);
            difficulty.clearCoreCrystalReward = Mathf.Max(0, difficulty.clearCoreCrystalReward);
            difficulty.firstClearCreditBonus = Mathf.Max(0, difficulty.firstClearCreditBonus);
            difficulty.firstClearCoreCrystalBonus = Mathf.Max(0, difficulty.firstClearCoreCrystalBonus);
        }
    }
}

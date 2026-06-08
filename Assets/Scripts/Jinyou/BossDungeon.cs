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
    }

    [SerializeField] private CommandCenter cmdCenter;
    [SerializeField] private List<BossDifficulty> difficulties = new List<BossDifficulty>
    {
        new BossDifficulty { difficultyId = "normal", displayName = "Normal Boss", requiredResearchLabLevel = 1, recommendedPower = 1000, rewardSummary = "Credits / Parts" },
        new BossDifficulty { difficultyId = "hard", displayName = "Hard Boss", requiredResearchLabLevel = 3, recommendedPower = 3000, rewardSummary = "Rare Parts" },
        new BossDifficulty { difficultyId = "elite", displayName = "Elite Boss", requiredResearchLabLevel = 5, recommendedPower = 6000, rewardSummary = "Core Materials" }
    };

    public IReadOnlyList<BossDifficulty> Difficulties => difficulties;
    public CommandCenter CmdCenter => cmdCenter;

    private void Awake()
    {
        ResolveReferences();
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
            && IsDifficultyUnlocked(difficulty);
    }

    public bool TryEnter(BossDifficulty difficulty)
    {
        if (!CanEnter(difficulty))
        {
            return false;
        }

        return cmdCenter.TryUseBossTicket();
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

    private void ResolveReferences()
    {
        cmdCenter ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.CommandCenter
            : FindFirstObjectByType<CommandCenter>();
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
        }
    }
}

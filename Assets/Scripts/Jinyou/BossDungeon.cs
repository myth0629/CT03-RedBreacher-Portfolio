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

    [SerializeField] private StrategyResearchLab researchLab;
    [SerializeField] private List<BossDifficulty> difficulties = new List<BossDifficulty>
    {
        new BossDifficulty { difficultyId = "normal", displayName = "Normal Boss", requiredResearchLabLevel = 1, recommendedPower = 1000, rewardSummary = "Credits / Parts" },
        new BossDifficulty { difficultyId = "hard", displayName = "Hard Boss", requiredResearchLabLevel = 3, recommendedPower = 3000, rewardSummary = "Rare Parts" },
        new BossDifficulty { difficultyId = "elite", displayName = "Elite Boss", requiredResearchLabLevel = 5, recommendedPower = 6000, rewardSummary = "Core Materials" }
    };

    public IReadOnlyList<BossDifficulty> Difficulties => difficulties;
    public StrategyResearchLab ResearchLab => researchLab;

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

        int researchLevel = researchLab != null ? researchLab.Level : 1;
        return researchLevel >= difficulty.requiredResearchLabLevel;
    }

    public bool CanEnter(BossDifficulty difficulty)
    {
        ResolveReferences();
        return researchLab != null
            && researchLab.BossTickets > 0
            && IsDifficultyUnlocked(difficulty);
    }

    public bool TryEnter(BossDifficulty difficulty)
    {
        if (!CanEnter(difficulty))
        {
            return false;
        }

        return researchLab.TryUseBossTicket();
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
        researchLab ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.ResearchLab
            : FindFirstObjectByType<StrategyResearchLab>();
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

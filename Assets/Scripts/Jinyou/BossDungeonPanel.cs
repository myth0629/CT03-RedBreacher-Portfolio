using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossDungeonPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private BossDungeon bossDungeon;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text ticketText;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text entryStateText;
    [SerializeField] private Image ticketProgressFill;

    private string entryStateMessage = "Entry placeholder only";

    private void OnEnable()
    {
        ResolveReferences();
        enterButton?.onClick.AddListener(TryEnterBossDungeon);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        enterButton?.onClick.RemoveListener(TryEnterBossDungeon);
        closeButton?.onClick.RemoveListener(ClosePanel);
    }

    private void Update()
    {
        Refresh();
    }

    private void TryEnterBossDungeon()
    {
        if (bossDungeon == null)
        {
            entryStateMessage = "Boss Dungeon not connected";
            Refresh();
            return;
        }

        BossDungeon.BossDifficulty difficulty = bossDungeon.GetHighestUnlockedDifficulty();
        if (difficulty == null)
        {
            entryStateMessage = "No boss difficulty unlocked";
            Refresh();
            return;
        }

        if (bossDungeon.TryEnter(difficulty))
        {
            entryStateMessage = $"{difficulty.displayName} ticket consumed";
        }
        else
        {
            entryStateMessage = "Need a boss ticket";
        }

        Refresh();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        ResolveReferences();

        StrategyResearchLab researchLab = baseCampManager != null ? baseCampManager.ResearchLab : null;
        if (researchLab == null)
        {
            SetText(ticketText, "Tickets --/--");
            SetText(productionText, "Production --/day");
            SetText(difficultyText, "Research Lab not connected");
            SetText(entryStateText, "Entry disabled");
            SetFill(ticketProgressFill, 0f);
            SetEnterButton(false);
            return;
        }

        SetText(ticketText, $"Tickets {researchLab.BossTickets}/{researchLab.BossTicketCapacity}");
        SetText(productionText, $"{researchLab.BossTicketsProducedPerDay}/day");
        SetText(difficultyText, BuildDifficultySummary());
        SetText(entryStateText, entryStateMessage);
        SetFill(ticketProgressFill, researchLab.BossTicketProductionProgress);
        SetEnterButton(bossDungeon != null && bossDungeon.CanEnter(bossDungeon.GetHighestUnlockedDifficulty()));
    }

    private string BuildDifficultySummary()
    {
        if (bossDungeon == null)
        {
            return "Boss Dungeon not connected";
        }

        string summary = string.Empty;
        foreach (BossDungeon.BossDifficulty difficulty in bossDungeon.Difficulties)
        {
            string state = bossDungeon.IsDifficultyUnlocked(difficulty)
                ? "OPEN"
                : $"Research Lv.{difficulty.requiredResearchLabLevel}";
            summary += $"{difficulty.displayName}: {state} / {difficulty.rewardSummary}\n";
        }

        return summary.TrimEnd();
    }

    private void SetEnterButton(bool interactable)
    {
        if (enterButton != null)
        {
            enterButton.interactable = interactable;
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        bossDungeon ??= FindFirstObjectByType<BossDungeon>();
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetFill(Image target, float value)
    {
        if (target != null)
        {
            target.fillAmount = Mathf.Clamp01(value);
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BossSummonButton : MonoBehaviour
{
    [Header("Source")]
    [FormerlySerializedAs("bossDungeon")]
    [SerializeField] private BossTracker bossTracker;

    [Header("UI")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TMP_Text ticketText;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text stateText;

    private string stateMessage = string.Empty;

    private void Awake()
    {
        summonButton ??= GetComponent<Button>();
    }

    private void OnEnable()
    {
        ResolveReferences();
        summonButton?.onClick.AddListener(TrySummonBoss);
        Refresh();
    }

    private void OnDisable()
    {
        summonButton?.onClick.RemoveListener(TrySummonBoss);
    }

    private void Update()
    {
        Refresh();
    }

    public void TrySummonBoss()
    {
        ResolveReferences();
        if (bossTracker == null)
        {
            stateMessage = "Boss Tracker is not connected.";
            Refresh();
            return;
        }

        BossTracker.BossDefinition boss = bossTracker.SelectedBoss;
        BossTracker.BossDifficulty difficulty = bossTracker.SelectedDifficulty;
        if (boss == null || difficulty == null)
        {
            stateMessage = "No boss is selected.";
            Refresh();
            return;
        }

        if (!bossTracker.IsDifficultyUnlocked(difficulty))
        {
            stateMessage = $"{difficulty.displayName} is locked.";
            Refresh();
            return;
        }

        if (bossTracker.TryEnterSelected())
        {
            stateMessage = $"{GetBossName(boss)} - {difficulty.displayName}";
            DailyMissionManager.ReportBossTicketUsed();
        }
        else
        {
            stateMessage = "Check the ticket count or boss encounter state.";
        }

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        CommandCenter commandCenter = bossTracker != null ? bossTracker.CmdCenter : null;
        BossTracker.BossDefinition boss = bossTracker != null ? bossTracker.SelectedBoss : null;
        BossTracker.BossDifficulty difficulty = bossTracker != null
            ? bossTracker.SelectedDifficulty
            : null;

        if (ticketText != null)
        {
            ticketText.text = commandCenter != null
                ? $"Ticket {commandCenter.BossTickets}/{commandCenter.BossTicketCapacity}"
                : "Ticket --/--";
        }

        if (bossNameText != null)
        {
            bossNameText.text = boss != null && difficulty != null
                ? $"{GetBossName(boss)} [{difficulty.displayName}]"
                : "No boss selected";
        }

        if (stateText != null)
        {
            stateText.text = stateMessage;
        }

        if (summonButton != null)
        {
            summonButton.interactable = bossTracker != null && bossTracker.CanEnterSelected();
        }
    }

    private static string GetBossName(BossTracker.BossDefinition boss)
    {
        if (!string.IsNullOrWhiteSpace(boss.displayName))
        {
            return boss.displayName;
        }

        return boss.bossConfig != null ? boss.bossConfig.DisplayName : "Boss";
    }

    private void ResolveReferences()
    {
        bossTracker ??= FindFirstObjectByType<BossTracker>();
    }
}

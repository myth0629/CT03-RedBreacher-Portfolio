using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossTrackerPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private BossTracker bossTracker;
    [SerializeField] private TMP_Text ticketText;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private Image ticketProgressFill;
    
    [Header("Bossinfo")]
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text bossHealthText;

    [Header("Visual")] 
    [SerializeField] private Image bossIcon;
    
    private void OnEnable()
    {
        ResolveReferences();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        CommandCenter researchLab = baseCampManager != null ? baseCampManager.CommandCenter : null;
        if (researchLab == null)
        {
            SetText(ticketText, "티켓 수: --/--");
            SetText(productionText, "1일당 티켓 지급없음");
            SetText(difficultyText, "사령부가 건설되어 있지 않습니다.");
            SetBossInfo(null);
            SetFill(ticketProgressFill, 0f);
            return;
        }

        BossTracker.BossDifficulty highestDifficulty = bossTracker != null
            ? bossTracker.GetHighestUnlockedDifficulty()
            : null;

        SetText(ticketText, $"티켓 수: {researchLab.BossTickets}/{researchLab.BossTicketCapacity}");
        SetText(productionText, $"* 1일당 {researchLab.BossTicketsProducedPerDay}티켓 지급 *");
        SetText(difficultyText, BuildDifficultySummary());
        SetBossInfo(highestDifficulty?.bossConfig);
        SetFill(ticketProgressFill, researchLab.BossTicketCapacity > 0
            ? (float)researchLab.BossTickets / researchLab.BossTicketCapacity
            : 0f);
    }

    private string BuildDifficultySummary()
    {
        if (bossTracker == null)
        {
            return "Boss Dungeon not connected";
        }

        string summary = string.Empty;
        foreach (BossTracker.BossDifficulty difficulty in bossTracker.Difficulties)
        {
            string state = bossTracker.IsDifficultyUnlocked(difficulty)
                ? "OPEN"
                : $"Research Lv.{difficulty.requiredResearchLabLevel}";
            summary += $"{difficulty.displayName}: {state} / {difficulty.rewardSummary}\n";
        }

        return summary.TrimEnd();
    }

    private void SetBossInfo(BossEnemyConfig bossConfig)
    {
        SetText(bossNameText, bossConfig != null ? bossConfig.DisplayName : "보스 미해금");
        SetText(bossHealthText, bossConfig != null ? $"{bossConfig.MaxHealth:0.##}" : string.Empty);
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        bossTracker ??= FindFirstObjectByType<BossTracker>();
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

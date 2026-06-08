using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossDungeonPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private BossDungeon bossDungeon;
    [SerializeField] private Button enterButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text ticketText;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text entryStateText;
    [SerializeField] private Image ticketProgressFill;

    [Header("Visual")]
    [SerializeField] private Image facilityImage;
    [SerializeField] private Sprite levelSprite;

    
    private string entryStateMessage = "Entry placeholder only";

    public void Configure(
        BaseCampManager manager,
        TMP_Text level,
        Image targetImage,
        Sprite sprite)
    {
        baseCampManager = manager;
        levelText = level;
        facilityImage = targetImage;
        levelSprite = sprite;
        Refresh();
    }
    
    private void OnEnable()
    {
        ResolveReferences();
        enterButton?.onClick.AddListener(TryEnterBossDungeon);
        Refresh();
    }

    private void OnDisable()
    {
        enterButton?.onClick.RemoveListener(TryEnterBossDungeon);
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
            DailyMissionManager.ReportBossTicketUsed();
        }
        else
        {
            entryStateMessage = "Need a boss ticket";
        }

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        CommandCenter researchLab = baseCampManager != null ? baseCampManager.CommandCenter : null;
        if (researchLab == null)
        {
            UpdateFacilityVisual();
            SetText(ticketText, "티켓 수: --/--");
            SetText(productionText, "1일당 티켓 지급없음");
            SetText(difficultyText, "사령부가 건설되어 있지 않습니다.");
            SetText(entryStateText, "Entry disabled");
            SetFill(ticketProgressFill, 0f);
            SetEnterButton(false);
            return;
        }

        UpdateFacilityVisual();
        SetText(levelText, $"Lv. {researchLab.Level}");
        SetText(ticketText, $"티켓 수: {researchLab.BossTickets}/{researchLab.BossTicketCapacity}");
        SetText(productionText, $"* 1일당 {researchLab.BossTicketsProducedPerDay}티켓 지급 *");
        SetText(difficultyText, BuildDifficultySummary());
        SetText(entryStateText, entryStateMessage);
        SetFill(ticketProgressFill, researchLab.BossTicketCapacity > 0
            ? (float)researchLab.BossTickets / researchLab.BossTicketCapacity
            : 0f);
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

    private void UpdateFacilityVisual()
    {
        if (facilityImage == null || levelSprite == null || bossDungeon == null)
        {
            return;
        }
        
        facilityImage.sprite = levelSprite;
        facilityImage.color = Color.white;
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

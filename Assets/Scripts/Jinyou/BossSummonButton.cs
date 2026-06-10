using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossSummonButton : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private BossDungeon bossDungeon;

    [Header("UI")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TMP_Text ticketText;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text stateText;

    private string stateMessage = string.Empty;

    private void Awake()
    {
        if (summonButton == null)
        {
            summonButton = GetComponent<Button>();
        }
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
        if (bossDungeon == null)
        {
            stateMessage = "보스 시스템이 연결되지 않았습니다.";
            Refresh();
            return;
        }

        BossDungeon.BossDifficulty difficulty = bossDungeon.GetHighestUnlockedDifficulty();
        if (difficulty == null)
        {
            stateMessage = "해금된 보스가 없습니다.";
            Refresh();
            return;
        }

        // 게임 HUD 버튼에서도 기지와 동일한 티켓 소모 및 소환 검증을 사용한다.
        if (bossDungeon.TryEnter(difficulty))
        {
            stateMessage = $"{difficulty.displayName} 소환";
            DailyMissionManager.ReportBossTicketUsed();
        }
        else
        {
            stateMessage = "티켓 또는 보스 설정을 확인하세요.";
        }

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        CommandCenter commandCenter = bossDungeon != null ? bossDungeon.CmdCenter : null;
        BossDungeon.BossDifficulty difficulty = bossDungeon != null
            ? bossDungeon.GetHighestUnlockedDifficulty()
            : null;

        if (ticketText != null)
        {
            ticketText.text = commandCenter != null
                ? $"티켓 {commandCenter.BossTickets}/{commandCenter.BossTicketCapacity}"
                : "티켓 --/--";
        }

        if (bossNameText != null)
        {
            bossNameText.text = difficulty != null
                ? difficulty.displayName
                : "보스 미해금";
        }

        if (stateText != null)
        {
            stateText.text = stateMessage;
        }

        if (summonButton != null)
        {
            summonButton.interactable = bossDungeon != null
                && difficulty != null
                && bossDungeon.CanEnter(difficulty);
        }
    }

    private void ResolveReferences()
    {
        bossDungeon ??= FindFirstObjectByType<BossDungeon>();
    }
}

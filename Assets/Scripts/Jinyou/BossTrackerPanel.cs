using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BossTrackerPanel : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private BossTracker bossTracker;

    [Header("Ticket")]
    [SerializeField] private TMP_Text ticketText;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private Image ticketProgressFill;

    [Header("Boss Info")]
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private TMP_Text rangedAttackText;
    [SerializeField] private TMP_Text laserAttackText;
    [SerializeField] private TMP_Text creditRewardText;
    [SerializeField] private TMP_Text coreRewardText;

    [Header("Visual")]
    [SerializeField] private Image bossIcon;

    [Header("Selection Buttons")]
    [SerializeField] private Button previousBossButton;
    [SerializeField] private Button nextBossButton;
    [SerializeField] private Button previousDifficultyButton;
    [SerializeField] private Button nextDifficultyButton;

    private void OnEnable()
    {
        ResolveReferences();
        ResolvePanelWidgets();
        BindButtons();
        if (bossTracker != null)
        {
            bossTracker.SelectionChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        UnbindButtons();
        if (bossTracker != null)
        {
            bossTracker.SelectionChanged -= Refresh;
        }
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        CommandCenter commandCenter = baseCampManager != null
            ? baseCampManager.CommandCenter
            : bossTracker != null ? bossTracker.CmdCenter : null;

        if (commandCenter == null)
        {
            SetText(ticketText, "티켓 --/--");
            SetText(productionText, "티켓 생산 정보 없음");
            SetText(difficultyText, "보스 트래커가 연결되지 않았습니다.");
            SetBossInfo(null, null);
            SetFill(ticketProgressFill, 0f);
            return;
        }

        BossTracker.BossDefinition boss = bossTracker != null ? bossTracker.SelectedBoss : null;
        BossTracker.BossDifficulty difficulty = bossTracker != null
            ? bossTracker.SelectedDifficulty
            : null;

        SetText(ticketText, $"티켓 {commandCenter.BossTickets}/{commandCenter.BossTicketCapacity}");
        SetText(productionText, $"하루 {commandCenter.BossTicketsProducedPerDay}개 지급");
        SetText(difficultyText, BuildDifficultySummary(difficulty));
        SetBossInfo(boss, difficulty);
        SetFill(ticketProgressFill, commandCenter.BossTicketCapacity > 0
            ? (float)commandCenter.BossTickets / commandCenter.BossTicketCapacity
            : 0f);

        bool hasMultipleBosses = bossTracker != null && bossTracker.Bosses.Count > 1;
        SetInteractable(previousBossButton, hasMultipleBosses);
        SetInteractable(nextBossButton, hasMultipleBosses);
        SetInteractable(previousDifficultyButton, bossTracker != null && bossTracker.Difficulties.Count > 1);
        SetInteractable(nextDifficultyButton, bossTracker != null && bossTracker.Difficulties.Count > 1);
    }

    /// <summary>
    /// 난이도 정보를 표시하는 UI 로직
    /// 난이도(일반>) | 해금, 미해금 | 권장 전투력
    /// </summary>
    /// <param name="difficulty"></param>
    /// <returns></returns>
    private string BuildDifficultySummary(BossTracker.BossDifficulty difficulty)
    {
        if (bossTracker == null || difficulty == null)
        {
            return "난이도 정보 없음";
        }

        string state = bossTracker.IsDifficultyUnlocked(difficulty)
            ? "해금됨"
            : $"사령부 Lv.{difficulty.requiredResearchLabLevel} 필요";
        return $"{difficulty.displayName} | {state} | 권장 전투력 {difficulty.recommendedPower:N0}";
    }

    /// <summary>
    /// 보스에 대한 정보
    /// </summary>
    /// <param name="boss"></param>
    /// <param name="difficulty"></param>
    private void SetBossInfo(
        BossTracker.BossDefinition boss,
        BossTracker.BossDifficulty difficulty)
    {
        BossEnemyConfig config = boss != null ? boss.bossConfig : null;
        string displayName = boss != null && !string.IsNullOrWhiteSpace(boss.displayName)
            ? boss.displayName
            : config != null ? config.DisplayName : "선택된 보스 없음";

        float healthMultiplier = difficulty != null ? difficulty.healthMultiplier : 1f;
        float damageMultiplier = difficulty != null ? difficulty.damageMultiplier : 1f;
        float rewardMultiplier = difficulty != null ? difficulty.rewardMultiplier : 1f;

        SetText(bossNameText, displayName);
        SetText(bossHealthText, config != null
            ? $"{config.MaxHealth * healthMultiplier:0} <color=#ffffff>HP</color>"
            : string.Empty);
        SetText(rangedAttackText, config != null
            ? $"범위 공격 ({config.RangedAttackDamage * damageMultiplier:0})"
            : string.Empty);
        SetText(laserAttackText, config != null
            ? $"레이저 공격 ({config.LaserDamage * damageMultiplier:0})"
            : string.Empty);
        SetText(creditRewardText, config != null
            ? $"크레딧 {Mathf.RoundToInt(config.CreditReward * rewardMultiplier):N0}"
            : string.Empty);
        SetText(coreRewardText, config != null
            ? $"코어 {Mathf.RoundToInt(config.CoreCrystalReward * rewardMultiplier):N0}"
            : string.Empty);

        if (bossIcon != null)
        {
            Sprite portrait = config != null && config.Portrait != null
                ? config.Portrait
                : boss != null ? boss.portrait : null;
            bossIcon.sprite = portrait;
            bossIcon.enabled = portrait != null;
            bossIcon.preserveAspect = true;
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        bossTracker ??= FindFirstObjectByType<BossTracker>();
    }

    private void ResolvePanelWidgets()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        Image[] images = GetComponentsInChildren<Image>(true);

        bossNameText ??= FindByName(texts, "BossName_txt");
        bossHealthText ??= FindByName(texts, "Boss_Health_Num");
        rangedAttackText ??= FindByName(texts, "RangedAttack_txt");
        laserAttackText ??= FindByName(texts, "LaserPatten_txt");
        creditRewardText ??= FindByName(texts, "CreditReward_txt");
        coreRewardText ??= FindByName(texts, "CoreReward_txt");
        bossIcon ??= FindByName(images, "Boss_Icon");
    }

    private void BindButtons()
    {
        previousBossButton?.onClick.AddListener(SelectPreviousBoss);
        nextBossButton?.onClick.AddListener(SelectNextBoss);
        previousDifficultyButton?.onClick.AddListener(SelectPreviousDifficulty);
        nextDifficultyButton?.onClick.AddListener(SelectNextDifficulty);
    }

    private void UnbindButtons()
    {
        previousBossButton?.onClick.RemoveListener(SelectPreviousBoss);
        nextBossButton?.onClick.RemoveListener(SelectNextBoss);
        previousDifficultyButton?.onClick.RemoveListener(SelectPreviousDifficulty);
        nextDifficultyButton?.onClick.RemoveListener(SelectNextDifficulty);
    }

    private void SelectPreviousBoss()
    {
        bossTracker?.SelectPreviousBoss();
    }

    private void SelectNextBoss()
    {
        bossTracker?.SelectNextBoss();
    }

    private void SelectPreviousDifficulty()
    {
        bossTracker?.SelectPreviousDifficulty();
    }

    private void SelectNextDifficulty()
    {
        bossTracker?.SelectNextDifficulty();
    }

    private static T FindByName<T>(T[] components, string objectName) where T : Component
    {
        foreach (T component in components)
        {
            if (component != null && component.gameObject.name == objectName)
            {
                return component;
            }
        }

        return null;
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

    private static void SetInteractable(Button target, bool value)
    {
        if (target != null)
        {
            target.interactable = value;
        }
    }
}

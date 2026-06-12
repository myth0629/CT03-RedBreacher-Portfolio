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
        EnsureSelectionControls();
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
            SetText(ticketText, "Ticket --/--");
            SetText(productionText, "Ticket production unavailable");
            SetText(difficultyText, "Boss Tracker is not connected.");
            SetBossInfo(null, null);
            SetFill(ticketProgressFill, 0f);
            return;
        }

        BossTracker.BossDefinition boss = bossTracker != null ? bossTracker.SelectedBoss : null;
        BossTracker.BossDifficulty difficulty = bossTracker != null
            ? bossTracker.SelectedDifficulty
            : null;

        SetText(ticketText, $"Ticket {commandCenter.BossTickets}/{commandCenter.BossTicketCapacity}");
        SetText(productionText, $"{commandCenter.BossTicketsProducedPerDay} tickets per day");
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

    private string BuildDifficultySummary(BossTracker.BossDifficulty difficulty)
    {
        if (bossTracker == null || difficulty == null)
        {
            return "Difficulty unavailable";
        }

        string state = bossTracker.IsDifficultyUnlocked(difficulty)
            ? "OPEN"
            : $"Command Center Lv.{difficulty.requiredResearchLabLevel}";
        return $"{difficulty.displayName} | {state} | Power {difficulty.recommendedPower:N0}";
    }

    private void SetBossInfo(
        BossTracker.BossDefinition boss,
        BossTracker.BossDifficulty difficulty)
    {
        BossEnemyConfig config = boss != null ? boss.bossConfig : null;
        string displayName = boss != null && !string.IsNullOrWhiteSpace(boss.displayName)
            ? boss.displayName
            : config != null ? config.DisplayName : "No boss selected";

        float healthMultiplier = difficulty != null ? difficulty.healthMultiplier : 1f;
        float damageMultiplier = difficulty != null ? difficulty.damageMultiplier : 1f;
        float rewardMultiplier = difficulty != null ? difficulty.rewardMultiplier : 1f;

        SetText(bossNameText, displayName);
        SetText(bossHealthText, config != null
            ? $"{config.MaxHealth * healthMultiplier:0}"
            : string.Empty);
        SetText(rangedAttackText, config != null
            ? $"Spread Shot ({config.RangedAttackDamage * damageMultiplier:0})"
            : string.Empty);
        SetText(laserAttackText, config != null
            ? $"Laser ({config.LaserDamage * damageMultiplier:0})"
            : string.Empty);
        SetText(creditRewardText, config != null
            ? $"Credits {Mathf.RoundToInt(config.CreditReward * rewardMultiplier):N0}"
            : string.Empty);
        SetText(coreRewardText, config != null
            ? $"Core {Mathf.RoundToInt(config.CoreCrystalReward * rewardMultiplier):N0}"
            : string.Empty);

        if (bossIcon != null)
        {
            Sprite portrait = boss != null && boss.portrait != null
                ? boss.portrait
                : config != null ? config.Portrait : null;
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
        difficultyText ??= CreateDifficultyText(
            bossNameText != null ? bossNameText.transform.parent : transform);
    }

    private void EnsureSelectionControls()
    {
        Transform iconTransform = bossIcon != null ? bossIcon.transform : transform;
        Transform panelTransform = bossNameText != null ? bossNameText.transform.parent : transform;

        previousBossButton ??= CreateArrowButton(
            "Previous Boss Button",
            iconTransform.parent,
            "<",
            new Vector2(0f, 0.5f),
            new Vector2(-45f, 0f),
            new Vector2(70f, 110f));
        nextBossButton ??= CreateArrowButton(
            "Next Boss Button",
            iconTransform.parent,
            ">",
            new Vector2(1f, 0.5f),
            new Vector2(45f, 0f),
            new Vector2(70f, 110f));
        previousDifficultyButton ??= CreateArrowButton(
            "Previous Difficulty Button",
            panelTransform,
            "v",
            new Vector2(1f, 1f),
            new Vector2(-95f, -115f),
            new Vector2(60f, 42f));
        nextDifficultyButton ??= CreateArrowButton(
            "Next Difficulty Button",
            panelTransform,
            "^",
            new Vector2(1f, 1f),
            new Vector2(-25f, -115f),
            new Vector2(60f, 42f));
    }

    private TMP_Text CreateDifficultyText(Transform parent)
    {
        Transform existing = parent.Find("Selected Difficulty Text");
        if (existing != null)
        {
            return existing.GetComponent<TMP_Text>();
        }

        GameObject textObject = new GameObject(
            "Selected Difficulty Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -92f);
        rect.sizeDelta = new Vector2(620f, 42f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 26f;
        text.color = new Color(1f, 0.8f, 0.25f, 1f);
        text.raycastTarget = false;
        return text;
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

    private static Button CreateArrowButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        Transform existing = parent != null ? parent.Find(objectName) : null;
        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = parent != null ? parent.gameObject.layer : 5;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.93f, 0.22f, 0.14f, 0.92f);

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = buttonObject.layer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = Mathf.Min(size.x, size.y) * 0.55f;
        text.color = Color.white;
        text.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
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

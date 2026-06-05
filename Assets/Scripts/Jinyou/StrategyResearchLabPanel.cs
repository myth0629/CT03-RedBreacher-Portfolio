using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StrategyResearchLabPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button bossTicketButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text bossTicketText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text unlockText;

    private StrategyResearchLab researchLab;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        upgradeButton?.onClick.AddListener(UpgradeResearchLab);
        Refresh();
    }

    private void OnDisable()
    {
        upgradeButton?.onClick.RemoveListener(UpgradeResearchLab);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button upgrade,
        Button bossTicket,
        TMP_Text level,
        TMP_Text bossTicketLabel,
        TMP_Text offlineReward,
        TMP_Text upgradeLabel,
        TMP_Text unlock)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        bossTicketButton = bossTicket;
        levelText = level;
        bossTicketText = bossTicketLabel;
        offlineRewardText = offlineReward;
        upgradeText = upgradeLabel;
        unlockText = unlock;
        Refresh();
    }

    private void UpgradeResearchLab()
    {
        baseCampManager?.UpgradeResearchLab();
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (researchLab == null)
        {
            return;
        }

        SetText(levelText, $"Lv. {researchLab.Level}");
        SetText(bossTicketText, $"{researchLab.BossTickets}/{researchLab.BossTicketCapacity}");
        SetText(offlineRewardText, $"{researchLab.BossTicketsProducedPerDay}/day");
        SetText(upgradeText, researchLab.IsUpgrading
            ? $"Upgrading {researchLab.UpgradeRemainingSeconds:0}s"
            : $"Upgrade Cost {researchLab.UpgradeCost}");
        SetText(unlockText, BuildUnlockSummary());

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = researchLab.Level;
            upgradeButton.interactable = researchLab.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
            SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
                researchLab,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, researchLab, ref observedUpgradeDuration);

        if (bossTicketButton != null)
        {
            bossTicketButton.interactable = false;
        }
    }

    private string BuildUnlockSummary()
    {
        string summary = string.Empty;

        foreach (StrategyResearchLab.FacilityUnlock item in researchLab.FacilityUnlocks)
        {
            summary += $"{item.displayName}: {(item.unlocked ? "OPEN" : $"Lv.{item.requiredLabLevel}")}\n";
        }

        return summary.TrimEnd();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        researchLab = baseCampManager != null ? baseCampManager.ResearchLab : null;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

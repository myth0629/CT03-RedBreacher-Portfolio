using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandCenterPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text unlockText;
    
    [Header("Visual")]
    [SerializeField] private Image facilityImage;
    [SerializeField] private Sprite[] levelSprites;

    private CommandCenter cmdCenter;
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
        TMP_Text level,
        TMP_Text offlineReward,
        TMP_Text upgradeLabel,
        TMP_Text unlock,
        Image targetImage,
        Sprite[] sprites)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        levelText = level;
        offlineRewardText = offlineReward;
        upgradeText = upgradeLabel;
        unlockText = unlock;
        facilityImage = targetImage;
        levelSprites = sprites;
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

        if (cmdCenter == null)
        {
            return;
        }

        SetText(levelText, $"Lv. {cmdCenter.Level}");
        UpdateFacilityVisual();
        SetText(offlineRewardText, $"* 1일당 {cmdCenter.BossTicketsProducedPerDay}티켓 지급 *");
        SetText(upgradeText, cmdCenter.IsUpgrading
            ? $"완료까지 {cmdCenter.UpgradeRemainingSeconds:0}초"
            : $"업그레이드 ({cmdCenter.UpgradeCost} 크레딧)");
        SetText(unlockText, BuildUnlockSummary());

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = cmdCenter.Level;
            upgradeButton.interactable = cmdCenter.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
            SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
                cmdCenter,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, cmdCenter, ref observedUpgradeDuration);
    }

    private void UpdateFacilityVisual()
    {
        if (facilityImage == null || levelSprites == null || levelSprites.Length == 0 || cmdCenter == null)
        {
            return;
        }

        int index = Mathf.Clamp(cmdCenter.Level - 1, 0, levelSprites.Length - 1);
        facilityImage.sprite = levelSprites[index];
        facilityImage.color = Color.white;
    }

    private string BuildUnlockSummary()
    {
        string summary = string.Empty;

        foreach (CommandCenter.FacilityUnlock item in cmdCenter.FacilityUnlocks)
        {
            summary += $"{item.displayName}: {(item.unlocked ? "잠금해제" : $"Lv.{item.requiredLabLevel} 증가")}\n";
        }

        return summary.TrimEnd();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        cmdCenter = baseCampManager != null ? baseCampManager.CommandCenter : null;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

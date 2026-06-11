using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandCenterPanel : MonoBehaviour
{
    private static readonly Color LockedColor = new Color32(0xFF, 0x39, 0x39, 0xFF);
    private static readonly Color UnlockedColor = Color.white;

    [Header("Panels")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private TMP_Text unlockText;
    
    [Header("BaseUnlockStatus")]
    [SerializeField] private TMP_Text energyRefineryUnlockText;
    [SerializeField] private TMP_Text assemblyFactoryUnlockText;
    [SerializeField] private TMP_Text coreChargerUnlockText;
    [SerializeField] private TMP_Text controlTowerUnlockText;
    
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
        RefreshBaseUnlockStatus();

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

    private void RefreshBaseUnlockStatus()
    {
        SetUnlockStatusText(energyRefineryUnlockText, "energy_refinery", "에너지 정제소");
        SetUnlockStatusText(assemblyFactoryUnlockText, "assembly_factory", "조립 공장");
        SetUnlockStatusText(coreChargerUnlockText, "core_charger", "코어 충전소");
        SetUnlockStatusText(controlTowerUnlockText, "boss_dungeon", "관제탑");
    }

    // 기지 해금과 레벨수치를 한 눈에 볼수 있는 기능
    private void SetUnlockStatusText(TMP_Text target, string facilityId, string fallbackDisplayName)
    {
        if (target == null || cmdCenter == null)
        {
            return;
        }

        CommandCenter.FacilityUnlock unlock = FindFacilityUnlock(facilityId);
        string displayName = unlock != null && !string.IsNullOrWhiteSpace(unlock.displayName)
            ? unlock.displayName
            : fallbackDisplayName;
        int requiredLevel = unlock != null ? unlock.requiredLabLevel : 1;
        bool unlocked = unlock != null && cmdCenter.Level >= requiredLevel;

        target.text = unlocked
            ? $"{displayName}: 해금됨(Lv. {requiredLevel})"
            : $"{displayName}: 해금되지 않음";
        target.color = unlocked ? UnlockedColor : LockedColor;
    }

    private CommandCenter.FacilityUnlock FindFacilityUnlock(string facilityId)
    {
        foreach (CommandCenter.FacilityUnlock item in cmdCenter.FacilityUnlocks)
        {
            if (item != null && item.facilityId == facilityId)
            {
                return item;
            }
        }

        return null;
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

    // 다음 업그레이드 시 해금요소 미리보기
    private string BuildUnlockSummary()
    {
        string summary = string.Empty;
        int nextLevel = cmdCenter.Level + 1;

        foreach (CommandCenter.FacilityUnlock item in cmdCenter.FacilityUnlocks)
        {
            if (item == null || item.unlocked || item.requiredLabLevel > nextLevel)
            {
                continue;
            }

            summary += $"{item.displayName}: 해금됨.\n";
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

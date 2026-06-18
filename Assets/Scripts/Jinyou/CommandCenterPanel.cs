using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandCenterPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BaseCampManager baseCampManager;

    [Header("Base CMD")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;

    [Header("PlayerLevel")]
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private TMP_Text playerEXPText;
    [SerializeField] private Image playerEXPProgressFill;

    [Header("BaseUnlockStatus")]
    [SerializeField] private baseUnlockStatus baseUnlockStatusPrefab;
    [SerializeField] private Transform statusList;

    [Header("Base CMD Upgrade")]
    [SerializeField] private TMP_Text commandNextLevelText;
    [SerializeField] private TMP_Text unlockDetailText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private Image upgradeProgressFill;

    [Header("Visual")]
    [SerializeField] private Image facilityImage;
    [SerializeField] private Sprite[] levelSprites;

    private readonly List<baseUnlockStatus> _baseUnlockStatusList = new List<baseUnlockStatus>();
    private CommandCenter cmdCenter;
    private PlayerProgression progression;
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
        TMP_Text upgradeLabel,
        TMP_Text unlock,
        Image targetImage,
        Sprite[] sprites)
    {
        baseCampManager = manager;
        upgradeButton = upgrade;
        levelText = level;
        upgradeText = upgradeLabel;
        unlockDetailText = unlock;
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
        RefreshPlayerProgression();

        if (cmdCenter == null)
        {
            return;
        }

        SetText(levelText, $"Lv. {cmdCenter.Level}");
        SetText(commandNextLevelText, cmdCenter.Level < cmdCenter.MaxLevel
            ? $"사령부 Lv. {cmdCenter.Level + 1}"
            : "최대 레벨");
        UpdateFacilityVisual();
        SetText(upgradeText, cmdCenter.IsUpgrading
            ? $"완료까지 {cmdCenter.UpgradeRemainingSeconds:0}초"
            : $"업그레이드 ({cmdCenter.UpgradeCost} 크레딧)");
        SetText(unlockDetailText, BuildUnlockSummary());
        RefreshBaseUnlockStatuses();

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = cmdCenter.Level;
            upgradeButton.interactable = cmdCenter.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, cmdCenter, ref observedUpgradeDuration);
    }

    // baseUnlockStatusPrefab을 연결하여 기지명(displayName), 레벨(requiredLabLevel), 언락상태(bool unlocked)를
    // FacilityUnlock에 연결해서 동기화.
    private void RefreshBaseUnlockStatuses()
    {
        if (baseUnlockStatusPrefab == null || statusList == null || cmdCenter == null)
        {
            return;
        }

        if (_baseUnlockStatusList.Count != cmdCenter.FacilityUnlocks.Count)
        {
            RebuildBaseUnlockStatuses();
        }

        foreach (CommandCenter.FacilityUnlock unlock in cmdCenter.FacilityUnlocks)
        {
            if (unlock != null)
            {
                cmdCenter.IsFacilityUnlocked(unlock.facilityId);
            }
        }

        foreach (baseUnlockStatus status in _baseUnlockStatusList)
        {
            status?.Refresh();
        }
    }

    private void RebuildBaseUnlockStatuses()
    {
        foreach (baseUnlockStatus status in _baseUnlockStatusList)
        {
            if (status != null)
            {
                Destroy(status.gameObject);
            }
        }

        _baseUnlockStatusList.Clear();
        BaseCampFacilityView[] facilityViews = FindObjectsByType<BaseCampFacilityView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CommandCenter.FacilityUnlock unlock in cmdCenter.FacilityUnlocks)
        {
            if (unlock == null)
            {
                continue;
            }

            cmdCenter.IsFacilityUnlocked(unlock.facilityId);
            BaseCampFacilityView facilityView = FindFacilityView(facilityViews, unlock.facilityId);
            baseUnlockStatus status = Instantiate(baseUnlockStatusPrefab, statusList);
            status.Configure(facilityView, unlock);
            _baseUnlockStatusList.Add(status);
        }
    }

    // baseUnlockStatusPrefab에 있는 Base_icon을 BaseCampFacilityView를 통해서 동기화
    private static BaseCampFacilityView FindFacilityView(
        BaseCampFacilityView[] facilityViews,
        string facilityId)
    {
        if (facilityViews == null)
        {
            return null;
        }

        foreach (BaseCampFacilityView facilityView in facilityViews)
        {
            if (facilityView != null && facilityView.FacilityId == facilityId)
            {
                return facilityView;
            }
        }

        return null;
    }

    // 사령부 기지 이미지 전용 비주얼 업그레이드
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

            summary += $"{item.displayName} 해금\n";
        }

        summary = summary.TrimEnd();
        return string.IsNullOrEmpty(summary)
            ? "해금 요소 없음"
            : summary;
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        cmdCenter = baseCampManager != null ? baseCampManager.CommandCenter : null;
        progression = baseCampManager != null
            ? baseCampManager.PlayerProgression
            : FindFirstObjectByType<PlayerProgression>();
    }

    private void RefreshPlayerProgression()
    {
        if (progression == null)
        {
            SetText(playerLevelText, string.Empty);
            SetText(playerEXPText, string.Empty);
            SetFill(playerEXPProgressFill, 0f);
            return;
        }

        SetText(playerLevelText, $"Lv. {progression.Level}");
        SetText(playerEXPText,
            $"{progression.CurrentExperience:0} / {progression.ExperienceToNextLevel:0}");
        SetFill(playerEXPProgressFill, progression.ExperienceProgress01);
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

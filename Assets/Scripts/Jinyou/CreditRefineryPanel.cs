using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditRefineryPanel : MonoBehaviour
{
    private const string FacilityId = "energy_refinery";

    [SerializeField] private BaseCampManager baseCampManager;

    [Header("Buttons")]
    [SerializeField] private Button collectButton;
    [SerializeField] private Button upgradeButton;

    [Header("BaseLevel")]
    [SerializeField] private TMP_Text levelText;

    [Header("Storage & Production")]
    [SerializeField] private TMP_Text storedCreditsText;
    [SerializeField] private Image refineryStorageFill;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private Image upgradeProgressFill;

    [Header("UpgradeProgress")]
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text beforeUpgradeText;
    [SerializeField] private TMP_Text afterUpgradeText;

    [Header("Visual")]
    [SerializeField] private Image facilityImage;
    [SerializeField] private Sprite[] levelSprites;

    private CreditRefinery refinery;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        collectButton?.onClick.AddListener(CollectCredits);
        upgradeButton?.onClick.AddListener(UpgradeRefinery);
        Refresh();
    }

    private void OnDisable()
    {
        collectButton?.onClick.RemoveListener(CollectCredits);
        upgradeButton?.onClick.RemoveListener(UpgradeRefinery);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        Button collect,
        Button upgrade,
        TMP_Text level,
        TMP_Text storedCredits,
        TMP_Text production,
        TMP_Text upgradeLabel,
        Image refineryFill,
        Image targetImage,
        Sprite[] sprites)
    {
        baseCampManager = manager;
        collectButton = collect;
        upgradeButton = upgrade;
        levelText = level;
        storedCreditsText = storedCredits;
        refineryStorageFill = refineryFill;
        productionText = production;
        upgradeText = upgradeLabel;
        facilityImage = targetImage;
        levelSprites = sprites;
        Refresh();
    }

    private void CollectCredits()
    {
        ResolveReferences();

        // 수집 직전의 저장량과 버튼 위치를 캡처한다(수집 후 상태가 바뀌므로).
        int collectedCredits = refinery != null ? refinery.StoredCredits : 0;
        Vector3 sourcePosition = collectButton != null ? collectButton.transform.position : Vector3.zero;

        baseCampManager?.CollectRefineryCredits();

        // 도전과제/임무와 동일하게, 수집한 크레딧 양에 따라 아이콘이 재화 표시로 날아가는 연출을 재생한다.
        if (collectedCredits > 0)
        {
            RewardFlyAnimator.Instance.PlayReward(sourcePosition, CurrencyType.Credits, collectedCredits);
        }

        Refresh();
    }

    private void UpgradeRefinery()
    {
        baseCampManager?.UpgradeEnergyRefinery();
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (refinery == null)
        {
            SetText(beforeUpgradeText, string.Empty);
            SetText(afterUpgradeText, string.Empty);
            return;
        }

        bool isStorageFull = refinery.StorageCapacity > 0 &&
                             refinery.StoredCredits >= refinery.StorageCapacity;

        UpdateFacilityVisual();
        SetText(levelText, $"Lv. {refinery.Level}");
        SetText(storedCreditsText, isStorageFull
            ? $"수집하기 (<b>{refinery.StorageCapacity}</b>)"
            : $"수집하기 (<b>{refinery.StoredCredits}</b>/{refinery.StorageCapacity})");
        SetText(productionText, $"1분 = {refinery.CreditsPerMinute:0}개 수집");
        RefreshUpgradePreview();
        SetText(upgradeText, refinery.IsUpgrading
            ? $"완료까지 {refinery.UpgradeRemainingSeconds:0}초"
            : $"업그레이드 ({refinery.UpgradeCost} 크레딧)");
        SetFill(refineryStorageFill, refinery.StorageCapacity > 0
            ? (float)refinery.StoredCredits / refinery.StorageCapacity
            : 0f);

        if (collectButton != null)
        {
            collectButton.interactable = refinery.StoredCredits > 0;
        }

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.CommandCenter != null ? baseCampManager.CommandCenter.Level : 1;
            upgradeButton.interactable = refinery.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, refinery, ref observedUpgradeDuration);
    }

    private void UpdateFacilityVisual()
    {
        if (facilityImage == null || levelSprites == null || levelSprites.Length == 0 || refinery == null)
        {
            return;
        }

        int index = Mathf.Clamp(refinery.Level - 1, 0, levelSprites.Length - 1);
        facilityImage.sprite = levelSprites[index];
        facilityImage.color = Color.white;
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        refinery = baseCampManager != null ? baseCampManager.CreditRefinery : null;
    }


    // 업그레이드 미리보기 관련 로직 (beforeUpgradeText, afterUpgradeText)
    private void RefreshUpgradePreview()
    {
        if (refinery == null)
        {
            SetText(beforeUpgradeText, string.Empty);
            SetText(afterUpgradeText, string.Empty);
            return;
        }

        BaseCampBalanceConfig.FacilityLevelData current = GetRefineryBalance(refinery.Level);
        BaseCampBalanceConfig.FacilityLevelData next = GetRefineryBalance(refinery.Level + 1);

        int currentStorage = current != null ? current.storageCapacity : refinery.StorageCapacity;
        float currentProduction = current != null ? current.creditsPerMinute : refinery.CreditsPerMinute;

        SetText(beforeUpgradeText, FormatUpgradePreview(currentStorage,
            currentProduction));

        SetText(afterUpgradeText, next != null
            ? FormatUpgradePreview(next.storageCapacity, next.creditsPerMinute)
            : "최대 레벨");
    }

    private static BaseCampBalanceConfig.FacilityLevelData GetRefineryBalance(int level)
    {
        return BaseCampBalanceConfig.Current?.GetLevel(FacilityId, level);
    }

    private static string FormatUpgradePreview(int storageCapacity, float creditsPerMinute)
    {
        return $"최대 용량: <b>{storageCapacity}</b>\n1분 = <b>{creditsPerMinute:0.##}</b>개 수집";
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

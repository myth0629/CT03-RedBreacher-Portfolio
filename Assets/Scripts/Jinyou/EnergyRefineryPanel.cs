using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyRefineryPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text storedCreditsText;
    [SerializeField] private Image refineryStorageFill;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;
    
    [Header("Visual")]
    [SerializeField] private Image facilityImage;
    [SerializeField] private Sprite[] levelSprites;

    private EnergyRefinery refinery;
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
        baseCampManager?.CollectRefineryCredits();
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
            return;
        }

        UpdateFacilityVisual();
        SetText(levelText, $"Lv. {refinery.Level}");
        SetText(storedCreditsText, $"{refinery.StoredCredits}/{refinery.StorageCapacity}");
        SetText(productionText, $"( 1분당 {refinery.CreditsPerMinute:0}개 수집 )");
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
            SetText(upgradeConditionText, BaseCampUpgradeStatus.BuildConditionText(
                refinery,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
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
        refinery = baseCampManager != null ? baseCampManager.EnergyRefinery : null;
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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyRefineryPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text storedCreditsText;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private Image upgradeProgressFill;

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
        Button close,
        TMP_Text level,
        TMP_Text storedCredits,
        TMP_Text production,
        TMP_Text upgradeLabel)
    {
        baseCampManager = manager;
        collectButton = collect;
        upgradeButton = upgrade;
        levelText = level;
        storedCreditsText = storedCredits;
        productionText = production;
        upgradeText = upgradeLabel;
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

        SetText(levelText, $"Lv. {refinery.Level}");
        SetText(storedCreditsText, $"{refinery.StoredCredits}/{refinery.StorageCapacity}");
        SetText(productionText, $"{refinery.CreditsPerMinute:0}/min");
        SetText(upgradeText, refinery.IsUpgrading
            ? $"Upgrading {refinery.UpgradeRemainingSeconds:0}s"
            : $"Upgrade Cost {refinery.UpgradeCost}");

        if (collectButton != null)
        {
            collectButton.interactable = refinery.StoredCredits > 0;
        }

        if (upgradeButton != null && baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.ResearchLab != null ? baseCampManager.ResearchLab.Level : 1;
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
}

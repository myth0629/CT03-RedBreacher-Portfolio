using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitPointFacilityPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private TraitPointFacility traitPointFacility;
    [SerializeField] private TMP_Text availablePointText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text maxHealthText;
    [SerializeField] private TMP_Text critChanceText;
    [SerializeField] private TMP_Text critMultiplierText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button maxHealthButton;
    [SerializeField] private Button critChanceButton;
    [SerializeField] private Button critMultiplierButton;
    [SerializeField] private Button resetButton;

    private void OnEnable()
    {
        ResolveReferences();
        attackButton?.onClick.AddListener(InvestAttack);
        maxHealthButton?.onClick.AddListener(InvestMaxHealth);
        critChanceButton?.onClick.AddListener(InvestCritChance);
        critMultiplierButton?.onClick.AddListener(InvestCritMultiplier);
        resetButton?.onClick.AddListener(ResetTraits);
        Refresh();
    }

    private void OnDisable()
    {
        attackButton?.onClick.RemoveListener(InvestAttack);
        maxHealthButton?.onClick.RemoveListener(InvestMaxHealth);
        critChanceButton?.onClick.RemoveListener(InvestCritChance);
        critMultiplierButton?.onClick.RemoveListener(InvestCritMultiplier);
        resetButton?.onClick.RemoveListener(ResetTraits);
    }

    private void Update()
    {
        Refresh();
    }

    public void InvestAttack()
    {
        Invest(TraitPointFacility.TraitStat.AttackDamage);
    }

    public void InvestMaxHealth()
    {
        Invest(TraitPointFacility.TraitStat.MaxHealth);
    }

    public void InvestCritChance()
    {
        Invest(TraitPointFacility.TraitStat.CritChance);
    }

    public void InvestCritMultiplier()
    {
        Invest(TraitPointFacility.TraitStat.CritMultiplier);
    }

    public void ResetTraits()
    {
        ResolveReferences();
        traitPointFacility?.ResetAllocatedPoints();
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();
        if (traitPointFacility == null)
        {
            return;
        }

        int availablePoints = traitPointFacility.AvailableTraitPoints;
        SetText(availablePointText, $"Trait Points {availablePoints}");
        SetText(attackText, BuildStatLine(TraitPointFacility.TraitStat.AttackDamage));
        SetText(maxHealthText, BuildStatLine(TraitPointFacility.TraitStat.MaxHealth));
        SetText(critChanceText, BuildStatLine(TraitPointFacility.TraitStat.CritChance));
        SetText(critMultiplierText, BuildStatLine(TraitPointFacility.TraitStat.CritMultiplier));
        SetText(summaryText, traitPointFacility.BuildSummary());

        SetInvestButton(attackButton, availablePoints);
        SetInvestButton(maxHealthButton, availablePoints);
        SetInvestButton(critChanceButton, availablePoints);
        SetInvestButton(critMultiplierButton, availablePoints);
        if (resetButton != null)
        {
            resetButton.interactable = traitPointFacility.AttackDamagePoints
                + traitPointFacility.MaxHealthPoints
                + traitPointFacility.CritChancePoints
                + traitPointFacility.CritMultiplierPoints > 0;
        }
    }

    private void Invest(TraitPointFacility.TraitStat stat)
    {
        ResolveReferences();
        traitPointFacility?.TryInvest(stat);
        Refresh();
    }

    private string BuildStatLine(TraitPointFacility.TraitStat stat)
    {
        int points = traitPointFacility.GetAllocatedPoints(stat);
        float bonus = traitPointFacility.GetBonusValue(stat) * 100f;
        return $"{TraitPointFacility.GetStatDisplayName(stat)} Lv.{points}  +{bonus:0.#}%";
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        traitPointFacility ??= baseCampManager != null
            ? baseCampManager.TraitPointFacility
            : FindFirstObjectByType<TraitPointFacility>();
    }

    private static void SetInvestButton(Button button, int availablePoints)
    {
        if (button != null)
        {
            button.interactable = availablePoints > 0;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseCampHud : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private TMP_Text commanderLevelText;
    [SerializeField] private TMP_Text bossTicketText;
    [SerializeField] private TMP_Text refineryStorageText;
    [SerializeField] private Image refineryStorageFill;

    private void OnEnable()
    {
        ResolveReferences();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
        TMP_Text credits,
        TMP_Text commanderLevel,
        TMP_Text bossTicket,
        TMP_Text refineryStorage,
        Image refineryFill)
    {
        baseCampManager = manager;
        creditsText = credits;
        commanderLevelText = commanderLevel;
        bossTicketText = bossTicket;
        refineryStorageText = refineryStorage;
        refineryStorageFill = refineryFill;
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (baseCampManager == null)
        {
            SetText(creditsText, "--");
            SetText(commanderLevelText, "Commander Lv. --");
            SetText(bossTicketText, "Tickets --/--");
            SetText(refineryStorageText, "--/--");
            SetFill(refineryStorageFill, 0f);
            return;
        }

        StrategyResearchLab researchLab = baseCampManager.ResearchLab;
        EnergyRefinery refinery = baseCampManager.EnergyRefinery;

        SetText(creditsText, $"{baseCampManager.Credits}");
        SetText(commanderLevelText, $"Commander Lv. {baseCampManager.CommanderLevel}");

        if (researchLab != null)
        {
            SetText(bossTicketText, $"Tickets {researchLab.BossTickets}/{researchLab.BossTicketCapacity}");
        }
        else
        {
            SetText(bossTicketText, "Tickets --/--");
        }

        if (refinery != null)
        {
            SetText(refineryStorageText, $"{refinery.StoredCredits}/{refinery.StorageCapacity}");
            SetFill(refineryStorageFill, refinery.StorageCapacity > 0
                ? (float)refinery.StoredCredits / refinery.StorageCapacity
                : 0f);
        }
        else
        {
            SetText(refineryStorageText, "Refinery --/--");
            SetFill(refineryStorageFill, 0f);
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
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

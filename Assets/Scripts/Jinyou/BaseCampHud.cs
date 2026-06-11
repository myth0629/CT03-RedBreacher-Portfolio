using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseCampHud : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    
    [Header("Commander")]
    [SerializeField] private TMP_Text commanderLevelText;
    
    [Header("Boss Ticket")]
    [SerializeField] private TMP_Text bossTicketText;
    
    [Header("Refinery Storage")]
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
            SetText(commanderLevelText, "지휘관 Lv. --");
            SetText(bossTicketText, "티켓 --/--");
            SetText(refineryStorageText, "--/--");
            SetFill(refineryStorageFill, 0f);
            return;
        }

        CommandCenter researchLab = baseCampManager.CommandCenter;
        EnergyRefinery refinery = baseCampManager.EnergyRefinery;
        SetText(commanderLevelText, $"지휘관 Lv. {baseCampManager.CommanderLevel}");

        if (researchLab != null)
        {
            SetText(bossTicketText, $"티켓 {researchLab.BossTickets}/{researchLab.BossTicketCapacity}");
        }
        else
        {
            SetText(bossTicketText, "티켓 --/--");
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
            SetText(refineryStorageText, "정제소 용량 --/--");
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

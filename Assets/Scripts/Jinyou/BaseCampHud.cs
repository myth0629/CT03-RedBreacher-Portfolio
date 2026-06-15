using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseCampHud : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;

    [Header("Commander")]
    [SerializeField] private TMP_Text commanderLevelText;
    [SerializeField] private TMP_Text commandCenterLevelText;

    [Header("Boss Ticket")]
    [SerializeField] private TMP_Text bossTicketText;

    [Header("Refinery Storage")]
    [SerializeField] private TMP_Text refineryStorageText;
    [SerializeField] private Image refineryStorageFill;
    [SerializeField] private Button collectButton;

    [Header("BaseUnlockStatus")]
    [SerializeField] private TMP_Text energyRefineryUnlockText;
    [SerializeField] private TMP_Text assemblyFactoryUnlockText;
    [SerializeField] private GameObject assemblyFactoryUnlockPanel;
    [SerializeField] private TMP_Text coreChargerUnlockText;
    [SerializeField] private GameObject coreChargerUnlockPanel;
    [SerializeField] private TMP_Text controlTowerUnlockText;

    private void OnEnable()
    {
        ResolveReferences();
        collectButton?.onClick.AddListener(CollectCredits);
        Refresh();
    }

    private void OnDisable()
    {
        collectButton?.onClick.RemoveListener(CollectCredits);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(
        BaseCampManager manager,
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
            SetText(commandCenterLevelText, "사령부 Lv. --");
            SetText(commanderLevelText, "지휘관 Lv. --");
            SetText(bossTicketText, "티켓 --/--");
            SetText(refineryStorageText, "--/--");
            SetFill(refineryStorageFill, 0f);
            SetButtonInteractable(collectButton, false);
            RefreshBaseUnlockStatus();
            return;
        }

        CommandCenter researchLab = baseCampManager.CommandCenter;
        CreditRefinery refinery = baseCampManager.CreditRefinery;
        SetText(commanderLevelText, $"지휘관 Lv. {baseCampManager.CommanderLevel}");
        SetText(commandCenterLevelText, researchLab != null
            ? $"사령부 Lv. {researchLab.Level}"
            : "사령부 Lv. --");

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
            float storageRate = refinery.StorageCapacity > 0
                ? (float)refinery.StoredCredits / refinery.StorageCapacity
                : 0f;
            bool isStorageFull = refinery.StorageCapacity > 0 &&
                                 refinery.StoredCredits >= refinery.StorageCapacity;

            SetText(refineryStorageText, isStorageFull
                ? $"가득참 ({refinery.StorageCapacity})"
                : $"{refinery.StoredCredits}/{refinery.StorageCapacity}");
            SetFill(refineryStorageFill, storageRate);
            SetButtonInteractable(collectButton, refinery.StoredCredits > 0);
        }
        else
        {
            SetText(refineryStorageText, "--/--");
            SetFill(refineryStorageFill, 0f);
            SetButtonInteractable(collectButton, false);
        }

        RefreshBaseUnlockStatus();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
    }


    private void CollectCredits()
    {
        baseCampManager?.CollectRefineryCredits();
        Refresh();
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


    private static void SetButtonInteractable(Button target, bool interactable)
    {
        if (target != null)
        {
            target.interactable = interactable;
        }
    }

    private void RefreshBaseUnlockStatus()
    {
        CommandCenter commandCenter = baseCampManager != null ? baseCampManager.CommandCenter : null;
        SetUnlockStatusText(energyRefineryUnlockText, commandCenter, "energy_refinery");
        SetUnlockStatusText(assemblyFactoryUnlockText, commandCenter, "assembly_factory", assemblyFactoryUnlockPanel);
        SetUnlockStatusText(coreChargerUnlockText, commandCenter, "core_charger", coreChargerUnlockPanel);
        SetUnlockStatusText(controlTowerUnlockText, commandCenter, "boss_dungeon");
    }

    private void SetUnlockStatusText(
        TMP_Text target,
        CommandCenter commandCenter,
        string facilityId,
        GameObject unlockPanel = null)
    {
        if (target == null)
        {
            SetActive(unlockPanel, false);
            return;
        }

        CommandCenter.FacilityUnlock unlock = FindFacilityUnlock(commandCenter, facilityId);
        if (commandCenter == null || unlock == null)
        {
            target.text = string.Empty;
            SetActive(unlockPanel, false);
            return;
        }

        int requiredLevel = unlock.requiredLabLevel;
        bool unlocked = commandCenter.IsFacilityUnlocked(facilityId);

        target.text = unlocked ? string.Empty : $"<color=#ED3724>잠금</color>\n사령부 Lv.{requiredLevel} 필요";
        SetActive(unlockPanel, !unlocked);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private static CommandCenter.FacilityUnlock FindFacilityUnlock(CommandCenter commandCenter, string facilityId)
    {
        if (commandCenter == null)
        {
            return null;
        }

        foreach (CommandCenter.FacilityUnlock item in commandCenter.FacilityUnlocks)
        {
            if (item != null && item.facilityId == facilityId)
            {
                return item;
            }
        }

        return null;
    }
}

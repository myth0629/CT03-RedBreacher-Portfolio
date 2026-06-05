using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseCampFacilityView : MonoBehaviour, IPointerClickHandler
{
    public enum FacilityType
    {
        StrategyResearchLab,
        EnergyRefinery,
        AssemblyFactory,
        CoreCharger,
        TraitPointFacility,
        Inventory,
        WeaponGacha,
        BossDungeon
    }

    [Header("Facility")]
    [SerializeField] private FacilityType facilityType;
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private GameObject facilityPanel;
    [SerializeField] private Button facilityButton;

    [Header("Visual")]
    [SerializeField] private Image facilityImage;
    [SerializeField] private Sprite[] levelSprites;

    [Header("Events")]
    public UnityEvent OnSelected = new UnityEvent();

    private int lastSelectFrame = -1;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeFacilityEvents();
        facilityButton?.onClick.AddListener(SelectFacility);
        SyncView();
    }

    private void OnDisable()
    {
        UnsubscribeFacilityEvents();
        facilityButton?.onClick.RemoveListener(SelectFacility);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectFacility();
    }

    public void SelectFacility()
    {
        if (lastSelectFrame == Time.frameCount)
        {
            return;
        }

        lastSelectFrame = Time.frameCount;
        ResolveReferences();

        if (!IsUnlocked())
        {
            return;
        }

        if (baseCampManager != null)
        {
            baseCampManager.OpenPanel(facilityPanel);
        }
        else if (facilityPanel != null)
        {
            facilityPanel.SetActive(true);
        }

        OnSelected.Invoke();
    }

    public void Configure(
        FacilityType type,
        BaseCampManager manager,
        GameObject panel,
        Image targetImage,
        Sprite[] sprites)
    {
        if (isActiveAndEnabled)
        {
            UnsubscribeFacilityEvents();
        }

        facilityType = type;
        baseCampManager = manager;
        facilityPanel = panel;
        facilityImage = targetImage;
        levelSprites = sprites;

        if (isActiveAndEnabled)
        {
            SubscribeFacilityEvents();
        }

        SyncView();
    }

    public void SyncView()
    {
        UpdateVisual();
        UpdateInteractable();
    }

    public void UpdateVisual()
    {
        if (facilityImage == null || levelSprites == null || levelSprites.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(GetCurrentLevel() - 1, 0, levelSprites.Length - 1);
        facilityImage.sprite = levelSprites[index];
        facilityImage.color = IsUnlocked() ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        UpdateInteractable();
    }

    private int GetCurrentLevel()
    {
        ResolveReferences();

        return facilityType switch
        {
            FacilityType.StrategyResearchLab => baseCampManager?.ResearchLab?.Level ?? 1,
            FacilityType.EnergyRefinery => baseCampManager?.EnergyRefinery?.Level ?? 1,
            FacilityType.AssemblyFactory => baseCampManager?.AssemblyFactory?.Level ?? 1,
            FacilityType.CoreCharger => baseCampManager?.CoreCharger?.Level ?? 1,
            FacilityType.TraitPointFacility => 1,
            FacilityType.Inventory => 1,
            FacilityType.WeaponGacha => 1,
            FacilityType.BossDungeon => baseCampManager?.ResearchLab?.Level ?? 1,
            _ => 1
        };
    }

    private bool IsUnlocked()
    {
        ResolveReferences();

        if (facilityType == FacilityType.StrategyResearchLab
            || facilityType == FacilityType.Inventory
            || facilityType == FacilityType.WeaponGacha)
        {
            return true;
        }

        CommandCenter researchLab = baseCampManager != null ? baseCampManager.ResearchLab : null;
        if (researchLab == null)
        {
            return true;
        }

        return researchLab.IsFacilityUnlocked(GetFacilityId());
    }

    private string GetFacilityId()
    {
        return facilityType switch
        {
            FacilityType.EnergyRefinery => "energy_refinery",
            FacilityType.AssemblyFactory => "assembly_factory",
            FacilityType.CoreCharger => "core_charger",
            FacilityType.TraitPointFacility => "trait_point_facility",
            FacilityType.Inventory => "inventory",
            FacilityType.WeaponGacha => "weapon_gacha",
            FacilityType.BossDungeon => "boss_dungeon",
            _ => string.Empty
        };
    }

    private void UpdateInteractable()
    {
        if (facilityButton != null)
        {
            facilityButton.interactable = IsUnlocked();
        }
    }

    private void SubscribeFacilityEvents()
    {
        UnsubscribeFacilityEvents();
        ResolveReferences();

        switch (facilityType)
        {
            case FacilityType.StrategyResearchLab:
                if (baseCampManager?.ResearchLab != null)
                {
                    baseCampManager.ResearchLab.OnLevelChanged.AddListener(HandleLevelChanged);
                    baseCampManager.ResearchLab.OnUpgradeStarted.AddListener(SyncView);
                    baseCampManager.ResearchLab.OnUpgradeCompleted.AddListener(SyncView);
                }
                break;
            case FacilityType.EnergyRefinery:
                SubscribeResearchLabUnlockEvents();
                if (baseCampManager?.EnergyRefinery != null)
                {
                    baseCampManager.EnergyRefinery.OnLevelChanged.AddListener(HandleLevelChanged);
                    baseCampManager.EnergyRefinery.OnUpgradeStarted.AddListener(SyncView);
                    baseCampManager.EnergyRefinery.OnUpgradeCompleted.AddListener(SyncView);
                }
                break;
            case FacilityType.AssemblyFactory:
                SubscribeResearchLabUnlockEvents();
                if (baseCampManager?.AssemblyFactory != null)
                {
                    baseCampManager.AssemblyFactory.OnLevelChanged.AddListener(HandleLevelChanged);
                    baseCampManager.AssemblyFactory.OnUpgradeStarted.AddListener(SyncView);
                    baseCampManager.AssemblyFactory.OnUpgradeCompleted.AddListener(SyncView);
                }
                break;
            case FacilityType.CoreCharger:
                SubscribeResearchLabUnlockEvents();
                if (baseCampManager?.CoreCharger != null)
                {
                    baseCampManager.CoreCharger.OnLevelChanged.AddListener(HandleLevelChanged);
                    baseCampManager.CoreCharger.OnUpgradeStarted.AddListener(SyncView);
                    baseCampManager.CoreCharger.OnUpgradeCompleted.AddListener(SyncView);
                }
                break;
            case FacilityType.TraitPointFacility:
                SubscribeResearchLabUnlockEvents();
                break;
            case FacilityType.Inventory:
                break;
            case FacilityType.WeaponGacha:
                break;
            case FacilityType.BossDungeon:
                SubscribeResearchLabUnlockEvents();
                break;
        }
    }

    private void UnsubscribeFacilityEvents()
    {
        if (baseCampManager == null)
        {
            return;
        }

        if (baseCampManager.ResearchLab != null)
        {
            baseCampManager.ResearchLab.OnLevelChanged.RemoveListener(HandleLevelChanged);
            baseCampManager.ResearchLab.OnUpgradeStarted.RemoveListener(SyncView);
            baseCampManager.ResearchLab.OnUpgradeCompleted.RemoveListener(SyncView);
        }

        if (baseCampManager.EnergyRefinery != null)
        {
            baseCampManager.EnergyRefinery.OnLevelChanged.RemoveListener(HandleLevelChanged);
            baseCampManager.EnergyRefinery.OnUpgradeStarted.RemoveListener(SyncView);
            baseCampManager.EnergyRefinery.OnUpgradeCompleted.RemoveListener(SyncView);
        }

        if (baseCampManager.AssemblyFactory != null)
        {
            baseCampManager.AssemblyFactory.OnLevelChanged.RemoveListener(HandleLevelChanged);
            baseCampManager.AssemblyFactory.OnUpgradeStarted.RemoveListener(SyncView);
            baseCampManager.AssemblyFactory.OnUpgradeCompleted.RemoveListener(SyncView);
        }

        if (baseCampManager.CoreCharger != null)
        {
            baseCampManager.CoreCharger.OnLevelChanged.RemoveListener(HandleLevelChanged);
            baseCampManager.CoreCharger.OnUpgradeStarted.RemoveListener(SyncView);
            baseCampManager.CoreCharger.OnUpgradeCompleted.RemoveListener(SyncView);
        }
    }

    private void SubscribeResearchLabUnlockEvents()
    {
        if (baseCampManager?.ResearchLab == null)
        {
            return;
        }

        baseCampManager.ResearchLab.OnLevelChanged.AddListener(HandleLevelChanged);
        baseCampManager.ResearchLab.OnUpgradeCompleted.AddListener(SyncView);
    }

    private void HandleLevelChanged(int level)
    {
        SyncView();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        facilityImage ??= GetComponent<Image>();
        facilityButton ??= GetComponent<Button>() ?? GetComponentInChildren<Button>();
    }

}

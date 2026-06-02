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
        facilityButton?.onClick.AddListener(SelectFacility);
        UpdateVisual();
        UpdateInteractable();
    }

    private void OnDisable()
    {
        facilityButton?.onClick.RemoveListener(SelectFacility);
    }

    private void Update()
    {
        UpdateVisual();
        UpdateInteractable();
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
        facilityType = type;
        baseCampManager = manager;
        facilityPanel = panel;
        facilityImage = targetImage;
        levelSprites = sprites;
        UpdateVisual();
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
            FacilityType.BossDungeon => baseCampManager?.ResearchLab?.Level ?? 1,
            _ => 1
        };
    }

    private bool IsUnlocked()
    {
        ResolveReferences();

        if (facilityType == FacilityType.StrategyResearchLab)
        {
            return true;
        }

        StrategyResearchLab researchLab = baseCampManager != null ? baseCampManager.ResearchLab : null;
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

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        facilityImage ??= GetComponent<Image>();
        facilityButton ??= GetComponent<Button>() ?? GetComponentInChildren<Button>();
    }

}

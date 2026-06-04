using UnityEngine;
using UnityEngine.Events;

public class BaseCampManager : MonoBehaviour
{
    public static BaseCampManager Instance { get; private set; }

    [Header("Facilities")]
    [SerializeField] private StrategyResearchLab researchLab;
    [SerializeField] private EnergyRefinery energyRefinery;
    [SerializeField] private AssemblyFactory assemblyFactory;
    [SerializeField] private CoreCharger coreCharger;
    [SerializeField] private BossDungeon bossDungeon;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private bool autoFindFacilities = true;

    [Header("Facility Panels")]
    [SerializeField] private GameObject[] facilityPanels;
    [SerializeField] private bool closePanelsOnStart = true;

    [Header("Player State")]
    [SerializeField] private int commanderLevel = 1;
    [SerializeField] private int credits = 500;

    [Header("Debug")]
    [SerializeField] private bool showDebugPanel = true;
    [SerializeField] private Rect debugPanelRect = new Rect(16f, 16f, 280f, 220f);
    [SerializeField] private int debugStatPointsToAdd = 1;

    [Header("Events")]
    public UnityEvent<int> OnCreditsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnCommanderLevelChanged = new UnityEvent<int>();

    public StrategyResearchLab ResearchLab => researchLab;
    public EnergyRefinery EnergyRefinery => energyRefinery;
    public AssemblyFactory AssemblyFactory => assemblyFactory;
    public CoreCharger CoreCharger => coreCharger;
    public BossDungeon BossDungeon => bossDungeon;
    public PlayerProgression PlayerProgression => playerProgression;
    public int CommanderLevel => commanderLevel;
    public int Credits => credits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ConnectFacilities();
    }

    private void Start()
    {
        if (closePanelsOnStart)
        {
            CloseAllPanels();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("Connect Facilities")]
    public void ConnectFacilities()
    {
        if (!autoFindFacilities)
        {
            return;
        }

        researchLab ??= FindFirstObjectByType<StrategyResearchLab>();
        energyRefinery ??= FindFirstObjectByType<EnergyRefinery>();
        assemblyFactory ??= FindFirstObjectByType<AssemblyFactory>();
        coreCharger ??= FindFirstObjectByType<CoreCharger>();
        bossDungeon ??= FindFirstObjectByType<BossDungeon>();
        playerProgression ??= FindFirstObjectByType<PlayerProgression>();
    }

    public void CollectRefineryCredits()
    {
        if (energyRefinery != null)
        {
            AddCredits(energyRefinery.CollectCredits());
        }
    }

    public void UpgradeResearchLab()
    {
        TrySpendAndUpgrade(researchLab);
    }

    public void UpgradeEnergyRefinery()
    {
        TrySpendAndUpgrade(energyRefinery);
    }

    public void UpgradeAssemblyFactory()
    {
        TrySpendAndUpgrade(assemblyFactory);
    }

    public void UpgradeCoreCharger()
    {
        TrySpendAndUpgrade(coreCharger);
    }

    public void SelectAssemblyMenu(string menuId)
    {
        assemblyFactory?.TrySelectMenu(menuId);
    }

    public void SelectCoreRoute(string routeId)
    {
        coreCharger?.TrySelectRoute(routeId);
    }

    public void SelectCoreOption(string optionId)
    {
        coreCharger?.TrySelectOption(optionId);
    }

    public void InvestCoreRoute(string routeId)
    {
        coreCharger?.TryInvestRoute(routeId);
    }

    public void InvestCoreOption(string optionId)
    {
        coreCharger?.TryInvestOption(optionId);
    }

    public void UseBossTicket()
    {
        // Boss tickets are produced by the research lab and consumed only by BossDungeon.
    }

    public void OpenPanel(GameObject panel)
    {
        CloseAllPanels();

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    public void CloseAllPanels()
    {
        foreach (GameObject panel in facilityPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }

    public void AddCredits(int amount)
    {
        SetCredits(credits + Mathf.Max(0, amount));
    }

    public void AddCommanderLevel(int amount)
    {
        SetCommanderLevel(commanderLevel + Mathf.Max(0, amount));
    }

    public void AddPlayerStatPoints(int amount)
    {
        (playerProgression ??= FindFirstObjectByType<PlayerProgression>())?.AddStatPoints(amount);
    }

    public void SetCommanderLevel(int value)
    {
        commanderLevel = Mathf.Max(1, value);
        OnCommanderLevelChanged.Invoke(commanderLevel);
    }

    private void SetCredits(int value)
    {
        credits = Mathf.Max(0, value);
        OnCreditsChanged.Invoke(credits);
    }

    private void TrySpendAndUpgrade(IBaseCampFacility facility)
    {
        if (facility == null)
        {
            return;
        }

        int availableCredits = credits;
        int researchLabLevel = researchLab != null ? researchLab.Level : 1;

        if (facility.TryStartUpgrade(ref availableCredits, commanderLevel, researchLabLevel))
        {
            SetCredits(availableCredits);
        }
    }

    private void OnGUI()
    {
        if (showDebugPanel)
        {
            debugPanelRect = GUILayout.Window(GetInstanceID(), debugPanelRect, DrawDebugPanel, "Base Camp");
        }
    }

    private void DrawDebugPanel(int windowId)
    {
        GUILayout.Label($"Commander Lv. {commanderLevel}");
        GUILayout.Label($"Credits: {credits}");
        GUILayout.Label($"Stat Points: {(playerProgression != null ? playerProgression.StatPoints : 0)}");

        if (GUILayout.Button("+ Level")) AddCommanderLevel(1);
        if (GUILayout.Button("+ 1000 Credits")) AddCredits(1000);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Add Stat Points", GUILayout.Width(110f));
        string statPointInput = GUILayout.TextField(debugStatPointsToAdd.ToString(), GUILayout.Width(48f));
        if (int.TryParse(statPointInput, out int parsedStatPoints))
        {
            debugStatPointsToAdd = Mathf.Max(0, parsedStatPoints);
        }

        if (GUILayout.Button("Add")) AddPlayerStatPoints(debugStatPointsToAdd);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Collect Refinery")) CollectRefineryCredits();
        if (GUILayout.Button("Upgrade Research")) UpgradeResearchLab();
        if (GUILayout.Button("Upgrade Refinery")) UpgradeEnergyRefinery();
        if (GUILayout.Button("Upgrade Assembly")) UpgradeAssemblyFactory();
        if (GUILayout.Button("Upgrade Core")) UpgradeCoreCharger();

        GUI.DragWindow();
    }
}

public interface IBaseCampFacility
{
    int Level { get; }
    int MaxLevel { get; }
    int UpgradeCost { get; }
    int RequiredCommanderLevel { get; }
    int RequiredResearchLabLevel { get; }
    bool IsUpgrading { get; }
    float UpgradeRemainingSeconds { get; }
    float CurrentUpgradeDurationSeconds { get; }
    int GetLevelLimit(int researchLabLevel);
    bool CanUpgrade(int credits, int commanderLevel);
    bool CanStartUpgrade(int credits, int commanderLevel, int researchLabLevel);
    bool TryStartUpgrade(ref int availableCredits, int commanderLevel, int researchLabLevel);
    void Upgrade();
}

using UnityEngine;
using UnityEngine.Events;

public class BaseCampManager : MonoBehaviour
{
    public static BaseCampManager Instance { get; private set; }

    [Header("Facilities")]
    [SerializeField] private CommandCenter researchLab;
    [SerializeField] private EnergyRefinery energyRefinery;
    [SerializeField] private AssemblyFactory assemblyFactory;
    [SerializeField] private CoreCharger coreCharger;
    [SerializeField] private TraitPointFacility traitPointFacility;
    [SerializeField] private InventoryFacility inventory;
    [SerializeField] private bool autoFindFacilities = true;

    [Header("Facility Panels")]
    [SerializeField] private GameObject[] facilityPanels;
    [SerializeField] private bool closePanelsOnStart = true;

    [Header("Player State")]
    [SerializeField] private int commanderLevel = 1;
    [SerializeField] private int credits = 500;
    [SerializeField] private PlayerCurrencyWallet currencyWallet;
    [SerializeField] private PlayerProgression playerProgression;

    [Header("Debug")]
    [SerializeField] private bool showDebugPanel = true;
    [SerializeField] private Rect debugPanelRect = new Rect(16f, 16f, 280f, 220f);

    [Header("Events")]
    public UnityEvent<int> OnCreditsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnCoreCrystalsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnCommanderLevelChanged = new UnityEvent<int>();

    private PlayerCurrencyWallet registeredCurrencyWallet;

    public CommandCenter ResearchLab => researchLab;
    public EnergyRefinery EnergyRefinery => energyRefinery;
    public AssemblyFactory AssemblyFactory => assemblyFactory;
    public CoreCharger CoreCharger => coreCharger;
    public TraitPointFacility TraitPointFacility => ResolveTraitPointFacility();
    public InventoryFacility Inventory => ResolveInventory();
    public int CommanderLevel => commanderLevel;
    public int Credits => CurrencyWallet.Credits;
    public int CoreCrystals => CurrencyWallet.CoreCrystals;
    public PlayerCurrencyWallet CurrencyWallet => EnsureCurrencyWallet();
    public PlayerProgression PlayerProgression => ResolvePlayerProgression();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureCurrencyWallet();
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
        if (registeredCurrencyWallet != null)
        {
            registeredCurrencyWallet.OnCreditsChanged.RemoveListener(HandleCreditsChanged);
            registeredCurrencyWallet.OnCoreCrystalsChanged.RemoveListener(HandleCoreCrystalsChanged);
            registeredCurrencyWallet = null;
        }

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

        researchLab ??= FindFirstObjectByType<CommandCenter>();
        energyRefinery ??= FindFirstObjectByType<EnergyRefinery>();
        assemblyFactory ??= FindFirstObjectByType<AssemblyFactory>();
        coreCharger ??= FindFirstObjectByType<CoreCharger>();
        traitPointFacility ??= FindFirstObjectByType<TraitPointFacility>();
        inventory ??= InventoryFacility.FindAny();
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

    public void SelectAssemblyWeapon(int weaponIndex)
    {
        assemblyFactory?.TrySelectWeapon(weaponIndex);
    }

    public void SelectAssemblyWeapon(ProjectileConfig weaponConfig)
    {
        assemblyFactory?.TrySelectWeapon(weaponConfig);
    }

    public void EnhanceAssemblyWeapon()
    {
        if (assemblyFactory == null)
        {
            return;
        }

        int availableCredits = Credits;
        if (assemblyFactory.TryEnhanceSelectedWeapon(ref availableCredits))
        {
            SetCreditsForFacility(availableCredits);
        }
    }

    public void SelectCoreUnit(int unitIndex)
    {
        coreCharger?.TrySelectUnit(unitIndex);
    }

    public void SelectCoreUnit(PlayerUnitConfig unitConfig)
    {
        coreCharger?.TrySelectUnit(unitConfig);
    }

    public void EnhanceCoreUnit()
    {
        if (coreCharger == null)
        {
            return;
        }

        int availableCredits = Credits;
        if (coreCharger.TryEnhanceSelectedUnit(ref availableCredits))
        {
            SetCreditsForFacility(availableCredits);
        }
    }

    public void InvestTraitPoint(TraitPointFacility.TraitStat stat)
    {
        TraitPointFacility?.TryInvest(stat);
    }

    public void UseBossTicket()
    {
        researchLab?.TryUseBossTicket();
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
        CurrencyWallet.AddCredits(amount);
    }

    public void AddCoreCrystals(int amount)
    {
        CurrencyWallet.AddCoreCrystals(amount);
    }

    public void AddCommanderLevel(int amount)
    {
        SetCommanderLevel(commanderLevel + Mathf.Max(0, amount));
    }

    public void SetCreditsForFacility(int value)
    {
        SetCredits(value);
    }

    public void SetCommanderLevel(int value)
    {
        commanderLevel = Mathf.Max(1, value);
        OnCommanderLevelChanged.Invoke(commanderLevel);
    }

    private void SetCredits(int value)
    {
        CurrencyWallet.SetCredits(value);
    }

    private void TrySpendAndUpgrade(IBaseCampFacility facility)
    {
        if (facility == null)
        {
            return;
        }

        int availableCredits = Credits;
        int researchLabLevel = researchLab != null ? researchLab.Level : 1;

        if (!facility.CanStartUpgrade(availableCredits, commanderLevel, researchLabLevel))
        {
            return;
        }

        // 시설은 타이머만 시작하고, 실제 재화 차감은 wallet 한 곳에서 처리한다.
        int simulatedCredits = availableCredits;
        if (CurrencyWallet.CanSpend(CurrencyType.Credits, facility.UpgradeCost)
            && facility.TryStartUpgrade(ref simulatedCredits, commanderLevel, researchLabLevel))
        {
            CurrencyWallet.TrySpend(CurrencyType.Credits, facility.UpgradeCost);
        }
    }

    private void OnGUI()
    {
        if (showDebugPanel)
        {
            debugPanelRect.height = Mathf.Max(debugPanelRect.height, 260f);
            debugPanelRect = GUILayout.Window(GetInstanceID(), debugPanelRect, DrawDebugPanel, "Base Camp");
        }
    }

    private void DrawDebugPanel(int windowId)
    {
        GUILayout.Label($"Commander Lv. {commanderLevel}");
        GUILayout.Label($"Credits: {Credits}");
        GUILayout.Label($"Core Crystals: {CoreCrystals}");

        if (GUILayout.Button("+ Level")) AddCommanderLevel(1);
        if (GUILayout.Button("+ 1000 Credits")) AddCredits(1000);
        if (GUILayout.Button("+ 100 Core Crystals")) AddCoreCrystals(100);
        if (GUILayout.Button("Collect Refinery")) CollectRefineryCredits();
        if (GUILayout.Button("Upgrade Research")) UpgradeResearchLab();
        if (GUILayout.Button("Upgrade Refinery")) UpgradeEnergyRefinery();
        if (GUILayout.Button("Upgrade Assembly")) UpgradeAssemblyFactory();
        if (GUILayout.Button("Upgrade Core")) UpgradeCoreCharger();
        if (GUILayout.Button("Reset Level/Stage")) ResetLevelAndStageDebug();

        GUI.DragWindow();
    }

    private void ResetLevelAndStageDebug()
    {
        ResetPlayerProgressionDebug();
        ResetStageProgressDebug();
    }

    private void ResetPlayerProgressionDebug()
    {
        PlayerProgression progression = FindFirstObjectByType<PlayerProgression>();
        if (progression != null)
        {
            progression.ResetProgression();
        }

        PlayerStatAllocator statAllocator = FindFirstObjectByType<PlayerStatAllocator>();
        if (statAllocator != null)
        {
            statAllocator.ResetAllocations();
        }
    }

    private void ResetStageProgressDebug()
    {
        EnemySpawnManager spawnManager = FindFirstObjectByType<EnemySpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.ResetStageProgress();
        }
    }

    private PlayerCurrencyWallet EnsureCurrencyWallet()
    {
        if (currencyWallet != null)
        {
            RegisterCurrencyWalletEvents();
            return currencyWallet;
        }

        currencyWallet = GetComponent<PlayerCurrencyWallet>();
        if (currencyWallet == null)
        {
            currencyWallet = FindFirstObjectByType<PlayerCurrencyWallet>();
        }

        if (currencyWallet == null)
        {
            currencyWallet = gameObject.AddComponent<PlayerCurrencyWallet>();
        }

        RegisterCurrencyWalletEvents();
        return currencyWallet;
    }

    private PlayerProgression ResolvePlayerProgression()
    {
        if (playerProgression != null)
        {
            return playerProgression;
        }

        playerProgression = FindFirstObjectByType<PlayerProgression>();
        return playerProgression;
    }

    private InventoryFacility ResolveInventory()
    {
        if (inventory != null)
        {
            return inventory;
        }

        inventory = InventoryFacility.FindAny();
        return inventory;
    }

    private TraitPointFacility ResolveTraitPointFacility()
    {
        if (traitPointFacility != null)
        {
            return traitPointFacility;
        }

        traitPointFacility = FindFirstObjectByType<TraitPointFacility>();
        return traitPointFacility;
    }

    private void RegisterCurrencyWalletEvents()
    {
        if (currencyWallet == null)
        {
            return;
        }

        if (registeredCurrencyWallet == currencyWallet)
        {
            return;
        }

        if (registeredCurrencyWallet != null)
        {
            registeredCurrencyWallet.OnCreditsChanged.RemoveListener(HandleCreditsChanged);
            registeredCurrencyWallet.OnCoreCrystalsChanged.RemoveListener(HandleCoreCrystalsChanged);
        }

        currencyWallet.OnCreditsChanged.RemoveListener(HandleCreditsChanged);
        currencyWallet.OnCoreCrystalsChanged.RemoveListener(HandleCoreCrystalsChanged);
        currencyWallet.OnCreditsChanged.AddListener(HandleCreditsChanged);
        currencyWallet.OnCoreCrystalsChanged.AddListener(HandleCoreCrystalsChanged);
        registeredCurrencyWallet = currencyWallet;
        HandleCreditsChanged(currencyWallet.Credits);
        HandleCoreCrystalsChanged(currencyWallet.CoreCrystals);
    }

    private void HandleCreditsChanged(int value)
    {
        // 기존 기지 UI 이벤트를 유지하면서 실제 값은 wallet이 관리한다.
        credits = value;
        OnCreditsChanged.Invoke(value);
    }

    private void HandleCoreCrystalsChanged(int value)
    {
        OnCoreCrystalsChanged.Invoke(value);
    }
}

public enum CurrencyType
{
    Credits,
    CoreCrystals
}

public interface ICurrencyWallet
{
    int GetAmount(CurrencyType type);
    void Add(CurrencyType type, int amount);
    bool CanSpend(CurrencyType type, int amount);
    bool TrySpend(CurrencyType type, int amount);
}

public class PlayerCurrencyWallet : MonoBehaviour, ICurrencyWallet
{
    private const string CreditsKey = "PlayerCurrencyWallet.Credits";
    private const string CoreCrystalsKey = "PlayerCurrencyWallet.CoreCrystals";

    [Header("Currency")]
    [SerializeField] private int credits = 500;
    [SerializeField] private int coreCrystals;
    [SerializeField] private bool saveToPlayerPrefs = true;

    [Header("Events")]
    public UnityEvent<int> OnCreditsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnCoreCrystalsChanged = new UnityEvent<int>();

    public int Credits => credits;
    public int CoreCrystals => coreCrystals;

    private void Awake()
    {
        Load();
    }

    public void AddCredits(int amount)
    {
        Add(CurrencyType.Credits, amount);
    }

    public bool TrySpendCredits(int amount)
    {
        return TrySpend(CurrencyType.Credits, amount);
    }

    public void AddCoreCrystals(int amount)
    {
        Add(CurrencyType.CoreCrystals, amount);
    }

    public bool TrySpendCoreCrystals(int amount)
    {
        return TrySpend(CurrencyType.CoreCrystals, amount);
    }

    public int GetAmount(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Credits:
                return credits;
            case CurrencyType.CoreCrystals:
                return coreCrystals;
            default:
                return 0;
        }
    }

    public void Add(CurrencyType type, int amount)
    {
        amount = Mathf.Max(0, amount);
        switch (type)
        {
            case CurrencyType.Credits:
                SetCredits(credits + amount);
                break;
            case CurrencyType.CoreCrystals:
                SetCoreCrystals(coreCrystals + amount);
                break;
        }
    }

    public bool CanSpend(CurrencyType type, int amount)
    {
        amount = Mathf.Max(0, amount);
        return GetAmount(type) >= amount;
    }

    public bool TrySpend(CurrencyType type, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (!CanSpend(type, amount))
        {
            return false;
        }

        // 재화 종류별 이벤트/저장은 기존 Set 메서드에 위임한다.
        switch (type)
        {
            case CurrencyType.Credits:
                SetCredits(credits - amount);
                break;
            case CurrencyType.CoreCrystals:
                SetCoreCrystals(coreCrystals - amount);
                break;
        }

        return true;
    }

    public void SetCredits(int value)
    {
        credits = Mathf.Max(0, value);
        Save();
        OnCreditsChanged.Invoke(credits);
    }

    public void SetCoreCrystals(int value)
    {
        coreCrystals = Mathf.Max(0, value);
        Save();
        OnCoreCrystalsChanged.Invoke(coreCrystals);
    }

    private void Load()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        credits = Mathf.Max(0, PlayerPrefs.GetInt(CreditsKey, credits));
        coreCrystals = Mathf.Max(0, PlayerPrefs.GetInt(CoreCrystalsKey, coreCrystals));
        OnCreditsChanged.Invoke(credits);
        OnCoreCrystalsChanged.Invoke(coreCrystals);
    }

    private void Save()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetInt(CreditsKey, credits);
        PlayerPrefs.SetInt(CoreCrystalsKey, coreCrystals);
        PlayerPrefs.Save();
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
    bool CanUpgrade(int credits, int commanderLevel);
    int GetLevelLimit(int researchLabLevel);
    bool CanStartUpgrade(int credits, int commanderLevel, int researchLabLevel);
    bool TryStartUpgrade(ref int availableCredits, int commanderLevel, int researchLabLevel);
    void Upgrade();
}

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BaseCampManager : MonoBehaviour
{
    public static BaseCampManager Instance { get; private set; }

    [Header("Facilities")]
    [SerializeField] private CommandCenter commandCenter;
    [SerializeField] private CreditRefinery creditRefinery;
    [SerializeField] private AssemblyFactory assemblyFactory;
    [SerializeField] private CoreCharger coreCharger;
    [SerializeField] private InventoryFacility inventory;
    [SerializeField] private DailyMissionManager dailyMissionManager;
    [SerializeField] private MainGuideMissionManager mainGuideMissionManager;
    [SerializeField] private bool autoFindFacilities = true;

    [Header("Facility Panels")]
    [SerializeField] private GameObject[] facilityPanels;
    [SerializeField] private bool closePanelsOnStart = true;

    [Header("Player State")]
    [SerializeField] private int commanderLevel = 1;
    [SerializeField] private int credits = 500;
    [SerializeField] private PlayerCurrencyWallet currencyWallet;
    [SerializeField] private PlayerProgression playerProgression;

    [Header("Unified Save")]
    [SerializeField] private bool useUnifiedSave = true;
    [SerializeField] private bool autoSaveUnifiedState = true;
    [SerializeField] private string unifiedSaveKey = "Jinyou.SaveData";

    [Header("Debug")]
    [SerializeField] private bool showDebugPanel;
    [SerializeField] private Rect debugPanelRect = new Rect(16f, 16f, 280f, 220f);

    [Header("Events")]
    public UnityEvent<int> OnCreditsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnCoreCrystalsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnCommanderLevelChanged = new UnityEvent<int>();
    public UnityEvent<JinyouOfflineRewardSaveData> OnOfflineRewardsClaimed = new UnityEvent<JinyouOfflineRewardSaveData>();

    private PlayerCurrencyWallet registeredCurrencyWallet;
    private bool unifiedSaveReady;
    private bool isRestoringUnifiedSave;
    private bool confirmPlayerPrefsReset;
    private JinyouOfflineRewardSaveData lastOfflineReward = new JinyouOfflineRewardSaveData();

    public CommandCenter CommandCenter => commandCenter;
    public CreditRefinery CreditRefinery => creditRefinery;
    public AssemblyFactory AssemblyFactory => assemblyFactory;
    public CoreCharger CoreCharger => coreCharger;
    public InventoryFacility Inventory => ResolveInventory();
    public int CommanderLevel => ResolveCommanderLevel();
    public int Credits => CurrencyWallet.Credits;
    public int CoreCrystals => CurrencyWallet.CoreCrystals;
    public PlayerCurrencyWallet CurrencyWallet => EnsureCurrencyWallet();
    public PlayerProgression PlayerProgression => ResolvePlayerProgression();
    public JinyouOfflineRewardSaveData LastOfflineReward => lastOfflineReward;

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
        EnsureDailyMissionManager();
        EnsureMainGuideMissionManager();
    }

    private void Start()
    {
        ConnectFacilities();
        LoadUnifiedGame();
        ConfigureUnifiedPersistence();
        SubscribeUnifiedSaveEvents();
        unifiedSaveReady = true;
        SaveUnifiedGame();
        ApplyEquippedLoadoutAtBoot();

        if (closePanelsOnStart)
        {
            CloseAllPanels();
        }
    }

    private void Update()
    {
        TickInactiveFacilities(Time.deltaTime);
    }

    // 저장된 장착 무기/드론을 부팅 시 적용한다. 로드아웃 패널이 닫힌 팝업 안에 있어
    // 패널을 한 번 열기 전까지 적용되지 않던 문제를 막는다(현재 유닛은 CoreCharger가 복원).
    private void ApplyEquippedLoadoutAtBoot()
    {
        PlayerLoadoutSelectionPanel loadout =
            FindFirstObjectByType<PlayerLoadoutSelectionPanel>(FindObjectsInactive.Include);
        if (loadout != null)
        {
            loadout.ApplySavedLoadout();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveUnifiedGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveUnifiedGame();
    }

    private void OnDestroy()
    {
        if (registeredCurrencyWallet != null)
        {
            registeredCurrencyWallet.OnCreditsChanged.RemoveListener(HandleCreditsChanged);
            registeredCurrencyWallet.OnCoreCrystalsChanged.RemoveListener(HandleCoreCrystalsChanged);
            registeredCurrencyWallet = null;
        }

        UnsubscribeUnifiedSaveEvents();

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

        commandCenter ??= FindFirstObjectByType<CommandCenter>(FindObjectsInactive.Include);
        creditRefinery ??= FindFirstObjectByType<CreditRefinery>(FindObjectsInactive.Include);
        assemblyFactory ??= FindFirstObjectByType<AssemblyFactory>(FindObjectsInactive.Include);
        coreCharger ??= FindFirstObjectByType<CoreCharger>(FindObjectsInactive.Include);
        inventory ??= InventoryFacility.FindAny();
    }

    public void CollectRefineryCredits()
    {
        if (creditRefinery != null)
        {
            int collectedCredits = creditRefinery.CollectCredits();
            AddCredits(collectedCredits);
            DailyMissionManager.ReportCreditsCollected(collectedCredits);
            MainGuideMissionManager.ReportCreditsCollected(collectedCredits);
        }
    }

    public void UpgradeResearchLab()
    {
        TrySpendAndUpgrade(commandCenter);
    }

    public void UpgradeEnergyRefinery()
    {
        TrySpendAndUpgrade(creditRefinery);
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

    public void SelectAssemblyWeapon(int weaponIndex)
    {
        if (assemblyFactory != null && assemblyFactory.TrySelectWeapon(weaponIndex))
        {
            SaveUnifiedGameIfReady();
        }
    }

    public void SelectAssemblyWeapon(ProjectileConfig weaponConfig)
    {
        if (assemblyFactory != null && assemblyFactory.TrySelectWeapon(weaponConfig))
        {
            SaveUnifiedGameIfReady();
        }
    }

    public void SelectAssemblyDrone(DroneConfig droneConfig)
    {
        if (assemblyFactory != null && assemblyFactory.TrySelectDrone(droneConfig))
        {
            SaveUnifiedGameIfReady();
        }
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
            DailyMissionManager.ReportWeaponEnhanced();
            MainGuideMissionManager.ReportWeaponEnhanced();
        }
    }

    public void EnhanceAssemblyDrone()
    {
        if (assemblyFactory == null)
        {
            return;
        }

        int availableCredits = Credits;
        if (assemblyFactory.TryEnhanceSelectedDrone(ref availableCredits))
        {
            SetCreditsForFacility(availableCredits);
            DailyMissionManager.ReportDroneEnhanced();
            MainGuideMissionManager.ReportDroneEnhanced();
            SaveUnifiedGameIfReady();
        }
    }

    public void EnhanceCoreUnit()
    {
        ConvertSelectedCoreUnit();
    }

    public void ConvertSelectedCoreUnit()
    {
        if (coreCharger == null)
        {
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        int playerLevel = PlayerProgression != null ? PlayerProgression.Level : commanderLevel;
        if (coreCharger.TryConvertCurrentUnit(Inventory, player, playerLevel))
        {
            DailyMissionManager.ReportUnitEnhanced();
            MainGuideMissionManager.ReportUnitEnhanced();
            SaveUnifiedGameIfReady();
        }
    }

    public void UseBossTicket()
    {
        commandCenter?.TryUseBossTicket();
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
        SetCommanderLevel(CommanderLevel + Mathf.Max(0, amount));
    }

    public void SetCreditsForFacility(int value)
    {
        SetCredits(value);
    }

    public void SetCommanderLevel(int value)
    {
        commanderLevel = Mathf.Max(1, value);
        OnCommanderLevelChanged.Invoke(CommanderLevel);
        SaveUnifiedGameIfReady();
    }

    [ContextMenu("Save Unified Game")]
    public void SaveUnifiedGame()
    {
        if (!useUnifiedSave
            || isRestoringUnifiedSave
            || string.IsNullOrWhiteSpace(unifiedSaveKey))
        {
            return;
        }

        // 준비된 시설 상태를 하나의 JSON으로 저장해 시스템 간 시점을 맞춘다.
        JinyouSaveData data = CaptureUnifiedSaveData();
        PlayerPrefs.SetString(unifiedSaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [ContextMenu("Load Unified Game")]
    public void LoadUnifiedGame()
    {
        if (!useUnifiedSave
            || string.IsNullOrWhiteSpace(unifiedSaveKey)
            || !PlayerPrefs.HasKey(unifiedSaveKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(unifiedSaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            RestoreUnifiedSaveData(JsonUtility.FromJson<JinyouSaveData>(json));
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"통합 저장 데이터를 읽지 못했습니다: {exception.Message}", this);
        }
    }

    [ContextMenu("Delete Unified Save")]
    public void DeleteUnifiedSave()
    {
        if (string.IsNullOrWhiteSpace(unifiedSaveKey))
        {
            return;
        }

        PlayerPrefs.DeleteKey(unifiedSaveKey);
        PlayerPrefs.Save();
    }

    private void SetCredits(int value)
    {
        CurrencyWallet.SetCredits(value);
    }

    private void TickInactiveFacilities(float deltaTime)
    {
        if (creditRefinery == null || creditRefinery.isActiveAndEnabled)
        {
            return;
        }

        // 정제소 UI 패널이 꺼져도 저장 생산/업그레이드 타이머는 계속 진행한다.
        creditRefinery.Produce(deltaTime);
        creditRefinery.AdvanceUpgradeOffline(deltaTime);
    }

    private void TrySpendAndUpgrade(IBaseCampFacility facility)
    {
        if (facility == null)
        {
            return;
        }

        int availableCredits = Credits;
        int effectiveCommanderLevel = CommanderLevel;
        int researchLabLevel = commandCenter != null ? commandCenter.Level : 1;
        int upgradeCost = facility.UpgradeCost;

        if (!facility.CanStartUpgrade(availableCredits, effectiveCommanderLevel, researchLabLevel))
        {
            return;
        }

        // 시설은 타이머만 시작하고, 실제 재화 차감은 wallet 한 곳에서 처리한다.
        int simulatedCredits = availableCredits;
        if (CurrencyWallet.CanSpend(CurrencyType.Credits, upgradeCost)
            && facility.TryStartUpgrade(ref simulatedCredits, effectiveCommanderLevel, researchLabLevel))
        {
            CurrencyWallet.TrySpend(CurrencyType.Credits, upgradeCost);
            DailyMissionManager.ReportFacilityUpgraded();
            MainGuideMissionManager.ReportFacilityUpgraded();
        }
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (showDebugPanel)
        {
            debugPanelRect.height = Mathf.Max(debugPanelRect.height, 260f);
            debugPanelRect = GUILayout.Window(GetInstanceID(), debugPanelRect, DrawDebugPanel, "Base Camp");
        }
#endif
    }

    private void DrawDebugPanel(int windowId)
    {
        GUILayout.Label($"Commander Lv. {CommanderLevel}");
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
        if (GUILayout.Button(confirmPlayerPrefsReset
                ? "Confirm Reset All Save"
                : "Reset All PlayerPrefs"))
        {
            if (confirmPlayerPrefsReset)
            {
                ResetAllPlayerPrefsDebug();
            }
            else
            {
                confirmPlayerPrefsReset = true;
            }
        }

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

    [ContextMenu("Debug/Reset All PlayerPrefs")]
    private void ResetAllPlayerPrefsDebug()
    {
        // 모든 저장 키를 제거한 뒤 씬을 다시 로드해 Inspector 초기값으로 테스트한다.
        unifiedSaveReady = false;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
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

    private int ResolveCommanderLevel()
    {
        PlayerProgression progression = ResolvePlayerProgression();
        if (progression != null)
        {
            // 지휘관 레벨은 전투 플레이어 레벨을 기준으로 시설 해금/업그레이드 조건에 사용한다.
            return Mathf.Max(1, progression.Level);
        }

        return Mathf.Max(1, commanderLevel);
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

    private DailyMissionManager EnsureDailyMissionManager()
    {
        dailyMissionManager ??= DailyMissionManager.Instance
            ?? FindFirstObjectByType<DailyMissionManager>();
        if (dailyMissionManager == null)
        {
            // 일일 미션이 없는 전투 씬에서도 동일한 저장 데이터를 유지한다.
            dailyMissionManager = gameObject.AddComponent<DailyMissionManager>();
        }

        return dailyMissionManager;
    }

    private MainGuideMissionManager EnsureMainGuideMissionManager()
    {
        mainGuideMissionManager ??= MainGuideMissionManager.Instance
            ?? FindFirstObjectByType<MainGuideMissionManager>();
        if (mainGuideMissionManager == null)
        {
            // 가이드 미션이 없는 전투 씬에서도 동일한 저장 데이터를 유지한다.
            mainGuideMissionManager = gameObject.AddComponent<MainGuideMissionManager>();
        }

        return mainGuideMissionManager;
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

    private void ConfigureUnifiedPersistence()
    {
        if (!useUnifiedSave)
        {
            return;
        }

        // 개별 저장을 통합 저장으로 한 번 마이그레이션한 뒤 중복 키를 제거한다.
        EnsureCurrencyWallet().SetStandaloneSaveEnabled(false, true);
        assemblyFactory?.SetStandaloneSaveEnabled(false, true);
        EnsureDailyMissionManager().SetStandaloneSaveEnabled(false, true);
        EnsureMainGuideMissionManager().SetStandaloneSaveEnabled(false, true);

        AchievementManager achievementManager = AchievementManager.Instance
            ?? FindFirstObjectByType<AchievementManager>();
        achievementManager?.SetStandaloneSaveEnabled(false, true);
    }

    private void HandleCreditsChanged(int value)
    {
        // 기존 기지 UI 이벤트를 유지하면서 실제 값은 wallet이 관리한다.
        credits = value;
        OnCreditsChanged.Invoke(value);
        SaveUnifiedGameIfReady();
    }

    private void HandleCoreCrystalsChanged(int value)
    {
        OnCoreCrystalsChanged.Invoke(value);
        SaveUnifiedGameIfReady();
    }

    private JinyouSaveData CaptureUnifiedSaveData()
    {
        PlayerCurrencyWallet wallet = EnsureCurrencyWallet();
        AchievementManager achievementManager = AchievementManager.Instance
            ?? FindFirstObjectByType<AchievementManager>();
        DailyMissionManager resolvedDailyMissionManager = EnsureDailyMissionManager();
        MainGuideMissionManager resolvedGuideMissionManager = EnsureMainGuideMissionManager();

        return new JinyouSaveData
        {
            version = 2,
            lastSavedUnixTime = GetCurrentUnixTime(),
            commanderLevel = CommanderLevel,
            mainBuildingLevel = commandCenter != null ? commandCenter.Level : 1,
            credits = wallet != null ? wallet.Credits : credits,
            coreCrystals = wallet != null ? wallet.CoreCrystals : 0,
            lastOfflineReward = lastOfflineReward,
            researchLab = commandCenter != null ? commandCenter.CaptureState() : new JinyouCommandCenterSaveData(),
            energyRefinery = creditRefinery != null ? creditRefinery.CaptureState() : new JinyouEnergyRefinerySaveData(),
            assemblyFactory = assemblyFactory != null ? assemblyFactory.CaptureState() : new JinyouAssemblyFactorySaveData(),
            coreCharger = coreCharger != null ? coreCharger.CaptureState() : new JinyouCoreChargerSaveData(),
            achievements = achievementManager != null ? achievementManager.CaptureState() : new JinyouAchievementSaveData(),
            dailyMissions = resolvedDailyMissionManager.CaptureState(),
            guideMissions = resolvedGuideMissionManager.CaptureState()
        };
    }

    private void RestoreUnifiedSaveData(JinyouSaveData data)
    {
        if (data == null)
        {
            return;
        }

        isRestoringUnifiedSave = true;
        try
        {
            // 재화와 시설을 먼저 복원한 뒤 업적과 일일 미션을 복원한다.
            commanderLevel = Mathf.Max(1, data.commanderLevel);
            data.researchLab ??= new JinyouCommandCenterSaveData();
            data.researchLab.level = data.version >= 2 && data.mainBuildingLevel > 0
                ? data.mainBuildingLevel
                : Mathf.Max(1, data.researchLab.level);
            PlayerCurrencyWallet wallet = EnsureCurrencyWallet();
            wallet?.SetCredits(data.credits);
            wallet?.SetCoreCrystals(data.coreCrystals);
            commandCenter?.RestoreState(data.researchLab);
            creditRefinery?.RestoreState(data.energyRefinery);
            assemblyFactory?.RestoreState(data.assemblyFactory);
            coreCharger?.RestoreState(data.coreCharger);
            coreCharger?.ApplyCompletedConversions(Inventory, FindFirstObjectByType<PlayerController>());
            AchievementManager achievementManager = AchievementManager.Instance
                ?? FindFirstObjectByType<AchievementManager>();
            achievementManager?.RestoreState(data.achievements);
            EnsureDailyMissionManager().RestoreState(data.dailyMissions);
            EnsureMainGuideMissionManager().RestoreState(data.guideMissions);

            ApplyOfflineRewards(data.lastSavedUnixTime);
            OnCommanderLevelChanged.Invoke(CommanderLevel);
        }
        finally
        {
            isRestoringUnifiedSave = false;
        }
    }

    private void ApplyOfflineRewards(long lastSavedUnixTime)
    {
        lastOfflineReward = new JinyouOfflineRewardSaveData();
        if (lastSavedUnixTime <= 0 || commandCenter == null)
        {
            return;
        }

        float elapsedSeconds = Mathf.Max(0f, GetCurrentUnixTime() - lastSavedUnixTime);
        if (elapsedSeconds <= 0f)
        {
            return;
        }

        // 건설 시간은 전체 미접속 시간을 반영하고 생산 보상은 시설별 한도를 적용한다.
        commandCenter.AdvanceUpgradeOffline(elapsedSeconds);
        creditRefinery?.AdvanceUpgradeOffline(elapsedSeconds);
        assemblyFactory?.AdvanceUpgradeOffline(elapsedSeconds);
        coreCharger?.AdvanceUpgradeOffline(elapsedSeconds);

        float maxOfflineSeconds = Mathf.Max(0f, commandCenter.OfflineRewardLimitHours) * 3600f;
        float appliedSeconds = Mathf.Min(elapsedSeconds, maxOfflineSeconds);
        int storedCreditsBefore = creditRefinery != null ? creditRefinery.StoredCredits : 0;
        creditRefinery?.Produce(appliedSeconds);
        int storedCreditsAfter = creditRefinery != null ? creditRefinery.StoredCredits : storedCreditsBefore;
        float ticketOfflineSeconds = Mathf.Min(
            elapsedSeconds,
            Mathf.Max(0f, commandCenter.TicketOfflineLimitHours) * 3600f);

        lastOfflineReward = new JinyouOfflineRewardSaveData
        {
            elapsedSeconds = elapsedSeconds,
            appliedSeconds = appliedSeconds,
            refineryCreditsAdded = Mathf.Max(0, storedCreditsAfter - storedCreditsBefore),
            bossTicketsAdded = commandCenter.ProduceBossTicketsOffline(ticketOfflineSeconds)
        };

        if (lastOfflineReward.HasReward)
        {
            OnOfflineRewardsClaimed.Invoke(lastOfflineReward);
            DailyMissionManager.ReportOfflineRewardClaimed();
            MainGuideMissionManager.ReportOfflineRewardClaimed();
        }
    }

    private void SubscribeUnifiedSaveEvents()
    {
        OnCommanderLevelChanged.AddListener(HandleUnifiedSaveEvent);
        commandCenter?.OnLevelChanged.AddListener(HandleUnifiedSaveEvent);
        commandCenter?.OnBossTicketsChanged.AddListener(HandleUnifiedSaveEvent);
        creditRefinery?.OnLevelChanged.AddListener(HandleUnifiedSaveEvent);
        creditRefinery?.OnCreditsChanged.AddListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnLevelChanged.AddListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnMenuSelected.AddListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnMenuUnlocked.AddListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnWeaponEnhanced.AddListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnDroneEnhanced.AddListener(HandleUnifiedSaveEvent);
        coreCharger?.OnLevelChanged.AddListener(HandleUnifiedSaveEvent);
        coreCharger?.OnUnitEnhanced.AddListener(HandleUnifiedSaveEvent);
        AchievementManager achievementManager = AchievementManager.Instance
            ?? FindFirstObjectByType<AchievementManager>();
        achievementManager?.OnAchievementsChanged.AddListener(HandleUnifiedSaveEvent);
        achievementManager?.OnAchievementCompleted.AddListener(HandleUnifiedSaveEvent);

        DailyMissionManager resolvedDailyMissionManager = EnsureDailyMissionManager();
        resolvedDailyMissionManager.OnDailyMissionsChanged.AddListener(HandleUnifiedSaveEvent);
        resolvedDailyMissionManager.OnDailyMissionCompleted.AddListener(HandleUnifiedSaveEvent);
        resolvedDailyMissionManager.OnDailyMissionRewardClaimed.AddListener(HandleUnifiedSaveEvent);

        MainGuideMissionManager resolvedGuideMissionManager = EnsureMainGuideMissionManager();
        resolvedGuideMissionManager.OnGuideMissionsChanged.AddListener(HandleUnifiedSaveEvent);
        resolvedGuideMissionManager.OnGuideStepCompleted.AddListener(HandleUnifiedSaveEvent);
        resolvedGuideMissionManager.OnGuideStepClaimed.AddListener(HandleUnifiedSaveEvent);
    }

    private void UnsubscribeUnifiedSaveEvents()
    {
        OnCommanderLevelChanged.RemoveListener(HandleUnifiedSaveEvent);
        commandCenter?.OnLevelChanged.RemoveListener(HandleUnifiedSaveEvent);
        commandCenter?.OnBossTicketsChanged.RemoveListener(HandleUnifiedSaveEvent);
        creditRefinery?.OnLevelChanged.RemoveListener(HandleUnifiedSaveEvent);
        creditRefinery?.OnCreditsChanged.RemoveListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnLevelChanged.RemoveListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnMenuSelected.RemoveListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnMenuUnlocked.RemoveListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnWeaponEnhanced.RemoveListener(HandleUnifiedSaveEvent);
        assemblyFactory?.OnDroneEnhanced.RemoveListener(HandleUnifiedSaveEvent);
        coreCharger?.OnLevelChanged.RemoveListener(HandleUnifiedSaveEvent);
        coreCharger?.OnUnitEnhanced.RemoveListener(HandleUnifiedSaveEvent);
        AchievementManager achievementManager = AchievementManager.Instance
            ?? FindFirstObjectByType<AchievementManager>();
        achievementManager?.OnAchievementsChanged.RemoveListener(HandleUnifiedSaveEvent);
        achievementManager?.OnAchievementCompleted.RemoveListener(HandleUnifiedSaveEvent);

        if (dailyMissionManager != null)
        {
            dailyMissionManager.OnDailyMissionsChanged.RemoveListener(HandleUnifiedSaveEvent);
            dailyMissionManager.OnDailyMissionCompleted.RemoveListener(HandleUnifiedSaveEvent);
            dailyMissionManager.OnDailyMissionRewardClaimed.RemoveListener(HandleUnifiedSaveEvent);
        }

        if (mainGuideMissionManager != null)
        {
            mainGuideMissionManager.OnGuideMissionsChanged.RemoveListener(HandleUnifiedSaveEvent);
            mainGuideMissionManager.OnGuideStepCompleted.RemoveListener(HandleUnifiedSaveEvent);
            mainGuideMissionManager.OnGuideStepClaimed.RemoveListener(HandleUnifiedSaveEvent);
        }
    }

    private void HandleUnifiedSaveEvent()
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(int value)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(string value)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(ProjectileConfig weaponConfig, int level)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(PlayerUnitConfig unitConfig, int level)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(DroneConfig droneConfig, int level)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(AchievementManager.AchievementEntry achievement)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(DailyMissionManager.DailyMissionEntry mission)
    {
        SaveUnifiedGameIfReady();
    }

    private void HandleUnifiedSaveEvent(GuideMissionConfig.GuideStepData guideStep)
    {
        SaveUnifiedGameIfReady();
    }

    private void SaveUnifiedGameIfReady()
    {
        if (autoSaveUnifiedState && unifiedSaveReady)
        {
            SaveUnifiedGame();
        }
    }

    private static long GetCurrentUnixTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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

    public void SetStandaloneSaveEnabled(bool enabled, bool clearStoredData)
    {
        saveToPlayerPrefs = enabled;
        if (!clearStoredData)
        {
            return;
        }

        PlayerPrefs.DeleteKey(CreditsKey);
        PlayerPrefs.DeleteKey(CoreCrystalsKey);
        PlayerPrefs.Save();
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

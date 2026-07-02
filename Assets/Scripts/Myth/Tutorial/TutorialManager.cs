using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 최초 1회 인터랙티브 온보딩 튜토리얼을 구동한다.
/// 프리팹/씬 배선 없이 <see cref="MenuAlertController"/>처럼 자가 부트스트랩한다.
/// 스텝 정의는 <see cref="TutorialConfig"/>(Resources/Tutorial/TutorialConfig)에서 로드,
/// 진행도는 PlayerPrefs에 저장한다(완료 시 다시 뜨지 않음).
/// </summary>
[DisallowMultipleComponent]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string StepIndexKey = "Tutorial.StepIndex";
    private const string CompletedKey = "Tutorial.Completed";
    private const float ResolveInterval = 0.5f;
    // 이름 기반 씬 전체 스캔(FindInScene)의 최소 간격. 타겟을 못 찾는 동안 매 프레임
    // Resources.FindObjectsOfTypeAll 전체 힙 스캔이 돌던 것을 막는다(모바일 프레임 스파이크 방지).
    private const float SceneScanInterval = 0.25f;

    private TutorialConfig config;
    private TutorialOverlay overlay;

    private int stepIndex;
    private bool completed;
    private bool running;

    private TutorialConfig.TutorialStep activeStep;
    private int eventProgress;
    private RectTransform resolvedTarget;
    private Button armedButton;
    private UnityEngine.Events.UnityAction armedHandler;

    private BaseCampManager subscribedCamp;
    private AssemblyFactory subscribedFactory;
    private CoreCharger subscribedCharger;
    private float nextResolveTime;
    private float nextSceneScanTime;
    private bool allowSceneScan;
    private bool missingOverlayPrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 이미 완료했거나 인스턴스가 있으면 생성하지 않는다.
        if (PlayerPrefs.GetInt(CompletedKey, 0) == 1)
        {
            return;
        }

        if (FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject host = new GameObject(nameof(TutorialManager));
        host.AddComponent<TutorialManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Bootstrap은 최초 씬(타이틀) 로드 직후 1회만 실행되므로, 씬 전환에도 파괴되지 않게 유지한다.
        // 그러지 않으면 타이틀에서 생성된 매니저가 게임(기지) 씬 로드 시 사라져 튜토리얼이 영영 뜨지 않는다.
        DontDestroyOnLoad(gameObject);

        config = TutorialConfig.Current;
        completed = PlayerPrefs.GetInt(CompletedKey, 0) == 1;
        stepIndex = Mathf.Max(0, PlayerPrefs.GetInt(StepIndexKey, 0));
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (completed || config == null || config.Steps.Count == 0)
        {
            return;
        }

        // 게임(기지) 씬에서만 동작 — BaseCampManager가 떠야 시작한다(타이틀 등에서 안 뜨도록).
        if (BaseCampManager.Instance == null)
        {
            return;
        }

        if (Time.unscaledTime >= nextResolveTime)
        {
            nextResolveTime = Time.unscaledTime + ResolveInterval;
            SubscribeEvents();
        }

        if (!running)
        {
            BeginStep(stepIndex);
            return;
        }

        // 씬 전체 스캔은 간격을 두고만 허용한다(찾은 타겟 추적 등 싼 경로는 매 프레임 유지).
        allowSceneScan = Time.unscaledTime >= nextSceneScanTime;
        if (allowSceneScan)
        {
            nextSceneScanTime = Time.unscaledTime + SceneScanInterval;
        }

        ResolveActiveTarget();
        PollPanelOpened();
    }

    // ── 진행 ────────────────────────────────────────────────────────────────
    private void BeginStep(int index)
    {
        if (index >= config.Steps.Count)
        {
            Complete();
            return;
        }

        stepIndex = index;
        activeStep = config.Steps[index];
        eventProgress = 0;
        resolvedTarget = null;
        ClearArmedButton();

        if (overlay == null)
        {
            overlay = CreateOverlay();
            if (overlay == null)
            {
                missingOverlayPrefab = true;
                return;
            }
        }

        running = true;
        overlay.Show(activeStep.bodyText, null, activeStep.advanceType, OnTapAdvance);
        allowSceneScan = true; // 스텝 시작 시엔 즉시 1회 스캔 허용.
        ResolveActiveTarget();
        Save();
    }

    private TutorialOverlay CreateOverlay()
    {
        TutorialOverlay prefab = config.OverlayPrefab;
        if (prefab == null)
        {
            if (!missingOverlayPrefab)
            {
                Debug.LogWarning("[Tutorial] TutorialOverlay 프리팹을 찾지 못했습니다. TutorialConfig에 지정하거나 Resources/Tutorial/TutorialOverlay.prefab을 생성하세요.");
            }

            return null;
        }

        // 오버레이 UI는 프리팹에서 만들고, 런타임에서는 인스턴스 제어만 한다.
        TutorialOverlay created = Instantiate(prefab, transform, false);
        created.Configure(config.BodyFont);
        return created;
    }

    private void OnTapAdvance()
    {
        if (running && activeStep != null && activeStep.advanceType == TutorialAdvanceType.Tap)
        {
            AdvanceStep();
        }
    }

    private void AdvanceStep()
    {
        ClearArmedButton();
        running = false;
        BeginStep(stepIndex + 1);
    }

    private void Complete()
    {
        completed = true;
        running = false;
        activeStep = null;
        ClearArmedButton();
        if (overlay != null)
        {
            overlay.Hide();
        }

        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
        UnsubscribeEvents();
    }

    // ── 클라우드 동기화(통합 세이브 편입) ──────────────────────────────────────
    // 인스턴스가 없어도 동작하도록 PlayerPrefs를 직접 읽고/쓴다(부트스트랩이 인스턴스 생성을 결정하므로).

    /// <summary>현재 튜토리얼 진행 상태 스냅샷.</summary>
    public static JinyouTutorialSaveData CaptureSaveData()
    {
        return new JinyouTutorialSaveData
        {
            captured = true,
            completed = PlayerPrefs.GetInt(CompletedKey, 0) == 1,
            stepIndex = Mathf.Max(0, PlayerPrefs.GetInt(StepIndexKey, 0)),
        };
    }

    /// <summary>통합 세이브에서 튜토리얼 상태를 복원한다. 클라우드가 '완료'면 진행 중이던 튜토리얼도 즉시 종료한다.</summary>
    public static void RestoreSaveData(JinyouTutorialSaveData data)
    {
        if (data == null || !data.captured)
        {
            return;
        }

        PlayerPrefs.SetInt(CompletedKey, data.completed ? 1 : 0);
        PlayerPrefs.SetInt(StepIndexKey, Mathf.Max(0, data.stepIndex));
        PlayerPrefs.Save();

        TutorialManager live = Instance;
        if (live == null)
        {
            return;
        }

        if (data.completed)
        {
            live.Complete(); // 이번 실행에 떠 있던 튜토리얼을 즉시 종료/숨김.
        }
        else
        {
            live.completed = false;
            if (!live.running)
            {
                live.stepIndex = Mathf.Max(0, data.stepIndex);
            }
        }
    }

    // 강조 타깃을 이름으로 계속 탐색해 늦게 활성화되는 패널 내부 요소도 따라간다.
    private void ResolveActiveTarget()
    {
        if (activeStep == null || string.IsNullOrWhiteSpace(activeStep.highlightTargetName))
        {
            return;
        }

        if (resolvedTarget != null && resolvedTarget.gameObject.activeInHierarchy)
        {
            ArmTargetButtonIfNeeded();
            return;
        }

        if (!allowSceneScan)
        {
            return; // 전체 스캔은 SceneScanInterval 간격으로만.
        }

        Transform found = FindInScene(activeStep.highlightTargetName);
        resolvedTarget = found as RectTransform;
        if (resolvedTarget != null)
        {
            overlay?.SetHighlight(resolvedTarget);
            ArmTargetButtonIfNeeded();
        }
    }

    private void ArmTargetButtonIfNeeded()
    {
        if (activeStep == null || activeStep.advanceType != TutorialAdvanceType.TargetClicked)
        {
            return;
        }

        if (armedButton != null || resolvedTarget == null)
        {
            return;
        }

        Button button = resolvedTarget.GetComponent<Button>() ?? resolvedTarget.GetComponentInChildren<Button>(true);
        if (button == null)
        {
            return;
        }

        armedButton = button;
        armedHandler = AdvanceStep;
        // 실제 버튼 동작(패널 열기 등)도 그대로 실행되고, 우리 리스너가 다음 스텝으로 넘긴다.
        armedButton.onClick.AddListener(armedHandler);
    }

    private void ClearArmedButton()
    {
        if (armedButton != null && armedHandler != null)
        {
            armedButton.onClick.RemoveListener(armedHandler);
        }

        armedButton = null;
        armedHandler = null;
    }

    private void PollPanelOpened()
    {
        if (activeStep == null
            || activeStep.advanceType != TutorialAdvanceType.GameEvent
            || activeStep.eventType != TutorialEventType.PanelOpened)
        {
            return;
        }

        string name = string.IsNullOrWhiteSpace(activeStep.targetId)
            ? activeStep.highlightTargetName
            : activeStep.targetId;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!allowSceneScan)
        {
            return; // 전체 스캔은 SceneScanInterval 간격으로만.
        }

        Transform panel = FindInScene(name);
        if (panel != null && panel.gameObject.activeInHierarchy)
        {
            AdvanceStep();
        }
    }

    // ── 정적 보고 훅(기존 Report* 호출부 옆에서 함께 호출) ─────────────────────
    public static void Report(TutorialEventType eventType, int amount = 1)
    {
        Instance?.HandleEvent(eventType, amount);
    }

    private void HandleEvent(TutorialEventType eventType, int amount)
    {
        if (!running
            || activeStep == null
            || activeStep.advanceType != TutorialAdvanceType.GameEvent
            || activeStep.eventType != eventType)
        {
            return;
        }

        eventProgress += Mathf.Max(1, amount);
        if (eventProgress >= Mathf.Max(1, activeStep.targetAmount))
        {
            AdvanceStep();
        }
    }

    // ── 이벤트 구독(소스 수정 없이 시설 강화 완료 등을 감지) ────────────────────
    private void SubscribeEvents()
    {
        BaseCampManager camp = BaseCampManager.Instance;
        if (camp != null && subscribedCamp != camp)
        {
            UnsubscribeFacilities();
            camp.CommandCenter?.OnUpgradeCompleted.AddListener(HandleFacilityUpgraded);
            camp.CreditRefinery?.OnUpgradeCompleted.AddListener(HandleFacilityUpgraded);
            camp.AssemblyFactory?.OnUpgradeCompleted.AddListener(HandleFacilityUpgraded);
            camp.CoreCharger?.OnUpgradeCompleted.AddListener(HandleFacilityUpgraded);

            subscribedFactory = camp.AssemblyFactory;
            subscribedFactory?.OnWeaponEnhanced.AddListener(HandleWeaponEnhanced);
            subscribedCharger = camp.CoreCharger;
            subscribedCharger?.OnUnitEnhanced.AddListener(HandleUnitEnhanced);

            subscribedCamp = camp;
        }
    }

    private void UnsubscribeFacilities()
    {
        if (subscribedCamp != null)
        {
            subscribedCamp.CommandCenter?.OnUpgradeCompleted.RemoveListener(HandleFacilityUpgraded);
            subscribedCamp.CreditRefinery?.OnUpgradeCompleted.RemoveListener(HandleFacilityUpgraded);
            subscribedCamp.AssemblyFactory?.OnUpgradeCompleted.RemoveListener(HandleFacilityUpgraded);
            subscribedCamp.CoreCharger?.OnUpgradeCompleted.RemoveListener(HandleFacilityUpgraded);
        }

        subscribedFactory?.OnWeaponEnhanced.RemoveListener(HandleWeaponEnhanced);
        subscribedCharger?.OnUnitEnhanced.RemoveListener(HandleUnitEnhanced);
        subscribedFactory = null;
        subscribedCharger = null;
    }

    private void UnsubscribeEvents()
    {
        UnsubscribeFacilities();
        subscribedCamp = null;
    }

    private void HandleFacilityUpgraded() => HandleEvent(TutorialEventType.FacilityUpgraded, 1);
    private void HandleWeaponEnhanced(ProjectileConfig weapon, int level) => HandleEvent(TutorialEventType.WeaponEnhanced, 1);
    private void HandleUnitEnhanced(PlayerUnitConfig unit, int level) => HandleEvent(TutorialEventType.UnitEnhanced, 1);

    private void Save()
    {
        PlayerPrefs.SetInt(StepIndexKey, stepIndex);
        PlayerPrefs.SetInt(CompletedKey, completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    // 씬 전체에서 이름으로 탐색(활성 우선, 비활성 폴백) — RewardFlyAnimator.FindInScene와 동일 패턴.
    private static Transform FindInScene(string objectName)
    {
        GameObject active = GameObject.Find(objectName);
        if (active != null)
        {
            return active.transform;
        }

        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform tr = all[i];
            if (tr.name == objectName && tr.gameObject.scene.IsValid())
            {
                return tr;
            }
        }

        return null;
    }
}

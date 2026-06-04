using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    private const string CurrentRoundKey = "EnemySpawnManager.CurrentRound";

    [Header("Stage")]
    [SerializeField] private int startStage = 1;
    [SerializeField] private int roundsPerStage = 5;
    [SerializeField] private bool saveToPlayerPrefs = true;

    [Header("Round")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private int startRound = 1;
    [SerializeField] private int baseEnemyCount = 4;
    [SerializeField] private int enemyCountIncreasePerStage = 3;
    [SerializeField] private int enemyCountIncreasePerRound = 1;
    [SerializeField] private float timeBetweenRounds = 2f;

    [Header("Player Death")]
    [SerializeField] private PlayerController player;
    [SerializeField] private float restartDelayOnPlayerDeath = 3f;

    [Header("Enemy Scaling")]
    [SerializeField] private float healthIncreasePerStage = 0.2f;
    [SerializeField] private float damageIncreasePerStage = 0.1f;
    [SerializeField] private float moveSpeedIncreasePerStage = 0.03f;
    [SerializeField] private float rewardIncreasePerStage = 0.15f;

    [Header("Spawn")]
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float spawnInterval = 0.25f;

    private readonly List<CombatHealth> aliveEnemies = new List<CombatHealth>();
    private Coroutine roundRoutine;
    private Coroutine playerDeathRestartRoutine;
    private int currentStage;
    private int currentRound;
    private int currentRoundInStage;
    private bool roundsActive;
    private bool spawningRound;

    public int CurrentStage => currentStage;
    public int CurrentRound => currentRound;
    public int CurrentRoundInStage => currentRoundInStage;
    public int RoundsPerStage => Mathf.Max(1, roundsPerStage);
    public int AliveEnemyCount => aliveEnemies.Count;
    public bool IsSpawningRound => spawningRound;

    private void Awake()
    {
        currentStage = Mathf.Max(1, startStage);
        currentRound = LoadCurrentRound();
        RefreshStageRoundState();
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartRounds();
        }
    }

    private void Update()
    {
        CleanupAliveEnemies();
        HandlePlayerDeathRestart();

        if (roundsActive && !spawningRound && roundRoutine == null && aliveEnemies.Count == 0)
        {
            roundRoutine = StartCoroutine(RoundRoutine());
        }
    }

    public void StartRounds()
    {
        if (roundRoutine != null)
        {
            return;
        }

        // 외부 UI나 게임 상태에서 라운드 생성을 명시적으로 시작할 수 있다.
        roundsActive = true;
        roundRoutine = StartCoroutine(RoundRoutine());
    }

    private IEnumerator RoundRoutine()
    {
        spawningRound = true;

        if (currentRound > startRound)
        {
            yield return new WaitForSeconds(timeBetweenRounds);
        }

        RefreshStageRoundState();
        int spawnCount = GetEnemyCountForRound(currentRound);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy(i);
            yield return new WaitForSeconds(spawnInterval);
        }

        spawningRound = false;
        currentRound++;
        SaveProgress();
        roundRoutine = null;
    }

    public void ResetStageProgress()
    {
        roundsActive = false;

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        if (playerDeathRestartRoutine != null)
        {
            StopCoroutine(playerDeathRestartRoutine);
            playerDeathRestartRoutine = null;
        }

        spawningRound = false;
        ClearSpawnedEnemies();
        RevivePlayer();
        currentRound = Mathf.Max(1, startRound);
        RefreshStageRoundState();
        SaveProgress();

        // 디버그 초기화 직후에도 기존 start 설정에 맞춰 바로 재시작한다.
        if (startOnAwake)
        {
            StartRounds();
        }
    }

    private int GetEnemyCountForRound(int round)
    {
        int stageOffset = Mathf.Max(0, GetStageForRound(round) - startStage);
        int roundInStageOffset = Mathf.Max(0, GetRoundInStage(round) - 1);
        return Mathf.Max(1, baseEnemyCount
            + enemyCountIncreasePerStage * stageOffset
            + enemyCountIncreasePerRound * roundInStageOffset);
    }

    private int GetStageForRound(int round)
    {
        int roundOffset = Mathf.Max(0, round - startRound);
        return Mathf.Max(1, startStage + roundOffset / RoundsPerStage);
    }

    private int GetRoundInStage(int round)
    {
        int roundOffset = Mathf.Max(0, round - startRound);
        return roundOffset % RoundsPerStage + 1;
    }

    private int GetFirstRoundForStage(int stage)
    {
        int stageOffset = Mathf.Max(0, stage - startStage);
        return Mathf.Max(1, startRound + stageOffset * RoundsPerStage);
    }

    private void RefreshStageRoundState()
    {
        currentStage = GetStageForRound(currentRound);
        currentRoundInStage = GetRoundInStage(currentRound);
    }

    private void SpawnEnemy(int index)
    {
        Vector3 spawnPosition = GetSpawnPosition(index);
        GameObject prefab = GetEnemyPrefab();
        GameObject enemyObject = prefab != null
            ? Instantiate(prefab, spawnPosition, Quaternion.identity)
            : CreateFallbackEnemy(spawnPosition);

        // 라운드 적으로 동작하는 데 필요한 전투 컴포넌트를 보강한다.
        EnemyController enemy = enemyObject.GetComponent<EnemyController>();
        if (enemy == null)
        {
            enemy = enemyObject.AddComponent<EnemyController>();
        }

        enemy.Initialize(
            enemyConfig,
            GetEnemyLevel(),
            GetStageScale(healthIncreasePerStage),
            GetStageScale(moveSpeedIncreasePerStage),
            GetStageScale(damageIncreasePerStage),
            GetStageScale(rewardIncreasePerStage));

        CombatHealth enemyHealth = enemyObject.GetComponent<CombatHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = enemyObject.AddComponent<CombatHealth>();
        }

        aliveEnemies.Add(enemyHealth);
    }

    private GameObject GetEnemyPrefab()
    {
        if (enemyConfig != null && enemyConfig.EnemyPrefab != null)
        {
            return enemyConfig.EnemyPrefab;
        }

        return enemyPrefab;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        Vector3 center = GetSpawnCenterPosition();
        float angle = (index * 137.5f + currentRound * 29f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
        return CombatPlane.WithFixedY(center + offset);
    }

    private int GetEnemyLevel()
    {
        // 적 레벨은 현재 스테이지에 맞춰 증가한다.
        return Mathf.Max(1, currentStage);
    }

    private float GetStageScale(float increasePerStage)
    {
        int stageOffset = Mathf.Max(0, currentStage - startStage);
        return Mathf.Max(0.01f, 1f + increasePerStage * stageOffset);
    }

    private Vector3 GetSpawnCenterPosition()
    {
        if (spawnCenter != null)
        {
            return CombatPlane.WithFixedY(spawnCenter.position);
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            return CombatPlane.WithFixedY(player.transform.position);
        }

        return CombatPlane.WithFixedY(transform.position);
    }

    private GameObject CreateFallbackEnemy(Vector3 spawnPosition)
    {
        GameObject enemyObject = new GameObject($"Round {currentRound} Enemy");
        enemyObject.transform.position = CombatPlane.WithFixedY(spawnPosition);

        // 프리팹이 없을 때도 라운드 테스트가 가능하도록 임시 표시를 만든다.
        SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CombatVisualFactory.CreateCircleSprite(Color.red);
        renderer.sortingOrder = 1;

        return enemyObject;
    }

    private void CleanupAliveEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null || aliveEnemies[i].IsDead)
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    private void HandlePlayerDeathRestart()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player == null || player.Health == null || !player.Health.IsDead || playerDeathRestartRoutine != null)
        {
            return;
        }

        playerDeathRestartRoutine = StartCoroutine(RestartCurrentStageAfterPlayerDeath());
    }

    private IEnumerator RestartCurrentStageAfterPlayerDeath()
    {
        int stageToRestart = Mathf.Max(1, currentStage);
        roundsActive = false;

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        spawningRound = false;
        ClearSpawnedEnemies();

        // 사망 연출/확인 시간을 둔 뒤 현재 스테이지를 처음부터 다시 시작한다.
        yield return new WaitForSeconds(Mathf.Max(0f, restartDelayOnPlayerDeath));

        RevivePlayer();
        currentRound = GetFirstRoundForStage(stageToRestart);
        RefreshStageRoundState();
        SaveProgress();
        roundsActive = true;
        playerDeathRestartRoutine = null;
        StartRounds();
    }

    private void RevivePlayer()
    {
        if (player == null || player.Health == null)
        {
            return;
        }

        // CombatHealth 초기화로 사망 상태와 체력을 함께 복구한다.
        player.Health.Initialize(player.Health.MaxHealth);
        CombatPlane.ClampTransform(player.transform);
    }

    private void ClearSpawnedEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                Destroy(enemies[i].gameObject);
            }
        }

        aliveEnemies.Clear();
    }

    private int LoadCurrentRound()
    {
        int defaultRound = Mathf.Max(1, startRound);
        if (!saveToPlayerPrefs)
        {
            return defaultRound;
        }

        return Mathf.Max(defaultRound, PlayerPrefs.GetInt(CurrentRoundKey, defaultRound));
    }

    private void SaveProgress()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetInt(CurrentRoundKey, Mathf.Max(1, currentRound));
        PlayerPrefs.Save();
    }
}

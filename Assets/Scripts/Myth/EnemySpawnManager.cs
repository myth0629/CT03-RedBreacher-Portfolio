using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private int startStage = 1;

    [Header("Round")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private int startRound = 1;
    [SerializeField] private int baseEnemyCount = 4;
    [SerializeField] private int enemyCountIncreasePerRound = 2;
    [SerializeField] private float timeBetweenRounds = 2f;

    [Header("Spawn")]
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float spawnInterval = 0.25f;

    private readonly List<CombatHealth> aliveEnemies = new List<CombatHealth>();
    private Coroutine roundRoutine;
    private int currentStage;
    private int currentRound;
    private bool roundsActive;
    private bool spawningRound;

    public int CurrentStage => currentStage;
    public int CurrentRound => currentRound;
    public int AliveEnemyCount => aliveEnemies.Count;
    public bool IsSpawningRound => spawningRound;

    private void Awake()
    {
        currentStage = Mathf.Max(1, startStage);
        currentRound = Mathf.Max(1, startRound);
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

        currentStage = GetStageForRound(currentRound);
        int spawnCount = GetEnemyCountForRound(currentRound);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy(i);
            yield return new WaitForSeconds(spawnInterval);
        }

        spawningRound = false;
        currentRound++;
        roundRoutine = null;
    }

    private int GetEnemyCountForRound(int round)
    {
        int roundOffset = Mathf.Max(0, round - 1);
        return Mathf.Max(1, baseEnemyCount + enemyCountIncreasePerRound * roundOffset);
    }

    private int GetStageForRound(int round)
    {
        int roundOffset = Mathf.Max(0, round - startRound);
        return Mathf.Max(1, startStage + roundOffset);
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

        enemy.Initialize(enemyConfig);

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
}

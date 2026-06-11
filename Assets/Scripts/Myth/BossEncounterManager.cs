using System;
using UnityEngine;

public class BossEncounterManager : MonoBehaviour
{
    [Header("Encounter")]
    [SerializeField] private EnemySpawnManager enemySpawnManager;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private Transform bossSpawnParent;
    [SerializeField] private BossEncounterHud bossEncounterHud;

    private BossEnemyController activeBoss;
    private CombatHealth activeBossHealth;
    private bool encounterActive;

    public bool IsEncounterActive => encounterActive;
    public event Action<bool> EncounterEnded;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!encounterActive)
        {
            return;
        }

        ResolveReferences();
        if (player != null && player.Health != null && player.Health.IsDead)
        {
            HandlePlayerDefeat();
            return;
        }

        if (activeBossHealth == null || activeBossHealth.IsDead)
        {
            HandleBossDefeat();
        }
    }

    public bool CanSummon(BossEnemyConfig config)
    {
        ResolveReferences();
        return !encounterActive
            && enemySpawnManager != null
            && enemySpawnManager.CanStartBossEncounter
            && config != null
            && config.EnemyPrefab != null
            && config.EnemyPrefab.GetComponent<BossEnemyController>() != null;
    }

    public bool TrySummon(BossEnemyConfig config)
    {
        if (!CanSummon(config) || !enemySpawnManager.PauseForBossEncounter())
        {
            return false;
        }

        Vector3 spawnPosition = bossSpawnPoint != null
            ? CombatPlane.WithFixedY(bossSpawnPoint.position)
            : CombatPlane.WithFixedY(transform.position);
        GameObject bossObject = Instantiate(
            config.EnemyPrefab,
            spawnPosition,
            Quaternion.identity,
            bossSpawnParent);
        activeBoss = bossObject.GetComponent<BossEnemyController>();
        activeBossHealth = bossObject.GetComponent<CombatHealth>();
        if (activeBoss == null || activeBossHealth == null)
        {
            Destroy(bossObject);
            enemySpawnManager.ResumePausedRound();
            return false;
        }

        // 보스 SO의 전투 수치를 적용하고 기존 보상/타겟 경로에 등록한다.
        activeBoss.InitializeBoss(config, enemySpawnManager.CurrentStage);
        encounterActive = true;
        bossEncounterHud?.Show(config, activeBossHealth);
        return true;
    }

    private void HandleBossDefeat()
    {
        encounterActive = false;
        activeBoss = null;
        activeBossHealth = null;
        bossEncounterHud?.Hide();
        enemySpawnManager?.ResumePausedRound();
        EncounterEnded?.Invoke(true);
    }

    private void HandlePlayerDefeat()
    {
        encounterActive = false;
        if (activeBoss != null)
        {
            Destroy(activeBoss.gameObject);
        }

        activeBoss = null;
        activeBossHealth = null;
        bossEncounterHud?.Hide();
        enemySpawnManager?.CancelBossEncounterForPlayerDeath();
        EncounterEnded?.Invoke(false);
    }

    public void ShowResult(string title, string detail, bool success)
    {
        bossEncounterHud?.ShowResult(title, detail, success);
    }

    private void ResolveReferences()
    {
        enemySpawnManager ??= FindFirstObjectByType<EnemySpawnManager>();
        player ??= FindFirstObjectByType<PlayerController>();
        bossEncounterHud ??= FindFirstObjectByType<BossEncounterHud>();
    }
}

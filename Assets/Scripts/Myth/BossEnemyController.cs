using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CombatHealth))]
public class BossEnemyController : EnemyController
{
    [SerializeField] private BossEnemyConfig bossConfig;
    [SerializeField] private Transform firePoint;

    private readonly List<Transform> firePoints = new List<Transform>();
    private Transform laserFirePoint;
    private LineRenderer laserLine;
    private Coroutine laserRoutine;
    private Vector3 dodgeDestination;
    private float nextRangedAttackTime;
    private float nextLaserTime;
    private float nextDodgeTime;
    private float nextDodgeCheckTime;
    private float attackDamageMultiplier = 1f;
    private bool isDodging;

    protected override void Awake()
    {
        base.Awake();
        ResolveFirePoints();
        EnsureLaserLine();
    }

    protected override void Start()
    {
        base.Start();
        nextLaserTime = Time.time + (bossConfig != null ? bossConfig.LaserCooldown : 0f);
    }

    public void InitializeBoss(BossEnemyConfig config, int level)
    {
        InitializeBoss(config, level, 1f, 1f, 1f, 1f);
    }

    public void InitializeBoss(
        BossEnemyConfig config,
        int level,
        float healthScale,
        float moveSpeedScale,
        float damageScale,
        float rewardScale)
    {
        bossConfig = config;
        attackDamageMultiplier = Mathf.Max(0.01f, damageScale);
        Initialize(
            config,
            level,
            healthScale,
            moveSpeedScale,
            damageScale,
            rewardScale);
        ResolveFirePoints();
        EnsureLaserLine();
        nextRangedAttackTime = Time.time;
        nextLaserTime = Time.time + bossConfig.LaserCooldown;
        nextDodgeTime = Time.time;
        nextDodgeCheckTime = Time.time;
        isDodging = false;
    }

    public override void ApplyKnockback(Vector3 knockbackDirection, float knockbackForce)
    {
        // 보스는 피해만 받고 무기나 스킬의 넉백에는 밀려나지 않는다.
    }

    protected override void FixedUpdate()
    {
        CombatPlane.ClampTransform(transform);
        PlayerController player = ResolveTargetPlayer();
        if (bossConfig == null || player == null || player.Health == null || player.Health.IsDead)
        {
            StopBody();
            return;
        }

        if (laserRoutine != null)
        {
            StopBody();
            return;
        }

        Vector3 currentPosition = CombatPlane.WithFixedY(transform.position);
        Vector3 targetPosition = CombatPlane.WithFixedY(player.transform.position);
        Vector3 targetDirection = CombatPlane.Direction(currentPosition, targetPosition);
        float targetDistance = Mathf.Sqrt(CombatPlane.DistanceSqr(currentPosition, targetPosition));
        if (targetDirection.sqrMagnitude <= 0f)
        {
            StopBody();
            return;
        }

        if (isDodging)
        {
            UpdateDodge(targetDirection);
            TryFireWhileInRange(targetDirection, targetDistance);
            return;
        }

        TryStartDodge(player);
        if (isDodging)
        {
            UpdateDodge(targetDirection);
            TryFireWhileInRange(targetDirection, targetDistance);
            return;
        }

        if (targetDistance > bossConfig.RangedAttackRange)
        {
            // 보스는 원거리 사거리 경계까지만 플레이어에게 접근한다.
            float moveDistance = Mathf.Min(
                MoveSpeedValue * Time.fixedDeltaTime,
                targetDistance - bossConfig.RangedAttackRange);
            transform.position = CombatPlane.WithFixedY(currentPosition + targetDirection * moveDistance);
            CombatPlane.SetYOnlyRotation(transform, targetDirection);
            StopBody();
            return;
        }

        CombatPlane.SetYOnlyRotation(transform, targetDirection);
        StopBody();

        if (Time.time >= nextLaserTime && bossConfig.LaserLineMaterial != null)
        {
            laserRoutine = StartCoroutine(ExecuteLaser(targetDirection));
            return;
        }

        TryFireWhileInRange(targetDirection, targetDistance);
    }

    private void TryFireWhileInRange(Vector3 targetDirection, float targetDistance)
    {
        if (targetDistance > bossConfig.RangedAttackRange || Time.time < nextRangedAttackTime)
        {
            return;
        }

        FireSpread(targetDirection);
        nextRangedAttackTime = Time.time + bossConfig.RangedAttackInterval;
    }

    private void FireSpread(Vector3 centerDirection)
    {
        int muzzleCount = Mathf.Max(1, firePoints.Count);
        int projectilesPerMuzzle = bossConfig.ProjectilesPerFirePoint;
        int totalProjectileCount = muzzleCount * projectilesPerMuzzle;
        float damagePerProjectile = bossConfig.RangedAttackDamage
            * attackDamageMultiplier
            / totalProjectileCount;

        for (int muzzleIndex = 0; muzzleIndex < muzzleCount; muzzleIndex++)
        {
            Transform muzzle = firePoints.Count > 0 ? firePoints[muzzleIndex] : null;
            Vector3 spawnPosition = muzzle != null
                ? CombatPlane.WithFixedY(muzzle.position)
                : CombatPlane.WithFixedY(transform.position + centerDirection * 0.5f);
            SpawnFireEffect(spawnPosition, centerDirection);

            for (int projectileIndex = 0; projectileIndex < projectilesPerMuzzle; projectileIndex++)
            {
                float angle = GetSpreadAngle(projectileIndex, projectilesPerMuzzle);
                Vector3 projectileDirection = CombatPlane.ProjectDirection(
                    Quaternion.AngleAxis(angle, Vector3.up) * centerDirection);
                BossProjectile projectile = CombatObjectPool.GetBossProjectile();
                projectile.transform.position = spawnPosition;
                projectile.Launch(bossConfig, projectileDirection, damagePerProjectile);
            }
        }
    }

    private float GetSpreadAngle(int index, int projectileCount)
    {
        if (projectileCount <= 1)
        {
            return 0f;
        }

        float progress = (float)index / (projectileCount - 1);
        return Mathf.Lerp(-bossConfig.SpreadAngle * 0.5f, bossConfig.SpreadAngle * 0.5f, progress);
    }

    private void SpawnFireEffect(Vector3 position, Vector3 direction)
    {
        if (bossConfig.FireEffectPrefab == null)
        {
            return;
        }

        GameObject flash = CombatObjectPool.GetEffect(
            bossConfig.FireEffectPrefab,
            position,
            Quaternion.LookRotation(direction, Vector3.up));
        CombatObjectPool.ReleaseEffect(flash, bossConfig.EffectCleanupDelay);
    }

    private void TryStartDodge(PlayerController player)
    {
        if (Time.time < nextDodgeTime || Time.time < nextDodgeCheckTime)
        {
            return;
        }

        nextDodgeCheckTime = Time.time + bossConfig.DodgeCheckInterval;
        PlayerProjectile threat = FindClosestProjectileThreat();
        if (threat == null)
        {
            return;
        }

        Vector3 projectileDirection = threat.TravelDirection;
        Vector3 leftDirection = CombatPlane.ProjectDirection(
            new Vector3(-projectileDirection.z, 0f, projectileDirection.x));
        Vector3 rightDirection = -leftDirection;
        bool canDodgeLeft = CanDodge(leftDirection);
        bool canDodgeRight = CanDodge(rightDirection);
        if (!canDodgeLeft && !canDodgeRight)
        {
            return;
        }

        Vector3 selectedDirection;
        if (canDodgeLeft && canDodgeRight)
        {
            Vector3 currentPosition = CombatPlane.WithFixedY(transform.position);
            Vector3 playerPosition = CombatPlane.WithFixedY(player.transform.position);
            float leftDistance = CombatPlane.DistanceSqr(
                playerPosition,
                currentPosition + leftDirection * bossConfig.DodgeDistance);
            float rightDistance = CombatPlane.DistanceSqr(
                playerPosition,
                currentPosition + rightDirection * bossConfig.DodgeDistance);
            selectedDirection = leftDistance >= rightDistance ? leftDirection : rightDirection;
        }
        else
        {
            selectedDirection = canDodgeLeft ? leftDirection : rightDirection;
        }

        // 접근하는 투사체 진행 방향의 수직 방향으로 짧고 빠르게 회피한다.
        dodgeDestination = CombatPlane.WithFixedY(
            transform.position + selectedDirection * bossConfig.DodgeDistance);
        isDodging = true;
        nextDodgeTime = Time.time + bossConfig.DodgeCooldown;
    }

    private PlayerProjectile FindClosestProjectileThreat()
    {
        PlayerProjectile[] projectiles = FindObjectsByType<PlayerProjectile>(FindObjectsSortMode.None);
        PlayerProjectile closestThreat = null;
        float closestDistanceSqr = float.PositiveInfinity;
        float detectionRadiusSqr = bossConfig.DodgeDetectionRadius * bossConfig.DodgeDetectionRadius;
        Vector3 bossPosition = CombatPlane.WithFixedY(transform.position);

        for (int i = 0; i < projectiles.Length; i++)
        {
            PlayerProjectile projectile = projectiles[i];
            if (projectile == null || !projectile.IsInFlight || projectile.TravelSpeed <= 0f)
            {
                continue;
            }

            Vector3 projectilePosition = CombatPlane.WithFixedY(projectile.transform.position);
            float distanceSqr = CombatPlane.DistanceSqr(projectilePosition, bossPosition);
            if (distanceSqr > detectionRadiusSqr || distanceSqr >= closestDistanceSqr)
            {
                continue;
            }

            Vector3 velocity = projectile.TravelVelocity;
            Vector3 toBoss = bossPosition - projectilePosition;
            float velocitySqr = velocity.sqrMagnitude;
            if (velocitySqr <= 0f || Vector3.Dot(velocity, toBoss) <= 0f)
            {
                continue;
            }

            float closestTime = Vector3.Dot(toBoss, velocity) / velocitySqr;
            if (closestTime < 0f || closestTime > bossConfig.DodgePredictionTime)
            {
                continue;
            }

            Vector3 predictedPosition = projectilePosition + velocity * closestTime;
            if (CombatPlane.DistanceSqr(predictedPosition, bossPosition)
                > bossConfig.DodgeCollisionRadius * bossConfig.DodgeCollisionRadius)
            {
                continue;
            }

            closestThreat = projectile;
            closestDistanceSqr = distanceSqr;
        }

        return closestThreat;
    }

    private bool CanDodge(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return false;
        }

        LayerMask obstacleMask = bossConfig.DodgeObstacleMask;
        if (obstacleMask.value == 0)
        {
            return true;
        }

        return !Physics.SphereCast(
            CombatPlane.WithFixedY(transform.position),
            bossConfig.DodgeCollisionRadius,
            direction,
            out _,
            bossConfig.DodgeDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private void UpdateDodge(Vector3 targetDirection)
    {
        Vector3 currentPosition = CombatPlane.WithFixedY(transform.position);
        float remainingDistance = Mathf.Sqrt(
            CombatPlane.DistanceSqr(currentPosition, dodgeDestination));
        if (remainingDistance <= 0.05f)
        {
            transform.position = dodgeDestination;
            isDodging = false;
            StopBody();
            return;
        }

        float moveDistance = Mathf.Min(
            bossConfig.DodgeSpeed * Time.fixedDeltaTime,
            remainingDistance);
        transform.position = CombatPlane.WithFixedY(
            Vector3.MoveTowards(currentPosition, dodgeDestination, moveDistance));
        CombatPlane.SetYOnlyRotation(transform, targetDirection);
        StopBody();
    }

    private IEnumerator ExecuteLaser(Vector3 lockedDirection)
    {
        isDodging = false;
        EnsureLaserLine();
        if (laserLine == null)
        {
            laserRoutine = null;
            nextLaserTime = Time.time + bossConfig.LaserCooldown;
            yield break;
        }

        // 경고가 시작된 순간의 방향을 고정해 플레이어가 범위를 보고 회피할 수 있게 한다.
        SetLaserLine(lockedDirection, bossConfig.LaserWarningColor, true);
        float warningEndTime = Time.time + bossConfig.LaserWarningDuration;
        while (Time.time < warningEndTime)
        {
            UpdateLaserPositions(lockedDirection);
            yield return null;
        }

        SpawnLaserBeamEffect(lockedDirection);
        ApplyLaserDamage(lockedDirection);
        SetLaserLine(lockedDirection, bossConfig.LaserActiveColor, true);

        float activeEndTime = Time.time + bossConfig.LaserActiveDuration;
        while (Time.time < activeEndTime)
        {
            UpdateLaserPositions(lockedDirection);
            yield return null;
        }

        laserLine.enabled = false;
        nextLaserTime = Time.time + bossConfig.LaserCooldown;
        laserRoutine = null;
    }

    private void SpawnLaserBeamEffect(Vector3 direction)
    {
        if (bossConfig.LaserBeamEffectPrefab == null)
        {
            return;
        }

        UpdateLaserPositions(direction);
        Vector3 lineStart = laserLine.GetPosition(0);
        Vector3 lineEnd = laserLine.GetPosition(1);
        Vector3 lineDirection = CombatPlane.ProjectDirection(lineEnd - lineStart);
        if (lineDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        Quaternion lineRotation = Quaternion.LookRotation(lineDirection, Vector3.up)
            * Quaternion.Euler(bossConfig.LaserBeamRotationOffset);
        GameObject beamEffect = CombatObjectPool.GetEffect(
            bossConfig.LaserBeamEffectPrefab,
            lineStart,
            lineRotation);
        if (beamEffect == null)
        {
            return;
        }

        // VFX를 경고선 방향으로 실제 투사체처럼 직진시킨다.
        DisableLaserEffectMotion(beamEffect);
        beamEffect.transform.position = lineStart;
        beamEffect.transform.rotation = lineRotation;
        beamEffect.transform.localScale = Vector3.one * bossConfig.LaserProjectileScale;

        BossLaserEffectMover mover = beamEffect.GetComponent<BossLaserEffectMover>();
        if (mover == null)
        {
            mover = beamEffect.AddComponent<BossLaserEffectMover>();
        }

        mover.Launch(
            lineDirection,
            bossConfig.LaserProjectileSpeed,
            Vector3.Distance(lineStart, lineEnd));
    }

    private static void DisableLaserEffectMotion(GameObject effect)
    {
        MonoBehaviour[] behaviours = effect.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            string typeName = behaviours[i].GetType().Name;
            if (typeName == "ProjectileMoveScript" || typeName == "HS_ProjectileMover")
            {
                behaviours[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = effect.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody effectBody = rigidbodies[i];
            if (!effectBody.isKinematic)
            {
                effectBody.linearVelocity = Vector3.zero;
                effectBody.angularVelocity = Vector3.zero;
            }

            effectBody.isKinematic = true;
            effectBody.useGravity = false;
        }

        Collider[] colliders = effect.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void ApplyLaserDamage(Vector3 direction)
    {
        Vector3 origin = GetLaserOrigin();
        Vector3 center = origin + direction * (bossConfig.LaserLength * 0.5f);
        Vector3 halfExtents = new Vector3(
            bossConfig.LaserWidth * 0.5f,
            1f,
            bossConfig.LaserLength * 0.5f);
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerController player = hits[i].GetComponentInParent<PlayerController>();
            if (player == null || player.Health == null || player.Health.IsDead)
            {
                continue;
            }

            player.Health.TakeDamage(bossConfig.LaserDamage * attackDamageMultiplier);
            return;
        }
    }

    private void EnsureLaserLine()
    {
        if (laserLine == null)
        {
            laserLine = GetComponentInChildren<LineRenderer>(true);
        }

        if (laserLine == null)
        {
            GameObject lineObject = new GameObject("Boss Laser Warning");
            lineObject.transform.SetParent(transform, false);
            laserLine = lineObject.AddComponent<LineRenderer>();
        }

        laserLine.useWorldSpace = true;
        laserLine.positionCount = 2;
        laserLine.textureMode = LineTextureMode.Stretch;
        laserLine.alignment = LineAlignment.View;
        laserLine.enabled = false;
        if (bossConfig != null && bossConfig.LaserLineMaterial != null)
        {
            laserLine.material = bossConfig.LaserLineMaterial;
        }
    }

    private void SetLaserLine(Vector3 direction, Color color, bool enabled)
    {
        laserLine.startWidth = bossConfig.LaserWidth;
        laserLine.endWidth = bossConfig.LaserWidth;
        laserLine.startColor = color;
        laserLine.endColor = color;
        laserLine.enabled = enabled;
        UpdateLaserPositions(direction);
    }

    private void UpdateLaserPositions(Vector3 direction)
    {
        Vector3 origin = GetLaserOrigin();
        origin.y += 0.03f;
        laserLine.SetPosition(0, origin);
        laserLine.SetPosition(1, origin + direction * bossConfig.LaserLength);
    }

    private Vector3 GetLaserOrigin()
    {
        return laserFirePoint != null
            ? CombatPlane.WithFixedY(laserFirePoint.position)
            : CombatPlane.WithFixedY(transform.position);
    }

    private void ResolveFirePoints()
    {
        firePoints.Clear();
        if (bossConfig != null)
        {
            CollectFirePoints(transform, bossConfig.FirePointNamePrefix, firePoints);
            laserFirePoint = FindChild(transform, bossConfig.LaserFirePointName);
        }

        if (firePoint != null && !firePoints.Contains(firePoint))
        {
            firePoints.Add(firePoint);
        }

        if (firePoint == null && firePoints.Count > 0)
        {
            firePoint = firePoints[0];
        }

        if (laserFirePoint == null)
        {
            laserFirePoint = firePoints.Count > 0 ? firePoints[0] : firePoint;
        }
    }

    private static void CollectFirePoints(
        Transform root,
        string namePrefix,
        List<Transform> results)
    {
        if (root == null)
        {
            return;
        }

        if (root.name.StartsWith(namePrefix))
        {
            results.Add(root);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectFirePoints(root.GetChild(i), namePrefix, results);
        }
    }

    private static Transform FindChild(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChild(root.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isDodging = false;
        if (laserRoutine != null)
        {
            StopCoroutine(laserRoutine);
            laserRoutine = null;
        }

        if (laserLine != null)
        {
            laserLine.enabled = false;
        }
    }
}

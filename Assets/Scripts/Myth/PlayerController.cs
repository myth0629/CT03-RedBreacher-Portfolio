using UnityEngine;
using TopDownAssets.Common.Scripts;

[RequireComponent(typeof(CombatHealth))]
public class PlayerController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerUnitConfig unitConfig;

    [Header("Auto Combat")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float fireAngleTolerance = 3f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private Transform firePoint;

    [Header("Fallback Projectile")]
    [SerializeField] private ProjectileConfig projectileConfig;
    [SerializeField] private PlayerProjectile projectilePrefab;
    [SerializeField] private GameObject fireFlashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;

    private CombatHealth health;
    private Vehicle vehicle;
    private CombatHealth currentTarget;
    private float nextAttackTime;

    public CombatHealth Health => health;
    public float AttackRange => AttackRangeValue;

    private float AttackRangeValue => unitConfig != null ? unitConfig.AttackRange : attackRange;
    private float AttackDamageValue => unitConfig != null ? unitConfig.AttackDamage : attackDamage;
    private float AttackIntervalValue => unitConfig != null ? unitConfig.AttackInterval : attackInterval;
    private float MoveSpeedValue => unitConfig != null ? unitConfig.MoveSpeed : moveSpeed;
    private float RotationSpeedValue => unitConfig != null ? unitConfig.RotationSpeed : rotationSpeed;
    private float FireAngleToleranceValue => unitConfig != null ? unitConfig.FireAngleTolerance : fireAngleTolerance;
    private ProjectileConfig ProjectileConfigValue => unitConfig != null && unitConfig.ProjectileConfig != null ? unitConfig.ProjectileConfig : projectileConfig;

    private void Awake()
    {
        health = GetComponent<CombatHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<CombatHealth>();
        }

        EnsureCombatComponents();
        vehicle = GetComponentInChildren<Vehicle>();
    }

    private void Update()
    {
        CombatPlane.ClampTransform(transform);

        if (health != null && health.IsDead)
        {
            return;
        }

        // 타겟은 한 번 잡으면 처치되거나 사라질 때까지 유지한다.
        if (!HasValidCurrentTarget())
        {
            currentTarget = FindClosestTarget();
        }

        if (currentTarget == null)
        {
            SetVehicleMoveInput(0f, 0f);
            return;
        }

        Vector3 targetDirection = CombatPlane.Direction(transform.position, currentTarget.transform.position);
        if (targetDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        RotateToward(targetDirection);

        if (!IsTargetInRange(currentTarget))
        {
            MoveUntilInRange(currentTarget, targetDirection);
            return;
        }

        SetVehicleMoveInput(0f, 0f);

        if (!IsFacing(targetDirection) || Time.time < nextAttackTime)
        {
            return;
        }

        FireForward();
        nextAttackTime = Time.time + AttackIntervalValue;
    }

    private bool HasValidCurrentTarget()
    {
        return currentTarget != null && !currentTarget.IsDead;
    }

    private void EnsureCombatComponents()
    {
        // 탑뷰 전투는 X/Z 평면만 쓰고 Y 높이는 0.1로 고정한다.
        CombatPlane.ClampTransform(transform);

        // 테스트 씬에서도 바로 동작하도록 필수 3D 물리 컴포넌트를 보강한다.
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        if (GetComponent<Collider>() == null)
        {
            SphereCollider playerCollider = gameObject.AddComponent<SphereCollider>();
            playerCollider.radius = 0.45f;
        }

        if (firePoint == null)
        {
            GameObject point = new GameObject("FirePoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = Vector3.forward * 0.45f;
            firePoint = point.transform;
        }
    }

    private CombatHealth FindClosestTarget()
    {
        CombatHealth closestTarget = null;
        float closestDistanceSqr = float.PositiveInfinity;

        // EnemyController가 붙은 적을 우선 탐색한다.
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            CombatHealth enemyHealth = enemies[i].GetComponent<CombatHealth>();
            closestTarget = SelectCloserTarget(enemyHealth, closestTarget, ref closestDistanceSqr);
        }

        // 태그/프리팹 구성이 없어도 CombatHealth만 있으면 기본 타겟으로 취급한다.
        CombatHealth[] healthTargets = FindObjectsByType<CombatHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < healthTargets.Length; i++)
        {
            if (healthTargets[i].GetComponent<PlayerController>() != null)
            {
                continue;
            }

            closestTarget = SelectCloserTarget(healthTargets[i], closestTarget, ref closestDistanceSqr);
        }

        return closestTarget;
    }

    private CombatHealth SelectCloserTarget(CombatHealth target, CombatHealth currentClosest, ref float closestDistanceSqr)
    {
        if (target == null || target == health || target.IsDead)
        {
            return currentClosest;
        }

        if (targetMask.value != 0 && ((1 << target.gameObject.layer) & targetMask.value) == 0)
        {
            return currentClosest;
        }

        float distanceSqr = CombatPlane.DistanceSqr(transform.position, target.transform.position);
        if (distanceSqr > closestDistanceSqr)
        {
            return currentClosest;
        }

        closestDistanceSqr = distanceSqr;
        return target;
    }

    private bool IsTargetInRange(CombatHealth target)
    {
        return CombatPlane.DistanceSqr(transform.position, target.transform.position) <= AttackRangeValue * AttackRangeValue;
    }

    private void RotateToward(Vector3 direction)
    {
        // 플레이어 정면이 타겟을 향하도록 X/Z 평면에서만 회전한다.
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeedValue * Time.deltaTime);
    }

    private void MoveUntilInRange(CombatHealth target, Vector3 direction)
    {
        float distance = Mathf.Sqrt(CombatPlane.DistanceSqr(transform.position, target.transform.position));
        float remainingMoveDistance = Mathf.Max(0f, distance - AttackRangeValue);
        float moveDistance = Mathf.Min(MoveSpeedValue * Time.deltaTime, remainingMoveDistance);

        // 사거리 경계까지만 이동해 타겟을 지나치지 않게 한다.
        transform.position = CombatPlane.WithFixedY(transform.position + direction * moveDistance);
        SetVehicleMoveInput(moveDistance > 0f ? 1f : 0f, 0f);
    }

    private bool IsFacing(Vector3 direction)
    {
        Vector3 forward = CombatPlane.ProjectDirection(transform.forward);
        if (forward.sqrMagnitude <= 0f)
        {
            return false;
        }

        return Vector3.Angle(forward, direction) <= FireAngleToleranceValue;
    }

    private void FireForward()
    {
        Vector3 direction = CombatPlane.ProjectDirection(firePoint.forward);
        if (direction.sqrMagnitude <= 0f)
        {
            direction = CombatPlane.ProjectDirection(transform.forward);
        }

        if (direction.sqrMagnitude <= 0f)
        {
            direction = Vector3.forward;
        }

        // 투사체는 타겟 위치가 아니라 플레이어 정면 방향으로만 발사한다.
        ProjectileConfig activeProjectileConfig = ProjectileConfigValue;
        PlayerProjectile projectile = CreateProjectile();
        projectile.transform.position = CombatPlane.WithFixedY(firePoint.position);
        projectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        projectile.Configure(activeProjectileConfig);
        projectile.ConfigureEffects(fireFlashEffectPrefab, projectileEffectPrefab, hitEffectPrefab);
        projectile.Launch(direction, AttackDamageValue, GetProjectileSpeed(activeProjectileConfig), GetProjectileLifetime(activeProjectileConfig), health);
    }

    private PlayerProjectile CreateProjectile()
    {
        PlayerProjectile activeProjectilePrefab = GetProjectilePrefab(ProjectileConfigValue);
        if (activeProjectilePrefab != null)
        {
            return Instantiate(activeProjectilePrefab);
        }

        // 프리팹이 없어도 기본 발사체를 런타임에 만들어 전투 루프를 검증한다.
        GameObject fallbackProjectileObject = new GameObject("Combat Projectile");
        return fallbackProjectileObject.AddComponent<PlayerProjectile>();
    }

    private PlayerProjectile GetProjectilePrefab(ProjectileConfig activeProjectileConfig)
    {
        if (activeProjectileConfig != null && activeProjectileConfig.ProjectilePrefab != null)
        {
            return activeProjectileConfig.ProjectilePrefab;
        }

        return projectilePrefab;
    }

    private float GetProjectileSpeed(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null ? activeProjectileConfig.Speed : projectileSpeed;
    }

    private float GetProjectileLifetime(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null ? activeProjectileConfig.Lifetime : projectileLifetime;
    }

    private void SetVehicleMoveInput(float torque, float steering)
    {
        if (vehicle == null)
        {
            return;
        }

        // 실제 이동은 PlayerController가 하고, Vehicle은 바퀴/트랙 연출만 따라간다.
        vehicle.SetAutoMoveInput(torque, steering);
    }
}

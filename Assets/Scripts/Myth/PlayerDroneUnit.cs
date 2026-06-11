using UnityEngine;

public class PlayerDroneUnit : MonoBehaviour
{
    private PlayerController player;
    private DroneConfig config;
    private Transform muzzle;
    private CombatHealth currentTarget;
    private int slotIndex;
    private int slotCount;
    private float nextAttackTime;

    public void Initialize(PlayerController owner, DroneConfig droneConfig, int index, int count)
    {
        player = owner;
        config = droneConfig;
        slotIndex = index;
        slotCount = Mathf.Max(1, count);
        muzzle = FindMuzzle(transform);
        CombatPlane.ClampTransform(transform);

        // 드론 외형과 동일한 실루엣 그림자를 생성한다.
        SpriteShapeShadow.Ensure(gameObject);
    }

    private void Update()
    {
        if (player == null || config == null || player.Health == null || player.Health.IsDead)
        {
            return;
        }

        FollowPlayerSlot();

        if (!HasValidTarget())
        {
            currentTarget = FindClosestTarget();
        }

        if (currentTarget == null || !IsTargetInRange(currentTarget))
        {
            return;
        }

        Vector3 direction = CombatPlane.Direction(transform.position, currentTarget.transform.position);
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        CombatPlane.RotateZOnlyToward(transform, direction, config.RotationSpeed * Time.deltaTime);
        if (!IsFacing(direction) || Time.time < nextAttackTime)
        {
            return;
        }

        Fire(direction);
        nextAttackTime = Time.time + GetAttackInterval();
    }

    private void FollowPlayerSlot()
    {
        Vector3 targetPosition = GetSlotPosition();
        CoreCharger coreCharger = GetCoreCharger();
        float followSpeedBonus = coreCharger != null ? coreCharger.DroneFollowSpeedBonus : 0f;
        float followRate = Mathf.Max(0f, config.FollowSpeed + followSpeedBonus) * Time.deltaTime;
        transform.position = CombatPlane.WithFixedY(Vector3.Lerp(transform.position, targetPosition, followRate));
    }

    private Vector3 GetSlotPosition()
    {
        float angleDegrees;
        if (slotCount == 1)
        {
            // 드론이 1개일 때는 플레이어의 바로 뒤(180도)에 배치
            angleDegrees = 180f;
        }
        else
        {
            // 드론이 2개 이상일 때는 플레이어의 좌측 양옆(90도)부터 우측 양옆(270도)까지 뒤쪽 반원에 걸쳐 고르게 분배
            float progress = (float)slotIndex / (slotCount - 1);
            angleDegrees = Mathf.Lerp(90f, 270f, progress);
        }

        // 플레이어의 몸체 회전 각도(Y Rotation)를 반영하여 플레이어 기준의 로컬 방향으로 회전시킴
        float playerYAngle = player.transform.eulerAngles.y;
        float finalAngle = (playerYAngle + angleDegrees) * Mathf.Deg2Rad;

        // X/Z 평면 기준 오프셋 계산 (Z는 정면, X는 오른쪽)
        Vector3 offset = new Vector3(Mathf.Sin(finalAngle), 0f, Mathf.Cos(finalAngle)) * config.FollowRadius;
        return CombatPlane.WithFixedY(player.transform.position + offset);
    }

    private bool HasValidTarget()
    {
        return currentTarget != null && !currentTarget.IsDead && IsTargetInRange(currentTarget);
    }

    private CombatHealth FindClosestTarget()
    {
        CombatHealth closestTarget = null;
        float closestDistanceSqr = float.PositiveInfinity;

        // 드론은 플레이어와 별도로 가장 가까운 적을 탐색한다.
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            CombatHealth targetHealth = enemies[i].GetComponent<CombatHealth>();
            closestTarget = SelectCloserTarget(targetHealth, closestTarget, ref closestDistanceSqr);
        }

        return closestTarget;
    }

    private CombatHealth SelectCloserTarget(CombatHealth target, CombatHealth currentClosest, ref float closestDistanceSqr)
    {
        if (target == null || target.IsDead || target == player.Health)
        {
            return currentClosest;
        }

        LayerMask targetMask = config.TargetMask;
        if (targetMask.value != 0 && ((1 << target.gameObject.layer) & targetMask.value) == 0)
        {
            return currentClosest;
        }

        float distanceSqr = CombatPlane.DistanceSqr(transform.position, target.transform.position);
        float attackRange = GetAttackRange();
        if (distanceSqr > closestDistanceSqr || distanceSqr > attackRange * attackRange)
        {
            return currentClosest;
        }

        closestDistanceSqr = distanceSqr;
        return target;
    }

    private bool IsTargetInRange(CombatHealth target)
    {
        float attackRange = GetAttackRange();
        return target != null && CombatPlane.DistanceSqr(transform.position, target.transform.position) <= attackRange * attackRange;
    }

    private bool IsFacing(Vector3 direction)
    {
        Vector3 forward = CombatPlane.DirectionFromZRotation(transform);
        return forward.sqrMagnitude > 0f && Vector3.Angle(forward, direction) <= config.FireAngleTolerance;
    }

    private void Fire(Vector3 direction)
    {
        PlayerProjectile projectile = CombatObjectPool.GetProjectile();
        ProjectileConfig projectileConfig = config.ProjectileConfig;
        projectile.transform.position = GetFirePosition(direction);
        projectile.Configure(projectileConfig);
        projectile.Launch(direction, GetDamage(projectileConfig), config.ProjectileSpeed, config.ProjectileLifetime, player.Health);
    }

    private float GetDamage(ProjectileConfig projectileConfig)
    {
        return config.AttackDamage
            + (projectileConfig != null ? projectileConfig.AttackDamage : 0f)
            + (GetCoreCharger()?.DroneAttackDamageBonus ?? 0f);
    }

    private float GetAttackRange()
    {
        return Mathf.Max(0f, config.AttackRange + (GetCoreCharger()?.DroneAttackRangeBonus ?? 0f));
    }

    private float GetAttackInterval()
    {
        return Mathf.Max(
            0.01f,
            config.AttackInterval - (GetCoreCharger()?.DroneAttackIntervalReduction ?? 0f));
    }

    private static CoreCharger GetCoreCharger()
    {
        return BaseCampManager.Instance != null ? BaseCampManager.Instance.CoreCharger : null;
    }

    private Vector3 GetFirePosition(Vector3 direction)
    {
        if (muzzle != null)
        {
            return CombatPlane.WithFixedY(muzzle.position);
        }

        return CombatPlane.WithFixedY(transform.position + direction * 0.25f);
    }

    private Transform FindMuzzle(Transform root)
    {
        if (root == null || config == null)
        {
            return null;
        }

        string prefix = string.IsNullOrWhiteSpace(config.MuzzleNamePrefix) ? "FireMuzzle" : config.MuzzleNamePrefix;
        if (root.name.StartsWith(prefix))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindMuzzle(root.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

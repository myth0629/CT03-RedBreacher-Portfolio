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
        nextAttackTime = Time.time + Mathf.Max(0.01f, config.AttackInterval);
    }

    private void FollowPlayerSlot()
    {
        Vector3 targetPosition = GetSlotPosition();
        float followRate = Mathf.Max(0f, config.FollowSpeed) * Time.deltaTime;
        transform.position = CombatPlane.WithFixedY(Vector3.Lerp(transform.position, targetPosition, followRate));
    }

    private Vector3 GetSlotPosition()
    {
        float centeredOffset = slotIndex - (slotCount - 1) * 0.5f;
        float angle = (config.StartAngle + centeredOffset * config.AngleStep) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * config.FollowRadius;
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
        if (distanceSqr > closestDistanceSqr || distanceSqr > config.AttackRange * config.AttackRange)
        {
            return currentClosest;
        }

        closestDistanceSqr = distanceSqr;
        return target;
    }

    private bool IsTargetInRange(CombatHealth target)
    {
        return target != null && CombatPlane.DistanceSqr(transform.position, target.transform.position) <= config.AttackRange * config.AttackRange;
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
        return config.AttackDamage + (projectileConfig != null ? projectileConfig.AttackDamage : 0f);
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

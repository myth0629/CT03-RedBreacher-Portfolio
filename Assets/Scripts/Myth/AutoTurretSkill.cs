using UnityEngine;

public class AutoTurretSkill : MonoBehaviour
{
    private static readonly Quaternion SkillSpawnRotation = Quaternion.Euler(90f, 0f, 0f);

    private PlayerController owner;
    private PlayerSkillConfig config;
    private Transform firePoint;
    private CombatHealth currentTarget;
    private float expireTime;
    private float nextAttackTime;
    private float nextTargetSearchTime;

    public static bool Spawn(
        PlayerController player,
        PlayerSkillConfig skillConfig,
        Vector3 targetPosition)
    {
        if (player == null || skillConfig == null)
        {
            return false;
        }

        Vector3 placementDirection = CombatPlane.Direction(player.transform.position, targetPosition);
        if (placementDirection.sqrMagnitude <= 0f)
        {
            placementDirection = CombatPlane.DirectionFromYRotation(player.transform);
        }

        Vector3 placementPosition = CombatPlane.WithFixedY(
            player.transform.position + placementDirection * skillConfig.TurretPlacementDistance);
        GameObject turretObject = skillConfig.TurretPrefab != null
            ? Instantiate(skillConfig.TurretPrefab, placementPosition, SkillSpawnRotation)
            : new GameObject($"AutoTurret_{skillConfig.Id}");
        turretObject.transform.position = placementPosition;
        // 탑뷰 스프라이트가 바닥을 향하도록 생성 시 X축을 90도로 고정한다.
        turretObject.transform.rotation = SkillSpawnRotation;

        AutoTurretSkill turret = turretObject.GetComponent<AutoTurretSkill>();
        if (turret == null)
        {
            turret = turretObject.AddComponent<AutoTurretSkill>();
        }

        turret.Initialize(player, skillConfig);
        return true;
    }

    private void Initialize(PlayerController player, PlayerSkillConfig skillConfig)
    {
        owner = player;
        config = skillConfig;
        expireTime = Time.time + config.TurretDuration;
        nextAttackTime = Time.time;
        firePoint = FindChildByName(transform, config.TurretFirePointName);
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Update()
    {
        CombatPlane.ClampTransform(transform);
        if (owner == null
            || owner.Health == null
            || owner.Health.IsDead
            || config == null
            || Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        if (!HasValidTarget())
        {
            currentTarget = null;
            if (Time.time >= nextTargetSearchTime)
            {
                currentTarget = PlayerSkillCombat.FindClosestEnemy(
                    transform.position,
                    config.TurretAttackRange);
                nextTargetSearchTime = currentTarget == null ? Time.time + 0.2f : 0f;
            }
        }

        if (currentTarget == null)
        {
            return;
        }

        // 회전하는 FirePoint가 아니라 터렛 중심을 기준으로 고정된 조준 방향을 계산한다.
        Vector3 direction = CombatPlane.Direction(
            transform.position,
            currentTarget.transform.position);
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        // 플레이어 터렛과 동일하게 로컬 X/Y는 유지하고 Z축만 목표 방향으로 회전한다.
        CombatPlane.RotateZOnlyToward(
            transform,
            direction,
            config.TurretRotationSpeed * Time.deltaTime);

        if (Time.time < nextAttackTime)
        {
            return;
        }

        // 정렬 중에도 발사하되 투사체는 항상 현재 터렛 정면으로 나간다.
        Fire(currentTarget);
        nextAttackTime = Time.time + config.TurretAttackInterval;
    }

    private bool HasValidTarget()
    {
        return currentTarget != null
            && !currentTarget.IsDead
            && CombatPlane.DistanceSqr(transform.position, currentTarget.transform.position)
                <= config.TurretAttackRange * config.TurretAttackRange;
    }

    private void Fire(CombatHealth target)
    {
        float damage = PlayerSkillCombat.CalculateDamage(owner, config);
        ProjectileConfig projectileConfig = config.TurretProjectileConfig;
        if (projectileConfig == null)
        {
            target.TakeDamage(damage);
            CombatRewardService.GrantIfKilled(owner, target);
            return;
        }

        Vector3 fireDirection = CombatPlane.DirectionFromZRotation(transform);
        if (fireDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        PlayerProjectile projectile = CombatObjectPool.GetProjectile();
        // X축 90도 프리팹의 FirePoint 로컬 좌표를 실제 X/Z 발사 위치로 변환한다.
        projectile.transform.position = firePoint == transform
            ? CombatPlane.WithFixedY(transform.position)
            : CombatPlane.PositionFromZPlaneChild(transform, firePoint, fireDirection);
        projectile.Configure(projectileConfig);
        projectile.Launch(
            fireDirection,
            damage,
            projectileConfig.Speed,
            projectileConfig.Lifetime,
            owner.Health);
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

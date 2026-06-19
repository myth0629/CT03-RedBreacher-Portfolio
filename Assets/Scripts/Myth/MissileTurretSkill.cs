using System.Collections;
using UnityEngine;

public class MissileTurretSkill : MonoBehaviour
{
    private static readonly Quaternion SkillSpawnRotation = Quaternion.Euler(90f, 0f, 0f);

    private PlayerController owner;
    private PlayerSkillConfig config;
    private Transform firePoint;
    private CombatHealth currentTarget;
    private float expireTime;
    private float nextAttackTime;
    private float nextTargetSearchTime;
    private float aimAngleDeg;

    public static bool Spawn(PlayerController player, PlayerSkillConfig skillConfig, Vector3 targetPosition)
    {
        if (player == null
            || skillConfig == null
            || skillConfig.MissileTurretPrefab == null
            || skillConfig.MissileProjectilePrefab == null)
        {
            return false;
        }

        Vector3 placementDirection = CombatPlane.Direction(player.transform.position, targetPosition);
        if (placementDirection.sqrMagnitude <= 0f)
        {
            placementDirection = CombatPlane.DirectionFromYRotation(player.transform);
        }

        Vector3 placementPosition = CombatPlane.WithFixedY(
            player.transform.position + placementDirection * skillConfig.MissileTurretPlacementDistance);
        GameObject turretObject = Instantiate(skillConfig.MissileTurretPrefab, placementPosition, SkillSpawnRotation);
        turretObject.transform.position = placementPosition;
        turretObject.transform.rotation = SkillSpawnRotation;

        MissileTurretSkill turret = turretObject.GetComponent<MissileTurretSkill>();
        if (turret == null)
        {
            turret = turretObject.AddComponent<MissileTurretSkill>();
        }

        turret.Initialize(player, skillConfig);
        return true;
    }

    private void Initialize(PlayerController player, PlayerSkillConfig skillConfig)
    {
        owner = player;
        config = skillConfig;
        expireTime = Time.time + config.MissileTurretDuration;
        nextAttackTime = Time.time;
        firePoint = FindChildByName(transform, config.MissileTurretFirePointName);
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
                currentTarget = PlayerSkillCombat.FindClosestEnemy(transform.position, config.MissileTurretAttackRange);
                nextTargetSearchTime = currentTarget == null ? Time.time + 0.2f : 0f;
            }
        }

        if (currentTarget == null)
        {
            return;
        }

        RotateToTarget(currentTarget.transform.position);
        if (Time.time < nextAttackTime)
        {
            return;
        }

        Fire(currentTarget);
        nextAttackTime = Time.time + config.MissileTurretAttackInterval;
    }

    private bool HasValidTarget()
    {
        return currentTarget != null
            && !currentTarget.IsDead
            && CombatPlane.DistanceSqr(transform.position, currentTarget.transform.position)
                <= config.MissileTurretAttackRange * config.MissileTurretAttackRange;
    }

    private void RotateToTarget(Vector3 targetPosition)
    {
        Vector3 direction = CombatPlane.Direction(transform.position, targetPosition);
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        float targetAngle = CombatPlane.DirectionToZAngle(direction);
        aimAngleDeg = Mathf.MoveTowardsAngle(
            aimAngleDeg,
            targetAngle,
            config.MissileTurretRotationSpeed * Time.deltaTime);
        transform.rotation = SkillSpawnRotation * Quaternion.Euler(0f, 0f, aimAngleDeg);
    }

    private void Fire(CombatHealth target)
    {
        if (target == null)
        {
            return;
        }

        // 미사일은 발사 시점의 적 위치로 느리게 날아가 착탄 지점에 범위 피해를 준다.
        Vector3 startPosition = firePoint == transform
            ? CombatPlane.WithFixedY(transform.position)
            : CombatPlane.PositionFromZPlaneChild(transform, firePoint, CombatPlane.Direction(transform.position, target.transform.position));
        Vector3 impactPosition = CombatPlane.WithFixedY(target.transform.position);
        StartCoroutine(LaunchMissile(startPosition, impactPosition));
    }

    private IEnumerator LaunchMissile(Vector3 startPosition, Vector3 impactPosition)
    {
        GameObject missile = Instantiate(config.MissileProjectilePrefab, startPosition, Quaternion.identity);
        missile.transform.position = startPosition;

        Vector3 direction = CombatPlane.Direction(startPosition, impactPosition);
        if (direction.sqrMagnitude > 0f)
        {
            missile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        float distance = Mathf.Sqrt(CombatPlane.DistanceSqr(startPosition, impactPosition));
        float duration = distance / Mathf.Max(0.1f, config.MissileProjectileSpeed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (missile == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            missile.transform.position = CombatPlane.WithFixedY(Vector3.Lerp(startPosition, impactPosition, t));
            yield return null;
        }

        if (missile != null)
        {
            Destroy(missile);
        }

        if (owner != null && owner.Health != null && !owner.Health.IsDead)
        {
            float damage = PlayerSkillCombat.CalculateDamage(owner, config, out bool isCritical);
            PlayerSkillCombat.ApplyAreaDamage(
                owner,
                impactPosition,
                config.MissileExplosionRadius,
                damage,
                config.MaxTargets,
                config.KnockbackForce,
                isCritical);
        }
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

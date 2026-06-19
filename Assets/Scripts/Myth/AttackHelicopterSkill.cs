using System.Collections;
using UnityEngine;

public class AttackHelicopterSkill : MonoBehaviour
{
    private PlayerController owner;
    private PlayerSkillConfig config;
    private GameObject helicopterVisual;
    private CombatHealth currentTarget;
    private float expireTime;
    private float nextAttackTime;
    private float nextTargetSearchTime;
    private bool leaving;

    public static bool Spawn(PlayerController player, PlayerSkillConfig skillConfig, Vector3 targetPosition)
    {
        if (player == null
            || skillConfig == null
            || skillConfig.AttackHelicopterPrefab == null
            || skillConfig.HelicopterRocketPrefab == null)
        {
            return false;
        }

        GameObject skillObject = new GameObject($"AttackHelicopter_{skillConfig.Id}");
        AttackHelicopterSkill helicopter = skillObject.AddComponent<AttackHelicopterSkill>();

        helicopter.Initialize(player, skillConfig, targetPosition);
        return true;
    }

    private void Initialize(PlayerController player, PlayerSkillConfig skillConfig, Vector3 targetPosition)
    {
        owner = player;
        config = skillConfig;
        expireTime = float.PositiveInfinity;
        nextAttackTime = Time.time + 0.7f;
        leaving = false;
        transform.position = GetSpawnPosition(owner, config);

        // 헬기가 보이기 전에 목표 주변에 로켓을 먼저 떨어뜨려 지원 등장을 예고한다.
        for (int i = 0; i < config.HelicopterOpeningRocketCount; i++)
        {
            Vector3 offset = GetOpeningRocketOffset(i, config.HelicopterOpeningRocketCount);
            StartCoroutine(LaunchRocket(transform.position, CombatPlane.WithFixedY(targetPosition + offset), i * 0.15f));
        }

        StartCoroutine(SpawnHelicopterAfterOpening());
    }

    private void Update()
    {
        if (owner == null || owner.Health == null || owner.Health.IsDead || config == null)
        {
            if (helicopterVisual != null)
            {
                Destroy(helicopterVisual);
            }

            Destroy(gameObject);
            return;
        }

        if (helicopterVisual == null)
        {
            return;
        }

        if (Time.time >= expireTime)
        {
            LeaveAndDestroy();
            return;
        }

        FollowOwner();
        TryAttack();
    }

    private void FollowOwner()
    {
        Vector3 desiredPosition = GetSpawnPosition(owner, config);
        helicopterVisual.transform.position = Vector3.MoveTowards(
            helicopterVisual.transform.position,
            desiredPosition,
            config.AttackHelicopterMoveSpeed * Time.deltaTime);
        helicopterVisual.transform.rotation = GetHelicopterRotation(owner);
        transform.position = helicopterVisual.transform.position;
    }

    private void TryAttack()
    {
        if (!HasValidTarget())
        {
            currentTarget = null;
            if (Time.time >= nextTargetSearchTime)
            {
                currentTarget = PlayerSkillCombat.FindClosestEnemy(helicopterVisual.transform.position, config.AttackHelicopterAttackRange);
                nextTargetSearchTime = currentTarget == null ? Time.time + 0.2f : 0f;
            }
        }

        if (currentTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        StartCoroutine(LaunchRocket(helicopterVisual.transform.position, CombatPlane.WithFixedY(currentTarget.transform.position), 0f));
        nextAttackTime = Time.time + config.AttackHelicopterAttackInterval;
    }

    private bool HasValidTarget()
    {
        return currentTarget != null
            && !currentTarget.IsDead
            && helicopterVisual != null
            && CombatPlane.DistanceSqr(helicopterVisual.transform.position, currentTarget.transform.position)
                <= config.AttackHelicopterAttackRange * config.AttackHelicopterAttackRange;
    }

    private IEnumerator SpawnHelicopterAfterOpening()
    {
        yield return new WaitForSeconds(0.35f);

        if (owner == null || owner.Health == null || owner.Health.IsDead || config == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector3 spawnPosition = GetSpawnPosition(owner, config);
        helicopterVisual = Instantiate(config.AttackHelicopterPrefab, spawnPosition, GetHelicopterRotation(owner));
        transform.position = helicopterVisual.transform.position;
        expireTime = Time.time + config.AttackHelicopterDuration;
    }

    private IEnumerator LaunchRocket(Vector3 startPosition, Vector3 impactPosition, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (config == null)
        {
            yield break;
        }

        GameObject rocket = Instantiate(config.HelicopterRocketPrefab, startPosition, Quaternion.identity);
        rocket.transform.position = startPosition;

        Vector3 direction = CombatPlane.Direction(startPosition, impactPosition);
        if (direction.sqrMagnitude > 0f)
        {
            rocket.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        float distance = Mathf.Sqrt(CombatPlane.DistanceSqr(startPosition, impactPosition));
        float duration = distance / config.HelicopterRocketSpeed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rocket == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            rocket.transform.position = CombatPlane.WithFixedY(Vector3.Lerp(startPosition, impactPosition, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        if (rocket != null)
        {
            Destroy(rocket);
        }

        if (owner != null && owner.Health != null && !owner.Health.IsDead)
        {
            float damage = PlayerSkillCombat.CalculateDamage(owner, config, out bool isCritical);
            PlayerSkillCombat.ApplyAreaDamage(
                owner,
                impactPosition,
                config.HelicopterRocketRadius,
                damage,
                config.MaxTargets,
                config.KnockbackForce,
                isCritical);
        }
    }

    private void LeaveAndDestroy()
    {
        if (leaving)
        {
            return;
        }

        leaving = true;
        StartCoroutine(LeaveRoutine());
    }

    private IEnumerator LeaveRoutine()
    {
        Vector3 forward = owner != null
            ? CombatPlane.DirectionFromYRotation(owner.transform)
            : Vector3.forward;
        Transform visualTransform = helicopterVisual != null ? helicopterVisual.transform : transform;
        Vector3 endPosition = CombatPlane.WithFixedY(visualTransform.position - forward * 5f);
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startPosition = visualTransform.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            visualTransform.position = CombatPlane.WithFixedY(Vector3.Lerp(startPosition, endPosition, Mathf.Clamp01(elapsed / duration)));
            transform.position = visualTransform.position;
            yield return null;
        }

        if (helicopterVisual != null)
        {
            Destroy(helicopterVisual);
        }

        Destroy(gameObject);
    }

    private static Vector3 GetSpawnPosition(PlayerController player, PlayerSkillConfig skillConfig)
    {
        Vector3 forward = CombatPlane.DirectionFromYRotation(player.transform);
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        if (side.sqrMagnitude <= 0f)
        {
            side = Vector3.right;
        }

        return CombatPlane.WithFixedY(player.transform.position + side * skillConfig.AttackHelicopterFollowOffset);
    }

    private static Quaternion GetHelicopterRotation(PlayerController player)
    {
        Vector3 forward = CombatPlane.DirectionFromYRotation(player.transform);
        if (forward.sqrMagnitude <= 0f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
    }

    private static Vector3 GetOpeningRocketOffset(int index, int count)
    {
        if (count <= 1)
        {
            return Vector3.zero;
        }

        float side = index - (count - 1) * 0.5f;
        return new Vector3(side * 1.5f, 0f, 0f);
    }
}

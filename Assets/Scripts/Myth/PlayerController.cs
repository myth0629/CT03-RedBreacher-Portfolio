using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TopDownAssets.Common.Scripts;

[RequireComponent(typeof(CombatHealth))]
[RequireComponent(typeof(PlayerProgression))]
public class PlayerController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerUnitConfig unitConfig;
    [SerializeField, FormerlySerializedAs("projectileConfig")] private ProjectileConfig weaponConfig;
    [SerializeField] private Transform unitRoot;
    [SerializeField] private string displayName = "탱크이름";

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Range(0f, 1f)] private float critChance = 0.1f;
    [SerializeField] private float critMultiplier = 1.5f;

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

    [Header("Fallback Effects")]
    [SerializeField] private GameObject fireFlashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;

    private readonly List<Transform> fireMuzzles = new List<Transform>();
    private CombatHealth health;
    private PlayerProgression progression;
    private Vehicle vehicle;
    private Turret turret;
    private CombatHealth currentTarget;
    private PlayerUnitConfig appliedUnitConfig;
    private GameObject spawnedUnitObject;
    private Vector3 weaponAimDirection;
    private float appliedMaxHealth;
    private float nextAttackTime;

    public CombatHealth Health => health;
    public PlayerProgression Progression => progression;
    public string DisplayName => unitConfig != null ? unitConfig.DisplayName : displayName;
    public float AttackRange => AttackRangeValue;

    private float MaxHealthValue => unitConfig != null ? unitConfig.MaxHealth : maxHealth;
    private float CritChanceValue => unitConfig != null ? unitConfig.CritChance : critChance;
    private float CritMultiplierValue => unitConfig != null ? unitConfig.CritMultiplier : critMultiplier;
    private float AttackRangeValue => unitConfig != null ? unitConfig.AttackRange : attackRange;
    private float AttackDamageValue => unitConfig != null ? unitConfig.AttackDamage : attackDamage;
    private float AttackIntervalValue => unitConfig != null ? unitConfig.AttackInterval : attackInterval;
    private float MoveSpeedValue => unitConfig != null ? unitConfig.MoveSpeed : moveSpeed;
    private float RotationSpeedValue => unitConfig != null ? unitConfig.RotationSpeed : rotationSpeed;
    private float FireAngleToleranceValue => unitConfig != null ? unitConfig.FireAngleTolerance : fireAngleTolerance;
    private ProjectileConfig ProjectileConfigValue => weaponConfig;

    private void Awake()
    {
        health = GetComponent<CombatHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<CombatHealth>();
        }

        progression = GetComponent<PlayerProgression>();
        if (progression == null)
        {
            progression = gameObject.AddComponent<PlayerProgression>();
        }

        ApplyUnitConfig();
        ApplyHealthStats();
        EnsureCombatComponents();
        RefreshUnitReferences();
    }

    private void Start()
    {
        ApplyHealthStats();
    }

    private void Update()
    {
        CombatPlane.ClampTransform(transform);
        ApplyUnitConfig();

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

        if (!IsTargetInRange(currentTarget))
        {
            RotateToward(targetDirection);
            AimWeaponForward();
            MoveUntilInRange(currentTarget, targetDirection);
            return;
        }

        SetVehicleMoveInput(0f, 0f);
        AimWeaponToward(targetDirection);

        if (!IsWeaponFacing(targetDirection) || Time.time < nextAttackTime)
        {
            return;
        }

        float attackIntervalMultiplier = FireForward();
        nextAttackTime = Time.time + AttackIntervalValue * attackIntervalMultiplier;
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

        EnsureFirePoint();
    }

    private void EnsureFirePoint()
    {
        if (firePoint == null)
        {
            GameObject point = new GameObject("FirePoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = Vector3.forward * 0.45f;
            firePoint = point.transform;
        }
    }

    private void ApplyUnitConfig()
    {
        if (appliedUnitConfig == unitConfig)
        {
            ApplyHealthStats();
            return;
        }

        appliedUnitConfig = unitConfig;
        ReplaceUnitPrefab(unitConfig != null ? unitConfig.UnitPrefab : null);
        RefreshUnitReferences();
        EnsureFirePoint();
        ApplyHealthStats();
    }

    private void ApplyHealthStats()
    {
        if (health == null)
        {
            return;
        }

        float configuredMaxHealth = Mathf.Max(1f, MaxHealthValue);
        if (Mathf.Approximately(appliedMaxHealth, configuredMaxHealth))
        {
            return;
        }

        // 플레이어 SO의 체력 값이 바뀔 때만 CombatHealth를 다시 초기화한다.
        appliedMaxHealth = configuredMaxHealth;
        health.Initialize(configuredMaxHealth);
    }

    private void ReplaceUnitPrefab(GameObject unitPrefab)
    {
        if (spawnedUnitObject != null)
        {
            Destroy(spawnedUnitObject);
            spawnedUnitObject = null;
        }

        if (unitPrefab == null)
        {
            return;
        }

        Transform root = GetOrCreateUnitRoot();
        spawnedUnitObject = Instantiate(unitPrefab, root);
        spawnedUnitObject.transform.localPosition = Vector3.zero;
        spawnedUnitObject.transform.localRotation = Quaternion.identity;
        spawnedUnitObject.transform.localScale = Vector3.one;

        // 기체 프리팹을 교체하면 이전 타겟/총구 참조가 어긋날 수 있어 다시 잡는다.
        currentTarget = null;
        firePoint = FindChildByName(spawnedUnitObject.transform, "FirePoint");
    }

    private Transform GetOrCreateUnitRoot()
    {
        if (unitRoot != null)
        {
            return unitRoot;
        }

        Transform existingRoot = transform.Find("UnitRoot");
        if (existingRoot != null)
        {
            unitRoot = existingRoot;
            return unitRoot;
        }

        GameObject rootObject = new GameObject("UnitRoot");
        rootObject.transform.SetParent(transform);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        unitRoot = rootObject.transform;
        return unitRoot;
    }

    private void RefreshUnitReferences()
    {
        if (firePoint == null && spawnedUnitObject != null)
        {
            firePoint = FindChildByName(spawnedUnitObject.transform, "FirePoint");
        }

        vehicle = GetComponentInChildren<Vehicle>();
        turret = GetComponentInChildren<Turret>();
        if (firePoint == null && turret != null && turret.FireTransform != null)
        {
            firePoint = turret.FireTransform;
        }

        RefreshFireMuzzles();
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
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
        // 플레이어 본체는 눕힌 스프라이트 기준이라 X/Z를 유지하고 Y축만 이동 방향으로 돌린다.
        CombatPlane.RotateYOnlyToward(transform, direction, RotationSpeedValue * Time.deltaTime);
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

    private void AimWeaponToward(Vector3 direction)
    {
        weaponAimDirection = CombatPlane.ProjectDirection(direction);

        if (turret != null)
        {
            turret.AimWorld(direction);
            return;
        }

        if (firePoint != null)
        {
            // 터렛 컴포넌트가 없으면 본체 대신 총구 Transform의 Z축만 돌린다.
            CombatPlane.RotateZOnlyToward(firePoint, direction, RotationSpeedValue * Time.deltaTime);
        }
    }

    private void AimWeaponForward()
    {
        Vector3 forwardDirection = CombatPlane.DirectionFromYRotation(transform);
        if (forwardDirection.sqrMagnitude <= 0f)
        {
            forwardDirection = CombatPlane.ProjectDirection(transform.forward);
        }

        // 이동 중에는 타겟 조준보다 본체 정면 정렬을 우선한다.
        AimWeaponToward(forwardDirection);
    }

    private bool IsWeaponFacing(Vector3 direction)
    {
        Vector3 forward = GetWeaponForwardDirection();
        if (forward.sqrMagnitude <= 0f)
        {
            return false;
        }

        return Vector3.Angle(forward, direction) <= FireAngleToleranceValue;
    }

    private float FireForward()
    {
        Vector3 direction = GetWeaponForwardDirection();
        if (direction.sqrMagnitude <= 0f)
        {
            direction = weaponAimDirection.sqrMagnitude > 0f ? weaponAimDirection : CombatPlane.ProjectDirection(transform.forward);
        }

        if (direction.sqrMagnitude <= 0f)
        {
            direction = Vector3.forward;
        }

        // 투사체는 타겟 위치가 아니라 플레이어 정면 방향으로만 발사한다.
        ProjectileConfig activeProjectileConfig = ProjectileConfigValue;
        RefreshFireMuzzles();

        int muzzleCount = GetFireMuzzleCount(activeProjectileConfig);
        float damage = CalculateAttackDamage(activeProjectileConfig);
        if (GetMultiMuzzleFireMode(activeProjectileConfig) == MultiMuzzleFireMode.SplitDamage)
        {
            damage /= Mathf.Max(1, muzzleCount);
        }

        for (int i = 0; i < muzzleCount; i++)
        {
            FireProjectileFrom(GetFireMuzzle(i), direction, damage, activeProjectileConfig);
        }

        return GetAttackIntervalMultiplier(activeProjectileConfig);
    }

    private void FireProjectileFrom(Transform muzzle, Vector3 direction, float damage, ProjectileConfig activeProjectileConfig)
    {
        PlayerProjectile projectile = CreateProjectile();
        projectile.transform.position = GetFirePosition(muzzle, direction);
        projectile.Configure(activeProjectileConfig);
        projectile.ConfigureEffects(fireFlashEffectPrefab, projectileEffectPrefab, hitEffectPrefab);
        projectile.Launch(direction, damage, GetProjectileSpeed(activeProjectileConfig), GetProjectileLifetime(activeProjectileConfig), health);
    }

    private float CalculateAttackDamage(ProjectileConfig activeProjectileConfig)
    {
        // 최종 기본 피해는 기체 공격력과 장착 무기 공격력을 합산한다.
        float damage = AttackDamageValue + GetWeaponAttackDamage(activeProjectileConfig);
        float chance = Mathf.Clamp01(CritChanceValue);
        float multiplier = Mathf.Max(1f, CritMultiplierValue);

        // 치명타는 방어력 없이 최종 발사 데미지에만 단순 배율로 적용한다.
        if (Random.value < chance)
        {
            damage *= multiplier;
        }

        return damage;
    }

    private float GetWeaponAttackDamage(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null ? activeProjectileConfig.AttackDamage : 0f;
    }

    private Vector3 GetFirePosition(Transform muzzle, Vector3 direction)
    {
        if (turret != null && muzzle != null)
        {
            return CombatPlane.PositionFromZPlaneChild(turret.transform, muzzle, direction);
        }

        if (muzzle != null)
        {
            return CombatPlane.WithFixedY(muzzle.position);
        }

        return CombatPlane.WithFixedY(transform.position);
    }

    private Vector3 GetWeaponForwardDirection()
    {
        if (turret != null)
        {
            return CombatPlane.DirectionFromZRotation(turret.transform);
        }

        if (firePoint != null)
        {
            return CombatPlane.DirectionFromZRotation(firePoint);
        }

        return CombatPlane.DirectionFromYRotation(transform);
    }

    private PlayerProjectile CreateProjectile()
    {
        // 발사체 본체는 공통 풀에서 꺼내고, 외형/스탯은 무기 SO에서 적용한다.
        return CombatObjectPool.GetProjectile();
    }

    private void RefreshFireMuzzles()
    {
        fireMuzzles.Clear();

        Transform muzzleRoot = turret != null ? turret.transform : spawnedUnitObject != null ? spawnedUnitObject.transform : transform;
        CollectFireMuzzles(muzzleRoot, GetMuzzleNamePrefix(ProjectileConfigValue), fireMuzzles);

        if (fireMuzzles.Count == 0 && turret != null && turret.FireTransform != null)
        {
            fireMuzzles.Add(turret.FireTransform);
        }

        if (fireMuzzles.Count == 0 && firePoint != null)
        {
            fireMuzzles.Add(firePoint);
        }
    }

    private void CollectFireMuzzles(Transform root, string muzzleNamePrefix, List<Transform> results)
    {
        if (root == null)
        {
            return;
        }

        if (root.name.StartsWith(muzzleNamePrefix))
        {
            results.Add(root);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectFireMuzzles(root.GetChild(i), muzzleNamePrefix, results);
        }
    }

    private int GetFireMuzzleCount(ProjectileConfig activeProjectileConfig)
    {
        if (GetMultiMuzzleFireMode(activeProjectileConfig) == MultiMuzzleFireMode.Single)
        {
            return 1;
        }

        int maxBurstMuzzleCount = activeProjectileConfig != null ? activeProjectileConfig.MaxBurstMuzzleCount : 1;
        return Mathf.Clamp(fireMuzzles.Count, 1, Mathf.Max(1, maxBurstMuzzleCount));
    }

    private Transform GetFireMuzzle(int index)
    {
        if (fireMuzzles.Count == 0)
        {
            return firePoint;
        }

        return fireMuzzles[Mathf.Clamp(index, 0, fireMuzzles.Count - 1)];
    }

    private MultiMuzzleFireMode GetMultiMuzzleFireMode(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null ? activeProjectileConfig.MultiMuzzleFireMode : MultiMuzzleFireMode.Single;
    }

    private string GetMuzzleNamePrefix(ProjectileConfig activeProjectileConfig)
    {
        if (activeProjectileConfig != null && !string.IsNullOrWhiteSpace(activeProjectileConfig.MuzzleNamePrefix))
        {
            return activeProjectileConfig.MuzzleNamePrefix;
        }

        return "FireMuzzle";
    }

    private float GetAttackIntervalMultiplier(ProjectileConfig activeProjectileConfig)
    {
        MultiMuzzleFireMode mode = GetMultiMuzzleFireMode(activeProjectileConfig);
        if (mode != MultiMuzzleFireMode.BurstKeepsDps)
        {
            return 1f;
        }

        return Mathf.Max(1, GetFireMuzzleCount(activeProjectileConfig));
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

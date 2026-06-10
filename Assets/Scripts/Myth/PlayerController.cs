using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TopDownAssets.Common.Scripts;

[RequireComponent(typeof(CombatHealth))]
[RequireComponent(typeof(PlayerProgression))]
[RequireComponent(typeof(PlayerStatAllocator))]
[RequireComponent(typeof(PlayerCurrencyWallet))]
[RequireComponent(typeof(PlayerEquipmentPartLoadout))]
public class PlayerController : MonoBehaviour
{
    private const string EquippedWeaponIdKey = "PlayerController.EquippedWeaponId";
    private const string EquippedSkillIdsKey = "PlayerController.EquippedSkillIds";
    private const int SkillSlotCount = 3;

    [System.Serializable]
    private class SkillLoadoutSaveData
    {
        public List<string> skillIds = new List<string>();
    }

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

    [Header("Auto Reposition")]
    [SerializeField] private bool enableAutoReposition = true;
    [SerializeField] private float repositionDetectionRadius = 2.5f;
    [SerializeField] private int repositionEnemyThreshold = 3;
    [SerializeField] private float repositionDistance = 1.8f;
    [SerializeField] private float repositionCooldown = 5f;
    [SerializeField] private float repositionCheckInterval = 0.5f;
    [SerializeField] private float repositionSpeedMultiplier = 1.35f;
    [SerializeField] private float repositionStopDistance = 0.05f;
    [SerializeField] private LayerMask repositionObstacleMask;
    [SerializeField] private float repositionCollisionRadius = 0.45f;
    [SerializeField] private bool clampRepositionToBounds = true;
    [SerializeField] private Transform repositionBoundsCenter;
    [SerializeField] private Vector2 repositionBoundsSize = new Vector2(24f, 24f);
    [SerializeField] private float repositionBoundsPadding = 1f;

    [Header("Fallback Effects")]
    [SerializeField] private GameObject fireFlashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;

    [Header("Auto Skills")]
    [SerializeField] private List<PlayerSkillConfig> autoSkills = new List<PlayerSkillConfig>();

    private readonly List<Transform> fireMuzzles = new List<Transform>();
    private CombatHealth health;
    private PlayerProgression progression;
    private Rigidbody _rigidbody;
    private PlayerStatAllocator statAllocator;
    private PlayerEquipmentPartLoadout equipmentPartLoadout;
    private PlayerAutoSkillController autoSkillController;
    private PlayerBossDodgeController bossDodgeController;
    private PlayerDebugModeController debugModeController;
    private InventoryFacility inventory;
    private AssemblyFactory assemblyFactory;
    private Vehicle vehicle;
    private Turret turret;
    private CombatHealth currentTarget;
    private PlayerUnitConfig appliedUnitConfig;
    private GameObject spawnedUnitObject;
    private Vector3 weaponAimDirection;
    private float appliedMaxHealth;
    private float nextAttackTime;
    private float nextRepositionTime;
    private float nextRepositionCheckTime;
    private Vector3 repositionDestination;
    private Vector3 fallbackRepositionBoundsCenter;
    private bool hasAppliedHealthStats;
    private bool isRepositioning;

    public CombatHealth Health => health;
    public PlayerProgression Progression => progression;
    public PlayerStatAllocator StatAllocator => statAllocator;
    public PlayerEquipmentPartLoadout EquipmentPartLoadout => equipmentPartLoadout;
    public PlayerAutoSkillController AutoSkillController => autoSkillController;
    public InventoryFacility Inventory => inventory;
    public string DisplayName => unitConfig != null ? unitConfig.DisplayName : displayName;
    public PlayerUnitConfig UnitConfig => unitConfig;
    public ProjectileConfig WeaponConfig => ProjectileConfigValue;
    public float AttackRange => AttackRangeValue;
    public float AttackDamage => AttackDamageValue;
    public float WeaponAttackDamage => GetWeaponAttackDamage(ProjectileConfigValue);
    public float TotalAttackDamage => GetTotalAttackDamage(ProjectileConfigValue);
    public float AttackInterval => AttackIntervalValue;
    public float MoveSpeed => MoveSpeedValue;
    public float CritChance => CritChanceValue;
    public float CritMultiplier => CritMultiplierValue;
    public float ProjectileSpeed => GetProjectileSpeed(ProjectileConfigValue);
    public float ProjectileLifetime => GetProjectileLifetime(ProjectileConfigValue);
    public int WeaponLevel => inventory != null ? inventory.GetWeaponLevel(ProjectileConfigValue) : ProjectileConfigValue != null ? 1 : 0;
    public float KnockbackForce => GetProjectileKnockback(ProjectileConfigValue);
    public float EstimatedDamagePerSecond
    {
        get
        {
            float expectedCritMultiplier = 1f + Mathf.Clamp01(CritChanceValue) * (Mathf.Max(1f, CritMultiplierValue) - 1f);
            return AttackIntervalValue > 0f ? TotalAttackDamage * expectedCritMultiplier / AttackIntervalValue : 0f;
        }
    }

    private float MaxHealthValue => (unitConfig != null ? unitConfig.MaxHealth : maxHealth)
        * (statAllocator != null ? statAllocator.HealthMultiplier : 1f)
        * (1f + (equipmentPartLoadout != null ? equipmentPartLoadout.HealthPercent : 0f));
    private float CritChanceValue
    {
        get
        {
            float baseValue = statAllocator != null
                ? statAllocator.ApplyCritChance(unitConfig != null ? unitConfig.CritChance : critChance)
                : unitConfig != null ? unitConfig.CritChance : critChance;
            float maxValue = statAllocator != null ? statAllocator.MaxCritChance : 1f;
            return Mathf.Min(maxValue, baseValue + (equipmentPartLoadout != null ? equipmentPartLoadout.CritChanceBonus : 0f));
        }
    }
    private float CritMultiplierValue
    {
        get
        {
            float baseValue = statAllocator != null
                ? statAllocator.ApplyCritMultiplier(unitConfig != null ? unitConfig.CritMultiplier : critMultiplier)
                : unitConfig != null ? unitConfig.CritMultiplier : critMultiplier;
            float maxValue = statAllocator != null ? statAllocator.MaxCritMultiplier : float.MaxValue;
            return Mathf.Min(maxValue, baseValue + (equipmentPartLoadout != null ? equipmentPartLoadout.CritDamageBonus : 0f));
        }
    }
    private float AttackRangeValue => unitConfig != null ? unitConfig.AttackRange : attackRange;
    private float AttackDamageValue => unitConfig != null ? unitConfig.AttackDamage : attackDamage;
    private float AttackIntervalValue
    {
        get
        {
            float baseInterval = unitConfig != null ? unitConfig.AttackInterval : attackInterval;
            float attackSpeedPercent = equipmentPartLoadout != null ? equipmentPartLoadout.AttackSpeedPercent : 0f;
            return Mathf.Max(0.1f, baseInterval / (1f + attackSpeedPercent));
        }
    }
    private float MoveSpeedValue => unitConfig != null ? unitConfig.MoveSpeed : moveSpeed;
    private float RotationSpeedValue => unitConfig != null ? unitConfig.RotationSpeed : rotationSpeed;
    private float FireAngleToleranceValue => unitConfig != null ? unitConfig.FireAngleTolerance : fireAngleTolerance;
    private float RepositionDistanceValue => unitConfig != null
        ? unitConfig.RepositionDistance
        : repositionDistance;
    private float RepositionCooldownValue => unitConfig != null
        ? unitConfig.RepositionCooldown
        : repositionCooldown;
    private ProjectileConfig ProjectileConfigValue => weaponConfig;

    private void Awake()
    {
        fallbackRepositionBoundsCenter = CombatPlane.WithFixedY(transform.position);
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

        _rigidbody = GetComponent<Rigidbody>();

        statAllocator = GetComponent<PlayerStatAllocator>();
        if (statAllocator == null)
        {
            statAllocator = gameObject.AddComponent<PlayerStatAllocator>();
        }

        equipmentPartLoadout = GetComponent<PlayerEquipmentPartLoadout>();
        if (equipmentPartLoadout == null)
        {
            equipmentPartLoadout = gameObject.AddComponent<PlayerEquipmentPartLoadout>();
        }

        equipmentPartLoadout.OnLoadoutChanged.AddListener(HandleEquipmentPartLoadoutChanged);
        ResolveCollectionSystems();
        InitializeCollectionLoadout();
        EnsureAutoSkillController();
        ApplyUnitConfig();
        ApplyHealthStats();
        EnsureCombatComponents();
        RefreshUnitReferences();
        bossDodgeController = PlayerBossDodgeController.Ensure(this);
        debugModeController = PlayerDebugModeController.Ensure(this);

        // 장착된 기체의 전체 스프라이트에 바닥 그림자를 적용한다.
        SpriteShapeShadow.Ensure(gameObject);
    }

    private void Start()
    {
        ApplyHealthStats();
    }

    private void OnDestroy()
    {
        equipmentPartLoadout?.OnLoadoutChanged.RemoveListener(HandleEquipmentPartLoadoutChanged);
    }

    public void SetWeaponConfig(ProjectileConfig config)
    {
        if (config != null && inventory != null && !inventory.ContainsWeapon(config))
        {
            return;
        }

        // 로드아웃 UI에서 선택한 무기를 런타임 전투 설정에 즉시 반영한다.
        weaponConfig = config;
        PlayerPrefs.SetString(EquippedWeaponIdKey, config != null ? config.Id : string.Empty);
        PlayerPrefs.Save();
        nextAttackTime = Time.time;
        RefreshUnitReferences();
    }

    public bool EquipSkill(int slotIndex, PlayerSkillConfig config)
    {
        if (slotIndex < 0 || slotIndex >= SkillSlotCount
            || config == null
            || inventory == null
            || !inventory.ContainsSkill(config))
        {
            return false;
        }

        EnsureSkillSlotCount();
        for (int i = 0; i < autoSkills.Count; i++)
        {
            if (i != slotIndex && autoSkills[i] == config)
            {
                autoSkills[i] = null;
            }
        }

        autoSkills[slotIndex] = config;
        SaveSkillLoadout();
        autoSkillController?.Initialize(this, autoSkills);
        return true;
    }

    public bool UnequipSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SkillSlotCount)
        {
            return false;
        }

        EnsureSkillSlotCount();
        autoSkills[slotIndex] = null;
        SaveSkillLoadout();
        autoSkillController?.Initialize(this, autoSkills);
        return true;
    }

    public PlayerSkillConfig GetEquippedSkill(int slotIndex)
    {
        EnsureSkillSlotCount();
        return slotIndex >= 0 && slotIndex < autoSkills.Count ? autoSkills[slotIndex] : null;
    }

    public int GetSkillLevel(PlayerSkillConfig config)
    {
        return inventory != null ? inventory.GetSkillLevel(config) : config != null ? 1 : 0;
    }

    private void EnsureAutoSkillController()
    {
        autoSkillController = GetComponent<PlayerAutoSkillController>();
        if (autoSkillController == null)
        {
            autoSkillController = gameObject.AddComponent<PlayerAutoSkillController>();
        }

        // PlayerController에 연결한 스킬 SO 목록을 자동 시전기에 전달한다.
        autoSkillController.Initialize(this, autoSkills);
    }

    private void ResolveCollectionSystems()
    {
        inventory = BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : InventoryFacility.FindAny();
        assemblyFactory = BaseCampManager.Instance != null
            ? BaseCampManager.Instance.AssemblyFactory
            : FindFirstObjectByType<AssemblyFactory>(FindObjectsInactive.Include);
    }

    private void InitializeCollectionLoadout()
    {
        EnsureSkillSlotCount();
        if (inventory == null)
        {
            return;
        }

        inventory.RegisterInitialWeapon(weaponConfig);
        inventory.RegisterInitialSkills(autoSkills);

        string savedWeaponId = PlayerPrefs.GetString(EquippedWeaponIdKey, string.Empty);
        ProjectileConfig savedWeapon = FindWeaponById(savedWeaponId);
        if (savedWeapon != null && inventory.ContainsWeapon(savedWeapon))
        {
            weaponConfig = savedWeapon;
        }
        else if (weaponConfig != null)
        {
            PlayerPrefs.SetString(EquippedWeaponIdKey, weaponConfig.Id);
        }

        string skillJson = PlayerPrefs.GetString(EquippedSkillIdsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(skillJson))
        {
            SkillLoadoutSaveData saveData = JsonUtility.FromJson<SkillLoadoutSaveData>(skillJson);
            for (int i = 0; i < SkillSlotCount; i++)
            {
                string skillId = saveData?.skillIds != null && i < saveData.skillIds.Count
                    ? saveData.skillIds[i]
                    : string.Empty;
                PlayerSkillConfig savedSkill = FindSkillById(skillId);
                autoSkills[i] = savedSkill != null && inventory.ContainsSkill(savedSkill) ? savedSkill : null;
            }
        }
        else
        {
            SaveSkillLoadout();
        }

        PlayerPrefs.Save();
    }

    private void EnsureSkillSlotCount()
    {
        autoSkills ??= new List<PlayerSkillConfig>();
        while (autoSkills.Count < SkillSlotCount)
        {
            autoSkills.Add(null);
        }

        if (autoSkills.Count > SkillSlotCount)
        {
            autoSkills.RemoveRange(SkillSlotCount, autoSkills.Count - SkillSlotCount);
        }
    }

    private void SaveSkillLoadout()
    {
        EnsureSkillSlotCount();
        SkillLoadoutSaveData saveData = new SkillLoadoutSaveData();
        for (int i = 0; i < SkillSlotCount; i++)
        {
            saveData.skillIds.Add(autoSkills[i] != null ? autoSkills[i].Id : string.Empty);
        }

        PlayerPrefs.SetString(EquippedSkillIdsKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    private ProjectileConfig FindWeaponById(string configId)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(configId))
        {
            return null;
        }

        for (int i = 0; i < inventory.WeaponConfigs.Count; i++)
        {
            ProjectileConfig config = inventory.WeaponConfigs[i];
            if (config != null && config.Id == configId)
            {
                return config;
            }
        }

        return null;
    }

    private PlayerSkillConfig FindSkillById(string configId)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(configId))
        {
            return null;
        }

        for (int i = 0; i < inventory.SkillConfigs.Count; i++)
        {
            PlayerSkillConfig config = inventory.SkillConfigs[i];
            if (config != null && config.Id == configId)
            {
                return config;
            }
        }

        return null;
    }

    private void Update()
    {
        CombatPlane.ClampTransform(transform);
        ApplyUnitConfig();

        if (health != null && health.IsDead)
        {
            if (isRepositioning)
            {
                FinishAutoReposition();
            }

            return;
        }

        if (bossDodgeController != null && bossDodgeController.IsDodging)
        {
            if (isRepositioning)
            {
                FinishAutoReposition();
            }

            SetVehicleMoveInput(0f, 0f);
            TryAimAndAttackCurrentTarget();
            return;
        }

        // 타겟은 한 번 잡으면 처치되거나 사라질 때까지 유지한다.
        if (!HasValidCurrentTarget())
        {
            currentTarget = FindClosestTarget();
        }

        if (HandleAutoReposition())
        {
            return;
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

    private bool HandleAutoReposition()
    {
        if (!enableAutoReposition)
        {
            isRepositioning = false;
            return false;
        }

        if (!isRepositioning
            && Time.time >= nextRepositionTime
            && Time.time >= nextRepositionCheckTime)
        {
            nextRepositionCheckTime = Time.time + Mathf.Max(0.1f, repositionCheckInterval);
            TryStartAutoReposition();
        }

        if (!isRepositioning)
        {
            return false;
        }

        Vector3 moveDirection = CombatPlane.Direction(transform.position, repositionDestination);
        float remainingDistance = Mathf.Sqrt(CombatPlane.DistanceSqr(transform.position, repositionDestination));
        if (remainingDistance <= Mathf.Max(0.01f, repositionStopDistance) || moveDirection.sqrMagnitude <= 0f)
        {
            FinishAutoReposition();
            return false;
        }

        RotateToward(moveDirection);
        float moveDistance = Mathf.Min(
            MoveSpeedValue * Mathf.Max(0.1f, repositionSpeedMultiplier) * Time.deltaTime,
            remainingDistance);
        Vector3 nextPosition = CombatPlane.WithFixedY(transform.position + moveDirection * moveDistance);
        if (IsRepositionPathBlocked(transform.position, nextPosition))
        {
            FinishAutoReposition();
            return false;
        }

        transform.position = ClampRepositionPosition(nextPosition);
        SetVehicleMoveInput(1f, 0f);

        // 자동 재배치 중에도 본체와 별개로 터렛은 기존 타겟을 계속 조준하고 공격한다.
        TryAimAndAttackCurrentTarget();
        return true;
    }

    private void TryStartAutoReposition()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        Vector3 enemyCenter = Vector3.zero;
        int nearbyEnemyCount = 0;
        float detectionRadiusSqr = Mathf.Max(0.01f, repositionDetectionRadius)
            * Mathf.Max(0.01f, repositionDetectionRadius);

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyController enemy = enemies[i];
            CombatHealth enemyHealth = enemy != null ? enemy.GetComponent<CombatHealth>() : null;
            if (enemyHealth == null || enemyHealth.IsDead)
            {
                continue;
            }

            if (CombatPlane.DistanceSqr(transform.position, enemy.transform.position) > detectionRadiusSqr)
            {
                continue;
            }

            enemyCenter += CombatPlane.WithFixedY(enemy.transform.position);
            nearbyEnemyCount++;
        }

        if (nearbyEnemyCount < Mathf.Max(1, repositionEnemyThreshold))
        {
            return;
        }

        enemyCenter /= nearbyEnemyCount;
        Vector3 escapeDirection = CombatPlane.Direction(enemyCenter, transform.position);
        if (escapeDirection.sqrMagnitude <= 0f)
        {
            escapeDirection = CombatPlane.ProjectDirection(-transform.right);
        }

        if (!TryFindRepositionDestination(escapeDirection, out repositionDestination))
        {
            nextRepositionTime = Time.time + 1f;
            return;
        }

        isRepositioning = true;
        nextRepositionTime = Time.time + Mathf.Max(0.1f, RepositionCooldownValue);
    }

    private bool TryFindRepositionDestination(Vector3 escapeDirection, out Vector3 destination)
    {
        float[] angleOffsets = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 180f };
        Vector3 startPosition = CombatPlane.WithFixedY(transform.position);
        for (int i = 0; i < angleOffsets.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, angleOffsets[i], 0f) * escapeDirection;
            Vector3 candidate = ClampRepositionPosition(
                startPosition + direction * Mathf.Max(0.1f, RepositionDistanceValue));
            if (CombatPlane.DistanceSqr(startPosition, candidate) <= 0.01f
                || IsRepositionPathBlocked(startPosition, candidate))
            {
                continue;
            }

            destination = candidate;
            return true;
        }

        destination = startPosition;
        return false;
    }

    private bool IsRepositionPathBlocked(Vector3 from, Vector3 to)
    {
        if (repositionObstacleMask.value == 0)
        {
            return false;
        }

        Vector3 direction = CombatPlane.Direction(from, to);
        float distance = Mathf.Sqrt(CombatPlane.DistanceSqr(from, to));
        if (direction.sqrMagnitude <= 0f || distance <= 0f)
        {
            return false;
        }

        // 벽을 통과하지 않도록 플레이어 크기 기준으로 이동 경로를 검사한다.
        return Physics.SphereCast(
            CombatPlane.WithFixedY(from),
            Mathf.Max(0.01f, repositionCollisionRadius),
            direction,
            out _,
            distance,
            repositionObstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private Vector3 ClampRepositionPosition(Vector3 position)
    {
        position = CombatPlane.WithFixedY(position);
        if (!clampRepositionToBounds)
        {
            return position;
        }

        Vector3 center = repositionBoundsCenter != null
            ? CombatPlane.WithFixedY(repositionBoundsCenter.position)
            : fallbackRepositionBoundsCenter;
        Vector2 halfSize = Vector2.Max(
            Vector2.zero,
            repositionBoundsSize * 0.5f - Vector2.one * Mathf.Max(0f, repositionBoundsPadding));
        position.x = Mathf.Clamp(position.x, center.x - halfSize.x, center.x + halfSize.x);
        position.z = Mathf.Clamp(position.z, center.z - halfSize.y, center.z + halfSize.y);
        return CombatPlane.WithFixedY(position);
    }

    private void FinishAutoReposition()
    {
        isRepositioning = false;
        SetVehicleMoveInput(0f, 0f);
    }

    private void TryAimAndAttackCurrentTarget()
    {
        if (!HasValidCurrentTarget())
        {
            return;
        }

        Vector3 targetDirection = CombatPlane.Direction(transform.position, currentTarget.transform.position);
        if (targetDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        AimWeaponToward(targetDirection);
        if (!IsTargetInRange(currentTarget)
            || !IsWeaponFacing(targetDirection)
            || Time.time < nextAttackTime)
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
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        _rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

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

        // 최초 설정만 완전 회복하고 이후 장비 교체는 현재 체력 비율을 보존한다.
        appliedMaxHealth = configuredMaxHealth;
        if (!hasAppliedHealthStats)
        {
            hasAppliedHealthStats = true;
            health.Initialize(configuredMaxHealth);
            return;
        }

        health.SetMaxHealth(configuredMaxHealth, true);
    }

    private void HandleEquipmentPartLoadoutChanged()
    {
        ApplyHealthStats();
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
        Vector3 nextPosition = CombatPlane.WithFixedY(transform.position + direction * moveDistance);
        if (_rigidbody != null)
        {
            _rigidbody.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }
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
        float damage = CalculateAttackDamage(activeProjectileConfig, out bool isCritical);
        if (GetMultiMuzzleFireMode(activeProjectileConfig) == MultiMuzzleFireMode.SplitDamage)
        {
            damage /= Mathf.Max(1, muzzleCount);
        }

        for (int i = 0; i < muzzleCount; i++)
        {
            FireProjectileFrom(GetFireMuzzle(i), direction, damage, activeProjectileConfig, isCritical);
        }

        return GetAttackIntervalMultiplier(activeProjectileConfig);
    }

    private void FireProjectileFrom(
        Transform muzzle,
        Vector3 direction,
        float damage,
        ProjectileConfig activeProjectileConfig,
        bool isCritical)
    {
        PlayerProjectile projectile = CreateProjectile();
        projectile.transform.position = GetFirePosition(muzzle, direction);
        projectile.Configure(activeProjectileConfig);
        projectile.ConfigureRuntimeStats(
            GetProjectileCollisionRadius(activeProjectileConfig),
            GetProjectileKnockback(activeProjectileConfig));
        projectile.ConfigureEffects(fireFlashEffectPrefab, projectileEffectPrefab, hitEffectPrefab);
        projectile.Launch(
            direction,
            damage,
            GetProjectileSpeed(activeProjectileConfig),
            GetProjectileLifetime(activeProjectileConfig),
            health,
            isCritical);
    }

    private float CalculateAttackDamage(ProjectileConfig activeProjectileConfig, out bool isCritical)
    {
        // 일반 공격과 스킬이 동일한 최종 공격력 계산을 사용한다.
        float damage = GetTotalAttackDamage(activeProjectileConfig);
        float chance = Mathf.Clamp01(CritChanceValue);
        float multiplier = Mathf.Max(1f, CritMultiplierValue);

        // 치명타는 방어력 없이 최종 발사 데미지에만 단순 배율로 적용한다.
        isCritical = Random.value < chance;
        if (isCritical)
        {
            damage *= multiplier;
        }

        return damage;
    }

    private float GetTotalAttackDamage(ProjectileConfig activeProjectileConfig)
    {
        // 기체, 무기, 스탯, 장비, 퍼펙트 회피 보정을 한 곳에서 합산한다.
        return (AttackDamageValue + GetWeaponAttackDamage(activeProjectileConfig))
            * (statAllocator != null ? statAllocator.AttackMultiplier : 1f)
            * (1f + (equipmentPartLoadout != null ? equipmentPartLoadout.AttackPercent : 0f))
            * (bossDodgeController != null ? bossDodgeController.AttackDamageMultiplier : 1f);
    }

    private float GetWeaponAttackDamage(ProjectileConfig activeProjectileConfig)
    {
        if (activeProjectileConfig == null)
        {
            return 0f;
        }

        int level = inventory != null ? Mathf.Max(1, inventory.GetWeaponLevel(activeProjectileConfig)) : 1;
        float levelMultiplier = DuplicateLevelProgression.GetLevelMultiplier(
            level,
            activeProjectileConfig.DamagePercentPerLevel);
        return activeProjectileConfig.AttackDamage * levelMultiplier
            + GetAssemblyBonus(activeProjectileConfig, AssemblyFactory.WeaponEnhancementStat.AttackDamage);
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
        return activeProjectileConfig != null
            ? activeProjectileConfig.Speed
                + GetAssemblyBonus(activeProjectileConfig, AssemblyFactory.WeaponEnhancementStat.Speed)
            : projectileSpeed;
    }

    private float GetProjectileLifetime(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null
            ? activeProjectileConfig.Lifetime
                + GetAssemblyBonus(activeProjectileConfig, AssemblyFactory.WeaponEnhancementStat.Lifetime)
            : projectileLifetime;
    }

    private float GetProjectileCollisionRadius(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null
            ? activeProjectileConfig.CollisionRadius
                + GetAssemblyBonus(activeProjectileConfig, AssemblyFactory.WeaponEnhancementStat.CollisionRadius)
            : 0.2f;
    }

    private float GetProjectileKnockback(ProjectileConfig activeProjectileConfig)
    {
        return activeProjectileConfig != null
            ? activeProjectileConfig.KnockbackForce
                + GetAssemblyBonus(activeProjectileConfig, AssemblyFactory.WeaponEnhancementStat.KnockbackForce)
            : 0f;
    }

    private float GetAssemblyBonus(
        ProjectileConfig activeProjectileConfig,
        AssemblyFactory.WeaponEnhancementStat stat)
    {
        return assemblyFactory != null && activeProjectileConfig != null
            ? assemblyFactory.GetWeaponStatBonus(activeProjectileConfig, stat)
            : 0f;
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

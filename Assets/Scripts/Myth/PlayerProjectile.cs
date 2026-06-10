using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ProjectileConfig projectileConfig;

    [Header("Effects")]
    [SerializeField] private GameObject fireFlashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float effectCleanupDelay = 2f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private WeaponAttackType attackType = WeaponAttackType.SingleTarget;
    [SerializeField] private float areaRadius = 2f;
    [SerializeField] private float areaDamageMultiplier = 0.7f;
    [SerializeField] private int maxAreaTargets = 10;
    [SerializeField] private int maxPierceTargets = 3;

    private readonly HashSet<CombatHealth> piercedTargets = new HashSet<CombatHealth>();
    private float damage;
    private float speed;
    private float expireTime;
    private Vector3 direction;
    private CombatHealth owner;
    private Rigidbody body;
    private bool hasHit;
    private bool isCritical;
    private GameObject projectileEffectInstance;
    private bool isReleased;
    private int _wallLayer;

    public Vector3 TravelDirection => direction;
    public float TravelSpeed => speed;
    public Vector3 TravelVelocity => direction * speed;
    public bool IsInFlight => !isReleased && gameObject.activeInHierarchy;

    private void Awake()
    {
        Configure(projectileConfig);
        EnsureProjectileComponents();
        _wallLayer = LayerMask.NameToLayer("Wall");
    }

    private void Update()
    {
        CombatPlane.ClampTransform(transform);
        CombatPlane.ClampVelocity(body);
        CheckOverlapHit();

        if (Time.time >= expireTime)
        {
            ReturnToPool();
        }
    }

    public void PrepareForReuse()
    {
        isReleased = false;
        hasHit = false;
        piercedTargets.Clear();
    }

    public void ResetForPool()
    {
        isReleased = true;
        hasHit = true;
        damage = 0f;
        speed = 0f;
        expireTime = 0f;
        owner = null;
        direction = Vector3.zero;
        isCritical = false;
        piercedTargets.Clear();

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        if (projectileEffectInstance != null)
        {
            CombatObjectPool.ReleaseEffect(projectileEffectInstance);
            projectileEffectInstance = null;
        }
    }

    public void ConfigureEffects(GameObject fireFlashEffect, GameObject projectileEffect, GameObject hitEffect)
    {
        // 플레이어 쪽에서 지정한 이펙트가 있으면 투사체 프리팹 기본값보다 우선 사용한다.
        if (fireFlashEffect != null)
        {
            fireFlashEffectPrefab = fireFlashEffect;
        }

        if (projectileEffect != null)
        {
            projectileEffectPrefab = projectileEffect;
        }

        if (hitEffect != null)
        {
            hitEffectPrefab = hitEffect;
        }
    }

    public void Configure(ProjectileConfig config)
    {
        if (config == null)
        {
            config = projectileConfig;
        }

        if (config == null)
        {
            return;
        }

        // 투사체 SO가 있으면 이펙트/충돌 반경/정리 시간을 한 번에 적용한다.
        projectileConfig = config;
        fireFlashEffectPrefab = config.FireFlashEffectPrefab;
        projectileEffectPrefab = config.ProjectileEffectPrefab;
        hitEffectPrefab = config.HitEffectPrefab;
        effectCleanupDelay = config.EffectCleanupDelay;
        collisionRadius = config.CollisionRadius;
        knockbackForce = config.KnockbackForce;
        attackType = config.AttackType;
        areaRadius = config.AreaRadius;
        areaDamageMultiplier = config.AreaDamageMultiplier;
        maxAreaTargets = config.MaxAreaTargets;
        maxPierceTargets = config.MaxPierceTargets;
        ApplyCollisionRadius();
    }

    public void Launch(
        Vector3 launchDirection,
        float launchDamage,
        float launchSpeed,
        float lifetime,
        CombatHealth launchOwner,
        bool launchCritical = false)
    {
        EnsureProjectileComponents();
        direction = CombatPlane.ProjectDirection(launchDirection);
        if (direction.sqrMagnitude <= 0f)
        {
            direction = Vector3.forward;
        }

        damage = launchDamage;
        speed = launchSpeed;
        owner = launchOwner;
        isCritical = launchCritical;
        expireTime = Time.time + lifetime;
        hasHit = false;
        isReleased = false;
        piercedTargets.Clear();

        transform.position = CombatPlane.WithFixedY(transform.position);
        // Hovl 투사체는 transform.forward 기준으로 움직이므로 루트 forward를 실제 발사 방향에 맞춘다.
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        SpawnFireFlashEffect();
        SpawnProjectileEffect();

        // 투사체 본체는 플레이어 정면 방향으로만 직진한다.
        body.linearVelocity = direction * speed;
    }

    public void ConfigureRuntimeStats(float runtimeCollisionRadius, float runtimeKnockbackForce)
    {
        collisionRadius = Mathf.Max(0f, runtimeCollisionRadius);
        knockbackForce = Mathf.Max(0f, runtimeKnockbackForce);
        ApplyCollisionRadius();
    }

    private void EnsureProjectileComponents()
    {
        CombatPlane.ClampTransform(transform);

        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
        }

        ApplyCollisionRadius();
    }

    private void ApplyCollisionRadius()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.radius = collisionRadius;
        }
    }

    private void SpawnFireFlashEffect()
    {
        if (fireFlashEffectPrefab == null)
        {
            return;
        }

        // 발사 순간 총구 위치에 플래시를 한 번 재생한다.
        GameObject flash = CombatObjectPool.GetEffect(fireFlashEffectPrefab, transform.position, transform.rotation);
        CombatObjectPool.ReleaseEffect(flash, effectCleanupDelay);
    }

    private void SpawnProjectileEffect()
    {
        if (projectileEffectPrefab == null)
        {
            return;
        }

        // 투사체 이펙트는 본체에 붙여 함께 이동시킨다.
        projectileEffectInstance = CombatObjectPool.GetEffect(projectileEffectPrefab, transform.position, transform.rotation, transform);
        projectileEffectInstance.transform.localPosition = Vector3.zero;
        projectileEffectInstance.transform.localRotation = Quaternion.identity;
        DisableExternalProjectileMotion(projectileEffectInstance);
    }

    private void DisableExternalProjectileMotion(GameObject effect)
    {
        if (effect == null)
        {
            return;
        }

        // 외부 VFX 에셋의 데모용 이동/충돌 스크립트는 우리 발사체 이동과 겹치므로 비활성화한다.
        MonoBehaviour[] behaviours = effect.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null
                && (behaviour.GetType().Name == "ProjectileMoveScript"
                    || behaviour.GetType().Name == "HS_ProjectileMover"))
            {
                behaviour.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = effect.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody effectBody = rigidbodies[i];
            if (!effectBody.isKinematic)
            {
                // Kinematic Rigidbody에는 속도를 설정할 수 없으므로 동적 바디만 먼저 정지한다.
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

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        // 충돌 위치에 히트 이펙트를 분리 생성해 투사체가 사라져도 재생되게 한다.
        GameObject hit = CombatObjectPool.GetEffect(hitEffectPrefab, transform.position, transform.rotation);
        CombatObjectPool.ReleaseEffect(hit, effectCleanupDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsWall(other.gameObject))
        {
            HitWall();
            return;
        }
        TryHit(other.GetComponentInParent<CombatHealth>());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsWall(collision.gameObject))
        {
            HitWall();
            return;
        }
        TryHit(collision.collider.GetComponentInParent<CombatHealth>());
    }

    private void CheckOverlapHit()
    {
        if (hasHit)
        {
            return;
        }

        // 물리 충돌 이벤트가 누락되어도 반경 안의 적을 직접 탐지한다.
        Collider[] hits = Physics.OverlapSphere(transform.position, collisionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsWall(hits[i].gameObject))
            {
                HitWall();
                return;
            }

            CombatHealth target = hits[i].GetComponentInParent<CombatHealth>();
            if (target == null || target == owner)
            {
                continue;
            }

            TryHit(target);
            if (hasHit)
            {
                return;
            }
        }
    }

    private bool IsWall(GameObject obj)
    {
        if (obj == null || _wallLayer == -1)
        {
            return false;
        }
        return obj.layer == _wallLayer;
    }

    private void HitWall()
    {
        if (hasHit)
        {
            return;
        }

        hasHit = true;
        SpawnHitEffect();
        ReturnToPool();
    }

    private void TryHit(CombatHealth target)
    {
        if (hasHit
            || target == null
            || target == owner
            || target.IsDead
            || piercedTargets.Contains(target))
        {
            return;
        }

        // 공격 방식에 따라 단일 대상 또는 충돌 지점 주변에 피해를 적용한다.
        if (attackType == WeaponAttackType.Area)
        {
            hasHit = true;
            ApplyAreaDamage(target);
        }
        else if (attackType == WeaponAttackType.Piercing)
        {
            // 같은 적은 한 번만 타격하고 설정한 관통 수까지 투사체를 유지한다.
            piercedTargets.Add(target);
            ApplyDamageToTarget(target, damage);
            SpawnHitEffect();
            if (piercedTargets.Count >= Mathf.Max(1, maxPierceTargets))
            {
                hasHit = true;
                ReturnToPool();
            }
            return;
        }
        else
        {
            hasHit = true;
            ApplyDamageToTarget(target, damage);
        }

        SpawnHitEffect();
        ReturnToPool();
    }

    private void ApplyAreaDamage(CombatHealth directTarget)
    {
        HashSet<CombatHealth> damagedTargets = new HashSet<CombatHealth>();
        int targetLimit = Mathf.Max(1, maxAreaTargets);
        float appliedAreaDamage = damage * Mathf.Max(0f, areaDamageMultiplier);

        // 직접 충돌한 적은 물리 탐지 결과와 관계없이 우선 피해를 적용한다.
        ApplyAreaTarget(directTarget, appliedAreaDamage, damagedTargets);

        Collider[] hits = Physics.OverlapSphere(transform.position, Mathf.Max(0f, areaRadius));
        for (int i = 0; i < hits.Length && damagedTargets.Count < targetLimit; i++)
        {
            CombatHealth target = hits[i].GetComponentInParent<CombatHealth>();
            ApplyAreaTarget(target, appliedAreaDamage, damagedTargets);
        }
    }

    private void ApplyAreaTarget(CombatHealth target, float appliedDamage, HashSet<CombatHealth> damagedTargets)
    {
        if (target == null || target == owner || target.IsDead || damagedTargets.Contains(target))
        {
            return;
        }

        if (target.GetComponentInParent<EnemyController>() == null)
        {
            return;
        }

        damagedTargets.Add(target);
        ApplyDamageToTarget(target, appliedDamage);
    }

    private void ApplyDamageToTarget(CombatHealth target, float appliedDamage)
    {
        target.TakeDamage(appliedDamage, isCritical);
        PlayerController player = owner != null ? owner.GetComponent<PlayerController>() : null;
        CombatRewardService.GrantIfKilled(player, target);
        ApplyKnockback(target);
    }

    private void ApplyKnockback(CombatHealth target)
    {
        if (knockbackForce <= 0f)
        {
            return;
        }

        EnemyController enemy = target.GetComponentInParent<EnemyController>();
        if (enemy == null)
        {
            return;
        }

        // 무기 설정값만큼 투사체 진행 방향으로 적을 밀어낸다.
        enemy.ApplyKnockback(direction, knockbackForce);
    }

    private void ReturnToPool()
    {
        if (isReleased)
        {
            return;
        }

        CombatObjectPool.ReleaseProjectile(this);
    }
}

public class CombatObjectPool : MonoBehaviour
{
    private static CombatObjectPool instance;
    private static readonly Queue<PlayerProjectile> projectiles = new Queue<PlayerProjectile>();
    private static readonly Queue<BossProjectile> bossProjectiles = new Queue<BossProjectile>();
    private static readonly Dictionary<GameObject, Queue<GameObject>> effects = new Dictionary<GameObject, Queue<GameObject>>();

    private Transform projectileRoot;
    private Transform effectRoot;

    private static CombatObjectPool Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject poolObject = new GameObject("CombatObjectPool");
            instance = poolObject.AddComponent<CombatObjectPool>();
            DontDestroyOnLoad(poolObject);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        projectileRoot = CreateRoot("Projectiles");
        effectRoot = CreateRoot("Effects");
    }

    public static PlayerProjectile GetProjectile()
    {
        PlayerProjectile projectile = projectiles.Count > 0 ? projectiles.Dequeue() : CreateProjectile();
        projectile.gameObject.SetActive(true);
        projectile.PrepareForReuse();
        return projectile;
    }

    public static void ReleaseProjectile(PlayerProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        // 발사체 본체는 제거하지 않고 비활성 큐로 돌려보낸다.
        projectile.ResetForPool();
        projectile.transform.SetParent(Instance.projectileRoot, false);
        projectile.gameObject.SetActive(false);
        projectiles.Enqueue(projectile);
    }

    public static BossProjectile GetBossProjectile()
    {
        BossProjectile projectile = bossProjectiles.Count > 0
            ? bossProjectiles.Dequeue()
            : CreateBossProjectile();
        projectile.gameObject.SetActive(true);
        projectile.PrepareForReuse();
        return projectile;
    }

    public static void ReleaseBossProjectile(BossProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        // 보스 발사체도 플레이어 발사체와 별도 큐에서 재사용한다.
        projectile.ResetForPool();
        projectile.transform.SetParent(Instance.projectileRoot, false);
        projectile.gameObject.SetActive(false);
        bossProjectiles.Enqueue(projectile);
    }

    public static GameObject GetEffect(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        Queue<GameObject> queue = GetEffectQueue(prefab);
        GameObject effect = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab);
        CombatPooledEffect pooledEffect = effect.GetComponent<CombatPooledEffect>();
        if (pooledEffect == null)
        {
            pooledEffect = effect.AddComponent<CombatPooledEffect>();
        }

        pooledEffect.Prefab = prefab;
        effect.transform.SetParent(parent, true);
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        effect.SetActive(true);
        RestartParticles(effect);
        return effect;
    }

    public static void ReleaseEffect(GameObject effect, float delay = 0f)
    {
        if (effect == null)
        {
            return;
        }

        if (delay > 0f)
        {
            Instance.StartCoroutine(Instance.ReleaseEffectAfterDelay(effect, delay));
            return;
        }

        CombatPooledEffect pooledEffect = effect.GetComponent<CombatPooledEffect>();
        if (pooledEffect == null || pooledEffect.Prefab == null)
        {
            Destroy(effect);
            return;
        }

        // 이펙트는 원래 프리팹별 큐에 넣어 재사용한다.
        StopParticles(effect);
        effect.transform.SetParent(Instance.effectRoot, false);
        effect.SetActive(false);
        GetEffectQueue(pooledEffect.Prefab).Enqueue(effect);
    }

    private static PlayerProjectile CreateProjectile()
    {
        GameObject projectileObject = new GameObject("Combat Projectile");
        projectileObject.transform.SetParent(Instance.projectileRoot, false);
        PlayerProjectile projectile = projectileObject.AddComponent<PlayerProjectile>();
        projectile.gameObject.SetActive(false);
        return projectile;
    }

    private static BossProjectile CreateBossProjectile()
    {
        GameObject projectileObject = new GameObject("Boss Projectile");
        projectileObject.transform.SetParent(Instance.projectileRoot, false);
        BossProjectile projectile = projectileObject.AddComponent<BossProjectile>();
        projectile.gameObject.SetActive(false);
        return projectile;
    }

    private static Queue<GameObject> GetEffectQueue(GameObject prefab)
    {
        if (!effects.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            effects[prefab] = queue;
        }

        return queue;
    }

    private static void RestartParticles(GameObject effect)
    {
        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }

    private static void StopParticles(GameObject effect)
    {
        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator ReleaseEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseEffect(effect);
    }

    private Transform CreateRoot(string rootName)
    {
        Transform existingRoot = transform.Find(rootName);
        if (existingRoot != null)
        {
            return existingRoot;
        }

        GameObject rootObject = new GameObject(rootName);
        rootObject.transform.SetParent(transform, false);
        return rootObject.transform;
    }
}

public class CombatPooledEffect : MonoBehaviour
{
    public GameObject Prefab;
}

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

    private float damage;
    private float speed;
    private float expireTime;
    private Vector3 direction;
    private CombatHealth owner;
    private Rigidbody body;
    private bool hasHit;
    private GameObject projectileEffectInstance;
    private bool isReleased;

    private void Awake()
    {
        Configure(projectileConfig);
        EnsureProjectileComponents();
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
        ApplyCollisionRadius();
    }

    public void Launch(Vector3 launchDirection, float launchDamage, float launchSpeed, float lifetime, CombatHealth launchOwner)
    {
        EnsureProjectileComponents();
        direction = CombatPlane.ProjectDirection(launchDirection);
        if (direction.sqrMagnitude <= 0f)
        {
            direction = Vector3.forward;
        }

        damage = launchDamage;
        speed = projectileConfig != null ? projectileConfig.Speed : launchSpeed;
        owner = launchOwner;
        expireTime = Time.time + (projectileConfig != null ? projectileConfig.Lifetime : lifetime);
        hasHit = false;
        isReleased = false;

        transform.position = CombatPlane.WithFixedY(transform.position);
        // Hovl 투사체는 transform.forward 기준으로 움직이므로 루트 forward를 실제 발사 방향에 맞춘다.
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        SpawnFireFlashEffect();
        SpawnProjectileEffect();

        // 투사체 본체는 플레이어 정면 방향으로만 직진한다.
        body.linearVelocity = direction * speed;
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
            if (behaviour != null && behaviour.GetType().Name == "ProjectileMoveScript")
            {
                behaviour.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = effect.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].linearVelocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
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
        TryHit(other.GetComponentInParent<CombatHealth>());
    }

    private void OnCollisionEnter(Collision collision)
    {
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

    private void TryHit(CombatHealth target)
    {
        if (hasHit || target == null || target == owner || target.IsDead)
        {
            return;
        }

        // 명중한 대상에게 데미지를 주고 충돌 이펙트를 재생한다.
        hasHit = true;
        target.TakeDamage(damage);
        GrantExperienceIfKilled(target);
        ApplyKnockback(target);
        SpawnHitEffect();
        ReturnToPool();
    }

    private void GrantExperienceIfKilled(CombatHealth target)
    {
        if (target == null || !target.IsDead || owner == null)
        {
            return;
        }

        PlayerProgression progression = owner.GetComponent<PlayerProgression>();
        EnemyController enemy = target.GetComponentInParent<EnemyController>();
        if (progression == null || enemy == null)
        {
            return;
        }

        // 플레이어 투사체가 적을 처치하면 v1 경험치를 지급한다.
        progression.AddExperience(enemy.ExperienceReward);
        GrantCurrencyReward(enemy);
    }

    private void GrantCurrencyReward(EnemyController enemy)
    {
        if (enemy == null || owner == null)
        {
            return;
        }

        PlayerCurrencyWallet wallet = owner.GetComponent<PlayerCurrencyWallet>();
        if (wallet == null && BaseCampManager.Instance != null)
        {
            wallet = BaseCampManager.Instance.CurrencyWallet;
        }

        if (wallet == null)
        {
            wallet = FindFirstObjectByType<PlayerCurrencyWallet>();
        }

        if (wallet == null)
        {
            return;
        }

        // 적 처치 보상은 공통 재화 API로 누적한다.
        wallet.Add(CurrencyType.Credits, enemy.CreditReward);
        wallet.Add(CurrencyType.CoreCrystals, enemy.CoreCrystalReward);
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

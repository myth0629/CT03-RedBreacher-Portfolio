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
            Destroy(gameObject);
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

        transform.position = CombatPlane.WithFixedY(transform.position);
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

        if (projectileEffectPrefab == null && GetComponent<SpriteRenderer>() == null)
        {
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CombatVisualFactory.CreateCircleSprite(Color.yellow);
            renderer.sortingOrder = 10;
        }
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
        GameObject flash = Instantiate(fireFlashEffectPrefab, transform.position, transform.rotation);
        Destroy(flash, effectCleanupDelay);
    }

    private void SpawnProjectileEffect()
    {
        if (projectileEffectPrefab == null)
        {
            return;
        }

        // 투사체 이펙트는 본체에 붙여 함께 이동시킨다.
        projectileEffectInstance = Instantiate(projectileEffectPrefab, transform);
        projectileEffectInstance.transform.localPosition = Vector3.zero;
        projectileEffectInstance.transform.localRotation = Quaternion.identity;
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        // 충돌 위치에 히트 이펙트를 분리 생성해 투사체가 사라져도 재생되게 한다.
        GameObject hit = Instantiate(hitEffectPrefab, transform.position, transform.rotation);
        Destroy(hit, effectCleanupDelay);
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
        ApplyKnockback(target);
        SpawnHitEffect();
        Destroy(gameObject);
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
}

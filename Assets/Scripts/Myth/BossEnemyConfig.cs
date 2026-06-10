using UnityEngine;

[CreateAssetMenu(menuName = "Myth/Combat/Boss Enemy Config")]
public class BossEnemyConfig : EnemyConfig
{
    [Header("Ranged Attack")]
    [SerializeField] private float rangedAttackRange = 7f;
    [SerializeField] private float rangedAttackDamage = 12f;
    [SerializeField] private float rangedAttackInterval = 1.5f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float projectileCollisionRadius = 0.25f;
    [SerializeField] private string firePointNamePrefix = "FirePoint";
    [SerializeField] private int projectilesPerFirePoint = 5;
    [SerializeField] private float spreadAngle = 60f;
    [SerializeField] private GameObject fireEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float effectCleanupDelay = 2f;

    [Header("Laser Pattern")]
    [SerializeField] private float laserCooldown = 6f;
    [SerializeField] private float laserDamage = 25f;
    [SerializeField] private float laserLength = 12f;
    [SerializeField] private float laserWidth = 1.5f;
    [SerializeField] private float laserWarningDuration = 1.5f;
    [SerializeField] private float laserActiveDuration = 0.25f;
    [SerializeField] private string laserFirePointName = "LaserFirePoint";
    [SerializeField] private GameObject laserBeamEffectPrefab;
    [SerializeField] private float laserProjectileSpeed = 20f;
    [SerializeField] private float laserProjectileScale = 1f;
    [SerializeField] private Vector3 laserBeamRotationOffset;
    [SerializeField] private Material laserLineMaterial;
    [SerializeField] private Color laserWarningColor = new Color(1f, 0.2f, 0.1f, 0.55f);
    [SerializeField] private Color laserActiveColor = Color.red;

    [Header("Projectile Dodge")]
    [SerializeField] private float dodgeDetectionRadius = 4f;
    [SerializeField] private float dodgeDistance = 2f;
    [SerializeField] private float dodgeCooldown = 2.5f;
    [SerializeField] private float dodgeSpeed = 6f;
    [SerializeField] private float dodgePredictionTime = 0.8f;
    [SerializeField] private float dodgeCollisionRadius = 0.75f;
    [SerializeField] private float dodgeCheckInterval = 0.1f;
    [SerializeField] private LayerMask dodgeObstacleMask;

    public float RangedAttackRange => Mathf.Max(0.1f, rangedAttackRange);
    public float RangedAttackDamage => Mathf.Max(0f, rangedAttackDamage);
    public float RangedAttackInterval => Mathf.Max(0.1f, rangedAttackInterval);
    public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
    public float ProjectileLifetime => Mathf.Max(0.1f, projectileLifetime);
    public float ProjectileCollisionRadius => Mathf.Max(0.01f, projectileCollisionRadius);
    public string FirePointNamePrefix => string.IsNullOrWhiteSpace(firePointNamePrefix)
        ? "FirePoint"
        : firePointNamePrefix;
    public int ProjectilesPerFirePoint => Mathf.Max(1, projectilesPerFirePoint);
    public float SpreadAngle => Mathf.Max(0f, spreadAngle);
    public GameObject FireEffectPrefab => fireEffectPrefab;
    public GameObject ProjectileEffectPrefab => projectileEffectPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public float EffectCleanupDelay => Mathf.Max(0f, effectCleanupDelay);
    public float LaserCooldown => Mathf.Max(0.1f, laserCooldown);
    public float LaserDamage => Mathf.Max(0f, laserDamage);
    public float LaserLength => Mathf.Max(0.1f, laserLength);
    public float LaserWidth => Mathf.Max(0.05f, laserWidth);
    public float LaserWarningDuration => Mathf.Max(0f, laserWarningDuration);
    public float LaserActiveDuration => Mathf.Max(0f, laserActiveDuration);
    public string LaserFirePointName => string.IsNullOrWhiteSpace(laserFirePointName)
        ? "LaserFirePoint"
        : laserFirePointName;
    public GameObject LaserBeamEffectPrefab => laserBeamEffectPrefab;
    public float LaserProjectileSpeed => Mathf.Max(0.1f, laserProjectileSpeed);
    public float LaserProjectileScale => Mathf.Max(0.01f, laserProjectileScale);
    public Vector3 LaserBeamRotationOffset => laserBeamRotationOffset;
    public Material LaserLineMaterial => laserLineMaterial;
    public Color LaserWarningColor => laserWarningColor;
    public Color LaserActiveColor => laserActiveColor;
    public float DodgeDetectionRadius => Mathf.Max(0.1f, dodgeDetectionRadius);
    public float DodgeDistance => Mathf.Max(0.1f, dodgeDistance);
    public float DodgeCooldown => Mathf.Max(0.1f, dodgeCooldown);
    public float DodgeSpeed => Mathf.Max(0.1f, dodgeSpeed);
    public float DodgePredictionTime => Mathf.Max(0.01f, dodgePredictionTime);
    public float DodgeCollisionRadius => Mathf.Max(0.01f, dodgeCollisionRadius);
    public float DodgeCheckInterval => Mathf.Max(0.02f, dodgeCheckInterval);
    public LayerMask DodgeObstacleMask => dodgeObstacleMask;
}

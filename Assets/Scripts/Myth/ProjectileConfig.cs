using UnityEngine;

[CreateAssetMenu(menuName = "Myth/Combat/Projectile Config")]
public class ProjectileConfig : ScriptableObject
{
    [Header("Projectile")]
    [SerializeField] private PlayerProjectile projectilePrefab;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float knockbackForce = 2f;

    [Header("Effects")]
    [SerializeField] private GameObject fireFlashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float effectCleanupDelay = 2f;

    public PlayerProjectile ProjectilePrefab => projectilePrefab;
    public float Speed => speed;
    public float Lifetime => lifetime;
    public float CollisionRadius => collisionRadius;
    public float KnockbackForce => knockbackForce;
    public GameObject FireFlashEffectPrefab => fireFlashEffectPrefab;
    public GameObject ProjectileEffectPrefab => projectileEffectPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public float EffectCleanupDelay => effectCleanupDelay;
}

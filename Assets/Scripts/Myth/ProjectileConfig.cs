using UnityEngine;

public enum MultiMuzzleFireMode
{
    Single,
    SplitDamage,
    BurstKeepsDps,
    FullPowerBurst
}

public enum WeaponAttackType
{
    SingleTarget,
    Area
}

[CreateAssetMenu(menuName = "Myth/Combat/Projectile Config")]
public class ProjectileConfig : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "weapon_default";
    [SerializeField] private string displayName = "기본 무기";
    [SerializeField] private string weaponCategory = "포탄";

    [Header("Combat")]
    [SerializeField] private float attackDamage = 0f;
    [SerializeField] private WeaponAttackType attackType = WeaponAttackType.SingleTarget;
    [SerializeField] private float areaRadius = 2f;
    [SerializeField] private float areaDamageMultiplier = 0.7f;
    [SerializeField] private int maxAreaTargets = 10;

    [Header("Collection Level")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private float damagePercentPerLevel = 0.1f;
    [SerializeField] private int maxLevelDuplicateCoreCrystalReward = 1;

    [Header("Projectile")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float knockbackForce = 2f;

    [Header("Muzzle")]
    [SerializeField] private MultiMuzzleFireMode multiMuzzleFireMode = MultiMuzzleFireMode.BurstKeepsDps;
    [SerializeField] private int maxBurstMuzzleCount = 3;
    [SerializeField] private string muzzleNamePrefix = "FireMuzzle";

    [Header("Effects")]
    [SerializeField] private GameObject fireFlashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float effectCleanupDelay = 2f;

    public string Id => id;
    public string DisplayName => displayName;
    public string WeaponCategory => weaponCategory;
    public float AttackDamage => attackDamage;
    public WeaponAttackType AttackType => attackType;
    public float AreaRadius => areaRadius;
    public float AreaDamageMultiplier => areaDamageMultiplier;
    public int MaxAreaTargets => maxAreaTargets;
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public float DamagePercentPerLevel => Mathf.Max(0f, damagePercentPerLevel);
    public int MaxLevelDuplicateCoreCrystalReward => Mathf.Max(0, maxLevelDuplicateCoreCrystalReward);
    public float Speed => speed;
    public float Lifetime => lifetime;
    public float CollisionRadius => collisionRadius;
    public float KnockbackForce => knockbackForce;
    public MultiMuzzleFireMode MultiMuzzleFireMode => multiMuzzleFireMode;
    public int MaxBurstMuzzleCount => maxBurstMuzzleCount;
    public string MuzzleNamePrefix => muzzleNamePrefix;
    public GameObject FireFlashEffectPrefab => fireFlashEffectPrefab;
    public GameObject ProjectileEffectPrefab => projectileEffectPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public float EffectCleanupDelay => effectCleanupDelay;
}

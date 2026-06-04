using UnityEngine;

[CreateAssetMenu(menuName = "Myth/Combat/Drone Config")]
public class DroneConfig : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "drone_default";
    [SerializeField] private string displayName = "기본 드론";
    [SerializeField] private GameObject dronePrefab;

    [Header("Combat")]
    [SerializeField] private ProjectileConfig projectileConfig;
    [SerializeField] private float attackDamage = 3f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float fireAngleTolerance = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private LayerMask targetMask;

    [Header("Projectile Fallback")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 2f;

    [Header("Formation")]
    [SerializeField] private int droneCount = 1;
    [SerializeField] private float followRadius = 1.2f;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float startAngle = 180f;
    [SerializeField] private float angleStep = 45f;
    [SerializeField] private string muzzleNamePrefix = "FireMuzzle";

    public string Id => id;
    public string DisplayName => displayName;
    public GameObject DronePrefab => dronePrefab;
    public ProjectileConfig ProjectileConfig => projectileConfig;
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public float FireAngleTolerance => fireAngleTolerance;
    public float RotationSpeed => rotationSpeed;
    public LayerMask TargetMask => targetMask;
    public float ProjectileSpeed => projectileConfig != null ? projectileConfig.Speed : projectileSpeed;
    public float ProjectileLifetime => projectileConfig != null ? projectileConfig.Lifetime : projectileLifetime;
    public int DroneCount => Mathf.Max(1, droneCount);
    public float FollowRadius => followRadius;
    public float FollowSpeed => followSpeed;
    public float StartAngle => startAngle;
    public float AngleStep => angleStep;
    public string MuzzleNamePrefix => muzzleNamePrefix;
}

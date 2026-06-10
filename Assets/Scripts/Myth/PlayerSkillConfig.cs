using UnityEngine;

public enum PlayerSkillType
{
    Bombardment,
    AutoTurret
}

[CreateAssetMenu(menuName = "Myth/Combat/Player Skill Config")]
public class PlayerSkillConfig : ScriptableObject, IDuplicateLevelConfig
{
    [Header("Identity")]
    [SerializeField] private string id = "skill_default";
    [SerializeField] private string displayName = "자동 스킬";
    [SerializeField] private Sprite icon;
    [SerializeField] private PlayerSkillType skillType;

    [Header("Auto Cast")]
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float castRange = 8f;
    [SerializeField] private int minimumEnemyCount = 1;

    [Header("Damage")]
    [SerializeField] private float attackPowerMultiplier = 2f;
    [SerializeField] private float flatDamage;
    [SerializeField] private bool canCritical;
    [SerializeField] private float effectRadius = 3f;
    [SerializeField] private int maxTargets = 20;
    [SerializeField] private float knockbackForce;

    [Header("Collection Level")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private float damagePercentPerLevel = 0.1f;
    [SerializeField] private int maxLevelDuplicateCoreCrystalReward = 1;

    [Header("Bombardment")]
    [SerializeField] private float impactDelay = 0.5f;
    [SerializeField] private GameObject warningEffectPrefab;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectCleanupDelay = 2f;
    [SerializeField] private GameObject _airplanePrefab;
    [SerializeField] private float _airplaneSpeed = 15f;
    [SerializeField] private float _airplaneSpawnOffset = 15f;
    [SerializeField] private float _airplaneHeight = 6f;
    [SerializeField] private GameObject _bombProjectilePrefab;
    [SerializeField] private float _bombEffectScale = 1f;
    [SerializeField] private int _bombCount = 1;
    [SerializeField] private float _bombInterval = 0.15f;
    [SerializeField] private float _screenShakeDuration = 0.22f;
    [SerializeField] private float _screenShakeStrength = 0.4f;
    [SerializeField] private float _screenShakeFrequency = 1.5f;

    [Header("Auto Turret")]
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private ProjectileConfig turretProjectileConfig;
    [SerializeField] private float turretDuration = 8f;
    [SerializeField] private float turretAttackInterval = 0.8f;
    [SerializeField] private float turretAttackRange = 6f;
    [SerializeField] private float turretRotationSpeed = 360f;
    [SerializeField] private float turretPlacementDistance = 1.5f;
    [SerializeField] private string turretFirePointName = "FirePoint";

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public PlayerSkillType SkillType => skillType;
    public float Cooldown => Mathf.Max(0.1f, cooldown);
    public float CastRange => Mathf.Max(0f, castRange);
    public int MinimumEnemyCount => Mathf.Max(1, minimumEnemyCount);
    public float AttackPowerMultiplier => Mathf.Max(0f, attackPowerMultiplier);
    public float FlatDamage => Mathf.Max(0f, flatDamage);
    public bool CanCritical => canCritical;
    public float EffectRadius => Mathf.Max(0f, effectRadius);
    public int MaxTargets => Mathf.Max(1, maxTargets);
    public float KnockbackForce => Mathf.Max(0f, knockbackForce);
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public float DamagePercentPerLevel => Mathf.Max(0f, damagePercentPerLevel);
    public int MaxLevelDuplicateCoreCrystalReward => Mathf.Max(0, maxLevelDuplicateCoreCrystalReward);
    public float ImpactDelay => Mathf.Max(0f, impactDelay);
    public GameObject WarningEffectPrefab => warningEffectPrefab;
    public GameObject ImpactEffectPrefab => impactEffectPrefab;
    public float EffectCleanupDelay => Mathf.Max(0f, effectCleanupDelay);
    public GameObject AirplanePrefab => _airplanePrefab;
    public float AirplaneSpeed => Mathf.Max(0.1f, _airplaneSpeed);
    public float AirplaneSpawnOffset => Mathf.Max(0f, _airplaneSpawnOffset);
    public float AirplaneHeight => _airplaneHeight;
    public GameObject BombProjectilePrefab => _bombProjectilePrefab;
    public float BombEffectScale => Mathf.Max(0.01f, _bombEffectScale);
    public int BombCount => Mathf.Max(1, _bombCount);
    public float BombInterval => Mathf.Max(0f, _bombInterval);
    public float ScreenShakeDuration => Mathf.Max(0f, _screenShakeDuration);
    public float ScreenShakeStrength => Mathf.Max(0f, _screenShakeStrength);
    public float ScreenShakeFrequency => Mathf.Max(0.01f, _screenShakeFrequency);
    public GameObject TurretPrefab => turretPrefab;
    public ProjectileConfig TurretProjectileConfig => turretProjectileConfig;
    public float TurretDuration => Mathf.Max(0.1f, turretDuration);
    public float TurretAttackInterval => Mathf.Max(0.1f, turretAttackInterval);
    public float TurretAttackRange => Mathf.Max(0f, turretAttackRange);
    public float TurretRotationSpeed => Mathf.Max(0f, turretRotationSpeed);
    public float TurretPlacementDistance => Mathf.Max(0f, turretPlacementDistance);
    public string TurretFirePointName => turretFirePointName;
}

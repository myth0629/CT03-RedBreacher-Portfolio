using UnityEngine;

[CreateAssetMenu(menuName = "Myth/Combat/Player Unit Config")]
public class PlayerUnitConfig : ScriptableObject
{
    [Header("Visual")]
    [SerializeField] private string displayName = "탱크이름";
    [SerializeField] private GameObject unitPrefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Range(0f, 1f)] private float critChance = 0.1f;
    [SerializeField] private float critMultiplier = 1.5f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float fireAngleTolerance = 3f;

    [Header("Projectile")]
    [SerializeField] private ProjectileConfig projectileConfig;

    public string DisplayName => displayName;
    public GameObject UnitPrefab => unitPrefab;
    public float MaxHealth => maxHealth;
    public float CritChance => critChance;
    public float CritMultiplier => critMultiplier;
    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float FireAngleTolerance => fireAngleTolerance;
    public ProjectileConfig ProjectileConfig => projectileConfig;
}

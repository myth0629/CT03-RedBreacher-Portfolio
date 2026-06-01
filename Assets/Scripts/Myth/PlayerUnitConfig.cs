using UnityEngine;

[CreateAssetMenu(menuName = "Myth/Combat/Player Unit Config")]
public class PlayerUnitConfig : ScriptableObject
{
    [Header("Combat")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float fireAngleTolerance = 3f;

    [Header("Projectile")]
    [SerializeField] private ProjectileConfig projectileConfig;

    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float FireAngleTolerance => fireAngleTolerance;
    public ProjectileConfig ProjectileConfig => projectileConfig;
}

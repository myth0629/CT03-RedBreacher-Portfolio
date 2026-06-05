using UnityEngine;

[CreateAssetMenu(menuName = "Myth/Combat/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "enemy_default";
    [SerializeField] private string displayName = "기본 적";

    [Header("Prefab")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float stopDistance = 1.1f;
    [SerializeField] private float contactDamage = 5f;
    [SerializeField] private float contactInterval = 1f;
    [SerializeField] private float experienceReward = 10f;
    [SerializeField] private int creditReward = 10;
    [SerializeField] private int coreCrystalReward;

    public string Id => id;
    public string DisplayName => displayName;
    public GameObject EnemyPrefab => enemyPrefab;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float StopDistance => stopDistance;
    public float ContactDamage => contactDamage;
    public float ContactInterval => contactInterval;
    public float ExperienceReward => experienceReward;
    public int CreditReward => creditReward;
    public int CoreCrystalReward => coreCrystalReward;
}

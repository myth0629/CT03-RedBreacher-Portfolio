using UnityEngine;

public class CombatHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    [Header("Auto Regen")]
    [SerializeField] private bool enableAutoRegen = true;
    [SerializeField] private bool playerOnlyRegen = true;
    [SerializeField] private float regenDelayAfterHit = 3f;
    [SerializeField] private float regenPercentPerSecond = 0.01f;
    [SerializeField] private float regenFlatPerSecond;

    private float currentHealth;
    private float lastDamageTime = float.NegativeInfinity;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        ApplyAutoRegen();
    }

    public void Initialize(float newMaxHealth)
    {
        // SO나 스폰 매니저에서 체력 값을 주입할 때 사용한다.
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        // 체력 감소 후 사망 여부를 즉시 판정한다.
        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void ApplyAutoRegen()
    {
        if (!enableAutoRegen || isDead || currentHealth >= maxHealth)
        {
            return;
        }

        if (playerOnlyRegen && GetComponent<PlayerController>() == null)
        {
            return;
        }

        if (Time.time < lastDamageTime + Mathf.Max(0f, regenDelayAfterHit))
        {
            return;
        }

        float regenPerSecond = maxHealth * Mathf.Max(0f, regenPercentPerSecond) + Mathf.Max(0f, regenFlatPerSecond);
        if (regenPerSecond <= 0f)
        {
            return;
        }

        // 피격 후 일정 시간이 지나면 최대 체력 기준 비율 회복을 적용한다.
        currentHealth = Mathf.Min(maxHealth, currentHealth + regenPerSecond * Time.deltaTime);
    }

    private void Die()
    {
        isDead = true;

        if (GetComponent<PlayerController>() != null)
        {
            Debug.Log($"{name} 사망: 플레이어 자동 전투를 중지합니다.");
            return;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}

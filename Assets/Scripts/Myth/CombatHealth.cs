using UnityEngine;

public class CombatHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
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
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth <= 0f)
        {
            Die();
        }
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

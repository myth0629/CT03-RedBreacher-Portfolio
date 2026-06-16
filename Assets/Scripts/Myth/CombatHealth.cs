using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("Damage Number")]
    [SerializeField] private bool showDamageNumber = true;
    [SerializeField] private TMP_FontAsset damageNumberFont;
    [SerializeField] private Color enemyDamageColor = Color.white;
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color criticalDamageColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private float damageNumberFontSize = 4f;
    [SerializeField] private float criticalSizeMultiplier = 1.35f;
    [SerializeField] private float damageNumberDuration = 0.8f;
    [SerializeField] private float damageNumberRiseDistance = 0.8f;
    [SerializeField] private float damageNumberRandomSpread = 0.25f;
    [SerializeField] private Vector3 damageNumberOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private int damageNumberSortingOrder = 50;

    private float currentHealth;
    private float lastDamageTime = float.NegativeInfinity;
    private float invulnerableUntil;
    private bool debugInvulnerable;
    private bool isDead;
    private bool deathRewardClaimed;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => debugInvulnerable || Time.time < invulnerableUntil;
    public bool IsDebugInvulnerable => debugInvulnerable;
    public event System.Action<float> OnDamageBlockedByInvulnerability;

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
        deathRewardClaimed = false;
        invulnerableUntil = 0f;
    }

    public void SetMaxHealth(float newMaxHealth, bool preserveCurrentRatio)
    {
        float previousMaxHealth = Mathf.Max(1f, maxHealth);
        float healthRatio = Mathf.Clamp01(currentHealth / previousMaxHealth);
        maxHealth = Mathf.Max(1f, newMaxHealth);

        // 장비 교체 시 회복 악용을 막기 위해 현재 체력 비율을 유지한다.
        currentHealth = preserveCurrentRatio
            ? maxHealth * healthRatio
            : Mathf.Min(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        if (debugInvulnerable)
        {
            return;
        }

        if (Time.time < invulnerableUntil)
        {
            OnDamageBlockedByInvulnerability?.Invoke(damage);
            return;
        }

        // 체력 감소 후 사망 여부를 즉시 판정한다.
        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // 데미지 숫자는 부수 연출이므로, 표시 실패가 데미지/사망 처리나 공격자 로직을 깨지 않게 격리한다.
        // 막타도 남은 체력이 아닌 공격이 계산한 원래 피해량을 표시한다.
        try
        {
            ShowDamageNumber(damage, isCritical);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void SetTemporaryInvulnerability(float duration)
    {
        // 회피 중 연속 피격을 막기 위해 기존 무적 시간보다 길 때만 갱신한다.
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + Mathf.Max(0f, duration));
    }

    public void SetDebugInvulnerable(bool enabled)
    {
        // 디버그 무적은 회피 무적 이벤트와 분리해 퍼펙트 회피가 오발동하지 않게 한다.
        debugInvulnerable = enabled;
    }

    private void ShowDamageNumber(float damage, bool isCritical)
    {
        if (!showDamageNumber || damage <= 0f)
        {
            return;
        }

        bool isPlayer = GetComponent<PlayerController>() != null;
        Color color = isCritical
            ? criticalDamageColor
            : isPlayer ? playerDamageColor : enemyDamageColor;
        DamageNumberVisual.Play(
            damage,
            isCritical,
            transform.position + damageNumberOffset,
            damageNumberFont,
            color,
            damageNumberFontSize,
            criticalSizeMultiplier,
            damageNumberDuration,
            damageNumberRiseDistance,
            damageNumberRandomSpread,
            damageNumberSortingOrder);
    }

    public bool TryClaimDeathReward()
    {
        if (!isDead || deathRewardClaimed)
        {
            return false;
        }

        // 같은 적에게 여러 투사체가 겹쳐도 처치 보상은 한 번만 지급한다.
        deathRewardClaimed = true;
        return true;
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

        // 적 사망 연출은 제거 직전에 월드 이펙트로 분리해 재생한다.
        GetComponent<EnemyController>()?.PlayDeathExplosionEffect();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}

public class DamageNumberVisual : MonoBehaviour
{
    private static readonly Queue<DamageNumberVisual> Pool = new Queue<DamageNumberVisual>();
    private static Transform poolRoot;

    private TextMeshPro text;
    private Coroutine animationRoutine;

    public static void Play(
        float damage,
        bool isCritical,
        Vector3 position,
        TMP_FontAsset font,
        Color color,
        float fontSize,
        float criticalSizeMultiplier,
        float duration,
        float riseDistance,
        float randomSpread,
        int sortingOrder)
    {
        DamageNumberVisual visual = Get();
        visual.Begin(
            damage,
            isCritical,
            position,
            font,
            color,
            fontSize,
            criticalSizeMultiplier,
            duration,
            riseDistance,
            randomSpread,
            sortingOrder);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPool()
    {
        // 도메인 리로드 비활성 환경에서 이전 세션의 (파괴된) 풀 항목이 남아 터지는 것을 막는다.
        Pool.Clear();
        poolRoot = null;
    }

    private static DamageNumberVisual Get()
    {
        EnsurePoolRoot();

        // 풀에 남은 항목이 외부에서 파괴됐을 수 있으므로 살아있는 인스턴스를 만날 때까지 건너뛴다.
        DamageNumberVisual visual = null;
        while (Pool.Count > 0)
        {
            DamageNumberVisual candidate = Pool.Dequeue();
            if (candidate != null)
            {
                visual = candidate;
                break;
            }
        }

        if (visual == null)
        {
            visual = Create();
        }

        visual.transform.SetParent(poolRoot, false);
        visual.gameObject.SetActive(true);
        return visual;
    }

    private static DamageNumberVisual Create()
    {
        GameObject visualObject = new GameObject("Damage Number");
        visualObject.transform.SetParent(poolRoot, false);
        DamageNumberVisual visual = visualObject.AddComponent<DamageNumberVisual>();
        visual.text = visualObject.AddComponent<TextMeshPro>();
        visual.text.alignment = TextAlignmentOptions.Center;
        visual.text.textWrappingMode = TextWrappingModes.NoWrap;
        visualObject.SetActive(false);
        return visual;
    }

    private static void EnsurePoolRoot()
    {
        if (poolRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("DamageNumberPool");
        DontDestroyOnLoad(rootObject);
        poolRoot = rootObject.transform;
    }

    private void Begin(
        float damage,
        bool isCritical,
        Vector3 position,
        TMP_FontAsset font,
        Color color,
        float fontSize,
        float criticalSizeMultiplier,
        float duration,
        float riseDistance,
        float randomSpread,
        int sortingOrder)
    {
        StopAnimation();
        text ??= GetComponent<TextMeshPro>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.text = Mathf.Max(1, Mathf.RoundToInt(damage)).ToString();
        text.color = color;
        text.fontSize = Mathf.Max(0.1f, fontSize)
            * (isCritical ? Mathf.Max(1f, criticalSizeMultiplier) : 1f);
        text.renderer.sortingOrder = sortingOrder;

        Vector2 spread = Random.insideUnitCircle * Mathf.Max(0f, randomSpread);
        transform.position = position + new Vector3(spread.x, 0f, spread.y);
        FaceCamera();
        transform.localScale = Vector3.one;
        animationRoutine = StartCoroutine(Animate(
            Mathf.Max(0.05f, duration),
            Mathf.Max(0f, riseDistance)));
    }

    private IEnumerator Animate(float duration, float riseDistance)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * riseDistance;
        Color startColor = text.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, endPosition, progress);
            FaceCamera();
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
            yield return null;
        }

        Release();
    }

    private void FaceCamera()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            transform.rotation = activeCamera.transform.rotation;
        }
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private void Release()
    {
        StopAnimation();
        gameObject.SetActive(false);
        transform.SetParent(poolRoot, false);
        Pool.Enqueue(this);
    }
}

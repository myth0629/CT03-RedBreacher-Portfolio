using UnityEngine;
using UnityEngine.Events;

public class TraitPointFacility : MonoBehaviour
{
    private const string AttackPointKey = "TraitPointFacility.AttackPoints";
    private const string MaxHealthPointKey = "TraitPointFacility.MaxHealthPoints";
    private const string CritChancePointKey = "TraitPointFacility.CritChancePoints";
    private const string CritMultiplierPointKey = "TraitPointFacility.CritMultiplierPoints";

    public enum TraitStat
    {
        AttackDamage,
        MaxHealth,
        CritChance,
        CritMultiplier
    }

    [Header("Source")]
    [SerializeField] private PlayerProgression playerProgression;

    [Header("Bonus Per Point")]
    [SerializeField] private float attackDamagePercentPerPoint = 2f;
    [SerializeField] private float maxHealthPercentPerPoint = 3f;
    [SerializeField] private float critChancePerPoint = 0.01f;
    [SerializeField] private float critMultiplierPercentPerPoint = 5f;

    [Header("Allocated Points")]
    [SerializeField] private int attackDamagePoints;
    [SerializeField] private int maxHealthPoints;
    [SerializeField] private int critChancePoints;
    [SerializeField] private int critMultiplierPoints;
    [SerializeField] private bool saveToPlayerPrefs = true;

    [Header("Events")]
    public UnityEvent OnTraitsChanged = new UnityEvent();

    public PlayerProgression PlayerProgression => ResolvePlayerProgression();
    public int AvailableTraitPoints => PlayerProgression != null ? PlayerProgression.StatPoints : 0;
    public int AttackDamagePoints => attackDamagePoints;
    public int MaxHealthPoints => maxHealthPoints;
    public int CritChancePoints => critChancePoints;
    public int CritMultiplierPoints => critMultiplierPoints;
    public float AttackDamageMultiplierBonus => attackDamagePoints * attackDamagePercentPerPoint * 0.01f;
    public float MaxHealthMultiplierBonus => maxHealthPoints * maxHealthPercentPerPoint * 0.01f;
    public float CritChanceBonus => critChancePoints * critChancePerPoint;
    public float CritMultiplierBonus => critMultiplierPoints * critMultiplierPercentPerPoint * 0.01f;

    private void Awake()
    {
        Load();
        NormalizeValues();
    }

    public bool TryInvest(TraitStat stat)
    {
        return TryInvest(stat, 1);
    }

    public bool TryInvest(TraitStat stat, int points)
    {
        points = Mathf.Max(0, points);
        if (points == 0)
        {
            return true;
        }

        PlayerProgression progression = ResolvePlayerProgression();
        if (progression == null || !progression.TrySpendStatPoints(points))
        {
            return false;
        }

        AddAllocatedPoints(stat, points);
        Save();
        OnTraitsChanged.Invoke();
        return true;
    }

    public void ResetAllocatedPoints()
    {
        int totalAllocatedPoints = attackDamagePoints + maxHealthPoints + critChancePoints + critMultiplierPoints;
        if (totalAllocatedPoints <= 0)
        {
            return;
        }

        attackDamagePoints = 0;
        maxHealthPoints = 0;
        critChancePoints = 0;
        critMultiplierPoints = 0;
        ResolvePlayerProgression()?.AddStatPoints(totalAllocatedPoints);
        Save();
        OnTraitsChanged.Invoke();
    }

    public int GetAllocatedPoints(TraitStat stat)
    {
        return stat switch
        {
            TraitStat.AttackDamage => attackDamagePoints,
            TraitStat.MaxHealth => maxHealthPoints,
            TraitStat.CritChance => critChancePoints,
            TraitStat.CritMultiplier => critMultiplierPoints,
            _ => 0
        };
    }

    public float GetBonusValue(TraitStat stat)
    {
        return stat switch
        {
            TraitStat.AttackDamage => AttackDamageMultiplierBonus,
            TraitStat.MaxHealth => MaxHealthMultiplierBonus,
            TraitStat.CritChance => CritChanceBonus,
            TraitStat.CritMultiplier => CritMultiplierBonus,
            _ => 0f
        };
    }

    public static string GetStatDisplayName(TraitStat stat)
    {
        return stat switch
        {
            TraitStat.AttackDamage => "Attack",
            TraitStat.MaxHealth => "Max Health",
            TraitStat.CritChance => "Crit Chance",
            TraitStat.CritMultiplier => "Crit Multiplier",
            _ => stat.ToString()
        };
    }

    public string BuildSummary()
    {
        return $"Attack +{AttackDamageMultiplierBonus * 100f:0.#}%\n"
            + $"Max Health +{MaxHealthMultiplierBonus * 100f:0.#}%\n"
            + $"Crit Chance +{CritChanceBonus * 100f:0.#}%\n"
            + $"Crit Multiplier +{CritMultiplierBonus * 100f:0.#}%";
    }

    private void AddAllocatedPoints(TraitStat stat, int points)
    {
        switch (stat)
        {
            case TraitStat.AttackDamage:
                attackDamagePoints += points;
                break;
            case TraitStat.MaxHealth:
                maxHealthPoints += points;
                break;
            case TraitStat.CritChance:
                critChancePoints += points;
                break;
            case TraitStat.CritMultiplier:
                critMultiplierPoints += points;
                break;
        }

        NormalizeValues();
    }

    private PlayerProgression ResolvePlayerProgression()
    {
        if (playerProgression != null)
        {
            return playerProgression;
        }

        if (BaseCampManager.Instance != null)
        {
            playerProgression = BaseCampManager.Instance.PlayerProgression;
        }

        playerProgression ??= FindFirstObjectByType<PlayerProgression>();
        return playerProgression;
    }

    private void NormalizeValues()
    {
        attackDamagePoints = Mathf.Max(0, attackDamagePoints);
        maxHealthPoints = Mathf.Max(0, maxHealthPoints);
        critChancePoints = Mathf.Max(0, critChancePoints);
        critMultiplierPoints = Mathf.Max(0, critMultiplierPoints);
        attackDamagePercentPerPoint = Mathf.Max(0f, attackDamagePercentPerPoint);
        maxHealthPercentPerPoint = Mathf.Max(0f, maxHealthPercentPerPoint);
        critChancePerPoint = Mathf.Max(0f, critChancePerPoint);
        critMultiplierPercentPerPoint = Mathf.Max(0f, critMultiplierPercentPerPoint);
    }

    private void Load()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        attackDamagePoints = Mathf.Max(0, PlayerPrefs.GetInt(AttackPointKey, attackDamagePoints));
        maxHealthPoints = Mathf.Max(0, PlayerPrefs.GetInt(MaxHealthPointKey, maxHealthPoints));
        critChancePoints = Mathf.Max(0, PlayerPrefs.GetInt(CritChancePointKey, critChancePoints));
        critMultiplierPoints = Mathf.Max(0, PlayerPrefs.GetInt(CritMultiplierPointKey, critMultiplierPoints));
    }

    private void Save()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetInt(AttackPointKey, attackDamagePoints);
        PlayerPrefs.SetInt(MaxHealthPointKey, maxHealthPoints);
        PlayerPrefs.SetInt(CritChancePointKey, critChancePoints);
        PlayerPrefs.SetInt(CritMultiplierPointKey, critMultiplierPoints);
        PlayerPrefs.Save();
    }

    private void OnValidate()
    {
        NormalizeValues();
    }
}

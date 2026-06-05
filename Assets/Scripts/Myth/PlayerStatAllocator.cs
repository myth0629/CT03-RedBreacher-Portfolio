using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerProgression))]
public class PlayerStatAllocator : MonoBehaviour
{
    private const string AttackLevelKey = "PlayerStatAllocator.AttackLevel";
    private const string HealthLevelKey = "PlayerStatAllocator.HealthLevel";
    private const string CritChanceLevelKey = "PlayerStatAllocator.CritChanceLevel";
    private const string CritMultiplierLevelKey = "PlayerStatAllocator.CritMultiplierLevel";

    [Header("Attack")]
    [SerializeField] private float attackPercentPerLevel = 0.02f;
    [SerializeField] private int maxAttackLevel = 100;

    [Header("Health")]
    [SerializeField] private float healthPercentPerLevel = 0.03f;
    [SerializeField] private int maxHealthLevel = 100;

    [Header("Critical")]
    [SerializeField] private float critChancePerLevel = 0.01f;
    [SerializeField] private float maxCritChance = 0.5f;
    [SerializeField] private float critMultiplierPerLevel = 0.05f;
    [SerializeField] private float maxCritMultiplier = 3f;

    [Header("Save")]
    [SerializeField] private bool saveToPlayerPrefs = true;

    [SerializeField] private int attackLevel;
    [SerializeField] private int healthLevel;
    [SerializeField] private int critChanceLevel;
    [SerializeField] private int critMultiplierLevel;

    private PlayerProgression progression;
    private PlayerController playerController;

    public int AttackLevel => attackLevel;
    public int HealthLevel => healthLevel;
    public int CritChanceLevel => critChanceLevel;
    public int CritMultiplierLevel => critMultiplierLevel;
    public int AttackDisplayLevel => attackLevel + 1;
    public int HealthDisplayLevel => healthLevel + 1;
    public int CritChanceDisplayLevel => critChanceLevel + 1;
    public int CritMultiplierDisplayLevel => critMultiplierLevel + 1;
    public float AttackPercentPerLevel => Mathf.Max(0f, attackPercentPerLevel);
    public float HealthPercentPerLevel => Mathf.Max(0f, healthPercentPerLevel);
    public float CritChancePerLevel => Mathf.Max(0f, critChancePerLevel);
    public float CritMultiplierPerLevel => Mathf.Max(0f, critMultiplierPerLevel);
    public float AttackBonusPercent => AttackDisplayLevel * AttackPercentPerLevel;
    public float HealthBonusPercent => HealthDisplayLevel * HealthPercentPerLevel;
    public float CritChanceBonus => CritChanceDisplayLevel * CritChancePerLevel;
    public float CritMultiplierBonus => CritMultiplierDisplayLevel * CritMultiplierPerLevel;
    public float AttackMultiplier => 1f + AttackBonusPercent;
    public float HealthMultiplier => 1f + HealthBonusPercent;
    public float MaxCritChance => Mathf.Clamp01(maxCritChance);
    public float MaxCritMultiplier => Mathf.Max(1f, maxCritMultiplier);
    public bool CanUpgradeAttack => attackLevel < Mathf.Max(0, maxAttackLevel);
    public bool CanUpgradeHealth => healthLevel < Mathf.Max(0, maxHealthLevel);
    public bool CanUpgradeCritChance => playerController == null || playerController.CritChance < MaxCritChance;
    public bool CanUpgradeCritMultiplier => playerController == null || playerController.CritMultiplier < MaxCritMultiplier;

    private void Awake()
    {
        progression = GetComponent<PlayerProgression>();
        playerController = GetComponent<PlayerController>();
        Load();
    }

    public void UpgradeAttack()
    {
        TryUpgradeAttack();
    }

    public void UpgradeHealth()
    {
        TryUpgradeHealth();
    }

    public void UpgradeCritChance()
    {
        TryUpgradeCritChance();
    }

    public void UpgradeCritMultiplier()
    {
        TryUpgradeCritMultiplier();
    }

    public bool TryUpgradeAttack()
    {
        return TryUpgrade(ref attackLevel, Mathf.Max(0, maxAttackLevel));
    }

    public bool TryUpgradeHealth()
    {
        return TryUpgrade(ref healthLevel, Mathf.Max(0, maxHealthLevel));
    }

    public bool TryUpgradeCritChance()
    {
        if (playerController != null && playerController.CritChance >= MaxCritChance)
        {
            return false;
        }

        return TryUpgrade(ref critChanceLevel, GetCritChanceMaxLevel());
    }

    public bool TryUpgradeCritMultiplier()
    {
        if (playerController != null && playerController.CritMultiplier >= MaxCritMultiplier)
        {
            return false;
        }

        return TryUpgrade(ref critMultiplierLevel, GetCritMultiplierMaxLevel());
    }

    public float ApplyCritChance(float baseChance)
    {
        return Mathf.Min(MaxCritChance, Mathf.Clamp01(baseChance) + CritChanceBonus);
    }

    public float ApplyCritMultiplier(float baseMultiplier)
    {
        return Mathf.Min(MaxCritMultiplier, Mathf.Max(1f, baseMultiplier) + CritMultiplierBonus);
    }

    public void ResetAllocations()
    {
        // 디버그 진행도 초기화 시 투자한 강화 레벨도 함께 제거한다.
        attackLevel = 0;
        healthLevel = 0;
        critChanceLevel = 0;
        critMultiplierLevel = 0;
        Save();
    }

    private bool TryUpgrade(ref int currentLevel, int maxLevel)
    {
        if (progression == null)
        {
            progression = GetComponent<PlayerProgression>();
        }

        if (currentLevel >= maxLevel || progression == null || !progression.TrySpendStatPoint())
        {
            return false;
        }

        currentLevel++;
        Save();
        return true;
    }

    private int GetCritChanceMaxLevel()
    {
        float amountPerLevel = Mathf.Max(0.0001f, critChancePerLevel);
        return Mathf.Max(0, Mathf.CeilToInt(MaxCritChance / amountPerLevel));
    }

    private int GetCritMultiplierMaxLevel()
    {
        float amountPerLevel = Mathf.Max(0.0001f, critMultiplierPerLevel);
        return Mathf.Max(0, Mathf.CeilToInt((MaxCritMultiplier - 1f) / amountPerLevel));
    }

    private void Load()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        attackLevel = Mathf.Clamp(PlayerPrefs.GetInt(AttackLevelKey, attackLevel), 0, Mathf.Max(0, maxAttackLevel));
        healthLevel = Mathf.Clamp(PlayerPrefs.GetInt(HealthLevelKey, healthLevel), 0, Mathf.Max(0, maxHealthLevel));
        critChanceLevel = Mathf.Clamp(PlayerPrefs.GetInt(CritChanceLevelKey, critChanceLevel), 0, GetCritChanceMaxLevel());
        critMultiplierLevel = Mathf.Clamp(PlayerPrefs.GetInt(CritMultiplierLevelKey, critMultiplierLevel), 0, GetCritMultiplierMaxLevel());
    }

    private void Save()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetInt(AttackLevelKey, attackLevel);
        PlayerPrefs.SetInt(HealthLevelKey, healthLevel);
        PlayerPrefs.SetInt(CritChanceLevelKey, critChanceLevel);
        PlayerPrefs.SetInt(CritMultiplierLevelKey, critMultiplierLevel);
        PlayerPrefs.Save();
    }
}

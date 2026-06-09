using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum GachaCategory
{
    Weapon,
    Skill
}

[Serializable]
public class GachaDrawResult
{
    public GachaCategory category;
    public ProjectileConfig weaponConfig;
    public PlayerSkillConfig skillConfig;
    public InventoryFacility.CollectionGrantResult grantResult;

    public string DisplayName => category == GachaCategory.Weapon
        ? weaponConfig != null ? weaponConfig.DisplayName : string.Empty
        : skillConfig != null ? skillConfig.DisplayName : string.Empty;
    public Sprite Icon => category == GachaCategory.Weapon
        ? weaponConfig != null ? weaponConfig.Icon : null
        : skillConfig != null ? skillConfig.Icon : null;
}

public class WeaponGachaFacility : MonoBehaviour
{
    [Serializable]
    public class WeaponGachaEntry
    {
        public ProjectileConfig weaponConfig;
        [Min(0f)] public float weight = 1f;
        public bool enabled = true;
    }

    [Serializable]
    public class SkillGachaEntry
    {
        public PlayerSkillConfig skillConfig;
        [Min(0f)] public float weight = 1f;
        public bool enabled = true;
    }

    [Header("Gacha Pools")]
    [SerializeField] private GachaPoolConfig weaponPool;
    [SerializeField] private GachaPoolConfig skillPool;

    [Header("Core Crystal Cost")]
    [SerializeField] private int weaponDrawCost = 10;
    [SerializeField] private int skillDrawCost = 10;
    [SerializeField] private int multiDrawCount = 10;

    [Header("Legacy Weapon Table")]
    [SerializeField] private List<WeaponGachaEntry> drawTable = new List<WeaponGachaEntry>();

    [Header("References")]
    [SerializeField] private InventoryFacility inventory;
    [SerializeField] private PlayerCurrencyWallet currencyWallet;

    [Header("Events")]
    public UnityEvent<ProjectileConfig> OnWeaponDrawn = new UnityEvent<ProjectileConfig>();
    public UnityEvent<PlayerSkillConfig> OnSkillDrawn = new UnityEvent<PlayerSkillConfig>();
    public UnityEvent<GachaDrawResult> OnResultDrawn = new UnityEvent<GachaDrawResult>();
    public UnityEvent OnDrawCompleted = new UnityEvent();

    private readonly List<ProjectileConfig> lastDrawResults = new List<ProjectileConfig>();
    private readonly List<GachaDrawResult> lastResults = new List<GachaDrawResult>();

    public int DrawCost => GetSingleDrawCost(GachaCategory.Weapon);
    public int MultiDrawCount => Mathf.Max(1, multiDrawCount);
    public IReadOnlyList<WeaponGachaEntry> DrawTable => GetWeaponEntries();
    public IReadOnlyList<ProjectileConfig> LastDrawResults => lastDrawResults;
    public IReadOnlyList<GachaDrawResult> LastResults => lastResults;

    private void Awake()
    {
        ResolveReferences();
    }

    public bool CanDraw(GachaCategory category, int count = 1)
    {
        ResolveReferences();
        return inventory != null
            && currencyWallet != null
            && GetValidDrawEntryCount(category) > 0
            && currencyWallet.CanSpend(CurrencyType.CoreCrystals, GetDrawCost(category, count));
    }

    public int GetDrawCost(GachaCategory category, int count)
    {
        return GetSingleDrawCost(category) * Mathf.Max(1, count);
    }

    public bool TryDraw(GachaCategory category, int count = 1)
    {
        count = Mathf.Max(1, count);
        ResolveReferences();
        if (inventory == null || currencyWallet == null)
        {
            return false;
        }

        List<UnityEngine.Object> pickedConfigs = PickConfigs(category, count);
        if (pickedConfigs.Count != count)
        {
            return false;
        }

        int cost = GetDrawCost(category, count);
        if (!currencyWallet.TrySpend(CurrencyType.CoreCrystals, cost))
        {
            return false;
        }

        GrantPickedConfigs(category, pickedConfigs);
        return true;
    }

    public void Draw(GachaCategory category, int count = 1)
    {
        count = Mathf.Max(1, count);
        ResolveReferences();
        if (inventory == null)
        {
            return;
        }

        List<UnityEngine.Object> pickedConfigs = PickConfigs(category, count);
        if (pickedConfigs.Count == count)
        {
            GrantPickedConfigs(category, pickedConfigs);
        }
    }

    public IReadOnlyList<WeaponGachaEntry> GetWeaponEntries()
    {
        return weaponPool != null && weaponPool.Category == GachaCategory.Weapon
            ? weaponPool.WeaponEntries
            : drawTable;
    }

    public IReadOnlyList<SkillGachaEntry> GetSkillEntries()
    {
        return skillPool != null && skillPool.Category == GachaCategory.Skill
            ? skillPool.SkillEntries
            : Array.Empty<SkillGachaEntry>();
    }

    // 기존 무기 전용 호출부 호환용 API다.
    public bool CanDraw(int coreCrystals, int count = 1)
    {
        return ResolveReferences() != null
            && GetValidDrawEntryCount(GachaCategory.Weapon) > 0
            && coreCrystals >= GetDrawCost(count);
    }

    public int GetDrawCost(int count)
    {
        return GetDrawCost(GachaCategory.Weapon, count);
    }

    public bool TryDraw(ref int availableCoreCrystals, int count = 1)
    {
        count = Mathf.Max(1, count);
        if (!CanDraw(availableCoreCrystals, count))
        {
            return false;
        }

        List<UnityEngine.Object> pickedConfigs = PickConfigs(GachaCategory.Weapon, count);
        if (pickedConfigs.Count != count)
        {
            return false;
        }

        availableCoreCrystals -= GetDrawCost(count);
        GrantPickedConfigs(GachaCategory.Weapon, pickedConfigs);
        return true;
    }

    public void Draw(int count = 1)
    {
        Draw(GachaCategory.Weapon, count);
    }

    private int GetSingleDrawCost(GachaCategory category)
    {
        return category == GachaCategory.Weapon
            ? Mathf.Max(0, weaponDrawCost)
            : Mathf.Max(0, skillDrawCost);
    }

    private List<UnityEngine.Object> PickConfigs(GachaCategory category, int count)
    {
        List<UnityEngine.Object> results = new List<UnityEngine.Object>(count);
        for (int i = 0; i < count; i++)
        {
            UnityEngine.Object config = category == GachaCategory.Weapon
                ? PickWeapon()
                : PickSkill();
            if (config == null)
            {
                results.Clear();
                return results;
            }

            results.Add(config);
        }

        return results;
    }

    private void GrantPickedConfigs(GachaCategory category, IReadOnlyList<UnityEngine.Object> configs)
    {
        lastResults.Clear();
        lastDrawResults.Clear();

        for (int i = 0; i < configs.Count; i++)
        {
            GachaDrawResult result = category == GachaCategory.Weapon
                ? GrantWeapon(configs[i] as ProjectileConfig)
                : GrantSkill(configs[i] as PlayerSkillConfig);
            if (result == null)
            {
                continue;
            }

            lastResults.Add(result);
            OnResultDrawn.Invoke(result);
        }

        OnDrawCompleted.Invoke();
    }

    private GachaDrawResult GrantWeapon(ProjectileConfig weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        InventoryFacility.CollectionGrantResult grantResult = inventory.GrantWeapon(weapon);
        if (!grantResult.success)
        {
            return null;
        }

        lastDrawResults.Add(weapon);
        OnWeaponDrawn.Invoke(weapon);
        return new GachaDrawResult
        {
            category = GachaCategory.Weapon,
            weaponConfig = weapon,
            grantResult = grantResult
        };
    }

    private GachaDrawResult GrantSkill(PlayerSkillConfig skill)
    {
        if (skill == null)
        {
            return null;
        }

        InventoryFacility.CollectionGrantResult grantResult = inventory.GrantSkill(skill);
        if (!grantResult.success)
        {
            return null;
        }

        OnSkillDrawn.Invoke(skill);
        return new GachaDrawResult
        {
            category = GachaCategory.Skill,
            skillConfig = skill,
            grantResult = grantResult
        };
    }

    private ProjectileConfig PickWeapon()
    {
        IReadOnlyList<WeaponGachaEntry> entries = GetWeaponEntries();
        float totalWeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaEntry entry = entries[i];
            if (IsValid(entry))
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaEntry entry = entries[i];
            if (!IsValid(entry))
            {
                continue;
            }

            roll -= entry.weight;
            if (roll <= 0f)
            {
                return entry.weaponConfig;
            }
        }

        return null;
    }

    private PlayerSkillConfig PickSkill()
    {
        IReadOnlyList<SkillGachaEntry> entries = GetSkillEntries();
        float totalWeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            SkillGachaEntry entry = entries[i];
            if (IsValid(entry))
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < entries.Count; i++)
        {
            SkillGachaEntry entry = entries[i];
            if (!IsValid(entry))
            {
                continue;
            }

            roll -= entry.weight;
            if (roll <= 0f)
            {
                return entry.skillConfig;
            }
        }

        return null;
    }

    private int GetValidDrawEntryCount(GachaCategory category)
    {
        int count = 0;
        if (category == GachaCategory.Weapon)
        {
            IReadOnlyList<WeaponGachaEntry> entries = GetWeaponEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                if (IsValid(entries[i]))
                {
                    count++;
                }
            }
        }
        else
        {
            IReadOnlyList<SkillGachaEntry> entries = GetSkillEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                if (IsValid(entries[i]))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool IsValid(WeaponGachaEntry entry)
    {
        return entry != null && entry.enabled && entry.weaponConfig != null && entry.weight > 0f;
    }

    private static bool IsValid(SkillGachaEntry entry)
    {
        return entry != null && entry.enabled && entry.skillConfig != null && entry.weight > 0f;
    }

    private InventoryFacility ResolveReferences()
    {
        inventory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : FindFirstObjectByType<InventoryFacility>(FindObjectsInactive.Include);
        currencyWallet ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.CurrencyWallet
            : FindFirstObjectByType<PlayerCurrencyWallet>(FindObjectsInactive.Include);
        return inventory;
    }

    private void OnValidate()
    {
        weaponDrawCost = Mathf.Max(0, weaponDrawCost);
        skillDrawCost = Mathf.Max(0, skillDrawCost);
        multiDrawCount = Mathf.Max(1, multiDrawCount);
        drawTable ??= new List<WeaponGachaEntry>();

        for (int i = 0; i < drawTable.Count; i++)
        {
            WeaponGachaEntry entry = drawTable[i];
            if (entry != null)
            {
                entry.weight = Mathf.Max(0f, entry.weight);
            }
        }
    }
}

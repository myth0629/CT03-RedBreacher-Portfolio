using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InventoryFacility : MonoBehaviour
{
    [System.Serializable]
    public class WeaponStack
    {
        public ProjectileConfig weaponConfig;
        [Min(1)] public int quantity = 1;

        public int DuplicateCount => Mathf.Max(0, quantity - 1);
    }

    [Header("Weapons")]
    [SerializeField] private List<ProjectileConfig> weaponConfigs = new List<ProjectileConfig>();
    [SerializeField] private List<WeaponStack> weaponStacks = new List<WeaponStack>();

    [Header("Units")]
    [SerializeField] private List<PlayerUnitConfig> unitConfigs = new List<PlayerUnitConfig>();

    [Header("Events")]
    public UnityEvent OnInventoryChanged = new UnityEvent();

    public IReadOnlyList<ProjectileConfig> WeaponConfigs => weaponConfigs;
    public IReadOnlyList<WeaponStack> WeaponStacks => weaponStacks;
    public IReadOnlyList<PlayerUnitConfig> UnitConfigs => unitConfigs;

    public bool ContainsWeapon(ProjectileConfig weaponConfig)
    {
        return weaponConfig != null && weaponConfigs.Contains(weaponConfig);
    }

    public bool ContainsUnit(PlayerUnitConfig unitConfig)
    {
        return unitConfig != null && unitConfigs.Contains(unitConfig);
    }

    public bool AddWeapon(ProjectileConfig weaponConfig)
    {
        return AddWeapon(weaponConfig, 1);
    }

    public bool AddWeapon(ProjectileConfig weaponConfig, int quantity)
    {
        if (weaponConfig == null || quantity <= 0)
        {
            return false;
        }

        WeaponStack stack = GetOrCreateWeaponStack(weaponConfig);
        stack.quantity += quantity;
        SyncWeaponConfigsFromStacks();
        OnInventoryChanged.Invoke();
        return true;
    }

    public bool AddUnit(PlayerUnitConfig unitConfig)
    {
        if (unitConfig == null || unitConfigs.Contains(unitConfig))
        {
            return false;
        }

        unitConfigs.Add(unitConfig);
        OnInventoryChanged.Invoke();
        return true;
    }

    public bool RemoveWeapon(ProjectileConfig weaponConfig)
    {
        return RemoveWeapon(weaponConfig, 1);
    }

    public bool RemoveWeapon(ProjectileConfig weaponConfig, int quantity)
    {
        if (weaponConfig == null || quantity <= 0)
        {
            return false;
        }

        WeaponStack stack = FindWeaponStack(weaponConfig);
        if (stack == null)
        {
            return false;
        }

        stack.quantity -= quantity;
        if (stack.quantity <= 0)
        {
            weaponStacks.Remove(stack);
        }

        SyncWeaponConfigsFromStacks();
        OnInventoryChanged.Invoke();
        return true;
    }

    public int GetWeaponQuantity(ProjectileConfig weaponConfig)
    {
        WeaponStack stack = FindWeaponStack(weaponConfig);
        return stack != null ? Mathf.Max(0, stack.quantity) : 0;
    }

    public int GetWeaponDuplicateCount(ProjectileConfig weaponConfig)
    {
        return Mathf.Max(0, GetWeaponQuantity(weaponConfig) - 1);
    }

    public bool RemoveUnit(PlayerUnitConfig unitConfig)
    {
        if (unitConfig == null || !unitConfigs.Remove(unitConfig))
        {
            return false;
        }

        OnInventoryChanged.Invoke();
        return true;
    }

    private void OnValidate()
    {
        NormalizeWeaponStacks();
        RemoveNullAndDuplicateUnits();
    }

    private WeaponStack FindWeaponStack(ProjectileConfig weaponConfig)
    {
        return weaponStacks.Find(item => item != null && item.weaponConfig == weaponConfig);
    }

    private WeaponStack GetOrCreateWeaponStack(ProjectileConfig weaponConfig)
    {
        WeaponStack stack = FindWeaponStack(weaponConfig);
        if (stack != null)
        {
            return stack;
        }

        stack = new WeaponStack { weaponConfig = weaponConfig, quantity = 0 };
        weaponStacks.Add(stack);
        return stack;
    }

    private void NormalizeWeaponStacks()
    {
        weaponStacks ??= new List<WeaponStack>();

        foreach (ProjectileConfig weaponConfig in weaponConfigs)
        {
            if (weaponConfig == null || FindWeaponStack(weaponConfig) != null)
            {
                continue;
            }

            weaponStacks.Add(new WeaponStack { weaponConfig = weaponConfig, quantity = 1 });
        }

        HashSet<ProjectileConfig> seen = new HashSet<ProjectileConfig>();
        for (int i = weaponStacks.Count - 1; i >= 0; i--)
        {
            WeaponStack stack = weaponStacks[i];
            if (stack == null || stack.weaponConfig == null || !seen.Add(stack.weaponConfig))
            {
                weaponStacks.RemoveAt(i);
                continue;
            }

            stack.quantity = Mathf.Max(1, stack.quantity);
        }

        SyncWeaponConfigsFromStacks();
    }

    private void SyncWeaponConfigsFromStacks()
    {
        weaponConfigs.Clear();
        foreach (WeaponStack stack in weaponStacks)
        {
            if (stack != null && stack.weaponConfig != null && !weaponConfigs.Contains(stack.weaponConfig))
            {
                weaponConfigs.Add(stack.weaponConfig);
            }
        }

        for (int i = weaponConfigs.Count - 1; i >= 0; i--)
        {
            if (weaponConfigs[i] == null)
            {
                weaponConfigs.RemoveAt(i);
            }
        }
    }

    private void RemoveNullAndDuplicateUnits()
    {
        HashSet<PlayerUnitConfig> seen = new HashSet<PlayerUnitConfig>();
        for (int i = unitConfigs.Count - 1; i >= 0; i--)
        {
            PlayerUnitConfig item = unitConfigs[i];
            if (item == null || !seen.Add(item))
            {
                unitConfigs.RemoveAt(i);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WeaponGachaFacility : MonoBehaviour
{
    [Serializable]
    public class WeaponGachaEntry
    {
        public ProjectileConfig weaponConfig;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Gacha")]
    [SerializeField] private int drawCost = 100;
    [SerializeField] private int multiDrawCount = 10;
    [SerializeField] private List<WeaponGachaEntry> drawTable = new List<WeaponGachaEntry>();

    [Header("Inventory")]
    [SerializeField] private InventoryFacility inventory;

    [Header("Events")]
    public UnityEvent<ProjectileConfig> OnWeaponDrawn = new UnityEvent<ProjectileConfig>();
    public UnityEvent OnDrawCompleted = new UnityEvent();

    private readonly List<ProjectileConfig> lastDrawResults = new List<ProjectileConfig>();

    public int DrawCost => Mathf.Max(0, drawCost);
    public int MultiDrawCount => Mathf.Max(1, multiDrawCount);
    public IReadOnlyList<WeaponGachaEntry> DrawTable => drawTable;
    public IReadOnlyList<ProjectileConfig> LastDrawResults => lastDrawResults;

    private void Awake()
    {
        ResolveReferences();
    }

    public bool CanDraw(int credits, int count = 1)
    {
        return ResolveReferences() != null
            && GetValidDrawEntryCount() > 0
            && credits >= GetDrawCost(count);
    }

    public int GetDrawCost(int count)
    {
        return DrawCost * Mathf.Max(1, count);
    }

    public bool TryDraw(ref int availableCredits, int count = 1)
    {
        count = Mathf.Max(1, count);
        if (!CanDraw(availableCredits, count))
        {
            return false;
        }

        availableCredits -= GetDrawCost(count);
        Draw(count);
        return true;
    }

    public void Draw(int count = 1)
    {
        count = Mathf.Max(1, count);
        InventoryFacility targetInventory = ResolveReferences();
        if (targetInventory == null)
        {
            return;
        }

        lastDrawResults.Clear();
        for (int i = 0; i < count; i++)
        {
            ProjectileConfig weapon = PickWeapon();
            if (weapon == null)
            {
                continue;
            }

            targetInventory.AddWeapon(weapon);
            lastDrawResults.Add(weapon);
            OnWeaponDrawn.Invoke(weapon);
        }

        OnDrawCompleted.Invoke();
    }

    private ProjectileConfig PickWeapon()
    {
        float totalWeight = 0f;
        foreach (WeaponGachaEntry entry in drawTable)
        {
            if (entry != null && entry.weaponConfig != null && entry.weight > 0f)
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (WeaponGachaEntry entry in drawTable)
        {
            if (entry == null || entry.weaponConfig == null || entry.weight <= 0f)
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

    private int GetValidDrawEntryCount()
    {
        int count = 0;
        foreach (WeaponGachaEntry entry in drawTable)
        {
            if (entry != null && entry.weaponConfig != null && entry.weight > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private InventoryFacility ResolveReferences()
    {
        inventory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : FindFirstObjectByType<InventoryFacility>();
        return inventory;
    }

    private void OnValidate()
    {
        drawCost = Mathf.Max(0, drawCost);
        multiDrawCount = Mathf.Max(1, multiDrawCount);

        if (drawTable == null)
        {
            drawTable = new List<WeaponGachaEntry>();
        }

        foreach (WeaponGachaEntry entry in drawTable)
        {
            if (entry != null)
            {
                entry.weight = Mathf.Max(0f, entry.weight);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerEquipmentPartLoadout : MonoBehaviour
{
    private const string ArmorKey = "PlayerEquipmentPartLoadout.Armor";
    private const string EngineKey = "PlayerEquipmentPartLoadout.Engine";
    private const string ChipKey = "PlayerEquipmentPartLoadout.Chip";

    [Header("Source")]
    [SerializeField] private InventoryFacility inventory;
    [SerializeField] private bool saveToPlayerPrefs = true;

    [Header("Equipped Instance IDs")]
    [SerializeField] private string armorInstanceId;
    [SerializeField] private string engineInstanceId;
    [SerializeField] private string chipInstanceId;

    [Header("Events")]
    public UnityEvent OnLoadoutChanged = new UnityEvent();

    public float AttackPercent { get; private set; }
    public float HealthPercent { get; private set; }
    public float AttackSpeedPercent { get; private set; }
    public float CritChanceBonus { get; private set; }
    public float CritDamageBonus { get; private set; }

    private void Awake()
    {
        Load();
    }

    private void Start()
    {
        ResolveInventory();
        ValidateEquippedParts();
        RecalculateStats();
        OnLoadoutChanged.Invoke();
    }

    public bool Equip(EquipmentPartInstance part)
    {
        ResolveInventory();
        if (part == null || inventory == null || inventory.FindEquipmentPart(part.instanceId) == null)
        {
            return false;
        }

        SetEquippedId(part.slot, part.instanceId);
        SaveAndNotify();
        return true;
    }

    public bool Unequip(EquipmentPartSlot slot)
    {
        if (string.IsNullOrEmpty(GetEquippedId(slot)))
        {
            return false;
        }

        SetEquippedId(slot, string.Empty);
        SaveAndNotify();
        return true;
    }

    public bool IsEquipped(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        return armorInstanceId == instanceId || engineInstanceId == instanceId || chipInstanceId == instanceId;
    }

    public EquipmentPartInstance GetEquippedPart(EquipmentPartSlot slot)
    {
        ResolveInventory();
        return inventory != null ? inventory.FindEquipmentPart(GetEquippedId(slot)) : null;
    }

    private void RecalculateStats()
    {
        AttackPercent = 0f;
        HealthPercent = 0f;
        AttackSpeedPercent = 0f;
        CritChanceBonus = 0f;
        CritDamageBonus = 0f;

        ApplyPartStats(GetEquippedPart(EquipmentPartSlot.Armor));
        ApplyPartStats(GetEquippedPart(EquipmentPartSlot.Engine));
        ApplyPartStats(GetEquippedPart(EquipmentPartSlot.Chip));
    }

    private void ApplyPartStats(EquipmentPartInstance part)
    {
        if (part == null)
        {
            return;
        }

        ApplyStat(part.mainStatType, part.mainStatValue);
        if (part.subStats == null)
        {
            return;
        }

        for (int i = 0; i < part.subStats.Count; i++)
        {
            EquipmentSubStat subStat = part.subStats[i];
            if (subStat != null)
            {
                ApplyStat(subStat.statType, subStat.value);
            }
        }
    }

    private void ApplyStat(EquipmentStatType statType, float value)
    {
        value = Mathf.Max(0f, value);
        switch (statType)
        {
            case EquipmentStatType.AttackPercent:
                AttackPercent += value;
                break;
            case EquipmentStatType.HealthPercent:
                HealthPercent += value;
                break;
            case EquipmentStatType.AttackSpeedPercent:
                AttackSpeedPercent += value;
                break;
            case EquipmentStatType.CritChance:
                CritChanceBonus += value;
                break;
            case EquipmentStatType.CritDamage:
                CritDamageBonus += value;
                break;
        }
    }

    private void ValidateEquippedParts()
    {
        if (inventory == null)
        {
            return;
        }

        bool changed = ValidateSlot(EquipmentPartSlot.Armor)
            | ValidateSlot(EquipmentPartSlot.Engine)
            | ValidateSlot(EquipmentPartSlot.Chip);
        if (changed)
        {
            Save();
        }
    }

    private bool ValidateSlot(EquipmentPartSlot slot)
    {
        string instanceId = GetEquippedId(slot);
        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        EquipmentPartInstance part = inventory.FindEquipmentPart(instanceId);
        if (part != null && part.slot == slot)
        {
            return false;
        }

        SetEquippedId(slot, string.Empty);
        return true;
    }

    private void ResolveInventory()
    {
        if (inventory != null)
        {
            return;
        }

        inventory = BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : FindFirstObjectByType<InventoryFacility>();
    }

    private string GetEquippedId(EquipmentPartSlot slot)
    {
        return slot switch
        {
            EquipmentPartSlot.Armor => armorInstanceId,
            EquipmentPartSlot.Engine => engineInstanceId,
            _ => chipInstanceId
        };
    }

    private void SetEquippedId(EquipmentPartSlot slot, string instanceId)
    {
        switch (slot)
        {
            case EquipmentPartSlot.Armor:
                armorInstanceId = instanceId;
                break;
            case EquipmentPartSlot.Engine:
                engineInstanceId = instanceId;
                break;
            case EquipmentPartSlot.Chip:
                chipInstanceId = instanceId;
                break;
        }
    }

    private void Load()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        armorInstanceId = PlayerPrefs.GetString(ArmorKey, armorInstanceId);
        engineInstanceId = PlayerPrefs.GetString(EngineKey, engineInstanceId);
        chipInstanceId = PlayerPrefs.GetString(ChipKey, chipInstanceId);
    }

    private void SaveAndNotify()
    {
        RecalculateStats();
        Save();
        OnLoadoutChanged.Invoke();
    }

    private void Save()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetString(ArmorKey, armorInstanceId ?? string.Empty);
        PlayerPrefs.SetString(EngineKey, engineInstanceId ?? string.Empty);
        PlayerPrefs.SetString(ChipKey, chipInstanceId ?? string.Empty);
        PlayerPrefs.Save();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InventoryFacility : MonoBehaviour
{
    private const string EquipmentPartsKey = "InventoryFacility.EquipmentParts";

    [System.Serializable]
    private class EquipmentPartSaveData
    {
        public List<EquipmentPartInstance> parts = new List<EquipmentPartInstance>();
    }

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

    [Header("Equipment Parts")]
    [SerializeField] private List<EquipmentPartConfig> equipmentPartConfigs = new List<EquipmentPartConfig>();
    [SerializeField] private List<EquipmentPartInstance> equipmentParts = new List<EquipmentPartInstance>();
    [SerializeField] private bool saveEquipmentPartsToPlayerPrefs = true;

    [Header("Equipment Part Drop Visual")]
    [SerializeField] private bool playEquipmentPartDropVisual = true;
    [SerializeField] private Transform equipmentPartCollectTarget;
    [SerializeField] private float partDropPopDuration = 0.25f;
    [SerializeField] private float partDropHoldDuration = 0.35f;
    [SerializeField] private float partDropCollectDuration = 0.45f;
    [SerializeField] private float partDropPopDistance = 0.8f;
    [SerializeField] private float partDropVisualSize = 0.7f;
    [SerializeField] private int partDropSortingOrder = 20;

    [Header("Equipment Part Drop Debug")]
    [SerializeField] private bool forceEquipmentPartDrop;

    [Header("Events")]
    public UnityEvent OnInventoryChanged = new UnityEvent();
    public UnityEvent OnEquipmentPartsChanged = new UnityEvent();

    private bool equipmentPartsInitialized;

    public IReadOnlyList<ProjectileConfig> WeaponConfigs => weaponConfigs;
    public IReadOnlyList<WeaponStack> WeaponStacks => weaponStacks;
    public IReadOnlyList<PlayerUnitConfig> UnitConfigs => unitConfigs;
    public IReadOnlyList<EquipmentPartConfig> EquipmentPartConfigs
    {
        get
        {
            EnsureEquipmentPartsInitialized();
            return equipmentPartConfigs;
        }
    }
    public IReadOnlyList<EquipmentPartInstance> EquipmentParts
    {
        get
        {
            EnsureEquipmentPartsInitialized();
            return equipmentParts;
        }
    }
    public bool ForceEquipmentPartDrop => forceEquipmentPartDrop;

    private void Awake()
    {
        EnsureEquipmentPartsInitialized();
    }

    public static InventoryFacility FindAny()
    {
        // 기지 UI가 비활성 상태여도 파츠 보상 저장소를 찾을 수 있어야 한다.
        return FindFirstObjectByType<InventoryFacility>(FindObjectsInactive.Include);
    }

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

    public bool AddEquipmentPart(EquipmentPartInstance part)
    {
        EnsureEquipmentPartsInitialized();
        if (part == null)
        {
            return false;
        }

        equipmentParts ??= new List<EquipmentPartInstance>();
        if (string.IsNullOrWhiteSpace(part.instanceId))
        {
            part.instanceId = System.Guid.NewGuid().ToString("N");
        }

        if (FindEquipmentPart(part.instanceId) != null)
        {
            return false;
        }

        part.subStats ??= new List<EquipmentSubStat>();
        equipmentParts.Add(part);
        SaveEquipmentParts();
        OnEquipmentPartsChanged.Invoke();
        OnInventoryChanged.Invoke();
        return true;
    }

    public EquipmentPartInstance FindEquipmentPart(string instanceId)
    {
        EnsureEquipmentPartsInitialized();
        if (string.IsNullOrWhiteSpace(instanceId) || equipmentParts == null)
        {
            return null;
        }

        return equipmentParts.Find(part => part != null && part.instanceId == instanceId);
    }

    public EquipmentPartConfig ResolveEquipmentPartConfig(string configId)
    {
        EnsureEquipmentPartsInitialized();
        return equipmentPartConfigs.Find(config => config != null && config.Id == configId);
    }

    public void PlayEquipmentPartDropVisual(
        EquipmentPartConfig config,
        EquipmentPartInstance part,
        Vector3 dropPosition)
    {
        if (!playEquipmentPartDropVisual || part == null)
        {
            return;
        }

        // 보상은 즉시 지급하고 드롭 위치에는 수집 피드백만 풀링해서 보여준다.
        EquipmentPartDropVisual.Play(
            config != null ? config.Icon : null,
            part.rarity,
            dropPosition,
            equipmentPartCollectTarget,
            partDropPopDuration,
            partDropHoldDuration,
            partDropCollectDuration,
            partDropPopDistance,
            partDropVisualSize,
            partDropSortingOrder);
    }

    public bool RemoveEquipmentPart(string instanceId, PlayerEquipmentPartLoadout loadout = null)
    {
        EquipmentPartInstance part = FindEquipmentPart(instanceId);
        loadout ??= FindFirstObjectByType<PlayerEquipmentPartLoadout>();
        if (part == null || (loadout != null && loadout.IsEquipped(instanceId)))
        {
            return false;
        }

        equipmentParts.Remove(part);
        SaveEquipmentParts();
        OnEquipmentPartsChanged.Invoke();
        OnInventoryChanged.Invoke();
        return true;
    }

    public bool TrySellEquipmentPart(
        string instanceId,
        PlayerEquipmentPartLoadout loadout,
        ICurrencyWallet wallet)
    {
        EquipmentPartInstance part = FindEquipmentPart(instanceId);
        if (part == null || wallet == null || (loadout != null && loadout.IsEquipped(instanceId)))
        {
            return false;
        }

        if (!RemoveEquipmentPart(instanceId, loadout))
        {
            return false;
        }

        wallet?.Add(CurrencyType.Credits, Mathf.Max(0, part.salePrice));
        return true;
    }

    private void OnValidate()
    {
        NormalizeWeaponStacks();
        RemoveNullAndDuplicateUnits();
        EnsureEquipmentPartConfigs(false);
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

    private void EnsureEquipmentPartConfigs(bool allowRuntimeDefaults = true)
    {
        equipmentPartConfigs ??= new List<EquipmentPartConfig>();
        equipmentPartConfigs.RemoveAll(config => config == null);

#if UNITY_EDITOR
        if (equipmentPartConfigs.Count == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(
                "t:EquipmentPartConfig",
                new[] { "Assets/SO/Balance/EquipmentParts" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                EquipmentPartConfig config = UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentPartConfig>(path);
                if (config != null && !equipmentPartConfigs.Contains(config))
                {
                    equipmentPartConfigs.Add(config);
                }
            }
        }
#endif

        if (equipmentPartConfigs.Count > 0 || !allowRuntimeDefaults)
        {
            return;
        }

        // 별도 SO 연결 전에도 드롭 테스트가 가능하도록 기본 3종을 런타임에 보강한다.
        equipmentPartConfigs.Add(CreateRuntimeConfig("part_armor_default", "전술 장갑", EquipmentPartSlot.Armor, 0.05f, 0.1f, 0.2f));
        equipmentPartConfigs.Add(CreateRuntimeConfig("part_engine_default", "고속 엔진", EquipmentPartSlot.Engine, 0.03f, 0.06f, 0.12f));
        equipmentPartConfigs.Add(CreateRuntimeConfig("part_chip_default", "화력 칩", EquipmentPartSlot.Chip, 0.05f, 0.1f, 0.2f));
    }

    private void EnsureEquipmentPartsInitialized()
    {
        if (equipmentPartsInitialized)
        {
            return;
        }

        equipmentPartsInitialized = true;
        EnsureEquipmentPartConfigs();
        LoadEquipmentParts();
    }

    private static EquipmentPartConfig CreateRuntimeConfig(
        string id,
        string displayName,
        EquipmentPartSlot slot,
        float commonValue,
        float rareValue,
        float epicValue)
    {
        EquipmentPartConfig config = ScriptableObject.CreateInstance<EquipmentPartConfig>();
        config.ConfigureRuntimeDefaults(id, displayName, slot, commonValue, rareValue, epicValue);
        return config;
    }

    private void LoadEquipmentParts()
    {
        equipmentParts ??= new List<EquipmentPartInstance>();
        if (!saveEquipmentPartsToPlayerPrefs)
        {
            NormalizeEquipmentParts();
            return;
        }

        string json = PlayerPrefs.GetString(EquipmentPartsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            EquipmentPartSaveData saveData = JsonUtility.FromJson<EquipmentPartSaveData>(json);
            equipmentParts = saveData?.parts ?? new List<EquipmentPartInstance>();
        }

        NormalizeEquipmentParts();
    }

    private void NormalizeEquipmentParts()
    {
        equipmentParts ??= new List<EquipmentPartInstance>();
        HashSet<string> instanceIds = new HashSet<string>();
        for (int i = equipmentParts.Count - 1; i >= 0; i--)
        {
            EquipmentPartInstance part = equipmentParts[i];
            if (part == null)
            {
                equipmentParts.RemoveAt(i);
                continue;
            }

            if (string.IsNullOrWhiteSpace(part.instanceId) || !instanceIds.Add(part.instanceId))
            {
                part.instanceId = System.Guid.NewGuid().ToString("N");
                instanceIds.Add(part.instanceId);
            }

            part.mainStatValue = Mathf.Max(0f, part.mainStatValue);
            part.salePrice = Mathf.Max(0, part.salePrice);
            part.subStats ??= new List<EquipmentSubStat>();
        }
    }

    private void SaveEquipmentParts()
    {
        if (!saveEquipmentPartsToPlayerPrefs)
        {
            return;
        }

        EquipmentPartSaveData saveData = new EquipmentPartSaveData { parts = equipmentParts };
        PlayerPrefs.SetString(EquipmentPartsKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }
}

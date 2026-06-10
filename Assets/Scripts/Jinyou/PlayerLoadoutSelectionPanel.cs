using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLoadoutSelectionPanel : MonoBehaviour
{
    private enum LoadoutMode
    {
        Weapon,
        Drone
    }

    [Header("Source")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerDroneController droneController;
    [SerializeField] private ProjectileConfig[] weaponOptions;
    [SerializeField] private DroneConfig[] droneOptions;

    [Header("Buttons")]
    [SerializeField] private Button equipButton;

    [Header("Panel")]
    [SerializeField] private GameObject selectionRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PlayerLoadoutOptionButton optionButtonPrefab;

    [Header("Detail")]
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailCategoryText;
    [SerializeField] private TMP_Text detailStatsText;

    private readonly List<PlayerLoadoutOptionButton> spawnedOptions = new List<PlayerLoadoutOptionButton>();
    private LoadoutMode currentMode;
    private ProjectileConfig selectedWeapon;
    private DroneConfig selectedDrone;
    private InventoryFacility inventory;
    private AssemblyFactory assemblyFactory;
    private Action<ProjectileConfig> weaponSelectionCallback;
    private Action<DroneConfig> droneSelectionCallback;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (weaponOptions == null || weaponOptions.Length == 0)
        {
            weaponOptions = LoadAssetsInEditor<ProjectileConfig>("Assets/SO/Balance/Weapons");
        }

        if (droneOptions == null || droneOptions.Length <= 1)
        {
            droneOptions = LoadAssetsInEditor<DroneConfig>("Assets/SO/Balance/Drones");
        }
    }
#endif

    private void Awake()
    {
        ResolveSources();
        selectionRoot?.SetActive(false);
    }

    private void OnEnable()
    {
        equipButton?.onClick.AddListener(ConfirmSelected);
    }

    private void OnDisable()
    {
        equipButton?.onClick.RemoveListener(ConfirmSelected);
    }

    public void OpenWeapons()
    {
        ResolveSources();
        weaponSelectionCallback = null;
        droneSelectionCallback = null;
        currentMode = LoadoutMode.Weapon;
        selectedWeapon = player != null ? player.WeaponConfig : null;
        OpenPanel("Weapon Loadout");
        RebuildWeaponList();
        RefreshWeaponDetail(selectedWeapon);
    }

    public void OpenDrones()
    {
        ResolveSources();
        weaponSelectionCallback = null;
        droneSelectionCallback = null;
        currentMode = LoadoutMode.Drone;
        selectedDrone = droneController != null ? droneController.DroneConfig : null;
        OpenPanel("Drone Loadout");
        RebuildDroneList();
        RefreshDroneDetail(selectedDrone);
    }

    public void OpenWeaponsForSelection(Action<ProjectileConfig> onSelected)
    {
        ResolveSources();
        weaponSelectionCallback = onSelected;
        droneSelectionCallback = null;
        currentMode = LoadoutMode.Weapon;
        selectedWeapon = assemblyFactory != null ? assemblyFactory.SelectedWeaponConfig : null;
        OpenPanel("Select Weapon To Enhance");
        RebuildWeaponList();
        RefreshWeaponDetail(selectedWeapon);
    }

    public void OpenDronesForSelection(Action<DroneConfig> onSelected)
    {
        ResolveSources();
        droneSelectionCallback = onSelected;
        weaponSelectionCallback = null;
        currentMode = LoadoutMode.Drone;
        selectedDrone = assemblyFactory != null ? assemblyFactory.SelectedDroneConfig : null;
        OpenPanel("Select Drone To Enhance");
        RebuildDroneList();
        RefreshDroneDetail(selectedDrone);
    }

    public void Close()
    {
        selectionRoot?.SetActive(false);
    }

    private void OpenPanel(string title)
    {
        selectionRoot?.SetActive(true);
        SetText(titleText, title);
    }

    private void RebuildWeaponList()
    {
        ClearOptions();
        if (weaponOptions == null)
        {
            return;
        }

        foreach (ProjectileConfig weapon in weaponOptions)
        {
            if (weapon == null || (inventory != null && !inventory.ContainsWeapon(weapon)))
            {
                continue;
            }

            PlayerLoadoutOptionButton option = CreateOption();
            if (option == null)
            {
                continue;
            }

            option.Bind(
                $"{weapon.DisplayName}  Factory Lv.{GetFactoryWeaponLevel(weapon)}",
                weapon.WeaponCategory,
                $"Factory Lv.{GetFactoryWeaponLevel(weapon)} / Damage {GetEnhancedWeaponDamage(weapon):0.##}",
                weapon == selectedWeapon,
                () => SelectWeapon(weapon));
        }
    }

    private void RebuildDroneList()
    {
        ClearOptions();
        if (droneOptions == null)
        {
            return;
        }

        foreach (DroneConfig drone in droneOptions)
        {
            if (drone == null)
            {
                continue;
            }

            PlayerLoadoutOptionButton option = CreateOption();
            if (option == null)
            {
                continue;
            }

            option.Bind(
                $"{drone.DisplayName}  Factory Lv.{GetFactoryDroneLevel(drone)}",
                $"Count {drone.DroneCount}",
                $"Factory Lv.{GetFactoryDroneLevel(drone)} / Damage {GetEnhancedDroneDamage(drone):0.##}",
                drone == selectedDrone,
                () => SelectDrone(drone));
        }
    }

    private PlayerLoadoutOptionButton CreateOption()
    {
        if (optionButtonPrefab == null || contentRoot == null)
        {
            return null;
        }

        PlayerLoadoutOptionButton option = Instantiate(optionButtonPrefab, contentRoot);
        option.gameObject.SetActive(true);
        spawnedOptions.Add(option);
        return option;
    }

    private void SelectWeapon(ProjectileConfig weapon)
    {
        selectedWeapon = weapon;
        RefreshWeaponDetail(weapon);
        RebuildWeaponList();
    }

    private void SelectDrone(DroneConfig drone)
    {
        selectedDrone = drone;
        RefreshDroneDetail(drone);
        RebuildDroneList();
    }

    private void ConfirmSelected()
    {
        ResolveSources();
        if (currentMode == LoadoutMode.Weapon)
        {
            if (weaponSelectionCallback != null)
            {
                Action<ProjectileConfig> callback = weaponSelectionCallback;
                weaponSelectionCallback = null;
                callback.Invoke(selectedWeapon);
                Close();
                return;
            }

            player?.SetWeaponConfig(selectedWeapon);
            RebuildWeaponList();
            RefreshWeaponDetail(selectedWeapon);
            return;
        }

        if (droneSelectionCallback != null)
        {
            Action<DroneConfig> callback = droneSelectionCallback;
            droneSelectionCallback = null;
            callback.Invoke(selectedDrone);
            Close();
            return;
        }

        droneController?.SetDroneConfig(selectedDrone);
        RebuildDroneList();
        RefreshDroneDetail(selectedDrone);
    }

    private void RefreshWeaponDetail(ProjectileConfig weapon)
    {
        SetText(detailNameText, weapon != null ? weapon.DisplayName : "Select a weapon.");
        SetText(detailCategoryText, weapon != null ? $"Type: {weapon.WeaponCategory}" : string.Empty);
        SetText(detailStatsText, weapon != null
            ? $"Factory Level: {GetFactoryWeaponLevel(weapon)}\n"
                + $"Collection Level: {GetCollectionWeaponLevel(weapon)}\n"
                + $"Base Damage: {weapon.AttackDamage:0.##}\n"
                + $"Enhanced Damage: {GetEnhancedWeaponDamage(weapon):0.##}\n"
                + $"Speed: {weapon.Speed:0.##}"
            : string.Empty);
    }

    private void RefreshDroneDetail(DroneConfig drone)
    {
        SetText(detailNameText, drone != null ? drone.DisplayName : "Select a drone.");
        SetText(detailCategoryText, drone != null ? $"Count: {drone.DroneCount}" : string.Empty);
        SetText(detailStatsText, drone != null
            ? $"Factory Level: {GetFactoryDroneLevel(drone)}\n"
                + $"Base Damage: {drone.AttackDamage:0.##}\n"
                + $"Enhanced Damage: {GetEnhancedDroneDamage(drone):0.##}\n"
                + $"Range: {drone.AttackRange:0.##}\n"
                + $"Attack Interval: {drone.AttackInterval:0.##}"
            : string.Empty);
    }

    private void ClearOptions()
    {
        for (int i = spawnedOptions.Count - 1; i >= 0; i--)
        {
            if (spawnedOptions[i] != null)
            {
                Destroy(spawnedOptions[i].gameObject);
            }
        }

        spawnedOptions.Clear();
    }

    private void ResolveSources()
    {
        player ??= FindFirstObjectByType<PlayerController>();
        droneController ??= player != null
            ? player.GetComponent<PlayerDroneController>()
            : FindFirstObjectByType<PlayerDroneController>();
        inventory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : InventoryFacility.FindAny();
        assemblyFactory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.AssemblyFactory
            : FindFirstObjectByType<AssemblyFactory>(FindObjectsInactive.Include);
    }

    private int GetCollectionWeaponLevel(ProjectileConfig weapon)
    {
        return inventory != null ? Mathf.Max(1, inventory.GetWeaponLevel(weapon)) : 1;
    }

    private int GetFactoryWeaponLevel(ProjectileConfig weapon)
    {
        return assemblyFactory != null ? assemblyFactory.GetWeaponEnhanceLevel(weapon) : 0;
    }

    private int GetFactoryDroneLevel(DroneConfig drone)
    {
        return assemblyFactory != null ? assemblyFactory.GetDroneEnhanceLevel(drone) : 0;
    }

    private float GetEnhancedWeaponDamage(ProjectileConfig weapon)
    {
        return weapon != null
            ? weapon.AttackDamage + (assemblyFactory != null
                ? assemblyFactory.GetWeaponStatBonus(
                    weapon,
                    AssemblyFactory.WeaponEnhancementStat.AttackDamage)
                : 0f)
            : 0f;
    }

    private float GetEnhancedDroneDamage(DroneConfig drone)
    {
        return drone != null
            ? drone.AttackDamage + (assemblyFactory != null
                ? assemblyFactory.GetDroneAttackDamageBonus(drone)
                : 0f)
            : 0f;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

#if UNITY_EDITOR
    private static T[] LoadAssetsInEditor<T>(string folder) where T : UnityEngine.Object
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        T[] assets = new T[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return assets;
    }
#endif
}

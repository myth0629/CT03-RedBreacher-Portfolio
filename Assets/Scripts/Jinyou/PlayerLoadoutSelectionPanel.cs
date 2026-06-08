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
    [SerializeField] private Button closeButton;
    [SerializeField] private Button equipButton;

    [Header("Panel")]
    [SerializeField] private GameObject selectionRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PlayerLoadoutOptionButton optionButtonPrefab;

    [Header("Detail")]
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailCategoryText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailStatsText;

    private readonly List<PlayerLoadoutOptionButton> spawnedOptions = new List<PlayerLoadoutOptionButton>();
    private LoadoutMode currentMode;
    private ProjectileConfig selectedWeapon;
    private DroneConfig selectedDrone;
    private InventoryFacility inventory;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (weaponOptions == null || weaponOptions.Length == 0)
        {
            weaponOptions = LoadAssetsInEditor<ProjectileConfig>("Assets/SO/Balance/Weapons");
        }

        if (droneOptions == null || droneOptions.Length == 0)
        {
            droneOptions = LoadAssetsInEditor<DroneConfig>("Assets/SO/Balance/Drones");
        }
    }
#endif

    private void Awake()
    {
        ResolveSources();

        if (selectionRoot != null)
        {
            selectionRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        closeButton?.onClick.AddListener(Close);
        equipButton?.onClick.AddListener(EquipSelected);
    }

    private void OnDisable()
    {
        closeButton?.onClick.RemoveListener(Close);
        equipButton?.onClick.RemoveListener(EquipSelected);
    }

    public void OpenWeapons()
    {
        currentMode = LoadoutMode.Weapon;
        selectedWeapon = player != null ? player.WeaponConfig : null;
        OpenPanel("Primary Weapon");
        RebuildWeaponList();
        RefreshWeaponDetail(selectedWeapon);
    }

    public void OpenDrones()
    {
        currentMode = LoadoutMode.Drone;
        selectedDrone = droneController != null ? droneController.DroneConfig : null;
        OpenPanel("Drone");
        RebuildDroneList();
        RefreshDroneDetail(selectedDrone);
    }

    public void Close()
    {
        if (selectionRoot != null)
        {
            selectionRoot.SetActive(false);
        }
    }

    private void OpenPanel(string title)
    {
        ResolveSources();

        if (selectionRoot != null)
        {
            selectionRoot.SetActive(true);
        }

        SetText(titleText, title);
    }

    private void RebuildWeaponList()
    {
        ClearOptions();

        if (weaponOptions == null)
        {
            return;
        }

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            ProjectileConfig weapon = weaponOptions[i];
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
                weapon.DisplayName,
                weapon.WeaponCategory,
                $"Lv.{GetWeaponLevel(weapon)} / Damage {weapon.AttackDamage:0.##} / Speed {weapon.Speed:0.##}",
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

        for (int i = 0; i < droneOptions.Length; i++)
        {
            DroneConfig drone = droneOptions[i];
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
                drone.DisplayName,
                $"{drone.DroneCount} unit(s)",
                $"Damage {drone.AttackDamage:0.##} / Range {drone.AttackRange:0.##}",
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

    private void EquipSelected()
    {
        ResolveSources();

        if (currentMode == LoadoutMode.Weapon)
        {
            player?.SetWeaponConfig(selectedWeapon);
            RebuildWeaponList();
            RefreshWeaponDetail(selectedWeapon);
            return;
        }

        droneController?.SetDroneConfig(selectedDrone);
        RebuildDroneList();
        RefreshDroneDetail(selectedDrone);
    }

    private void RefreshWeaponDetail(ProjectileConfig weapon)
    {
        SetText(detailNameText, weapon != null ? weapon.DisplayName : "No weapon selected");
        SetText(detailCategoryText, weapon != null ? weapon.WeaponCategory : string.Empty);
        SetText(detailDescriptionText, weapon != null ? weapon.Id : string.Empty);
        SetText(detailStatsText, weapon != null
            ? $"Level: {GetWeaponLevel(weapon)}\nDamage: {weapon.AttackDamage:0.##}\nSpeed: {weapon.Speed:0.##}\nLifetime: {weapon.Lifetime:0.##}\nKnockback: {weapon.KnockbackForce:0.##}\nMuzzle: {weapon.MultiMuzzleFireMode}"
            : string.Empty);
    }

    private void RefreshDroneDetail(DroneConfig drone)
    {
        SetText(detailNameText, drone != null ? drone.DisplayName : "No drone selected");
        SetText(detailCategoryText, drone != null ? $"{drone.DroneCount} unit(s)" : string.Empty);
        SetText(detailDescriptionText, drone != null ? drone.Id : string.Empty);
        SetText(detailStatsText, drone != null
            ? $"Damage: {drone.AttackDamage:0.##}\nRange: {drone.AttackRange:0.##}\nInterval: {drone.AttackInterval:0.##}\nProjectile Speed: {drone.ProjectileSpeed:0.##}\nFollow Radius: {drone.FollowRadius:0.##}"
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
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (droneController == null)
        {
            droneController = player != null
                ? player.GetComponent<PlayerDroneController>()
                : FindFirstObjectByType<PlayerDroneController>();
        }

        inventory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : InventoryFacility.FindAny();
    }

    private int GetWeaponLevel(ProjectileConfig weapon)
    {
        return inventory != null ? Mathf.Max(1, inventory.GetWeaponLevel(weapon)) : 1;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

#if UNITY_EDITOR
    private static T[] LoadAssetsInEditor<T>(string folder) where T : Object
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

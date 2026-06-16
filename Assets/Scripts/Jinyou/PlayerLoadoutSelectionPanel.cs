using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLoadoutSelectionPanel : MonoBehaviour
{
    private const string SelectedWeaponKey = "PlayerLoadout.SelectedWeapon";
    private const string SelectedDroneKey = "PlayerLoadout.SelectedDrone";

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
    [SerializeField] private Image detailIcon;
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


    private static void SetIcon(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
        target.preserveAspect = true;
    }

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
        // selectionRoot가 이 컴포넌트 자신의 GameObject인 구성에서는,
        // Awake가 첫 OpenPanel(SetActive(true)) 직후에 지연 실행되며 패널을 즉시 닫아버려
        // "처음엔 안 열리고 두 번째 클릭에야 열리는" 문제가 생긴다.
        // 패널은 씬/프리팹에서 비활성으로 시작하므로 여기서 따로 끌 필요가 없다.
        ResolveSources();
    }

    private void Start()
    {
        ResolveSources();
        LoadEquippedLoadout();
    }

    /// <summary>저장된 장착 무기/드론을 즉시 플레이어에 적용한다. 로드아웃 팝업이 비활성이라
    /// Start가 늦게 실행되는 문제를 피하려고, 부팅 시 외부(BaseCampManager)에서 호출한다.
    /// 비활성 상태에서도 동작한다.</summary>
    public void ApplySavedLoadout()
    {
        ResolveSources();
        LoadEquippedLoadout();
    }

    private void LoadEquippedLoadout()
    {
        DroneConfig initialDrone = RegisterInitialDrone();

        string savedWeaponId = PlayerPrefs.GetString(SelectedWeaponKey, string.Empty);
        if (!string.IsNullOrEmpty(savedWeaponId))
        {
            ProjectileConfig weapon = FindWeaponById(savedWeaponId);
            if (weapon != null && (inventory == null || inventory.ContainsWeapon(weapon)))
            {
                selectedWeapon = weapon;
                player?.SetWeaponConfig(weapon);
            }
        }

        string savedDroneId = PlayerPrefs.GetString(SelectedDroneKey, string.Empty);
        if (!string.IsNullOrEmpty(savedDroneId))
        {
            DroneConfig drone = FindDroneById(savedDroneId);
            if (drone != null && (inventory == null || inventory.ContainsDrone(drone)))
            {
                selectedDrone = drone;
                droneController?.SetDroneConfig(drone);
                return;
            }
        }

        selectedDrone = initialDrone;
        droneController?.SetDroneConfig(initialDrone);
        if (initialDrone != null)
        {
            PlayerPrefs.SetString(SelectedDroneKey, initialDrone.Id);
            PlayerPrefs.Save();
        }
    }

    private ProjectileConfig FindWeaponById(string id)
    {
        if (weaponOptions == null)
        {
            return null;
        }

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (weaponOptions[i] != null && weaponOptions[i].Id == id)
            {
                return weaponOptions[i];
            }
        }

        return null;
    }

    private DroneConfig FindDroneById(string id)
    {
        if (droneOptions == null)
        {
            return null;
        }

        for (int i = 0; i < droneOptions.Length; i++)
        {
            if (droneOptions[i] != null && droneOptions[i].Id == id)
            {
                return droneOptions[i];
            }
        }

        return null;
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
        OpenPanel("무기 로드아웃");
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
        OpenPanel("드론 로드아웃");
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
        OpenPanel("강화하고자 하는 무기를 선택하세요.");
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
        OpenPanel("강화하고자 하는 드론을 선택하세요.");
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
                $"{weapon.DisplayName} Lv.{GetFactoryWeaponLevel(weapon)}",
                weapon.WeaponCategory,
                $"Lv.{GetFactoryWeaponLevel(weapon)} / 피해량 {GetEnhancedWeaponDamage(weapon):0.##}",
                weapon == selectedWeapon,
                () => SelectWeapon(weapon),
                weapon.Icon);
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
            if (drone == null || (inventory != null && !inventory.ContainsDrone(drone)))
            {
                continue;
            }

            PlayerLoadoutOptionButton option = CreateOption();
            if (option == null)
            {
                continue;
            }

            option.Bind(
                $"{drone.DisplayName} Lv.{GetFactoryDroneLevel(drone)}",
                $"갯수 {drone.DroneCount}",
                $"Lv.{GetFactoryDroneLevel(drone)} / 피해량 {GetEnhancedDroneDamage(drone):0.##}",
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
            if (selectedWeapon != null)
            {
                PlayerPrefs.SetString(SelectedWeaponKey, selectedWeapon.Id);
                PlayerPrefs.Save();
            }
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

        if (selectedDrone != null && (inventory == null || inventory.ContainsDrone(selectedDrone)))
        {
            droneController?.SetDroneConfig(selectedDrone);
            PlayerPrefs.SetString(SelectedDroneKey, selectedDrone.Id);
            PlayerPrefs.Save();
        }
        RebuildDroneList();
        RefreshDroneDetail(selectedDrone);
    }

    private void RefreshWeaponDetail(ProjectileConfig weapon)
    {
        SetIcon(detailIcon, weapon != null ? weapon.Icon : null);
        SetText(detailNameText, weapon != null ? weapon.DisplayName : "무기를 선택하세요.");
        SetText(detailCategoryText, weapon != null ? $"Type: {weapon.WeaponCategory}" : string.Empty);
        SetText(detailStatsText, weapon != null
            ? $"공장강화 Lv. {GetFactoryWeaponLevel(weapon)}\n"
                + $"수집강화 Lv. {GetCollectionWeaponLevel(weapon)}\n"
                + $"피해량: {weapon.AttackDamage:0.##} (+ {GetEnhancedWeaponDamage(weapon):0.##})\n"
                + $"발사간격: {weapon.Speed:0.##}"
            : string.Empty);
    }

    private void RefreshDroneDetail(DroneConfig drone)
    {
        SetIcon(detailIcon, null);
        SetText(detailNameText, drone != null ? drone.DisplayName : "드론을 선택하세요.");
        SetText(detailCategoryText, drone != null ? $"갯수: {drone.DroneCount}" : string.Empty);
        SetText(detailStatsText, drone != null
            ? $"공장강화 Lv. {GetFactoryDroneLevel(drone)}\n"
                + $"피해량: {drone.AttackDamage:0.##} (+ {GetEnhancedDroneDamage(drone):0.##})\n"
                + $"사거리: {drone.AttackRange:0.##}\n"
                + $"발사간격: {drone.AttackInterval:0.##}"
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

    private DroneConfig RegisterInitialDrone()
    {
        if (droneOptions == null || droneOptions.Length == 0)
        {
            return null;
        }

        DroneConfig initialDrone = FindDroneById("drone_default") ?? droneOptions[0];
        // 기본 드론은 최초 지급이므로 수집 업적에는 포함하지 않는다.
        inventory?.RegisterInitialDrone(initialDrone);
        return initialDrone;
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

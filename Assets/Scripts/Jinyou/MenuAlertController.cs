using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuAlertController : MonoBehaviour
{
    private enum AlertCategory
    {
        Aircraft,
        Equipment,
        Base,
        Achievement,
        Shop
    }

    private const string CollectionSeenKey = "MenuAlert.CollectionSeenSignature";
    private const string EquipmentSeenKey = "MenuAlert.EquipmentSeenSignature";
    private const string BaseSeenKey = "MenuAlert.BaseSeenSignature";
    private const float RescanInterval = 1f;

    private class AlertBinding
    {
        public AlertCategory category;
        public Button button;
        public GameObject alertIcon;
    }

    private readonly List<AlertBinding> bindings = new List<AlertBinding>();
    private readonly HashSet<Button> wiredButtons = new HashSet<Button>();

    private BaseCampManager baseCampManager;
    private InventoryFacility inventory;
    private AchievementManager achievementManager;
    private WeaponGachaFacility gachaFacility;
    private InventoryFacility subscribedInventory;
    private AchievementManager subscribedAchievementManager;
    private BaseCampManager subscribedBaseCampManager;
    private float nextRescanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<MenuAlertController>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject host = new GameObject(nameof(MenuAlertController));
        host.AddComponent<MenuAlertController>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResolveReferences();
        SubscribeEvents();
        RefreshBindings();
        RefreshAlerts();
    }

    private void Start()
    {
        ResolveReferences();
        InitializeSeenSignatures();
        RefreshBindings();
        RefreshAlerts();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRescanTime)
        {
            return;
        }

        nextRescanTime = Time.unscaledTime + RescanInterval;
        ResolveReferences();
        SubscribeEvents();
        RefreshBindings();
        RefreshAlerts();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeEvents();
        bindings.Clear();
        wiredButtons.Clear();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();
        SubscribeEvents();
        RefreshBindings();
        RefreshAlerts();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>(FindObjectsInactive.Include);
        inventory ??= baseCampManager != null ? baseCampManager.Inventory : InventoryFacility.FindAny();
        achievementManager ??= AchievementManager.Instance ?? FindFirstObjectByType<AchievementManager>(FindObjectsInactive.Include);
        gachaFacility ??= FindFirstObjectByType<WeaponGachaFacility>(FindObjectsInactive.Include);
    }

    private void SubscribeEvents()
    {
        if (inventory != null && subscribedInventory != inventory)
        {
            if (subscribedInventory != null)
            {
                subscribedInventory.OnCollectionProgressChanged.RemoveListener(RefreshAlerts);
                subscribedInventory.OnEquipmentPartsChanged.RemoveListener(RefreshAlerts);
                subscribedInventory.OnInventoryChanged.RemoveListener(RefreshAlerts);
            }

            inventory.OnCollectionProgressChanged.AddListener(RefreshAlerts);
            inventory.OnEquipmentPartsChanged.AddListener(RefreshAlerts);
            inventory.OnInventoryChanged.AddListener(RefreshAlerts);
            subscribedInventory = inventory;
        }

        if (achievementManager != null && subscribedAchievementManager != achievementManager)
        {
            if (subscribedAchievementManager != null)
            {
                subscribedAchievementManager.OnAchievementsChanged.RemoveListener(RefreshAlerts);
                subscribedAchievementManager.OnAchievementCompleted.RemoveListener(HandleAchievementCompleted);
            }

            achievementManager.OnAchievementsChanged.AddListener(RefreshAlerts);
            achievementManager.OnAchievementCompleted.AddListener(HandleAchievementCompleted);
            subscribedAchievementManager = achievementManager;
        }

        if (baseCampManager != null && subscribedBaseCampManager != baseCampManager)
        {
            if (subscribedBaseCampManager != null)
            {
                subscribedBaseCampManager.OnCoreCrystalsChanged.RemoveListener(HandleCurrencyChanged);
                UnsubscribeFacilityEvents(subscribedBaseCampManager);
            }

            baseCampManager.OnCoreCrystalsChanged.AddListener(HandleCurrencyChanged);
            SubscribeFacilityEvents(baseCampManager);
            subscribedBaseCampManager = baseCampManager;
        }
    }

    private void UnsubscribeEvents()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory.OnCollectionProgressChanged.RemoveListener(RefreshAlerts);
            subscribedInventory.OnEquipmentPartsChanged.RemoveListener(RefreshAlerts);
            subscribedInventory.OnInventoryChanged.RemoveListener(RefreshAlerts);
            subscribedInventory = null;
        }

        if (subscribedAchievementManager != null)
        {
            subscribedAchievementManager.OnAchievementsChanged.RemoveListener(RefreshAlerts);
            subscribedAchievementManager.OnAchievementCompleted.RemoveListener(HandleAchievementCompleted);
            subscribedAchievementManager = null;
        }

        if (subscribedBaseCampManager != null)
        {
            subscribedBaseCampManager.OnCoreCrystalsChanged.RemoveListener(HandleCurrencyChanged);
            UnsubscribeFacilityEvents(subscribedBaseCampManager);
            subscribedBaseCampManager = null;
        }
    }

    private void SubscribeFacilityEvents(BaseCampManager manager)
    {
        manager.CommandCenter?.OnUpgradeCompleted.AddListener(RefreshAlerts);
        manager.CreditRefinery?.OnUpgradeCompleted.AddListener(RefreshAlerts);
        manager.AssemblyFactory?.OnUpgradeCompleted.AddListener(RefreshAlerts);
        manager.CoreCharger?.OnUpgradeCompleted.AddListener(RefreshAlerts);
    }

    private void UnsubscribeFacilityEvents(BaseCampManager manager)
    {
        manager.CommandCenter?.OnUpgradeCompleted.RemoveListener(RefreshAlerts);
        manager.CreditRefinery?.OnUpgradeCompleted.RemoveListener(RefreshAlerts);
        manager.AssemblyFactory?.OnUpgradeCompleted.RemoveListener(RefreshAlerts);
        manager.CoreCharger?.OnUpgradeCompleted.RemoveListener(RefreshAlerts);
    }

    private void InitializeSeenSignatures()
    {
        InitializeSignature(CollectionSeenKey, BuildCollectionSignature());
        InitializeSignature(EquipmentSeenKey, BuildEquipmentSignature());
        InitializeSignature(BaseSeenKey, BuildBaseSignature());
    }

    private static void InitializeSignature(string key, string signature)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return;
        }

        PlayerPrefs.SetString(key, signature);
        PlayerPrefs.Save();
    }

    private void RefreshBindings()
    {
        Transform menuPanel = FindMenuButtonsPanel();
        if (menuPanel == null)
        {
            return;
        }

        BindMenuButton(menuPanel, "Player_Btn", AlertCategory.Aircraft);
        BindMenuButton(menuPanel, "Inventory_Btn", AlertCategory.Equipment);
        BindMenuButton(menuPanel, "Base_Btn", AlertCategory.Base);
        BindMenuButton(menuPanel, "Achievement_Btn", AlertCategory.Achievement);
        BindMenuButton(menuPanel, "Shop_Btn", AlertCategory.Shop);
    }

    private void BindMenuButton(Transform menuPanel, string buttonName, AlertCategory category)
    {
        Transform buttonTransform = FindDirectChild(menuPanel, buttonName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        Transform alert = buttonTransform != null ? FindChildByName(buttonTransform, "alert") : null;
        if (button == null || alert == null || wiredButtons.Contains(button))
        {
            return;
        }

        // Focus 안의 alert를 그대로 사용해서 프리팹 계층을 런타임에 바꾸지 않는다.
        bindings.Add(new AlertBinding
        {
            category = category,
            button = button,
            alertIcon = alert.gameObject
        });
        wiredButtons.Add(button);

        AlertCategory capturedCategory = category;
        button.onClick.AddListener(() => MarkSeen(capturedCategory));
    }

    private void RefreshAlerts()
    {
        bool aircraftAlert = HasNewToken(CollectionSeenKey, BuildCollectionSignature());
        bool equipmentAlert = HasNewToken(EquipmentSeenKey, BuildEquipmentSignature());
        bool baseAlert = HasBaseUpgradeCompleted(BuildBaseSignature());
        bool achievementAlert = HasCompletedAchievement();
        bool shopAlert = CanUseShop();

        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            AlertBinding binding = bindings[i];
            if (binding?.button == null || binding.alertIcon == null)
            {
                bindings.RemoveAt(i);
                continue;
            }

            binding.alertIcon.SetActive(binding.category switch
            {
                AlertCategory.Aircraft => aircraftAlert,
                AlertCategory.Equipment => equipmentAlert,
                AlertCategory.Base => baseAlert,
                AlertCategory.Achievement => achievementAlert,
                AlertCategory.Shop => shopAlert,
                _ => false
            });
        }
    }

    private void MarkSeen(AlertCategory category)
    {
        switch (category)
        {
            case AlertCategory.Aircraft:
                PlayerPrefs.SetString(CollectionSeenKey, BuildCollectionSignature());
                break;
            case AlertCategory.Equipment:
                PlayerPrefs.SetString(EquipmentSeenKey, BuildEquipmentSignature());
                break;
            case AlertCategory.Base:
                PlayerPrefs.SetString(BaseSeenKey, BuildBaseSignature());
                break;
        }

        PlayerPrefs.Save();
        RefreshAlerts();
    }

    private string BuildCollectionSignature()
    {
        if (inventory == null)
        {
            return string.Empty;
        }

        List<string> ids = new List<string>();
        IReadOnlyList<ProjectileConfig> weapons = inventory.WeaponConfigs;
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
            {
                ids.Add("W:" + weapons[i].Id);
            }
        }

        IReadOnlyList<string> drones = inventory.OwnedDroneIds;
        for (int i = 0; i < drones.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(drones[i]))
            {
                ids.Add("D:" + drones[i]);
            }
        }

        return JoinSorted(ids);
    }

    private string BuildEquipmentSignature()
    {
        if (inventory == null)
        {
            return string.Empty;
        }

        List<string> ids = new List<string>();
        IReadOnlyList<EquipmentPartInstance> parts = inventory.EquipmentParts;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && !string.IsNullOrWhiteSpace(parts[i].instanceId))
            {
                ids.Add(parts[i].instanceId);
            }
        }

        return JoinSorted(ids);
    }

    private string BuildBaseSignature()
    {
        if (baseCampManager == null)
        {
            return string.Empty;
        }

        return string.Join("|",
            baseCampManager.CommandCenter != null ? baseCampManager.CommandCenter.Level.ToString() : "0",
            baseCampManager.CreditRefinery != null ? baseCampManager.CreditRefinery.Level.ToString() : "0",
            baseCampManager.AssemblyFactory != null ? baseCampManager.AssemblyFactory.Level.ToString() : "0",
            baseCampManager.CoreCharger != null ? baseCampManager.CoreCharger.Level.ToString() : "0");
    }

    private bool HasCompletedAchievement()
    {
        if (achievementManager == null)
        {
            return false;
        }

        IReadOnlyList<AchievementManager.AchievementEntry> achievements = achievementManager.Achievements;
        for (int i = 0; i < achievements.Count; i++)
        {
            if (achievements[i] != null && achievements[i].Completed)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanUseShop()
    {
        if (gachaFacility == null)
        {
            return false;
        }

        return gachaFacility.CanDraw(GachaCategory.Weapon, 1)
            || gachaFacility.CanDraw(GachaCategory.Skill, 1);
    }

    private static string JoinSorted(List<string> values)
    {
        values.Sort();
        return string.Join("|", values);
    }

    private static bool HasNewToken(string key, string currentSignature)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            return false;
        }

        HashSet<string> seenTokens = BuildTokenSet(PlayerPrefs.GetString(key, string.Empty));
        string[] currentTokens = SplitSignature(currentSignature);
        for (int i = 0; i < currentTokens.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(currentTokens[i]) && !seenTokens.Contains(currentTokens[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasBaseUpgradeCompleted(string currentSignature)
    {
        if (!PlayerPrefs.HasKey(BaseSeenKey))
        {
            return false;
        }

        string[] seen = SplitSignature(PlayerPrefs.GetString(BaseSeenKey, string.Empty));
        string[] current = SplitSignature(currentSignature);
        int count = Mathf.Min(seen.Length, current.Length);
        for (int i = 0; i < count; i++)
        {
            if (int.TryParse(current[i], out int currentLevel)
                && int.TryParse(seen[i], out int seenLevel)
                && currentLevel > seenLevel)
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> BuildTokenSet(string signature)
    {
        HashSet<string> tokens = new HashSet<string>();
        string[] values = SplitSignature(signature);
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
            {
                tokens.Add(values[i]);
            }
        }

        return tokens;
    }

    private static string[] SplitSignature(string signature)
    {
        return string.IsNullOrWhiteSpace(signature)
            ? System.Array.Empty<string>()
            : signature.Split('|');
    }

    private static void HandleAchievementCompleted(AchievementManager.AchievementEntry achievement)
    {
        FindFirstObjectByType<MenuAlertController>(FindObjectsInactive.Include)?.RefreshAlerts();
    }

    private static void HandleCurrencyChanged(int value)
    {
        FindFirstObjectByType<MenuAlertController>(FindObjectsInactive.Include)?.RefreshAlerts();
    }

    private static Transform FindMenuButtonsPanel()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == "MenuButtons_Panel")
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildByName(root.GetChild(i), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}

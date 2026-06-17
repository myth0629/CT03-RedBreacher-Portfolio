using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentPartsPanel : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private InventoryFacility inventory;
    [SerializeField] private PlayerEquipmentPartLoadout loadout;
    [SerializeField] private PlayerCurrencyWallet currencyWallet;

    [Header("Part List")]
    [SerializeField] private RectTransform partContentRoot;
    [SerializeField] private Button partButtonPrefab;
    [SerializeField] private Color commonFrameColor = Color.white;
    [SerializeField] private Color rareFrameColor = new Color(0.25f, 0.55f, 1f, 1f);
    [SerializeField] private Color epicFrameColor = new Color(0.75f, 0.3f, 1f, 1f);

    [Header("Equipped Slots")]
    [SerializeField] private TMP_Text armorSlotText;
    [SerializeField] private TMP_Text engineSlotText;
    [SerializeField] private TMP_Text chipSlotText;

    [Header("Selected Part")]
    [SerializeField] private TMP_Text partNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text mainStatText;
    [SerializeField] private TMP_Text subStatText;
    [SerializeField] private TMP_Text salePriceText;
    [SerializeField] private Image partIcon;
    [SerializeField] private TMP_Text beforeSelectText;
    [SerializeField] private TMP_Text mainStatTitle;
    [SerializeField] private TMP_Text subStatTitle;

    [Header("Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button sellButton;

    [Header("Filter")]
    [SerializeField] private Button armorFilterButton;
    [SerializeField] private Button engineFilterButton;
    [SerializeField] private Button chipFilterButton;
    [SerializeField] private Button allFilterButton;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly Dictionary<string, GameObject> newBadges = new Dictionary<string, GameObject>();
    private string selectedInstanceId;

    // null이면 전체 표시, 값이 있으면 해당 슬롯 파츠만 표시한다.
    private EquipmentPartSlot? slotFilter;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
        equipButton?.onClick.AddListener(EquipSelected);
        unequipButton?.onClick.AddListener(UnequipSelected);
        sellButton?.onClick.AddListener(SellSelected);
        armorFilterButton?.onClick.AddListener(FilterByArmor);
        engineFilterButton?.onClick.AddListener(FilterByEngine);
        chipFilterButton?.onClick.AddListener(FilterByChip);
        allFilterButton?.onClick.AddListener(ShowAllParts);
        Rebuild();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        equipButton?.onClick.RemoveListener(EquipSelected);
        unequipButton?.onClick.RemoveListener(UnequipSelected);
        sellButton?.onClick.RemoveListener(SellSelected);
        armorFilterButton?.onClick.RemoveListener(FilterByArmor);
        engineFilterButton?.onClick.RemoveListener(FilterByEngine);
        chipFilterButton?.onClick.RemoveListener(FilterByChip);
        allFilterButton?.onClick.RemoveListener(ShowAllParts);
        ClearButtons();
    }

    public void Rebuild()
    {
        ResolveReferences();
        ClearButtons();

        if (inventory != null && partContentRoot != null && partButtonPrefab != null)
        {
            List<EquipmentPartInstance> parts = CollectSortedParts();
            for (int i = 0; i < parts.Count; i++)
            {
                CreatePartButton(parts[i]);
            }
        }

        RefreshFilterButtons();
        RefreshEquippedSlots();
        RefreshDetail();
    }

    private List<EquipmentPartInstance> CollectSortedParts()
    {
        List<EquipmentPartInstance> parts = new List<EquipmentPartInstance>();
        foreach (EquipmentPartInstance part in inventory.EquipmentParts)
        {
            if (part == null)
            {
                continue;
            }

            // 필터가 설정된 경우 해당 슬롯의 파츠만 목록에 표시한다.
            if (slotFilter.HasValue && part.slot != slotFilter.Value)
            {
                continue;
            }

            parts.Add(part);
        }

        // 장착 중 → 희귀도 내림차순 → 레벨 내림차순 → 이름 순으로 정렬한다.
        parts.Sort(ComparePartsForDisplay);
        return parts;
    }

    private int ComparePartsForDisplay(EquipmentPartInstance a, EquipmentPartInstance b)
    {
        bool equippedA = loadout != null && loadout.IsEquipped(a.instanceId);
        bool equippedB = loadout != null && loadout.IsEquipped(b.instanceId);
        if (equippedA != equippedB)
        {
            return equippedA ? -1 : 1;
        }

        if (a.rarity != b.rarity)
        {
            return b.rarity.CompareTo(a.rarity);
        }

        if (a.level != b.level)
        {
            return b.level.CompareTo(a.level);
        }

        return string.Compare(GetSortName(a), GetSortName(b), System.StringComparison.Ordinal);
    }

    private string GetSortName(EquipmentPartInstance part)
    {
        EquipmentPartConfig config = inventory != null ? inventory.ResolveEquipmentPartConfig(part.configId) : null;
        return GetDisplayName(config, part);
    }

    public void FilterByArmor() => SetSlotFilter(EquipmentPartSlot.Armor);

    public void FilterByEngine() => SetSlotFilter(EquipmentPartSlot.Engine);

    public void FilterByChip() => SetSlotFilter(EquipmentPartSlot.Chip);

    public void ShowAllParts() => SetSlotFilter(null);

    private void SetSlotFilter(EquipmentPartSlot? slot)
    {
        // 활성화된 필터 버튼을 다시 누르면 전체 표시로 토글한다.
        slotFilter = slotFilter == slot ? null : slot;
        Rebuild();
    }

    private void RefreshFilterButtons()
    {
        SetFilterButtonActive(armorFilterButton, slotFilter == EquipmentPartSlot.Armor);
        SetFilterButtonActive(engineFilterButton, slotFilter == EquipmentPartSlot.Engine);
        SetFilterButtonActive(chipFilterButton, slotFilter == EquipmentPartSlot.Chip);
        SetFilterButtonActive(allFilterButton, slotFilter == null);
    }

    private static void SetFilterButtonActive(Button button, bool active)
    {
        // 현재 선택된 필터 버튼은 비활성(눌린 상태)으로 표시해 시각적으로 구분한다.
        if (button != null)
        {
            button.interactable = !active;
        }
    }

    public void EquipSelected()
    {
        EquipmentPartInstance part = GetSelectedPart();
        if (part != null && loadout != null)
        {
            loadout.Equip(part);
        }
    }

    public void UnequipSelected()
    {
        EquipmentPartInstance part = GetSelectedPart();
        if (part != null && loadout != null && loadout.IsEquipped(part.instanceId))
        {
            loadout.Unequip(part.slot);
        }
    }

    public void SellSelected()
    {
        EquipmentPartInstance part = GetSelectedPart();
        if (part == null || inventory == null)
        {
            return;
        }

        if (inventory.TrySellEquipmentPart(part.instanceId, loadout, currencyWallet))
        {
            selectedInstanceId = string.Empty;
        }
    }

    private void CreatePartButton(EquipmentPartInstance part)
    {
        if (part == null)
        {
            return;
        }

        Button button = Instantiate(partButtonPrefab, partContentRoot);
        EquipmentPartConfig config = inventory.ResolveEquipmentPartConfig(part.configId);
        Image iconImage = FindChildImage(button.transform, "Icon");
        if (iconImage != null)
        {
            // 파츠 종류별 SO에 지정된 아이콘을 목록 슬롯에 표시한다.
            iconImage.sprite = GetPartsIcon(config);
            iconImage.enabled = iconImage.sprite != null;
            iconImage.preserveAspect = true;
        }

        Image frameImage = FindChildImage(button.transform, "Frame");
        if (frameImage != null)
        {
            // 파츠 희귀도에 따라 슬롯 프레임 색상을 구분한다.
            frameImage.color = GetRarityFrameColor(part.rarity);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            string equipped = loadout != null && loadout.IsEquipped(part.instanceId) ? " [장착]" : string.Empty;
            label.text = $"{GetDisplayName(config, part)} / Lv.{part.level} / {GetRarityName(part.rarity)}{equipped}";
        }

        // 아직 확인하지 않은 신규 파츠에는 "New" 뱃지를 표시한다(프리팹에 "New" 자식이 있을 때만).
        Transform newBadge = FindChildObject(button.transform, "New");
        if (newBadge != null)
        {
            newBadge.gameObject.SetActive(part.isNew);
            if (!string.IsNullOrEmpty(part.instanceId))
            {
                newBadges[part.instanceId] = newBadge.gameObject;
            }
        }

        string capturedId = part.instanceId;
        button.onClick.AddListener(() => Select(capturedId));
        spawnedButtons.Add(button);
    }

    private static Transform FindChildObject(Transform root, string childName)
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
            Transform found = FindChildObject(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Image FindChildImage(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root.GetComponent<Image>();
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Image image = FindChildImage(root.GetChild(i), childName);
            if (image != null)
            {
                return image;
            }
        }

        return null;
    }

    private Color GetRarityFrameColor(EquipmentPartRarity rarity)
    {
        return rarity switch
        {
            EquipmentPartRarity.Rare => rareFrameColor,
            EquipmentPartRarity.Epic => epicFrameColor,
            _ => commonFrameColor
        };
    }

    private void Select(string instanceId)
    {
        selectedInstanceId = instanceId;

        // 선택한 파츠는 확인 처리하여 신규 뱃지를 해제한다(목록 전체 리빌드 없이 해당 뱃지만 끈다).
        if (inventory != null)
        {
            inventory.MarkEquipmentPartSeen(instanceId);
        }

        if (!string.IsNullOrEmpty(instanceId)
            && newBadges.TryGetValue(instanceId, out GameObject badge)
            && badge != null)
        {
            badge.SetActive(false);
        }

        RefreshDetail();
    }

    private void RefreshEquippedSlots()
    {
        SetText(armorSlotText, BuildEquippedText(EquipmentPartSlot.Armor));
        SetText(engineSlotText, BuildEquippedText(EquipmentPartSlot.Engine));
        SetText(chipSlotText, BuildEquippedText(EquipmentPartSlot.Chip));
    }

    private string BuildEquippedText(EquipmentPartSlot slot)
    {
        EquipmentPartInstance part = loadout != null ? loadout.GetEquippedPart(slot) : null;
        if (part == null)
        {
            return "미장착";
        }

        EquipmentPartConfig config = inventory != null ? inventory.ResolveEquipmentPartConfig(part.configId) : null;
        return $"{GetDisplayName(config, part)} Lv.{part.level} ({GetRarityName(part.rarity)})";
    }

    private void RefreshDetail()
    {
        EquipmentPartInstance part = GetSelectedPart();
        EquipmentPartConfig config = part != null && inventory != null
            ? inventory.ResolveEquipmentPartConfig(part.configId)
            : null;

        SetText(beforeSelectText, part != null ? string.Empty : "파츠를 선택하세요");
        SetText(partNameText, part != null ? GetDisplayName(config, part) : string.Empty);
        SetText(rarityText, part != null ? $"/ Lv.{part.level} {GetRarityName(part.rarity)}" : string.Empty);
        SetText(mainStatTitle, part != null ? "주 옵션" : string.Empty);
        SetText(mainStatText, part != null ? FormatStat(part.mainStatType, part.GetScaledMainValue()) : string.Empty);
        SetText(subStatTitle, part != null ? "부가 옵션" : string.Empty);
        SetText(subStatText, part != null ? BuildSubStatText(part) : string.Empty);
        SetText(salePriceText, part != null ? $"판매\n{part.salePrice:N0} 크레딧" : string.Empty);
        SetPartIcon(partIcon, GetPartsIcon(config));

        bool hasSelection = part != null;
        bool equipped = part != null && loadout != null && loadout.IsEquipped(part.instanceId);
        SetButtonVisible(equipButton, hasSelection);
        SetButtonVisible(unequipButton, hasSelection);
        SetButtonVisible(sellButton, hasSelection);

        if (equipButton != null)
        {
            equipButton.interactable = hasSelection && !equipped;
        }

        if (unequipButton != null)
        {
            unequipButton.interactable = equipped;
        }

        if (sellButton != null)
        {
            sellButton.interactable = hasSelection && !equipped;
        }
    }

    private string BuildSubStatText(EquipmentPartInstance part)
    {
        if (part.subStats == null || part.subStats.Count == 0)
        {
            return "부옵 없음";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < part.subStats.Count; i++)
        {
            EquipmentSubStat subStat = part.subStats[i];
            if (subStat == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(FormatStat(subStat.statType, part.GetScaledSubStatValue(subStat)));
        }

        return builder.ToString();
    }

    private EquipmentPartInstance GetSelectedPart()
    {
        return inventory != null ? inventory.FindEquipmentPart(selectedInstanceId) : null;
    }

    private void ResolveReferences()
    {
        inventory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : FindFirstObjectByType<InventoryFacility>();
        loadout ??= FindFirstObjectByType<PlayerEquipmentPartLoadout>();
        currencyWallet ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.CurrencyWallet
            : FindFirstObjectByType<PlayerCurrencyWallet>();
    }

    private void SubscribeEvents()
    {
        inventory?.OnEquipmentPartsChanged.AddListener(Rebuild);
        loadout?.OnLoadoutChanged.AddListener(Rebuild);
    }

    private void UnsubscribeEvents()
    {
        inventory?.OnEquipmentPartsChanged.RemoveListener(Rebuild);
        loadout?.OnLoadoutChanged.RemoveListener(Rebuild);
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                Destroy(spawnedButtons[i].gameObject);
            }
        }

        spawnedButtons.Clear();
        newBadges.Clear();
    }

    private static string GetDisplayName(EquipmentPartConfig config, EquipmentPartInstance part)
    {
        return config != null ? config.DisplayName : part.configId;
    }

    private static Sprite GetPartsIcon(EquipmentPartConfig config)
    {
        return config != null ? config.Icon : null;
    }

    private static string GetRarityName(EquipmentPartRarity rarity)
    {
        string rarityName = rarity switch
        {
            EquipmentPartRarity.Rare => "희귀",
            EquipmentPartRarity.Epic => "영웅",
            _ => "일반"
        };

        return $"<color=#{GetRarityColorHex(rarity)}>{rarityName}</color>";
    }

    private static string GetRarityColorHex(EquipmentPartRarity rarity)
    {
        return rarity switch
        {
            EquipmentPartRarity.Rare => "59CCFF",
            EquipmentPartRarity.Epic => "FF73E6",
            _ => "FFFFFF"
        };
    }

    private static string FormatStat(EquipmentStatType statType, float value)
    {
        string statName = statType switch
        {
            EquipmentStatType.AttackPercent => "공격력",
            EquipmentStatType.HealthPercent => "최대 체력",
            EquipmentStatType.AttackSpeedPercent => "공격속도",
            EquipmentStatType.CritChance => "치명타 확률",
            _ => "치명타 피해"
        };

        return $"{statName} +{value * 100f:0.##}%";
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetButtonVisible(Button target, bool visible)
    {
        if (target != null && target.gameObject.activeSelf != visible)
        {
            target.gameObject.SetActive(visible);
        }
    }

    private static void SetPartIcon(Image target, Sprite icon)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = icon;
        target.enabled = icon != null;
        target.preserveAspect = true;
    }
}

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

    [Header("Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button closeButton;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private string selectedInstanceId;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
        equipButton?.onClick.AddListener(EquipSelected);
        unequipButton?.onClick.AddListener(UnequipSelected);
        sellButton?.onClick.AddListener(SellSelected);
        closeButton?.onClick.AddListener(Close);
        Rebuild();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        equipButton?.onClick.RemoveListener(EquipSelected);
        unequipButton?.onClick.RemoveListener(UnequipSelected);
        sellButton?.onClick.RemoveListener(SellSelected);
        closeButton?.onClick.RemoveListener(Close);
        ClearButtons();
    }

    public void Rebuild()
    {
        ResolveReferences();
        ClearButtons();

        if (inventory != null && partContentRoot != null && partButtonPrefab != null)
        {
            foreach (EquipmentPartInstance part in inventory.EquipmentParts)
            {
                CreatePartButton(part);
            }
        }

        RefreshEquippedSlots();
        RefreshDetail();
    }

    public void Close()
    {
        gameObject.SetActive(false);
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
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            string equipped = loadout != null && loadout.IsEquipped(part.instanceId) ? " [장착]" : string.Empty;
            label.text = $"{GetDisplayName(config, part)} / {GetRarityName(part.rarity)}{equipped}";
        }

        string capturedId = part.instanceId;
        button.onClick.AddListener(() => Select(capturedId));
        spawnedButtons.Add(button);
    }

    private void Select(string instanceId)
    {
        selectedInstanceId = instanceId;
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
            return $"{GetSlotName(slot)}: 미장착";
        }

        EquipmentPartConfig config = inventory != null ? inventory.ResolveEquipmentPartConfig(part.configId) : null;
        return $"{GetSlotName(slot)}: {GetDisplayName(config, part)} ({GetRarityName(part.rarity)})";
    }

    private void RefreshDetail()
    {
        EquipmentPartInstance part = GetSelectedPart();
        EquipmentPartConfig config = part != null && inventory != null
            ? inventory.ResolveEquipmentPartConfig(part.configId)
            : null;

        SetText(partNameText, part != null ? GetDisplayName(config, part) : "파츠를 선택하세요");
        SetText(rarityText, part != null ? GetRarityName(part.rarity) : string.Empty);
        SetText(mainStatText, part != null ? FormatStat(part.mainStatType, part.mainStatValue) : string.Empty);
        SetText(subStatText, part != null ? BuildSubStatText(part) : string.Empty);
        SetText(salePriceText, part != null ? $"{part.salePrice:N0} 크레딧" : string.Empty);

        bool equipped = part != null && loadout != null && loadout.IsEquipped(part.instanceId);
        if (equipButton != null)
        {
            equipButton.interactable = part != null && !equipped;
        }

        if (unequipButton != null)
        {
            unequipButton.interactable = equipped;
        }

        if (sellButton != null)
        {
            sellButton.interactable = part != null && !equipped;
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

            builder.Append(FormatStat(subStat.statType, subStat.value));
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
    }

    private static string GetDisplayName(EquipmentPartConfig config, EquipmentPartInstance part)
    {
        return config != null ? config.DisplayName : part.configId;
    }

    private static string GetSlotName(EquipmentPartSlot slot)
    {
        return slot switch
        {
            EquipmentPartSlot.Armor => "장갑",
            EquipmentPartSlot.Engine => "엔진",
            _ => "칩"
        };
    }

    private static string GetRarityName(EquipmentPartRarity rarity)
    {
        return rarity switch
        {
            EquipmentPartRarity.Rare => "희귀",
            EquipmentPartRarity.Epic => "영웅",
            _ => "일반"
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
}

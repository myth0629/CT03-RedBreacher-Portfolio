using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SkillLoadoutPanel : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerController player;
    [SerializeField] private InventoryFacility inventory;

    [Header("Skill List")]
    [SerializeField] private RectTransform skillContentRoot;
    [SerializeField] private Button skillButtonPrefab;

    [Header("Equipped Slots")]
    [SerializeField] private TMP_Text[] equippedSlotTexts = new TMP_Text[3];

    [Header("Detail")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillLevelText;
    [SerializeField] private TMP_Text duplicateProgressText;

    [Header("Commands")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button[] equipSlotButtons = new Button[3];
    [SerializeField] private Button[] unequipSlotButtons = new Button[3];

    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly UnityAction[] equipActions = new UnityAction[3];
    private readonly UnityAction[] unequipActions = new UnityAction[3];
    private PlayerSkillConfig selectedSkill;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeButtons();
        SubscribeInventory();
        Rebuild();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
        UnsubscribeInventory();
        ClearButtons();
    }

    public void EquipSelectedToSlot(int slotIndex)
    {
        if (player != null && selectedSkill != null && player.EquipSkill(slotIndex, selectedSkill))
        {
            Refresh();
        }
    }

    public void UnequipSlot(int slotIndex)
    {
        if (player != null && player.UnequipSkill(slotIndex))
        {
            Refresh();
        }
    }

    private void Rebuild()
    {
        ClearButtons();
        if (inventory == null)
        {
            Refresh();
            return;
        }

        for (int i = 0; i < inventory.SkillConfigs.Count; i++)
        {
            PlayerSkillConfig skill = inventory.SkillConfigs[i];
            if (skill == null || skillContentRoot == null || skillButtonPrefab == null)
            {
                continue;
            }

            Button button = Instantiate(skillButtonPrefab, skillContentRoot);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = $"{skill.DisplayName} Lv.{inventory.GetSkillLevel(skill)}";
            }

            PlayerSkillConfig capturedSkill = skill;
            button.onClick.AddListener(() => SelectSkill(capturedSkill));
            spawnedButtons.Add(button);
        }

        Refresh();
    }

    private void SelectSkill(PlayerSkillConfig skill)
    {
        selectedSkill = skill;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < equippedSlotTexts.Length; i++)
        {
            PlayerSkillConfig equipped = player != null ? player.GetEquippedSkill(i) : null;
            SetText(
                equippedSlotTexts[i],
                equipped != null
                    ? $"{equipped.DisplayName} Lv.{GetSkillLevel(equipped)}"
                    : "비어 있음");
        }

        SetText(skillNameText, selectedSkill != null ? selectedSkill.DisplayName : "스킬 선택");
        SetText(skillLevelText, selectedSkill != null ? $"Lv.{GetSkillLevel(selectedSkill)}" : string.Empty);

        if (selectedSkill == null || inventory == null)
        {
            SetText(duplicateProgressText, string.Empty);
            return;
        }

        int required = inventory.GetRequiredDuplicates(selectedSkill);
        SetText(
            duplicateProgressText,
            required > 0
                ? $"{inventory.GetDuplicateProgress(selectedSkill)} / {required}"
                : "MAX");
    }

    private int GetSkillLevel(PlayerSkillConfig skill)
    {
        return inventory != null ? Mathf.Max(1, inventory.GetSkillLevel(skill)) : 1;
    }

    private void ResolveReferences()
    {
        player ??= FindFirstObjectByType<PlayerController>();
        inventory ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : InventoryFacility.FindAny();
    }

    private void SubscribeButtons()
    {
        closeButton?.onClick.AddListener(Close);
        for (int i = 0; i < Mathf.Min(equipSlotButtons.Length, equipActions.Length); i++)
        {
            int slotIndex = i;
            equipActions[i] = () => EquipSelectedToSlot(slotIndex);
            equipSlotButtons[i]?.onClick.AddListener(equipActions[i]);
        }

        for (int i = 0; i < Mathf.Min(unequipSlotButtons.Length, unequipActions.Length); i++)
        {
            int slotIndex = i;
            unequipActions[i] = () => UnequipSlot(slotIndex);
            unequipSlotButtons[i]?.onClick.AddListener(unequipActions[i]);
        }
    }

    private void UnsubscribeButtons()
    {
        closeButton?.onClick.RemoveListener(Close);
        for (int i = 0; i < Mathf.Min(equipSlotButtons.Length, equipActions.Length); i++)
        {
            if (equipActions[i] != null)
            {
                equipSlotButtons[i]?.onClick.RemoveListener(equipActions[i]);
            }
        }

        for (int i = 0; i < Mathf.Min(unequipSlotButtons.Length, unequipActions.Length); i++)
        {
            if (unequipActions[i] != null)
            {
                unequipSlotButtons[i]?.onClick.RemoveListener(unequipActions[i]);
            }
        }
    }

    private void SubscribeInventory()
    {
        inventory?.OnCollectionProgressChanged.AddListener(Rebuild);
    }

    private void UnsubscribeInventory()
    {
        inventory?.OnCollectionProgressChanged.RemoveListener(Rebuild);
    }

    private void Close()
    {
        gameObject.SetActive(false);
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

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

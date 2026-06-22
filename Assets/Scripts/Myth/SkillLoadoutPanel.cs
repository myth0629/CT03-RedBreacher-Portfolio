using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SkillLoadoutPanel : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private PlayerController player;
    [SerializeField] private InventoryFacility inventory;
    [SerializeField] private SkillHangerFacility skillHanger;

    [Header("Skill List")]
    [SerializeField] private RectTransform skillContentRoot;
    [SerializeField] private PlayerLoadoutOptionButton skillButtonPrefab;

    [Header("Equipped Slots")]
    [SerializeField] private TMP_Text[] equippedSlotTexts = new TMP_Text[3];
    [SerializeField] private Button[] slotButtons = new Button[4];
    [SerializeField] private Image[] slotSkillIcons = new Image[4];
    [SerializeField] private GameObject[] slotLockedObjects = new GameObject[4];

    [Header("Detail")]
    [SerializeField] private Image detailSkillIcon;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillLevelText;
    [SerializeField] private TMP_Text duplicateProgressText;

    [Header("Commands")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button changeSkillButton;
    [SerializeField] private Button unequipSelectedButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text facilityLevelText;
    [SerializeField] private TMP_Text upgradeText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text upgradeConditionText;
    [SerializeField] private TMP_Text upgradeRemainingText;
    [SerializeField] private Image upgradeProgressFill;
    [SerializeField] private PlayerLoadoutSelectionPanel loadoutSelectionPanel;
    [SerializeField] private Button[] equipSlotButtons = new Button[3];
    [SerializeField] private Button[] unequipSlotButtons = new Button[3];

    private readonly List<PlayerLoadoutOptionButton> spawnedButtons = new List<PlayerLoadoutOptionButton>();
    private UnityAction[] slotSelectActions = new UnityAction[4];
    private readonly UnityAction[] equipActions = new UnityAction[3];
    private readonly UnityAction[] unequipActions = new UnityAction[3];
    private PlayerSkillConfig selectedSkill;
    private int selectedSlotIndex;
    private SkillHangerFacility subscribedFacility;
    private float observedUpgradeDuration;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeButtons();
        SubscribeInventory();
        SubscribeFactory();
        Rebuild();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
        UnsubscribeInventory();
        UnsubscribeFactory();
        ClearButtons();
    }

    private void Update()
    {
        RefreshUpgradeStatus();
    }

    public void EquipSelectedToSlot(int slotIndex)
    {
        if (player != null && selectedSkill != null && player.EquipSkill(slotIndex, selectedSkill))
        {
            selectedSlotIndex = slotIndex;
            Refresh();
        }
    }

    public void UnequipSlot(int slotIndex)
    {
        if (player != null && player.UnequipSkill(slotIndex))
        {
            if (selectedSlotIndex == slotIndex)
            {
                selectedSkill = null;
            }

            Refresh();
        }
    }

    public void UnequipSelectedSlot()
    {
        // 전역 장착 해제 버튼은 현재 선택된 슬롯만 해제한다.
        UnequipSlot(selectedSlotIndex);
    }

    public void OpenSkillSelectionPanel()
    {
        ResolveReferences();
        if (player != null && !player.IsSkillSlotUnlocked(selectedSlotIndex))
        {
            return;
        }

        loadoutSelectionPanel?.OpenSkillsForSelection(EquipSelectedSkillToSelectedSlot, GetSelectedSlotSkill());
    }

    public void UpgradeSkillHanger()
    {
        ResolveReferences();
        baseCampManager?.UpgradeSkillHanger();
        Refresh();
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0)
        {
            return;
        }

        selectedSlotIndex = slotIndex;
        if (player != null && !player.IsSkillSlotUnlocked(selectedSlotIndex))
        {
            Refresh();
            return;
        }

        selectedSkill = GetSelectedSlotSkill();
        Refresh();
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

            PlayerSkillConfig capturedSkill = skill;
            PlayerLoadoutOptionButton option = Instantiate(skillButtonPrefab, skillContentRoot);
            option.gameObject.SetActive(true);
            option.Bind(
                $"{skill.DisplayName} Lv.{inventory.GetSkillLevel(skill)}",
                "스킬",
                BuildSkillSummary(skill),
                skill == selectedSkill,
                () => SelectSkill(capturedSkill),
                skill.Icon);
            spawnedButtons.Add(option);
        }

        Refresh();
    }

    private void SelectSkill(PlayerSkillConfig skill)
    {
        selectedSkill = skill;
        Refresh();
    }

    private void EquipSelectedSkillToSelectedSlot(PlayerSkillConfig skill)
    {
        selectedSkill = skill;
        EquipSelectedToSlot(selectedSlotIndex);
    }

    private void Refresh()
    {
        EnsureSelectedSlotIndex();
        RefreshSkillOptionSelection();
        RefreshUpgradeStatus();

        for (int i = 0; i < equippedSlotTexts.Length; i++)
        {
            bool unlocked = player == null || player.IsSkillSlotUnlocked(i);
            PlayerSkillConfig equipped = player != null ? player.GetEquippedSkill(i) : null;
            SetText(
                equippedSlotTexts[i],
                !unlocked
                    ? $"스킬 격납고 Lv.{player.GetSkillSlotRequiredSkillHangerLevel(i)} 해금"
                    : equipped != null
                    ? $"{equipped.DisplayName} Lv.{GetSkillLevel(equipped)}"
                    : "비어 있음");
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            bool unlocked = player == null || player.IsSkillSlotUnlocked(i);
            PlayerSkillConfig equipped = player != null ? player.GetEquippedSkill(i) : null;
            SetInteractable(slotButtons[i], true);
            SetSkillIcon(slotSkillIcons, i, equipped);
            SetActive(slotLockedObjects, i, !unlocked);
        }

        for (int i = 0; i < equipSlotButtons.Length; i++)
        {
            if (IsSlotButton(equipSlotButtons[i]))
            {
                continue;
            }

            bool unlocked = player == null || player.IsSkillSlotUnlocked(i);
            SetInteractable(equipSlotButtons[i], unlocked && selectedSkill != null);
        }

        for (int i = 0; i < unequipSlotButtons.Length; i++)
        {
            bool unlocked = player == null || player.IsSkillSlotUnlocked(i);
            SetInteractable(unequipSlotButtons[i], unlocked && player != null && player.GetEquippedSkill(i) != null);
        }

        PlayerSkillConfig detailSkill = GetSelectedSlotSkill();
        bool selectedSlotUnlocked = player == null || player.IsSkillSlotUnlocked(selectedSlotIndex);
        SetInteractable(unequipSelectedButton, selectedSlotUnlocked && detailSkill != null);
        SetSkillIcon(detailSkillIcon, detailSkill);
        SetText(
            skillNameText,
            !selectedSlotUnlocked && player != null
                ? $"스킬 격납고 Lv.{player.GetSkillSlotRequiredSkillHangerLevel(selectedSlotIndex)} 해금"
                : detailSkill != null
                ? detailSkill.DisplayName
                : "스킬 없음");
        SetText(skillLevelText, selectedSlotUnlocked && detailSkill != null ? $"Lv.{GetSkillLevel(detailSkill)}" : string.Empty);

        if (!selectedSlotUnlocked || detailSkill == null)
        {
            SetText(duplicateProgressText, string.Empty);
            return;
        }

        SetText(duplicateProgressText, $"{detailSkill.GetCooldown(GetSkillLevel(detailSkill)):0.##}");
    }

    private int GetSkillLevel(PlayerSkillConfig skill)
    {
        return inventory != null ? Mathf.Max(1, inventory.GetSkillLevel(skill)) : 1;
    }

    private string BuildSkillSummary(PlayerSkillConfig skill)
    {
        if (skill == null || inventory == null)
        {
            return string.Empty;
        }

        int required = inventory.GetRequiredDuplicates(skill);
        return required > 0
            ? $"중복 {inventory.GetDuplicateProgress(skill)} / {required}"
            : "MAX";
    }

    private void RefreshSkillOptionSelection()
    {
        if (inventory == null)
        {
            return;
        }

        int optionIndex = 0;
        for (int i = 0; i < inventory.SkillConfigs.Count && optionIndex < spawnedButtons.Count; i++)
        {
            PlayerSkillConfig skill = inventory.SkillConfigs[i];
            if (skill == null)
            {
                continue;
            }

            spawnedButtons[optionIndex]?.SetSelected(skill == selectedSkill);
            optionIndex++;
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance != null
            ? BaseCampManager.Instance
            : FindFirstObjectByType<BaseCampManager>(FindObjectsInactive.Include);
        player ??= FindFirstObjectByType<PlayerController>();
        inventory ??= baseCampManager != null
            ? baseCampManager.Inventory
            : InventoryFacility.FindAny();
        skillHanger ??= baseCampManager != null
            ? baseCampManager.SkillHanger
            : FindFirstObjectByType<SkillHangerFacility>(FindObjectsInactive.Include);
        // 스킬 격납고 프리팹의 교체 버튼/선택 패널을 코드에서 한 번만 연결한다.
        changeSkillButton ??= FindChildComponentByName<Button>(transform, "ChangeSkill Button");
        changeSkillButton ??= FindChildComponentByName<Button>(transform, "Equip_Button");
        unequipSelectedButton ??= FindChildComponentByName<Button>(transform, "Unequip_Button");
        unequipSelectedButton ??= FindChildComponentByName<Button>(transform, "Unequip Button");
        upgradeButton ??= FindChildComponentByName<Button>(transform, "Upgrade_Button");
        upgradeButton ??= FindChildComponentByName<Button>(transform, "UpgradeButton");
        upgradeButton ??= FindChildComponentByName<Button>(transform, "Upgrade Button");
        loadoutSelectionPanel ??= FindChildComponentByName<PlayerLoadoutSelectionPanel>(transform, "LoadoutSelectionPanel_Skill");
        loadoutSelectionPanel ??= FindFirstObjectByType<PlayerLoadoutSelectionPanel>(FindObjectsInactive.Include);
        detailSkillIcon ??= FindChildComponentByName<Image>(FindChildTransformByName(transform, "Detail_SkillIcon"), "Skill_Icon");
        ResolveSlotButtons();
    }

    private void SubscribeButtons()
    {
        closeButton?.onClick.AddListener(Close);
        changeSkillButton?.onClick.AddListener(OpenSkillSelectionPanel);
        unequipSelectedButton?.onClick.AddListener(UnequipSelectedSlot);
        upgradeButton?.onClick.AddListener(UpgradeSkillHanger);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;
            slotSelectActions[i] = () => SelectSlot(slotIndex);
            slotButtons[i]?.onClick.AddListener(slotSelectActions[i]);
        }

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
        changeSkillButton?.onClick.RemoveListener(OpenSkillSelectionPanel);
        unequipSelectedButton?.onClick.RemoveListener(UnequipSelectedSlot);
        upgradeButton?.onClick.RemoveListener(UpgradeSkillHanger);
        for (int i = 0; i < Mathf.Min(slotButtons.Length, slotSelectActions.Length); i++)
        {
            if (slotSelectActions[i] != null)
            {
                slotButtons[i]?.onClick.RemoveListener(slotSelectActions[i]);
            }
        }

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

    private void SubscribeFactory()
    {
        if (skillHanger == null || subscribedFacility == skillHanger)
        {
            return;
        }

        UnsubscribeFactory();
        subscribedFacility = skillHanger;
        subscribedFacility.OnLevelChanged.AddListener(HandleFactoryLevelChanged);
        subscribedFacility.OnUpgradeStarted.AddListener(HandleFactoryUpgradeStarted);
        subscribedFacility.OnUpgradeCompleted.AddListener(HandleFactoryUpgradeCompleted);
    }

    private void UnsubscribeFactory()
    {
        if (subscribedFacility == null)
        {
            return;
        }

        subscribedFacility.OnLevelChanged.RemoveListener(HandleFactoryLevelChanged);
        subscribedFacility.OnUpgradeStarted.RemoveListener(HandleFactoryUpgradeStarted);
        subscribedFacility.OnUpgradeCompleted.RemoveListener(HandleFactoryUpgradeCompleted);
        subscribedFacility = null;
    }

    private void HandleFactoryLevelChanged(int level)
    {
        EnsureSelectedSlotIndex();
        Refresh();
    }

    private void HandleFactoryUpgradeStarted()
    {
        Refresh();
    }

    private void HandleFactoryUpgradeCompleted()
    {
        EnsureSelectedSlotIndex();
        Refresh();
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

    private static void SetInteractable(Button target, bool interactable)
    {
        if (target != null)
        {
            target.interactable = interactable;
        }
    }

    private bool IsSlotButton(Button button)
    {
        if (button == null || slotButtons == null)
        {
            return false;
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == button)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshUpgradeStatus()
    {
        ResolveReferences();
        if (skillHanger == null)
        {
            SetText(facilityLevelText, string.Empty);
            SetText(upgradeConditionText, "스킬 격납고 시설 참조 없음");
            SetText(upgradeRemainingText, upgradeConditionText == null ? "스킬 격납고 시설 참조 없음" : string.Empty);
            BaseCampUpgradeStatus.SetUpgradeProgress(upgradeProgressFill, null, ref observedUpgradeDuration);
            SetInteractable(upgradeButton, false);
            return;
        }

        SetText(facilityLevelText, $"Lv. {skillHanger.Level}");

        bool canShowCost = !skillHanger.IsUpgrading && skillHanger.Level < skillHanger.MaxLevel;
        BaseCampUpgradeButtonText.Set(
            upgradeText,
            upgradeCostText,
            skillHanger.Level >= skillHanger.MaxLevel ? "최대레벨" : "시설 업그레이드",
            skillHanger.UpgradeCost,
            canShowCost);

        if (baseCampManager != null)
        {
            int researchLabLevel = baseCampManager.CommandCenter != null
                ? baseCampManager.CommandCenter.Level
                : 1;
            string conditionText = BaseCampUpgradeStatus.BuildConditionText(
                skillHanger,
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel);
            SetText(upgradeConditionText, conditionText);
            SetInteractable(upgradeButton, skillHanger.CanStartUpgrade(
                baseCampManager.Credits,
                baseCampManager.CommanderLevel,
                researchLabLevel));
            SetText(upgradeRemainingText, skillHanger.IsUpgrading
                ? $"{skillHanger.UpgradeRemainingSeconds:0}s"
                : upgradeConditionText == null ? conditionText : string.Empty);
        }
        else
        {
            SetText(upgradeConditionText, "BaseCampManager 참조 없음");
            SetText(upgradeRemainingText, upgradeConditionText == null ? "BaseCampManager 참조 없음" : string.Empty);
            SetInteractable(upgradeButton, false);
        }

        BaseCampUpgradeStatus.SetUpgradeProgress(
            upgradeProgressFill,
            skillHanger,
            ref observedUpgradeDuration);
    }

    private void ResolveSlotButtons()
    {
        if (slotLockedObjects == null || slotLockedObjects.Length != slotButtons.Length)
        {
            slotLockedObjects = new GameObject[slotButtons.Length];
        }

        if (slotSkillIcons == null || slotSkillIcons.Length != slotButtons.Length)
        {
            slotSkillIcons = new Image[slotButtons.Length];
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            slotButtons[i] ??= FindChildComponentByName<Button>(transform, $"SkillButton_{i + 1}");
            // 슬롯 버튼 안의 SkillIcon 이미지를 현재 장착 스킬 아이콘으로 동기화한다.
            slotSkillIcons[i] ??= FindChildComponentByName<Image>(
                slotButtons[i] != null ? slotButtons[i].transform : null,
                "SkillIcon");
            slotLockedObjects[i] ??= FindChildTransformByName(
                slotButtons[i] != null ? slotButtons[i].transform : null,
                "locked")?.gameObject;
        }

        if (slotSelectActions == null || slotSelectActions.Length != slotButtons.Length)
        {
            slotSelectActions = new UnityAction[slotButtons.Length];
        }
    }

    private void EnsureSelectedSlotIndex()
    {
        if (player == null || player.IsSkillSlotUnlocked(selectedSlotIndex))
        {
            return;
        }

        for (int i = 0; i < player.SkillSlotCount; i++)
        {
            if (player.IsSkillSlotUnlocked(i))
            {
                selectedSlotIndex = i;
                return;
            }
        }
    }

    private PlayerSkillConfig GetSelectedSlotSkill()
    {
        return player != null ? player.GetEquippedSkill(selectedSlotIndex) : null;
    }

    private static void SetSkillIcon(Image target, PlayerSkillConfig skill)
    {
        if (target == null)
        {
            return;
        }

        Sprite icon = skill != null ? skill.Icon : null;
        target.sprite = icon;
        target.enabled = icon != null;
        target.preserveAspect = true;
        target.gameObject.SetActive(icon != null);
    }

    private static void SetSkillIcon(Image[] targets, int index, PlayerSkillConfig skill)
    {
        if (targets == null || index < 0 || index >= targets.Length)
        {
            return;
        }

        SetSkillIcon(targets[index], skill);
    }

    private static void SetActive(GameObject[] targets, int index, bool active)
    {
        if (targets == null || index < 0 || index >= targets.Length || targets[index] == null)
        {
            return;
        }

        targets[index].SetActive(active);
    }

    private static Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == childName)
            {
                return component;
            }
        }

        return null;
    }
}

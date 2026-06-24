using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GachaPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private WeaponGachaFacility weaponGacha;

    [Header("Weapon Draw Commands")]
    [SerializeField] private Button weaponDrawOnceButton;
    [SerializeField] private Button weaponDrawMultiButton;
    [SerializeField] private TMP_Text weaponDrawOnceCostText;
    [SerializeField] private TMP_Text weaponDrawMultiCostText;

    [Header("Skill Draw Commands")]
    [SerializeField] private Button skillDrawOnceButton;
    [SerializeField] private Button skillDrawMultiButton;
    [SerializeField] private TMP_Text skillDrawOnceCostText;
    [SerializeField] private TMP_Text skillDrawMultiCostText;

    // 기존의 Odd슬롯 프리팹 삭제를 까먹고 그대로 코드에 구현하는 바람에 연결 대상이 불명확해져
    // FormerlySerializedAs로 명시적 구현
    [Header("WeaponGachaDetailPopup_Slot")]
    [SerializeField] private Transform weaponOddRoot;
    [FormerlySerializedAs("weaponOddPrefab")]
    [SerializeField] private WeaponItemOdd weaponItemOddSlot;

    [Header("SkillGachaDetailPopup_Slot")]
    [SerializeField] private Transform skillOddRoot;
    [FormerlySerializedAs("skillOddPrefab")]
    [SerializeField] private SkillItemOdd skillItemOddSlot;

    [Header("Result Slots")]
    [SerializeField] private Transform resultSlotRoot;
    [SerializeField] private GachaResultSlot resultSlotPrefab;

    [Header("Result Panels")]
    [SerializeField] private GameObject _resultPanel;

    [Header("Tween")]
    [SerializeField] private GachaTweenTransition gachaTweenTransition;

    private readonly List<WeaponItemOdd> _weaponItemOdds = new List<WeaponItemOdd>();
    private readonly List<SkillItemOdd> _skillItemOdds = new List<SkillItemOdd>();

    private readonly List<GachaResultSlot> resultSlots = new List<GachaResultSlot>();
    private GachaCategory selectedCategory = GachaCategory.Weapon;
    private bool isDrawing;

    private void OnEnable()
    {
        ResolveReferences();
        weaponDrawOnceButton?.onClick.AddListener(DrawWeaponOnce);
        weaponDrawMultiButton?.onClick.AddListener(DrawWeaponMulti);
        skillDrawOnceButton?.onClick.AddListener(DrawSkillOnce);
        skillDrawMultiButton?.onClick.AddListener(DrawSkillMulti);
        Refresh();
    }

    private void OnDisable()
    {
        weaponDrawOnceButton?.onClick.RemoveListener(DrawWeaponOnce);
        weaponDrawMultiButton?.onClick.RemoveListener(DrawWeaponMulti);
        skillDrawOnceButton?.onClick.RemoveListener(DrawSkillOnce);
        skillDrawMultiButton?.onClick.RemoveListener(DrawSkillMulti);
        gachaTweenTransition?.ResetAll();
    }

    private void Update()
    {
        Refresh();
    }

    public void DrawWeaponOnce()
    {
        Draw(GachaCategory.Weapon, 1);
    }

    public void DrawWeaponMulti()
    {
        ResolveReferences();
        Draw(GachaCategory.Weapon, weaponGacha != null ? weaponGacha.MultiDrawCount : 10);
    }

    public void DrawSkillOnce()
    {
        Draw(GachaCategory.Skill, 1);
    }

    public void DrawSkillMulti()
    {
        ResolveReferences();
        Draw(GachaCategory.Skill, weaponGacha != null ? weaponGacha.MultiDrawCount : 10);
    }

    private void Draw(GachaCategory category, int count)
    {
        ResolveReferences();
        if (isDrawing || weaponGacha == null)
        {
            return;
        }

        isDrawing = true;
        // 각 버튼이 자신의 뽑기 종류를 직접 지정해 탭 상태와 섞이지 않게 한다.
        selectedCategory = category;
        CloseResultPanel();
        bool succeeded = weaponGacha.TryDraw(category, count);
        if (succeeded)
        {
            DailyMissionManager.ReportWeaponGachaDrawn(count);
            MainGuideMissionManager.ReportWeaponGachaDrawn(count);
            IReadOnlyList<GachaDrawResult> results = weaponGacha.LastResults;
            PlayDrawTween(category, () =>
            {
                ShowResults(results);
                isDrawing = false;
                Refresh();
            });
            return;
        }

        isDrawing = false;
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        bool connected = weaponGacha != null;
        int crystals = baseCampManager != null ? baseCampManager.CoreCrystals : 0;
        int multiCount = connected ? weaponGacha.MultiDrawCount : 10;
        
        RefreshDrawCostTexts(connected, multiCount);
        RefreshWeaponOddList();
        RefreshSkillOddList();

        bool canDrawWeaponOnce = connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Weapon, 1);
        bool canDrawWeaponMulti = connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Weapon, multiCount);
        bool canDrawSkillOnce = connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Skill, 1);
        bool canDrawSkillMulti = connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Skill, multiCount);

        SetButton(weaponDrawOnceButton, canDrawWeaponOnce);
        SetButton(weaponDrawMultiButton, canDrawWeaponMulti);
        SetButton(skillDrawOnceButton, canDrawSkillOnce);
        SetButton(skillDrawMultiButton, canDrawSkillMulti);
        SetCostTextColor(weaponDrawOnceCostText, canDrawWeaponOnce);
        SetCostTextColor(weaponDrawMultiCostText, canDrawWeaponMulti);
        SetCostTextColor(skillDrawOnceCostText, canDrawSkillOnce);
        SetCostTextColor(skillDrawMultiCostText, canDrawSkillMulti);
    }

    private void RefreshWeaponOddList()
    {
        if (weaponGacha == null || weaponOddRoot == null || weaponItemOddSlot == null)
        {
            return;
        }

        IReadOnlyList<WeaponGachaFacility.WeaponGachaEntry> entries = weaponGacha.GetWeaponEntries();
        float totalWeight = 0f;
        int validCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.WeaponGachaEntry entry = entries[i];
            if (!IsValidWeaponOddEntry(entry))
            {
                continue;
            }

            totalWeight += entry.weight;
            validCount++;
        }

        while (_weaponItemOdds.Count < validCount)
        {
            WeaponItemOdd slot = Instantiate(weaponItemOddSlot, weaponOddRoot);
            slot.gameObject.SetActive(true);
            _weaponItemOdds.Add(slot);
        }

        int slotIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.WeaponGachaEntry entry = entries[i];
            if (!IsValidWeaponOddEntry(entry))
            {
                continue;
            }

            WeaponItemOdd slot = _weaponItemOdds[slotIndex++];
            slot.gameObject.SetActive(true);
            slot.Setup(entry, totalWeight);
        }

        for (int i = slotIndex; i < _weaponItemOdds.Count; i++)
        {
            if (_weaponItemOdds[i] != null)
            {
                _weaponItemOdds[i].gameObject.SetActive(false);
            }
        }
    }

    private static bool IsValidWeaponOddEntry(WeaponGachaFacility.WeaponGachaEntry entry)
    {
        return entry != null
            && entry.enabled
            && entry.weaponConfig != null
            && entry.weight > 0f;
    }

    private void RefreshSkillOddList()
    {
        if (weaponGacha == null || skillOddRoot == null || skillItemOddSlot == null)
        {
            return;
        }

        IReadOnlyList<WeaponGachaFacility.SkillGachaEntry> entries = weaponGacha.GetSkillEntries();
        float totalWeight = 0f;
        int validCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.SkillGachaEntry entry = entries[i];
            if (!IsValidSkillOddEntry(entry))
            {
                continue;
            }

            totalWeight += entry.weight;
            validCount++;
        }

        while (_skillItemOdds.Count < validCount)
        {
            SkillItemOdd slot = Instantiate(skillItemOddSlot, skillOddRoot);
            slot.gameObject.SetActive(true);
            _skillItemOdds.Add(slot);
        }

        int slotIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.SkillGachaEntry entry = entries[i];
            if (!IsValidSkillOddEntry(entry))
            {
                continue;
            }

            SkillItemOdd slot = _skillItemOdds[slotIndex++];
            slot.gameObject.SetActive(true);
            slot.Setup(entry, totalWeight);
        }

        for (int i = slotIndex; i < _skillItemOdds.Count; i++)
        {
            if (_skillItemOdds[i] != null)
            {
                _skillItemOdds[i].gameObject.SetActive(false);
            }
        }
    }

    private static bool IsValidSkillOddEntry(WeaponGachaFacility.SkillGachaEntry entry)
    {
        return entry != null
            && entry.enabled
            && entry.skillConfig != null
            && entry.weight > 0f;
    }

    private void RefreshDrawCostTexts(bool connected, int multiCount)
    {
        if (!connected)
        {
            SetText(weaponDrawOnceCostText, "--");
            SetText(weaponDrawMultiCostText, "--");
            SetText(skillDrawOnceCostText, "--");
            SetText(skillDrawMultiCostText, "--");
            return;
        }

        SetText(weaponDrawOnceCostText, FormatCoreCrystalCost(weaponGacha.GetDrawCost(GachaCategory.Weapon, 1)));
        SetText(weaponDrawMultiCostText, FormatCoreCrystalCost(weaponGacha.GetDrawCost(GachaCategory.Weapon, multiCount)));
        SetText(skillDrawOnceCostText, FormatCoreCrystalCost(weaponGacha.GetDrawCost(GachaCategory.Skill, 1)));
        SetText(skillDrawMultiCostText, FormatCoreCrystalCost(weaponGacha.GetDrawCost(GachaCategory.Skill, multiCount)));
    }

    private static string FormatCoreCrystalCost(int cost)
    {
        return $"{cost:N0} 소모";
    }

    private void ShowResults(IReadOnlyList<GachaDrawResult> results)
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }

        gachaTweenTransition?.PlayResultPanel();

        int resultCount = Mathf.Min(10, results != null ? results.Count : 0);
        if (resultSlotRoot != null && resultSlotPrefab != null)
        {
            EnsureResultSlotCount(resultCount);
            for (int i = 0; i < resultSlots.Count; i++)
            {
                bool active = i < resultCount;
                resultSlots[i].gameObject.SetActive(active);
                if (active)
                {
                    resultSlots[i].Setup(results[i]);
                }
            }
        }
    }

    private void EnsureResultSlotCount(int count)
    {
        while (resultSlots.Count < count)
        {
            GachaResultSlot slot = Instantiate(resultSlotPrefab, resultSlotRoot);
            resultSlots.Add(slot);
        }
    }

    public void CloseResultPanel()
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(false);
        }

        gachaTweenTransition?.ResetAll();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        weaponGacha ??= FindFirstObjectByType<WeaponGachaFacility>(FindObjectsInactive.Include);
        gachaTweenTransition ??= GetComponentInChildren<GachaTweenTransition>(true);
        gachaTweenTransition ??= FindFirstObjectByType<GachaTweenTransition>(FindObjectsInactive.Include);
        if (gachaTweenTransition == null)
        {
            gachaTweenTransition = gameObject.AddComponent<GachaTweenTransition>();
        }
    }

    private void PlayDrawTween(GachaCategory category, System.Action onComplete)
    {
        ResolveReferences();
        if (gachaTweenTransition == null)
        {
            onComplete?.Invoke();
            return;
        }

        gachaTweenTransition.PlayTitle(category, onComplete);
    }

    private static void SetButton(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetCostTextColor(TMP_Text target, bool canDraw)
    {
        if (target != null)
        {
            target.color = canDraw ? Color.white : Color.red;
        }
    }
}

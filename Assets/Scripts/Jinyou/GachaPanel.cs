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

    [Header("Labels")]
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text tableText;

    [Header("Result Slots")]
    [SerializeField] private Transform resultSlotRoot;
    [SerializeField] private GachaResultSlot resultSlotPrefab;

    [Header("Result Panels")]
    [SerializeField] private GameObject _resultPanel;

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
        bool succeeded = weaponGacha.TryDraw(category, count);
        if (succeeded)
        {
            DailyMissionManager.ReportWeaponGachaDrawn(count);
            MainGuideMissionManager.ReportWeaponGachaDrawn(count);
            ShowResults(weaponGacha.LastResults);
        }
        else
        {
            ClearResultSlots();
            SetText(resultText, "코어 크리스탈이 부족하거나 확률표가 비어 있습니다.");
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

        SetText(currencyText, crystals.ToString());
        SetText(
            costText,
            connected
                ? $"1회 {weaponGacha.GetDrawCost(selectedCategory, 1)} / {multiCount}회 {weaponGacha.GetDrawCost(selectedCategory, multiCount)}"
                : "뽑기 시설이 연결되지 않았습니다.");
        RefreshDrawCostTexts(connected, multiCount);
        SetText(tableText, BuildTableText());
        RefreshWeaponOddList();
        RefreshSkillOddList();

        SetButton(
            weaponDrawOnceButton,
            connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Weapon, 1));
        SetButton(
            weaponDrawMultiButton,
            connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Weapon, multiCount));
        SetButton(
            skillDrawOnceButton,
            connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Skill, 1));
        SetButton(
            skillDrawMultiButton,
            connected && !isDrawing && weaponGacha.CanDraw(GachaCategory.Skill, multiCount));
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

            SetText(resultText, string.Empty);
            return;
        }

        SetText(resultText, BuildResultText(results));
    }

    private void SelectCategory(GachaCategory category)
    {
        selectedCategory = category;
        ClearResultSlots();
        SetText(resultText, string.Empty);
        CloseResultPanel();
        Refresh();
    }

    private void EnsureResultSlotCount(int count)
    {
        while (resultSlots.Count < count)
        {
            GachaResultSlot slot = Instantiate(resultSlotPrefab, resultSlotRoot);
            resultSlots.Add(slot);
        }
    }

    private void ClearResultSlots()
    {
        for (int i = 0; i < resultSlots.Count; i++)
        {
            if (resultSlots[i] != null)
            {
                resultSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private string BuildResultText(IReadOnlyList<GachaDrawResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return "결과 없음";
        }

        string text = string.Empty;
        for (int i = 0; i < results.Count; i++)
        {
            GachaDrawResult result = results[i];
            if (result?.grantResult == null)
            {
                continue;
            }

            string state = result.grantResult.isNew ? "NEW" : "중복";
            string progress = result.grantResult.requiredDuplicates > 0
                ? $"{result.grantResult.duplicateProgress}/{result.grantResult.requiredDuplicates}"
                : "MAX";
            text += $"{result.DisplayName} [{state}] Lv.{result.grantResult.currentLevel} {progress}\n";
        }

        return text.TrimEnd();
    }

    private string BuildTableText()
    {
        if (weaponGacha == null)
        {
            return string.Empty;
        }

        return selectedCategory == GachaCategory.Weapon
            ? BuildWeaponTableText(weaponGacha.GetWeaponEntries())
            : BuildSkillTableText(weaponGacha.GetSkillEntries());
    }

    private static string BuildWeaponTableText(IReadOnlyList<WeaponGachaFacility.WeaponGachaEntry> entries)
    {
        float totalWeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.WeaponGachaEntry entry = entries[i];
            if (entry != null && entry.enabled && entry.weaponConfig != null && entry.weight > 0f)
            {
                totalWeight += entry.weight;
            }
        }

        string text = string.Empty;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.WeaponGachaEntry entry = entries[i];
            if (entry == null || !entry.enabled || entry.weaponConfig == null || entry.weight <= 0f)
            {
                continue;
            }

            text += $"{entry.weaponConfig.DisplayName}: {entry.weight / totalWeight * 100f:0.##}%\n";
        }

        return string.IsNullOrWhiteSpace(text) ? "확률표가 비어 있습니다." : text.TrimEnd();
    }

    private static string BuildSkillTableText(IReadOnlyList<WeaponGachaFacility.SkillGachaEntry> entries)
    {
        float totalWeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.SkillGachaEntry entry = entries[i];
            if (entry != null && entry.enabled && entry.skillConfig != null && entry.weight > 0f)
            {
                totalWeight += entry.weight;
            }
        }

        string text = string.Empty;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponGachaFacility.SkillGachaEntry entry = entries[i];
            if (entry == null || !entry.enabled || entry.skillConfig == null || entry.weight <= 0f)
            {
                continue;
            }

            text += $"{entry.skillConfig.DisplayName}: {entry.weight / totalWeight * 100f:0.##}%\n";
        }

        return string.IsNullOrWhiteSpace(text) ? "확률표가 비어 있습니다." : text.TrimEnd();
    }

    public void CloseResultPanel()
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        weaponGacha ??= FindFirstObjectByType<WeaponGachaFacility>(FindObjectsInactive.Include);
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
}

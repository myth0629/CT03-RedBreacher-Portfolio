using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponGachaPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private WeaponGachaFacility weaponGacha;

    [Header("Category Tabs")]
    [SerializeField] private Button weaponTabButton;
    [SerializeField] private Button skillTabButton;
    [SerializeField] private GameObject weaponTabSelected;
    [SerializeField] private GameObject skillTabSelected;

    [Header("Commands")]
    [SerializeField] private Button drawOnceButton;
    [SerializeField] private Button drawMultiButton;
    [SerializeField] private Button closeButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text tableText;

    [Header("Result Slots")]
    [SerializeField] private Transform resultSlotRoot;
    [SerializeField] private GachaResultSlot resultSlotPrefab;

    private readonly List<GachaResultSlot> resultSlots = new List<GachaResultSlot>();
    private GachaCategory selectedCategory = GachaCategory.Weapon;
    private bool isDrawing;

    private void OnEnable()
    {
        ResolveReferences();
        weaponTabButton?.onClick.AddListener(SelectWeaponTab);
        skillTabButton?.onClick.AddListener(SelectSkillTab);
        drawOnceButton?.onClick.AddListener(DrawOnce);
        drawMultiButton?.onClick.AddListener(DrawMulti);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        weaponTabButton?.onClick.RemoveListener(SelectWeaponTab);
        skillTabButton?.onClick.RemoveListener(SelectSkillTab);
        drawOnceButton?.onClick.RemoveListener(DrawOnce);
        drawMultiButton?.onClick.RemoveListener(DrawMulti);
        closeButton?.onClick.RemoveListener(ClosePanel);
    }

    private void Update()
    {
        Refresh();
    }

    public void SelectWeaponTab()
    {
        SelectCategory(GachaCategory.Weapon);
    }

    public void SelectSkillTab()
    {
        SelectCategory(GachaCategory.Skill);
    }

    private void SelectCategory(GachaCategory category)
    {
        selectedCategory = category;
        ClearResultSlots();
        SetText(resultText, string.Empty);
        Refresh();
    }

    private void DrawOnce()
    {
        Draw(1);
    }

    private void DrawMulti()
    {
        ResolveReferences();
        Draw(weaponGacha != null ? weaponGacha.MultiDrawCount : 10);
    }

    private void Draw(int count)
    {
        ResolveReferences();
        if (isDrawing || weaponGacha == null)
        {
            return;
        }

        isDrawing = true;
        bool succeeded = weaponGacha.TryDraw(selectedCategory, count);
        if (succeeded)
        {
            DailyMissionManager.ReportWeaponGachaDrawn(count);
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
        SetText(tableText, BuildTableText());

        SetButton(drawOnceButton, connected && !isDrawing && weaponGacha.CanDraw(selectedCategory, 1));
        SetButton(drawMultiButton, connected && !isDrawing && weaponGacha.CanDraw(selectedCategory, multiCount));
        weaponTabSelected?.SetActive(selectedCategory == GachaCategory.Weapon);
        skillTabSelected?.SetActive(selectedCategory == GachaCategory.Skill);
    }

    private void ShowResults(IReadOnlyList<GachaDrawResult> results)
    {
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

    private void ClosePanel()
    {
        gameObject.SetActive(false);
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

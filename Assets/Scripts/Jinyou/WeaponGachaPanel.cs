using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponGachaPanel : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private WeaponGachaFacility weaponGacha;
    [SerializeField] private Button drawOnceButton;
    [SerializeField] private Button drawMultiButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text tableText;

    private void OnEnable()
    {
        ResolveReferences();
        drawOnceButton?.onClick.AddListener(DrawOnce);
        drawMultiButton?.onClick.AddListener(DrawMulti);
        closeButton?.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        drawOnceButton?.onClick.RemoveListener(DrawOnce);
        drawMultiButton?.onClick.RemoveListener(DrawMulti);
        closeButton?.onClick.RemoveListener(ClosePanel);
    }

    private void Update()
    {
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
        if (baseCampManager == null || weaponGacha == null)
        {
            return;
        }

        int availableCredits = baseCampManager.Credits;
        if (!weaponGacha.TryDraw(ref availableCredits, count))
        {
            SetText(resultText, "Not enough credits or draw table is empty");
            return;
        }

        baseCampManager.SetCreditsForFacility(availableCredits);
        SetText(resultText, BuildResultText());
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (weaponGacha == null)
        {
            SetText(costText, "Weapon Gacha not connected");
            SetButton(drawOnceButton, false);
            SetButton(drawMultiButton, false);
            return;
        }

        int credits = baseCampManager != null ? baseCampManager.Credits : 0;
        SetText(costText, $"Credits {credits} / 1 Draw {weaponGacha.DrawCost} / {weaponGacha.MultiDrawCount} Draw {weaponGacha.GetDrawCost(weaponGacha.MultiDrawCount)}");
        SetText(tableText, BuildTableText());
        SetButton(drawOnceButton, weaponGacha.CanDraw(credits, 1));
        SetButton(drawMultiButton, weaponGacha.CanDraw(credits, weaponGacha.MultiDrawCount));
    }

    private string BuildResultText()
    {
        if (weaponGacha == null || weaponGacha.LastDrawResults.Count == 0)
        {
            return "No Results";
        }

        string text = string.Empty;
        foreach (ProjectileConfig weapon in weaponGacha.LastDrawResults)
        {
            if (weapon != null)
            {
                text += $"{weapon.DisplayName}\n";
            }
        }

        return text.TrimEnd();
    }

    private string BuildTableText()
    {
        if (weaponGacha == null || weaponGacha.DrawTable.Count == 0)
        {
            return "Draw table is empty";
        }

        string text = string.Empty;
        foreach (WeaponGachaFacility.WeaponGachaEntry entry in weaponGacha.DrawTable)
        {
            if (entry?.weaponConfig == null || entry.weight <= 0f)
            {
                continue;
            }

            text += $"{entry.weaponConfig.DisplayName}: weight {entry.weight:0.##}\n";
        }

        return string.IsNullOrWhiteSpace(text) ? "Draw table is empty" : text.TrimEnd();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        weaponGacha ??= FindFirstObjectByType<WeaponGachaFacility>();
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

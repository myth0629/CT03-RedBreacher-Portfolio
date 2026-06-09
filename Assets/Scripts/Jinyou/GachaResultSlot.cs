using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text progressText;

    public void Setup(GachaDrawResult result)
    {
        if (result == null || result.grantResult == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = result.Icon;
            iconImage.enabled = result.Icon != null;
        }

        SetText(nameText, result.DisplayName);
        SetText(stateText, result.grantResult.isNew ? "NEW" : "중복");
        SetText(levelText, $"Lv.{result.grantResult.currentLevel}");
        SetText(
            progressText,
            result.grantResult.requiredDuplicates > 0
                ? $"{result.grantResult.duplicateProgress} / {result.grantResult.requiredDuplicates}"
                : result.grantResult.coreCrystalReward > 0
                    ? $"MAX +{result.grantResult.coreCrystalReward}"
                    : "MAX");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

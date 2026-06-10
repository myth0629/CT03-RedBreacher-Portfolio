using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GachaResultSlot : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("iconImage")]
    private Image _iconImage;

    [SerializeField, FormerlySerializedAs("nameText")]
    private TMP_Text _nameText;

    [SerializeField, FormerlySerializedAs("stateText")]
    private TMP_Text _stateText;

    [SerializeField, FormerlySerializedAs("levelText")]
    private TMP_Text _levelText;

    [SerializeField, FormerlySerializedAs("progressText")]
    private TMP_Text _progressText;

    [SerializeField]
    private Image _progressImage;

    public void Setup(GachaDrawResult result)
    {
        if (result == null || result.grantResult == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = result.Icon;
            _iconImage.enabled = result.Icon != null;
        }

        SetText(_nameText, result.DisplayName);
        SetText(_stateText, result.grantResult.isNew ? "NEW" : "중복");
        SetText(_levelText, $"Lv.{result.grantResult.currentLevel}");
        SetText(
            _progressText,
            result.grantResult.requiredDuplicates > 0
                ? $"{result.grantResult.duplicateProgress} / {result.grantResult.requiredDuplicates}"
                : result.grantResult.coreCrystalReward > 0
                    ? $"MAX +{result.grantResult.coreCrystalReward}"
                    : "MAX");

        if (_progressImage != null)
        {
            _progressImage.fillAmount = result.grantResult.requiredDuplicates > 0
                ? (float)result.grantResult.duplicateProgress / result.grantResult.requiredDuplicates
                : 1f;
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

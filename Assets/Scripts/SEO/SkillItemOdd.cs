using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillItemOdd : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text skillItemName;
    [SerializeField] private TMP_Text skillItemOddNum;

    public void Setup(WeaponGachaFacility.SkillGachaEntry entry, float totalWeight)
    {
        PlayerSkillConfig skill = entry != null ? entry.skillConfig : null;
        Sprite skillIcon = skill != null ? skill.Icon : null;

        if (icon != null)
        {
            icon.sprite = skillIcon;
            icon.enabled = skillIcon != null;
            icon.preserveAspect = true;
        }

        SetText(skillItemName, skill != null ? skill.DisplayName : string.Empty);

        float probability = entry != null && totalWeight > 0f
            ? Mathf.Max(0f, entry.weight) / totalWeight * 100f
            : 0f;
        SetText(skillItemOddNum, $"{probability:0.##}%");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

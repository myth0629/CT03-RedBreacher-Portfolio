using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponItemOdd : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text weaponItemName;
    [SerializeField] private TMP_Text weaponItemType;
    [SerializeField] private TMP_Text weaponItemOddNum;

    public void Setup(WeaponGachaFacility.WeaponGachaEntry entry, float totalWeight)
    {
        ProjectileConfig weapon = entry != null ? entry.weaponConfig : null;
        Sprite weaponIcon = weapon != null ? weapon.Icon : null;

        if (icon != null)
        {
            icon.sprite = weaponIcon;
            icon.enabled = weaponIcon != null;
            icon.preserveAspect = true;
        }

        SetText(weaponItemName, weapon != null ? weapon.DisplayName : string.Empty);
        SetText(weaponItemType, weapon != null ? weapon.WeaponCategory : string.Empty);

        float probability = entry != null && totalWeight > 0f
            ? Mathf.Max(0f, entry.weight) / totalWeight * 100f
            : 0f;
        SetText(weaponItemOddNum, $"{probability:0.##}%");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}

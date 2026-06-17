using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLoadoutOptionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private RawImage droneIcon;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private GameObject selectedMark;

    private System.Action onClick;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        }
    }

    private void OnEnable()
    {
        button?.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        button?.onClick.RemoveListener(HandleClick);
    }

    public void Bind(
        string title,
        string category,
        string summary,
        bool selected,
        System.Action clickAction,
        Sprite iconSprite = null,
        DroneConfig droneConfig = null)
    {
        onClick = clickAction;
        SetText(nameText, title);
        SetText(categoryText, category);
        SetText(summaryText, summary);
        SetWeaponIcon(iconSprite);
        SetDroneIcon(droneConfig);
        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
        {
            selectedMark.SetActive(selected);
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke();
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    // 무기는 스프라이트 이미지로 동기화
    private void SetWeaponIcon(Sprite sprite)
    {
        if (weaponIcon == null)
        {
            return;
        }

        weaponIcon.sprite = sprite;
        weaponIcon.enabled = sprite != null;
        weaponIcon.preserveAspect = true;
        weaponIcon.gameObject.SetActive(sprite != null);
    }

    // 드론은 프리팹으로 렌더링헤서 이미지 동기화
    private void SetDroneIcon(DroneConfig droneConfig)
    {
        if (droneIcon == null)
        {
            return;
        }

        GameObject prefab = droneConfig != null ? droneConfig.DronePrefab : null;
        if (prefab == null)
        {
            droneIcon.texture = null;
            droneIcon.color = Color.clear;
            droneIcon.gameObject.SetActive(false);
            return;
        }

        RenderTexture preview = UnitPreviewRenderer.Instance.GetPreview(prefab);
        droneIcon.texture = preview;
        droneIcon.color = preview != null ? Color.white : Color.clear;
        droneIcon.gameObject.SetActive(preview != null);
    }
}

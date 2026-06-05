using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLoadoutOptionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
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

    public void Bind(string title, string category, string summary, bool selected, System.Action clickAction)
    {
        onClick = clickAction;
        SetText(nameText, title);
        SetText(categoryText, category);
        SetText(summaryText, summary);
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
}

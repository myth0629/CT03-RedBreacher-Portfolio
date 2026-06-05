using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLoadoutPopupButtonBinder : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button droneButton;

    [Header("Target")]
    [SerializeField] private PlayerLoadoutSelectionPanel selectionPanel;

    private void OnEnable()
    {
        weaponButton?.onClick.AddListener(OpenWeapons);
        droneButton?.onClick.AddListener(OpenDrones);
    }

    private void OnDisable()
    {
        weaponButton?.onClick.RemoveListener(OpenWeapons);
        droneButton?.onClick.RemoveListener(OpenDrones);
    }

    private void OpenWeapons()
    {
        selectionPanel?.OpenWeapons();
    }

    private void OpenDrones()
    {
        selectionPanel?.OpenDrones();
    }
}

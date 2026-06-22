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

    private PanelTweenTransition panelTransition;

    private void OnEnable()
    {
        EnsurePanelTransition();
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

    private void EnsurePanelTransition()
    {
        // 플레이어 팝업도 prefab 수정 없이 공통 패널 연출을 적용한다.
        panelTransition ??= GetComponent<PanelTweenTransition>();
        if (panelTransition == null)
        {
            panelTransition = gameObject.AddComponent<PanelTweenTransition>();
        }
    }
}

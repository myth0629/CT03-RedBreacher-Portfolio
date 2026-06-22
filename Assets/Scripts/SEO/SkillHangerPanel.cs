using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillHangerPanel : MonoBehaviour
{
    [Header("Buttons")] 
    [SerializeField] private Button[] skillButtons;
    [SerializeField] private Button changeSkillButton;
    
    [Header("Skill Info SubPanel")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillLevelText;
    [SerializeField] private TMP_Text skillCooldownText;
    [SerializeField] private TMP_Text skillDescriptionText;
}

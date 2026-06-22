using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillLoadoutSelectionPanel : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerController player;
    
    [Header("Buttons")]
    [SerializeField] private Button equipButton;
    
    [Header("Panel")]
    [SerializeField] private GameObject selectionRoot;
    [SerializeField] private Transform contentRoot;
    
    [Header("Detail")] 
    [SerializeField] private Image detailIconSkill;
    [SerializeField] private TMP_Text detailSkillNameText;
    [SerializeField] private TMP_Text detailSkillLevelText;
    [SerializeField] private TMP_Text detailSkillCooldownText;
    [SerializeField] private TMP_Text detailSkillDescriptionText;
}

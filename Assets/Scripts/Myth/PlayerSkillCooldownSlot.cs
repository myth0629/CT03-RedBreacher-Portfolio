using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSkillCooldownSlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private TMP_Text _cooldownText;

    private PlayerAutoSkillController _skillController;
    private PlayerSkillConfig _skillConfig;

    public void Setup(PlayerAutoSkillController controller, PlayerSkillConfig skill)
    {
        _skillController = controller;
        _skillConfig = skill;

        if (skill == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = skill.Icon;
            _iconImage.enabled = skill.Icon != null;
        }

        UpdateCooldownVisuals();
    }

    private void Update()
    {
        UpdateCooldownVisuals();
    }

    private void UpdateCooldownVisuals()
    {
        if (_skillController == null || _skillConfig == null)
        {
            return;
        }

        float remaining = _skillController.GetRemainingCooldown(_skillConfig);
        float progress = _skillController.GetCooldownProgress01(_skillConfig);

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = progress;
        }

        if (_cooldownText != null)
        {
            if (remaining > 0f)
            {
                _cooldownText.gameObject.SetActive(true);
                // 1초 이상 남으면 소수점을 버리고 올림 정수로, 1초 미만이면 소수점 첫째자리까지 표시
                _cooldownText.text = remaining >= 1f ? $"{Mathf.CeilToInt(remaining)}" : $"{remaining:F1}";
            }
            else
            {
                _cooldownText.gameObject.SetActive(false);
            }
        }
    }
}

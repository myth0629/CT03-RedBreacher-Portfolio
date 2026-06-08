using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillCooldownHud : MonoBehaviour
{
    [SerializeField] private PlayerSkillCooldownSlot _slotPrefab;
    [SerializeField] private Transform _slotContainer;

    private PlayerController _player;
    private PlayerAutoSkillController _skillController;
    private readonly List<PlayerSkillCooldownSlot> _activeSlots = new List<PlayerSkillCooldownSlot>();

    private void Awake()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
        }

        ResolveSkillController();
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                ResolveSkillController();
            }
            return;
        }

        if (_skillController == null)
        {
            ResolveSkillController();
            return;
        }

        // 스킬 장착 목록이 바뀔 경우를 위해 슬롯 수와 스킬 수를 체크하여 리빌드
        if (_activeSlots.Count != _skillController.EquippedSkills.Count)
        {
            RebuildSlots();
        }
    }

    private void ResolveSkillController()
    {
        if (_player != null)
        {
            _skillController = _player.GetComponent<PlayerAutoSkillController>();
            if (_skillController != null)
            {
                RebuildSlots();
            }
        }
    }

    private void RebuildSlots()
    {
        // 기존 활성화된 슬롯 정리
        for (int i = 0; i < _activeSlots.Count; i++)
        {
            if (_activeSlots[i] != null)
            {
                Destroy(_activeSlots[i].gameObject);
            }
        }
        _activeSlots.Clear();

        if (_skillController == null || _slotPrefab == null || _slotContainer == null)
        {
            return;
        }

        IReadOnlyList<PlayerSkillConfig> skills = _skillController.EquippedSkills;
        for (int i = 0; i < skills.Count; i++)
        {
            PlayerSkillConfig skill = skills[i];
            if (skill == null)
            {
                continue;
            }

            PlayerSkillCooldownSlot newSlot = Instantiate(_slotPrefab, _slotContainer);
            newSlot.Setup(_skillController, skill);
            _activeSlots.Add(newSlot);
        }
    }
}

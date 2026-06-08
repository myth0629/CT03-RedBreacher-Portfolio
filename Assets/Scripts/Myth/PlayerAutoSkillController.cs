using System.Collections.Generic;
using UnityEngine;

public class PlayerAutoSkillController : MonoBehaviour
{
    private readonly List<PlayerSkillConfig> equippedSkills = new List<PlayerSkillConfig>();
    private readonly Dictionary<PlayerSkillConfig, float> nextCastTimes =
        new Dictionary<PlayerSkillConfig, float>();
    private readonly Dictionary<PlayerSkillConfig, float> nextSearchTimes =
        new Dictionary<PlayerSkillConfig, float>();

    private PlayerController player;

    public IReadOnlyList<PlayerSkillConfig> EquippedSkills => equippedSkills;
    public int LoadoutVersion { get; private set; }

    public float GetRemainingCooldown(PlayerSkillConfig skill)
    {
        if (skill == null || !nextCastTimes.TryGetValue(skill, out float nextCastTime))
        {
            return 0f;
        }

        return Mathf.Max(0f, nextCastTime - Time.time);
    }

    public float GetCooldownProgress01(PlayerSkillConfig skill)
    {
        if (skill == null || skill.Cooldown <= 0f)
        {
            return 0f;
        }

        float remaining = GetRemainingCooldown(skill);
        return Mathf.Clamp01(remaining / skill.Cooldown);
    }

    public void Initialize(PlayerController owner, IReadOnlyList<PlayerSkillConfig> skills)
    {
        player = owner;
        equippedSkills.Clear();
        nextCastTimes.Clear();
        nextSearchTimes.Clear();
        LoadoutVersion++;

        if (skills == null)
        {
            return;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            PlayerSkillConfig skill = skills[i];
            if (skill == null || equippedSkills.Contains(skill))
            {
                continue;
            }

            equippedSkills.Add(skill);
            nextCastTimes[skill] = Time.time + skill.Cooldown;
            nextSearchTimes[skill] = 0f;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        if (player == null || player.Health == null || player.Health.IsDead)
        {
            return;
        }

        for (int i = 0; i < equippedSkills.Count; i++)
        {
            TryCast(equippedSkills[i]);
        }
    }

    private void TryCast(PlayerSkillConfig skill)
    {
        if (skill == null
            || !nextCastTimes.TryGetValue(skill, out float nextCastTime)
            || Time.time < nextCastTime
            || Time.time < nextSearchTimes[skill])
        {
            return;
        }

        if (!PlayerSkillCombat.TryFindBestAreaCenter(
                player.transform.position,
                skill.CastRange,
                skill.EffectRadius,
                skill.MinimumEnemyCount,
                out Vector3 targetPosition))
        {
            // 준비된 스킬은 유지하되 적 전체 탐색은 짧은 주기로 제한한다.
            nextSearchTimes[skill] = Time.time + 0.25f;
            return;
        }

        bool castSucceeded = skill.SkillType switch
        {
            PlayerSkillType.AutoTurret => AutoTurretSkill.Spawn(player, skill, targetPosition),
            _ => BombardmentSkill.Cast(player, skill, targetPosition)
        };

        if (castSucceeded)
        {
            // 설치 터렛은 사라진 시점부터 재사용 쿨타임을 계산한다.
            float cooldownStartDelay = skill.SkillType == PlayerSkillType.AutoTurret
                ? skill.TurretDuration
                : 0f;
            nextCastTimes[skill] = Time.time + cooldownStartDelay + skill.Cooldown;
            nextSearchTimes[skill] = 0f;
        }
    }
}

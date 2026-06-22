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
        float cooldown = GetCooldown(skill);
        if (skill == null || cooldown <= 0f)
        {
            return 0f;
        }

        float remaining = GetRemainingCooldown(skill);
        return Mathf.Clamp01(remaining / cooldown);
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
            nextCastTimes[skill] = Time.time + GetCooldown(skill);
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
            PlayerSkillType.MissileTurret => MissileTurretSkill.Spawn(player, skill, targetPosition),
            PlayerSkillType.StealthBomber => StealthBomberSkill.Cast(player, skill, targetPosition),
            PlayerSkillType.AttackHelicopter => AttackHelicopterSkill.Spawn(player, skill, targetPosition),
            _ => BombardmentSkill.Cast(player, skill, targetPosition)
        };

        if (castSucceeded)
        {
            // 설치 터렛은 사라진 시점부터 재사용 쿨타임을 계산한다.
            float cooldownStartDelay = GetCooldownStartDelay(skill);
            nextCastTimes[skill] = Time.time + cooldownStartDelay + GetCooldown(skill);
            nextSearchTimes[skill] = 0f;
        }
    }

    private static float GetCooldownStartDelay(PlayerSkillConfig skill)
    {
        // 지속형 소환 스킬은 종료 시점부터 쿨타임을 시작해 중첩 소환을 막는다.
        return skill.SkillType switch
        {
            PlayerSkillType.AutoTurret => skill.TurretDuration,
            PlayerSkillType.MissileTurret => skill.MissileTurretDuration,
            PlayerSkillType.AttackHelicopter => skill.AttackHelicopterDuration,
            _ => 0f
        };
    }

    private float GetCooldown(PlayerSkillConfig skill)
    {
        int level = player != null ? player.GetSkillLevel(skill) : 1;
        return skill != null ? skill.GetCooldown(level) : 0f;
    }
}

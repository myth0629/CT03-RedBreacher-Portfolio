using UnityEngine;
using UnityEngine.Events;

public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private int level = 1;
    [SerializeField] private float experience;
    [SerializeField] private float experienceToNextLevel = 100f;
    [SerializeField] private float experienceRequirementMultiplier = 1.2f;
    [SerializeField] private int traitPoints;
    [SerializeField] private float enemyExperienceBase = 10f;
    [SerializeField] private float enemyExperienceStageMultiplier = 1.15f;

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<float> OnExperienceChanged = new UnityEvent<float>();
    public UnityEvent<int> OnTraitPointsChanged = new UnityEvent<int>();

    public int Level => level;
    public float Experience => experience;
    public float ExperienceToNextLevel => experienceToNextLevel;
    public float ExperienceProgress => experienceToNextLevel > 0f ? Mathf.Clamp01(experience / experienceToNextLevel) : 1f;
    public int TraitPoints => traitPoints;

    public float GetEnemyExperienceReward(int stage)
    {
        int stageIndex = Mathf.Max(0, stage - 1);
        return enemyExperienceBase * Mathf.Pow(enemyExperienceStageMultiplier, stageIndex);
    }

    public void AddEnemyKillExperience(int stage)
    {
        AddExperience(GetEnemyExperienceReward(stage));
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        experience += amount;

        while (experienceToNextLevel > 0f && experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel;
            LevelUp();
        }

        OnExperienceChanged.Invoke(experience);
    }

    public bool TrySpendTraitPoint()
    {
        if (traitPoints <= 0)
        {
            return false;
        }

        traitPoints--;
        OnTraitPointsChanged.Invoke(traitPoints);
        return true;
    }

    public void AddTraitPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        traitPoints += amount;
        OnTraitPointsChanged.Invoke(traitPoints);
    }

    private void LevelUp()
    {
        level++;
        traitPoints++;
        experienceToNextLevel = Mathf.Max(1f, experienceToNextLevel * experienceRequirementMultiplier);
        Debug.Log($"Player level up: Lv.{level}, trait points {traitPoints}");
        OnLevelChanged.Invoke(level);
        OnTraitPointsChanged.Invoke(traitPoints);
    }

    [ContextMenu("Debug/Add Stage 1 Enemy Kill Experience")]
    private void DebugAddStageOneEnemyKillExperience()
    {
        AddEnemyKillExperience(1);
    }

    [ContextMenu("Debug/Add Trait Point")]
    private void DebugAddTraitPoint()
    {
        AddTraitPoints(1);
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        experience = Mathf.Max(0f, experience);
        experienceToNextLevel = Mathf.Max(1f, experienceToNextLevel);
        experienceRequirementMultiplier = Mathf.Max(1f, experienceRequirementMultiplier);
        traitPoints = Mathf.Max(0, traitPoints);
        enemyExperienceBase = Mathf.Max(0f, enemyExperienceBase);
        enemyExperienceStageMultiplier = Mathf.Max(1f, enemyExperienceStageMultiplier);
    }
}

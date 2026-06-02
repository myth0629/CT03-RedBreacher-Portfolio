using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExperience;
    [SerializeField] private float experienceToNextLevel = 100f;
    [SerializeField] private float experienceGrowthRate = 1.2f;
    [SerializeField] private int statPoints;

    public int Level => level;
    public float CurrentExperience => currentExperience;
    public float ExperienceToNextLevel => experienceToNextLevel;
    public float ExperienceProgress01 => experienceToNextLevel > 0f ? Mathf.Clamp01(currentExperience / experienceToNextLevel) : 0f;
    public int StatPoints => statPoints;

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        // 적 처치 경험치를 누적하고 필요하면 여러 레벨업도 한 번에 처리한다.
        currentExperience += amount;
        while (currentExperience >= experienceToNextLevel)
        {
            currentExperience -= experienceToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        statPoints++;
        experienceToNextLevel = Mathf.Max(1f, experienceToNextLevel * experienceGrowthRate);

        Debug.Log($"플레이어 레벨업: Lv.{level}, 특성 포인트 {statPoints}");
    }
}

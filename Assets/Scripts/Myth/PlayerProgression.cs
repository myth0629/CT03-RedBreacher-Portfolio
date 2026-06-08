using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    private const string LevelKey = "PlayerProgression.Level";
    private const string CurrentExperienceKey = "PlayerProgression.CurrentExperience";
    private const string ExperienceToNextLevelKey = "PlayerProgression.ExperienceToNextLevel";
    private const string StatPointsKey = "PlayerProgression.StatPoints";

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExperience;
    [SerializeField] private float experienceToNextLevel = 100f;
    [SerializeField] private float experienceGrowthRate = 1.2f;
    [SerializeField] private int statPoints;
    [SerializeField] private bool saveToPlayerPrefs = true;

    private int initialLevel;
    private float initialExperienceToNextLevel;

    public int Level => level;
    public float CurrentExperience => currentExperience;
    public float ExperienceToNextLevel => experienceToNextLevel;
    public float ExperienceProgress01 => experienceToNextLevel > 0f ? Mathf.Clamp01(currentExperience / experienceToNextLevel) : 0f;
    public int StatPoints => statPoints;

    private void Awake()
    {
        initialLevel = Mathf.Max(1, level);
        initialExperienceToNextLevel = Mathf.Max(1f, experienceToNextLevel);
        Load();
        AchievementManager.ReportPlayerLevelReached(level);
    }

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

        Save();
    }

    public void ResetProgression()
    {
        // 디버그 초기화는 Inspector 기본값 기준으로 레벨/경험치를 되돌린다.
        level = initialLevel;
        currentExperience = 0f;
        experienceToNextLevel = initialExperienceToNextLevel;
        statPoints = 0;
        Save();
    }

    public bool TrySpendStatPoint()
    {
        if (statPoints <= 0)
        {
            return false;
        }

        // 코어 투자에서 사용하는 특성 포인트 차감은 진행도 저장과 함께 처리한다.
        statPoints--;
        Save();
        return true;
    }

    public bool TrySpendStatPoints(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
        {
            return true;
        }

        if (statPoints < amount)
        {
            return false;
        }

        statPoints -= amount;
        Save();
        return true;
    }

    public void AddStatPoints(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
        {
            return;
        }

        statPoints += amount;
        Save();
    }

    private void LevelUp()
    {
        level++;
        statPoints++;
        experienceToNextLevel = Mathf.Max(1f, experienceToNextLevel * experienceGrowthRate);
        AchievementManager.ReportPlayerLevelReached(level);

        Debug.Log($"플레이어 레벨업: Lv.{level}, 특성 포인트 {statPoints}");
    }

    private void Load()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        level = Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, level));
        currentExperience = Mathf.Max(0f, PlayerPrefs.GetFloat(CurrentExperienceKey, currentExperience));
        experienceToNextLevel = Mathf.Max(1f, PlayerPrefs.GetFloat(ExperienceToNextLevelKey, experienceToNextLevel));
        statPoints = Mathf.Max(0, PlayerPrefs.GetInt(StatPointsKey, statPoints));
    }

    private void Save()
    {
        if (!saveToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetInt(LevelKey, level);
        PlayerPrefs.SetFloat(CurrentExperienceKey, currentExperience);
        PlayerPrefs.SetFloat(ExperienceToNextLevelKey, experienceToNextLevel);
        PlayerPrefs.SetInt(StatPointsKey, statPoints);
        PlayerPrefs.Save();
    }
}

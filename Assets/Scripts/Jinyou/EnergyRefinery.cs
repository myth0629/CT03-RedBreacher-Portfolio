using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnergyRefinery : MonoBehaviour, IBaseCampFacility
{
    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Production")]
    [SerializeField] private int storedCredits;
    [SerializeField] private int storageCapacity = 1000;
    [SerializeField] private List<int> storageCapacityByLevel = new List<int>();
    [SerializeField] private float creditsPerMinute = 60f;
    [SerializeField] private List<float> creditsPerMinuteByLevel = new List<float>();
    [SerializeField] private bool produceWhilePlaying = true;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 250;
    [SerializeField] private List<int> upgradeCostByLevel = new List<int>();
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 1;
    [SerializeField] private List<int> requiredResearchLabLevelByLevel = new List<int>();
    [SerializeField] private float upgradeDurationSeconds = 10f;
    [SerializeField] private List<float> upgradeDurationSecondsByLevel = new List<float>();

    [Header("Events")]
    public UnityEvent<int> OnCreditsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private float productionBuffer;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int StoredCredits => storedCredits;
    public int StorageCapacity => GetStorageCapacityForCurrentLevel();
    public int UpgradeCost => GetUpgradeCostForCurrentLevel();
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => GetRequiredResearchLabLevelForCurrentUpgrade();
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public float CreditsPerMinute => GetCreditsPerMinuteForCurrentLevel();
    public bool IsStorageFull => storedCredits >= StorageCapacity;

    private void Awake()
    {
        NormalizeConfiguredValues();
    }

    private void Update()
    {
        if (produceWhilePlaying)
        {
            Produce(Time.deltaTime);
        }

        TickUpgrade(Time.deltaTime);
    }

    public void Produce(float deltaTime)
    {
        productionBuffer += CreditsPerMinute / 60f * deltaTime;
        int amount = Mathf.FloorToInt(productionBuffer);

        if (amount <= 0)
        {
            return;
        }

        productionBuffer -= amount;
        storedCredits = Mathf.Clamp(storedCredits + amount, 0, StorageCapacity);
        OnCreditsChanged.Invoke(storedCredits);
    }

    public int CollectCredits()
    {
        int amount = storedCredits;
        storedCredits = 0;
        OnCreditsChanged.Invoke(storedCredits);
        return amount;
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= UpgradeCost && commanderLevel >= requiredCommanderLevel;
    }

    public int GetLevelLimit(int researchLabLevel)
    {
        return Mathf.Min(maxLevel, Mathf.Max(1, researchLabLevel) * 2);
    }

    public bool CanStartUpgrade(int availableCredits, int commanderLevel, int researchLabLevel)
    {
        return CanUpgrade(availableCredits, commanderLevel)
            && researchLabLevel >= RequiredResearchLabLevel
            && level < GetLevelLimit(researchLabLevel);
    }

    public bool TryStartUpgrade(ref int availableCredits, int commanderLevel, int researchLabLevel)
    {
        if (!CanStartUpgrade(availableCredits, commanderLevel, researchLabLevel))
        {
            return false;
        }

        availableCredits -= UpgradeCost;
        StartUpgradeTimer();
        return true;
    }

    public void Upgrade()
    {
        if (isUpgrading)
        {
            CompleteUpgrade();
            return;
        }

        if (level >= maxLevel)
        {
            return;
        }

        OnUpgradeStarted.Invoke();
        CompleteUpgrade();
    }

    public void CompleteUpgradeImmediately()
    {
        Upgrade();
    }

    private void StartUpgradeTimer()
    {
        OnUpgradeStarted.Invoke();

        currentUpgradeDurationSeconds = GetUpgradeDurationForCurrentLevel();

        if (currentUpgradeDurationSeconds <= 0f)
        {
            CompleteUpgrade();
            return;
        }

        isUpgrading = true;
        upgradeRemainingSeconds = currentUpgradeDurationSeconds;
    }

    private void TickUpgrade(float deltaTime)
    {
        if (!isUpgrading)
        {
            return;
        }

        upgradeRemainingSeconds -= deltaTime;

        if (upgradeRemainingSeconds <= 0f)
        {
            CompleteUpgrade();
        }
    }

    private void CompleteUpgrade()
    {
        if (level >= maxLevel)
        {
            isUpgrading = false;
            upgradeRemainingSeconds = 0f;
            return;
        }

        isUpgrading = false;
        upgradeRemainingSeconds = 0f;
        currentUpgradeDurationSeconds = 0f;
        level++;
        storedCredits = Mathf.Clamp(storedCredits, 0, StorageCapacity);
        requiredCommanderLevel++;
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private int GetUpgradeCostForCurrentLevel()
    {
        if (level >= maxLevel)
        {
            return 0;
        }

        int index = Mathf.Max(0, level - 1);
        if (index < upgradeCostByLevel.Count)
        {
            return Mathf.Max(0, upgradeCostByLevel[index]);
        }

        return Mathf.Max(0, upgradeCost);
    }

    private float GetCreditsPerMinuteForCurrentLevel()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < creditsPerMinuteByLevel.Count)
        {
            return Mathf.Max(0f, creditsPerMinuteByLevel[index]);
        }

        return Mathf.Max(0f, creditsPerMinute);
    }

    private int GetStorageCapacityForCurrentLevel()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < storageCapacityByLevel.Count)
        {
            return Mathf.Max(1, storageCapacityByLevel[index]);
        }

        return Mathf.Max(1, storageCapacity);
    }

    private float GetUpgradeDurationForCurrentLevel()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < upgradeDurationSecondsByLevel.Count)
        {
            return Mathf.Max(0f, upgradeDurationSecondsByLevel[index]);
        }

        return upgradeDurationSeconds;
    }

    private int GetRequiredResearchLabLevelForCurrentUpgrade()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < requiredResearchLabLevelByLevel.Count)
        {
            return Mathf.Max(1, requiredResearchLabLevelByLevel[index]);
        }

        return Mathf.Max(1, requiredResearchLabLevel);
    }

    private void NormalizeUpgradeDurations()
    {
        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (upgradeDurationSecondsByLevel.Count < targetCount)
        {
            upgradeDurationSecondsByLevel.Add(upgradeDurationSeconds);
        }

        for (int i = 0; i < upgradeDurationSecondsByLevel.Count; i++)
        {
            upgradeDurationSecondsByLevel[i] = Mathf.Max(0f, upgradeDurationSecondsByLevel[i]);
        }
    }

    private void NormalizeUpgradeCosts()
    {
        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (upgradeCostByLevel.Count < targetCount)
        {
            upgradeCostByLevel.Add(upgradeCost);
        }

        for (int i = 0; i < upgradeCostByLevel.Count; i++)
        {
            upgradeCostByLevel[i] = Mathf.Max(0, upgradeCostByLevel[i]);
        }
    }

    private void NormalizeCreditsPerMinute()
    {
        int targetCount = Mathf.Max(1, maxLevel);
        while (creditsPerMinuteByLevel.Count < targetCount)
        {
            creditsPerMinuteByLevel.Add(creditsPerMinute);
        }

        for (int i = 0; i < creditsPerMinuteByLevel.Count; i++)
        {
            creditsPerMinuteByLevel[i] = Mathf.Max(0f, creditsPerMinuteByLevel[i]);
        }
    }

    private void NormalizeStorageCapacities()
    {
        int targetCount = Mathf.Max(1, maxLevel);
        while (storageCapacityByLevel.Count < targetCount)
        {
            storageCapacityByLevel.Add(storageCapacity);
        }

        for (int i = 0; i < storageCapacityByLevel.Count; i++)
        {
            storageCapacityByLevel[i] = Mathf.Max(1, storageCapacityByLevel[i]);
        }
    }

    private void NormalizeConfiguredValues()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        storageCapacity = Mathf.Max(1, storageCapacity);
        NormalizeStorageCapacities();
        storedCredits = Mathf.Clamp(storedCredits, 0, StorageCapacity);
        creditsPerMinute = Mathf.Max(0f, creditsPerMinute);
        NormalizeCreditsPerMinute();
        upgradeCost = Mathf.Max(0, upgradeCost);
        NormalizeUpgradeCosts();
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        NormalizeResearchLabRequirements();
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        NormalizeUpgradeDurations();
    }

    private void NormalizeResearchLabRequirements()
    {
        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (requiredResearchLabLevelByLevel.Count < targetCount)
        {
            requiredResearchLabLevelByLevel.Add(requiredResearchLabLevel);
        }

        for (int i = 0; i < requiredResearchLabLevelByLevel.Count; i++)
        {
            requiredResearchLabLevelByLevel[i] = Mathf.Max(1, requiredResearchLabLevelByLevel[i]);
        }
    }

    private void OnValidate()
    {
        NormalizeConfiguredValues();
    }
}

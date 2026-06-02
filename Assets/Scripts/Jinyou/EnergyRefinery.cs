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
    [SerializeField] private float creditsPerMinute = 60f;
    [SerializeField] private bool produceWhilePlaying = true;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 250;
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 1;
    [SerializeField] private float upgradeDurationSeconds = 10f;

    [Header("Events")]
    public UnityEvent<int> OnCreditsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private float productionBuffer;
    private bool isUpgrading;
    private float upgradeRemainingSeconds;

    public int Level => level;
    public int StoredCredits => storedCredits;
    public int StorageCapacity => storageCapacity;
    public int UpgradeCost => upgradeCost;
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => requiredResearchLabLevel;
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CreditsPerMinute => creditsPerMinute;
    public bool IsStorageFull => storedCredits >= storageCapacity;

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
        productionBuffer += creditsPerMinute / 60f * deltaTime;
        int amount = Mathf.FloorToInt(productionBuffer);

        if (amount <= 0)
        {
            return;
        }

        productionBuffer -= amount;
        storedCredits = Mathf.Clamp(storedCredits + amount, 0, storageCapacity);
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
        return !isUpgrading && level < maxLevel && credits >= upgradeCost && commanderLevel >= requiredCommanderLevel;
    }

    public bool CanStartUpgrade(int availableCredits, int commanderLevel, int researchLabLevel)
    {
        return CanUpgrade(availableCredits, commanderLevel) && researchLabLevel >= requiredResearchLabLevel;
    }

    public bool TryStartUpgrade(ref int availableCredits, int commanderLevel, int researchLabLevel)
    {
        if (!CanStartUpgrade(availableCredits, commanderLevel, researchLabLevel))
        {
            return false;
        }

        availableCredits -= upgradeCost;
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

        if (upgradeDurationSeconds <= 0f)
        {
            CompleteUpgrade();
            return;
        }

        isUpgrading = true;
        upgradeRemainingSeconds = upgradeDurationSeconds;
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
        level++;
        storageCapacity = Mathf.RoundToInt(storageCapacity * 1.25f);
        creditsPerMinute = Mathf.Round(creditsPerMinute * 1.2f);
        upgradeCost = Mathf.RoundToInt(upgradeCost * 1.35f);
        requiredCommanderLevel++;
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        storageCapacity = Mathf.Max(1, storageCapacity);
        storedCredits = Mathf.Clamp(storedCredits, 0, storageCapacity);
        creditsPerMinute = Mathf.Max(0f, creditsPerMinute);
        upgradeCost = Mathf.Max(0, upgradeCost);
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
    }
}

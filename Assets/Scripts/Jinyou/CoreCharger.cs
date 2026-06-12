using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CoreCharger : MonoBehaviour, IBaseCampFacility
{
    [Serializable]
    public class UnitConversionStage
    {
        [Min(1)] public int requiredPlayerLevel = 5;
        public PlayerUnitConfig currentUnit;
        public PlayerUnitConfig nextUnit;

        public string DisplayName
        {
            get
            {
                string currentName = currentUnit != null ? currentUnit.DisplayName : "Unassigned";
                string nextName = nextUnit != null ? nextUnit.DisplayName : "Unassigned";
                return $"{currentName} -> {nextName}";
            }
        }

        public bool IsConfigured => currentUnit != null && nextUnit != null && currentUnit != nextUnit;
    }

    [Serializable]
    public class DroneUnlock
    {
        public DroneConfig droneConfig;
        [Min(1)] public int requiredCoreChargerLevel = 2;
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 400;
    [SerializeField] private List<int> upgradeCostByLevel = new List<int>();
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 1;
    [SerializeField] private float upgradeDurationSeconds = 10f;
    [SerializeField] private List<float> upgradeDurationSecondsByLevel = new List<float>();

    [Header("Unit SO Conversion")]
    [SerializeField, Min(1)] private int levelsPerConversion = 5;
    [SerializeField] private List<UnitConversionStage> conversionStages = new List<UnitConversionStage>();
    [SerializeField] private List<int> convertedStageIndices = new List<int>();

    [Header("Drone Enhancement")]
    [SerializeField] private float droneAttackDamagePerLevel = 1f;
    [SerializeField] private float droneAttackRangePerLevel = 0.1f;
    [SerializeField] private float droneAttackIntervalReductionPerLevel = 0.02f;
    [SerializeField] private float droneFollowSpeedPerLevel = 0.2f;
    [SerializeField] private List<DroneUnlock> droneUnlocks = new List<DroneUnlock>();

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<PlayerUnitConfig, int> OnUnitEnhanced = new UnityEvent<PlayerUnitConfig, int>();
    public UnityEvent OnUpgradeStarted = new UnityEvent();
    public UnityEvent OnUpgradeCompleted = new UnityEvent();

    private bool isUpgrading;
    private float upgradeRemainingSeconds;
    private float currentUpgradeDurationSeconds;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int UpgradeCost => GetUpgradeCostForCurrentLevel();
    public int RequiredCommanderLevel => requiredCommanderLevel;
    public int RequiredResearchLabLevel => requiredResearchLabLevel;
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public int CompletedConversionCount => convertedStageIndices.Count;
    public int CurrentStageIndex => FindFirstIncompleteStage();
    public IReadOnlyList<UnitConversionStage> ConversionStages => conversionStages;
    public UnitConversionStage CurrentConversionStage => GetConversionStageAt(CurrentStageIndex);
    public float DroneAttackDamageBonus => Mathf.Max(0, level - 1) * droneAttackDamagePerLevel;
    public float DroneAttackRangeBonus => Mathf.Max(0, level - 1) * droneAttackRangePerLevel;
    public float DroneAttackIntervalReduction => Mathf.Max(0, level - 1) * droneAttackIntervalReductionPerLevel;
    public float DroneFollowSpeedBonus => Mathf.Max(0, level - 1) * droneFollowSpeedPerLevel;

    private void Awake()
    {
        Normalize();
        SyncUnlockedDrones(false);
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
    }

    public bool IsConversionCompleted(int stageIndex)
    {
        return convertedStageIndices.Contains(stageIndex);
    }

    public bool CanConvertCurrentUnit(InventoryFacility inventory, PlayerController player, int playerLevel)
    {
        int stageIndex = CurrentStageIndex;
        UnitConversionStage stage = GetConversionStageAt(stageIndex);
        if (stage == null || !stage.IsConfigured)
        {
            return false;
        }

        bool ownsCurrentUnit = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool hasCurrentUnitEquipped = player != null && player.UnitConfig == stage.currentUnit;
        int requiredCoreChargerLevel = GetRequiredCoreChargerLevel(stageIndex);
        return playerLevel >= stage.requiredPlayerLevel
            && level >= requiredCoreChargerLevel
            && (ownsCurrentUnit || hasCurrentUnitEquipped);
    }

    public bool TryConvertCurrentUnit(InventoryFacility inventory, PlayerController player, int playerLevel)
    {
        if (!CanConvertCurrentUnit(inventory, player, playerLevel))
        {
            return false;
        }

        int stageIndex = CurrentStageIndex;
        UnitConversionStage stage = conversionStages[stageIndex];
        bool wasEquipped = player != null && player.UnitConfig == stage.currentUnit;
        bool inventoryChanged = inventory != null && inventory.ReplaceUnit(stage.currentUnit, stage.nextUnit);

        if (!inventoryChanged && inventory != null && !inventory.ContainsUnit(stage.nextUnit))
        {
            inventory.AddUnit(stage.nextUnit);
        }

        if (wasEquipped)
        {
            player.SetUnitConfig(stage.nextUnit);
        }

        convertedStageIndices.Add(stageIndex);
        Normalize();
        OnUnitEnhanced.Invoke(stage.nextUnit, stageIndex + 1);
        OnLevelChanged.Invoke(Level);
        return true;
    }

    public void ApplyCompletedConversions(InventoryFacility inventory, PlayerController player)
    {
        if (inventory == null)
        {
            return;
        }

        List<int> completedStages = new List<int>(convertedStageIndices);
        completedStages.Sort();

        foreach (int stageIndex in completedStages)
        {
            UnitConversionStage stage = GetConversionStageAt(stageIndex);
            if (stage == null || !stage.IsConfigured)
            {
                continue;
            }

            bool wasEquipped = player != null && player.UnitConfig == stage.currentUnit;
            inventory.ReplaceUnit(stage.currentUnit, stage.nextUnit);
            if (wasEquipped)
            {
                player.SetUnitConfig(stage.nextUnit);
            }
        }
    }

    public JinyouCoreChargerSaveData CaptureState()
    {
        JinyouCoreChargerSaveData data = new JinyouCoreChargerSaveData
        {
            level = level,
            isUpgrading = isUpgrading,
            upgradeRemainingSeconds = upgradeRemainingSeconds,
            currentUpgradeDurationSeconds = currentUpgradeDurationSeconds
        };
        data.convertedStageIndices.AddRange(convertedStageIndices);
        return data;
    }

    public void RestoreState(JinyouCoreChargerSaveData data)
    {
        level = Mathf.Clamp(data?.level ?? 1, 1, maxLevel);
        isUpgrading = data != null && data.isUpgrading;
        upgradeRemainingSeconds = Mathf.Max(0f, data?.upgradeRemainingSeconds ?? 0f);
        currentUpgradeDurationSeconds = Mathf.Max(0f, data?.currentUpgradeDurationSeconds ?? 0f);
        convertedStageIndices = data?.convertedStageIndices != null
            ? new List<int>(data.convertedStageIndices)
            : new List<int>();

        Normalize();
        SyncUnlockedDrones(false);
        OnLevelChanged.Invoke(Level);
    }

    public int GetRequiredCoreChargerLevel(int stageIndex)
    {
        return Mathf.Clamp(stageIndex + 2, 2, maxLevel);
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading
            && level < maxLevel
            && credits >= UpgradeCost
            && commanderLevel >= requiredCommanderLevel;
    }

    public int GetLevelLimit(int researchLabLevel)
    {
        return Mathf.Min(maxLevel, Mathf.Max(1, researchLabLevel) + 2);
    }

    public bool CanStartUpgrade(int credits, int commanderLevel, int researchLabLevel)
    {
        return CanUpgrade(credits, commanderLevel)
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

    private UnitConversionStage GetConversionStageAt(int index)
    {
        if (index < 0 || index >= conversionStages.Count)
        {
            return null;
        }

        return conversionStages[index];
    }

    private int FindFirstIncompleteStage()
    {
        for (int i = 0; i < conversionStages.Count; i++)
        {
            if (!convertedStageIndices.Contains(i))
            {
                return i;
            }
        }

        return -1;
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
        // 코어 충전소 레벨에 맞는 드론을 인벤토리에 해금한다.
        SyncUnlockedDrones(true);
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
        return index < upgradeCostByLevel.Count
            ? Mathf.Max(0, upgradeCostByLevel[index])
            : Mathf.Max(0, upgradeCost);
    }

    private float GetUpgradeDurationForCurrentLevel()
    {
        int index = Mathf.Max(0, level - 1);
        return index < upgradeDurationSecondsByLevel.Count
            ? Mathf.Max(0f, upgradeDurationSecondsByLevel[index])
            : Mathf.Max(0f, upgradeDurationSeconds);
    }

    private void SyncUnlockedDrones(bool reportCollection)
    {
        InventoryFacility inventory = BaseCampManager.Instance != null
            ? BaseCampManager.Instance.Inventory
            : InventoryFacility.FindAny();
        if (inventory == null || droneUnlocks == null)
        {
            return;
        }

        foreach (DroneUnlock droneUnlock in droneUnlocks)
        {
            if (droneUnlock?.droneConfig == null
                || level < Mathf.Max(1, droneUnlock.requiredCoreChargerLevel))
            {
                continue;
            }

            if (reportCollection)
            {
                inventory.AddDrone(droneUnlock.droneConfig);
            }
            else
            {
                inventory.RegisterInitialDrone(droneUnlock.droneConfig);
            }
        }
    }

    private void Normalize()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        upgradeCostByLevel ??= new List<int>();
        upgradeDurationSecondsByLevel ??= new List<float>();

        int upgradeCount = Mathf.Max(0, maxLevel - 1);
        while (upgradeCostByLevel.Count < upgradeCount)
        {
            upgradeCostByLevel.Add(upgradeCost);
        }

        while (upgradeDurationSecondsByLevel.Count < upgradeCount)
        {
            upgradeDurationSecondsByLevel.Add(upgradeDurationSeconds);
        }

        levelsPerConversion = Mathf.Max(1, levelsPerConversion);
        conversionStages ??= new List<UnitConversionStage>();
        convertedStageIndices ??= new List<int>();

        for (int i = 0; i < conversionStages.Count; i++)
        {
            UnitConversionStage stage = conversionStages[i];
            if (stage != null)
            {
                stage.requiredPlayerLevel = (i + 1) * levelsPerConversion;
            }
        }

        convertedStageIndices.RemoveAll(index => index < 0 || index >= conversionStages.Count);
        convertedStageIndices.Sort();
        for (int i = convertedStageIndices.Count - 1; i > 0; i--)
        {
            if (convertedStageIndices[i] == convertedStageIndices[i - 1])
            {
                convertedStageIndices.RemoveAt(i);
            }
        }

        droneAttackDamagePerLevel = Mathf.Max(0f, droneAttackDamagePerLevel);
        droneAttackRangePerLevel = Mathf.Max(0f, droneAttackRangePerLevel);
        droneAttackIntervalReductionPerLevel = Mathf.Max(0f, droneAttackIntervalReductionPerLevel);
        droneFollowSpeedPerLevel = Mathf.Max(0f, droneFollowSpeedPerLevel);
        droneUnlocks ??= new List<DroneUnlock>();
        foreach (DroneUnlock droneUnlock in droneUnlocks)
        {
            if (droneUnlock != null)
            {
                droneUnlock.requiredCoreChargerLevel = Mathf.Clamp(
                    droneUnlock.requiredCoreChargerLevel,
                    1,
                    maxLevel);
            }
        }
    }

    private void OnValidate()
    {
        Normalize();
    }
}

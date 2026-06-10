using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CoreCharger : MonoBehaviour, IBaseCampFacility
{
    [Serializable]
    public class UnitConversionStage
    {
        [Min(1)] public int requiredPlayerLevel = 1;
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

    public enum UnitEnhancementStat
    {
        MaxHealth,
        CritChance,
        CritMultiplier,
        AttackRange,
        AttackDamage,
        AttackInterval,
        MoveSpeed,
        RotationSpeed,
        FireAngleTolerance
    }

    [Serializable]
    public class UnitStatIncrease
    {
        public UnitEnhancementStat stat = UnitEnhancementStat.MaxHealth;
        public float amount = 10f;
    }

    [Serializable]
    public class UnitEnhancementLevel
    {
        public int cost = 100;
        public List<UnitStatIncrease> statIncreases = new List<UnitStatIncrease>
        {
            new UnitStatIncrease { stat = UnitEnhancementStat.MaxHealth, amount = 10f }
        };

        public void Normalize()
        {
            cost = Mathf.Max(0, cost);
            statIncreases ??= new List<UnitStatIncrease>();
        }
    }

    [Serializable]
    public class UnitEnhancement
    {
        public PlayerUnitConfig unitConfig;
        public string displayNameOverride;
        public int enhanceLevel;
        public List<UnitEnhancementLevel> enhancementLevels = new List<UnitEnhancementLevel>
        {
            new UnitEnhancementLevel()
        };

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayNameOverride))
                {
                    return displayNameOverride;
                }

                return unitConfig != null ? unitConfig.DisplayName : "Unassigned Unit";
            }
        }

        public int MaxEnhanceLevel => enhancementLevels != null ? enhancementLevels.Count : 0;
        public int NextEnhanceCost => GetEnhancementLevel(enhanceLevel)?.cost ?? 0;
        public bool IsMaxLevel => enhanceLevel >= MaxEnhanceLevel;

        public float GetStatBonus(UnitEnhancementStat stat)
        {
            float bonus = 0f;
            for (int i = 0; i < enhanceLevel; i++)
            {
                UnitEnhancementLevel level = GetEnhancementLevel(i);
                if (level?.statIncreases == null)
                {
                    continue;
                }

                foreach (UnitStatIncrease statIncrease in level.statIncreases)
                {
                    if (statIncrease != null && statIncrease.stat == stat)
                    {
                        bonus += statIncrease.amount;
                    }
                }
            }

            return bonus;
        }

        public UnitEnhancementLevel GetEnhancementLevel(int levelIndex)
        {
            if (enhancementLevels == null || levelIndex < 0 || levelIndex >= enhancementLevels.Count)
            {
                return null;
            }

            return enhancementLevels[levelIndex];
        }

        public void Normalize()
        {
            enhancementLevels ??= new List<UnitEnhancementLevel>();
            foreach (UnitEnhancementLevel level in enhancementLevels)
            {
                level?.Normalize();
            }

            enhanceLevel = Mathf.Clamp(enhanceLevel, 0, MaxEnhanceLevel);
        }
    }

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 10;

    [Header("Upgrade")]
    [SerializeField] private int upgradeCost = 400;
    [SerializeField] private List<int> upgradeCostByLevel = new List<int>();
    [SerializeField] private int requiredCommanderLevel = 1;
    [SerializeField] private int requiredResearchLabLevel = 3;
    [SerializeField] private List<int> requiredResearchLabLevelByLevel = new List<int>();
    [SerializeField] private float upgradeDurationSeconds = 10f;
    [SerializeField] private List<float> upgradeDurationSecondsByLevel = new List<float>();

    [Header("Unit Enhancement")]
    [SerializeField] private List<UnitEnhancement> unitEnhancements = new List<UnitEnhancement>();
    [SerializeField] private int selectedUnitIndex;

    [Header("Unit Conversion")]
    [SerializeField] private List<UnitConversionStage> conversionStages = new List<UnitConversionStage>();
    [SerializeField, Min(1)] private int defaultRequiredLevelInterval = 5;
    [SerializeField] private List<int> convertedStageIndices = new List<int>();

    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent<PlayerUnitConfig> OnUnitSelected = new UnityEvent<PlayerUnitConfig>();
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
    public int RequiredResearchLabLevel => GetRequiredResearchLabLevelForCurrentUpgrade();
    public bool IsUpgrading => isUpgrading;
    public float UpgradeRemainingSeconds => upgradeRemainingSeconds;
    public float CurrentUpgradeDurationSeconds => currentUpgradeDurationSeconds;
    public IReadOnlyList<UnitEnhancement> UnitEnhancements => unitEnhancements;
    public IReadOnlyList<UnitConversionStage> ConversionStages => conversionStages;
    public UnitConversionStage SelectedConversionStage => GetConversionStageAt(selectedUnitIndex);
    public UnitEnhancement SelectedUnitEnhancement => GetUnitEnhancementAt(selectedUnitIndex);
    public PlayerUnitConfig SelectedUnitConfig => SelectedUnitEnhancement?.unitConfig;
    public int SelectedUnitIndex => selectedUnitIndex;

    public bool IsConversionCompleted(int stageIndex)
    {
        return convertedStageIndices.Contains(stageIndex);
    }

    public bool TrySelectConversionStage(int index)
    {
        if (index < 0 || index >= conversionStages.Count)
        {
            return false;
        }

        selectedUnitIndex = index;
        OnUnitSelected.Invoke(conversionStages[index]?.currentUnit);
        return true;
    }

    public bool CanConvertSelectedUnit(InventoryFacility inventory, PlayerController player, int playerLevel)
    {
        UnitConversionStage stage = SelectedConversionStage;
        if (stage == null || !stage.IsConfigured || IsConversionCompleted(selectedUnitIndex))
        {
            return false;
        }

        bool ownsCurrentUnit = inventory != null && inventory.ContainsUnit(stage.currentUnit);
        bool hasCurrentUnitEquipped = player != null && player.UnitConfig == stage.currentUnit;
        return playerLevel >= stage.requiredPlayerLevel && (ownsCurrentUnit || hasCurrentUnitEquipped);
    }

    public bool TryConvertSelectedUnit(InventoryFacility inventory, PlayerController player, int playerLevel)
    {
        if (!CanConvertSelectedUnit(inventory, player, playerLevel))
        {
            return false;
        }

        UnitConversionStage stage = SelectedConversionStage;
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

        int completedStageIndex = selectedUnitIndex;
        convertedStageIndices.Add(completedStageIndex);
        SelectNextIncompleteStage(completedStageIndex + 1);
        OnUnitEnhanced.Invoke(stage.nextUnit, completedStageIndex + 1);
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

    private void Awake()
    {
        NormalizeConfiguredValues();
    }

    private void Update()
    {
        TickUpgrade(Time.deltaTime);
    }

    public bool TrySelectUnit(int index)
    {
        if (index < 0 || index >= unitEnhancements.Count)
        {
            return false;
        }

        selectedUnitIndex = index;
        OnUnitSelected.Invoke(SelectedUnitConfig);
        return true;
    }

    public bool TrySelectUnit(PlayerUnitConfig unitConfig)
    {
        if (unitConfig == null)
        {
            return false;
        }

        for (int i = 0; i < unitEnhancements.Count; i++)
        {
            if (unitEnhancements[i].unitConfig == unitConfig)
            {
                return TrySelectUnit(i);
            }
        }

        return false;
    }

    public bool TrySelectRoute(string routeId)
    {
        return TrySelectUnit(ParseIndexId(routeId));
    }

    public bool TrySelectOption(string optionId)
    {
        return TrySelectUnit(ParseIndexId(optionId));
    }

    public bool HasUnitEnhancement(PlayerUnitConfig unitConfig)
    {
        return FindUnitEnhancement(unitConfig) != null;
    }

    public JinyouCoreChargerSaveData CaptureState()
    {
        JinyouCoreChargerSaveData data = new JinyouCoreChargerSaveData
        {
            level = level,
            selectedUnitIndex = selectedUnitIndex,
            isUpgrading = isUpgrading,
            upgradeRemainingSeconds = upgradeRemainingSeconds,
            currentUpgradeDurationSeconds = currentUpgradeDurationSeconds
        };

        foreach (UnitEnhancement unitEnhancement in unitEnhancements)
        {
            data.unitEnhanceLevels.Add(unitEnhancement != null ? unitEnhancement.enhanceLevel : 0);
        }

        data.convertedStageIndices.AddRange(convertedStageIndices);

        return data;
    }

    public void RestoreState(JinyouCoreChargerSaveData data)
    {
        if (data == null)
        {
            return;
        }

        level = Mathf.Clamp(data.level, 1, maxLevel);
        selectedUnitIndex = data.selectedUnitIndex;
        isUpgrading = data.isUpgrading;
        upgradeRemainingSeconds = Mathf.Max(0f, data.upgradeRemainingSeconds);
        currentUpgradeDurationSeconds = Mathf.Max(0f, data.currentUpgradeDurationSeconds);
        convertedStageIndices = data.convertedStageIndices != null
            ? new List<int>(data.convertedStageIndices)
            : new List<int>();

        if (data.unitEnhanceLevels != null)
        {
            int count = Mathf.Min(data.unitEnhanceLevels.Count, unitEnhancements.Count);
            for (int i = 0; i < count; i++)
            {
                if (unitEnhancements[i] != null)
                {
                    unitEnhancements[i].enhanceLevel = data.unitEnhanceLevels[i];
                    unitEnhancements[i].Normalize();
                }
            }
        }

        NormalizeConfiguredValues();
        OnLevelChanged.Invoke(level);
    }

    public bool CanEnhanceSelectedUnit(int credits)
    {
        UnitEnhancement selectedUnit = SelectedUnitEnhancement;
        return selectedUnit != null
            && !selectedUnit.IsMaxLevel
            && credits >= selectedUnit.NextEnhanceCost;
    }

    public bool TryEnhanceSelectedUnit(ref int availableCredits)
    {
        if (!CanEnhanceSelectedUnit(availableCredits))
        {
            return false;
        }

        availableCredits -= SelectedUnitEnhancement.NextEnhanceCost;
        EnhanceSelectedUnit();
        return true;
    }

    public bool TryInvestRoute(string routeId)
    {
        if (!TrySelectRoute(routeId))
        {
            return false;
        }

        BaseCampManager manager = BaseCampManager.Instance;
        int availableCredits = manager != null ? manager.Credits : int.MaxValue;
        if (!TryEnhanceSelectedUnit(ref availableCredits))
        {
            return false;
        }

        manager?.SetCreditsForFacility(availableCredits);
        return true;
    }

    public bool TryInvestOption(string optionId)
    {
        if (!TrySelectOption(optionId))
        {
            return false;
        }

        BaseCampManager manager = BaseCampManager.Instance;
        int availableCredits = manager != null ? manager.Credits : int.MaxValue;
        if (!TryEnhanceSelectedUnit(ref availableCredits))
        {
            return false;
        }

        manager?.SetCreditsForFacility(availableCredits);
        return true;
    }

    public void EnhanceSelectedUnit()
    {
        UnitEnhancement selectedUnit = SelectedUnitEnhancement;
        if (selectedUnit == null || selectedUnit.IsMaxLevel)
        {
            return;
        }

        selectedUnit.enhanceLevel++;
        OnUnitEnhanced.Invoke(selectedUnit.unitConfig, selectedUnit.enhanceLevel);
    }

    public float GetUnitStatBonus(PlayerUnitConfig unitConfig, UnitEnhancementStat stat)
    {
        UnitEnhancement unitEnhancement = FindUnitEnhancement(unitConfig);
        return unitEnhancement != null ? unitEnhancement.GetStatBonus(stat) : 0f;
    }

    public bool CanUpgrade(int credits, int commanderLevel)
    {
        return !isUpgrading && level < maxLevel && credits >= UpgradeCost && commanderLevel >= requiredCommanderLevel;
    }

    public int GetLevelLimit(int researchLabLevel)
    {
        return Mathf.Min(maxLevel, Mathf.Max(1, researchLabLevel) + 2);
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

    public static string GetStatDisplayName(UnitEnhancementStat stat)
    {
        return stat switch
        {
            UnitEnhancementStat.MaxHealth => "Max Health",
            UnitEnhancementStat.CritChance => "Crit Chance",
            UnitEnhancementStat.CritMultiplier => "Crit Multiplier",
            UnitEnhancementStat.AttackRange => "Attack Range",
            UnitEnhancementStat.AttackDamage => "Attack Damage",
            UnitEnhancementStat.AttackInterval => "Attack Interval",
            UnitEnhancementStat.MoveSpeed => "Move Speed",
            UnitEnhancementStat.RotationSpeed => "Rotation Speed",
            UnitEnhancementStat.FireAngleTolerance => "Fire Angle",
            _ => stat.ToString()
        };
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
        requiredCommanderLevel++;
        OnLevelChanged.Invoke(level);
        OnUpgradeCompleted.Invoke();
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

    private int GetRequiredResearchLabLevelForCurrentUpgrade()
    {
        int index = Mathf.Max(0, level - 1);
        if (index < requiredResearchLabLevelByLevel.Count)
        {
            return Mathf.Max(1, requiredResearchLabLevelByLevel[index]);
        }

        return Mathf.Max(1, requiredResearchLabLevel);
    }

    private UnitEnhancement FindUnitEnhancement(PlayerUnitConfig unitConfig)
    {
        if (unitConfig == null)
        {
            return null;
        }

        return unitEnhancements.Find(item => item.unitConfig == unitConfig);
    }

    private UnitEnhancement GetUnitEnhancementAt(int index)
    {
        if (index < 0 || index >= unitEnhancements.Count)
        {
            return null;
        }

        return unitEnhancements[index];
    }

    private UnitConversionStage GetConversionStageAt(int index)
    {
        if (index < 0 || index >= conversionStages.Count)
        {
            return null;
        }

        return conversionStages[index];
    }

    private int ParseIndexId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return selectedUnitIndex;
        }

        if (int.TryParse(value, out int parsedIndex))
        {
            return Mathf.Clamp(parsedIndex, 0, Mathf.Max(0, unitEnhancements.Count - 1));
        }

        for (int i = 0; i < unitEnhancements.Count; i++)
        {
            UnitEnhancement enhancement = unitEnhancements[i];
            if (enhancement == null)
            {
                continue;
            }

            if (string.Equals(enhancement.DisplayName, value, StringComparison.OrdinalIgnoreCase)
                || (enhancement.unitConfig != null && string.Equals(enhancement.unitConfig.Id, value, StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return selectedUnitIndex;
    }

    private void NormalizeUpgradeDurations()
    {
        if (upgradeDurationSecondsByLevel == null)
        {
            upgradeDurationSecondsByLevel = new List<float>();
        }

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
        if (upgradeCostByLevel == null)
        {
            upgradeCostByLevel = new List<int>();
        }

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

    private void NormalizeResearchLabRequirements()
    {
        if (requiredResearchLabLevelByLevel == null)
        {
            requiredResearchLabLevelByLevel = new List<int>();
        }

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

    private void NormalizeUnitEnhancements()
    {
        unitEnhancements ??= new List<UnitEnhancement>();
        foreach (UnitEnhancement unitEnhancement in unitEnhancements)
        {
            unitEnhancement?.Normalize();
        }

        if (unitEnhancements.Count == 0)
        {
            selectedUnitIndex = 0;
            return;
        }

        selectedUnitIndex = Mathf.Clamp(selectedUnitIndex, 0, unitEnhancements.Count - 1);
    }

    private void NormalizeConfiguredValues()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(level, maxLevel);
        upgradeCost = Mathf.Max(0, upgradeCost);
        NormalizeUpgradeCosts();
        requiredCommanderLevel = Mathf.Max(1, requiredCommanderLevel);
        requiredResearchLabLevel = Mathf.Max(1, requiredResearchLabLevel);
        NormalizeResearchLabRequirements();
        upgradeDurationSeconds = Mathf.Max(0f, upgradeDurationSeconds);
        NormalizeUpgradeDurations();
        NormalizeUnitEnhancements();
        conversionStages ??= new List<UnitConversionStage>();
        defaultRequiredLevelInterval = Mathf.Max(1, defaultRequiredLevelInterval);
        BuildDefaultConversionStages();
        convertedStageIndices ??= new List<int>();
        convertedStageIndices.RemoveAll(index => index < 0 || index >= conversionStages.Count);
        foreach (UnitConversionStage stage in conversionStages)
        {
            if (stage != null)
            {
                stage.requiredPlayerLevel = Mathf.Max(1, stage.requiredPlayerLevel);
            }
        }

        if (conversionStages.Count > 0)
        {
            selectedUnitIndex = Mathf.Clamp(selectedUnitIndex, 0, conversionStages.Count - 1);
            if (IsConversionCompleted(selectedUnitIndex))
            {
                SelectNextIncompleteStage(0);
            }
        }
    }

    private void SelectNextIncompleteStage(int startIndex)
    {
        for (int i = Mathf.Max(0, startIndex); i < conversionStages.Count; i++)
        {
            if (!IsConversionCompleted(i))
            {
                selectedUnitIndex = i;
                return;
            }
        }

        for (int i = 0; i < Mathf.Min(startIndex, conversionStages.Count); i++)
        {
            if (!IsConversionCompleted(i))
            {
                selectedUnitIndex = i;
                return;
            }
        }
    }

    private void BuildDefaultConversionStages()
    {
        if (conversionStages.Count > 0 || unitEnhancements == null || unitEnhancements.Count < 2)
        {
            return;
        }

        for (int i = 0; i < unitEnhancements.Count - 1; i++)
        {
            PlayerUnitConfig currentUnit = unitEnhancements[i]?.unitConfig;
            PlayerUnitConfig nextUnit = unitEnhancements[i + 1]?.unitConfig;
            if (currentUnit == null || nextUnit == null || currentUnit == nextUnit)
            {
                continue;
            }

            conversionStages.Add(new UnitConversionStage
            {
                requiredPlayerLevel = (i + 1) * defaultRequiredLevelInterval,
                currentUnit = currentUnit,
                nextUnit = nextUnit
            });
        }
    }

    private void OnValidate()
    {
        NormalizeConfiguredValues();
    }
}

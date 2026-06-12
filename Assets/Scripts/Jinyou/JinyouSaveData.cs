using System;
using System.Collections.Generic;

[Serializable]
public class JinyouSaveData
{
    public int version = 2;
    public long lastSavedUnixTime;
    public int commanderLevel = 1;
    public int mainBuildingLevel = 1;
    public int credits = 500;
    public int coreCrystals;
    public JinyouOfflineRewardSaveData lastOfflineReward = new JinyouOfflineRewardSaveData();
    public JinyouCommandCenterSaveData researchLab = new JinyouCommandCenterSaveData();
    public JinyouEnergyRefinerySaveData energyRefinery = new JinyouEnergyRefinerySaveData();
    public JinyouAssemblyFactorySaveData assemblyFactory = new JinyouAssemblyFactorySaveData();
    public JinyouCoreChargerSaveData coreCharger = new JinyouCoreChargerSaveData();
    public JinyouTraitPointSaveData traitPoints = new JinyouTraitPointSaveData();
    public JinyouAchievementSaveData achievements = new JinyouAchievementSaveData();
    public JinyouDailyMissionSaveData dailyMissions = new JinyouDailyMissionSaveData();
}

[Serializable]
public class JinyouOfflineRewardSaveData
{
    public float elapsedSeconds;
    public float appliedSeconds;
    public int refineryCreditsAdded;
    public int bossTicketsAdded;

    public bool HasReward => refineryCreditsAdded > 0 || bossTicketsAdded > 0;
}

[Serializable]
public class JinyouCommandCenterSaveData
{
    public int level = 1;
    public int upgradeCost;
    public int bossTickets;
    public int bossTicketCapacity;
    public float bossTicketProductionSeconds;
    public bool isUpgrading;
    public float upgradeRemainingSeconds;
    public float currentUpgradeDurationSeconds;
}

[Serializable]
public class JinyouEnergyRefinerySaveData
{
    public int level = 1;
    public int storedCredits;
    public bool isUpgrading;
    public float upgradeRemainingSeconds;
    public float currentUpgradeDurationSeconds;
}

[Serializable]
public class JinyouAssemblyFactorySaveData
{
    public int level = 1;
    public string selectedMenuId;
    public int selectedWeaponIndex;
    public string selectedWeaponId;
    public string selectedDroneId;
    public bool isUpgrading;
    public float upgradeRemainingSeconds;
    public float currentUpgradeDurationSeconds;
    public List<JinyouMenuSaveData> menus = new List<JinyouMenuSaveData>();
    public List<int> weaponEnhanceLevels = new List<int>();
    public List<JinyouEnhancementLevelSaveData> weaponEnhancements = new List<JinyouEnhancementLevelSaveData>();
    public List<JinyouEnhancementLevelSaveData> droneEnhancements = new List<JinyouEnhancementLevelSaveData>();
}

[Serializable]
public class JinyouEnhancementLevelSaveData
{
    public string configId;
    public int level;
}

[Serializable]
public class JinyouMenuSaveData
{
    public string menuId;
    public int developmentLevel;
    public bool unlocked;
}

[Serializable]
public class JinyouCoreChargerSaveData
{
    public int level = 1;
    public bool isUpgrading;
    public float upgradeRemainingSeconds;
    public float currentUpgradeDurationSeconds;
    public List<int> convertedStageIndices = new List<int>();
}

[Serializable]
public class JinyouTraitPointSaveData
{
    public int attackDamagePoints;
    public int maxHealthPoints;
    public int critChancePoints;
    public int critMultiplierPoints;
}

[Serializable]
public class JinyouAchievementSaveData
{
    public List<JinyouAchievementEntrySaveData> entries = new List<JinyouAchievementEntrySaveData>();
}

[Serializable]
public class JinyouAchievementEntrySaveData
{
    public string id;
    public int currentAmount;
    public int completedCount;
}

[Serializable]
public class JinyouDailyMissionSaveData
{
    public string dateKey;
    public List<JinyouDailyMissionEntrySaveData> entries = new List<JinyouDailyMissionEntrySaveData>();
}

[Serializable]
public class JinyouDailyMissionEntrySaveData
{
    public string id;
    public int currentAmount;
    public bool rewardClaimed;
}

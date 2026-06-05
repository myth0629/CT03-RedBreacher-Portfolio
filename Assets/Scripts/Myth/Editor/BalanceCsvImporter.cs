#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BalanceCsvImporter
{
    private const string UnitCsvPath = "Assets/Data/Balance/units.csv";
    private const string WeaponCsvPath = "Assets/Data/Balance/weapons.csv";
    private const string EnemyCsvPath = "Assets/Data/Balance/enemies.csv";
    private const string DroneCsvPath = "Assets/Data/Balance/drones.csv";
    private const string EquipmentPartCsvPath = "Assets/Data/Balance/equipment_parts.csv";

    private const string UnitOutputPath = "Assets/SO/Balance/Units";
    private const string WeaponOutputPath = "Assets/SO/Balance/Weapons";
    private const string EnemyOutputPath = "Assets/SO/Balance/Enemies";
    private const string DroneOutputPath = "Assets/SO/Balance/Drones";
    private const string EquipmentPartOutputPath = "Assets/SO/Balance/EquipmentParts";

    [MenuItem("Tools/Balance/CSV to SO/All")]
    public static void ImportAll()
    {
        EnsureOutputFolders();
        ImportWeapons();
        ImportUnits();
        ImportEnemies();
        ImportDrones();
        ImportEquipmentParts();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("밸런스 CSV 임포트 완료");
    }

    [MenuItem("Tools/Balance/SO to CSV/All")]
    public static void ExportAll()
    {
        ExportUnits();
        ExportWeapons();
        ExportEnemies();
        ExportDrones();
        ExportEquipmentParts();
        AssetDatabase.Refresh();
        Debug.Log("밸런스 SO CSV 내보내기 완료");
    }

    [MenuItem("Tools/Balance/CSV to SO/Units")]
    public static void ImportUnits()
    {
        foreach (Dictionary<string, string> row in ReadCsv(UnitCsvPath))
        {
            string id = GetRequired(row, "id", UnitCsvPath);
            PlayerUnitConfig config = LoadOrCreate<PlayerUnitConfig>(UnitOutputPath, id, "Unit");
            SerializedObject serializedObject = new SerializedObject(config);

            SetString(serializedObject, "id", id);
            SetString(serializedObject, "displayName", Get(row, "displayName"));
            SetObject(serializedObject, "unitPrefab", GetAsset<GameObject>(row, "unitPrefab"));
            SetFloat(serializedObject, "maxHealth", row, "maxHealth");
            SetFloat(serializedObject, "critChance", row, "critChance");
            SetFloat(serializedObject, "critMultiplier", row, "critMultiplier");
            SetFloat(serializedObject, "attackRange", row, "attackRange");
            SetFloat(serializedObject, "attackDamage", row, "attackDamage");
            SetFloat(serializedObject, "attackInterval", row, "attackInterval");
            SetFloat(serializedObject, "moveSpeed", row, "moveSpeed");
            SetFloat(serializedObject, "rotationSpeed", row, "rotationSpeed");
            SetFloat(serializedObject, "fireAngleTolerance", row, "fireAngleTolerance");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }

    [MenuItem("Tools/Balance/CSV to SO/Weapons")]
    public static void ImportWeapons()
    {
        foreach (Dictionary<string, string> row in ReadCsv(WeaponCsvPath))
        {
            string id = GetRequired(row, "id", WeaponCsvPath);
            ProjectileConfig config = LoadOrCreate<ProjectileConfig>(WeaponOutputPath, id, "Weapon");
            SerializedObject serializedObject = new SerializedObject(config);

            SetString(serializedObject, "id", id);
            SetString(serializedObject, "displayName", Get(row, "displayName"));
            SetString(serializedObject, "weaponCategory", Get(row, "weaponCategory"));
            SetFloat(serializedObject, "attackDamage", row, "attackDamage");
            SetEnum(serializedObject, "attackType", row, "attackType");
            SetFloat(serializedObject, "areaRadius", row, "areaRadius");
            SetFloat(serializedObject, "areaDamageMultiplier", row, "areaDamageMultiplier");
            SetInt(serializedObject, "maxAreaTargets", row, "maxAreaTargets");
            SetFloat(serializedObject, "speed", row, "speed");
            SetFloat(serializedObject, "lifetime", row, "lifetime");
            SetFloat(serializedObject, "collisionRadius", row, "collisionRadius");
            SetFloat(serializedObject, "knockbackForce", row, "knockbackForce");
            SetEnum(serializedObject, "multiMuzzleFireMode", row, "multiMuzzleFireMode");
            SetInt(serializedObject, "maxBurstMuzzleCount", row, "maxBurstMuzzleCount");
            SetString(serializedObject, "muzzleNamePrefix", Get(row, "muzzleNamePrefix"));
            SetObject(serializedObject, "fireFlashEffectPrefab", GetAsset<GameObject>(row, "fireFlashEffect"));
            SetObject(serializedObject, "projectileEffectPrefab", GetAsset<GameObject>(row, "projectileEffect"));
            SetObject(serializedObject, "hitEffectPrefab", GetAsset<GameObject>(row, "hitEffect"));
            SetFloat(serializedObject, "effectCleanupDelay", row, "effectCleanupDelay");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }

    [MenuItem("Tools/Balance/CSV to SO/Enemies")]
    public static void ImportEnemies()
    {
        foreach (Dictionary<string, string> row in ReadCsv(EnemyCsvPath))
        {
            string id = GetRequired(row, "id", EnemyCsvPath);
            EnemyConfig config = LoadOrCreate<EnemyConfig>(EnemyOutputPath, id, "Enemy");
            SerializedObject serializedObject = new SerializedObject(config);

            SetString(serializedObject, "id", id);
            SetString(serializedObject, "displayName", Get(row, "displayName"));
            SetObject(serializedObject, "enemyPrefab", GetAsset<GameObject>(row, "enemyPrefab"));
            SetFloat(serializedObject, "maxHealth", row, "maxHealth");
            SetFloat(serializedObject, "moveSpeed", row, "moveSpeed");
            SetFloat(serializedObject, "stopDistance", row, "stopDistance");
            SetFloat(serializedObject, "contactDamage", row, "contactDamage");
            SetFloat(serializedObject, "contactInterval", row, "contactInterval");
            SetFloat(serializedObject, "experienceReward", row, "experienceReward");
            SetInt(serializedObject, "creditReward", row, "creditReward");
            SetInt(serializedObject, "coreCrystalReward", row, "coreCrystalReward");
            SetFloat(serializedObject, "partDropChance", row, "partDropChance");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }

    [MenuItem("Tools/Balance/CSV to SO/Drones")]
    public static void ImportDrones()
    {
        foreach (Dictionary<string, string> row in ReadCsv(DroneCsvPath))
        {
            string id = GetRequired(row, "id", DroneCsvPath);
            DroneConfig config = LoadOrCreate<DroneConfig>(DroneOutputPath, id, "Drone");
            SerializedObject serializedObject = new SerializedObject(config);

            SetString(serializedObject, "id", id);
            SetString(serializedObject, "displayName", Get(row, "displayName"));
            SetObject(serializedObject, "dronePrefab", GetAsset<GameObject>(row, "dronePrefab"));
            SetObject(serializedObject, "projectileConfig", GetAsset<ProjectileConfig>(row, "projectileConfig"));
            SetFloat(serializedObject, "attackDamage", row, "attackDamage");
            SetFloat(serializedObject, "attackRange", row, "attackRange");
            SetFloat(serializedObject, "attackInterval", row, "attackInterval");
            SetFloat(serializedObject, "fireAngleTolerance", row, "fireAngleTolerance");
            SetFloat(serializedObject, "rotationSpeed", row, "rotationSpeed");
            SetInt(serializedObject, "targetMask", row, "targetMask");
            SetFloat(serializedObject, "projectileSpeed", row, "projectileSpeed");
            SetFloat(serializedObject, "projectileLifetime", row, "projectileLifetime");
            SetInt(serializedObject, "droneCount", row, "droneCount");
            SetFloat(serializedObject, "followRadius", row, "followRadius");
            SetFloat(serializedObject, "followSpeed", row, "followSpeed");
            SetFloat(serializedObject, "startAngle", row, "startAngle");
            SetFloat(serializedObject, "angleStep", row, "angleStep");
            SetString(serializedObject, "muzzleNamePrefix", Get(row, "muzzleNamePrefix"));

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }

    [MenuItem("Tools/Balance/CSV to SO/Equipment Parts")]
    public static void ImportEquipmentParts()
    {
        EnsureFolder(EquipmentPartOutputPath);
        foreach (Dictionary<string, string> row in ReadCsv(EquipmentPartCsvPath))
        {
            string id = GetRequired(row, "id", EquipmentPartCsvPath);
            EquipmentPartConfig config = LoadOrCreate<EquipmentPartConfig>(
                EquipmentPartOutputPath,
                id,
                "EquipmentPart");
            SerializedObject serializedObject = new SerializedObject(config);

            SetString(serializedObject, "id", id);
            SetString(serializedObject, "displayName", Get(row, "displayName"));
            SetObject(serializedObject, "icon", GetAsset<Sprite>(row, "icon"));
            SetEnum(serializedObject, "slot", row, "slot");
            SetFloat(serializedObject, "commonMainValue", row, "commonMainValue");
            SetFloat(serializedObject, "rareMainValue", row, "rareMainValue");
            SetFloat(serializedObject, "epicMainValue", row, "epicMainValue");
            SetInt(serializedObject, "commonSalePrice", row, "commonSalePrice");
            SetInt(serializedObject, "rareSalePrice", row, "rareSalePrice");
            SetInt(serializedObject, "epicSalePrice", row, "epicSalePrice");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }

    [MenuItem("Tools/Balance/SO to CSV/Units")]
    public static void ExportUnits()
    {
        string[] headers =
        {
            "id", "displayName", "unitPrefab", "maxHealth", "critChance", "critMultiplier", "attackRange",
            "attackDamage", "attackInterval", "moveSpeed", "rotationSpeed", "fireAngleTolerance"
        };

        List<string[]> rows = new List<string[]>();
        foreach (PlayerUnitConfig config in LoadAllAssets<PlayerUnitConfig>(UnitOutputPath))
        {
            rows.Add(new[]
            {
                config.Id,
                config.DisplayName,
                GetAssetPath(config.UnitPrefab),
                FormatFloat(config.MaxHealth),
                FormatFloat(config.CritChance),
                FormatFloat(config.CritMultiplier),
                FormatFloat(config.AttackRange),
                FormatFloat(config.AttackDamage),
                FormatFloat(config.AttackInterval),
                FormatFloat(config.MoveSpeed),
                FormatFloat(config.RotationSpeed),
                FormatFloat(config.FireAngleTolerance)
            });
        }

        WriteCsv(UnitCsvPath, headers, rows);
        Debug.Log($"유닛 SO CSV 내보내기 완료: {UnitCsvPath}");
    }

    [MenuItem("Tools/Balance/SO to CSV/Weapons")]
    public static void ExportWeapons()
    {
        string[] headers =
        {
            "id", "displayName", "weaponCategory", "attackDamage", "attackType", "areaRadius", "areaDamageMultiplier",
            "maxAreaTargets", "speed", "lifetime", "collisionRadius", "knockbackForce",
            "multiMuzzleFireMode", "maxBurstMuzzleCount", "muzzleNamePrefix", "fireFlashEffect",
            "projectileEffect", "hitEffect", "effectCleanupDelay"
        };

        List<string[]> rows = new List<string[]>();
        foreach (ProjectileConfig config in LoadAllAssets<ProjectileConfig>(WeaponOutputPath))
        {
            rows.Add(new[]
            {
                config.Id,
                config.DisplayName,
                config.WeaponCategory,
                FormatFloat(config.AttackDamage),
                config.AttackType.ToString(),
                FormatFloat(config.AreaRadius),
                FormatFloat(config.AreaDamageMultiplier),
                config.MaxAreaTargets.ToString(CultureInfo.InvariantCulture),
                FormatFloat(config.Speed),
                FormatFloat(config.Lifetime),
                FormatFloat(config.CollisionRadius),
                FormatFloat(config.KnockbackForce),
                config.MultiMuzzleFireMode.ToString(),
                config.MaxBurstMuzzleCount.ToString(CultureInfo.InvariantCulture),
                config.MuzzleNamePrefix,
                GetAssetPath(config.FireFlashEffectPrefab),
                GetAssetPath(config.ProjectileEffectPrefab),
                GetAssetPath(config.HitEffectPrefab),
                FormatFloat(config.EffectCleanupDelay)
            });
        }

        WriteCsv(WeaponCsvPath, headers, rows);
        Debug.Log($"무기 SO CSV 내보내기 완료: {WeaponCsvPath}");
    }

    [MenuItem("Tools/Balance/SO to CSV/Enemies")]
    public static void ExportEnemies()
    {
        string[] headers =
        {
            "id", "displayName", "enemyPrefab", "maxHealth", "moveSpeed", "stopDistance", "contactDamage",
            "contactInterval", "experienceReward", "creditReward", "coreCrystalReward", "partDropChance"
        };

        List<string[]> rows = new List<string[]>();
        foreach (EnemyConfig config in LoadAllAssets<EnemyConfig>(EnemyOutputPath))
        {
            rows.Add(new[]
            {
                config.Id,
                config.DisplayName,
                GetAssetPath(config.EnemyPrefab),
                FormatFloat(config.MaxHealth),
                FormatFloat(config.MoveSpeed),
                FormatFloat(config.StopDistance),
                FormatFloat(config.ContactDamage),
                FormatFloat(config.ContactInterval),
                FormatFloat(config.ExperienceReward),
                config.CreditReward.ToString(CultureInfo.InvariantCulture),
                config.CoreCrystalReward.ToString(CultureInfo.InvariantCulture),
                FormatFloat(config.PartDropChance)
            });
        }

        WriteCsv(EnemyCsvPath, headers, rows);
        Debug.Log($"적 SO CSV 내보내기 완료: {EnemyCsvPath}");
    }

    [MenuItem("Tools/Balance/SO to CSV/Drones")]
    public static void ExportDrones()
    {
        string[] headers =
        {
            "id", "displayName", "dronePrefab", "projectileConfig", "attackDamage", "attackRange", "attackInterval",
            "fireAngleTolerance", "rotationSpeed", "targetMask", "projectileSpeed", "projectileLifetime",
            "droneCount", "followRadius", "followSpeed", "startAngle", "angleStep", "muzzleNamePrefix"
        };

        List<string[]> rows = new List<string[]>();
        foreach (DroneConfig config in LoadAllAssets<DroneConfig>(DroneOutputPath))
        {
            rows.Add(new[]
            {
                config.Id,
                config.DisplayName,
                GetAssetPath(config.DronePrefab),
                GetAssetPath(config.ProjectileConfig),
                FormatFloat(config.AttackDamage),
                FormatFloat(config.AttackRange),
                FormatFloat(config.AttackInterval),
                FormatFloat(config.FireAngleTolerance),
                FormatFloat(config.RotationSpeed),
                config.TargetMask.value.ToString(CultureInfo.InvariantCulture),
                FormatFloat(config.ProjectileSpeed),
                FormatFloat(config.ProjectileLifetime),
                config.DroneCount.ToString(CultureInfo.InvariantCulture),
                FormatFloat(config.FollowRadius),
                FormatFloat(config.FollowSpeed),
                FormatFloat(config.StartAngle),
                FormatFloat(config.AngleStep),
                config.MuzzleNamePrefix
            });
        }

        WriteCsv(DroneCsvPath, headers, rows);
        Debug.Log($"드론 SO CSV 내보내기 완료: {DroneCsvPath}");
    }

    [MenuItem("Tools/Balance/SO to CSV/Equipment Parts")]
    public static void ExportEquipmentParts()
    {
        string[] headers =
        {
            "id", "displayName", "icon", "slot", "commonMainValue", "rareMainValue", "epicMainValue",
            "commonSalePrice", "rareSalePrice", "epicSalePrice"
        };

        List<string[]> rows = new List<string[]>();
        foreach (EquipmentPartConfig config in LoadAllAssets<EquipmentPartConfig>(EquipmentPartOutputPath))
        {
            rows.Add(new[]
            {
                config.Id,
                config.DisplayName,
                GetAssetPath(config.Icon),
                config.Slot.ToString(),
                FormatFloat(config.CommonMainValue),
                FormatFloat(config.RareMainValue),
                FormatFloat(config.EpicMainValue),
                config.CommonSalePrice.ToString(CultureInfo.InvariantCulture),
                config.RareSalePrice.ToString(CultureInfo.InvariantCulture),
                config.EpicSalePrice.ToString(CultureInfo.InvariantCulture)
            });
        }

        WriteCsv(EquipmentPartCsvPath, headers, rows);
        Debug.Log($"장비 파츠 SO CSV 내보내기 완료: {EquipmentPartCsvPath}");
    }

    private static T LoadOrCreate<T>(string outputPath, string id, string prefix) where T : ScriptableObject
    {
        string assetPath = $"{outputPath}/{prefix}_{SanitizeFileName(id)}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void EnsureOutputFolders()
    {
        EnsureFolder("Assets/SO");
        EnsureFolder("Assets/SO/Balance");
        EnsureFolder(UnitOutputPath);
        EnsureFolder(WeaponOutputPath);
        EnsureFolder(EnemyOutputPath);
        EnsureFolder(DroneOutputPath);
        EnsureFolder(EquipmentPartOutputPath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static IEnumerable<Dictionary<string, string>> ReadCsv(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"CSV 파일을 찾을 수 없습니다: {path}");
            yield break;
        }

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length <= 1)
        {
            yield break;
        }

        List<string> headers = ParseCsvLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            List<string> values = ParseCsvLine(lines[i]);
            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
            {
                string value = j < values.Count ? values[j] : string.Empty;
                row[headers[j]] = value;
            }

        yield return row;
        }
    }

    private static IEnumerable<T> LoadAllAssets<T>(string folderPath) where T : ScriptableObject
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"SO 폴더를 찾을 수 없습니다: {folderPath}");
            yield break;
        }

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
        List<T> assets = new List<T>();
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        assets.Sort((left, right) => string.Compare(GetSortId(left), GetSortId(right), StringComparison.Ordinal));
        foreach (T asset in assets)
        {
            yield return asset;
        }
    }

    private static void WriteCsv(string path, string[] headers, List<string[]> rows)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers));
        for (int i = 0; i < rows.Count; i++)
        {
            string[] row = rows[i];
            for (int j = 0; j < row.Length; j++)
            {
                if (j > 0)
                {
                    builder.Append(',');
                }

                builder.Append(EscapeCsv(row[j]));
            }

            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool needsQuotes = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
        if (!needsQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string GetAssetPath(UnityEngine.Object asset)
    {
        return asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;
    }

    private static string GetSortId(ScriptableObject asset)
    {
        switch (asset)
        {
            case PlayerUnitConfig unit:
                return unit.Id;
            case ProjectileConfig weapon:
                return weapon.Id;
            case EnemyConfig enemy:
                return enemy.Id;
            case DroneConfig drone:
                return drone.Id;
            case EquipmentPartConfig equipmentPart:
                return equipmentPart.Id;
            default:
                return asset.name;
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> values = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static T GetAsset<T>(Dictionary<string, string> row, string key) where T : UnityEngine.Object
    {
        string path = Get(row, key);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogWarning($"에셋을 찾을 수 없습니다: {key}={path}");
        }

        return asset;
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static string GetRequired(Dictionary<string, string> row, string key, string path)
    {
        string value = Get(row, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{path} CSV에 필수 컬럼 값이 없습니다: {key}");
        }

        return value;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, Dictionary<string, string> row, string key)
    {
        string value = Get(row, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            Debug.LogWarning($"숫자 파싱 실패: {key}={value}");
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = parsed;
        }
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, Dictionary<string, string> row, string key)
    {
        string value = Get(row, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            Debug.LogWarning($"정수 파싱 실패: {key}={value}");
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = parsed;
        }
    }

    private static void SetEnum(SerializedObject serializedObject, string propertyName, Dictionary<string, string> row, string key)
    {
        string value = Get(row, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        for (int i = 0; i < property.enumNames.Length; i++)
        {
            if (string.Equals(property.enumNames[i], value, StringComparison.OrdinalIgnoreCase))
            {
                property.enumValueIndex = i;
                return;
            }
        }

        Debug.LogWarning($"enum 파싱 실패: {key}={value}");
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        if (value == null)
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c.ToString(), "_");
        }

        return value;
    }
}
#endif

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

    private const string UnitOutputPath = "Assets/SO/Balance/Units";
    private const string WeaponOutputPath = "Assets/SO/Balance/Weapons";
    private const string EnemyOutputPath = "Assets/SO/Balance/Enemies";

    [MenuItem("Tools/Balance/Import All CSV")]
    public static void ImportAll()
    {
        EnsureOutputFolders();
        ImportWeapons();
        ImportUnits();
        ImportEnemies();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("밸런스 CSV 임포트 완료");
    }

    [MenuItem("Tools/Balance/Import Units CSV")]
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
            SetObject(serializedObject, "projectileConfig", GetAsset<ProjectileConfig>(row, "projectileConfig"));
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

    [MenuItem("Tools/Balance/Import Weapons CSV")]
    public static void ImportWeapons()
    {
        foreach (Dictionary<string, string> row in ReadCsv(WeaponCsvPath))
        {
            string id = GetRequired(row, "id", WeaponCsvPath);
            ProjectileConfig config = LoadOrCreate<ProjectileConfig>(WeaponOutputPath, id, "Weapon");
            SerializedObject serializedObject = new SerializedObject(config);

            SetString(serializedObject, "id", id);
            SetString(serializedObject, "displayName", Get(row, "displayName"));
            SetObject(serializedObject, "projectilePrefab", GetAsset<PlayerProjectile>(row, "projectilePrefab"));
            SetFloat(serializedObject, "speed", row, "speed");
            SetFloat(serializedObject, "lifetime", row, "lifetime");
            SetFloat(serializedObject, "collisionRadius", row, "collisionRadius");
            SetFloat(serializedObject, "knockbackForce", row, "knockbackForce");
            SetObject(serializedObject, "fireFlashEffectPrefab", GetAsset<GameObject>(row, "fireFlashEffect"));
            SetObject(serializedObject, "projectileEffectPrefab", GetAsset<GameObject>(row, "projectileEffect"));
            SetObject(serializedObject, "hitEffectPrefab", GetAsset<GameObject>(row, "hitEffect"));
            SetFloat(serializedObject, "effectCleanupDelay", row, "effectCleanupDelay");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }

    [MenuItem("Tools/Balance/Import Enemies CSV")]
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

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
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

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AchievementPopup : MonoBehaviour
{
    [SerializeField] private AchievementManager achievementManager;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject achievementItemTemplate;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (achievementManager != null)
        {
            achievementManager.OnAchievementsChanged.AddListener(Rebuild);
        }

        Rebuild();
    }

    private void OnDisable()
    {
        if (achievementManager != null)
        {
            achievementManager.OnAchievementsChanged.RemoveListener(Rebuild);
        }
    }

    public void Rebuild()
    {
        ResolveReferences();
        ClearSpawnedItems();

        if (achievementManager == null || contentRoot == null || achievementItemTemplate == null)
        {
            return;
        }

        bool templateIsSceneChild = achievementItemTemplate.transform.IsChildOf(contentRoot);
        if (templateIsSceneChild)
        {
            achievementItemTemplate.SetActive(false);
        }
        IReadOnlyList<AchievementManager.AchievementEntry> achievements = achievementManager.Achievements;
        for (int i = 0; i < achievements.Count; i++)
        {
            AchievementManager.AchievementEntry achievement = achievements[i];
            if (achievement == null)
            {
                continue;
            }

            GameObject item = Instantiate(achievementItemTemplate, contentRoot);
            item.SetActive(true);
            BindItem(item, achievement);
            spawnedItems.Add(item);
        }
    }

    private void BindItem(GameObject item, AchievementManager.AchievementEntry achievement)
    {
        SetText(item, "AchName_Txt", achievement.Title);
        SetText(item, "AchDesc_Txt", string.Format(achievement.Description, achievement.NextTargetAmount));
        SetText(item, "Progress_Num", $"{achievement.CurrentAmount} / {achievement.NextTargetAmount}");
        SetText(item, "Count", achievement.RewardAmount.ToString());

        Image achievementIcon = FindChildComponent<Image>(item.transform, "Ach_Icon");
        if (achievementIcon != null && achievement.IconSprite != null)
        {
            achievementIcon.sprite = achievement.IconSprite;
        }

        Image progressFill = FindChildComponent<Image>(item.transform, "Progress_Fill");
        if (progressFill != null)
        {
            progressFill.fillAmount = achievement.Progress01;
        }

        Image rewardClaim = FindChildComponent<Image>(item.transform, "Reward_Claim");
        if (rewardClaim != null)
        {
            rewardClaim.enabled = achievement.Completed;
        }
    }

    private void ClearSpawnedItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i]);
            }
        }

        spawnedItems.Clear();
    }

    private void ResolveReferences()
    {
        achievementManager ??= AchievementManager.Instance ?? FindFirstObjectByType<AchievementManager>();
        contentRoot ??= FindDeepChild(transform, "Content");
        achievementItemTemplate ??= FindDeepChild(transform, "Achievement_Button")?.gameObject;
    }

    private static void SetText(GameObject root, string childName, string value)
    {
        TMP_Text target = FindChildComponent<TMP_Text>(root.transform, childName);
        if (target != null)
        {
            target.text = value;
        }
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindDeepChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDeepChild(root.GetChild(i), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}

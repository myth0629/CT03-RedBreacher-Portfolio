using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoreChargerTreeBuilder : MonoBehaviour
{
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private CoreChargerPanel coreChargerPanel;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private CoreChargerOptionButton optionNodePrefab;
    [SerializeField] private HorizontalLayoutGroup tierRowPrefab;
    [SerializeField] private bool rebuildOnEnable = true;

    private CoreCharger coreCharger;
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    private void OnEnable()
    {
        ResolveReferences();

        if (rebuildOnEnable)
        {
            RebuildSelectedRoute();
        }
    }

    public void RebuildSelectedRoute()
    {
        RebuildUnits();
    }

    public void RebuildRoute(string routeId)
    {
        RebuildUnits();
    }

    public void RebuildUnits()
    {
        ResolveReferences();
        Clear();

        if (contentRoot == null || optionNodePrefab == null || coreCharger == null)
        {
            return;
        }

        RectTransform row = GetOrCreateRow();
        for (int i = 0; i < coreCharger.UnitEnhancements.Count; i++)
        {
            CoreChargerOptionButton node = Instantiate(optionNodePrefab, row);
            node.Configure(baseCampManager, coreChargerPanel, i);
            spawnedObjects.Add(node.gameObject);
        }
    }

    private RectTransform GetOrCreateRow()
    {
        RectTransform row;
        if (tierRowPrefab != null)
        {
            HorizontalLayoutGroup rowLayout = Instantiate(tierRowPrefab, contentRoot);
            row = rowLayout.GetComponent<RectTransform>();
            spawnedObjects.Add(rowLayout.gameObject);
        }
        else
        {
            GameObject rowObject = new GameObject("Units", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row = rowObject.GetComponent<RectTransform>();
            row.SetParent(contentRoot, false);
            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            spawnedObjects.Add(rowObject);
        }

        row.name = "Units";
        return row;
    }

    private void Clear()
    {
        foreach (GameObject spawnedObject in spawnedObjects)
        {
            if (spawnedObject != null)
            {
                Destroy(spawnedObject);
            }
        }

        spawnedObjects.Clear();
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        coreChargerPanel ??= FindFirstObjectByType<CoreChargerPanel>();
        coreCharger = baseCampManager != null ? baseCampManager.CoreCharger : null;
    }
}

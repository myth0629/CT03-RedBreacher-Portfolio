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
    private string builtRouteId;

    private void OnEnable()
    {
        ResolveReferences();

        if (rebuildOnEnable)
        {
            RebuildSelectedRoute();
        }
    }

    private void Update()
    {
        ResolveReferences();

        if (coreCharger != null && coreCharger.SelectedRouteId != builtRouteId)
        {
            RebuildSelectedRoute();
        }
    }

    public void RebuildSelectedRoute()
    {
        ResolveReferences();

        if (coreCharger == null)
        {
            Clear();
            builtRouteId = string.Empty;
            return;
        }

        string routeId = string.IsNullOrEmpty(coreCharger.SelectedRouteId)
            ? "health"
            : coreCharger.SelectedRouteId;
        RebuildRoute(routeId);
    }

    public void RebuildRoute(string routeId)
    {
        ResolveReferences();
        Clear();
        builtRouteId = routeId;

        if (contentRoot == null
            || optionNodePrefab == null
            || coreCharger == null
            || !coreCharger.TryGetRoute(routeId, out CoreCharger.CoreRoute route)
            || route.options == null)
        {
            return;
        }

        Dictionary<int, RectTransform> tierRows = new Dictionary<int, RectTransform>();
        foreach (CoreCharger.CoreRouteOption option in route.options)
        {
            if (option == null)
            {
                continue;
            }

            RectTransform row = GetOrCreateTierRow(tierRows, Mathf.Max(1, option.tier));
            CoreChargerOptionButton node = Instantiate(optionNodePrefab, row);
            node.Configure(baseCampManager, coreChargerPanel, option.optionId);
            spawnedObjects.Add(node.gameObject);
        }
    }

    private RectTransform GetOrCreateTierRow(Dictionary<int, RectTransform> tierRows, int tier)
    {
        if (tierRows.TryGetValue(tier, out RectTransform existingRow))
        {
            return existingRow;
        }

        RectTransform row;
        if (tierRowPrefab != null)
        {
            HorizontalLayoutGroup rowLayout = Instantiate(tierRowPrefab, contentRoot);
            row = rowLayout.GetComponent<RectTransform>();
            spawnedObjects.Add(rowLayout.gameObject);
        }
        else
        {
            GameObject rowObject = new GameObject($"Tier {tier}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row = rowObject.GetComponent<RectTransform>();
            row.SetParent(contentRoot, false);
            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            spawnedObjects.Add(rowObject);
        }

        row.name = $"Tier {tier}";
        tierRows.Add(tier, row);
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

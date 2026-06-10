using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpriteShapeShadow : MonoBehaviour
{
    private const string ShadowObjectName = "__SpriteShapeShadow";

    [Header("그림자")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
    [SerializeField] private Vector3 worldOffset = new Vector3(0.16f, -0.02f, -0.16f);
    [SerializeField] private int sortingOrderOffset = 10;
    [SerializeField] private float hierarchyCheckInterval = 0.5f;

    private readonly List<ShadowBinding> bindings = new List<ShadowBinding>();
    private float nextHierarchyCheckTime;

    private sealed class ShadowBinding
    {
        public SpriteRenderer source;
        public SpriteRenderer shadow;
    }

    public static SpriteShapeShadow Ensure(GameObject owner)
    {
        SpriteShapeShadow shadow = owner.GetComponent<SpriteShapeShadow>();
        if (shadow == null)
        {
            shadow = owner.AddComponent<SpriteShapeShadow>();
        }

        shadow.Refresh();
        return shadow;
    }

    private void LateUpdate()
    {
        if (HasMissingSource())
        {
            Refresh();
        }
        else if (Time.unscaledTime >= nextHierarchyCheckTime)
        {
            nextHierarchyCheckTime = Time.unscaledTime + Mathf.Max(0.1f, hierarchyCheckInterval);
            if (GetSourceRendererCount() != bindings.Count)
            {
                Refresh();
            }
        }

        SyncShadows();
    }

    public void Refresh()
    {
        ClearShadows();

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer source = renderers[i];
            if (!IsValidSource(source))
            {
                continue;
            }

            GameObject shadowObject = new GameObject(ShadowObjectName);
            shadowObject.layer = source.gameObject.layer;
            shadowObject.transform.SetParent(source.transform, false);

            SpriteRenderer shadow = shadowObject.AddComponent<SpriteRenderer>();
            bindings.Add(new ShadowBinding
            {
                source = source,
                shadow = shadow
            });
        }

        nextHierarchyCheckTime = Time.unscaledTime + Mathf.Max(0.1f, hierarchyCheckInterval);
        SyncShadows();
    }

    private void SyncShadows()
    {
        int shadowSortingOrder = GetMinimumSortingOrder() - Mathf.Abs(sortingOrderOffset);

        for (int i = 0; i < bindings.Count; i++)
        {
            ShadowBinding binding = bindings[i];
            if (binding.source == null || binding.shadow == null)
            {
                continue;
            }

            SpriteRenderer source = binding.source;
            SpriteRenderer shadow = binding.shadow;

            // 원본의 모양과 애니메이션 프레임을 그대로 따라간다.
            shadow.sprite = source.sprite;
            shadow.flipX = source.flipX;
            shadow.flipY = source.flipY;
            shadow.sharedMaterial = source.sharedMaterial;
            shadow.sortingLayerID = source.sortingLayerID;
            shadow.sortingOrder = shadowSortingOrder;
            shadow.maskInteraction = source.maskInteraction;
            shadow.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * source.color.a);
            shadow.enabled = source.enabled && source.sprite != null;

            if (source.drawMode != SpriteDrawMode.Simple)
            {
                shadow.drawMode = source.drawMode;
                shadow.size = source.size;
                shadow.tileMode = source.tileMode;
            }

            Transform shadowTransform = shadow.transform;
            shadowTransform.localPosition = source.transform.InverseTransformVector(worldOffset);
            shadowTransform.localRotation = Quaternion.identity;
            shadowTransform.localScale = Vector3.one;
        }
    }

    private int GetMinimumSortingOrder()
    {
        int minimumOrder = 0;
        bool foundRenderer = false;

        for (int i = 0; i < bindings.Count; i++)
        {
            SpriteRenderer source = bindings[i].source;
            if (source == null)
            {
                continue;
            }

            minimumOrder = foundRenderer ? Mathf.Min(minimumOrder, source.sortingOrder) : source.sortingOrder;
            foundRenderer = true;
        }

        return minimumOrder;
    }

    private bool HasMissingSource()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].source == null || bindings[i].shadow == null)
            {
                return true;
            }
        }

        return false;
    }

    private int GetSourceRendererCount()
    {
        int count = 0;
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsValidSource(renderers[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsValidSource(SpriteRenderer renderer)
    {
        return renderer != null && renderer.gameObject.name != ShadowObjectName;
    }

    private void ClearShadows()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].shadow != null)
            {
                Destroy(bindings[i].shadow.gameObject);
            }
        }

        bindings.Clear();
    }
}

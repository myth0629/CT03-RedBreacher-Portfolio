using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 SpriteRenderer로 구성된 유닛 프리팹(탱크)을 통째로 RenderTexture에 렌더링해
/// UI에서 "프리팹 모습 그대로" 아이콘으로 쓸 수 있게 한다.
/// 화면 밖 전용 스테이지에 프리팹을 복제(스크립트/물리는 제거)하고 톱다운 오쏘 카메라로 1프레임 렌더한다.
/// 결과는 프리팹별로 캐시한다.
/// </summary>
public class UnitPreviewRenderer : MonoBehaviour
{
    private const string PreviewLayerName = "UnitPreview";
    private const int TextureSize = 256;
    private static readonly Vector3 StageOrigin = new Vector3(10000f, 10000f, 10000f);

    private static UnitPreviewRenderer instance;

    private Camera previewCamera;
    private int previewLayer;
    private readonly Dictionary<int, RenderTexture> cache = new Dictionary<int, RenderTexture>();

    public static UnitPreviewRenderer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("UnitPreviewRenderer");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<UnitPreviewRenderer>();
                instance.Initialize();
            }

            return instance;
        }
    }

    private void Initialize()
    {
        previewLayer = LayerMask.NameToLayer(PreviewLayerName);

        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.SetParent(transform, false);
        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // 투명 배경
        previewCamera.cullingMask = previewLayer >= 0 ? (1 << previewLayer) : ~0;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 1000f;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.enabled = false; // 수동 Render만 사용
    }

    public RenderTexture GetPreview(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        int key = prefab.GetInstanceID();
        if (cache.TryGetValue(key, out RenderTexture cached) && cached != null)
        {
            return cached;
        }

        RenderTexture rt = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "UnitPreview_" + prefab.name
        };
        rt.Create();
        RenderPrefab(prefab, rt);
        cache[key] = rt;
        return rt;
    }

    private void RenderPrefab(GameObject prefab, RenderTexture target)
    {
        // 비활성 부모 아래에 복제 → 인스턴스의 Awake/OnEnable이 실행되지 않는다.
        GameObject stage = new GameObject("PreviewStage");
        stage.transform.SetParent(transform, false);
        stage.transform.position = StageOrigin;
        stage.SetActive(false);

        GameObject unit = Instantiate(prefab, stage.transform);
        unit.transform.localPosition = Vector3.zero;

        // 렌더에 필요한 SpriteRenderer만 남기고 스크립트/물리는 제거(활성화 전이라 Awake 미실행).
        foreach (MonoBehaviour mb in unit.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null)
            {
                DestroyImmediate(mb);
            }
        }

        foreach (Rigidbody rb in unit.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb != null)
            {
                DestroyImmediate(rb);
            }
        }

        foreach (Collider col in unit.GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
            {
                DestroyImmediate(col);
            }
        }

        SetLayerRecursively(stage, previewLayer >= 0 ? previewLayer : 0);
        stage.SetActive(true);

        FrameCamera(unit);

        RenderTexture previous = previewCamera.targetTexture;
        previewCamera.targetTexture = target;
        previewCamera.Render();
        previewCamera.targetTexture = previous;

        Destroy(stage);
    }

    private void FrameCamera(GameObject unit)
    {
        Bounds bounds;
        Vector3 center = StageOrigin;
        if (TryGetSpriteBounds(unit, out bounds))
        {
            center = bounds.center;
            float halfExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            previewCamera.orthographicSize = Mathf.Max(0.1f, halfExtent * 1.15f);
        }
        else
        {
            previewCamera.orthographicSize = 3f;
        }

        // 톱다운: 위(+Y)에서 내려다보며, 차량 전방(+Z)이 아이콘 위쪽을 향하게 한다.
        previewCamera.transform.position = center + Vector3.up * 100f;
        previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
    }

    private static bool TryGetSpriteBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool initialized = false;
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = sr.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        return initialized;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}

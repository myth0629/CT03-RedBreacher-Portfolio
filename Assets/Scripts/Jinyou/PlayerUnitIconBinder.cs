using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대상 RawImage를 현재 플레이어 유닛(<see cref="PlayerUnitConfig"/>) 프리팹의
/// "렌더된 모습 그대로"(본체+포탑+포신+트랙 등 전체)와 동기화한다.
/// 활성화(팝업 열림)될 때마다 갱신하므로 유닛을 교체하면 아이콘도 따라간다.
/// 실제 렌더링은 <see cref="UnitPreviewRenderer"/>가 RenderTexture로 처리한다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerUnitIconBinder : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private PlayerController player;

    private void Reset()
    {
        targetImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        Sync();
    }

    public void Sync()
    {
        if (targetImage == null)
        {
            return;
        }

        player ??= FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        PlayerUnitConfig config = player != null ? player.UnitConfig : null;
        GameObject prefab = config != null ? config.UnitPrefab : null;
        if (prefab == null)
        {
            return;
        }

        RenderTexture rt = UnitPreviewRenderer.Instance.GetPreview(prefab);
        if (rt != null)
        {
            targetImage.texture = rt;
            targetImage.color = Color.white;
        }
    }
}

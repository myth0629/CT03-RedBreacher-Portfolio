using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerBossDodgeController : MonoBehaviour
{
    [Header("보스전 드래그 회피")]
    [SerializeField] private float minimumDragDistance = 40f;
    [SerializeField] private float dodgeDistance = 2.5f;
    [SerializeField] private float dodgeDuration = 0.2f;
    [SerializeField] private float invulnerabilityDuration = 0.25f;
    [SerializeField] private float dodgeCooldown = 1.5f;
    [SerializeField] private float collisionRadius = 0.45f;
    [SerializeField] private float wallClearance = 0.05f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("회피 쿨타임 UI")]
    [SerializeField] private GameObject dodgeUiRoot;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private PlayerController player;
    private BossEncounterManager bossEncounterManager;
    private CombatHealth health;
    private Camera mainCamera;
    private Vector2 dragStartPosition;
    private Vector3 dodgeStartPosition;
    private Vector3 dodgeDestination;
    private float dodgeElapsed;
    private float nextDodgeTime;
    private int activeTouchId = -1;
    private bool isDragging;

    public bool IsDodging { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextDodgeTime - Time.time);

    public static PlayerBossDodgeController Ensure(PlayerController owner)
    {
        PlayerBossDodgeController controller = owner.GetComponent<PlayerBossDodgeController>();
        if (controller == null)
        {
            controller = owner.gameObject.AddComponent<PlayerBossDodgeController>();
        }

        controller.player = owner;
        controller.health = owner.Health;
        return controller;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<CombatHealth>();
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (obstacleMask.value == 0 && wallLayer >= 0)
        {
            obstacleMask = 1 << wallLayer;
        }
    }

    private void Update()
    {
        ResolveReferences();
        if (!CanReceiveDodgeInput())
        {
            CancelDrag();
            IsDodging = false;
            RefreshCooldownUi(false);
            return;
        }

        RefreshCooldownUi(true);
        if (IsDodging)
        {
            UpdateDodge();
            return;
        }

        HandleTouchInput();
        HandleMouseInput();
    }

    private bool CanReceiveDodgeInput()
    {
        return bossEncounterManager != null
            && bossEncounterManager.IsEncounterActive
            && health != null
            && !health.IsDead;
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
        {
            return;
        }

        if (!isDragging)
        {
            foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                Vector2 position = touch.position.ReadValue();
                if (!IsPointerOverUi(position))
                {
                    BeginDrag(position, touch.touchId.ReadValue());
                }
                return;
            }
        }

        foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
        {
            if (touch.touchId.ReadValue() != activeTouchId)
            {
                continue;
            }

            if (touch.press.wasReleasedThisFrame)
            {
                EndDrag(touch.position.ReadValue());
            }
            return;
        }
    }

    private void HandleMouseInput()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        Vector2 position = Mouse.current.position.ReadValue();
        if (!isDragging && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUi(position))
        {
            BeginDrag(position, -1);
        }
        else if (isDragging && activeTouchId < 0 && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag(position);
        }
    }

    private void BeginDrag(Vector2 screenPosition, int touchId)
    {
        dragStartPosition = screenPosition;
        activeTouchId = touchId;
        isDragging = true;
    }

    private void EndDrag(Vector2 screenPosition)
    {
        Vector2 dragDelta = screenPosition - dragStartPosition;
        CancelDrag();

        if (Time.time < nextDodgeTime || dragDelta.magnitude < Mathf.Max(1f, minimumDragDistance))
        {
            return;
        }

        Vector3 direction = GetWorldDirection(dragDelta.normalized);
        if (direction.sqrMagnitude > 0f)
        {
            StartDodge(direction);
        }
    }

    private void StartDodge(Vector3 direction)
    {
        dodgeStartPosition = CombatPlane.WithFixedY(transform.position);
        float distance = Mathf.Max(0f, dodgeDistance);

        // 벽이 있으면 충돌 지점보다 플레이어 반경만큼 앞에서 회피를 끝낸다.
        if (obstacleMask.value != 0
            && Physics.SphereCast(
                dodgeStartPosition,
                Mathf.Max(0.01f, collisionRadius),
                direction,
                out RaycastHit hit,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0f, hit.distance - Mathf.Max(0f, wallClearance));
        }

        if (distance <= 0.01f)
        {
            return;
        }

        dodgeDestination = CombatPlane.WithFixedY(dodgeStartPosition + direction * distance);
        dodgeElapsed = 0f;
        IsDodging = true;
        nextDodgeTime = Time.time + Mathf.Max(0f, dodgeCooldown);
        health?.SetTemporaryInvulnerability(invulnerabilityDuration);
    }

    private void UpdateDodge()
    {
        float duration = Mathf.Max(0.01f, dodgeDuration);
        dodgeElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(dodgeElapsed / duration);
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        transform.position = CombatPlane.WithFixedY(
            Vector3.Lerp(dodgeStartPosition, dodgeDestination, easedProgress));

        if (progress >= 1f)
        {
            IsDodging = false;
        }
    }

    private Vector3 GetWorldDirection(Vector2 screenDirection)
    {
        mainCamera ??= Camera.main;
        if (mainCamera == null)
        {
            return CombatPlane.ProjectDirection(new Vector3(screenDirection.x, 0f, screenDirection.y));
        }

        Vector3 cameraUp = CombatPlane.ProjectDirection(mainCamera.transform.up);
        if (cameraUp.sqrMagnitude <= 0f)
        {
            cameraUp = CombatPlane.ProjectDirection(mainCamera.transform.forward);
        }

        Vector3 cameraRight = CombatPlane.ProjectDirection(mainCamera.transform.right);
        return CombatPlane.ProjectDirection(cameraRight * screenDirection.x + cameraUp * screenDirection.y);
    }

    private bool IsPointerOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            if (hitObject.GetComponentInParent<Selectable>() != null
                || hitObject.GetComponentInParent<ScrollRect>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        bossEncounterManager ??= FindFirstObjectByType<BossEncounterManager>();
        mainCamera ??= Camera.main;
    }

    private void CancelDrag()
    {
        isDragging = false;
        activeTouchId = -1;
    }

    private void RefreshCooldownUi(bool encounterActive)
    {
        if (dodgeUiRoot != null && dodgeUiRoot.activeSelf != encounterActive)
        {
            dodgeUiRoot.SetActive(encounterActive);
        }

        float cooldown = Mathf.Max(0.01f, dodgeCooldown);
        float remaining = CooldownRemaining;
        if (cooldownOverlay != null)
        {
            // 스킬 아이콘처럼 회피 준비 진행도를 이미지 Fill로 표시한다.
            cooldownOverlay.fillAmount = 1f - Mathf.Clamp01(remaining / cooldown);
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(remaining > 0f);
            if (remaining > 0f)
            {
                cooldownText.text = remaining >= 1f
                    ? Mathf.CeilToInt(remaining).ToString()
                    : remaining.ToString("0.0");
            }
        }
    }
}

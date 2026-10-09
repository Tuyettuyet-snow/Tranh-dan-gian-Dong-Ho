using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class StaircaseLocomotion : MonoBehaviour
{
    [Header("Waypoints dọc cầu thang (chân → đỉnh)")]
    [SerializeField] private Transform[] waypoints;

    [Header("Tốc độ di chuyển")]
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("Khoảng cách activate (m)")]
    [SerializeField] private float activateDistance = 2.0f;

    [Header("Độ nhấp nhô bước chân (0 = tắt)")]
    [SerializeField] private float bobbingAmount = 0.05f;

    [Header("Hint UI")]
    [SerializeField] private HintUIManager hintUI;

    private ContinuousMoveProvider continuousMoveProvider;
    private Transform xrRig;
    private CharacterController cc;

    private bool isMoving = false;
    private bool isNearStair = false;
    private int currentWPIndex = -1;

    // Biến để lưu lại lớp Layer của cầu thang (dùng cho việc dò mặt đất)
    // Hãy tạo một Layer mới tên "Stair" và gán cho mô hình cầu thang cong của bạn
    public LayerMask stairLayerMask = ~0;

    private IEnumerator Start()
    {
        yield return null;

        xrRig = GameObject.Find("XRRig_WebXR")?.transform;
        cc = xrRig?.GetComponent<CharacterController>();
        continuousMoveProvider = FindFirstObjectByType<ContinuousMoveProvider>();

        if (xrRig == null) Debug.LogError("[Staircase] Không tìm thấy XRRig_WebXR!");
    }

    private void Update()
    {
        if (xrRig == null || waypoints == null || waypoints.Length < 2) return;

        CheckNearStair();

#if UNITY_EDITOR
        if (!isMoving) HandleKeyboardInput();
#else
        if (!isMoving && isNearStair) HandleVRInput();
#endif
    }

    private void CheckNearStair()
    {
        Transform cam = Camera.main?.transform;
        if (cam == null) return;
        if (waypoints[0] == null || waypoints[^1] == null) return;

        float distBottom = Vector3.Distance(
            new Vector3(cam.position.x, 0, cam.position.z),
            new Vector3(waypoints[0].position.x, 0, waypoints[0].position.z)
        );

        float distTop = Vector3.Distance(
            new Vector3(cam.position.x, 0, cam.position.z),
            new Vector3(waypoints[^1].position.x, 0, waypoints[^1].position.z)
        );

        bool wasNear = isNearStair;
        bool nearBottom = distBottom < activateDistance;
        bool nearTop = distTop < activateDistance;
        isNearStair = nearBottom || nearTop || (currentWPIndex != -1);

        if (isNearStair && !wasNear) ToggleLocomotion(false);
        if (!isNearStair && wasNear)
        {
            hintUI?.HideHint();
            ToggleLocomotion(true);
        }

        if (isNearStair && !isMoving)
        {
            if (currentWPIndex != -1) hintUI?.ShowHint("F / G", "Lên ↑  |  Xuống ↓");
            else if (nearBottom && nearTop) hintUI?.ShowHint("F / G", "Lên ↑  |  Xuống ↓");
            else if (nearBottom) hintUI?.ShowHint("F", "Bấm F để lên");
            else if (nearTop) hintUI?.ShowHint("G", "Bấm G để xuống");
        }
    }

    private void ToggleLocomotion(bool state)
    {
        if (continuousMoveProvider != null) continuousMoveProvider.enabled = state;
    }

    private void HandleKeyboardInput()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.fKey.wasPressedThisFrame) TryMoveUp();
        if (keyboard.gKey.wasPressedThisFrame) TryMoveDown();
    }

    private void HandleVRInput()
    {
        if (!XRSettings.isDeviceActive) return;
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 axis))
        {
            if (axis.y > 0.7f && !isMoving) TryMoveUp();
            else if (axis.y < -0.7f && !isMoving) TryMoveDown();
        }
    }

    private void TryMoveUp()
    {
        if (!isNearStair || isMoving) return;
        if (currentWPIndex == -1)
        {
            float distBottom = Vector3.Distance(xrRig.position, waypoints[0].position);
            float distTop = Vector3.Distance(xrRig.position, waypoints[^1].position);
            if (distBottom <= distTop) currentWPIndex = 0;
            else return;
        }
        if (currentWPIndex >= waypoints.Length - 1) return;
        currentWPIndex++;
        StartCoroutine(MoveToSingleWaypoint(waypoints[currentWPIndex]));
    }

    private void TryMoveDown()
    {
        if (!isNearStair || isMoving) return;
        if (currentWPIndex == -1)
        {
            float distBottom = Vector3.Distance(xrRig.position, waypoints[0].position);
            float distTop = Vector3.Distance(xrRig.position, waypoints[^1].position);
            if (distTop <= distBottom) currentWPIndex = waypoints.Length - 1;
            else return;
        }
        if (currentWPIndex <= 0) return;
        currentWPIndex--;
        StartCoroutine(MoveToSingleWaypoint(waypoints[currentWPIndex]));
    }

    private IEnumerator MoveToSingleWaypoint(Transform targetWP)
    {
        isMoving = true;
        ToggleLocomotion(false);
        if (cc != null) cc.enabled = false;
        hintUI?.HideHint();

        Vector3 startPos = xrRig.position;
        Vector3 targetPos = targetWP.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 basePosition = Vector3.Lerp(startPos, targetPos, t);
            float stepBounce = Mathf.Sin(t * Mathf.PI) * bobbingAmount;
            basePosition.y += stepBounce;
            xrRig.position = basePosition;
            yield return null;
        }

        // ================= CẬP NHẬT MỚI CỰC QUAN TRỌNG Ở ĐÂY =================
        // HẠ CÁNH MỀM (SOFT LANDING) DÒ TÌM MẶT ĐẤT

        Vector3 finalPos = targetPos;
        // Bắn tia laser thẳng từ trên cao (0.5m) xuống ngay tại vị trí Waypoint
        RaycastHit hit;
        if (Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out hit, 1.0f, stairLayerMask))
        {
            // Nếu tia laser chạm vào mặt Mesh cong của bậc thang
            // Ép vị trí trục Y của nhân vật cao hơn điểm chạm đúng một chút (offset)
            float safeOffset = 0.05f; // Nổi lên 5cm để an toàn
            finalPos.y = hit.point.y + safeOffset;
        }

        xrRig.position = finalPos;
        // ======================================================================

        isMoving = false;

        if (currentWPIndex == 0 || currentWPIndex == waypoints.Length - 1)
        {
            // DesktopVRController.Instance?.ResetVerticalVelocity(); 
            if (cc != null) cc.enabled = true;
            ToggleLocomotion(true);
            currentWPIndex = -1;
            Debug.Log("[Staircase] Đã rời khỏi cầu thang an toàn!");
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            Gizmos.DrawSphere(waypoints[i].position, 0.05f); // Thu nhỏ Gizmo cho dễ nhìn
        }
        Gizmos.DrawSphere(waypoints[^1].position, 0.1f);
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        if (waypoints[0] != null) Gizmos.DrawSphere(waypoints[0].position, activateDistance);
        if (waypoints[^1] != null) Gizmos.DrawSphere(waypoints[^1].position, activateDistance);
    }
}
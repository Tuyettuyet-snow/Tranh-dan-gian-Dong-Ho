using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class EditorVRTestHelper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactDistance = 10f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = GetComponent<Camera>();
        if (mainCam == null) mainCam = Camera.main;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers))
            {
                Debug.Log($"[Editor Test] Click vào: {hit.collider.gameObject.name}");

                // 1. Ưu tiên DoorAnimator (animation mở cửa trước)
                var doorAnim = hit.collider.GetComponentInParent<DoorAnimatorRight>();
                if (doorAnim != null)
                {
                    Debug.Log("[Editor Test] Kích hoạt DoorAnimatorRight!");
                    doorAnim.ToggleDoor();
                    return;
                }

                // 2. Doortransition (chuyển scene không có animation)
                var door = hit.collider.GetComponentInParent<Doortransition>();
                if (door != null)
                {
                    Debug.Log("[Editor Test] Kích hoạt Doortransition!");
                    door.TriggerTransition();
                    return;
                }

                // 3. WaypointTeleport
                var waypoint = hit.collider.GetComponentInParent<WaypointTeleport>();
                if (waypoint != null)
                {
                    waypoint.Teleport();
                    return;
                }

                // 4. XR Interactable thông thường
                var interactable = hit.collider.GetComponentInParent<XRBaseInteractable>();
                if (interactable != null)
                {
                    Debug.Log($"[Editor Test] XR Interactable: {interactable.gameObject.name}");
                }
            }
        }
#endif
    }
}
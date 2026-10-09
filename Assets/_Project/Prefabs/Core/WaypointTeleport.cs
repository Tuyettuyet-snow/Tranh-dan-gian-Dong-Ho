using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WaypointTeleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform targetPosition;   // điểm đích
    [SerializeField] private GameObject arrowIndicator;  // mũi tên 3D gắn kèm
    [SerializeField] private float arrowBobSpeed = 1.5f;
    [SerializeField] private float arrowBobAmount = 0.1f;

    private XRBaseInteractable interactable;
    private Transform xrRig;
    private Vector3 arrowStartPos;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        // Tìm XR Rig trong scene
        xrRig = GameObject.FindWithTag("Player")?.transform
                ?? GameObject.Find("XRRig_WebXR")?.transform;

        if (arrowIndicator != null)
            arrowStartPos = arrowIndicator.transform.localPosition;
    }

    private void OnEnable()
    {
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void Update()
    {
        // Animation bob lên xuống cho mũi tên
        if (arrowIndicator != null)
        {
            float newY = arrowStartPos.y + Mathf.Sin(Time.time * arrowBobSpeed) * arrowBobAmount;
            arrowIndicator.transform.localPosition = new Vector3(
                arrowStartPos.x, newY, arrowStartPos.z);
        }
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Teleport();
    }

    // Gọi từ EditorVRTestHelper khi click chuột (Editor test)
    public void Teleport()
    {
        if (xrRig == null || targetPosition == null) return;
        xrRig.position = targetPosition.position;
        Debug.Log($"[Waypoint] Teleport tới: {targetPosition.name}");
    }
}
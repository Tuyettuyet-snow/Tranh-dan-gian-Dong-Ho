using UnityEngine;
using UnityEngine.InputSystem;

public class PCMouseLook : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Sensitivity")]
    [SerializeField] private float sensitivity = 0.1f;

    [Header("Vertical Limit")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * sensitivity;
        pitch -= mouseDelta.y * sensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        // Xoay người chơi theo chiều ngang
        transform.rotation =
            Quaternion.Euler(0f, yaw, 0f);

        // Xoay camera theo chiều dọc
        cameraTransform.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
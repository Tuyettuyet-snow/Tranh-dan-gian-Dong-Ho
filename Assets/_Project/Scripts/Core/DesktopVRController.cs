#if !UNITY_ANDROID
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class DesktopVRController : MonoBehaviour
{
    [Header("Tốc độ")]
    [SerializeField] private float moveSpeed = 1f;      // tốc độ di chuyển
    [SerializeField] private float lookSpeed = 1f;      // tốc độ xoay camera

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private Transform xrRig;
    private CharacterController characterController;    // thêm mới
    private float rotX = 0f;
    private float rotY = 0f;
    private bool isDesktopMode = false;
    private float verticalVelocity = 0f;               // thêm mới - xử lý gravity
    private float gravity = -9.81f;                    // thêm mới - trọng lực
    public static DesktopVRController Instance;
    private void Awake()
    {
        xrRig = GameObject.Find("XRRig_WebXR")?.transform;

        // Tìm CharacterController trên XRRig
        if (xrRig != null)
            characterController = xrRig.GetComponent<CharacterController>();
    }

    // Gọi từ StaircaseLocomotion sau khi leo xong
    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
        Debug.Log("[DesktopVR] Reset verticalVelocity về 0");
    }

    private void Start()
    {
        isDesktopMode = !XRSettings.isDeviceActive;
        Debug.Log($"[DesktopVR] Chế độ: {(isDesktopMode ? "Desktop" : "VR thật")}");

        if (isDesktopMode)
        {   

            rotY = xrRig.eulerAngles.y ;
            xrRig.rotation = Quaternion.Euler(0, rotY, 0);

            rotX = 0f; // camera nhìn thẳng về phía trước

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(0, 0, 0);


            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!isDesktopMode || xrRig == null) return;

        HandleLook();
        HandleMove();
        HandleCursorToggle();
    }

    private void HandleLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Dùng lookSpeed thay vì hardcode 1f
        float mouseX = mouse.delta.x.ReadValue() * lookSpeed * Time.deltaTime;
        float mouseY = mouse.delta.y.ReadValue() * lookSpeed * Time.deltaTime;

        rotY += mouseX;
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -80f, 80f);

        xrRig.rotation = Quaternion.Euler(0, rotY, 0);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(rotX, 0, 0);
    }

    private void HandleMove()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;


        Vector3 forward = new Vector3(
            Mathf.Sin(rotY * Mathf.Deg2Rad), 0,
            Mathf.Cos(rotY * Mathf.Deg2Rad));
        Vector3 right = new Vector3(
            Mathf.Cos(rotY * Mathf.Deg2Rad), 0,
            -Mathf.Sin(rotY * Mathf.Deg2Rad));

        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move -= forward;
        if (keyboard.sKey.isPressed) move += forward;
        if (keyboard.aKey.isPressed) move += right;
        if (keyboard.dKey.isPressed) move -= right;
        move.y = 0;

        if (characterController != null && characterController.enabled)
        {
            if (characterController.isGrounded)
                verticalVelocity = 0f;
            else
            {
                // Chỉ apply gravity khi KHÔNG đang leo cầu thang
                verticalVelocity += gravity * Time.deltaTime;

                // Giới hạn tốc độ rơi tối đa
                verticalVelocity = Mathf.Max(verticalVelocity, -10f);
            }

            Vector3 motion = move.normalized * moveSpeed * Time.deltaTime;
            motion.y = verticalVelocity * Time.deltaTime;
            characterController.Move(motion);
        }

    }

    private void HandleCursorToggle()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
#endif
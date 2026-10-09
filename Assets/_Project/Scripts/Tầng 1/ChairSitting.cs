using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

namespace DongHoVR.Tang1
{
    public class ChairSitting : MonoBehaviour
    {
        [Header("Cac vi tri ngoi tren ghe nay")]
        public Transform[] sitPositions;

        public float interactDistance = 2.0f;

        [Header("Cac Locomotion can khoa khi ngoi")]
        public Behaviour[] locomotionProvidersToDisable;

        [Header("Click chuot de tuong tac (Desktop test)")]
        public bool allowMouseClick = true;

        [Tooltip("Khoảng cách tối đa của tia Raycast từ Camera khi click chuột.")]
        public float clickRaycastMaxDistance = 100f;

        private XROrigin xrOrigin;
        private UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider[] allLocomotionProviders;

               private UnityEngine.XR.Interaction.Toolkit.Locomotion.XRBodyTransformer bodyTransformer;

        private bool isSitting = false;
        private Transform activeSitPosition;
        private Vector3 originalOriginPos;
        private Quaternion originalOriginRot;

        public static bool IsAnyPlayerSitting { get; private set; }

        private void Start()
        {
            xrOrigin = FindFirstObjectByType<XROrigin>();
            allLocomotionProviders = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(FindObjectsSortMode.None);

            bool hasRealVrHeadset = XRSettings.isDeviceActive;

            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

            if (interactable != null)
            {
                interactable.enabled = hasRealVrHeadset;
            }
        }

        private void Update()
        {
            if (xrOrigin == null)
                return;

            bool mouseClicked = allowMouseClick
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame;

            if (!mouseClicked)
                return;

            Camera cam = xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

            if (cam == null || Mouse.current == null)
                return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mouseScreenPosition);

            bool didHit = Physics.Raycast(ray, out RaycastHit hit, clickRaycastMaxDistance);
            bool hitThisChair = didHit && (hit.transform == transform || hit.transform.IsChildOf(transform));

            if (hitThisChair)
            {
                if (!isSitting)
                {
                    // Không cho NGỒI trong lúc đang ZOOM vào 1 bức tranh khác -
                    // nếu cho phép, originalOriginPos sẽ lưu NHẦM vị trí đang
                    // zoom (không phải chỗ đứng thật ban đầu), và cờ
                    // IsAnyPlayerZoomed sẽ bị kẹt true mãi mãi sau khi đứng dậy.
                    if (ArtworkInteraction.IsAnyPlayerZoomed)
                    {
                        Debug.Log("[ChairSitting] Dang zoom vao tranh, khong the ngoi ghe luc nay.");
                    }
                    else
                    {
                        TrySitAtPoint(hit.point);
                    }
                }
                // Đang ngồi mà click lại đúng ghế -> không làm gì (đã ngồi rồi).
            }
            else if (isSitting)
            {
               
                bool hitOtherInteractable = didHit
                    && hit.transform.GetComponentInParent<ArtworkInteraction>() != null;

                if (!hitOtherInteractable)
                {
                    StandUp();
                }
            }
        }
        public void OnLaserSelect(SelectEnterEventArgs args)
        {
            if (xrOrigin == null)
                return;

            if (isSitting)
            {
                StandUp();
                return;
            }

            if (ArtworkInteraction.IsAnyPlayerZoomed)
            {
                Debug.Log("[ChairSitting] Dang zoom vao tranh, khong the ngoi ghe luc nay.");
                return;
            }

            Vector3 referencePoint = args.interactorObject.transform.position;
            TrySitAtPoint(referencePoint);
        }

        private void TrySitAtPoint(Vector3 referencePoint)
        {
            Transform nearestSeat = FindNearestSeatToPoint(referencePoint);

            if (nearestSeat != null)
            {
                SitDown(nearestSeat);
            }
            else
            {
                Debug.LogWarning("[ChairSitting] Da tuong tac trung ghe nhung khong co cho ngoi nao du gan!");
            }
        }

        private Transform FindNearestSeatToPoint(Vector3 referencePoint)
        {
            if (sitPositions == null || sitPositions.Length == 0)
                return null;

            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Transform seat in sitPositions)
            {
                if (seat == null)
                    continue;

                float distance = Vector3.Distance(referencePoint, seat.position);

                if (distance <= interactDistance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = seat;
                }
            }

            return nearest;
        }

        private void SitDown(Transform targetSeat)
        {
            activeSitPosition = targetSeat;

            Transform camTransform = xrOrigin.Camera != null
                ? xrOrigin.Camera.transform
                : xrOrigin.transform;

            // Lưu lại vị trí + góc xoay GỐC TRƯỚC khi thay đổi gì cả, để
            // StandUp() đưa Camera về đúng chỗ đứng ban đầu.
            originalOriginPos = xrOrigin.transform.position;
            originalOriginRot = xrOrigin.transform.rotation;

            // Xoay Rig quanh VỊ TRÍ CAMERA để hướng nhìn khớp với hướng ghế.
            Vector3 currentCameraForward = camTransform.forward;
            currentCameraForward.y = 0f;

            Vector3 desiredForward = targetSeat.forward;
            desiredForward.y = 0f;

            if (currentCameraForward.sqrMagnitude > 0.0001f && desiredForward.sqrMagnitude > 0.0001f)
            {
                float angleDelta = Vector3.SignedAngle(
                    currentCameraForward.normalized,
                    desiredForward.normalized,
                    Vector3.up
                );

                xrOrigin.transform.RotateAround(camTransform.position, Vector3.up, angleDelta);
            }

            // Dịch chuyển để Camera khớp CHÍNH XÁC vào vị trí + độ cao điểm ngồi.
            Vector3 headToOriginOffset = xrOrigin.transform.position - camTransform.position;
            xrOrigin.transform.position = targetSeat.position + headToOriginOffset;

            SetLocomotionEnabled(false);

            isSitting = true;
            IsAnyPlayerSitting = true;
            Debug.Log($"--> DA NGOI XUONG GHE '{targetSeat.name}' THANH CONG! <--");
        }

        private void StandUp()
        {
            isSitting = false;
            IsAnyPlayerSitting = false;
            activeSitPosition = null;

            if (xrOrigin != null)
            {
                xrOrigin.transform.position = originalOriginPos;
                xrOrigin.transform.rotation = originalOriginRot;
            }

            Debug.Log("--> DA DUNG DAY KHOI GHE, DA VE DUNG VI TRI BAN DAU! <--");

            SetLocomotionEnabled(true);
        }

        private void SetLocomotionEnabled(bool enabled)
        {
            if (locomotionProvidersToDisable != null && locomotionProvidersToDisable.Length > 0)
            {
                foreach (Behaviour provider in locomotionProvidersToDisable)
                {
                    if (provider != null)
                    {
                        provider.enabled = enabled;
                    }
                }

                return;
            }

            if (allLocomotionProviders == null)
                return;

            foreach (var provider in allLocomotionProviders)
            {
                if (provider != null)
                {
                    provider.enabled = enabled;
                }
            }
        }
    }
}
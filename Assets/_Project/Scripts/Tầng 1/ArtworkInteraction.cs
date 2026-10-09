using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DongHoVR.Tang1
{
    public class ArtworkInteraction : MonoBehaviour
    {
        [Header("Cấu hình Camera Zoom khi bấm vào tranh")]
        [Tooltip("Vị trí + hướng mà CAMERA (đầu người chơi) sẽ khớp vào khi zoom vào tranh.")]
        public Transform zoomTargetPosition;

        [Min(0.1f)]
        [Tooltip("Tốc độ zoom. Thời gian zoom (giây) = 2 / Zoom Speed.")]
        public float zoomSpeed = 2f;

        [Header("Giao diện & Âm thanh")]
        public GameObject infoPanel;

        [Tooltip("Kéo file nhạc nền chung vào ĐÚNG 1 tranh duy nhất. Các tranh còn lại để trống.")]
        public AudioClip backgroundMusicClip;

        [Tooltip("Kéo file thuyết minh của bức tranh này vào đây")]
        public AudioClip artworkAudioClip;

        [Header("Am luong nhac nen")]
        [Range(0f, 1f)]
        [Tooltip("Âm lượng nhạc nền bình thường. Khi đang phát thuyết minh, nhạc nền luôn bị đặt = 0.")]
        public float normalBackgroundVolume = 1f;

        [Header("Vi tri Bang thong tin truoc mat nguoi choi")]
        public float infoPanelDistance = 1.2f;
        public float infoPanelHeightOffset = 0f;
        [Tooltip("Bật nếu bảng thông tin bị NGƯỢC MẶT (chữ bị lật/khuất).")]
        public bool flipInfoPanelFacing = false;

        [Header("3 vung bam rieng biet (de nhan dien click chuot)")]
        public Transform paintingSurface;
        public Transform titlePlate;
        public Transform musicButton;

        [Header("Click chuot de test tren Desktop")]
        public bool allowMouseClick = true;
        public float clickRaycastMaxDistance = 100f;

        // Chỉ có DUY NHẤT 1 AudioSource nhạc nền cho cả Scene.
        private static AudioSource sharedBackgroundAudioSource;

        // Tranh đang phát thuyết minh. Chỉ 1 thuyết minh được phát tại 1 thời điểm.
        private static ArtworkInteraction activeNarrationOwner;

        private static bool zoomedFlag;
        private static bool zoomMoving;

        /// <summary>True khi đang zoom HOẶC camera còn đang bay vào/ra (chưa xong).</summary>
        public static bool IsAnyPlayerZoomed => zoomedFlag || zoomMoving;

        private AudioSource artworkAudioSource;
        private bool isZoomed = false;
        private Vector3 originalOriginPos;
        private Quaternion originalOriginRot;
        private XROrigin xrOrigin;
        private Coroutine zoomCoroutine;
        private bool wasArtworkAudioPlaying = false;

        private UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider[] allLocomotionProviders;

        // Các component do SCRIPT NÀY tắt khi zoom (để chỉ bật lại đúng những cái đó).
        private readonly List<Behaviour> disabledByUs = new List<Behaviour>();

        // "Ghim" đầu vào điểm zoom (chỉ PC) để không bị lệch/trôi/bay.
        private bool headPinned;
        private Vector3 pinnedHeadPosition;

        // Reset biến static mỗi lần vào Play Mode (phòng trường hợp tắt Reload Domain).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sharedBackgroundAudioSource = null;
            activeNarrationOwner = null;
            zoomedFlag = false;
            zoomMoving = false;
        }

        private void Start()
        {
            if (infoPanel != null)
                infoPanel.SetActive(false);

            xrOrigin = FindFirstObjectByType<XROrigin>();

            allLocomotionProviders = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(FindObjectsSortMode.None);

            if (sharedBackgroundAudioSource == null && backgroundMusicClip != null)
            {
                sharedBackgroundAudioSource = gameObject.AddComponent<AudioSource>();
                sharedBackgroundAudioSource.clip = backgroundMusicClip;
                sharedBackgroundAudioSource.loop = true;
                sharedBackgroundAudioSource.playOnAwake = false;
                sharedBackgroundAudioSource.volume = normalBackgroundVolume;
                sharedBackgroundAudioSource.Play();
            }
            else if (sharedBackgroundAudioSource != null && backgroundMusicClip != null)
            {
                Debug.LogWarning($"[ArtworkInteraction] '{gameObject.name}' co gan Background Music Clip " +
                                  "nhung da co 1 nhac nen chung dang chay roi - bo qua clip nay.");
            }

            if (artworkAudioClip != null)
            {
                foreach (var src in GetComponents<AudioSource>())
                {
                    if (src != sharedBackgroundAudioSource)
                    {
                        artworkAudioSource = src;
                        break;
                    }
                }

                if (artworkAudioSource == null)
                {
                    artworkAudioSource = gameObject.AddComponent<AudioSource>();
                }

                artworkAudioSource.clip = artworkAudioClip;
                artworkAudioSource.playOnAwake = false;
                artworkAudioSource.loop = false;
            }

            bool hasRealVrHeadset = XRSettings.isDeviceActive;

            SetInteractableEnabled(gameObject, hasRealVrHeadset);
            SetInteractableEnabled(paintingSurface != null ? paintingSurface.gameObject : null, hasRealVrHeadset);
            SetInteractableEnabled(titlePlate != null ? titlePlate.gameObject : null, hasRealVrHeadset);
            SetInteractableEnabled(musicButton != null ? musicButton.gameObject : null, hasRealVrHeadset);
            SetInteractableEnabled(infoPanel, hasRealVrHeadset);
        }

        private void OnDestroy()
        {
            EndHeadPin();

            if (isZoomed || zoomCoroutine != null)
            {
                isZoomed = false;
                zoomedFlag = false;
                zoomMoving = false;
                SetLocomotionEnabled(true);
            }

            if (activeNarrationOwner == this)
            {
                activeNarrationOwner = null;
                RestoreBackgroundMusic();
            }
        }

        private void SetInteractableEnabled(GameObject target, bool enabledState)
        {
            if (target == null)
                return;

            XRSimpleInteractable interactable = target.GetComponent<XRSimpleInteractable>();

            if (interactable != null)
            {
                interactable.enabled = enabledState;
            }
        }

        private void Update()
        {
            HandleMouseClick();
            CheckArtworkAudioFinishedNaturally();
        }

        private void HandleMouseClick()
        {
            if (!allowMouseClick || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            TryHandleMouseClick();
        }

        private void CheckArtworkAudioFinishedNaturally()
        {
            if (artworkAudioSource == null)
                return;

            bool isPlayingNow = artworkAudioSource.isPlaying;

            if (wasArtworkAudioPlaying && !isPlayingNow && activeNarrationOwner == this)
            {
                activeNarrationOwner = null;
                RestoreBackgroundMusic();
            }

            wasArtworkAudioPlaying = isPlayingNow;
        }

        private void TryHandleMouseClick()
        {
            Camera cam = (xrOrigin != null && xrOrigin.Camera != null) ? xrOrigin.Camera : Camera.main;

            if (cam == null)
                return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mouseScreenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, clickRaycastMaxDistance))
                return;

            Transform hitTransform = hit.transform;

            // Click trúng chính bảng thông tin đang mở -> TẮT nó đi, ưu tiên kiểm
            // tra trước vì bảng che toàn bộ phía trước mặt.
            if (infoPanel != null && infoPanel.activeSelf && IsSameOrChild(hitTransform, infoPanel.transform))
            {
                OnClickShowInfo();
                return;
            }

            if (IsSameOrChild(hitTransform, paintingSurface))
            {
                OnClickZoomArtwork();
            }
            else if (IsSameOrChild(hitTransform, titlePlate))
            {
                OnClickShowInfo();
            }
            else if (IsSameOrChild(hitTransform, musicButton))
            {
                OnClickToggleArtworkAudio();
            }
            else if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                OnClickZoomArtwork();
            }
        }

        private bool IsSameOrChild(Transform hitTransform, Transform target)
        {
            if (target == null)
                return false;

            return hitTransform == target || hitTransform.IsChildOf(target);
        }

        // 1. Bấm vào Bức Tranh -> Zoom camera lại gần quan sát
        public void OnClickZoomArtwork()
        {
            if (xrOrigin == null || zoomTargetPosition == null)
                return;

            // Không cho ZOOM VÀO trong lúc đang NGỒI GHẾ ở đâu đó khác - zoom ra
            // (đóng lại) vẫn luôn được phép, chỉ chặn chiều mở zoom mới.
            // Camera đang bay vào/ra -> bỏ qua click (tránh lưu nhầm vị trí gốc giữa đường).
            if (zoomMoving)
                return;

            // Không zoom VÀO khi đang ngồi ghế hoặc đang zoom 1 tranh KHÁC.
            if (!isZoomed && (ChairSitting.IsAnyPlayerSitting || IsAnyPlayerZoomed))
            {
                Debug.Log("[ArtworkInteraction] Dang ngoi ghe / zoom tranh khac, khong the zoom luc nay.");
                return;
            }

            bool zoomingIn = !isZoomed;
            isZoomed = zoomingIn;
            zoomedFlag = zoomingIn;

            // Khóa di chuyển khi zoom vào; chỉ trả khóa SAU KHI camera bay ra xong.
            if (zoomingIn)
                SetLocomotionEnabled(false);
            else
                EndHeadPin();

            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }

            if (zoomingIn)
            {
                originalOriginPos = xrOrigin.transform.position;
                originalOriginRot = xrOrigin.transform.rotation;

                Transform camTransform = xrOrigin.Camera != null
                    ? xrOrigin.Camera.transform
                    : xrOrigin.transform;

                Vector3 currentForward = camTransform.forward;
                currentForward.y = 0f;

                Vector3 desiredForward = zoomTargetPosition.forward;
                desiredForward.y = 0f;

                float angleDelta = 0f;

                if (currentForward.sqrMagnitude > 0.0001f && desiredForward.sqrMagnitude > 0.0001f)
                {
                    angleDelta = Vector3.SignedAngle(
                        currentForward.normalized,
                        desiredForward.normalized,
                        Vector3.up
                    );
                }

                Quaternion rotationDelta = Quaternion.AngleAxis(angleDelta, Vector3.up);
                Quaternion targetOriginRot = rotationDelta * xrOrigin.transform.rotation;

                Vector3 headToOriginOffset = xrOrigin.transform.position - camTransform.position;
                Vector3 rotatedOffset = rotationDelta * headToOriginOffset;
                Vector3 targetOriginPos = zoomTargetPosition.position + rotatedOffset;

                zoomCoroutine = StartCoroutine(SmoothMoveOrigin(targetOriginPos, targetOriginRot));
            }
            else
            {
                zoomCoroutine = StartCoroutine(SmoothMoveOrigin(originalOriginPos, originalOriginRot));
            }
        }

        // 2. Bấm vào Bảng Tên Tranh -> Hiện/Ẩn bảng thông tin ngay TRƯỚC MẶT người chơi
        public void OnClickShowInfo()
        {
            if (infoPanel == null)
                return;

            bool willOpen = !infoPanel.activeSelf;

            if (willOpen)
            {
                PositionInfoPanelInFrontOfPlayer();
            }

            infoPanel.SetActive(willOpen);
        }

        private void PositionInfoPanelInFrontOfPlayer()
        {
            Camera cam = (xrOrigin != null && xrOrigin.Camera != null) ? xrOrigin.Camera : Camera.main;

            if (cam == null)
                return;

            Transform camTransform = cam.transform;

            Vector3 flatForward = camTransform.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            flatForward.Normalize();

            Vector3 targetPos = camTransform.position + flatForward * infoPanelDistance;
            targetPos.y = camTransform.position.y + infoPanelHeightOffset;

            infoPanel.transform.position = targetPos;

            Vector3 lookDirection = flipInfoPanelFacing
                ? (targetPos - camTransform.position)
                : (camTransform.position - targetPos);

            infoPanel.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        // 3. Bấm vào Logo Thuyết Minh -> Bật/Tắt thuyết minh, nhạc nền = 0 khi đang phát.
        public void OnClickToggleArtworkAudio()
        {
            if (artworkAudioSource == null)
            {
                Debug.LogWarning("Chua co file am thanh thuyet minh duoc gan vao Artwork Audio Clip!");
                return;
            }

            if (artworkAudioSource.isPlaying)
            {
                artworkAudioSource.Pause();

                if (activeNarrationOwner == this)
                    activeNarrationOwner = null;

                RestoreBackgroundMusic();
                return;
            }

            if (IsAnotherNarrationPlaying())
            {
                Debug.Log($"[ArtworkInteraction] Dang phat thuyet minh cua tranh '{activeNarrationOwner.name}' - " +
                          "hay tat no truoc khi bat thuyet minh tranh khac.");
                return;
            }

            activeNarrationOwner = this;
            MuteBackgroundMusic();
            artworkAudioSource.Play();
        }

        private bool IsAnotherNarrationPlaying()
        {
            if (activeNarrationOwner == null || activeNarrationOwner == this)
                return false;

            AudioSource other = activeNarrationOwner.artworkAudioSource;

            if (other != null && other.isPlaying)
                return true;

            activeNarrationOwner = null;
            return false;
        }

        private void SetLocomotionEnabled(bool enabled)
        {
            if (!enabled)
            {
                disabledByUs.Clear();

                if (allLocomotionProviders != null)
                {
                    foreach (var provider in allLocomotionProviders)
                        DisableOne(provider);
                }

                // XR Body Transformer không phải LocomotionProvider nhưng kéo Rig về
                // độ cao sàn -> gây giật/bay camera. Tìm theo tên kiểu để khỏi lỗi biên dịch.
                if (xrOrigin != null)
                {
                    foreach (var b in xrOrigin.GetComponentsInChildren<Behaviour>(true))
                    {
                        if (b != null && b.GetType().Name == "XRBodyTransformer")
                            DisableOne(b);
                    }
                }
            }
            else
            {
                foreach (var b in disabledByUs)
                {
                    if (b != null)
                        b.enabled = true;
                }

                disabledByUs.Clear();
            }
        }

        private void DisableOne(Behaviour b)
        {
            if (b == null || !b.enabled)
                return;

            b.enabled = false;
            disabledByUs.Add(b);
        }

        // ---- Ghim đầu (chỉ PC): mỗi khung hình trước khi render, kéo Camera về đúng điểm zoom ----
        private void BeginHeadPin(Vector3 headPosition)
        {
            pinnedHeadPosition = headPosition;

            if (!headPinned)
            {
                headPinned = true;
                Application.onBeforeRender += ApplyHeadPin;
            }
        }

        private void EndHeadPin()
        {
            if (headPinned)
            {
                headPinned = false;
                Application.onBeforeRender -= ApplyHeadPin;
            }
        }

        private void ApplyHeadPin()
        {
            if (xrOrigin == null || xrOrigin.Camera == null)
                return;

            Vector3 delta = pinnedHeadPosition - xrOrigin.Camera.transform.position;

            if (delta.sqrMagnitude > 1e-8f)
                xrOrigin.transform.position += delta;
        }

        private void MuteBackgroundMusic()
        {
            if (sharedBackgroundAudioSource != null)
            {
                sharedBackgroundAudioSource.volume = 0f;
            }
        }

        private void RestoreBackgroundMusic()
        {
            if (sharedBackgroundAudioSource != null)
            {
                sharedBackgroundAudioSource.volume = normalBackgroundVolume;
            }
        }

        // Đầu (Camera) bay theo ĐƯỜNG THẲNG tới đích trong khi Rig xoay quanh đầu -
        // không còn bị vẽ cung/lệch như xoay quanh gốc Rig.
        private IEnumerator SmoothMoveOrigin(Vector3 targetPos, Quaternion targetRot)
        {
            zoomMoving = true;

            Transform originT = xrOrigin.transform;
            Transform camT = xrOrigin.Camera != null ? xrOrigin.Camera.transform : originT;

            Vector3 startPos = originT.position;
            Quaternion startRot = originT.rotation;

            // Vị trí đầu so với gốc Rig, trong hệ tọa độ của Rig (hằng số khi xoay).
            Vector3 camLocal = Quaternion.Inverse(startRot) * (camT.position - startPos);

            Vector3 startHead = startPos + startRot * camLocal;
            Vector3 endHead = targetPos + targetRot * camLocal;

            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * (zoomSpeed * 0.5f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                Quaternion rot = Quaternion.Slerp(startRot, targetRot, k);
                Vector3 head = Vector3.Lerp(startHead, endHead, k);

                originT.rotation = rot;
                originT.position = head - rot * camLocal;
                yield return null;
            }

            originT.rotation = targetRot;
            originT.position = targetPos;

            zoomMoving = false;
            zoomCoroutine = null;

            if (isZoomed)
            {
                if (!XRSettings.isDeviceActive)
                    BeginHeadPin(endHead);
            }
            else
            {
                SetLocomotionEnabled(true);
            }
        }
    }
}
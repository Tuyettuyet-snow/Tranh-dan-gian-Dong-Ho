using System.Collections;
using UnityEngine;

public class DoorAnimatorRight : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float openAngleY = 0f;
    [SerializeField] private float closedAngleY = 100f;
    [SerializeField] private float animDuration = 1.5f;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        // TẮT Animator đi — dùng code xoay thay thế
        if (anim != null) anim.enabled = false;
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
        {
            Debug.Log("[Door] Bấm H → Toggle cửa!");
            ToggleDoor();
        }
#endif
    }
    public void ToggleDoor()
    {
        if (isAnimating) return;
        isOpen = !isOpen;
        StartCoroutine(AnimateDoor(isOpen ? openAngleY : closedAngleY));
    }

    private IEnumerator AnimateDoor(float targetY)
    {
        isAnimating = true;
        float startY = transform.localEulerAngles.y;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            float currentY = Mathf.LerpAngle(startY, targetY, t);
            transform.localEulerAngles = new Vector3(
                transform.localEulerAngles.x,
                currentY,
                transform.localEulerAngles.z
            );
            yield return null;
        }

        transform.localEulerAngles = new Vector3(
            transform.localEulerAngles.x,
            targetY,
            transform.localEulerAngles.z
        );

        isAnimating = false;
        Debug.Log($"[Door] Animation xong! isOpen={isOpen}");

        if (isOpen)
        {
            yield return new WaitForSeconds(0.5f);
            var transition = GetComponent<Doortransition>();
            if (transition != null)
                transition.TriggerTransition();
            else
                Debug.LogError("[Door] Không tìm thấy Doortransition!");
        }
    }
}

using UnityEngine;

public class ArrowTrigger : MonoBehaviour
{
    [Header("Kéo Mũi Tên vào đây")]
    public AdvancedArrowController arrowController;

    public enum TriggerAction { PointToTarget, HideArrow }

    [Header("Khi người chơi chạm vào vùng này thì làm gì?")]
    public TriggerAction action;

    [Header("Nếu chọn PointToTarget, kéo đích đến vào đây")]
    public Transform targetToPoint;

    // Khi có vật chạm vào vùng Trigger
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem có phải là người chơi chạm vào không
        // Giả sử XRRig của bạn (hoặc Camera Offset) có gắn tag "Player" hoặc "MainCamera"
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            if (arrowController == null) return;

            if (action == TriggerAction.PointToTarget)
            {
                if (targetToPoint != null)
                {
                    arrowController.PointTo(targetToPoint);
                }
            }
            else if (action == TriggerAction.HideArrow)
            {
                arrowController.HideArrow();
            }

            // Tùy chọn: Xóa luôn cục Trigger này đi để nó không kích hoạt lần 2
            Destroy(gameObject);
        }
    }
}
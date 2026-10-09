using UnityEngine;

public class AdvancedArrowController : MonoBehaviour
{
    [Header("Cài đặt Người chơi")]
    public string playerRigName = "XRRig_WebXR";
    private Transform playerRig;
    private bool isPlayerFound = false;

    [Header("Hiệu ứng bay lướt")]
    public float moveSpeed = 4f;
    public float loopDistance = 5f;
    public float bobHeight = 0.2f;
    public float groundOffset = 0.5f;

    private MeshRenderer meshRenderer;

    // BIẾN QUAN TRỌNG: Mũi tên đang chỉ đi đâu, và có đang được bật không?
    private Transform currentTarget = null;
    private bool isArrowActive = false;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false; // Luôn ẩn khi mới bắt đầu game
        FindPlayerRig();
    }

    private void FindPlayerRig()
    {
        if (playerRig != null) { isPlayerFound = true; return; }
        GameObject rigObject = GameObject.Find(playerRigName);
        if (rigObject != null)
        {
            playerRig = rigObject.transform;
            isPlayerFound = true;
        }
    }

    void Update()
    {
        if (!isPlayerFound)
        {
            FindPlayerRig();
            return;
        }

        // Tắt render nếu mũi tên không được kích hoạt hoặc không có đích đến
        if (!isArrowActive || currentTarget == null)
        {
            meshRenderer.enabled = false;
            return;
        }

        meshRenderer.enabled = true;

        // TÍNH TOÁN HIỆU ỨNG MŨI TÊN BAY LƯỚT (Giống như cũ)
        float distanceToTarget = Vector3.Distance(playerRig.position, currentTarget.position);

        // Tắt mũi tên nếu đứng quá sát đích (để không bị bay xuyên người)
        if (distanceToTarget < 1.5f)
        {
            meshRenderer.enabled = false;
            return;
        }

        Vector3 direction = (currentTarget.position - playerRig.position).normalized;
        direction.y = 0;

        float maxFlyDist = Mathf.Min(distanceToTarget, loopDistance);
        float currentFlyDist = Mathf.Repeat(Time.time * moveSpeed, maxFlyDist);

        Vector3 startPos = playerRig.position + direction * 1.0f;
        startPos.y = playerRig.position.y + groundOffset;

        Vector3 newPos = startPos + direction * currentFlyDist;
        newPos.y += Mathf.Sin(Time.time * 6f) * bobHeight;

        transform.position = newPos;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            transform.rotation *= Quaternion.Euler(90, 0, 0);
        }
    }

    // ==========================================
    // CÁC HÀM ĐỂ GỌI TỪ BÊN NGOÀI (SỰ KIỆN)
    // ==========================================

    // Lệnh: Chỉ mũi tên đến một điểm nào đó và bật nó lên
    public void PointTo(Transform newTarget)
    {
        currentTarget = newTarget;
        isArrowActive = true;
        Debug.Log("[Arrow] Bắt đầu dẫn đường đến: " + newTarget.name);
    }

    // Lệnh: Ẩn mũi tên đi (khi người chơi đang trải nghiệm)
    public void HideArrow()
    {
        isArrowActive = false;
        meshRenderer.enabled = false;
        Debug.Log("[Arrow] Đã ẩn mũi tên.");
    }
}
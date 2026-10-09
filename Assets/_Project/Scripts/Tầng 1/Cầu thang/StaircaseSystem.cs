using UnityEngine;

public class StaircaseSystem : MonoBehaviour
{
    public enum StairBranch
    {
        None,
        Left,
        Right,
        Upper
    }

    [Header("=== CÁC NHÁNH CẦU THANG Y ===")]
    [SerializeField] private Transform[] leftWaypoints;
    [SerializeField] private Transform[] rightWaypoints;
    [SerializeField] private Transform[] upperWaypoints;

    [Header("=== CHIẾU NGHỈ ===")]
    [SerializeField] private Transform landing;

    // Nhánh hiện tại của Player
    private StairBranch currentBranch = StairBranch.None;

    // Player có đang ở trong vùng trigger hay không
    private bool playerInsideTrigger = false;

    public StairBranch CurrentBranch => currentBranch;
    public bool PlayerInsideTrigger => playerInsideTrigger;
    public Transform Landing => landing;


    // =========================================================
    // PLAYER ĐI VÀO NHÁNH
    // =========================================================

    public void EnterBranch(StairBranch branch)
    {
        currentBranch = branch;
        playerInsideTrigger = true;

        Debug.Log(
            $"[StaircaseSystem] Player vào nhánh: {currentBranch}"
        );
    }


    // =========================================================
    // PLAYER RỜI TRIGGER
    // =========================================================

    public void ExitBranch(StairBranch branch)
    {
        // Không reset nếu Player hiện đã ở nhánh khác
        if (currentBranch != branch)
            return;

        currentBranch = StairBranch.None;
        playerInsideTrigger = false;

        Debug.Log(
            $"[StaircaseSystem] Player rời nhánh: {branch}"
        );
    }


    // =========================================================
    // LẤY WAYPOINT THEO NHÁNH
    // =========================================================

    public Transform[] GetWaypoints(StairBranch branch)
    {
        switch (branch)
        {
            case StairBranch.Left:
                return leftWaypoints;

            case StairBranch.Right:
                return rightWaypoints;

            case StairBranch.Upper:
                return upperWaypoints;

            default:
                return null;
        }
    }


    // =========================================================
    // LẤY WAYPOINT CỦA NHÁNH HIỆN TẠI
    // =========================================================

    public Transform[] GetCurrentWaypoints()
    {
        return GetWaypoints(currentBranch);
    }


    // =========================================================
    // KIỂM TRA WAYPOINT
    // =========================================================

    public bool HasWaypoints(StairBranch branch)
    {
        Transform[] waypoints = GetWaypoints(branch);

        return waypoints != null &&
               waypoints.Length >= 2;
    }
}
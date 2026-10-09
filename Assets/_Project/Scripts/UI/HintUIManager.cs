using UnityEngine;
using TMPro;

public class HintUIManager : MonoBehaviour
{
    public static HintUIManager Instance;

    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI hintText;

    private void Awake()
    {
        Instance = this;
        HideHint();
    }

    private void LateUpdate()
    {
        if (hintPanel != null && hintPanel.activeSelf && Camera.main != null)
        {
            // Tính hướng từ panel đến camera
            Vector3 dirToCamera = Camera.main.transform.position - hintPanel.transform.position;

            // Xoay panel nhìn về phía camera ĐÚNG chiều
            hintPanel.transform.rotation = Quaternion.LookRotation(dirToCamera);
        }
    }

    public void ShowHint(string key, string message)
    {
        Debug.Log($"[HintUI] ShowHint gọi! key={key} | msg={message} | panel={hintPanel?.name} | active={hintPanel?.activeSelf}");

        gameObject.SetActive(true);
        if (hintPanel != null) hintPanel.SetActive(true);
        if (keyText != null) keyText.text = key;
        if (hintText != null) hintText.text = message;
    }

    public void HideHint()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
        // KHÔNG tắt gameObject/Canvas — chỉ tắt panel bên trong 

    }
}
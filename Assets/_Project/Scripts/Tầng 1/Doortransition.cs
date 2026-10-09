using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Doortransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private string currentSceneName = "01_Tang1";
    [SerializeField] private string nextSceneName = "02_Tang2";
    // Tăng lên 2.5s = 1.5s animation + 0.5s chờ + 0.5s buffer
    [SerializeField] private float waitTimeBeforeTransition = 2.5f;

    private XRBaseInteractable interactable;
    private bool isTriggered = false;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    // XÓA Update() và OnMouseDown() đi
    // Chỉ giữ TriggerTransition và WaitAndSwitchScene

    public void TriggerTransition()
    {
        if (isTriggered) return;
        isTriggered = true;
        Debug.Log("[Door] Bắt đầu đếm ngược chuyển scene...");
        StartCoroutine(WaitAndSwitchScene());
    }

    private IEnumerator WaitAndSwitchScene()
    {
        yield return new WaitForSeconds(waitTimeBeforeTransition);

        if (SceneLoaderManager.Instance != null)
            SceneLoaderManager.Instance.LoadSceneAdditive(currentSceneName, nextSceneName);
        else
            Debug.LogError("Không tìm thấy SceneLoaderManager!");
    }
}
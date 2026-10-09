using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tự nạp Scene đầu tiên của game (tầng 1) mà ko làm ngắt kết nối 
public class BootstrapperManager : MonoBehaviour
{
    [Header("First Scene Config")]
    [SerializeField] private string firstSceneName = "01_Tang1";

    private void Awake()
    {
        // Giữ object này không bị xóa khi chuyển Scene
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.2f);

        // Nạp ngầm Scene Tầng 1 theo dạng Additive
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(firstSceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Đặt Scene Tầng 1 làm Active Scene để render ánh sáng chuẩn
        Scene targetScene = SceneManager.GetSceneByName(firstSceneName);
        if (targetScene.IsValid())
        {
            SceneManager.SetActiveScene(targetScene);
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// điều khiển vc chuyển giữa tầng 1 và t ầng 2 , và các scene khác trong game
public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance { get; private set; }

    [SerializeField] private ScreenFadeController fadeController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadSceneAdditive(string currentSceneToUnload, string newSceneToLoad)
    {
        StartCoroutine(RoutineSwitchScene(currentSceneToUnload, newSceneToLoad));
    }

    private IEnumerator RoutineSwitchScene(string currentScene, string newScene)
    {
        // 1. Mờ màn hình tối đen
        if (fadeController != null)
        {
            yield return StartCoroutine(fadeController.FadeOut());
        }

        // 2. Unload Scene cũ (nếu có)
        if (!string.IsNullOrEmpty(currentScene) && SceneManager.GetSceneByName(currentScene).isLoaded)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);
            while (!unloadOp.isDone) yield return null;
        }

        // 3. Load Additive Scene mới
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null;

        // 4. Set Active Scene mới
        Scene newlyLoadedScene = SceneManager.GetSceneByName(newScene);
        if (newlyLoadedScene.IsValid())
        {
            SceneManager.SetActiveScene(newlyLoadedScene);
        }

        // 5. Hiện màn hình trở lại
        if (fadeController != null)
        {
            yield return StartCoroutine(fadeController.FadeIn());
        }
    }
}
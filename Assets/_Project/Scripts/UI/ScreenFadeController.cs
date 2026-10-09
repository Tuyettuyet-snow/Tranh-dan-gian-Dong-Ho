using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeController : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    private void Awake()
    {
        if (fadeImage != null)
        {
            // Mặc định ban đầu màn hình trong suốt
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }
    }

    public IEnumerator FadeOut()
    {
        // Mờ dần thành màu đen (100% Alpha)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(timer / fadeDuration));
            yield return null;
        }
        SetAlpha(1f);
    }

    public IEnumerator FadeIn()
    {
        // Hiện dần từ màu đen ra trong suốt (0% Alpha)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(1f - (timer / fadeDuration)));
            yield return null;
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}
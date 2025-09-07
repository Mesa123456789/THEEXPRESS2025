using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f; 
    void Awake()
    {
        if (Instance == null) Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }
    void Start()
    {
        StartCoroutine(FadeManager.Instance.FadeOut(2f));
    }

    public IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return StartCoroutine(FadeManager.Instance.FadeIn(2f));
        yield return new WaitForSecondsRealtime(1f);
        SceneManager.LoadScene(sceneName);
    }

    public IEnumerator FadeIn(float duration = 1.5f)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f; // มืดสนิท
    }

    public IEnumerator FadeOut(float duration = 1.5f)
    {
        float t = duration;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f; // โปร่งใสสนิท
    }


}

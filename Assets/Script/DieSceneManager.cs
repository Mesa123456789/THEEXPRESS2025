using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class DieSceneManager : MonoBehaviour
{
    [Header("Where to go")]
    public string gameplayScene = "Gameplay";

    [Header("GameManager")]
    public GameManager GameManager;

    [Header("Options")]
    public bool goToNextDayAt15 = false;

    IEnumerator Start()
    {

        if (FadeManager.Instance != null)
            yield return StartCoroutine(FadeManager.Instance.FadeOut(1.5f));
        Time.timeScale = 1f;
    }

    void Awake()
    {
        // โชว์เมาส์ในซีนตาย
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f; 

    }


    public void BacktoGameplay()
    {
        GameManager = GetComponent<GameManager>();
        if (goToNextDayAt15 && GameManager != null)
            GameManager.SleepNow();

        // ล็อกเมาส์กลับสำหรับ FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(gameplayScene);
    }
}

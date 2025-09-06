using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DieSceneManager : MonoBehaviour
{
    [Header("Where to go")]
    public string gameplayScene = "Gameplay";

    [Header("GameManager")]
    public GameManager GameManager;

    [Header("Options")]
    public bool goToNextDayAt15 = false;

    void Awake()
    {
        // โชว์เมาส์ในซีนตาย
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f; 

    }

    private void Start()
    {
        GameManager = GetComponent<GameManager>();
    }

    public void BacktoGameplay()
    {
        if (goToNextDayAt15 && GameManager != null)
            GameManager.SleepNow();

        // ล็อกเมาส์กลับสำหรับ FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(gameplayScene);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DieSceneManager : MonoBehaviour
{
    [Header("Where to go")]
    public string gameplayScene = "Gameplay";

    [Header("Options")]
    public bool goToNextDayAt15 = false; // ถ้าต้องการให้ไปวันใหม่ 15:00 ก่อนกลับ

    void Awake()
    {
        // โชว์เมาส์ในซีนตาย
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f; // กันเผื่อมีการหยุดเวลา

        // เผื่อซีนนี้ลืมใส่ EventSystem (จำเป็นสำหรับปุ่ม UI)

    }

    public void BacktoGameplay()
    {
        // (ออปชัน) ให้ GM ตั้งเวลาไปวันใหม่ 15:00 ก่อนกลับ
        if (goToNextDayAt15 && GameManager.Instance != null)
            GameManager.Instance.SleepNow();

        // ล็อกเมาส์กลับสำหรับ FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(gameplayScene);
    }
}

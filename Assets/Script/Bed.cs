using UnityEngine;
using UnityEngine.EventSystems;
using StarterAssets; // FirstPersonController

public class Bed : MonoBehaviour
{
    [Header("UI")]
    public Canvas sleepUI;                 // พาเนลที่มีปุ่ม Sleep / No

    [Header("Player")]
    public FirstPersonController playerController;

    [Header("Raycast")]
    public Camera rayCamera;               // ว่างไว้จะใช้ Camera.main
    public string bedTag = "Bed";
    public float rayDistance = 5f;

    [Header("Rule")]
    [Tooltip("เปิดได้เฉพาะช่วงอันตราย 03:00–05:00")]
    public bool requireDangerTime = true;

    void Start()
    {
        if (!rayCamera) rayCamera = Camera.main;
        if (sleepUI) sleepUI.enabled = false;
        LockGameplay(); // ซ่อนเมาส์เริ่มเกม
    }

    void Update()
    {
        // ถ้า UI เปิดอยู่ ไม่ต้องฟังคลิกเปิดซ้ำ
        if (sleepUI && sleepUI.enabled) return;

        // กันคลิกทะลุ UI อื่น ๆ
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        // คลิกซ้ายเท่านั้น
        if (!Input.GetMouseButtonDown(0)) return;

        if (requireDangerTime && GameManager.Instance && !GameManager.Instance.IsDangerTime)
            return;

        if (!rayCamera) return;

        // ยิง Ray จากตำแหน่งเมาส์ไปหาเตียง
        Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, rayDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (HasTagInHierarchy(hit.collider.transform, bedTag))
            {
                OpenSleepUI();
            }
        }
    }

    bool HasTagInHierarchy(Transform t, string tag)
    {
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }

    // ===== UI control =====
    void OpenSleepUI()
    {
        if (!sleepUI) return;
        sleepUI.enabled = true;
        UnlockForUI();   // ปลดเมาส์ + ล็อกการเดิน (เหมือน Computer.cs)
    }

    void CloseSleepUI()
    {
        if (!sleepUI) return;
        sleepUI.enabled = false;
        LockGameplay();  // ซ่อนเมาส์ + ปลดล็อกการเดินกลับสู่เกม
    }

    // ===== Buttons =====
    public void OnClickSleep()
    {
        GameManager.Instance?.SleepNow(); // ข้ามไปวันถัดไป 15:00
        CloseSleepUI();
    }

    public void OnClickNo()
    {
        CloseSleepUI();  // แค่ปิดแล้วเล่นต่อได้เลย
    }

    // ===== Cursor & movement helpers =====
    void UnlockForUI()
    {
        if (playerController) playerController.isMovementLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LockGameplay()
    {
        if (playerController) playerController.isMovementLocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

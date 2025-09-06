using UnityEngine;
using UnityEngine.EventSystems;
using StarterAssets; // FirstPersonController

public class Bed : MonoBehaviour
{
    [Header("UI")]
    public Canvas sleepUI;

    [Header("Player")]
    public FirstPersonController playerController;

    [Header("Game")]
    public GameManager gameManager;   // <-- เปลี่ยนชื่อฟิลด์และตั้งให้หาอัตโนมัติ

    [Header("Raycast")]
    public Camera rayCamera;
    public string bedTag = "Bed";
    public float rayDistance = 5f;

    [Header("Rule")]
    [Tooltip("เปิดได้เฉพาะช่วงอันตราย 03:00–05:00")]
    public bool requireDangerTime = true;

    void Start()
    {
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>(); // หา GM ของซีนปัจจุบัน
        if (!rayCamera) rayCamera = Camera.main;
        if (sleepUI) sleepUI.enabled = false;
        LockGameplay();
    }

    void Update()
    {
        if (sleepUI && sleepUI.enabled) return;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // ต้องมี GM ถ้าบังคับช่วงอันตราย
        if (requireDangerTime)
        {
            if (!gameManager) return;
            if (!gameManager.IsDangerTime) return;
        }

        if (!rayCamera) return;

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

    void OpenSleepUI()
    {
        if (!sleepUI) return;
        sleepUI.enabled = true;
        UnlockForUI();
    }

    void CloseSleepUI()
    {
        if (!sleepUI) return;
        sleepUI.enabled = false;
        LockGameplay();
    }

    // ===== Buttons =====
    public void OnClickSleep()
    {
        // เรียก GM ของซีนปัจจุบัน (เผื่อมีการเปลี่ยนซีนหรือถูกลบทิ้ง)
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
        gameManager?.SleepNow();
        CloseSleepUI();
    }

    public void OnClickNo()
    {
        CloseSleepUI();
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

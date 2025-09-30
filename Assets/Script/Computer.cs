using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class Computer : MonoBehaviour
{
    [Header("UI & Player")]
    public Canvas informUI;
    public FirstPersonController playerController;

    [Header("Yaw Lerp")]
    [Tooltip("วัตถุแกนหมุนแกน Y (ปล่อยว่างจะใช้ Transform ของผู้เล่น)")]
    public Transform yawRoot;
    [Tooltip("เป้าหมาย Y (องศา) เมื่อเปิดคอม")]
    public float targetYawOnOpen = -90f;
    [Tooltip("ระยะเวลาหมุน (วินาที)")]
    public float yawLerpDuration = 0.35f;

    private Coroutine yawRoutine;

    private void Start()
    {
        playerController = FindFirstObjectByType<FirstPersonController>();
        if (!yawRoot && playerController) yawRoot = playerController.transform; // fallback
        if (informUI) informUI.enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, 3f) && hit.collider.CompareTag("Computer"))
            {
                OnOpenComputer();
            }
        }

    }

    public void OnOpenComputer()
    {
        if (informUI) informUI.enabled = true;

        // ล็อกการเดินและการมองรอบ
        playerController.isMovementLocked = true;
        // ถ้ามีฟิลด์ LockCameraPosition ใน StarterAssets ให้ล็อกด้วย (ไม่ error หากไม่มี)
        try { playerController.isMovementLocked = true; } catch { }

        if (CursorCoordinator.I) CursorCoordinator.I.SetComputerOpen(true);

        // เริ่ม lerp มุม Y ไปที่ -90
        if (yawRoutine != null) StopCoroutine(yawRoutine);
        yawRoutine = StartCoroutine(LerpYawTo(targetYawOnOpen, yawLerpDuration));
    }

    public void OnCloseComputer()
    {
        if (informUI) informUI.enabled = false;

        playerController.isMovementLocked = false;
        try { playerController.isMovementLocked = false; } catch { }

        if (CursorCoordinator.I) CursorCoordinator.I.SetComputerOpen(false);

        // ไม่จำเป็นต้องหมุนกลับ แต่ถ้าต้องการ ให้เรียก:
        // if (yawRoutine != null) StopCoroutine(yawRoutine);
        // yawRoutine = StartCoroutine(LerpYawTo(previousYaw, yawLerpDuration));
    }

    private IEnumerator LerpYawTo(float targetYaw, float duration)
    {
        if (!yawRoot) yield break;

        float t = 0f;
        // อ่านมุมเริ่มต้นแบบ 0–360
        float startYaw = yawRoot.eulerAngles.y;

        // ปิดอินพุตกล้องชั่วคราว (กันสู้กับการหมุน)
        bool oldLookLock = false;
        try { oldLookLock = playerController.isMovementLocked; playerController.isMovementLocked = true; } catch { }

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);

            // ใช้ LerpAngle + SmoothStep ให้ลื่น
            float y = Mathf.LerpAngle(startYaw, targetYaw, Mathf.SmoothStep(0f, 1f, t));
            Vector3 e = yawRoot.eulerAngles;
            e.y = y;
            yawRoot.eulerAngles = e;

            yield return null;
        }

        // คืนค่า lock กล้องตามเดิม (ยังคงล็อกการเดินไว้จนกว่าจะปิดคอม)
        try { playerController.isMovementLocked = oldLookLock; } catch { }
        yawRoutine = null;
    }
}

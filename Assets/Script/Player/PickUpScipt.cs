using System.Collections.Generic;
using UnityEngine;

public class PickUpScipt : MonoBehaviour
{
    [Header("Refs")]
    public Transform holdPos;                  // จุดอ้างอิงการถือ (วางไว้หน้ากล้อง)
    public Collider playerCollider;            // คอลลิเดอร์ผู้เล่นสำหรับ Ignore ตอนถือ

    [Header("Filters")]
    public string pickableTag = "pickable";
    public LayerMask pickableLayer = ~0;

    [Header("Input")]
    [Tooltip("คลิกซ้าย = หยิบ/วาง")]
    public bool useLeftClickToggle = true;

    [Header("Hold Physics")]
    public float pickUpRayRange = 5f;          // ระยะ Raycast หาไอเท็ม
    public bool antiClipWhileHolding = true;   // กันทะลุผนังตอนถือ (SphereCast)
    public float holdSmooth = 20f;
    public float skin = 0.02f;

    [Header("Rotation (Yaw only)")]
    public float wheelYawSpeed = 160f;         // หมุนด้วยล้อเมาส์ แกน Y
    [Tooltip("ปิดการลากเมาส์เพื่อหมุน (เหลือแค่สกอร์ล)")]
    public bool disableDragRotate = true;

    [Header("Keep Visual Size")]
    [Tooltip("คงระยะจากกล้องตอนหยิบ เพื่อให้ขนาดที่เห็นไม่เปลี่ยน")]
    public bool preserveApparentSize = true;
    public float minHoldDistance = 0.6f;
    public float maxHoldDistance = 3.0f;

    [Header("Two-Camera Layering")]
    public string holdLayerName = "holdItem";
    public int defaultLayer = 0;

    [Header("Optional Camera Sync (Base/Overlay)")]
    public Camera baseCam;     // กล้องหลัก (ถ้าไม่กำหนด จะใช้ Camera.main)
    public Camera overlayCam;  // กล้อง Overlay ที่เรนเดอร์เลเยอร์ holdItem (ปล่อยว่างได้)

    // --- runtime ---
    private GameObject heldObj;
    private Rigidbody heldRb;
    private readonly List<Collider> heldCols = new List<Collider>();
    private int holdLayer;
    private bool canDrop = true;
    private Quaternion targetRotation;

    // เก็บสถานะเดิมของ Rigidbody
    private bool prevKinematic;
    private bool prevUseGravity;
    private RigidbodyInterpolation prevInterp;
    private CollisionDetectionMode prevCdm;
    private bool prevDetectCollisions;

    // ระยะที่ใช้คงขนาดภาพ
    private float heldDistanceFromCam = 1.0f;

    void Awake()
    {
        holdLayer = LayerMask.NameToLayer(holdLayerName);
        if (holdLayer < 0)
            Debug.LogWarning($"[PickupController] Layer '{holdLayerName}' ยังไม่ได้สร้างใน Project Settings > Tags and Layers");
    }

    void Update()
    {
        // รับอินพุตเท่านั้น (ย้ายการขยับไป LateUpdate ให้ซิงก์กับกล้อง)
        if (useLeftClickToggle && Input.GetMouseButtonDown(0))
        {
            if (heldObj == null) TryPickup();
            else if (canDrop) DropObject();
        }
    }

    void LateUpdate()
    {
        // เคลื่อนย้าย/หมุนหลังกล้องอัปเดต -> ลดอาการสั่น
        if (heldObj != null)
        {
            if (antiClipWhileHolding) MoveHeldWithAntiClip();
            else MoveHeldSimple();

            HandleWheelYaw();
        }

        // ออปชัน: ซิงก์กล้อง Overlay กับกล้องหลัก (ถ้ากำหนดไว้)
        if (overlayCam != null)
        {
            Camera cam = baseCam != null ? baseCam : Camera.main;
            if (cam != null)
            {
                overlayCam.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
                overlayCam.fieldOfView = cam.fieldOfView;
                overlayCam.nearClipPlane = cam.nearClipPlane;
                overlayCam.farClipPlane = cam.farClipPlane;
            }
        }
    }

    // ----------------- Pick / Drop -----------------
    void TryPickup()
    {
        Camera cam = baseCam != null ? baseCam : Camera.main;
        if (!cam) return;

        // Raycast เลือกวัตถุ
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickUpRayRange, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform tr = hit.transform;

            bool tagOK = !string.IsNullOrEmpty(pickableTag) && tr.CompareTag(pickableTag);
            bool layerOK = ((1 << tr.gameObject.layer) & pickableLayer) != 0;
            if (!(tagOK || layerOK)) return;

            Rigidbody rb = hit.rigidbody != null ? hit.rigidbody : hit.collider.GetComponent<Rigidbody>();
            if (!rb) return;

            heldObj = rb.gameObject;
            heldRb = rb;

            // เก็บสถานะเดิม
            prevKinematic = heldRb.isKinematic;
            prevUseGravity = heldRb.useGravity;
            prevInterp = heldRb.interpolation;
            prevCdm = heldRb.collisionDetectionMode;
            prevDetectCollisions = heldRb.detectCollisions;

            // ตั้งค่าสำหรับ "ถือ"
            heldRb.isKinematic = true;
            heldRb.useGravity = false;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
            heldRb.detectCollisions = false;                     // ปิด contact ตอนถือ (กันโดนฟิสิกส์ผลัก)
            heldRb.interpolation = RigidbodyInterpolation.None; // ปิด interp บนคิเนมาติค ลด jitter
            heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // ย้ายเลเยอร์ไป holdItem ให้กล้อง Overlay เรนเดอร์
            SetLayerRecursively(heldObj, holdLayer);

            // Ignore ชนกับผู้เล่น
            heldCols.Clear();
            heldObj.GetComponentsInChildren(false, heldCols);
            if (playerCollider)
                foreach (var c in heldCols) Physics.IgnoreCollision(c, playerCollider, true);

            // ตั้งมุมเริ่มต้น
            targetRotation = heldObj.transform.rotation;

            // คงขนาดภาพ: เก็บระยะจากกล้อง -> จุดที่โดนยิง
            if (preserveApparentSize)
            {
                float d = Vector3.Distance(cam.transform.position, hit.point);
                heldDistanceFromCam = Mathf.Clamp(d, minHoldDistance, maxHoldDistance);
            }
        }
    }

    void DropObject()
    {
        if (heldObj == null) return;

        // เลิก Ignore กับผู้เล่น
        if (playerCollider)
            foreach (var c in heldCols) Physics.IgnoreCollision(c, playerCollider, false);

        // คืนเลเยอร์เดิม
        SetLayerRecursively(heldObj, defaultLayer);

        // คืนค่าฟิสิกส์เดิม
        heldRb.isKinematic = prevKinematic;
        heldRb.useGravity = prevUseGravity;
        heldRb.interpolation = prevInterp;
        heldRb.collisionDetectionMode = prevCdm;
        heldRb.detectCollisions = prevDetectCollisions;

        heldObj = null;
        heldRb = null;
        heldCols.Clear();
    }

    // ----------------- Movement while holding -----------------
    Vector3 GetHoldTargetPosition()
    {
        Camera cam = baseCam != null ? baseCam : Camera.main;
        if (!cam) return holdPos ? holdPos.position : transform.position + transform.forward * 1.0f;

        if (preserveApparentSize)
        {
            // วางตามแนวกล้องที่ "ระยะเดิม" ตอนหยิบ
            return cam.transform.position + cam.transform.forward * heldDistanceFromCam;
        }
        // โหมดปกติ: ใช้ตำแหน่ง holdPos ถ้ามี
        return holdPos ? holdPos.position : cam.transform.position + cam.transform.forward * 1.0f;
    }

    void MoveHeldSimple()
    {
        Vector3 target = GetHoldTargetPosition();
        heldObj.transform.position = target;   // คิเนมาติค + ปิด detectCollisions แล้ว ปลอดภัย
        if (heldRb) heldRb.MoveRotation(targetRotation);
    }

    void MoveHeldWithAntiClip()
    {
        if (heldRb == null) return;

        Vector3 current = heldRb.position;
        Vector3 goal = GetHoldTargetPosition();
        Vector3 target = Vector3.Lerp(current, goal, Time.deltaTime * holdSmooth);

        Bounds b = GetCombinedBounds(heldCols);
        float radius = b.extents.magnitude;

        Vector3 dir = target - current;
        float dist = dir.magnitude;
        if (dist > 1e-4f)
        {
            dir /= dist;

            // ไม่ชนเลเยอร์ที่ถืออยู่เอง
            int mask = ~0;
            if (holdLayer >= 0) mask &= ~(1 << holdLayer);

            if (Physics.SphereCast(current, radius, dir, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                if (!IsSelf(hit.collider))
                {
                    Vector3 stop = hit.point - dir * (radius + skin);
                    heldRb.MovePosition(stop);
                    heldRb.MoveRotation(targetRotation);
                    return;
                }
            }
        }

        heldRb.MovePosition(target);
        heldRb.MoveRotation(targetRotation);
    }

    // ----------------- Rotation (wheel only) -----------------
    void HandleWheelYaw()
    {
        // (ยังไม่เปิดโหมดลากเมาส์ตามเดิม)
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0005f)
        {
            targetRotation = Quaternion.AngleAxis(wheel * wheelYawSpeed, Vector3.up) * targetRotation;
        }
    }

    // ----------------- Helpers -----------------
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    Bounds GetCombinedBounds(List<Collider> cols)
    {
        if (heldObj == null || cols.Count == 0)
            return new Bounds(GetHoldTargetPosition(), Vector3.one * 0.2f);

        Bounds b = cols[0].bounds;
        for (int i = 1; i < cols.Count; i++) b.Encapsulate(cols[i].bounds);
        return b;
    }

    bool IsSelf(Collider c)
    {
        foreach (var col in heldCols) if (c == col) return true;
        return false;
    }
}

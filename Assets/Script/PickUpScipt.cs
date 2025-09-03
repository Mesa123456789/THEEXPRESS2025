using System.Collections.Generic;
using UnityEngine;

public class PickUpScipt : MonoBehaviour
{
    [Header("Refs")]
    public Transform holdPos;                  // ใช้เป็นตำแหน่งอ้างอิง/แกนการยก (วางไว้หน้ากล้อง)
    public Collider playerCollider;            // คอลลิเดอร์ของผู้เล่นไว้ Ignore ตอนถือ

    [Header("Filters")]
    public string pickableTag = "pickable";
    public LayerMask pickableLayer = ~0;

    [Header("Input")]
    [Tooltip("คลิกซ้าย = หยิบ/วาง")]
    public bool useLeftClickToggle = true;     // true = LMB toggle (ตามที่ขอ)

    [Header("Hold Physics")]
    public float pickUpRayRange = 5f;          // ระยะ Raycast หาไอเท็ม
    public bool antiClipWhileHolding = true;  // กันทะลุผนังตอนถือ
    public float holdSmooth = 20f;
    public float skin = 0.02f;

    [Header("Rotation (Yaw only)")]
    public float wheelYawSpeed = 160f;         // ความเร็วหมุนด้วยสกอร์ลเมาส์ (ซ้าย/ขวา)
    [Tooltip("ปิดการลากเมาส์เพื่อหมุน (เหลือแค่สกอร์ล)")]
    public bool disableDragRotate = true;      // ปิดโหมด R+ลาก เมาส์

    [Header("Keep Visual Size")]
    [Tooltip("ทำให้ของไม่ดูเล็ก/ใหญ่ขึ้นเมื่อยก: คงระยะจากกล้องตอนหยิบ")]
    public bool preserveApparentSize = true;
    public float minHoldDistance = 0.6f;       // กันไม่ให้จ่อหน้ากล้องเกินไป
    public float maxHoldDistance = 3.0f;       // กันไม่ให้ไกลเกินไป

    [Header("Two-Camera Layering")]
    public string holdLayerName = "holdItem";
    public int defaultLayer = 0;

    // runtime
    private GameObject heldObj;
    private Rigidbody heldRb;
    private List<Collider> heldCols = new List<Collider>();
    private int holdLayer;
    private bool canDrop = true;
    private Quaternion targetRotation;

    // เก็บสถานะของ Rigidbody เดิม
    private bool prevKinematic;
    private bool prevUseGravity;
    private RigidbodyInterpolation prevInterp;
    private CollisionDetectionMode prevCdm;

    // เก็บระยะที่ใช้คงขนาดภาพ
    private float heldDistanceFromCam = 1.0f;

    void Awake()
    {
        holdLayer = LayerMask.NameToLayer(holdLayerName);
        if (holdLayer < 0)
            Debug.LogWarning($"[PickupController] Layer '{holdLayerName}' ยังไม่ได้สร้างใน Project Settings > Tags and Layers");
    }

    void Update()
    {
        // --- Toggle pickup/drop ด้วยคลิกซ้าย ---
        if (useLeftClickToggle && Input.GetMouseButtonDown(0))
        {
            if (heldObj == null) TryPickup();
            else if (canDrop) DropObject();
        }

        if (heldObj != null)
        {
            // เคลื่อนที่ไอเท็มไปยังตำแหน่งถือ
            if (antiClipWhileHolding) MoveHeldWithAntiClip();
            else MoveHeldSimple();

            // หมุนด้วยล้อเมาส์ (yaw เท่านั้น)
            HandleWheelYaw();

            // ปิดการโยน/ขว้าง -> ไม่มีโค้ดโยน
        }
    }

    // ----------------- Pick / Drop -----------------
    void TryPickup()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        // 1) Raycast ทะลุทุกเลเยอร์ก่อน แล้วค่อยเช็กเงื่อนไขเอง
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickUpRayRange, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform tr = hit.transform;

            // 2) เช็ก "อย่างใดอย่างหนึ่ง"
            bool tagOK = !string.IsNullOrEmpty(pickableTag) && tr.CompareTag(pickableTag);
            bool layerOK = ((1 << tr.gameObject.layer) & pickableLayer) != 0;

            if (!(tagOK || layerOK)) return;   // ต้องมีแท็กตรง หรือ อยู่เลเยอร์ที่อนุญาต อย่างใดอย่างหนึ่ง

            // ดึง rigidbody จาก RaycastHit
            Rigidbody rb = hit.rigidbody != null ? hit.rigidbody : hit.collider.GetComponent<Rigidbody>();
            if (!rb) return;

            heldObj = rb.gameObject;
            heldRb = rb;

            // --- เก็บสถานะเดิม ---
            prevKinematic = heldRb.isKinematic;
            prevUseGravity = heldRb.useGravity;
            prevInterp = heldRb.interpolation;
            prevCdm = heldRb.collisionDetectionMode;

            // --- ตั้งค่าระหว่างถือ ---
            heldRb.isKinematic = true;
            heldRb.useGravity = false;
            heldRb.detectCollisions = true;
            heldRb.interpolation = RigidbodyInterpolation.Interpolate;
            heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // เปลี่ยนเลเยอร์ทั้งก้อน -> holdItem (กล้อง Overlay เรนเดอร์)
            SetLayerRecursively(heldObj, holdLayer);

            // Ignore ชนกับผู้เล่น
            heldCols.Clear();
            heldObj.GetComponentsInChildren(false, heldCols);
            if (playerCollider)
                foreach (var c in heldCols) Physics.IgnoreCollision(c, playerCollider, true);

            // มุมเริ่มต้น
            targetRotation = heldObj.transform.rotation;

            // คงขนาดภาพ
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

        if (playerCollider)
            foreach (var c in heldCols) Physics.IgnoreCollision(c, playerCollider, false);

        SetLayerRecursively(heldObj, defaultLayer);

        heldRb.isKinematic = prevKinematic;
        heldRb.useGravity = prevUseGravity;
        heldRb.interpolation = prevInterp;
        heldRb.collisionDetectionMode = prevCdm;

        heldObj = null;
        heldRb = null;
        heldCols.Clear();
    }

    // ----------------- Movement while holding -----------------
    Vector3 GetHoldTargetPosition()
    {
        var cam = Camera.main;
        if (!cam) return holdPos ? holdPos.position : transform.position + transform.forward * 1.0f;

        if (preserveApparentSize)
        {
            // วางไว้ตามแนวกล้องที่ "ระยะเดิม" ตอนหยิบ เพื่อให้ขนาดที่เห็นไม่เปลี่ยน
            return cam.transform.position + cam.transform.forward * heldDistanceFromCam;
        }
        // โหมดปกติ: ใช้ตำแหน่ง holdPos
        return holdPos ? holdPos.position : cam.transform.position + cam.transform.forward * 1.0f;
    }

    void MoveHeldSimple()
    {
        Vector3 target = GetHoldTargetPosition();
        heldObj.transform.position = target;
        heldRb.MoveRotation(targetRotation);
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
            if (Physics.SphereCast(current, radius, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
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
        // ปิดโหมดลากเมาส์: เหลือเฉพาะสกอร์ลเพื่อหมุนแกน Y
        if (!disableDragRotate)
        {
            // ถ้าภายหลังอยากเปิดการลากหมุน ค่อยเพิ่มได้
        }

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

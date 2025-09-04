using System.Collections.Generic;
using UnityEngine;

public class SimplePickupOverlay : MonoBehaviour
{
    [Header("Raycast & Hold")]
    public Camera rayCam;                 // กล้องที่ใช้ Raycast (ว่างไว้จะใช้ Camera.main)
    public Transform holdAnchor;          // จุดยกของหน้ากล้อง (ตั้ง empty ไว้หน้ากล้อง)
    public float pickRange = 5f;          // ระยะหยิบ
    [Tooltip("คัดเลือกด้วยแท็กเท่านั้น ไม่ได้ใช้เลเยอร์กรอง")]
    public LayerMask pickMask = ~0;       // ไม่ได้ใช้กรองคัดเลือก

    [Header("Pickable Tags (หลายแท็กได้)")]
    public string[] pickableTags = new string[] { "pickable" }; // ใส่ได้หลายชื่อ เช่น {"pickable","box","tool"}

    [Header("Render Layer for Held Item")]
    public string holdLayerName = "holdItem";   // เลเยอร์ที่กล้อง Overlay เรนเดอร์
    public int defaultLayer = 0;                // สำรอง (เผื่อใช้กรณีพิเศษ)

    [Header("Rotation")]
    public float scrollYawSpeed = 160f;         // หมุนด้วยสกอร์ล (แกน Y)

    [Header("Input")]
    public KeyCode pickupKey = KeyCode.Mouse0;  // คลิกซ้าย = หยิบ/วาง

    // ---------- runtime ----------
    private GameObject heldObj;
    private Rigidbody heldRb;
    private Transform originalParent;
    private Quaternion targetLocalRot;

    // ฟิสิกส์เดิม
    private bool prevKinematic, prevUseGravity, prevDetectCollisions;
    private RigidbodyInterpolation prevInterp;
    private CollisionDetectionMode prevCdm;

    // collider เดิม
    private struct ColState { public Collider col; public bool enabled; }
    private readonly List<ColState> colStates = new List<ColState>();

    // เลเยอร์เดิมทั้งหมด (ทั้งฮีรารคี)
    private struct LayerState { public Transform t; public int layer; }
    private readonly List<LayerState> layerStates = new List<LayerState>();

    private int holdLayer;
    private readonly HashSet<string> pickableTagSet = new HashSet<string>(); // เก็บแท็กที่อนุญาต

    void Awake()
    {
        if (!rayCam) rayCam = Camera.main;

        holdLayer = LayerMask.NameToLayer(holdLayerName);
        if (holdLayer < 0)
            Debug.LogWarning($"[SimplePickupOverlay] Layer '{holdLayerName}' ยังไม่ได้สร้างใน Project Settings > Tags and Layers");

        if (!holdAnchor && rayCam)
        {
            var go = new GameObject("HoldAnchor (auto)");
            holdAnchor = go.transform;
            holdAnchor.SetParent(rayCam.transform, false);
            holdAnchor.localPosition = new Vector3(0, 0, 1.0f);
            holdAnchor.localRotation = Quaternion.identity;
        }

        // เตรียมชุดแท็กที่อนุญาต
        pickableTagSet.Clear();
        if (pickableTags != null)
            foreach (var t in pickableTags)
                if (!string.IsNullOrEmpty(t)) pickableTagSet.Add(t);
        if (pickableTagSet.Count == 0)
            Debug.LogWarning("[SimplePickupOverlay] ยังไม่ได้ระบุ pickableTags เลย จะหยิบอะไรไม่ได้");
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (heldObj == null) TryPickup();
            else Drop();
        }

        if (heldObj != null)
            HandleScrollYaw();
    }

    void LateUpdate()
    {
        if (heldObj != null)
        {
            // ล็อคที่ anchor หลังกล้องอัปเดต
            heldObj.transform.localPosition = Vector3.zero;
            heldObj.transform.localRotation = targetLocalRot;
        }
    }

    // ---------- Core ----------
    void TryPickup()
    {
        if (!rayCam || !holdAnchor) return;

        Ray ray = new Ray(rayCam.transform.position, rayCam.transform.forward);
        // ยิงทะลุทุกเลเยอร์เสมอ (คัดเลือกด้วยแท็กเท่านั้น)
        if (!Physics.Raycast(ray, out RaycastHit hit, pickRange, ~0, QueryTriggerInteraction.Ignore))
            return;

        Transform tr = hit.transform;
        if (!IsAllowedTag(tr)) return; // << เช็กหลายแท็กได้

        Rigidbody rb = hit.rigidbody ? hit.rigidbody : tr.GetComponent<Rigidbody>();
        if (!rb) return;

        heldObj = rb.gameObject;
        heldRb = rb;
        originalParent = heldObj.transform.parent;

        // เก็บฟิสิกส์เดิม
        prevKinematic = heldRb.isKinematic;
        prevUseGravity = heldRb.useGravity;
        prevDetectCollisions = heldRb.detectCollisions;
        prevInterp = heldRb.interpolation;
        prevCdm = heldRb.collisionDetectionMode;

        // ปิดฟิสิกส์/ชนตอนถือ
        heldRb.isKinematic = true;
        heldRb.useGravity = false;
        heldRb.linearVelocity = Vector3.zero;   // Unity 6
        heldRb.angularVelocity = Vector3.zero;
        heldRb.detectCollisions = false;
        heldRb.interpolation = RigidbodyInterpolation.None;
        heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // ปิด collider ทั้งหมดชั่วคราว + เก็บสถานะ
        colStates.Clear();
        var cols = heldObj.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            colStates.Add(new ColState { col = c, enabled = c.enabled });
            c.enabled = false;
        }

        // ย้ายเลเยอร์ “เฉพาะชิ้นที่ถือ” ไป holdItem
        CacheAndSetLayerRecursive(heldObj.transform, holdLayer);

        // ผูกกับ anchor
        heldObj.transform.SetParent(holdAnchor, true);
        heldObj.transform.position = holdAnchor.position;
        heldObj.transform.rotation = holdAnchor.rotation;
        targetLocalRot = heldObj.transform.localRotation;
    }

    void Drop()
    {
        if (heldObj == null) return;

        // ปลดจาก anchor
        heldObj.transform.SetParent(originalParent, true);

        // คืนเลเยอร์เดิมทั้งหมด
        RestoreLayers();

        // เปิด collider กลับตามเดิม
        foreach (var s in colStates)
            if (s.col) s.col.enabled = s.enabled;
        colStates.Clear();

        // คืนค่าฟิสิกส์เดิม
        if (heldRb)
        {
            heldRb.isKinematic = prevKinematic;
            heldRb.useGravity = prevUseGravity;
            heldRb.detectCollisions = prevDetectCollisions;
            heldRb.interpolation = prevInterp;
            heldRb.collisionDetectionMode = prevCdm;
        }

        // ล้างสถานะ
        heldObj = null;
        heldRb = null;
        originalParent = null;
        layerStates.Clear();
    }

    // ---------- Rotation (scroll = yaw) ----------
    void HandleScrollYaw()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0005f)
        {
            targetLocalRot = Quaternion.AngleAxis(wheel * scrollYawSpeed, Vector3.up) * targetLocalRot;
        }
    }

    // ---------- Helpers ----------
    bool IsAllowedTag(Transform tr)
    {
        if (pickableTagSet.Count == 0) return false;
        // ใช้ CompareTag เพื่อความปลอดภัย/เร็ว (แทน .tag)
        foreach (var t in pickableTagSet)
            if (tr.CompareTag(t)) return true;
        return false;
    }

    void CacheAndSetLayerRecursive(Transform root, int newLayer)
    {
        layerStates.Clear();
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            layerStates.Add(new LayerState { t = t, layer = t.gameObject.layer });
            t.gameObject.layer = newLayer;
        }
    }

    void RestoreLayers()
    {
        foreach (var s in layerStates)
            if (s.t) s.t.gameObject.layer = s.layer;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class ReceiptClickPlace : MonoBehaviour
{
    [Header("Snap Preview")]
    public float previewDetectRadius = 0.12f;            // รัศมีค้นหา SnapArea รอบของที่ถือ
    public bool snapAlwaysHorizontal = true;            // พรีวิวแนวนอนเสมอ
    public Vector3 placementEulerOffset = Vector3.zero;  // หมุนชดเชยถ้าโมเดลเอียง

    [Header("Pick / Hold")]
    public float maxPickDistance = 3.5f;                 // ระยะยิงเรย์เพื่อหยิบ
    public LayerMask rayMask = ~0;                       // เลเยอร์ที่อนุญาตให้หยิบ
    public Transform holdPoint;                          // จุดลอยหน้ากล้องตอนถือ
    public Collider playerCollider;                     // กันชนกับผู้เล่นตอนถือ

    // runtime
    private Camera cam;
    private ReceiptItem held;
    private readonly List<Collider> heldCols = new List<Collider>();

    // preview state
    private bool snappingPreview;
    private SnapArea previewArea;
    private SnapArea lastPreviewArea;   // ใช้ปิด grid ของพื้นที่ก่อนหน้า
    private Vector3 previewWorld;
    private Quaternion previewRot;

    public GameManager manager;

    void Awake()
    {
        cam = Camera.main;
        if (!cam) Debug.LogWarning("[ReceiptClickPlace] Main Camera not found.");
    }
    void Start()
    {
       // receipt = FindAnyObjectByType<ReceiptItem>();

    }

    void Update()
    {
        if (held == null)
        {
            // ยังไม่ได้ถือ: คลิกซ้าย = หยิบ
            if (Input.GetMouseButtonDown(0))
                TryPick();
            return;
        }

        // กำลังถือ: อัปเดตพรีวิวก่อน
        UpdateSnapPreview();

        // ถ้าไม่อยู่บน SnapArea ให้ลอยไว้ที่ holdPoint
        if (!snappingPreview && holdPoint)
        {
            held.transform.position = holdPoint.position;
            held.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }

        // คลิกซ้าย = วาง (เฉพาะตอนมีพรีวิวเท่านั้น)
        if (Input.GetMouseButtonDown(0) && snappingPreview && previewArea != null)
        {
            FinalizePlace(previewArea, previewWorld, previewRot);
            manager.currentBox.PastedLabel = true;
        }


    }

    // ---------- พรีวิวสแนประหว่างถือ ----------
    void UpdateSnapPreview()
    {
        snappingPreview = false;
        previewArea = null;
        if (!held) return;

        // 1) หา SnapArea รอบ ๆ ของที่ถือ
        var hits = Physics.OverlapSphere(
            held.transform.position,
            previewDetectRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );
        if (hits == null || hits.Length == 0)
        {
            // ออกจากพรีวิว: ปิดกริดของอันเก่า (ถ้ามี)
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            lastPreviewArea = null;
            return;
        }

        // 2) เลือก SnapArea ใกล้สุด
        float bestDist = float.MaxValue;
        SnapArea bestArea = null;
        BoxCollider bestCol = null;

        foreach (var c in hits)
        {
            var a = c.GetComponentInParent<SnapArea>();
            if (!a) continue;

            var box = a.area ? a.area : a.GetComponent<BoxCollider>();
            if (!box) continue;

            float d = (held.transform.position - box.bounds.center).sqrMagnitude;
            if (d < bestDist) { bestDist = d; bestArea = a; bestCol = box; }
        }

        if (!bestArea || !bestCol)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            lastPreviewArea = null;
            return;
        }

        // 3) ใช้เรย์จากกล้องให้เลื่อนไปตามการเล็ง
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 samplePoint;
        if (bestCol.Raycast(ray, out var hitOnArea, maxPickDistance))
        {
            samplePoint = hitOnArea.point;
        }
        else
        {
            // โปรเจกต์ลงระนาบหน้าบนของพื้นที่วาง (กันกรณีเรย์เริ่มใน Collider)
            var t = bestCol.transform;
            Plane topPlane = new Plane(t.up, t.TransformPoint(new Vector3(0, bestCol.size.y * 0.5f, 0)));
            if (!topPlane.Raycast(ray, out float enter))
            {
                if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
                lastPreviewArea = null;
                return;
            }
            samplePoint = ray.GetPoint(enter);
        }

        // 4) Clamp ขอบใน local-space
        Transform tf = bestCol.transform;
        Vector3 local = tf.InverseTransformPoint(samplePoint);
        Vector3 half = bestCol.bounds.extents;

        local.y = +half.y; // หน้า +Y (ผิวบน)

        float mx = bestArea.margin + held.halfSizeXZ.x;
        float mz = bestArea.margin + held.halfSizeXZ.y;

        local.x = Mathf.Clamp(local.x, -half.x + mx, half.x - mx);
        local.z = Mathf.Clamp(local.z, -half.z + mz, half.z - mz);

        if (bestArea.gridStep > 0.0001f)
        {
            local.x = Mathf.Round(local.x / bestArea.gridStep) * bestArea.gridStep;
            local.z = Mathf.Round(local.z / bestArea.gridStep) * bestArea.gridStep;
        }

        // 5) world pos + rotation แนวนอนเสมอ (บวก offset)
        previewWorld = tf.TransformPoint(local + new Vector3(0, held.surfaceOffset, 0));
        Quaternion baseRot = snapAlwaysHorizontal
            ? Quaternion.LookRotation(tf.forward, tf.up)
            : held.transform.rotation;
        previewRot = baseRot * Quaternion.Euler(placementEulerOffset);

        // 6) ย้ายไปตำแหน่งพรีวิวครั้งเดียวในเฟรมนี้
        held.transform.SetPositionAndRotation(previewWorld, previewRot);

        snappingPreview = true;
        previewArea = bestArea;

        // แสดง/ซ่อน grid ตามพื้นที่พรีวิวปัจจุบัน
        if (lastPreviewArea != previewArea)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            previewArea.ShowGrid(true);
            lastPreviewArea = previewArea;
        }
    }

    // ---------- หยิบ ----------
    void TryPick()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out var hit, maxPickDistance, rayMask, QueryTriggerInteraction.Ignore))
            return;

        var rec = hit.collider.GetComponentInParent<ReceiptItem>();
        if (!rec) return;

        // ถ้าเป็นลูกของ SnapArea แล้ว -> ไม่ให้หยิบอีก
        if (rec.transform.GetComponentInParent<SnapArea>() != null)
            return;

        held = rec;

        // ปิดฟิสิกส์ตอนถือ
        if (held.rb)
        {
            held.rb.isKinematic = true;
            held.rb.useGravity = false;
            held.rb.detectCollisions = true;
        }

        // Ignore ชนกับผู้เล่น
        heldCols.Clear();
        held.GetComponentsInChildren(true, heldCols);
        if (playerCollider)
            for (int i = 0; i < heldCols.Count; i++)
                Physics.IgnoreCollision(heldCols[i], playerCollider, true);
    }

    // ---------- วางจริง ----------
    void FinalizePlace(SnapArea area, Vector3 worldPos, Quaternion rot)
    {
        var box = area.area ? area.area : area.GetComponent<BoxCollider>();
        if (!box) return;
            // สมมติ receipt คือ BoxOrder หรือกล่อง
            //receipt.PastedLabel = true;
        // วางและผูกเป็นลูกของพื้นที่ (จะขยับตามกล่อง)
        held.transform.SetPositionAndRotation(worldPos, rot);
        held.transform.SetParent(area.transform, true);

        // ฟิสิกส์หลังวาง: เป็นคีนีแมติก (ไม่ตก/ไม่เด้ง)
        if (held.rb)
        {
            held.rb.isKinematic = true;
            held.rb.useGravity = false;
        }

        // ปลด Ignore กับผู้เล่น
        if (playerCollider)
        {
            heldCols.Clear();
            held.GetComponentsInChildren(true, heldCols);
            for (int i = 0; i < heldCols.Count; i++)
                Physics.IgnoreCollision(heldCols[i], playerCollider, false);
        }

        // ปิด grid ของพื้นที่ที่วาง + ของเก่า (ถ้ามี)
        area.ShowGrid(false);
        if (lastPreviewArea && lastPreviewArea != area)
            lastPreviewArea.ShowGrid(false);

        // เลิกถือ + ล้างสถานะ
        
        snappingPreview = false;
        previewArea = null;
        lastPreviewArea = null;
        heldCols.Clear();
        held = null;
    }
}

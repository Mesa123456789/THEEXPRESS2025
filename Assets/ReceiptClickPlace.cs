using System.Collections.Generic;
using UnityEngine;

public class ReceiptClickPlace : MonoBehaviour
{
    [Header("Snap Preview")]
    public float previewDetectRadius = 0.12f;
    public bool snapAlwaysHorizontal = true;
    public Vector3 placementEulerOffset = Vector3.zero;

    [Header("Pick / Hold")]
    public float maxPickDistance = 3.5f;
    public string pickupTag = "Reciept";         
    public Transform holdPoint;
    public Collider playerCollider;

    // runtime
    private Camera cam;
    private ReceiptItem held;
    private readonly List<Collider> heldCols = new List<Collider>();

    // preview state
    private bool snappingPreview;
    private SnapArea previewArea;
    private SnapArea lastPreviewArea;
    private Vector3 previewWorld;
    private Quaternion previewRot;

    public GameManager manager;

    void Awake()
    {
        cam = Camera.main;
        if (!cam) Debug.LogWarning("[ReceiptClickPlace] Main Camera not found.");
    }

    void Update()
    {
        if(manager.currentBox == null) return;

        if (held == null)
        {
            if (Input.GetMouseButtonDown(0)) 
                TryPick();
            return;
        }

        UpdateSnapPreview();

        if (!snappingPreview && holdPoint)
        {
            held.transform.position = holdPoint.position;
            held.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }

        if (Input.GetMouseButtonDown(0) && snappingPreview && previewArea != null)
        {
            FinalizePlace(previewArea, previewWorld, previewRot);
            if (manager && manager.currentBox != null)
                manager.currentBox.PastedLabel = true;
        }
    }

    void UpdateSnapPreview()
    {
        snappingPreview = false;
        previewArea = null;
        if (!held) return;

        var hits = Physics.OverlapSphere(
            held.transform.position,
            previewDetectRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            lastPreviewArea = null;
            return;
        }

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

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 samplePoint;
        if (bestCol.Raycast(ray, out var hitOnArea, maxPickDistance))
        {
            samplePoint = hitOnArea.point;
        }
        else
        {
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

        Transform tf = bestCol.transform;
        Vector3 local = tf.InverseTransformPoint(samplePoint);
        Vector3 half = bestCol.bounds.extents;

        local.y = +half.y; // ผิวบน

        float mx = bestArea.margin + held.halfSizeXZ.x;
        float mz = bestArea.margin + held.halfSizeXZ.y;

        local.x = Mathf.Clamp(local.x, -half.x + mx, half.x - mx);
        local.z = Mathf.Clamp(local.z, -half.z + mz, half.z - mz);

        if (bestArea.gridStep > 0.0001f)
        {
            local.x = Mathf.Round(local.x / bestArea.gridStep) * bestArea.gridStep;
            local.z = Mathf.Round(local.z / bestArea.gridStep) * bestArea.gridStep;
        }

        previewWorld = tf.TransformPoint(local + new Vector3(0, held.surfaceOffset, 0));
        Quaternion baseRot = snapAlwaysHorizontal
            ? Quaternion.LookRotation(tf.forward, tf.up)
            : held.transform.rotation;
        previewRot = baseRot * Quaternion.Euler(placementEulerOffset);

        held.transform.SetPositionAndRotation(previewWorld, previewRot);

        snappingPreview = true;
        previewArea = bestArea;

        if (lastPreviewArea != previewArea)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            previewArea.ShowGrid(true);
            lastPreviewArea = previewArea;
        }
    }

   
    void TryPick()
    {
        if (!manager.currentBox.Tape.isTapeDone) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // ยิงเรย์แบบไม่กรองเลเยอร์ แล้วคัดด้วยแท็กเอง
        if (!Physics.Raycast(ray, out var hit, maxPickDistance, ~0, QueryTriggerInteraction.Ignore))
            return;

        // หา ReceiptItem บนตัวที่โดนหรือพาเรนต์
        var rec = hit.collider.GetComponentInParent<ReceiptItem>();
        if (!rec) return;

        // ต้องมีแท็กตรงตาม pickupTag (กันหยิบของอื่น)
        if (!string.IsNullOrEmpty(pickupTag) && !rec.CompareTag(pickupTag))
            return;

        // ถ้าเป็นลูกของ SnapArea แล้ว -> ไม่ให้หยิบ
        if (rec.transform.GetComponentInParent<SnapArea>() != null)
            return;

        held = rec;

        if (held.rb)
        {
            held.rb.isKinematic = true;
            held.rb.useGravity = false;
            held.rb.detectCollisions = true;
        }

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

        held.transform.SetPositionAndRotation(worldPos, rot);
        held.transform.SetParent(area.transform, true);

        if (held.rb)
        {
            held.rb.isKinematic = true;
            held.rb.useGravity = false;
        }

        if (playerCollider)
        {
            heldCols.Clear();
            held.GetComponentsInChildren(true, heldCols);
            for (int i = 0; i < heldCols.Count; i++)
                Physics.IgnoreCollision(heldCols[i], playerCollider, false);
        }

        area.ShowGrid(false);
        if (lastPreviewArea && lastPreviewArea != area)
            lastPreviewArea.ShowGrid(false);

        snappingPreview = false;
        previewArea = null;
        lastPreviewArea = null;
        heldCols.Clear();
        held = null;
    }
}

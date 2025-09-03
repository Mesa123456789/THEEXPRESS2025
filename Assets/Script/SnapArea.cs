using UnityEngine;

[DisallowMultipleComponent]
public class SnapArea : MonoBehaviour
{
    [Tooltip("ถ้าเว้นว่าง จะใช้ BoxCollider บน GameObject เดียวกัน")]
    public BoxCollider area;

    [Tooltip("ระยะกันขอบ (เมตร)")]
    public float margin = 0.005f;

    [Tooltip("สแนปเข้าตาราง (เมตร) ใส่ 0 เพื่อปิด")]
    public float gridStep = 0f;

    [Header("In-Game Grid Visual")]
    [Tooltip("ใส่ GameObject ของกริด (เช่น Quad ที่มีวัสดุลายกริด). ระบบจะ SetActive(true/false) จาก ReceiptClickPlace")]
    public GameObject gridVisual;

    [Header("Debug (เฉพาะ Scene View)")]
    public bool debugDrawGizmos = true;
    public Color gizmoAreaColor = new Color(0f, 0.7f, 1f, 0.2f);
    public Color gizmoSafeColor = new Color(0f, 1f, 0.2f, 0.25f);
    public Color gizmoWire = new Color(0f, 0.7f, 1f, 1f);

    void Reset()
    {
        if (!area) area = GetComponent<BoxCollider>();
        if (!area)
        {
            area = gameObject.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.size = new Vector3(0.20f, 0.01f, 0.30f);
            area.center = new Vector3(0, 0.005f, 0);
        }
    }

    void Awake()
    {
        // ปิดกริดไว้ก่อนเสมอ
        if (gridVisual) gridVisual.SetActive(false);
    }

    void OnValidate()
    {
        if (area && !area.isTrigger) area.isTrigger = true;
        if (margin < 0f) margin = 0f;
        if (gridStep < 0f) gridStep = 0f;
    }

    // ควบคุมการแสดงกริดจากภายนอก (ReceiptClickPlace)
    public void ShowGrid(bool show)
    {
        if (gridVisual) gridVisual.SetActive(show);
    }

    // ---------- Debug gizmos (เฉพาะ Scene) ----------
    void OnDrawGizmos()
    {
        if (!debugDrawGizmos) return;
        var col = area ? area : GetComponent<BoxCollider>();
        if (!col) return;

        Gizmos.color = gizmoAreaColor;
        Matrix4x4 m = Matrix4x4.TRS(col.transform.TransformPoint(col.center), col.transform.rotation, col.transform.lossyScale);
        var prev = Gizmos.matrix; Gizmos.matrix = m;
        Gizmos.DrawCube(Vector3.zero, col.size);
        Gizmos.color = gizmoWire;
        Gizmos.DrawWireCube(Vector3.zero, col.size);

        Vector3 safeSize = new Vector3(
            Mathf.Max(0.0001f, col.size.x - 2f * margin),
            col.size.y,
            Mathf.Max(0.0001f, col.size.z - 2f * margin)
        );
        Gizmos.color = gizmoSafeColor;
        Gizmos.DrawCube(Vector3.zero, safeSize);
        Gizmos.matrix = prev;
    }
}

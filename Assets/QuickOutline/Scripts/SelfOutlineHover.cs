using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SelfOutlineHover : MonoBehaviour
{
    [Header("Outline Targets")]
    [Tooltip("ปล่อยว่างได้: จะค้นหา Outline ใต้ลูก ๆ อัตโนมัติ")]
    public Outline[] outlineTargets;

    [Header("Hover Check")]
    public float maxDistance = 3f;
    [Range(1f, 30f)] public float halfViewAngle = 6f;
    public LayerMask rayMask = ~0;

    [Header("Outline Mode When Hover")]
    public Outline.Mode outlineModeWhenHover = Outline.Mode.OutlineVisible;

    Camera _cam;
    bool _isHover;

    void Awake()
    {
        EnsureCamera();
        EnsureTargets();
        SetOutline(false); // ปิดไว้ก่อน
    }

    void Update()
    {
        EnsureCamera();

        bool nowHover = ComputeHover();
        if (nowHover != _isHover)
        {
            _isHover = nowHover;
            SetOutline(_isHover);
        }
    }

    // ---------- Hover detection ----------
    bool ComputeHover()
    {
        if (!_cam) return false;

        // จุดโฟกัส = center ของ Collider/Renderer (ถ้าไม่มี ใช้ transform)
        Vector3 targetPos = GetFocusPoint();
        Vector3 camPos = _cam.transform.position;
        Vector3 toTarget = targetPos - camPos;
        float dist = toTarget.magnitude;
        if (dist > maxDistance) return false;

        Vector3 dir = toTarget / (dist > 1e-4f ? dist : 1f);
        if (Vector3.Angle(_cam.transform.forward, dir) > halfViewAngle) return false;

        // ต้องชนวัตถุนี้เป็นอันดับแรก
        if (Physics.Raycast(camPos, dir, out var hit, maxDistance, rayMask, QueryTriggerInteraction.Ignore))
            return hit.collider && hit.collider.transform.IsChildOf(transform);

        return false;
    }

    Vector3 GetFocusPoint()
    {
        var col = GetComponentsInChildren<Collider>(true).FirstOrDefault();
        if (col) return col.bounds.center;
        var r = GetComponentsInChildren<Renderer>(true).FirstOrDefault();
        if (r) return r.bounds.center;
        return transform.position;
    }

    // ---------- Outline on/off ----------
    void SetOutline(bool on)
    {
        EnsureTargets();
        foreach (var o in outlineTargets.Where(o => o))
        {
            if (on && o.OutlineMode != outlineModeWhenHover)
                o.OutlineMode = outlineModeWhenHover;
            o.enabled = on;
        }
    }

    void EnsureTargets()
    {
        if (outlineTargets == null || outlineTargets.Length == 0)
            outlineTargets = GetComponentsInChildren<Outline>(includeInactive: true);
    }

    void EnsureCamera()
    {
        if (_cam) return;
        _cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
    }
}

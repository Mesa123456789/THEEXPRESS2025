using System.Linq;
using UnityEngine;
using TMPro;

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

    // ---------- UI Prompt (TMP only) ----------
    [Header("Prompt UI (Optional)")]
    [Tooltip("เปิด/ปิดการแสดง UI ตอนเล็ง")]
    public bool showPromptUI = true;

    [Tooltip("โหนด UI หลักที่ต้องเปิด/ปิด (แนะนำให้วาง TMP เป็นลูกของตัวนี้)")]
    public GameObject promptRoot;

    [Tooltip("TextMeshProUGUI ที่จะเปลี่ยนข้อความ")]
    public TMP_Text promptTMP;

    [TextArea] public string defaultMessage = "Interact";
    [Tooltip("ดีเลย์ก่อนแสดง (วินาที)")]
    public float showDelay = 0f;
    [Tooltip("ดีเลย์ก่อนซ่อน (วินาที)")]
    public float hideDelay = 0f;

    [Tooltip("ถ้า TMP ไม่ได้เป็นลูกของ promptRoot ให้เลือกว่าจะปิดด้วย SetActive บน GameObject หรือใช้ enabled")]
    public bool toggleTMPGameObject = true;

    // ---------- Internals ----------
    Camera _cam;
    bool _isHover;                  // พฤติกรรมเดิม (คงไว้)
    bool _promptShown;
    float _showTimer, _hideTimer;

    public bool IsHovering => _isHover; // เผื่อสคริปต์อื่นอยากเช็ค

    void Awake()
    {
        EnsureCamera();
        EnsureTargets();
        SetOutline(false);                        // เดิม: ปิด outline ไว้ก่อน
        ApplyPromptVisible(false, immediate: true); // ซ่อน UI ตอนเริ่ม
    }

    void Update()
    {
        EnsureCamera();

        // ===== เดิม: คำนวณ hover + คุม Outline =====
        bool nowHover = ComputeHover();
        if (nowHover != _isHover)
        {
            _isHover = nowHover;
            SetOutline(_isHover);
        }

        // ===== ใหม่: คุม UI ตามสถานะ hover =====
        if (!showPromptUI)
        {
            if (_promptShown) ApplyPromptVisible(false);
            return;
        }

        if (_isHover)
        {
            _hideTimer = 0f;
            if (!_promptShown)
            {
                _showTimer += Time.unscaledDeltaTime;
                if (_showTimer >= showDelay) ApplyPromptVisible(true);
            }
            else
            {
                UpdatePromptText(); // อัปเดตข้อความระหว่างเปิดได้
            }
        }
        else
        {
            _showTimer = 0f;
            if (_promptShown)
            {
                _hideTimer += Time.unscaledDeltaTime;
                if (_hideTimer >= hideDelay) ApplyPromptVisible(false);
            }
        }
    }

    // ---------- Hover detection (พฤติกรรมเดิม) ----------
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

    // ---------- Outline on/off (พฤติกรรมเดิม) ----------
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

    // ---------- Prompt UI helpers ----------
    void ApplyPromptVisible(bool visible, bool immediate = false)
    {
        _promptShown = visible;

        // ซ่อน/โชว์ root ถ้ามี
        if (promptRoot) promptRoot.SetActive(visible);

        // ซ่อน/โชว์ TMP แยก (กรณีไม่ได้อยู่ใต้ root)
        if (promptTMP)
        {
            if (toggleTMPGameObject) promptTMP.gameObject.SetActive(visible);
            else promptTMP.enabled = visible;

            if (visible) UpdatePromptText();
        }

        if (immediate)
        {
            _showTimer = 0f;
            _hideTimer = 0f;
        }
    }

    void UpdatePromptText()
    {
        if (promptTMP && !string.IsNullOrEmpty(defaultMessage))
            promptTMP.text = defaultMessage;
    }
}

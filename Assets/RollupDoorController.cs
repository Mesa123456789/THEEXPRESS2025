using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RollupDoorController : MonoBehaviour
{
    [Header("Refs")]
    public GameManager gameManager;

    [Tooltip("บานประตูที่จะสเกล (เว้นว่างจะใช้ transform ของตัวเอง)")]
    public Transform doorTransform;

    [Tooltip("ต้นกำเนิด Ray (กล้องผู้เล่น/หัวผู้เล่น)")]
    public Transform rayOrigin;

    [Tooltip("UI ข้อความเตือน (ลูกของประตู)")]
    public TMP_Text shopUI;

    [Header("Interaction")]
    [Tooltip("Tag ของประตู")]
    public string doorTag = "RollDoor";

    [Tooltip("ระยะ Raycast จากผู้เล่น")]
    public float rayDistance = 3f;

    public KeyCode toggleKey = KeyCode.E;

    [Header("Roll-up Scale (แกน Z)")]
    public float closedZ = 100f;
    public float openZ = 36f;
    public float scaleDuration = 0.6f;

    // ----- Runtime -----
    Vector3 _baseScaleXY;
    bool _isOpen;
    Coroutine _scaleCo;

    int _lastCheckedHour = -1;
    bool _showedOpenPromptThisDay = false;
    bool _showedClosePromptThisNight = false;

    void Awake()
    {
        if (!doorTransform) doorTransform = transform;
        if (!rayOrigin && Camera.main) rayOrigin = Camera.main.transform;
    }

    void Start()
    {
        _baseScaleXY = new Vector3(doorTransform.localScale.x, doorTransform.localScale.y, 0f);

        _isOpen = gameManager && gameManager.shopIsOpen;
        ApplyScaleInstant(_isOpen);

        TryShowTimeBoundPrompt(initialCheck: true);
    }

    void Update()
    {
        TryShowTimeBoundPrompt(initialCheck: false);

        if (Input.GetKeyDown(toggleKey) && IsLookingAtDoor())
        {
            TryToggleDoor();
        }
    }

    bool IsLookingAtDoor()
    {
        if (!rayOrigin) return false;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            return hit.collider && hit.collider.CompareTag(doorTag);
        }
        return false;
    }

    void TryToggleDoor()
    {
        // ถ้าจะปิด แต่ไม่ใช่เวลาตี 2 → ห้ามปิด
        if (_isOpen && gameManager && gameManager.CurrentHour != gameManager.shopCloseHour)
        {
            Debug.Log("ยังไม่ถึงเวลาปิดร้าน (ต้องตี 2 เท่านั้น)");
            return;
        }

        ToggleDoor();
    }

    void ToggleDoor()
    {
        _isOpen = !_isOpen;

        if (_scaleCo != null) StopCoroutine(_scaleCo);
        float targetZ = _isOpen ? openZ : closedZ;
        _scaleCo = StartCoroutine(ScaleZTo(targetZ, scaleDuration));

        if (gameManager) gameManager.SetShopOpen(_isOpen);

        if (shopUI) shopUI.gameObject.SetActive(false);
    }

    IEnumerator ScaleZTo(float targetZ, float duration)
    {
        Vector3 start = doorTransform.localScale;
        Vector3 end = new Vector3(_baseScaleXY.x, _baseScaleXY.y, targetZ);

        float t = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            doorTransform.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }
        doorTransform.localScale = end;
    }

    void ApplyScaleInstant(bool open)
    {
        float z = open ? openZ : closedZ;
        doorTransform.localScale = new Vector3(_baseScaleXY.x, _baseScaleXY.y, z);
    }

    void TryShowTimeBoundPrompt(bool initialCheck)
    {
        if (!gameManager || !shopUI) return;

        int hour = gameManager.CurrentHour;

        if (!initialCheck && hour == _lastCheckedHour) return;
        _lastCheckedHour = hour;

        int openH = ((gameManager.shopOpenHour % 24) + 24) % 24;
        int closeH = ((gameManager.shopCloseHour % 24) + 24) % 24;

        if (!_showedOpenPromptThisDay && hour == openH && !_isOpen)
        {
            ShowPrompt("OPEN NOW");
            _showedOpenPromptThisDay = true;
            _showedClosePromptThisNight = false;
            return;
        }

        if (!_showedClosePromptThisNight && hour == closeH && _isOpen)
        {
            ShowPrompt("CLOSE NOW");
            _showedClosePromptThisNight = true;
            return;
        }

        if (hour != openH && hour != closeH)
        {
            if (shopUI) shopUI.gameObject.SetActive(false);
        }
    }

    void ShowPrompt(string msg)
    {
        if (!shopUI) return;
        shopUI.text = msg;
        shopUI.gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!rayOrigin) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * rayDistance);
    }
#endif
}

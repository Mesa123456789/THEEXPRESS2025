using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Door Rotate (local Z)")]
    public float closedZ = 0f;
    public float openZ = 90f;
    public float rotateSpeedDegPerSec = 180f;
    public float maxRotateSeconds = 3f;
    public bool startOpened = false;

    [Header("Physics")]
    public bool manageRigidbodyWhileMoving = true;

    [Header("Handle (local X press & release DURING move)")]
    public Transform handle;                 // ลากลูกบิด (child) มาตรงนี้
    public float handleDownX = -90f;         // กดลง
    public float handlePressShare = 0.25f; // สัดส่วนเวลา press เทียบกับเวลาหมุน
    public float handleHoldShare = 0.10f; // สัดส่วนเวลา hold
    public float handleReleaseShare = 0.25f; // สัดส่วนเวลา release
    public AnimationCurve handleEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Quaternion _baseLocalRot;
    Quaternion _targetClosed;
    Quaternion _targetOpen;

    bool _isOpened;
    bool _isMoving;
    Rigidbody _rb;

    // handle state
    Quaternion _handleBaseLocalRot;
    Quaternion _handleDownLocalRot;

    // coroutines
    Coroutine _doorCo;
    Coroutine _handleCo;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _baseLocalRot = transform.localRotation;
        _targetClosed = _baseLocalRot * Quaternion.Euler(0f, 0f, closedZ);
        _targetOpen = _baseLocalRot * Quaternion.Euler(0f, 0f, openZ);

        _isOpened = startOpened;
        transform.localRotation = _isOpened ? _targetOpen : _targetClosed;

        if (handle)
        {
            _handleBaseLocalRot = handle.localRotation;
            var e = _handleBaseLocalRot.eulerAngles;
            _handleDownLocalRot = Quaternion.Euler(handleDownX, e.y, e.z);
        }
    }

    public void Toggle()
    {
        if (_isMoving) return;
        _isOpened = !_isOpened;

        if (_doorCo != null) StopCoroutine(_doorCo);
        _doorCo = StartCoroutine(RotateTo(_isOpened ? _targetOpen : _targetClosed));
    }

    IEnumerator RotateTo(Quaternion target)
    {
        _isMoving = true;

        // --- คำนวณเวลาหมุนโดยประมาณ เพื่อนำไปซิงก์กับลูกบิด ---
        float angleRemain = Quaternion.Angle(transform.localRotation, target);
        float estDuration = (rotateSpeedDegPerSec > 0f) ? angleRemain / rotateSpeedDegPerSec : 0f;
        estDuration = Mathf.Clamp(estDuration, 0.05f, maxRotateSeconds);

        // --- สตาร์ตลูกบิด "พร้อมกัน" กับการหมุนบาน ---
        if (handle)
        {
            if (_handleCo != null) StopCoroutine(_handleCo);
            _handleCo = StartCoroutine(AnimateHandleDuringMove(estDuration));
        }

        // --- จัดการฟิสิกส์ชั่วคราว ---
        bool rbChanged = false;
        if (manageRigidbodyWhileMoving && _rb != null)
        {
            rbChanged = !_rb.isKinematic;
            _rb.isKinematic = true;
#if UNITY_600_0_OR_NEWER || UNITY_2022_3_OR_NEWER
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
#else
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
#endif
        }

        // --- หมุนบาน ---
        float elapsed = 0f;
        while (true)
        {
            float remain = Quaternion.Angle(transform.localRotation, target);
            if (remain <= 0.1f)
            {
                transform.localRotation = target; // snap
                break;
            }

            float stepDeg = rotateSpeedDegPerSec * Time.deltaTime;
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, stepDeg);

            elapsed += Time.deltaTime;
            if (elapsed > maxRotateSeconds)
            {
                transform.localRotation = target;
                break;
            }

            yield return null;
        }

        if (manageRigidbodyWhileMoving && _rb != null && rbChanged)
            _rb.isKinematic = false;

        _isMoving = false;
    }

    IEnumerator AnimateHandleDuringMove(float total)
    {
        // แปลงสัดส่วนเป็นเวลา จริง (รวมทั้งหมดไม่จำเป็นต้อง = 1)
        float pressT = Mathf.Max(0f, handlePressShare) * total;
        float holdT = Mathf.Max(0f, handleHoldShare) * total;
        float releaseT = Mathf.Max(0f, handleReleaseShare) * total;

        // 1) กดลง
        if (pressT > 0f)
        {
            float t = 0f;
            while (t < pressT)
            {
                float k = handleEase.Evaluate(t / pressT);
                handle.localRotation = Quaternion.Slerp(_handleBaseLocalRot, _handleDownLocalRot, k);
                t += Time.deltaTime;
                yield return null;
            }
        }
        handle.localRotation = _handleDownLocalRot;

        // 2) ค้าง
        if (holdT > 0f) yield return new WaitForSeconds(holdT);

        // 3) ปล่อยกลับ
        if (releaseT > 0f)
        {
            float t = 0f;
            while (t < releaseT)
            {
                float k = handleEase.Evaluate(t / releaseT);
                handle.localRotation = Quaternion.Slerp(_handleDownLocalRot, _handleBaseLocalRot, k);
                t += Time.deltaTime;
                yield return null;
            }
        }
        handle.localRotation = _handleBaseLocalRot;
    }
}

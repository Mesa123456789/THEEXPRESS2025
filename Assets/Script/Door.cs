using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Local Z (degrees)")]
    [Tooltip("����Դ (�Ϳ��᡹ Z Ẻ local)")]
    public float closedZ = 0f;

    [Tooltip("����Դ (�Ϳ��᡹ Z Ẻ local)")]
    public float openZ = 90f;

    [Header("Animation")]
    [Tooltip("ͧ�ҵ���Թҷ� (����ҳ��� �ŧ�繤���������ع Quaternion)")]
    public float rotateSpeedDegPerSec = 180f;

    [Tooltip("�Ѵ�����ٷչ����Թ���ҹ�� (�ѹ�ٻ��ҧ)")]
    public float maxRotateSeconds = 3f;

    [Tooltip("����������Դ�������")]
    public bool startOpened = false;

    [Header("Optional (�й�)")]
    [Tooltip("����� Rigidbody ����� true �е�� isKinematic �����ع �ѹ���ԡ�����ѹ")]
    public bool manageRigidbodyWhileMoving = true;

    Quaternion _baseLocalRot;
    Quaternion _targetClosed;
    Quaternion _targetOpen;

    bool _isOpened;
    bool _isMoving;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // �纰ҹ localRotation �Ѩ�غѹ����� reference
        _baseLocalRot = transform.localRotation;

        // ������� = �ҹ * ��ع�ͺ Z ����Ϳ��
        _targetClosed = _baseLocalRot * Quaternion.Euler(0f, 0f, closedZ);
        _targetOpen = _baseLocalRot * Quaternion.Euler(0f, 0f, openZ);

        _isOpened = startOpened;
        transform.localRotation = _isOpened ? _targetOpen : _targetClosed;
    }

    public void Toggle()
    {
        if (_isMoving) return;
        _isOpened = !_isOpened;
        StopAllCoroutines();
        StartCoroutine(RotateTo(_isOpened ? _targetOpen : _targetClosed));
    }

    System.Collections.IEnumerator RotateTo(Quaternion target)
    {
        _isMoving = true;

        // �Ѵ��ÿ��ԡ��ѹ����ç (�����)
        bool rbChanged = false;
        if (manageRigidbodyWhileMoving && _rb != null)
        {
            rbChanged = !_rb.isKinematic;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        float elapsed = 0f;

        while (true)
        {
            // �礤ú��͹��ѹ overshoot/��ҧ
            float remain = Quaternion.Angle(transform.localRotation, target);
            if (remain <= 0.1f) // ࡳ�쨺
            {
                transform.localRotation = target; // snap �Դ��ͺ
                break;
            }

            // �ӹǳ "����" �����ع�������ҡ��������ͧ��/�Թҷ�
            float stepDeg = rotateSpeedDegPerSec * Time.deltaTime;

            // ��ع�����Ҵ��� RotateTowards (��ʹ��� ���ǹ 0/360)
            transform.localRotation =
                Quaternion.RotateTowards(transform.localRotation, target, stepDeg);

            elapsed += Time.deltaTime;
            if (elapsed > maxRotateSeconds)
            {
                // �ѹ�ٻ��ҧ: �Ѵ����� snap ��Ҩش����
                transform.localRotation = target;
                break;
            }

            yield return null;
        }

        // �׹ʶҹ� Rigidbody
        if (manageRigidbodyWhileMoving && _rb != null && rbChanged)
            _rb.isKinematic = false;

        _isMoving = false;
    }
}

using UnityEngine;

public class SmoothLidClose : MonoBehaviour
{
    public float closedAngle = -180f; // ปรับตามที่ต้องการ
    public float openAngle = 0f;

    public float smoothSpeed = 3.5f;

    // เพิ่มตัวแปรสำหรับตำแหน่ง y,z ตั้งต้น
    private float initialY;
    private float initialZ;

    private float targetAngle;
    private float currentAngle;
    public bool isClosed = false;

    private void Start()
    {
        // ดึงค่าต้นฉบับจาก Transform ของ Lid
        Vector3 startAngles = transform.localEulerAngles;
        initialY = startAngles.y;
        initialZ = startAngles.z;

        currentAngle = openAngle;
        targetAngle = openAngle;
        isClosed = false;

        // เซ็ตด้วยแกน y,z ตั้งต้น
        transform.localRotation = Quaternion.Euler(currentAngle, initialY, initialZ);
    }

    private void Update()
    {
        if (Mathf.Abs(currentAngle - targetAngle) > 0.01f)
        {
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
            transform.localRotation = Quaternion.Euler(currentAngle, initialY, initialZ);

            if (Mathf.Abs(currentAngle - closedAngle) < 0.5f)
            {
                currentAngle = closedAngle;
                transform.localRotation = Quaternion.Euler(currentAngle, initialY, initialZ);
                isClosed = true;
            }
        }
    }

    public void CloseLid()
    {
        targetAngle = closedAngle;
        isClosed = false;
    }
}
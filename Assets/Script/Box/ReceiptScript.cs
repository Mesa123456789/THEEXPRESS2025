using UnityEngine;

public class ReceiptScript : MonoBehaviour
{
    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 offset;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        // เริ่มลากใบเสร็จ
        isDragging = true;
        offset = transform.position - GetMouseWorldPos();
    }

    private void OnMouseUp()
    {
        isDragging = false;

        // เช็คว่าปล่อยทับกล่องหรือไม่
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f))
        {
            if (hit.collider.CompareTag("Box"))
            {
               
                // เช็คว่าตำแหน่งไม่เกินขอบกล่อง
                Vector3 boxMin = hit.collider.bounds.min;
                Vector3 boxMax = hit.collider.bounds.max;
                Vector3 point = hit.point;

                if (point.x > boxMin.x && point.x < boxMax.x &&
                    point.y > boxMin.y && point.y < boxMax.y &&
                    point.z > boxMin.z && point.z < boxMax.z)
                {
                    // แปะใบเสร็จลงกล่อง
                    transform.position = hit.point;
                    transform.SetParent(hit.collider.transform);
                    Debug.Log("วางใบเสร็จลงกล่องสำเร็จ!");
                    // แจ้งกล่องว่ามีใบเสร็จแล้ว
                    BoxScript box = hit.collider.GetComponent<BoxScript>();
                    if (box != null)
                    //    box.receiptPlaced = true;
                    this.tag = null;

                    
                    return; // จบที่แปะสำเร็จ
                }
            }
        }
        // ถ้าไม่ได้แปะบนกล่อง ใบเสร็จจะอยู่ที่จุดปล่อยปกติ
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPos() + offset;
            transform.position = new Vector3(mousePos.x, mousePos.y, mousePos.z);
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = 3f; // ปรับระยะห่างจากกล้องตามต้องการ
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}
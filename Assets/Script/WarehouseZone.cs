using UnityEngine;

public class WarehouseZone : MonoBehaviour
{
    GameObject boxInZone = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            boxInZone = other.gameObject;
            Debug.Log("กล่องเข้าเขตโกดัง รอกด E เพื่อเก็บเข้าคลัง");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box") && boxInZone == other.gameObject)
        {
            boxInZone = null;
        }
    }

    void Update()
    {
        if (boxInZone != null && Input.GetKeyDown(KeyCode.E))
        {
            Destroy(boxInZone);
            boxInZone = null;
            Debug.Log("เก็บกล่องเข้าคลังแล้ว!");
        }
    }
}
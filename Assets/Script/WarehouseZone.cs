using System;
using UnityEngine;

public class WarehouseZone : MonoBehaviour
{
    public static event Action OnBoxStored; 
    public GameObject boxInZone = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BoxInteract"))
        {
            boxInZone = other.gameObject;
            Debug.Log("กล่องเข้าเขตโกดัง รอกด E เพื่อเก็บเข้าคลัง");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BoxInteract") && boxInZone == other.gameObject)
        {
            boxInZone = null;
        }
    }

    void Update()
    {
        if (boxInZone != null )
        {
            Destroy(boxInZone);
            boxInZone = null;
            Debug.Log("เก็บกล่องเข้าคลังแล้ว!");

            OnBoxStored?.Invoke(); // << แจ้งทุก NPC ว่าเก็บเข้าคลังแล้ว
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

public class WarehouseZone : MonoBehaviour
{
    public static event Action OnBoxStored; 
    public GameObject boxInZone = null;
    IEnumerator Start()
    {
        if (FadeManager.Instance != null)
            yield return StartCoroutine(FadeManager.Instance.FadeOut(1.5f)); // ค่อย ๆ สว่าง

        Time.timeScale = 1f; // คืนเวลาให้เดินต่อหลังเฟดออกเสร็จ
    }
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

            OnBoxStored?.Invoke(); 
        }
    }
}

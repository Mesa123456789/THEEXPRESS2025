using System.Collections;
using UnityEngine;

public class WarehouseZone : MonoBehaviour
{
    public GameObject boxInZone = null;
    private bool canStoreHere = false;

    public enum ZoneType { LegalZone, IllegalZone }
    public ZoneType zoneType = ZoneType.LegalZone;

    IEnumerator Start()
    {
        if (FadeManager.Instance != null)
            yield return StartCoroutine(FadeManager.Instance.FadeOut(1.5f));
        Time.timeScale = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ต้องเป็นกล่องเท่านั้น
        if (!other.CompareTag("BoxInteract")) return;

        // ต้องมี BoxScript
        var box = other.GetComponent<BoxScript>();
        if (!box)
        {
            Debug.LogWarning("[WarehouseZone] BoxInteract ไม่มี BoxScript");
            return;
        }

        boxInZone = other.gameObject;

        // ตัดสินใจว่าโซนนี้เก็บได้ไหม
        // (สมมติ BoxPrice > 0 = ของถูกกฎหมาย, < 0 = ของผิดกฎหมาย)
        canStoreHere = false;
        if (zoneType == ZoneType.LegalZone && box.BoxPrice > 0)
            canStoreHere = true;
        else if (zoneType == ZoneType.IllegalZone && box.BoxPrice < 0)
            canStoreHere = true;

        Debug.Log($"[WarehouseZone] Enter: {boxInZone.name}, canStoreHere={canStoreHere}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("BoxInteract")) return;

        if (boxInZone == other.gameObject)
        {
            Debug.Log("[WarehouseZone] Exit box");
            boxInZone = null;
            canStoreHere = false;
        }
    }

    void Update()
    {
        // ต้องการ “คลิกเมาส์ซ้าย” เพื่อเก็บกล่อง
        if (boxInZone != null && canStoreHere && Input.GetMouseButtonDown(0))
        {
            Debug.Log("[WarehouseZone] เก็บกล่องเข้าคลังแล้ว!");
            Destroy(boxInZone);
            boxInZone = null;
            canStoreHere = false;

            // แจ้งระบบอื่น ๆ ว่าเก็บแล้ว (ถ้ามี)
            // BoxScript.OnBoxStored?.Invoke();
        }

        // (ทางเลือก) ถ้าอยากกดคีย์ E แทน:
        // if (boxInZone != null && canStoreHere && Input.GetKeyDown(KeyCode.E)) { ... }
    }
}

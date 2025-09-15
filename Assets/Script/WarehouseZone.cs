using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static WarehouseZone;

public class WarehouseZone : MonoBehaviour
{
    public GameObject boxInZone = null;
    private bool canStoreHere = false;
    public ZoneType zoneType = ZoneType.LegalZone;
    public enum ZoneType
    {
        LegalZone,    
        IllegalZone   
    }
    IEnumerator Start()
    {
        if (FadeManager.Instance != null)
            yield return StartCoroutine(FadeManager.Instance.FadeOut(1.5f));

        Time.timeScale = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(boxInZone == null) return;
        BoxScript box = other.GetComponent<BoxScript>();
        if (other.CompareTag("BoxInteract")) 
            boxInZone = other.gameObject;
        if (zoneType == ZoneType.LegalZone && box.BoxPrice > 0)
        {
            canStoreHere = true;
        }
        else if (zoneType == ZoneType.IllegalZone && box.BoxPrice < 0)
        {
            canStoreHere = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (boxInZone == null) return;
        if (other.CompareTag("BoxInteract") && boxInZone == other.gameObject)
        {
            boxInZone = null;
            canStoreHere = false;
        }
    }

    void Update()
    {
        if (boxInZone != null && canStoreHere)
        {
            Destroy(boxInZone);
            boxInZone = null;
            canStoreHere = false;
            Debug.Log("เก็บกล่องเข้าคลังแล้ว!");
        }
    }
}

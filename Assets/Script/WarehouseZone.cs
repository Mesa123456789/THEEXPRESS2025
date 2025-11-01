using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class WarehouseZone : MonoBehaviour
{
    public GameObject boxInZone = null;
    private bool canStoreHere = false;

    public enum ZoneType { LegalZone, IllegalZone }
    public ZoneType zoneType = ZoneType.LegalZone;

    public FormChecker formChecker;
    public TutorialSlideUIQueue TutorialSlideUIQueue;

    private void Start()
    {
        TutorialSlideUIQueue = FindFirstObjectByType<TutorialSlideUIQueue>();
    }
    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("BoxInteract")) return;


        var box = other.GetComponent<BoxScript>();
        boxInZone = other.gameObject;


        if (zoneType == ZoneType.LegalZone && box.illegal == false)
            canStoreHere = true;
        else if (zoneType == ZoneType.IllegalZone && box.illegal == true)
            canStoreHere = true;
        else
            canStoreHere = false;

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
        if (canStoreHere)
        {
            var box = boxInZone.GetComponent<BoxScript>();
            if (box != null)
            {
                box.StoreBox(); 
             box.TutorialSlideUIQueue.CompleteCurrentByIndex(10);
            }

            Destroy(boxInZone);  // แล้วค่อยลบกล่อง
            boxInZone = null;
            canStoreHere = false;
           
            TutorialSlideUIQueue.EnqueueTutorialByIndex(11);
        }
    }

}

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
            Destroy(boxInZone);
            boxInZone = null;
            canStoreHere = false;


        }

    }
}

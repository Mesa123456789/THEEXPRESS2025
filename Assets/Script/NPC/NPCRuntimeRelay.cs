using UnityEngine;

public class NPCRuntimeRelay : MonoBehaviour
{
    // เรียกจาก UnityEvent ของช้อยส์ที่ต้องการให้ NPC ออก
    public void ExitNPCNow(bool destroyItemToo = true)
    {
        var npc = FindFirstObjectByType<NPC>();
        if (!npc) { Debug.LogWarning("No NPC found in scene."); return; }

        GameObject itemGo = null;
        if (destroyItemToo)
        {
            var item = FindFirstObjectByType<ItemScript>();
            if (item) itemGo = item.gameObject;
        }

        npc.ForceExitAndClearItem(itemGo);
    }
}

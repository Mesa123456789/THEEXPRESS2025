using UnityEngine;

public class ItemScript : MonoBehaviour
{
    public itemData itemData;
    public NPC ownerNPC { get; private set; }

    void Awake()
    {
        ownerNPC = NPCSpawner.CurrentNPC;   // อ้างอิง NPC ปัจจุบันจาก Spawner
    }
}

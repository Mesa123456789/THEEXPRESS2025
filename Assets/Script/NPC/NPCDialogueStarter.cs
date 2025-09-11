using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCDialogueStarter : MonoBehaviour
{
    [Header("Behavior")]
    public bool oneShot = false;      // คุยครั้งเดียวพอไหม
    private bool locked;
    public GameObject ui;
    void OnMouseDown() 
    {
        if (ui == null) return;
        Destroy(ui);
        TryStartDialogue();
    }

    public void TryStartDialogue()
    {
        if (locked) return;

        var mgr = ItemDialogueManager.Instance;
        if (!mgr)
        {
            Debug.LogWarning("[NPCDialogueStarter] No ItemDialogueManager in scene.");
            return;
        }

        // กันเริ่มซ้อน: ถ้า panel แสดงอยู่แล้วไม่เริ่มใหม่
        if (mgr.panel && mgr.panel.activeSelf) return;

        // ===== หา ItemScript ตัวแรกในซีน =====
        var item = FindFirstObjectByType<ItemScript>();
        if (!item)
        {
            Debug.LogWarning("[NPCDialogueStarter] No ItemScript found in scene.");
            return;
        }

        // ดึง ItemDialogueData จาก item
        ItemDialogueData dlg =
            item.dialogueSequence
            ?? (item.itemData ? item.itemData.dialogueData : null);

        if (!dlg)
        {
            Debug.LogWarning("[NPCDialogueStarter] Found ItemScript but no ItemDialogueData (dialogueSequence / itemData.dialogueData).");
            return;
        }

        // เริ่มไดอะล็อก
        mgr.Show(
            dlg,
            onChoice: (choiceIdx) =>
            {
                // ถ้าต้องการ: ปุ่มลำดับที่ 2 (index=1) ให้ NPC เจ้าของเดินออกและเคลียร์ของ
                if (choiceIdx == 1)
                {
                    //item.ownerNPC?.ForceExitAndClearItem(item.gameObject);
                }
            },
            onFinished: () =>
            {
                if (oneShot) locked = true;
            }
        );
    }
}

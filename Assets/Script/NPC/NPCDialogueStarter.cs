using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCDialogueStarter : MonoBehaviour
{
    [Header("Behavior")]
    public bool oneShot = false;          // คุยครั้งเดียว
    private bool locked;
    public GameObject ui;                 // Hint/ป้าย “กดคุย” (จะถูกลบทิ้งครั้งแรกที่กด)

    [Header("Optional Override")]
    [Tooltip("ถ้าตั้งไว้ จะใช้ไดอะล็อกนี้แทนทุกกรณี (ทั้งตำรวจ/ลูกค้า)")]
    public ItemDialogueData overrideDialogue;

    void OnMouseDown()
    {
        if (ui) Destroy(ui);
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

        // กันกดซ้อน ถ้ากำลังเปิดอยู่ไม่เริ่มใหม่
        if (mgr.panel && mgr.panel.activeSelf) return;

        // 1) ถ้ามี override ให้ใช้ก่อน
        ItemDialogueData dlg = overrideDialogue;

        // 2) ถ้า GameObject นี้เป็น 'ตำรวจ' ให้ใช้ policeDialogue
        if (!dlg)
        {
            var police = GetComponent<NPCPolice>();
            if (police && police.policeDialogue)
            {
                dlg = police.policeDialogue;
            }
        }

        // 3) ไม่ใช่ตำรวจ → หาจาก ItemScript (ของบนโต๊ะ/กล่อง) เหมือนเดิม
        if (!dlg)
        {
            var item = FindFirstObjectByType<ItemScript>();
            if (!item)
            {
                Debug.LogWarning("[NPCDialogueStarter] No ItemScript found in scene (customer path).");
            }
            else
            {
                dlg = item.dialogueSequence
                      ?? (item.itemData ? item.itemData.dialogueData : null);

                if (!dlg)
                {
                    Debug.LogWarning("[NPCDialogueStarter] Found ItemScript but no ItemDialogueData.");
                }
            }
        }

        if (!dlg) return;

        // เริ่มบทสนทนา
        mgr.Show(
            dlg,
            onChoice: null,          // ไม่ต้องทำแอ็กชันที่ช้อยส์—เราไปอยู่ใน LineAction แล้ว
            onFinished: () =>
            {
                if (oneShot) locked = true;
            }
        );
    }
}

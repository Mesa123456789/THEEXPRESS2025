using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCDialogueStarter : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("กันดับเบิลคลิก/สแปมคลิก (วินาที)")]
    public float retriggerCooldown = 0.25f;
    private float lastTriggerTime = -999f;

    public GameObject ui; // Hint/ป้าย “กดคุย” (จะถูกลบเมื่อกดครั้งแรก)

    [Header("Optional Override")]
    [Tooltip("ถ้าตั้งไว้ จะใช้ไดอะล็อกนี้แทนทุกกรณี (ทั้งตำรวจ/ลูกค้า)")]
    public ItemDialogueData overrideDialogue;

    void OnMouseDown()
    {
        if (!CanTriggerNow()) return;

        lastTriggerTime = Time.time;
        if (ui) Destroy(ui);
        TryStartDialogue();
    }

    bool CanTriggerNow()
    {
        // กันสแปมคลิก
        if (Time.time - lastTriggerTime < retriggerCooldown) return false;

        var mgr = ItemDialogueManager.Instance;
        // ถ้าไดอะล็อกกำลังเปิดอยู่ ไม่ให้เริ่มใหม่ (กันซ้อน)
        if (mgr && mgr.panel && mgr.panel.activeSelf) return false;

        return true;
    }

    public void TryStartDialogue()
    {
        var mgr = ItemDialogueManager.Instance;
        if (!mgr)
        {
            Debug.LogWarning("[NPCDialogueStarter] No ItemDialogueManager in scene.");
            return;
        }

        // หา dialogue ตามลำดับความสำคัญ: override → ตำรวจ → ไอเท็มลูกค้า
        ItemDialogueData dlg = overrideDialogue;

        if (!dlg)
        {
            var police = GetComponent<NPCPolice>();
            if (police && police.policeDialogue) dlg = police.policeDialogue;
        }

        if (!dlg)
        {
            var item = FindFirstObjectByType<ItemScript>();
            if (!item)
            {
                Debug.LogWarning("[NPCDialogueStarter] No ItemScript found in scene (customer path).");
                return;
            }

            dlg = item.dialogueSequence ?? (item.itemData ? item.itemData.dialogueData : null);
            if (!dlg)
            {
                Debug.LogWarning("[NPCDialogueStarter] Found ItemScript but no ItemDialogueData.");
                return;
            }
        }

        // แค่เปิดคุย—ไม่ปิดของเดิมทับ (เพราะเราบล็อกไว้แล้ว)
        mgr.Show(dlg,
            onChoice: null,
            onFinished: () =>
            {
                // กันสแปมต่อท้ายทันทีหลังปิด
                lastTriggerTime = Time.time;
            }
        );
    }
}

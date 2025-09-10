using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemDialogueTableTrigger : MonoBehaviour
{
    public string itemTag = "BoxInteract";
    public float retriggerCooldown = 1f;
    float lastTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(itemTag)) return;
        if (Time.time - lastTime < retriggerCooldown) return;

        var item = other.GetComponent<ItemScript>() ?? other.GetComponentInParent<ItemScript>();
        if (!item || item.dialogueSequence == null) return;

        lastTime = Time.time;

        ItemDialogueManager.Instance?.Show(
            item.dialogueSequence,
            onChoice: (choiceIdx) =>
            {
                // ตัวอย่างเงื่อนไข: choice 1 = ทำปกติ, choice 2 = ไล่ลูกค้า + ทำลายของ
                if (choiceIdx == 1) // index เริ่มที่ 0
                {
                    item.ownerNPC?.ForceExitAndClearItem(item.gameObject);
                }
            },
            onFinished: null // ถ้าอยากทำอะไรเมื่อ flow จบทั้งหมด ใส่เพิ่มได้
        );
    }
}

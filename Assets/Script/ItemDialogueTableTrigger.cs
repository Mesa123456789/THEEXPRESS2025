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

        // A) ถ้าไอเทมใช้ ItemDialogueSequence
        var seqHolder = other.GetComponent<ItemScript>() ?? other.GetComponentInParent<ItemScript>();
        if (seqHolder && seqHolder.dialogueSequence != null)
        {
            lastTime = Time.time;
            ItemDialogueSequenceManager.Instance?.Show(
                seqHolder.dialogueSequence,
                (choice) => Debug.Log($"Choice idx = {choice}")
            );
            return;
        }

        // B) (ถ้ายังอยากรองรับเวอร์ชันเก่า item.dialogueData ก็ทำเพิ่มได้ที่นี่)
    }
}

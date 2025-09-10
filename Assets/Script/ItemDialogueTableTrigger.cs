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

        // A) ��������� ItemDialogueSequence
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

        // B) (����ѧ��ҡ�ͧ�Ѻ�����ѹ��� item.dialogueData �������������)
    }
}

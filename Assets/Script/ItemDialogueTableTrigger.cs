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
                // ������ҧ���͹�: choice 1 = �ӻ���, choice 2 = ����١��� + ����¢ͧ
                if (choiceIdx == 1) // index �������� 0
                {
                    item.ownerNPC?.ForceExitAndClearItem(item.gameObject);
                }
            },
            onFinished: null // �����ҡ����������� flow �������� ���������
        );
    }
}

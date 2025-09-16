using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemDialogueTableTrigger : MonoBehaviour
{
    public string itemTag = "BoxInteract";
    public float retriggerCooldown = 1f;
    float lastTime;

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!other.CompareTag(itemTag)) return;
    //    if (Time.time - lastTime < retriggerCooldown) return;

    //    var item = other.GetComponent<ItemScript>() ?? other.GetComponentInParent<ItemScript>();
    //    if (!item || item.dialogueSequence == null) return;

    //    lastTime = Time.time;

    //    ItemDialogueManager.Instance?.Show(
    //        item.dialogueSequence,
    //        onChoice: (choiceIdx) =>
    //        {
    //            if (choiceIdx == 1)
    //            {
    //                item.ownerNPC?.ForceExitAndClearItem(item.gameObject);
    //            }
    //        },
    //        onFinished: null 
    //    );
    //}
}

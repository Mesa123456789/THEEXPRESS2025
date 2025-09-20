using UnityEngine;

public class NPCPolice : NPC
{
    [Header("Police")]
    public ItemDialogueData policeDialogue;

    protected override void SpawnPackageAndWait()
    {

        if (npcBoxcollider && Vector3.Distance(transform.position, npcBoxcollider.transform.position) > reachThreshold)
        {

            state = State.Entering;
            return;
        }

        state = GetStateWaiting();

    }


    void OpenPoliceDialogue()
    {
        if (!itemDialogueManager || policeDialogue == null) return;

        itemDialogueManager.Show(
            actorOwner: gameObject,        // <— NPC ตัวนี้
            flow: policeDialogue,
            onChoice: (idx) =>
            {
                if (idx == 0)
                {
                    GameManager.Instance?.SpendMoney(500);
                    ForceExitAndClearItem(null);
                }
                else if (idx == 1)
                {
                    GameManager.Instance?.KillPlayerNow();
                }
            },
            onFinished: null
        );
    }

}

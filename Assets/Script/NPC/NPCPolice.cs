using UnityEngine;

public class NPCPolice : NPC
{
    [Header("Police")]
    public ItemDialogueData policeDialogue;

    protected override void SpawnPackageAndWait()
    {

        if (npcBoxcollider && Vector3.Distance(transform.position, npcBoxcollider.transform.position) > reachThreshold)
        {
            // เผื่อพลาด: บังคับให้ไปหาโต๊ะก่อน
            state = State.Entering;
            return;
        }

        state = GetStateWaiting();
        //OpenPoliceDialogue();
    }


    void OpenPoliceDialogue()
    {
        if (!itemDialogueManager || policeDialogue == null) return;

        itemDialogueManager.Show(
            policeDialogue,
            onChoice: (idx) =>
            {
                // 0 = จ่าย, 1 = ไม่จ่าย (ตามที่คุณตั้งใน SO)
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

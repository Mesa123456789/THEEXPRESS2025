using UnityEngine;

public class DialogueForgetOnDestroy : MonoBehaviour
{
    private int id;

    void Awake()
    {
        id = GetInstanceID();
    }

    void OnDestroy()
    {
        if (ItemDialogueManager.Instance != null)
            ItemDialogueManager.Instance.ForgetActor(id);
    }
}

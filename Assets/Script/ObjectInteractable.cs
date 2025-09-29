using UnityEngine;

public class ObjectInteractable : MonoBehaviour, IInteractable
{
    [Header("Outline Target")]
    public Component outlineTarget;

    [Header("Test Action")]
    public string interactMessage = "You interacted with object!";

    public void Interact()
    {
        Debug.Log(interactMessage);
    }

    public Component GetOutlineTarget()
    {
        return outlineTarget ? outlineTarget : this;
    }
}

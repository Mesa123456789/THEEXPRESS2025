using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Raycast")]
    public Transform rayOrigin;          
    public float rayDistance = 3f;
    public LayerMask interactMask = ~0; 

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;
    [Header("Debug")]
    public bool drawRay = true;

    void Reset()
    {
        if (rayOrigin == null) rayOrigin = Camera.main ? Camera.main.transform : transform;
    }

    void Update()
    {
        if (rayOrigin == null) return;

        if (drawRay)
        {
            Debug.DrawRay(rayOrigin.position, rayOrigin.forward * rayDistance, Color.cyan);
        }

        if (Input.GetKeyDown(interactKey))
        {
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, rayDistance, interactMask, QueryTriggerInteraction.Collide))
            {

                if (hit.collider.CompareTag("Door"))
                {
                    if (hit.collider.GetComponent<Door>() is Door door)
                    {
                        door.Toggle();
                    }
                    else if (hit.collider.GetComponentInParent<Door>() is Door parentDoor) 
                    {
                        parentDoor.Toggle();
                    }
                }
            }
        }
        if (Input.GetMouseButtonDown(0)
            && Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit Mhit, rayDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            if (Mhit.collider.CompareTag("CallButton"))
            {
                // เรียกคิวแรกเข้าหน้าโต๊ะ
                NPCSpawner.Instance?.CallNext();
            }
        }

    }
}

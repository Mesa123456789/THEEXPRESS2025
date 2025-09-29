using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Raycast")]
    public Transform rayOrigin;          // ตั้งเป็นกล้องผู้เล่นหรือจุดกลางตัวละคร
    public float rayDistance = 3f;
    public LayerMask interactMask = ~0;  // เลือกเลเยอร์ที่ยิงชนได้ (หรือ ~0 คือทั้งหมด)

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
                // ต้องชนวัตถุที่ tag == "Door"
                if (hit.collider.CompareTag("Door"))
                {
                    // หา Door component แล้ว Toggle
                    if (hit.collider.GetComponent<Door>() is Door door)
                    {
                        door.Toggle();
                    }
                    else if (hit.collider.GetComponentInParent<Door>() is Door parentDoor) // เผื่อ collider อยู่ลูก
                    {
                        parentDoor.Toggle();
                    }
                }
            }
        }
    }
}

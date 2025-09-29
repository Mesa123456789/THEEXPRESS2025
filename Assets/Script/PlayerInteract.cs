using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Raycast")]
    public Transform rayOrigin;          // ����繡��ͧ���������ͨش��ҧ����Ф�
    public float rayDistance = 3f;
    public LayerMask interactMask = ~0;  // ���͡����������ԧ���� (���� ~0 ��ͷ�����)

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
                // ��ͧ���ѵ�ط�� tag == "Door"
                if (hit.collider.CompareTag("Door"))
                {
                    // �� Door component ���� Toggle
                    if (hit.collider.GetComponent<Door>() is Door door)
                    {
                        door.Toggle();
                    }
                    else if (hit.collider.GetComponentInParent<Door>() is Door parentDoor) // ���� collider �����١
                    {
                        parentDoor.Toggle();
                    }
                }
            }
        }
    }
}

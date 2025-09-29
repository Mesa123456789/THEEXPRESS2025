using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCam;

    [Header("Raycast")]
    public float maxDistance = 3f;
    public LayerMask hitMask = ~0; // ทั้งหมด หรือปรับเป็น Default/Interactable

    private IInteractable _currentInteractable;
    private OutlineHighlighter _currentOutline;

    void Update()
    {
        UpdateRaycast();
        if (Input.GetKeyDown(KeyCode.E))
        {
            _currentInteractable?.Interact();
        }
    }

    void UpdateRaycast()
    {
        if (!playerCam) return;

        var ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out var hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            // หา IInteractable จากวัตถุที่โดน (รวมถึง parent)
            var interact = hit.collider.GetComponentInParent<IInteractable>();
            if (interact != null)
            {
                // เจาะจงว่าจะทำ outline ที่ตัวไหน (อาจเป็น root/child)
                var outlineTarget = interact.GetOutlineTarget();
                var outline = outlineTarget ? outlineTarget.GetComponent<OutlineHighlighter>() : null;

                // เปลี่ยนเป้าหมาย?
                if (!ReferenceEquals(interact, _currentInteractable))
                {
                    ClearCurrentOutline();
                    _currentInteractable = interact;
                    _currentOutline = outline;
                    if (_currentOutline) _currentOutline.SetVisible(true);
                }
                else
                {
                    // เป้าเดิม—ถ้าไม่มี outline แต่เพิ่งติดเพิ่มตอน runtime
                    if (_currentOutline == null && outline != null)
                    {
                        _currentOutline = outline;
                        _currentOutline.SetVisible(true);
                    }
                }
                return;
            }
        }

        // ไม่โดน หรือไม่ใช่ IInteractable
        ClearCurrentOutline();
    }

    void ClearCurrentOutline()
    {
        if (_currentOutline) _currentOutline.SetVisible(false);
        _currentOutline = null;
        _currentInteractable = null;
    }
}

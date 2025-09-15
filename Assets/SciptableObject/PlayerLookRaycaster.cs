using UnityEngine;

public class PlayerRaycaster : MonoBehaviour
{
    public Camera rayCam;
    public float range = 5f;
    public LayerMask mask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    HoverUIOnLook current;

    void Awake()
    {
        if (!rayCam) rayCam = Camera.main;
    }

    void Update()
    {
        Ray ray = new Ray(rayCam.transform.position, rayCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask, triggerInteraction))
        {
            var hover = hit.collider.GetComponentInParent<HoverUIOnLook>();
            if (hover != null)
            {
                if (hover != current)
                {
                    current?.HideUI();
                    current = hover;
                    current.ShowUI();
                }
                return;
            }
        }

        if (current != null)
        {
            current.HideUI();
            current = null;
        }
    }
}

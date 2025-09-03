using UnityEngine;

public class TapeDragScaler : MonoBehaviour
{
    public Transform tapeStart;
    public Transform tapeEnd;
    public GameObject tapeObject;
    public float dragTolerance = 0.2f;
    public float startDragThreshold = 0.12f;

    private bool isDragging = false;
    private bool tapeVisible = false;
    private float lastLength = 0f;

    private Vector3 dragStartPoint;
    [SerializeField] private TapeDispenser selectedDispenser = null;

    public bool isTapeDone;


    BoxScript currentBox;

    void Start()
    {
        tapeObject.SetActive(false);
        SetTapeScale(0f);
        currentBox = FindAnyObjectByType<BoxScript>();
    }

    void Update()
    {
        //Debug.Log("IsFinsihedClose: " + BoxScript.IsFinsihedClose);
        if (!currentBox.IsFinsihedClose) return;

        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f))
            {
                //Debug.Log("Raycast โดนวัตถุชื่อ: " + hit.collider.gameObject.name);
                var dispenser = hit.collider.GetComponent<TapeDispenser>();
                if (dispenser != null)
                {
                    selectedDispenser = dispenser;
                    //Debug.Log("เลือก Dispenser สำเร็จ");
                    return;
                }
            }
            
        }

        if (selectedDispenser == null)
        return;


        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);
            Vector3 tapeTip = tapeStart.position + (tapeEnd.position - tapeStart.position).normalized * lastLength;

            if (Vector3.Distance(mouseWorld, tapeTip) < dragTolerance)
            {
                isDragging = true;
                tapeVisible = false;
                dragStartPoint = mouseWorld;
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);
            Vector3 guideVec = tapeEnd.position - tapeStart.position;
            float guideLen = guideVec.magnitude;
            Vector3 tapeTip = tapeStart.position + guideVec.normalized * lastLength;
            Vector3 fromTipToMouse = mouseWorld - tapeTip;
            float dragDist = Vector3.Dot(fromTipToMouse, guideVec.normalized);

            if (!tapeVisible && dragDist > startDragThreshold)
            {
                tapeObject.SetActive(true);
                tapeVisible = true;

                if (selectedDispenser != null)
                {
                    Material mat = selectedDispenser.GetMaterial();
                    var renderer = tapeObject.GetComponentInChildren<Renderer>();
                    //Debug.Log("เปลี่ยน Material เป็น: " + (mat != null ? mat.name : "null") + ", Renderer: " + (renderer != null ? renderer.name : "null"));
                    if (renderer != null && mat != null)
                        renderer.material = mat;
                }
            }

            if (tapeVisible)
            {
                Vector3 fromStartToMouse = mouseWorld - tapeStart.position;
                float dot = Vector3.Dot(fromStartToMouse, guideVec.normalized);
                float newLength = Mathf.Clamp(dot, 0f, guideLen);
                newLength = Mathf.Max(newLength, lastLength);
                SetTapeScale(newLength);
            }
        }
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (tapeVisible)
            {
                lastLength = tapeObject.transform.localScale.z;
            }
            isDragging = false;
            tapeVisible = false;
            if (lastLength == 0f) tapeObject.SetActive(false);

            if (lastLength > 0f)
            {
                isTapeDone = true; // แจ้งว่าเทปเสร็จ
            }
        }
    }

    void SetTapeScale(float length)
    {
        tapeObject.transform.position = tapeStart.position;
        tapeObject.transform.LookAt(tapeEnd.position);
        Vector3 scale = tapeObject.transform.localScale;
        scale.z = length;
        tapeObject.transform.localScale = scale;
    }

    Vector3 GetMouseWorldPositionAtY(float yLevel)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, yLevel, 0));
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return tapeStart.position;
    }
}
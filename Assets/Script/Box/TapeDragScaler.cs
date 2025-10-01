using UnityEngine;

public class TapeDragScaler : MonoBehaviour
{
    public Transform tapeStart;
    public Transform tapeEnd;
    public GameObject tapeObject;

    [Header("Drag")]
    public float dragTolerance = 0.2f;
    public float startDragThreshold = 0.12f;

    [Header("Pivot")]
    public bool pivotAtCenter = false;

    private bool isDragging = false;
    private bool tapeVisible = false;


    private float lastWorldLength = 0f;
    private float currentWorldLength = 0f;

    private Vector3 dragStartPoint;
    [SerializeField] private TapeDispenser selectedDispenser = null;

    public bool isTapeDone;


    private Vector3 baseLocalScale;
    private Transform parentForScale;

    BoxScript currentBox;

    public GameObject cube;

    void Start()
    {
        if (!tapeObject) { enabled = false; return; }

        baseLocalScale = tapeObject.transform.localScale;
        parentForScale = tapeObject.transform.parent;

        tapeObject.SetActive(false);
        SetTapeScaleWorld(0f);

        currentBox = FindAnyObjectByType<BoxScript>();
    }

    void Update()
    {
        if(!currentBox.IsFinsihedClose) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                var dispenser = hit.collider.GetComponent<TapeDispenser>();
                if (dispenser != null)
                {
                    selectedDispenser = dispenser;
                    cube.SetActive(true);

                }
            }
        }
        if (!currentBox || !currentBox.IsFinsihedClose && selectedDispenser == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);

            Vector3 guideDir = (tapeEnd.position - tapeStart.position).normalized;
            Vector3 tip = tapeStart.position + guideDir * lastWorldLength;

            if (Vector3.Distance(mouseWorld, tip) < dragTolerance)
            {
                isDragging = true;
                tapeVisible = false;
                dragStartPoint = mouseWorld;
            }
        }

        // ระหว่างลาก
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);

            Vector3 guideVec = (tapeEnd.position - tapeStart.position);
            float guideLen = guideVec.magnitude;
            Vector3 guideDir = guideVec.normalized;

            Vector3 tip = tapeStart.position + guideDir * lastWorldLength;
            float dragDist = Vector3.Dot((mouseWorld - tip), guideDir);

            if (!tapeVisible && dragDist > startDragThreshold)
            {
                tapeObject.SetActive(true);
                tapeVisible = true;

                // ใส่วัสดุจาก dispenser
                if (selectedDispenser != null)
                {
                    var mat = selectedDispenser.GetMaterial();
                    var r = tapeObject.GetComponentInChildren<Renderer>();
                    if (r && mat) r.material = mat;
                }
            }

            if (tapeVisible)
            {
                // คำนวณ “ความยาวโลกจริง” ใหม่
                float projected = Vector3.Dot((mouseWorld - tapeStart.position), guideDir);
                float newLen = Mathf.Clamp(projected, 0f, guideLen);
                newLen = Mathf.Max(newLen, lastWorldLength); // ลากต่อจากปลายเดิม

                SetTapeScaleWorld(newLen);
            }
        }

        // ปล่อยเมาส์
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (tapeVisible)
            {
                lastWorldLength = currentWorldLength; // << เก็บเป็น “ความยาวโลกจริง”
            }
            isDragging = false;
            tapeVisible = false;

            if (lastWorldLength == 0f) tapeObject.SetActive(false);
            if (lastWorldLength > 0f)
            {
                isTapeDone = true;
                GameObject.Destroy(cube);
            }
        }
    }

    /// <summary>
    /// เซ็ตความยาวเทปด้วยหน่วย "โลกจริง" และคงความหนา/ความกว้างตาม baseLocalScale
    /// </summary>
    void SetTapeScaleWorld(float worldLength)
    {
        currentWorldLength = worldLength;

        Vector3 dir = (tapeEnd.position - tapeStart.position).normalized;

        // หมุนให้แกน +X ของเทปชี้ไปทางปลาย
        tapeObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);

        // แปลง worldLength -> localScale.x โดยชดเชยสเกลของพาเรนต์
        float parentX = (parentForScale != null) ? parentForScale.lossyScale.x : 1f;
        float localX = worldLength / Mathf.Max(0.0001f, parentX);

        // ล็อค Y/Z ให้เท่ากับสเกลตั้งต้นเสมอ (กันความหนา/กว้างเพี้ยน)
        Vector3 s = baseLocalScale;
        s.x = localX;
        tapeObject.transform.localScale = s;

        // วางตำแหน่ง: pivot ที่ปลายเริ่ม หรือกึ่งกลาง
        if (pivotAtCenter)
            tapeObject.transform.position = tapeStart.position + dir * (worldLength * 0.5f);
        else
            tapeObject.transform.position = tapeStart.position;
    }

    Vector3 GetMouseWorldPositionAtY(float yLevel)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, yLevel, 0));
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return tapeStart.position;
    }
}

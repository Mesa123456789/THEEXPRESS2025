using UnityEngine;

public class BoxScript : MonoBehaviour
{
    [Header("Box Settings")]
    public bool hasItem = false; 
    public int Closing = 0;

    [Header("Lids")]
    public SmoothLidClose leftLid;
    public SmoothLidClose rightLid;

    [Header("Tape")]
    public TapeDragScaler Tape;

    [Header("Bubble Check")]
    public bool bubbleInserted = false;
    public int bubbleCount = 0;
    public GameObject bubble;

    public bool IsFinsihedClose = false;

    BoxSpawner boxSpawner;
    Rigidbody rb;

    public bool PastedLabel = false;
    

    void Start()
    {
        boxSpawner = FindFirstObjectByType<BoxSpawner>();
        rb = GetComponent<Rigidbody>();
        bubble.SetActive(false);
        rb.isKinematic = true;
        rb.useGravity = false;
        PastedLabel = false;


    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pickable"))
        {
            hasItem = true;

        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("pickable") && !IsPickableStillInside())
            hasItem = false;
    }

    private bool IsPickableStillInside()
    {
        Collider[] contents = Physics.OverlapBox(transform.position, transform.localScale / 2, Quaternion.identity);
        foreach (Collider col in contents)
            if (col.CompareTag("pickable")) return true;

        return false;
    }

    public void AddBubble()
    {
        bubble.SetActive (true);
        if (bubbleCount >= 3) return;

        bubbleCount++;
        Debug.Log($"Bubble inserted {bubbleCount} times");

        Vector3 scale = bubble.transform.localScale;
        scale.y += 0.08f;
        bubble.transform.localScale = scale;

        if (bubbleCount >= 3)
        {
            bubbleInserted = true;
            Debug.Log("พร้อมปิดกล่องได้แล้ว!");
        }
    }
    private bool boxCleared = false;

    private void Update()
    {
        if (!hasItem) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsFinsihedClose) return;

            if (!bubbleInserted)
            {
                Debug.Log("ต้องกดใส่บับเบิ้ลให้ครบ 3 ครั้งก่อนปิดกล่อง!");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("Boxlid"))
            {
                SmoothLidClose lid = hit.collider.GetComponent<SmoothLidClose>();
                if (lid != null)
                {
                    lid.CloseLid();
                    Closing += 1;
                }
            }
        }

        if (leftLid != null && rightLid != null && leftLid.isClosed && rightLid.isClosed)
        {
            IsFinsihedClose = true;
        }
        if (Tape != null && Tape.isTapeDone && PastedLabel && !boxCleared)
        {
            boxCleared = true;
            Collider[] items = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            foreach (Collider item in items)
            {
                if (item.CompareTag("pickable"))
                    Destroy(item.gameObject);
                Debug.Log("ของข้างในถูกลบ");
            }

            gameObject.tag = "BoxInteract";
            rb.isKinematic = false;
            rb.useGravity = true;
            boxSpawner.hasSpawnedBox = false;
            Tape.isTapeDone = false;
            int moneyEarned = Random.Range(150, 301);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddSales(moneyEarned);
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null");
            }

            Debug.Log("กล่องเสร็จ!หยิบได้");
        }

    }

}

using System;
using UnityEngine;
using static UnityEditor.Progress;

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

    [Header("GameManager")]
    public GameManager gameManager;  

    [Header("Bubble Check")]
    public bool bubbleInserted = false;
    public int bubbleCount = 0;
    public GameObject bubble;
    public int BoxPrice = 0;
    public bool IsFinsihedClose = false;

    BoxSpawner boxSpawner;
    Rigidbody rb;

    public bool PastedLabel = false;
    private bool boxCleared = false;

    ItemScript itemScript;
    public static event Action OnBoxStored;


    void Start()
    {
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>(); 

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
            hasItem = true;
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
        bubble.SetActive(true);
        if (bubbleCount >= 3) return;

        bubbleCount++;
        Vector3 scale = bubble.transform.localScale;
        scale.y += 0.07f;
        bubble.transform.localScale = scale;

        if (bubbleCount >= 3)
            bubbleInserted = true;
    }
    public void StoreBox()
    {
        ItemScript itemScript = FindFirstObjectByType<ItemScript>();
        BoxPrice = itemScript.itemData.caughtPercent;
        int moneyEarned = itemScript.itemData.price;
        int risk = itemScript.itemData.caughtPercent;
        gameManager.AddSales(moneyEarned, risk);
        if (gameManager.totalCaughtPercent >= 100f)
        {
            Debug.Log("โดนจับ! Game Over");
        }
    }
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
            if (Physics.Raycast(ray, out var hit, 3f) && hit.collider.CompareTag("Boxlid"))
            {
                var lid = hit.collider.GetComponent<SmoothLidClose>();
                if (lid != null)
                {
                    lid.CloseLid();
                    Closing += 1;
                }
            }
        }

        if (leftLid && rightLid && leftLid.isClosed && rightLid.isClosed)
            IsFinsihedClose = true;

        if (Tape && Tape.isTapeDone && PastedLabel && !boxCleared)
        {
            boxCleared = true;

            Collider[] items = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            foreach (Collider item in items)
                if (item.CompareTag("pickable"))
                    Destroy(item.gameObject);

            gameObject.tag = "BoxInteract";
            rb.isKinematic = false;
            rb.useGravity = true;
            if (boxSpawner) boxSpawner.hasSpawnedBox = false;
            Tape.isTapeDone = false;
            StoreBox();
            OnBoxStored?.Invoke();
            Debug.Log("กล่องเสร็จ!หยิบได้");
        }
    }
}

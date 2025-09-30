using System;
using System.Buffers.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;


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

    [Header("Bubble UI/Visual")]
    public GameObject bubble;            // ตัววัตถุที่จะ scale (เช่นกองฟองในกล่อง)

    [Header("Bubble Logic")]
    public int bubbleCount = 0;          // นับรอบที่เพิ่มแล้ว
    public int maxBubble = 3;            // เพิ่มได้สูงสุด 3 รอบ
    public float stepY = 0.001f;           // ต่อคลิกเพิ่ม Y เท่าไร
    public float scaleDuration = 0.25f;  // เวลาที่ใช้ในการ scale ต่อคลิก
    public bool bubbleInserted = false;

    private Coroutine scaleCo;
    private float baseY;                 // ค่า y เดิมก่อนเริ่มเพิ่ม

    public bool illegal;
    public int price;
    public bool IsFinsihedClose = false;
    public int risk;

    BoxSpawner boxSpawner;
    Rigidbody rb;

    public bool PastedLabel = false;
    private bool boxCleared = false;

    ItemScript itemScript;
    public static event Action OnBoxStored;


    void Start()
    {
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
        itemScript = FindFirstObjectByType<ItemScript>();
        boxSpawner = FindFirstObjectByType<BoxSpawner>();
        rb = GetComponent<Rigidbody>();
        bubble.SetActive(false);
        rb.isKinematic = true;
        rb.useGravity = false;
        PastedLabel = false;

        if (bubble != null)
        {
            baseY = bubble.transform.localScale.y;
            bubble.SetActive(false);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pickable"))
            hasItem = true;
        illegal = itemScript.itemData.illegal;
        price = itemScript.itemData.price;
        risk = itemScript.itemData.caughtPercent;
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
        if (!hasItem) return;


        if (!bubble.activeSelf) bubble.SetActive(true);

        if (bubbleCount >= maxBubble) return; // เต็มแล้ว

        bubbleCount++;

        // คำนวณเป้าหมาย Y (base + step * count)
        var s = bubble.transform.localScale;
        float targetY = baseY + stepY * bubbleCount;
        Vector3 target = new Vector3(s.x, targetY, s.z);

        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleTo(target, scaleDuration));

        if (bubbleCount >= maxBubble)
            bubbleInserted = true;
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = bubble.transform.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            bubble.transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        bubble.transform.localScale = target;
        scaleCo = null;
    }
    public void StoreBox()
    {
       
        gameManager.AddSales(price, risk);
        AddSalesPopupUI.ShowNotice(price);

        OnBoxStored?.Invoke();
    }

    void HidePickable(GameObject obj)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (var c in obj.GetComponentsInChildren<Collider>())
            c.enabled = false;
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
            else
            {
                Collider[] items = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
                foreach (Collider item in items)
                    if (item.CompareTag("pickable"))
                    {
                        HidePickable(item.gameObject); 
                    }
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
        {
            IsFinsihedClose = true;
        }

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
            //StoreBox();
        }
    }
}

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
    bool DoOnce = false;
    ItemScript itemScript;
    public static event Action OnBoxStored;

    public TutorialSlideUIQueue TutorialSlideUIQueue;
    // --- Tutorial step flags ---
    bool step4Closed = false;
    bool step5Shown = false, step5Closed = false;
    bool step6Shown = false, step6Closed = false;
    bool step7Shown = false, step7Closed = false;
    bool step8Shown = false, step8Closed = false;
    bool step9Shown = false, step9Closed = false;
    bool step10Shown = false, step10Closed = false;
    bool step11Shown = false, step11Closed = false;


    void Start()
    {
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
        itemScript = FindFirstObjectByType<ItemScript>();
        boxSpawner = FindFirstObjectByType<BoxSpawner>();
        TutorialSlideUIQueue = FindFirstObjectByType<TutorialSlideUIQueue>();
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

        // ปิด step 4 แล้วเปิด 5 (ครั้งเดียว)
        StartCoroutine(Close4Open5());
    }

    IEnumerator Close4Open5()
    {
        if (!step4Closed)
        {
            TutorialSlideUIQueue.CompleteCurrentByIndex(4);
            step4Closed = true;
            yield return new WaitForSeconds(0.3f);
        }
        if (!step5Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(5);
            step5Shown = true;
        }
    }

    IEnumerator AfterSpawnOpenStep5()
    {
        // ให้เวลาระบบ UI ปิด step 4 (มาจาก Table)
        yield return new WaitForSeconds(0.3f);
        TutorialSlideUIQueue.CompleteCurrentByIndex(4);
        yield return new WaitForSeconds(0.3f);
        TutorialSlideUIQueue.EnqueueTutorialByIndex(5);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pickable"))
        {
            hasItem = true;

            if (step5Shown && !step5Closed)      // กันซ้ำ
                StartCoroutine(Close5Open6());
        }

        illegal = itemScript.itemData.illegal;
        price = itemScript.itemData.price;
        risk = itemScript.itemData.caughtPercent;

        Table table = FindFirstObjectByType<Table>();
        Destroy(table);
    }

    IEnumerator Close5Open6()
    {
        TutorialSlideUIQueue.CompleteCurrentByIndex(5);
        step5Closed = true;
        yield return new WaitForSeconds(0.3f);

        if (!step6Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(6);
            step6Shown = true;
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
        if (!hasItem) return;
        if (!bubble.activeSelf) bubble.SetActive(true);
        if (bubbleCount >= maxBubble) return;

        bubbleCount++;

        var s = bubble.transform.localScale;
        float targetY = baseY + stepY * bubbleCount;
        Vector3 target = new Vector3(s.x, targetY, s.z);

        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleTo(target, scaleDuration));

        if (bubbleCount >= maxBubble)
        {
            bubbleInserted = true;  // << แค่นี้พอ
        }
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

        // คลิกปิดฝากล่อง
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsFinsihedClose && bubbleInserted)
            {
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
        }

        // --- ใส่บับเบิลครบ → ปิด 6 เปิด 7 (ครั้งเดียว) ---
        if (bubbleInserted && step6Shown && !step6Closed)
        {
            StartCoroutine(Close6Open7AndHideItems());
        }

        // --- ปิดฝาซ้าย+ขวา → ปิด 7 เปิด 8 (ครั้งเดียว) ---
        if (leftLid && rightLid && leftLid.isClosed && rightLid.isClosed && !step7Closed)
        {
            IsFinsihedClose = true;
            StartCoroutine(Close7Open8());
        }

        // --- เทปเสร็จ → ปิด 8 เปิด 9 (ครั้งเดียว) ---
        if (Tape && Tape.isTapeDone && !step8Closed)
        {
            StartCoroutine(Close8Open9());
        }

        // --- ได้กล่องครบ (เทป+ฉลาก) → ปิด 9 เปิด 10 (ครั้งเดียว) ---
        if (Tape && Tape.isTapeDone && PastedLabel && !boxCleared)
        {
            boxCleared = true;
            StartCoroutine(Close9Open10());

            //// โลจิกเดิมของคุณ
            //Collider[] items = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            //foreach (Collider item in items)
            //    if (item.CompareTag("pickable"))
            //        Destroy(item.gameObject);

            gameObject.tag = "BoxInteract";
            rb.isKinematic = false;
            rb.useGravity = true;
            if (boxSpawner) boxSpawner.hasSpawnedBox = false;
            Tape.isTapeDone = false;
            //StartCoroutine(Close10Open11());
        }
    }

    IEnumerator Close6Open7AndHideItems()
    {
        TutorialSlideUIQueue.CompleteCurrentByIndex(6);
        step6Closed = true;
        yield return new WaitForSeconds(0.3f);

        if (!step7Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(7);
            step7Shown = true;
        }

        // ซ่อนของในกล่อง
        Collider[] items = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
        foreach (Collider item in items)
            if (item.CompareTag("pickable"))
                HidePickable(item.gameObject);
    }

    IEnumerator Close7Open8()
    {
        TutorialSlideUIQueue.CompleteCurrentByIndex(7);
        step7Closed = true;
        yield return new WaitForSeconds(0.3f);

        if (!step8Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(8);
            step8Shown = true;
        }
    }

    IEnumerator Close8Open9()
    {
        TutorialSlideUIQueue.CompleteCurrentByIndex(8);
        step8Closed = true;
        yield return new WaitForSeconds(0.3f);

        if (!step9Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(9);
            step9Shown = true;
        }
    }

    IEnumerator Close9Open10()
    {
        TutorialSlideUIQueue.CompleteCurrentByIndex(9);
        step9Closed = true;
        yield return new WaitForSeconds(0.3f);

        if (!step10Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(10);
            step10Shown = true;
        }
    }
    IEnumerator Close10Open11()
    {
        TutorialSlideUIQueue.CompleteCurrentByIndex(10);
        step10Closed = true;
        yield return new WaitForSeconds(0.3f);

        if (!step11Shown)
        {
            TutorialSlideUIQueue.EnqueueTutorialByIndex(11);
            step11Shown = true;
        }
    }

}

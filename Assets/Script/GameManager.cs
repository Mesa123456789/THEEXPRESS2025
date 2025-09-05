using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ========= Singleton =========
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // กันมีหลายตัว
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // แยกเป็น root object (กันโดนลบเพราะพาเรนต์ถูก unload)
        if (transform.parent != null)
            transform.SetParent(null, worldPositionStays: true);

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ฟังเหตุการณ์เปลี่ยนซีน เพื่อ re-bind UI ถ้าจำเป็น
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ซีนใหม่อาจมี UI คนละชุด — ถ้าไม่ได้โยงใน Inspector ให้ลองหาแบบอ่อน ๆ
        AutoBindUIIfMissing();
        // อัปเดต UI ให้ตรงค่า ณ ปัจจุบัน
        UpdateDayUI();
        UpdateTimeUI();
        UpdateSalesUI();
        UpdateDangerUI();
    }

    // ========= Day / Sales =========
    public int currentDay = 1;
    public int salesGoal = 500;   // เป้าขายต่อวัน
    public int currentSales = 0;  // ยอดขาย "วันนี้"

    // ========= Clock =========
    [Header("Clock Settings")]
    [Tooltip("ชั่วโมงเริ่มต้นของรอบ (24h) เช่น 15 = 15:00")]
    public int startHour = 15;          // 15:00
    [Tooltip("วินาทีจริงต่อ 1 ชั่วโมงในเกม")]
    public float hourDuration = 10f;    // 10 วิ = 1 ชม.

    [Header("Danger & Sleep")]
    [Tooltip("เริ่มช่วงอันตราย (รวมชั่วโมงนี้)")]
    public int dangerStartHour = 3;     // 03:00
    [Tooltip("เวลาจบรอบ (ถึงชั่วโมงนี้แล้วจะจบวัน/ตายถ้าไม่ได้นอน)")]
    public int dayEndHour = 6;          // 06:00
    public string deathSceneName = "CutScene_Die";

    // เวลาภายใน
    private float hourTimer = 0f;
    private int currentHour;                 // 0–23
    private int elapsedHoursThisDay = 0;     // นับชั่วโมงที่ผ่านในรอบปัจจุบัน
    private int runtimeDayLength = 0;        // คำนวณจาก startHour → dayEndHour
    private bool sleptThisCycle = false;     // วันนี้ได้นอนหรือยัง
    private bool isEnding = false;           // กันโหลดซีนซ้ำ

    // ========= UI =========
    [Header("UI (assign per scene or auto-bind)")]
    public TMP_Text timeText;
    public TMP_Text salesText;
    public TMP_Text goalText;
    public TMP_Text DayText;

    [Header("Danger UI")]
    public TMP_Text dangerText;              // เช่น "Danger time! Go to bed."
    public string dangerMessage = "Danger time! Go to bed.";

    // อยู่ช่วงอันตรายหรือไม่ (เช็คแบบวง 24 ชม.)
    public bool IsDangerTime => IsHourInRange(currentHour, dangerStartHour, dayEndHour);

    void Start()
    {
        StartNewDay();
    }

    void Update()
    {
        if (isEnding) return;

        hourTimer += Time.deltaTime;

        // รองรับเฟรมตก
        while (hourTimer >= hourDuration && !isEnding)
        {
            hourTimer -= hourDuration;
            AdvanceHour();

            // เพิ่งรีเซ็ตวัน -> ออกจากลูปเฟรมนี้
            if (elapsedHoursThisDay == 0) break;
        }
    }

    // เดินเวลาไป 1 ชั่วโมง
    void AdvanceHour()
    {
        currentHour = (currentHour + 1) % 24;
        elapsedHoursThisDay++;
        UpdateTimeUI();
        UpdateDangerUI();

        // จบรอบเมื่อครบความยาวที่คำนวณ หรือชั่วโมงถึง dayEndHour ตรง ๆ
        if (elapsedHoursThisDay >= runtimeDayLength || currentHour == (dayEndHour % 24))
        {
            EndDay();
        }
    }

    // ========= Day Control =========
    void StartNewDay()
    {
        currentHour = startHour % 24;  // 15:00
        elapsedHoursThisDay = 0;
        hourTimer = 0f;
        sleptThisCycle = false;
        isEnding = false;

        // คำนวณความยาววันจาก startHour → dayEndHour (วน 24 ชม.)
        runtimeDayLength = (dayEndHour - startHour + 24) % 24;
        if (runtimeDayLength <= 0) runtimeDayLength = 24;

        // ตั้งเป้า/รีเซ็ตยอดขายวันนี้
        if (salesGoal <= 0) salesGoal = 500;
        currentSales = 0;

        UpdateDayUI();
        UpdateTimeUI();
        UpdateSalesUI();
        UpdateDangerUI();
    }

    void EndDay()
    {
        if (isEnding) return;
        isEnding = true;

        // ไม่ได้นอน -> ไปซีนตาย
        if (!sleptThisCycle)
        {
            SceneManager.LoadScene(deathSceneName);
            return;
        }

        // นอนแล้ว -> วันถัดไป
        currentDay++;
        StartNewDay();
    }

    /// <summary>เรียกเมื่อผู้เล่นเลือกนอน (ผ่าน UI เตียง)</summary>
    public void SleepNow()
    {
        if (isEnding) return;

        sleptThisCycle = true;
        currentDay++;
        StartNewDay(); // ข้ามไป 15:00 ของวันใหม่ทันที
    }

    // ========= Public API =========
    public void AddSales(int amount)
    {
        currentSales += amount;
        UpdateSalesUI();
    }

    // ========= UI Update =========
    void UpdateSalesUI()
    {
        if (salesText) salesText.text = $"Sales: {currentSales}";
        if (goalText) goalText.text = $"Goal: {salesGoal}";
    }

    void UpdateTimeUI()
    {
        if (timeText) timeText.text = $"{currentHour:00}:00";
    }

    void UpdateDayUI()
    {
        if (DayText) DayText.text = $"Day {currentDay}";
    }

    void UpdateDangerUI()
    {
        if (!dangerText) return;

        if (IsDangerTime)
        {
            dangerText.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(dangerMessage))
                dangerText.text = dangerMessage;
        }
        else
        {
            dangerText.gameObject.SetActive(false);
        }
    }

    // ========= Helpers =========
    // เช็คว่าชั่วโมง h อยู่ในช่วง [start, end) แบบวน 24 ชม.
    bool IsHourInRange(int h, int start, int end)
    {
        h = (h + 24) % 24;
        start = (start + 24) % 24;
        end = (end + 24) % 24;

        if (start == end) return true;          // ทั้งวัน
        if (start < end) return h >= start && h < end;
        return h >= start || h < end;           // ช่วงคร่อมเที่ยงคืน
    }

    // หา UI อัตโนมัติแบบอ่อน ๆ (เผื่อซีนใหม่ไม่ได้โยง)
    void AutoBindUIIfMissing()
    {
        // ชื่อเหล่านี้ปรับตามโปรเจ็กต์ได้
        if (!timeText)
        {
            var go = GameObject.Find("TimeText");
            if (!go) go = GameObject.Find("Text_Time");
            timeText = go ? go.GetComponent<TMP_Text>() : null;
        }

        if (!salesText)
        {
            var go = GameObject.Find("SalesText");
            if (!go) go = GameObject.Find("Text_Sales");
            salesText = go ? go.GetComponent<TMP_Text>() : null;
        }

        if (!goalText)
        {
            var go = GameObject.Find("GoalText");
            if (!go) go = GameObject.Find("Text_Goal");
            goalText = go ? go.GetComponent<TMP_Text>() : null;
        }

        if (!DayText)
        {
            var go = GameObject.Find("DayText");
            if (!go) go = GameObject.Find("Text_Day");
            DayText = go ? go.GetComponent<TMP_Text>() : null;
        }

        if (!dangerText)
        {
            var go = GameObject.Find("DangerText");
            if (!go) go = GameObject.Find("Text_Danger");
            dangerText = go ? go.GetComponent<TMP_Text>() : null;
        }
    }
}

using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Day / Goal")]
    public int currentDay = 1;
    public int salesGoal = 500;
    public int currentSales = 0;

    [Header("Clock Settings")]
    [Tooltip("ชั่วโมงเริ่มต้นของวัน (24 ชม.) เช่น 15 = 15:00")]
    public int startHour = 15;           // เริ่ม 15:00
    [Tooltip("จำนวนชั่วโมงต่อวันเกม (15→03 = 12 ชม.)")]
    public int hoursPerDay = 12;         // 15:00 → 03:00
    [Tooltip("วินาทีจริงต่อ 1 ชั่วโมงในเกม (ต้องการ 10 วินาที)")]
    public float hourDuration = 10f;     // 10s = 1h in-game

    [Tooltip("ถ้าเปิด ต้องทำยอดถึงเป้าถึงจะข้ามวันได้")]
    public bool requireGoalToProgress = false;

    // ตัวนับภายใน
    private float hourTimer = 0f;
    private int currentHour;               // 0–23
    private int elapsedHoursThisDay = 0;   // 0..hoursPerDay

    [Header("UI")]
    public TMP_Text timeText;
    public TMP_Text salesText;
    public TMP_Text goalText;
    public TMP_Text DayText;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // กันซ้ำ
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);    // อยู่ข้ามซีนได้
    }
    void Start()
    {
        StartNewDay();
    }

    void Update()
    {
        hourTimer += Time.deltaTime;

        // รองรับเฟรมตก: เดินทีละ "ชั่วโมงเกม" จนครบ
        while (hourTimer >= hourDuration)
        {
            hourTimer -= hourDuration;
            AdvanceHour();

            // ถ้าจบวันแล้ว เรารีเซ็ตใน EndDay() → หยุด loop เฟรมนี้เลย
            if (elapsedHoursThisDay == 0) break;
        }
    }

    // เลื่อนเวลาไป 1 ชั่วโมงเกม
    void AdvanceHour()
    {
        currentHour = (currentHour + 1) % 24;
        elapsedHoursThisDay++;
        UpdateTimeUI();

        if (elapsedHoursThisDay >= hoursPerDay)
        {
            EndDay();
        }
    }

    // ---------- Day Control ----------
    void StartNewDay()
    {
        // เวลา
        currentHour = startHour % 24;
        elapsedHoursThisDay = 0;
        hourTimer = 0f;

        // ยอดขาย
        if (salesGoal <= 0) salesGoal = 500;      // กันค่าเพี้ยน
        currentSales = 0;

        // UI
        UpdateDayUI();
        UpdateTimeUI();
        UpdateSalesUI();

        // Debug.Log($"[Day {currentDay}] Start {currentHour:00}:00  Goal {salesGoal}");
    }

    void EndDay()
    {
        bool pass = currentSales >= salesGoal;
        if (!requireGoalToProgress || pass)
        {
            currentDay++;
            StartNewDay();
        }
        else
        {
            // ยังอยากวนวันต่อก็เปลี่ยนเป็น StartNewDay();
            // ตอนนี้: ไม่ผ่านเป้า → ค้างวันไว้ หรือแสดง GameOver ตามต้องการ
            // ตัวอย่าง: รีเซ็ตวันใหม่ต่อให้แม้ไม่ถึงเป้า:
            // currentDay++; StartNewDay();

            // ถ้าอยาก "หยุดเกม" ให้ทำ UI/Scene ที่นี่
            // Debug.Log("Game Over! ยอดขายไม่ถึงเป้า");
            currentDay++;       // ถ้าต้องการให้เดินต่อแม้ไม่ถึงเป้า ให้คอมเมนต์บรรทัดนี้ออกได้
            StartNewDay();      // ← เลือกแนวทางนี้เพื่อให้ตรงคำขอ “วนลูปไปเรื่อยๆ”
        }
    }

    // ---------- Public API ----------
    public void AddSales(int amount)
    {
        currentSales += amount;
        UpdateSalesUI();
    }

    // ---------- UI ----------
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
}

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Day / Goal")]
    public int currentDay = 1;
    public int salesGoal = 500;
    public int currentSales = 0;

    [Header("Clock Settings")]
    [Tooltip("ชั่วโมงเริ่มต้นของแต่ละวัน (24h) เช่น 15 = 15:00")]
    public int startHour = 15;          // เริ่ม 15:00
    [Tooltip("จำนวนชั่วโมงในหนึ่งวันของเกม เช่น 15->03 = 12 ชั่วโมง")]
    public int hoursPerDay = 12;        // 15:00 ถึง 03:00
    [Tooltip("ระยะเวลา (วินาทีจริง) ต่อ 1 ชั่วโมงในเกม")]
    public float hourDuration = 60f;    // 60 วินาที = 1 ชั่วโมงในเกม

    // ตัวนับภายใน
    private float hourTimer = 0f;
    private float dayTimer = 0f;
    private int currentHour;               // ชั่วโมงปัจจุบัน (0–23)
    private int elapsedHoursThisDay = 0;   // จำนวนชั่วโมงที่ผ่านไปในวันนี้ (0..hoursPerDay)

    [Header("UI")]
    public TMP_Text timeText;
    public TMP_Text salesText;
    public TMP_Text goalText;
    public TMP_Text DayText;

    void Start()
    {
        StartNewDay();   // รีเซ็ตเวลา + เป้าขาย + UI
    }

    void Update()
    {
        // นับเวลาจริง -> ชั่วโมงในเกม
        hourTimer += Time.deltaTime;
        dayTimer += Time.deltaTime;

        if (hourTimer >= hourDuration)
        {
            hourTimer -= hourDuration;

            // เดินหน้าชั่วโมงจริง (วน 24 ชั่วโมง)
            currentHour = (currentHour + 1) % 24;
            elapsedHoursThisDay++;

            UpdateTimeUI();

            // ครบชั่วโมงของวันนี้ -> จบวัน
            if (elapsedHoursThisDay >= hoursPerDay)
            {
                EndDay();
            }
        }
    }

    // ---------- Day Control ----------
    void StartNewDay()
    {
        // รีเซ็ตเวลา
        currentHour = startHour % 24;
        elapsedHoursThisDay = 0;
        hourTimer = 0f;
        dayTimer = 0f;

        // สุ่ม/ตั้งเป้ายอดขายใหม่ และรีเซ็ตยอดวันนี้
        salesGoal = Random.Range(900, 1501);
        currentSales = 0;

        // อัปเดต UI
        UpdateDayUI();
        UpdateTimeUI();
        UpdateSalesUI();

        Debug.Log($"[Day {currentDay}] Start at {currentHour:00}:00  Goal: {salesGoal}");
        // TODO: Spawn รอบใหม่/สุ่มลูกค้าได้ที่นี่
    }

    void EndDay()
    {
        Debug.Log($"[Day {currentDay}] End. Sales {currentSales}/{salesGoal}");

        if (currentSales >= salesGoal)
        {
            currentDay++;
            StartNewDay();
        }
        else
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over! ยอดขายไม่ถึงเป้า");
        // ใส่ UI GameOver หรือรีสตาร์ทซีนตามต้องการ
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // หรือถ้าจะให้เล่นต่อวันใหม่แม้ไม่ถึงเป้า ก็สลับมาเรียก StartNewDay() แทน
    }

    // ---------- UI ----------
    void UpdateSalesUI()
    {
        if (salesText) salesText.text = $"Sales: {currentSales}";
        if (goalText) goalText.text = $"Goal: {salesGoal}";
    }

    void UpdateTimeUI()
    {
        if (timeText)
            timeText.text = $"{currentHour:00}:00"; // แสดงแบบ 24 ชั่วโมง สองหลัก
    }

    void UpdateDayUI()
    {
        if (DayText)
            DayText.text = $"Day {currentDay}";
    }

    // ---------- Public API ----------
    public void AddSales(int amount)
    {
        currentSales += amount;
        UpdateSalesUI();
        // Debug.Log($"Sold! Today's sales: {currentSales}/{salesGoal}");
    }
}

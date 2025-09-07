using StarterAssets;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public FirstPersonController playerController;
    // ========= Day / Sales =========
    public int currentDay = 1;
    public int salesGoal = 500;   // เป้าขายต่อวัน
    public int currentSales = 0;  // ยอดขาย "วันนี้"

    // ========= Clock =========
    [Header("Clock Settings")]
    [Tooltip("ชั่วโมงเริ่มต้นของรอบ (24h) เช่น 15 = 15:00")]
    public int startHour = 15;         
    [Tooltip("วินาทีจริงต่อ 1 ชั่วโมงในเกม")]
    public float hourDuration = 10f;  

    [Header("Danger & Sleep")]
    [Tooltip("เริ่มช่วงอันตราย (รวมชั่วโมงนี้)")]
    public int dangerStartHour = 3;  
    [Tooltip("เวลาจบรอบ (ถึงชั่วโมงนี้แล้วจะจบวัน/ตายถ้าไม่ได้นอน)")]
    public int dayEndHour = 6;   
    public string deathSceneName = "CutScene_Die";

    // เวลาภายใน
    private float hourTimer = 0f;
    public int currentHour;              
    private int elapsedHoursThisDay = 0;   
    private int runtimeDayLength = 0;      
    private bool sleptThisCycle = false;     
    private bool isEnding = false;          

    // ========= UI =========
    [Header("UI (assign per scene or auto-bind)")]
    public TMP_Text timeText;
    public TMP_Text salesText;
    public TMP_Text goalText;
    public TMP_Text DayText;

    [Header("Danger UI")]
    public TMP_Text dangerText;            
    public string dangerMessage = "Danger time! Go to bed.";

    [Header("Danger Gauge")]
    public DangerTimeGauge dangerGauge;  // ลากคอมโพเนนต์ DangerTimeGauge มาใส่

    bool wasInDanger = false;            // สถานะก่อนหน้า (ใช้เช็คเปลี่ยนสถานะ)

    [Header("End of Day")]
    public float deathDelaySeconds = 1f;   // เวลาหน่วงก่อนเข้า CutScene_Die
    public bool freezeDuringDeathDelay = true; // true = หยุดเกมระหว่างดีเลย์



    // อยู่ช่วงอันตรายหรือไม่ (เช็คแบบวง 24 ชม.)
    public bool IsDangerTime => IsHourInRange(currentHour, dangerStartHour, dayEndHour);

    void Start()
    {
        bool inDanger = IsHourInRange(currentHour, dangerStartHour, dayEndHour);
        if (dangerGauge)
        {
            if (inDanger) dangerGauge.BeginDanger(dangerStartHour, dayEndHour);
            else dangerGauge.EndDanger();
        }
        wasInDanger = inDanger;
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
        // --- Danger Gauge driving ---
        bool inDanger = IsHourInRange(currentHour, dangerStartHour, dayEndHour);

        if (dangerGauge)
        {
            // เปลี่ยนสถานะ: เข้าสู่ช่วงอันตราย
            if (inDanger && !wasInDanger)
                dangerGauge.BeginDanger(dangerStartHour, dayEndHour);

            // อยู่ในช่วงอันตราย → อัปเดตเกจทุกเฟรม
            if (inDanger)
                dangerGauge.UpdateDanger(currentHour, hourTimer, hourDuration);

            // เปลี่ยนสถานะ: ออกจากช่วงอันตราย
            if (!inDanger && wasInDanger)
                dangerGauge.EndDanger();
        }

        wasInDanger = inDanger;

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

        if (!sleptThisCycle)
        {
            
            StartCoroutine(DeathSequence());
            return;
        }

        currentDay++;
        StartNewDay();
    }

    public void SleepNow()
    {
        if (isEnding) return;

        sleptThisCycle = true;
        currentDay++;
        StartNewDay(); 
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

    bool IsHourInRange(int h, int start, int end)
    {
        h = (h + 24) % 24;
        start = (start + 24) % 24;
        end = (end + 24) % 24;

        if (start == end) return true;        
        if (start < end) return h >= start && h < end;
        return h >= start || h < end;          
    }
    public int CurrentHour => currentHour;

    public float HourProgress01
    {
        get
        {
            if (hourDuration <= 0f) return 0f;
            return Mathf.Clamp01(hourTimer / hourDuration);
        }
    }
    IEnumerator DeathSequence()
    {
        if (freezeDuringDeathDelay)
            Time.timeScale = 0f;
        playerController.isMovementLocked = true;
        StartCoroutine(FadeManager.Instance.FadeOutAndLoad("CutScene_Die"));
        yield return new WaitForSecondsRealtime(deathDelaySeconds);

        if (freezeDuringDeathDelay)
            Time.timeScale = 1f;

       

    }





}


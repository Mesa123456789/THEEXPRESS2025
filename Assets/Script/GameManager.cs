using StarterAssets;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ChoiceResult { None, Yes, No }

public class GameManager : MonoBehaviour
{
    public FirstPersonController playerController;
    public static GameManager Instance { get; private set; }

    public BoxScript currentBox;
    // ===== Gameplay Money & Progress =====
    public int currentDay = 1;
    public int salesGoal = 500;

    // กระเป๋าเงินสองใบ (A-Model)
    public int currentSales = 0;   // เงินสดวันนี้ (รีเซ็ตทุกวัน)
    public int bankBalance = 0;    // เงินสะสม/กองกลาง (คงอยู่ข้ามวัน)
    public int totalCaughtPercent = 0;
    public int Debt = 500000;

    // อ่านอย่างเดียว: ยอดรวมที่ใช้ได้จริงตอนนี้
    public int TotalFunds => currentSales + bankBalance;

    // ===== PlayerPrefs Keys =====
    const string KEY_RELOAD = "GM_ReloadOnNewDay";
    const string KEY_DAY = "GM_DaySaved";
    const string KEY_TOTAL_SALES = "GM_TotalSales"; // คีย์เก่า (migrate -> KEY_BANK)
    const string KEY_Debt = "KEY_Debt";
    const string KEY_BANK = "GM_BankBalance"; // คีย์ใหม่

    [Header("Clock Settings")]
    public int startHour = 15;
    public float hourDuration = 10f;

    [Header("Danger & Sleep")]
    public int dangerStartHour = 3;
    public int dayEndHour = 6;
    public string deathSceneName = "CutScene_Die";

    private float hourTimer = 0f;
    public int currentHour;
    private int elapsedHoursThisDay = 0;
    private int runtimeDayLength = 0;
    private bool sleptThisCycle = false;
    private bool isEnding = false;

    [Header("UI (assign per scene or auto-bind)")]
    public TMP_Text timeText;
    public TMP_Text salesText;            // currentSales
    public TMP_Text goalText;
    public TMP_Text DayText;
    public TMP_Text totalSalesText;       // จะโชว์ TotalFunds
    [Header("Danger UI")]
    public TMP_Text dangerText;
    public string dangerMessage = "Danger time! Go to bed.";
    public TMP_Text totalCaughtPercentText;

    [Header("Danger Gauge")]
    public DangerTimeGauge dangerGauge;
    bool wasInDanger = false;

    [Header("End of Day")]
    public float deathDelaySeconds = 1f;
    public bool freezeDuringDeathDelay = true;

    [Header("Shop Time")]
    public int shopOpenHour = 15;
    public int shopCloseHour = 2;
    public bool shopIsOpen = false;
    public NPCSpawner npcSpawner;


    [Header("Day/Night Lighting")]
    public Light directionalLight;
    public Vector3 lightAxis = Vector3.right;
    public float baseRotation = 0f;

    bool tutorialStep12Shown = false;
    const string KEY_TUTORIAL_DONE = "Tutorial_Done";

    public bool IsDangerTime => IsHourInRange(currentHour, dangerStartHour, dayEndHour);
    public TutorialSlideUIQueue TutorialSlideUIQueue;


    void Awake()
    {
        // โหลดค่าข้ามวัน
        if (PlayerPrefs.GetInt(KEY_RELOAD, 0) == 1)
        {
            currentDay = PlayerPrefs.GetInt(KEY_DAY, currentDay);
            Debt = PlayerPrefs.GetInt(KEY_Debt, Debt);

            // --- Migration: ถ้ามีคีย์เก่า (KEY_TOTAL_SALES) แต่ยังไม่มี KEY_BANK ให้ย้ายค่าไป bankBalance ---
            if (PlayerPrefs.HasKey(KEY_BANK))
            {
                bankBalance = PlayerPrefs.GetInt(KEY_BANK, bankBalance);
            }
            else
            {
                bankBalance = PlayerPrefs.GetInt(KEY_TOTAL_SALES, 0); // ย้ายยอดเก่าเข้ากองกลาง
                PlayerPrefs.SetInt(KEY_BANK, bankBalance);
            }

            PlayerPrefs.SetInt(KEY_RELOAD, 0);
            PlayerPrefs.Save();
        }
    }

    void Start()
    {
        if (currentDay == 1 && TutorialSlideUIQueue)
        {
            PlayerPrefs.DeleteKey(TutorialSlideUIQueue.KEY_TUTORIAL_DISABLED);
            TutorialSlideUIQueue.enabled = true;
            TutorialSlideUIQueue.ClearQueueAndHideImmediate();
            if (TutorialSlideUIQueue.tutorialMessages != null && TutorialSlideUIQueue.tutorialMessages.Count > 0)
                TutorialSlideUIQueue.EnqueueTutorialByIndex(0);
        }
        else if (currentDay > 1 && TutorialSlideUIQueue)
        {
            // ✅ ปิดหมดทุกรอบในวันถัดไป
            PlayerPrefs.SetInt(TutorialSlideUIQueue.KEY_TUTORIAL_DISABLED, 1);
            PlayerPrefs.Save();
            TutorialSlideUIQueue.SetDisabled(true);  // เกตกลางกันทุก Enqueue/Complete
        }

        shopIsOpen = false;

        StartNewDay();

        // ถ้าเคยถูกปิดถาวรไว้
        if (PlayerPrefs.GetInt(TutorialSlideUIQueue.KEY_TUTORIAL_DISABLED, 0) == 1)
        {
            if (TutorialSlideUIQueue)
            {
                TutorialSlideUIQueue.ClearQueueAndHideImmediate();
                TutorialSlideUIQueue.enabled = false;
            }
        }

        bool inDanger = IsHourInRange(currentHour, dangerStartHour, dayEndHour);
        if (dangerGauge)
        {
            if (inDanger) dangerGauge.BeginDanger(dangerStartHour, dayEndHour);
            else dangerGauge.EndDanger();
        }
        wasInDanger = inDanger;

        currentBox = FindFirstObjectByType<BoxScript>();
    }



    void Update()
    {
        if (isEnding) return;

        if (currentBox == null)
        {
            currentBox = FindFirstObjectByType<BoxScript>();
            if (currentBox != null) Debug.Log("เชื่อม currentBox สำเร็จ!");
            // ❌ ห้าม return ที่นี่
        }

        hourTimer += Time.deltaTime;
        while (hourTimer >= hourDuration && !isEnding)
        {
            hourTimer -= hourDuration;
            AdvanceHour();
            if (elapsedHoursThisDay == 0) break;
        }
        if (!tutorialStep12Shown && TutorialSlideUIQueue && currentHour == shopCloseHour)
        {
            tutorialStep12Shown = true;

            // ถ้าตอนนี้โชว์ 11 อยู่ ให้ complete; ไม่งั้นข้าม
            if (TutorialSlideUIQueue.CurrentIndex == 11)
                TutorialSlideUIQueue.CompleteCurrentByIndex(11);

            StartCoroutine(OpenTutorial12());
        }


        // อัปเดต danger + เกจ
        bool inDanger = IsHourInRange(currentHour, dangerStartHour, dayEndHour);
        if (dangerGauge)
        {
            if (inDanger && !wasInDanger) dangerGauge.BeginDanger(dangerStartHour, dayEndHour);
            if (inDanger) dangerGauge.UpdateDanger(currentHour, hourTimer, hourDuration);
            if (!inDanger && wasInDanger) dangerGauge.EndDanger();
        }
        wasInDanger = inDanger;

        // หลังอัปเดตชั่วโมง/สถานะเวลาแล้ว เพิ่ม:
        if (npcSpawner)
        {
            // ข้อ 5: หลังตี 2 ไม่ให้สปาวน์เพิ่ม
            if (currentHour == ((shopCloseHour % 24) + 24) % 24 || currentHour > ((shopCloseHour % 24) + 24) % 24)
            {
                npcSpawner.SetSpawningEnabled(false);
            }
        }

        UpdateTimeUI();
        UpdateSunLight();
    }

    IEnumerator OpenTutorial12()
    {
        yield return new WaitForSeconds(0.3f);
        if (TutorialSlideUIQueue) TutorialSlideUIQueue.EnqueueTutorialByIndex(12);
    }

    // เรียกจุดเช็คเวลา: ใส่หลังเปลี่ยนชั่วโมง (เช่นใน AdvanceHour() หรือหลัง while เดินเวลาใน Update())



    void UpdateSunLight()
    {
        if (!directionalLight) return;
        float hourFloat = currentHour + HourProgress01;
        float t = (hourFloat / 24f) * 360f;
        directionalLight.transform.rotation = Quaternion.Euler(new Vector3(t + baseRotation, 170f, 0f));
    }

    public void SetShopOpen(bool open)
    {
        shopIsOpen = open;
        Debug.Log($"[GameManager] Shop is now {(shopIsOpen ? "OPEN" : "CLOSED")}.");

        if (!npcSpawner) return;

        if (!open)
        {
            // ปิดร้าน: ล้างคิว (เว้น current) และหยุดสปาวน์
            npcSpawner.HandleShopClosed();
            npcSpawner.SetSpawningEnabled(false);
            return;
        }

        // เปิดร้าน: อนุญาตให้สปาวน์ และ "พรีวอร์มคิว" ให้มีคนมารอทันที
        npcSpawner.SetSpawningEnabled(true);

        // จำนวนเริ่มต้นที่จะให้ยืนรอคิวหน้าโต๊ะ
        // ถ้ามี Queue Points ให้เติมให้เต็มแถว; ถ้าไม่มีก็เท่ากับ maxAlive
        int targetInitial =
            (npcSpawner.queuePoints != null && npcSpawner.queuePoints.Length > 0)
            ? npcSpawner.queuePoints.Length
            : npcSpawner.maxAlive;

        // กันเกินโควตา
        targetInitial = Mathf.Min(targetInitial, npcSpawner.maxAlive);

        // สปาวน์ให้ครบจำนวนที่ตั้งใจ (ปล่อยให้ NPCSpawner จัดเข้าคิวเอง)
        for (int i = 0; i < targetInitial; i++)
        {
            npcSpawner.SpawnOne();
        }
    }



    // ====== เงิน: เติมจากการขาย -> เข้า current ก่อน (พฤติกรรมเดิม) ======
    public void AddSales(int amount, int caughtPercent)
    {
        amount = Mathf.Max(0, amount);
        currentSales += amount;   // เงินสดวันนี้
        totalCaughtPercent += caughtPercent;

        UpdateSalesUI();

        // ตำรวจ
        if (totalCaughtPercent >= 90)
        {
            var spawner = FindFirstObjectByType<NPCSpawner>();
            if (spawner) spawner.ForcePoliceNext();
        }
    }

    // ====== เงิน: จ่าย -> ใช้ current ก่อน ถ้าไม่พอรูดจาก bankBalance ======
    public bool SpendMoney(int amount)
    {
        amount = Mathf.Abs(amount);
        int available = TotalFunds;
        if (available < amount) return false;

        if (currentSales >= amount) { currentSales -= amount; }
        else
        {
            int remain = amount - currentSales;
            currentSales = 0;
            bankBalance -= remain;
        }

        UpdateSalesUI();
        return true;
    }

    // Back-compat (เมธอดเก่าที่ยังถูกเรียกจากที่อื่น)
    public bool TrySpendFromTotalSales(int amount) => SpendMoney(amount);

    public void KillPlayerNow()
    {
        if (isEnding) return;
        StartCoroutine(DeathSequence());
    }

    void AdvanceHour()
    {
        currentHour = (currentHour + 1) % 24;
        elapsedHoursThisDay++;
        UpdateTimeUI();
        UpdateDangerUI();

        if (elapsedHoursThisDay >= runtimeDayLength || currentHour == (dayEndHour % 24))
        {
            EndDay();
        }
    }

    void StartNewDay()
    {

        currentHour = startHour % 24;
        elapsedHoursThisDay = 0;
        hourTimer = 0f;
        sleptThisCycle = false;
        isEnding = false;

        runtimeDayLength = (dayEndHour - startHour + 24) % 24;
        if (runtimeDayLength <= 0) runtimeDayLength = 24;

        if (salesGoal <= 0) salesGoal = 500;

        // รีเซ็ตเงินสดรายวันเหมือนเดิม
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

        // ✅ เก็บเงินของวันเข้าสู่กองกลางก่อนรีเซ็ต
        bankBalance += currentSales;
        currentSales = 0;

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

    public void UpdateSalesUI()
    {
        if (salesText) salesText.text = $"Sales: {currentSales}";
        if (goalText) goalText.text = $"Goal: {salesGoal}";
        if (totalSalesText) totalSalesText.text = $"Total Sales: {TotalFunds}"; // โชว์ยอดรวมที่ใช้ได้จริง
        if (totalCaughtPercentText) totalCaughtPercentText.text = $"total CaughtPercent: {totalCaughtPercent}";
    }

    void UpdateTimeUI()
    {
        if (!timeText) return;

        //int minutes = Mathf.FloorToInt(HourProgress01 * 60f);
        timeText.text = $"{currentHour:00} : 00";
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
        else { dangerText.gameObject.SetActive(false); }
    }

    bool IsHourInRange(int h, int start, int end)
    {
        h = (h + 24) % 24; start = (start + 24) % 24; end = (end + 24) % 24;
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
        if (freezeDuringDeathDelay) Time.timeScale = 0f;
        playerController.isMovementLocked = true;
        StartCoroutine(FadeManager.Instance.FadeOutAndLoad("DieWMonster"));
        //SceneManager.LoadScene("DieWMonster");
        yield return new WaitForSecondsRealtime(deathDelaySeconds);
        if (freezeDuringDeathDelay) Time.timeScale = 1f;
    }

    public void SleepNowAndReloadScene(string gameplaySceneName = "Gameplay")
    {
        bankBalance += currentSales;
        currentSales = 0;

        int nextDay = currentDay + 1;
        PlayerPrefs.SetInt(KEY_BANK, bankBalance);
        PlayerPrefs.SetInt(KEY_Debt, Debt);
        PlayerPrefs.SetInt(KEY_DAY, nextDay);
        PlayerPrefs.SetInt(KEY_RELOAD, 1);

        // ✅ ทำ Tutorial เสร็จแล้ว
        PlayerPrefs.SetInt(KEY_TUTORIAL_DONE, 1);

        PlayerPrefs.Save();
        SceneManager.LoadScene(gameplaySceneName);
    }



}

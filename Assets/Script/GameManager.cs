using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // สำหรับรีสตาร์ท/จบเกม

public class GameManager : MonoBehaviour
{
    public int currentDay = 1;
    public float dayDuration = 60f; 
    public int salesGoal = 500;
    public int currentSales = 0;
    private float dayTimer = 0f;

    [Header("Time Settings")]
    public int startHour = 9;
    public int hoursPerDay = 10;
    public float totalDayDuration = 600f;
    public float hourDuration = 60f;
    private float hourTimer = 0f;
    private int currentHour;

    public TMP_Text timeText;
    public TMP_Text salesText; 
    public TMP_Text goalText; 
    public TMP_Text DayText;
    void Start()
    {
        currentHour = startHour;
        UpdateTimeUI();
        UpdateSalesUI();
        StartNewDay();
    }

    void UpdateSalesUI()
    {
        if (salesText != null)
            salesText.text = $"Sales: {currentSales}";

        if (goalText != null)
            goalText.text = $"Goal: {salesGoal}";
    }

    void UpdateTimeUI()
    {
        if (timeText != null)
        {
            timeText.text = $"{currentHour}:00";
        }
    }
    void Update()
    {
        dayTimer += Time.deltaTime;
        hourTimer += Time.deltaTime;

        if (hourTimer >= hourDuration)
        {
            hourTimer = 0f;
            currentHour++;
            UpdateTimeUI();

            // ถ้าจบวัน (เช่นเกิน 18.00)
            if (currentHour >= startHour + hoursPerDay)
            {
                EndDay();
            }
        }
    }

    void StartNewDay()
    {
        // randomize salesGoal, reset sales
        salesGoal = Random.Range(900, 1501); 
        currentSales = 0;
        dayTimer = 0f;

        Debug.Log($"Day {currentDay}, Goal: {salesGoal}");
        // Spawn NPC รอบใหม่ได้ตรงนี้ตามระบบสุ่ม
        UpdateSalesUI();
    }

    public void AddSales(int amount)
    {
        currentSales += amount;
        Debug.Log($"Sold! Today's sales: {currentSales}/{salesGoal}");
        // Update UI
        UpdateSalesUI();
    }

    void EndDay()
    {
        if (currentSales >= salesGoal)
        {
            currentDay++;
            StartNewDay();
        }
        else
        {
            GameOver();
        }
        UpdateSalesUI();
    }

    void GameOver()
    {
        Debug.Log("Game Over! ยอดขายไม่ถึงเป้า");
        // อาจใส่ UI แพ้หรือกดเริ่มใหม่
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


}
using UnityEngine;
using UnityEngine.UI;

public class DangerTimeGauge : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;                  // ตั้ง Max Value = 100
    public bool hideWhenNotDanger = true;  // ซ่อนเมื่อไม่ใช่ช่วงอันตราย

    // cache หน้าต่างอันตราย
    int startHour;
    int endHour;
    int windowH = 0;
    bool isActive = false;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!slider)
        {
            Debug.LogError("[DangerTimeGauge] Please assign Slider.");
            enabled = false;
            return;
        }
        if (slider.maxValue <= 0f) slider.maxValue = 100f;
        if (hideWhenNotDanger) slider.gameObject.SetActive(false);
        slider.value = 0f;
    }

    // เรียกเมื่อเข้าสู่ช่วงอันตราย
    public void BeginDanger(int dangerStartHour, int dangerEndHour)
    {
        startHour = Mod24(dangerStartHour);
        endHour = Mod24(dangerEndHour);
        windowH = Mod24(endHour - startHour);
        if (windowH == 0) windowH = 24;

        isActive = true;
        if (hideWhenNotDanger) slider.gameObject.SetActive(true);
        slider.value = slider.maxValue; // เริ่มเต็ม
    }

    // เรียกทุกเฟรม “ขณะอยู่ในช่วงอันตราย”
    public void UpdateDanger(int currentHour, float hourTimer, float hourDuration)
    {
        if (!isActive) return;

        int sinceStartH = Mod24(currentHour - startHour);
        float hourT = (hourDuration > 0f) ? Mathf.Clamp01(hourTimer / hourDuration) : 0f;

        float progressed = Mathf.Clamp(sinceStartH + hourT, 0f, windowH);
        float remain01 = Mathf.Clamp01((windowH - progressed) / windowH); // 1 → 0

        slider.value = slider.maxValue * remain01;
    }

    // เรียกเมื่อออกจากช่วงอันตราย
    public void EndDanger()
    {
        isActive = false;
        slider.value = 0f;
        if (hideWhenNotDanger) slider.gameObject.SetActive(false);
    }

    static int Mod24(int v) => (v % 24 + 24) % 24;
}

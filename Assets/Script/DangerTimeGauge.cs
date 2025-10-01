using UnityEngine;
using UnityEngine.UI;

public class DangerTimeGauge : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;                   // ใส่ Slider ของเกจ
    public bool hideWhenNotDanger = true;   // ซ่อนเมื่อไม่ใช่ช่วงอันตราย

    [Header("Smoothing")]
    public bool smooth = true;
    [Range(1f, 30f)] public float smoothSpeed = 12f;

    // cache
    int startHour, endHour, windowH;
    bool isActive = false;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!slider) { Debug.LogError("[DangerTimeGauge] Please assign Slider."); enabled = false; return; }

        slider.wholeNumbers = false;           // ❗ ทำให้เลื่อนไหล ไม่เป็นขั้น
        if (slider.maxValue <= 0f) slider.maxValue = 100f;

        slider.value = 0f;
        if (hideWhenNotDanger) slider.gameObject.SetActive(false);
    }

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

    public void UpdateDanger(int currentHour, float hourTimer, float hourDuration)
    {
        if (!isActive) return;

        int sinceStartH = Mod24(currentHour - startHour);
        float hourT = (hourDuration > 0f) ? Mathf.Clamp01(hourTimer / hourDuration) : 0f;

        float progressed = Mathf.Clamp(sinceStartH + hourT, 0f, windowH);   // เดินหน้ากี่ชั่วโมง
        float remain01 = Mathf.Clamp01((windowH - progressed) / windowH); // คงเหลือ 1→0
        float target = slider.maxValue * remain01;

        if (smooth)
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            slider.value = Mathf.Lerp(slider.value, target, t);
        }
        else
        {
            slider.value = target;
        }
    }

    public void EndDanger()
    {
        isActive = false;
        slider.value = 0f;
        if (hideWhenNotDanger) slider.gameObject.SetActive(false);
    }

    static int Mod24(int v) => (v % 24 + 24) % 24;
}

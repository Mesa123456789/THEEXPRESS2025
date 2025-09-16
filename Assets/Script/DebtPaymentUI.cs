using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DebtPaymentUI : MonoBehaviour
{
    [Header("Debt UI")]
    [SerializeField] TMP_Text debtAmountText;     // ตัวเลขใหญ่ "500,000"
    [SerializeField] TMP_InputField payInput;     // ช่องกรอกจำนวน
    [SerializeField] Button submitButton;         // ปุ่ม Submit

    [Header("Status Text (TMP)")]
    [SerializeField] TMP_Text statusText;         // ข้อความแจ้งผล เช่น PAYMENT SUCCESS / NOT ENOUGH MONEY

    [Header("Config")]
    [SerializeField] int startingDebt = 500000;
    [SerializeField] float statusShowSecs = 1.25f; // เวลาซ่อนสถานะอัตโนมัติ
    [SerializeField] bool alsoAffectTotal = false; // ถ้าจ่ายแล้วให้ลดยอด totalSales ด้วยไหม

    int outstandingDebt;
    GameManager gm;
    Coroutine hideCo;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        outstandingDebt = Mathf.Max(0, startingDebt);
    }

    void Start()
    {
        if (payInput) payInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        if (submitButton) submitButton.onClick.AddListener(OnSubmitPay);
        ShowStatus(false, "");           // ซ่อนตอนเริ่ม
        RefreshDebtUI();
    }

    void RefreshDebtUI()
    {
        if (debtAmountText) debtAmountText.text = outstandingDebt.ToString("N0");
    }

    public void OnSubmitPay()
    {
        if (!gm)
        {
            ShowStatus(false, "NOT FOUND: GAMEMANAGER");
            return;
        }

        // อ่านจำนวนเงิน (ตัดคอมมา/ช่องว่าง)
        var raw = payInput ? payInput.text : "";
        var sanitized = SanitizeNumber(raw);
        if (!int.TryParse(sanitized, out int amount) || amount <= 0)
        {
            ShowStatus(false, "PAYMENT SUCCESS");
            return;
        }

        // กันจ่ายเกินหนี้
        if (amount > outstandingDebt)
        {
            ShowStatus(false, "EXCEEDS OUTSTANDING BALANCE");
            return;
        }

        // กันจ่ายเกินเงินที่มี
        if (amount > gm.currentSales)
        {
            ShowStatus(false, "NOT ENOUGH MONEY");
            return;
        }

        // ตัดเงินจริง
        bool ok = gm.TrySpendFromCurrentSales(amount, alsoAffectTotal); // ต้องมีใน GameManager
        if (!ok)
        {
            ShowStatus(false, "PAYMENT FAILED");
            return;
        }

        // อัปเดตยอดหนี้ + UI
        outstandingDebt -= amount;
        RefreshDebtUI();
        if (payInput) payInput.text = "";

        // แสดงผลสำเร็จ
        ShowStatus(true, "PAYMENT SUCCESS");

        if (outstandingDebt <= 0 && submitButton)
            submitButton.interactable = false;
    }
    void ShowStatus(bool success, string message)
    {
        if (!statusText) return;

        statusText.text = message.ToUpperInvariant();

        // เขียวสด / แดงสด
        statusText.color = success ? Color.green : Color.red;

        statusText.gameObject.SetActive(true);

        if (hideCo != null) StopCoroutine(hideCo);
        hideCo = StartCoroutine(AutoHideStatus());
    }

    // แสดงข้อความสถานะบน TMP_Text


    IEnumerator AutoHideStatus()
    {
        yield return new WaitForSecondsRealtime(statusShowSecs);
        if (statusText)
        {
            var c = statusText.color;
            c.a = 0f;
            statusText.color = c;
            statusText.gameObject.SetActive(false);
        }
        hideCo = null;
    }

    // ตัดอักขระที่ไม่ใช่ตัวเลข (รองรับ "10,000")
    string SanitizeNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return "0";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s)
            if (char.IsDigit(ch)) sb.Append(ch);
        return sb.Length == 0 ? "0" : sb.ToString();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
public class DebtPaymentUI : MonoBehaviour
{
    [Header("Debt UI")]
    [SerializeField] TMP_Text debtAmountText;
    [SerializeField] TMP_InputField payInput;
    [SerializeField] Button submitButton;

    [Header("Status Text (TMP)")]
    [SerializeField] TMP_Text statusText;

    [Header("Config")]
    [SerializeField] float statusShowSecs = 1.25f;

    int outstandingDebt;
    GameManager gm;
    Coroutine hideCo;
    private bool isProcessing = false;


    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        outstandingDebt = Mathf.Max(0, gm.Debt);
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitPay);
        }
    }


    void Start()
    {
        if (payInput) payInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        if (submitButton) submitButton.onClick.AddListener(OnSubmitPay);
        ShowStatus(false, "");
        RefreshDebtUI();
    }

    void RefreshDebtUI()
    {
        if (debtAmountText) debtAmountText.text = outstandingDebt.ToString("N0");
    }

    public void OnSubmitPay()
    {
        if (isProcessing) return;             // กันดับเบิลคลิก
        isProcessing = true;

        try
        {
            if (payInput == null)
            {
                ShowStatus(false, "INPUT NOT FOUND");
                return;
            }

            // 1) ทำความสะอาดอินพุต: เอาเฉพาะตัวเลข (รองรับคั่นหลักพันด้วย , ก็ได้)
            string raw = payInput.text ?? "";
            string sanitized = SanitizeNumber(raw);  // ดูฟังก์ชันด้านล่าง

            // 2) แปลงเป็นจำนวนเต็ม
            //    ถ้าอยากรองรับคั่นหลักพันด้วย comma ให้ใช้ NumberStyles.AllowThousands
            if (!int.TryParse(sanitized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
            {
                ShowStatus(false, "INVALID INPUT");
                return; // ออกจากฟังก์ชันทันที -> ห้ามตัดเงิน
            }

            // 3) ตรวจหนี้
            if (amount > outstandingDebt)
            {
                ShowStatus(false, "EXCEEDS OUTSTANDING BALANCE");
                return;
            }

            // 4) ตรวจเงินคงเหลือ
            if (amount > gm.TotalFunds)
            {
                ShowStatus(false, "NOT ENOUGH MONEY");
                return;
            }

            // 5) ตัดเงิน
            bool ok = gm.SpendMoney(amount);
            if (!ok)
            {
                ShowStatus(false, "PAYMENT FAILED");
                return;
            }

            // 6) อัปเดตหนี้ / UI
            outstandingDebt -= amount;
            RefreshDebtUI();
            payInput.text = "";

            if (outstandingDebt <= 0 && submitButton)
                submitButton.interactable = false;

            // 7) สำเร็จ
            ShowStatus(true, "PAYMENT SUCCESS");
        }
        finally
        {
            isProcessing = false;
        }
    }

    // เก็บไว้ให้ชัดเจน: เอาเฉพาะตัวเลข (ถ้ามีคอมมา ให้ลบทิ้ง)
    private string SanitizeNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // ลบทุกอย่างที่ไม่ใช่ 0-9
        return Regex.Replace(s, "[^0-9]", "");
    }


    void ShowStatus(bool success, string message)
    {
        if (!statusText) return;

        statusText.text = message.ToUpperInvariant();
        statusText.color = success ? Color.green : Color.red;
        statusText.gameObject.SetActive(true);

        if (hideCo != null) StopCoroutine(hideCo);
        hideCo = StartCoroutine(AutoHideStatus());
    }

    IEnumerator AutoHideStatus()
    {
        yield return new WaitForSecondsRealtime(statusShowSecs);
        if (statusText)
        {
            statusText.gameObject.SetActive(false);
        }
        hideCo = null;
    }


}

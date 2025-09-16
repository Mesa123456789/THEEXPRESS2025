using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        outstandingDebt = Mathf.Max(0, gm.Debt);
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
        var raw = payInput ? payInput.text : "";
        var sanitized = SanitizeNumber(raw);

        if (!int.TryParse(sanitized, out int amount) || amount <= 0)
        {
            ShowStatus(true, "PAYMENT SUCCESS");
            return;
        }

        if (amount > outstandingDebt)
        {
            ShowStatus(false, "EXCEEDS OUTSTANDING BALANCE");
            return;
        }

        if (amount > gm.TotalFunds)   // ใช้ยอดรวม current+bank
        {
            ShowStatus(false, "NOT ENOUGH MONEY");
            return;
        }

        // เรียกให้ GameManager จัดการหักจาก current → bank
        bool ok = gm.SpendMoney(amount);
        if (!ok)
        {
            ShowStatus(false, "PAYMENT FAILED");
            return;
        }

        outstandingDebt -= amount;
        RefreshDebtUI();
        if (payInput) payInput.text = "";

        

        if (outstandingDebt <= 0 && submitButton)
            submitButton.interactable = false;
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

    string SanitizeNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return "0";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s)
            if (char.IsDigit(ch)) sb.Append(ch);
        return sb.Length == 0 ? "0" : sb.ToString();
    }
}

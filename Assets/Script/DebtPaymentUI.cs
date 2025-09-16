using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DebtPaymentUI : MonoBehaviour
{
    [Header("Debt UI")]
    [SerializeField] TMP_Text debtAmountText;     // ����Ţ�˭� "500,000"
    [SerializeField] TMP_InputField payInput;     // ��ͧ��͡�ӹǹ
    [SerializeField] Button submitButton;         // ���� Submit

    [Header("Status Text (TMP)")]
    [SerializeField] TMP_Text statusText;         // ��ͤ����駼� �� PAYMENT SUCCESS / NOT ENOUGH MONEY

    [Header("Config")]
    [SerializeField] int startingDebt = 500000;
    [SerializeField] float statusShowSecs = 1.25f; // ���ҫ�͹ʶҹ��ѵ��ѵ�
    [SerializeField] bool alsoAffectTotal = false; // ��Ҩ����������Ŵ�ʹ totalSales �������

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
        ShowStatus(false, "");           // ��͹�͹�����
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

        // ��ҹ�ӹǹ�Թ (�Ѵ�����/��ͧ��ҧ)
        var raw = payInput ? payInput.text : "";
        var sanitized = SanitizeNumber(raw);
        if (!int.TryParse(sanitized, out int amount) || amount <= 0)
        {
            ShowStatus(false, "PAYMENT SUCCESS");
            return;
        }

        // �ѹ�����Թ˹��
        if (amount > outstandingDebt)
        {
            ShowStatus(false, "EXCEEDS OUTSTANDING BALANCE");
            return;
        }

        // �ѹ�����Թ�Թ�����
        if (amount > gm.currentSales)
        {
            ShowStatus(false, "NOT ENOUGH MONEY");
            return;
        }

        // �Ѵ�Թ��ԧ
        bool ok = gm.TrySpendFromCurrentSales(amount, alsoAffectTotal); // ��ͧ��� GameManager
        if (!ok)
        {
            ShowStatus(false, "PAYMENT FAILED");
            return;
        }

        // �ѻവ�ʹ˹�� + UI
        outstandingDebt -= amount;
        RefreshDebtUI();
        if (payInput) payInput.text = "";

        // �ʴ��������
        ShowStatus(true, "PAYMENT SUCCESS");

        if (outstandingDebt <= 0 && submitButton)
            submitButton.interactable = false;
    }
    void ShowStatus(bool success, string message)
    {
        if (!statusText) return;

        statusText.text = message.ToUpperInvariant();

        // ����ʴ / ᴧʴ
        statusText.color = success ? Color.green : Color.red;

        statusText.gameObject.SetActive(true);

        if (hideCo != null) StopCoroutine(hideCo);
        hideCo = StartCoroutine(AutoHideStatus());
    }

    // �ʴ���ͤ���ʶҹк� TMP_Text


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

    // �Ѵ�ѡ��з����������Ţ (�ͧ�Ѻ "10,000")
    string SanitizeNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return "0";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s)
            if (char.IsDigit(ch)) sb.Append(ch);
        return sb.Length == 0 ? "0" : sb.ToString();
    }
}

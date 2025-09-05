using System;
using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;

public class FormChecker : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameField;
    public TMP_InputField addressField;

    [Header("Player Control")]
    public FirstPersonController playerController;

    [Header("Receipt")]
    public GameObject receiptPrefab;
    public Transform receiptSpawnPoint;

    [Header("Detection")]
    public string npcTag = "NPC";

    [Header("Feedback")]
    public TMP_Text feedbackText;                 // <- ใส่ Text ไว้โชว์ข้อความผิด
    public float feedbackDuration = 2f;           // เวลาที่จะแสดงข้อความ (วินาที)
    public string incorrectMessage = "Incorrect information."; // ข้อความอังกฤษ

    [SerializeField] private NPC currentNPC;
    private Coroutine feedbackCo;

    void Start()
    {
        if (feedbackText) feedbackText.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(npcTag)) return;
        currentNPC = other.GetComponent<NPC>() ?? other.GetComponentInParent<NPC>();
    }

    public void OnSubmitButton()
    {
        // เก็บค่าที่ผู้เล่นพิมพ์ไว้ก่อน แล้วค่อยล้างฟอร์ม
        string inputName = nameField ? nameField.text : string.Empty;
        string inputAddr = addressField ? addressField.text : string.Empty;

        ClearFormInputs(); // ล้างทุกครั้งที่กด Submit

        if (!currentNPC) { ShowFeedback(incorrectMessage); return; }

        var data = currentNPC.GetData();
        if (data == null) { ShowFeedback(incorrectMessage); return; }

        bool ok =
            !string.IsNullOrWhiteSpace(inputName) &&
            !string.IsNullOrWhiteSpace(inputAddr) &&
            string.Equals(inputName.Trim(), data.npcName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(inputAddr.Trim(), data.address?.Trim(), StringComparison.OrdinalIgnoreCase);

        if (ok)
        {
            if (playerController) playerController.isMovementLocked = false;
            OnFormSuccess();
        }
        else
        {
            ShowFeedback(incorrectMessage);
        }
    }

    void OnFormSuccess()
    {
        if (!receiptPrefab || !receiptSpawnPoint) return;
        Instantiate(receiptPrefab, receiptSpawnPoint.position, receiptPrefab.transform.rotation);
    }

    // ---------- Helpers ----------
    void ClearFormInputs()
    {
        if (nameField) nameField.SetTextWithoutNotify(string.Empty);
        if (addressField) addressField.SetTextWithoutNotify(string.Empty);
    }

    void ShowFeedback(string msg)
    {
        if (!feedbackText) return;
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
        if (feedbackCo != null) StopCoroutine(feedbackCo);
        feedbackCo = StartCoroutine(HideFeedbackAfter(feedbackDuration));
    }

    IEnumerator HideFeedbackAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (feedbackText) feedbackText.gameObject.SetActive(false);
        feedbackCo = null;
    }
}

using System;
using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem;

public class FormChecker : MonoBehaviour
{
    [Header("UI (Dropdown Mode)")]
    public TMP_Dropdown nameDropdown;      // แทน nameField
    public TMP_Dropdown addressDropdown;   // แทน addressField

    [Header("Player Control")]
    public FirstPersonController playerController;

    [Header("Receipt")]
    public GameObject receiptPrefab;
    public Transform receiptSpawnPoint;

    [Header("Detection")]
    public string npcTag = "NPC";

    [Header("Feedback")]
    public TMP_Text feedbackText;
    public float feedbackDuration = 2f;
    public string incorrectMessage = "Incorrect information.";

    [SerializeField] public NPC currentNPC;
    private Coroutine feedbackCo;
    public Computer computer;

    [Header("Dropdown UI")]
    public bool usePlaceholder = true;
    public string placeholderText = "-- Select --";
    ItemDialogueManager ItemDialogueManager;
    //[Header("Tutorial UI")]
    public TutorialSlideUIQueue tutorialUI;   // อ้างอิงถึงตัว UI สไลด์ที่ใช้ก่อนหน้า


    void Start()
    {
        if (feedbackText) feedbackText.gameObject.SetActive(false);
        ItemDialogueManager = FindFirstObjectByType<ItemDialogueManager>();

    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(npcTag)) return;

        currentNPC = other.GetComponent<NPC>() ?? other.GetComponentInParent<NPC>();
        SetupDropdownsFromCurrentNPC();
        ItemDialogueManager.ShowTutorialUI();
    }

    /// <summary>
    /// ปุ่ม Submit: อ่านค่าที่เลือก -> ตรวจถูก/ผิด
    /// ไม่ล้าง dropdown เมื่อผิด
    /// </summary>
    public void OnSubmitButton()
    {
        if (!currentNPC) { ShowFeedback(incorrectMessage); return; }
        var data = currentNPC.GetData();
        if (data == null) { ShowFeedback(incorrectMessage); return; }

        // ค่านี้คือ index ที่ผู้เล่น "เลือกจริง" ในชุด 4 ตัวเลือก (ไม่รวม placeholder)
        int selectedNameIndex = GetSelectedOptionIndex(nameDropdown);
        int selectedAddrIndex = GetSelectedOptionIndex(addressDropdown);

        // ถ้า user ยังไม่เลือกอะไร (อยู่ที่ placeholder) ให้ถือว่าผิด
        bool nameChosen = selectedNameIndex >= 0;
        bool addrChosen = selectedAddrIndex >= 0;

        bool nameOK = nameChosen && selectedNameIndex == data.correctNameIndex;
        bool addrOK = addrChosen && selectedAddrIndex == data.correctAddressIndex;

        bool ok = nameOK && addrOK;

        if (ok)
        {
            computer.OnCloseComputer();
            OnFormSuccess();
            SetDropdownToPlaceholder(nameDropdown);
            SetDropdownToPlaceholder(addressDropdown);
        }
        else
        {
            ShowFeedback(incorrectMessage);
            // ❌ ไม่ล้าง options ไม่รีเซ็ตค่า ปล่อยให้ผู้เล่นแก้แล้วส่งใหม่
        }
    }

    void OnFormSuccess()
    {
        tutorialUI.CompleteCurrentByIndex(2);
        StartCoroutine(OpenNext(3));

        if (!receiptPrefab || !receiptSpawnPoint) return;
        Instantiate(receiptPrefab, receiptSpawnPoint.position, receiptPrefab.transform.rotation);
    }

    IEnumerator OpenNext(int idx)
    {
        yield return new WaitForSeconds(0.3f);
        tutorialUI.EnqueueTutorialByIndex(idx);
    }


    // ---------------- Helpers ----------------

    void SetupDropdownsFromCurrentNPC()
    {
        var data = currentNPC ? currentNPC.GetData() : null;
        if (data == null) return;

        // เติมตัวเลือก 4 ตัว + placeholder (ถ้าเปิดใช้)
        PopulateDropdown(nameDropdown, data.nameOptions);
        PopulateDropdown(addressDropdown, data.addressOptions);

        // ตั้งให้เริ่มที่ placeholder
        SetDropdownToPlaceholder(nameDropdown);
        SetDropdownToPlaceholder(addressDropdown);
    }

    void PopulateDropdown(TMP_Dropdown dd, string[] options)
    {
        if (!dd) return;
        dd.ClearOptions();

        var list = new List<string>();
        if (usePlaceholder) list.Add(placeholderText);

        // เติม 4 ตัวเลือก (หรือเท่าที่ส่งมา)
        if (options != null)
        {
            for (int i = 0; i < options.Length && i < 4; i++)
                list.Add(string.IsNullOrEmpty(options[i]) ? "---" : options[i]);
        }

        // เผื่อกรณี options ส่งมาน้อยกว่า 4
        while (list.Count < (usePlaceholder ? 1 + 4 : 4))
            list.Add("---");

        dd.AddOptions(list);
        dd.RefreshShownValue();
    }

    void SetDropdownToPlaceholder(TMP_Dropdown dd)
    {
        if (!dd) return;
        dd.value = usePlaceholder ? 0 : 0; // ถ้าไม่มี placeholder ก็อยู่ที่ตัวแรกของ options
        dd.RefreshShownValue();
    }

    /// <summary>
    /// แปลงค่าที่ผู้ใช้เลือกใน dropdown ให้เป็น index 0..3 ของ "ตัวเลือกจริง"
    /// คืน -1 ถ้ายังอยู่ที่ placeholder หรือค่าไม่ถูกต้อง
    /// </summary>
    int GetSelectedOptionIndex(TMP_Dropdown dd)
    {
        if (!dd) return -1;
        int v = dd.value;
        if (usePlaceholder)
        {
            // value == 0 คือ placeholder → ยังไม่เลือก
            if (v == 0) return -1;
            return v - 1; // map 1..4 → 0..3
        }
        else
        {
            // ไม่มี placeholder: 0..3 คือ 4 ตัวเลือก
            return v; // ถ้าอยากป้องกันเกิน 3 ก็ clamp ได้
        }
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

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

public class ItemDialogueSequenceManager : MonoBehaviour
{
    public static ItemDialogueSequenceManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public TMP_Text speakerText;     // ถ้าไม่ใช้ speaker ให้เว้นว่างได้
    public TMP_Text bodyText;
    public Button[] optionButtons;   // 4 ปุ่ม
    public TMP_Text[] optionLabels;  // 4 Label จับคู่ปุ่ม

    [Header("Typing")]
    public bool enableTyping = true;
    public float charsPerSecond = 40f;

    [Header("Player Control")]
    public FirstPersonController player;

    // runtime
    private ItemDialogueData data;
    private Action<int> onChoice;
    private int lineIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private int activeChoiceCount;   // 2–4
    private int highlightedIndex;    // ตัวเลือกที่ไฮไลต์ (ตอนท้าย)

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    void Start()
    {
        if (!player) player = FindFirstObjectByType<FirstPersonController>();
    }

    public void Show(ItemDialogueData sequence, Action<int> onChoice = null)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
        {
            Debug.LogWarning("[ItemDialogueSequenceManager] Invalid sequence");
            return;
        }

        this.data = sequence;
        this.onChoice = onChoice;
        this.lineIndex = 0;

        // เตรียม UI
        if (panel) panel.SetActive(true);
        if (data.lockPlayer && player) player.isMovementLocked = true;
        if (data.openSfx) AudioSource.PlayClipAtPoint(data.openSfx, Camera.main.transform.position);

        // ซ่อนปุ่มตัวเลือกทั้งหมดก่อน (จะโผล่ตอนจบ)
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(false);
            optionButtons[i].onClick.RemoveAllListeners();
        }

        isShowing = true;

        // แสดงบรรทัดแรก
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (lineIndex >= data.lines.Length)
        {
            // จบทุกบรรทัดแล้ว → แสดงตัวเลือก
            ShowChoices();
            return;
        }

        var line = data.lines[lineIndex];
        if (speakerText) speakerText.text = string.IsNullOrEmpty(line.speaker) ? "" : line.speaker;

        if (enableTyping)
        {
            if (typeCo != null) StopCoroutine(typeCo);
            typeCo = StartCoroutine(TypeLine(line.text, line.voice));
        }
        else
        {
            if (bodyText) bodyText.text = line.text;
            isTyping = false;
        }
    }

    IEnumerator TypeLine(string text, AudioClip voice)
    {
        isTyping = true;
        if (bodyText) bodyText.text = "";

        if (voice) AudioSource.PlayClipAtPoint(voice, Camera.main.transform.position);

        float counter = 0f;
        int shown = 0;

        while (shown < text.Length)
        {
            // Space เพื่อ “ข้ามพิมพ์” ให้เต็มทันที
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (bodyText) bodyText.text = text;
                isTyping = false;
                yield break;
            }

            counter += Time.deltaTime * charsPerSecond;
            int want = Mathf.Clamp(Mathf.FloorToInt(counter), 0, text.Length);
            if (want != shown)
            {
                shown = want;
                if (bodyText) bodyText.text = text.Substring(0, shown);
            }
            yield return null;
        }

        isTyping = false;
    }

    void Update()
    {
        if (!isShowing) return;

        // ยังพิมพ์อยู่ → ให้ coroutine จัดการ Space (skip-to-end)
        if (isTyping) return;

        // อยู่ช่วงบรรทัด (ยังไม่ถึงตัวเลือก)
        if (lineIndex < (data?.lines?.Length ?? 0))
        {
            // Space เพื่อไป "บรรทัดถัดไป"
            if (Input.GetKeyDown(KeyCode.Space))
            {
                lineIndex++;
                ShowCurrentLine();
            }
            return;
        }

        // === อยู่ช่วงตัวเลือก ===
        // เปลี่ยนตัวเลือกด้วยลูกศรซ้าย/ขวา หรือ A/D
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            highlightedIndex = (highlightedIndex - 1 + activeChoiceCount) % activeChoiceCount;
            UpdateChoiceHighlight();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            highlightedIndex = (highlightedIndex + 1) % activeChoiceCount;
            UpdateChoiceHighlight();
        }

        // Space → เลือกตัวเลือกที่ไฮไลต์
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SelectChoice(highlightedIndex);
        }
    }

    void ShowChoices()
    {
        // ไม่มี options → ปิดเลย
        if (data.options == null || data.options.Length < 2)
        {
            Close();
            return;
        }

        // โชว์ปุ่มตามจำนวนจริง (2–4)
        activeChoiceCount = Mathf.Clamp(data.options.Length, 2, 4);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool enable = i < activeChoiceCount;
            optionButtons[i].gameObject.SetActive(enable);

            if (enable)
            {
                if (i < optionLabels.Length) optionLabels[i].text = data.options[i];

                int idx = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SelectChoice(idx));
            }
            else
            {
                optionButtons[i].onClick.RemoveAllListeners();
            }
        }

        highlightedIndex = 0;
        UpdateChoiceHighlight();
    }

    void UpdateChoiceHighlight()
    {
        for (int i = 0; i < activeChoiceCount; i++)
        {
            // เน้นแบบง่าย: select ปุ่ม + ปรับ alpha ของ label
            optionButtons[i].Select();
            if (i < optionLabels.Length)
                optionLabels[i].alpha = (i == highlightedIndex) ? 1f : 0.6f;
        }
    }

    void SelectChoice(int index)
    {
        onChoice?.Invoke(index);
        Close();
    }

    void Close()
    {
        if (panel) panel.SetActive(false);
        if (player) player.isMovementLocked = false;

        isShowing = false;
        isTyping = false;
        if (typeCo != null) { StopCoroutine(typeCo); typeCo = null; }

        data = null;
        onChoice = null;
        lineIndex = 0;
        activeChoiceCount = 0;
        highlightedIndex = 0;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using UnityEngine.SceneManagement;

public class ItemDialogueManager : MonoBehaviour
{
    public static ItemDialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public Button[] optionButtons;
    public TMP_Text[] optionLabels;

    [Header("Typing")]
    public bool enableTyping = true;
    [Tooltip("ตัวอักษร/วินาที")] public float charsPerSecond = 40f;
    [Tooltip("หน่วงเมื่อเจอเครื่องหมายวรรคตอน")] public float punctuationPause = 0.08f;
    [Tooltip("รองรับ <b>, <i>, <color>")] public bool supportRichText = true;
    [Tooltip("เสียงทีละตัว (ออปชัน)")] public AudioClip perCharSfx;
    [Tooltip("ทุก N ตัวอักษรจะเล่นเสียง 1 ครั้ง")] public int sfxEveryNChars = 2;
    KeyCode advanceKey = KeyCode.Mouse0;

    [Header("Player Control")]
    public FirstPersonController player;

    // runtime
    private ItemDialogueData flow;
    private int stepIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private Action<int> onChoice;     // แจ้ง index ปุ่ม (เฉพาะ Choice)
    private Action onFinished;        // Flow จบ
    public bool IsShowing => isShowing;

    // ===== คุณสมบัติเพิ่มตามข้อกำหนด =====
    // เคยคุยมาแล้วหรือยัง (ใช้คุม "รอบแรก/รอบถัดไป")
    public bool hasEverTalked = false;

    // กันคลิกแรกตอนเปิดหน้าต่าง (ดีบาวน์)
    [Header("Debounce")]
    [Tooltip("เวลาหน่วงเพื่อกันคลิกแรกไหลไปข้าม step0")]
    public float advanceCooldown = 0.12f;
    private bool suppressFirstClick = false;
    private float nextAdvanceAllowed = 0f;

    // ความจำตัวเลือกที่ผู้เล่นเคยกด: key = flowName#stepIndex -> choiceIndex
    private readonly Dictionary<string, int> choiceMemory = new Dictionary<string, int>();
    // ใส่ในคลาส ItemDialogueManager
    private bool echoingChoice = false;
    private int pendingGotoIndex = -999; // ค่า sentinel

    string ChoiceKeyFor(int stepIdx)
    {
        string flowId = flow ? flow.name : "noflow";
        return $"{flowId}#{stepIdx}";
    }

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

    public void Show(ItemDialogueData flow, Action<int> onChoice = null, Action onFinished = null)
    {
        if (flow == null || flow.steps == null || flow.steps.Length == 0)
        {
            Debug.LogWarning("[ItemDialogueManager] Invalid flow");
            return;
        }

        this.flow = flow;
        this.onChoice = onChoice;
        this.onFinished = onFinished;

        // เริ่มใหม่ทุกครั้งจาก stepIndex 0
        stepIndex = 0;

        if (panel) panel.SetActive(true);

        if (flow.lockPlayer && player) player.isMovementLocked = true;
        Cursor.lockState = CursorLockMode.None;

        if (flow.openSfx && Camera.main)
            AudioSource.PlayClipAtPoint(flow.openSfx, Camera.main.transform.position);

        HideAllChoices();

        // รอบแรก -> เปิดพิมพ์ทีละตัว, รอบถัดไป -> ปิดพิมพ์ (โชว์ทันที)
        enableTyping = !hasEverTalked;

        // ดีบาวน์กันคลิกแรกไหลไปข้าม step0
        suppressFirstClick = true;
        nextAdvanceAllowed = Time.unscaledTime + advanceCooldown;

        isShowing = true;
        ShowCurrentStep();
    }

    void HideAllChoices()
    {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (!optionButtons[i]) continue;
            optionButtons[i].gameObject.SetActive(false);
            optionButtons[i].onClick.RemoveAllListeners();
        }
    }

    bool HasChoices(ItemDialogueData.Step step)
    {
        return step != null && step.options != null && step.options.Length >= 2;
    }

    void ShowCurrentStep()
    {
        if (flow == null || flow.steps == null) { Close(); return; }
        if (stepIndex < 0 || stepIndex >= flow.steps.Length)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        var step = flow.steps[stepIndex];
        HideAllChoices();

        // Speaker & Text header
        if (speakerText) speakerText.text = string.IsNullOrEmpty(step.speaker) ? "" : step.speaker;

        // ====== ถ้าเป็น "รอบถัดไป" และ step นี้เป็น Choice -> ข้ามปุ่ม ไปตามความจำ ======
        if (hasEverTalked && HasChoices(step))
        {
            string key = ChoiceKeyFor(stepIndex);
            bool found = choiceMemory.TryGetValue(key, out int savedIdx);
            if (!found || savedIdx < 0 || savedIdx >= step.options.Length)
                savedIdx = 0;

            EchoChoiceThenGoto(step.options[savedIdx].text, step.options[savedIdx].gotoIndex);

            return;
        }

        // ====== Line (ไม่มี Choice) ======
        if (!HasChoices(step))
        {
            if (enableTyping)
            {
                Cursor.visible = false;
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
                    // ยิง Action หลังพิมพ์จบ (ถ้ามี)
                    if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                        StartCoroutine(InvokeAfterDelay(() => ExecuteLineEndAction(step), Mathf.Max(0f, step.onLineEndDelay)));

                    // terminal line
                    if (step.gotoIndex < 0)
                    {
                        if (step.onLineEndAction == ItemDialogueData.LineAction.None)
                        {
                            // รอกดเพื่อปิด (ใน Update)
                        }
                        else
                        {
                            Close();
                            onFinished?.Invoke();
                            return;
                        }
                    }
                }));
            }
            else
            {
                Cursor.visible = false;
                if (bodyText) bodyText.text = step.text ?? "";
                isTyping = false;

                if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                    StartCoroutine(InvokeAfterDelay(() => ExecuteLineEndAction(step), Mathf.Max(0f, step.onLineEndDelay)));

                if (step.gotoIndex < 0)
                {
                    if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                    {
                        Close();
                        onFinished?.Invoke();
                        return;
                    }
                }
            }
        }
        else // ====== Choice (เฉพาะ "รอบแรก" เท่านั้นที่มาถึงจุดนี้) ======
        {
            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
                    Cursor.visible = true;
                    ShowChoices(step.options);
                }));
            }
            else
            {
                if (bodyText) bodyText.text = step.text ?? "";
                Cursor.visible = true;
                ShowChoices(step.options);
            }
        }
    }

    IEnumerator TypeLine(string text, AudioClip voice, Action onTypedDone = null)
    {
        isTyping = true;
        if (bodyText) bodyText.text = "";

        if (voice && Camera.main)
            AudioSource.PlayClipAtPoint(voice, Camera.main.transform.position);

        text ??= "";
        int i = 0;
        float secPerChar = (charsPerSecond <= 0f) ? 0f : (1f / charsPerSecond);

        while (i < text.Length)
        {
            if (supportRichText && text[i] == '<')
            {
                int closeIdx = text.IndexOf('>', i);
                if (closeIdx == -1) closeIdx = i;
                Append(text.Substring(i, closeIdx - i + 1));
                i = closeIdx + 1;
            }
            else
            {
                Append(text[i].ToString());
                i++;

                if (perCharSfx && sfxEveryNChars > 0 && (i % sfxEveryNChars == 0) && Camera.main)
                    AudioSource.PlayClipAtPoint(perCharSfx, Camera.main.transform.position, 0.7f);

                if (secPerChar > 0f) yield return new WaitForSeconds(secPerChar);
                if (punctuationPause > 0f && IsPunc(text[i - 1]))
                    yield return new WaitForSeconds(punctuationPause);
            }
        }

        isTyping = false;
        onTypedDone?.Invoke();
    }

    void Append(string s)
    {
        if (bodyText) bodyText.text += s;
    }

    bool IsPunc(char c)
    {
        return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == '…' || c == '，' || c == '。';
    }

    void ShowChoices(ItemDialogueData.ChoiceOption[] options)
    {
        // รอบถัดไปปกติจะไม่เข้าฟังก์ชันนี้เพราะถูก auto-route ไปแล้วใน ShowCurrentStep()
        if (options == null || options.Length < 2)
        {
            GoTo(flow.steps[stepIndex].gotoIndex);
            return;
        }

        int count = Mathf.Clamp(options.Length, 2, 4);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool enable = i < count;
            if (!optionButtons[i]) continue;

            optionButtons[i].gameObject.SetActive(enable);
            optionButtons[i].onClick.RemoveAllListeners();

            if (enable)
            {
                if (i < optionLabels.Length && optionLabels[i])
                    optionLabels[i].text = options[i].text ?? "";

                int idx = i;
                optionButtons[i].onClick.AddListener(() =>
                {
                    for (int k = 0; k < optionButtons.Length; k++)
                        if (optionButtons[k]) optionButtons[k].interactable = false;

                    onChoice?.Invoke(idx);

                    // จำตัวเลือก
                    string key = ChoiceKeyFor(stepIndex);
                    if (choiceMemory.ContainsKey(key)) choiceMemory[key] = idx;
                    else choiceMemory.Add(key, idx);

                    EchoChoiceThenGoto(options[idx].text, options[idx].gotoIndex);

                });


                optionButtons[i].interactable = true;
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (!isShowing) return;

        // 1) กันคลิกค้าง
        if (suppressFirstClick)
        {
            if (Input.GetKeyUp(KeyCode.Mouse0) || Time.unscaledTime >= nextAdvanceAllowed)
                suppressFirstClick = false;
            else
                return;
        }

        // 2) โหมด echo มาก่อน isTyping
        if (echoingChoice)
        {
            // ยังพิมพ์ echo อยู่ -> รอให้พิมพ์จบก่อน
            if (isTyping) return;

            // พิมพ์จบแล้ว -> รอคลิกไปต่อ
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                int target = pendingGotoIndex;
                echoingChoice = false;
                pendingGotoIndex = -999;
                GoTo(target);
            }
            return;
        }

        // 3) จากนี้ค่อยเช็คสถานะพิมพ์ทั่วไป
        if (isTyping) return;

        if (flow == null || flow.steps == null) return;
        if (stepIndex < 0 || stepIndex >= flow.steps.Length) return;

        var step = flow.steps[stepIndex];

        if (!HasChoices(step))
        {
            if (Input.GetKeyDown(advanceKey))
                GoTo(step.gotoIndex);
        }
    }



    void GoTo(int gotoIndex)
    {
        // -1 (หรือต่ำกว่า) = จบไดอะล็อกทันที
        if (gotoIndex < 0)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        // ค่าอื่น ๆ: กระโดดถ้าอยู่ในช่วง
        if (gotoIndex >= 0 && gotoIndex < (flow?.steps?.Length ?? 0))
        {
            stepIndex = gotoIndex;
            ShowCurrentStep();
        }
        else
        {
            // นอกช่วง → เลือกจบ
            Close();
            onFinished?.Invoke();
        }
    }

    IEnumerator InvokeAfterDelay(Action act, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        act?.Invoke();
    }

    void ExecuteLineEndAction(ItemDialogueData.Step step)
    {
        if (step == null) return;

        switch (step.onLineEndAction)
        {
            case ItemDialogueData.LineAction.None:
                break;

            case ItemDialogueData.LineAction.NPCExit:
                {
                    var npc = FindFirstObjectByType<NPC>();
                    if (!npc) { Debug.LogWarning("LineEndAction: No NPC found."); break; }
                    var item = FindFirstObjectByType<ItemScript>();
                    GameObject itemGo = item ? item.gameObject : null;
                    npc.ForceExitAndClearItem(itemGo);
                    break;
                }

            case ItemDialogueData.LineAction.PolicePay:
                {
                    var gm = FindFirstObjectByType<GameManager>();
                    if (gm)
                    {
                        gm.totalCaughtPercent = 0;      // reset
                        gm.SpendMoney(500);            // หักเงินตามระบบใหม่ (current->bank)
                        gm.UpdateSalesUI();
                        Debug.Log("[Police] Paid bribe: -500, reset caught percent.");
                    }
                    break;
                }

            case ItemDialogueData.LineAction.KillPlayer:
                {
                    var gm = FindFirstObjectByType<GameManager>();
                    if (gm)
                    {
                        Debug.Log("[Police] Refused to pay. Game over.");
                        SceneManager.LoadScene("DirSeceneManager");
                    }
                    break;
                }
        }
    }
    void EchoChoiceThenGoto(string choiceText, int gotoIndex)
    {
        HideAllChoices();

        echoingChoice = true;
        pendingGotoIndex = gotoIndex;

        Cursor.visible = true;

        // หน่วงกันคลิกค้าง (ทั้งสองกรณี)
        suppressFirstClick = true;
        nextAdvanceAllowed = Time.unscaledTime + advanceCooldown;

        if (!hasEverTalked && enableTyping)
        {
            // รอบแรก: เล่นอนิเมชันพิมพ์ echo ให้จบก่อน
            if (typeCo != null) StopCoroutine(typeCo);
            typeCo = StartCoroutine(TypeLine(
                choiceText ?? "",
                null, // ไม่จำเป็นต้องมีเสียงก็ได้
                onTypedDone: () =>
                {
                    // พิมพ์จบแล้ว ยังอยู่ในโหมด echo รอคลิกไปต่อ
                    // (ไม่ต้องทำอะไรเพิ่ม ปล่อยให้ Update() จัดการต่อ)
                }
            ));
        }
        else
        {
            // รอบถัดไป: แสดงทันที ไม่มีอนิเมชัน
            isTyping = false;
            if (bodyText) bodyText.text = choiceText ?? "";
        }
    }
    public void Close()
    {
        if (panel) panel.SetActive(false);

        if (player) player.isMovementLocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isShowing = false;
        isTyping = false;
        if (typeCo != null) { StopCoroutine(typeCo); typeCo = null; }

        flow = null;
        stepIndex = 0;
        onChoice = null;
        onFinished = null;

        // ปิดรอบแรก
        hasEverTalked = true;
    }
}

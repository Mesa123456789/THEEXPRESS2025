using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

public class ItemDialogueManager : MonoBehaviour
{
    public static ItemDialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public Button[] optionButtons;    // วาง 2–4 ปุ่ม
    public TMP_Text[] optionLabels;   // จับคู่ปุ่ม

    [Header("Typing")]
    public bool enableTyping = true;
    [Tooltip("ตัวอักษร/วินาที")] public float charsPerSecond = 40f;
    [Tooltip("หน่วงเมื่อเจอเครื่องหมายวรรคตอน")] public float punctuationPause = 0.08f;
    [Tooltip("รองรับ <b>, <i>, <color>")] public bool supportRichText = true;
    [Tooltip("เสียงทีละตัว (ออปชัน)")] public AudioClip perCharSfx;
    [Tooltip("ทุก N ตัวอักษรจะเล่นเสียง 1 ครั้ง")] public int sfxEveryNChars = 2;
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Player Control")]
    public FirstPersonController player;

    // runtime
    private ItemDialogueData flow;
    private int stepIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private Action<int> onChoice;     // แจ้ง index ปุ่มที่กด (เฉพาะ Choice)
    private Action onFinished;        // Flow จบ

    public bool IsShowing => isShowing;

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

    /// <summary>แสดงบทสนทนา</summary>
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
        stepIndex = 0;

        if (panel) panel.SetActive(true);

        if (flow.lockPlayer && player) player.isMovementLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (flow.openSfx && Camera.main)
            AudioSource.PlayClipAtPoint(flow.openSfx, Camera.main.transform.position);

        HideAllChoices();

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

        if (!HasChoices(step)) // ===== Line =====
        {
            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
                    // ยิง Action หลังพิมพ์จบ (ถ้ามี)
                    if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                        StartCoroutine(InvokeAfterDelay(() => ExecuteLineEndAction(step), Mathf.Max(0f, step.onLineEndDelay)));

                    // จบหรือไม่?
                    if (step.gotoIndex < 0)
                    {
                        if (step.onLineEndAction == ItemDialogueData.LineAction.None)
                        {
                            // ไม่มีแอ็กชัน → รอ Space ใน Update() เพื่อปิด
                            // (ไม่ทำอะไรที่นี่)
                        }
                        else
                        {
                            // มีแอ็กชัน → ปิดอัตโนมัติหลังจบประโยค
                            Close();
                            onFinished?.Invoke();
                            return;
                        }
                    }
                }));
            }
            else
            {
                if (bodyText) bodyText.text = step.text ?? "";
                isTyping = false;

                // ยิง Action หลังแสดงจบ (ถ้ามี)
                if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                    StartCoroutine(InvokeAfterDelay(() => ExecuteLineEndAction(step), Mathf.Max(0f, step.onLineEndDelay)));

                // จบหรือไม่?
                if (step.gotoIndex < 0)
                {
                    if (step.onLineEndAction == ItemDialogueData.LineAction.None)
                    {
                        // ไม่มีแอ็กชัน → รอ Space ใน Update() เพื่อปิด
                    }
                    else
                    {
                        // มีแอ็กชัน → ปิดอัตโนมัติ
                        Close();
                        onFinished?.Invoke();
                        return;
                    }
                }
            }
        }
        else // ===== Choice =====
        {
            // แสดงข้อความหัว Choice แล้วค่อยโชว์ปุ่ม
            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
                    ShowChoices(step.options);
                }));
            }
            else
            {
                if (bodyText) bodyText.text = step.text ?? "";
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
        if (options == null || options.Length < 2)
        {
            // ไม่มีตัวเลือก → ถือว่าเป็น Line แล้วไปต่อ (ตาม gotoIndex ของ Step ปัจจุบัน)
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
                    // กันดับเบิลคลิก
                    for (int k = 0; k < optionButtons.Length; k++)
                        if (optionButtons[k]) optionButtons[k].interactable = false;

                    onChoice?.Invoke(idx);

                    // แตกกิ่งด้วย gotoIndex ของปุ่ม
                    GoTo(options[idx].gotoIndex);
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
        if (isTyping) return; // ระหว่างพิมพ์ Space ไม่มีผล

        if (flow == null || flow.steps == null) return;
        if (stepIndex < 0 || stepIndex >= flow.steps.Length) return;

        var step = flow.steps[stepIndex];

        // Line: Space เพื่อไป gotoIndex (หรือ -1 เพื่อปิด)
        if (!HasChoices(step))
        {
            if (Input.GetKeyDown(advanceKey))
                GoTo(step.gotoIndex);
        }
        // Choice: ไม่ใช้ Space (รอคลิกปุ่ม)
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
                    // หา NPC (มีตัวเดียว)
                    var npc = FindFirstObjectByType<NPC>();
                    if (!npc) { Debug.LogWarning("LineEndAction: No NPC found."); break; }

                    // ถ้าต้องการลบของด้วย: หา ItemScript ตัวแรก
                    var item = FindFirstObjectByType<ItemScript>();
                    GameObject itemGo = item ? item.gameObject : null;

                    npc.ForceExitAndClearItem(itemGo);
                    break;
                }
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
    }
}

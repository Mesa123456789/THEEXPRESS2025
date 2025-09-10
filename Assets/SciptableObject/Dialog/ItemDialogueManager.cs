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
    public Button[] optionButtons;   // วาง 4 ปุ่ม
    public TMP_Text[] optionLabels;  // วาง 4 TMP_Text จับคู่ปุ่ม

    [Header("Typing")]
    public bool enableTyping = true;
    [Tooltip("ตัวอักษร/วินาที")]
    public float charsPerSecond = 40f;
    [Tooltip("หยุดเพิ่มตอนพบ .,!?…")]
    public float punctuationPause = 0.08f;
    [Tooltip("รองรับ Rich Text (<b>, <i>, <color>)")]
    public bool supportRichText = true;
    [Tooltip("เสียงทีละตัว (ออปชัน)")]
    public AudioClip perCharSfx;
    [Tooltip("ทุก N ตัวอักษรจะเล่นเสียง 1 ครั้ง")]
    public int sfxEveryNChars = 2;
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Player Control")]
    public FirstPersonController player;

    // runtime
    private ItemDialogueData flow;
    private int stepIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private Action<int> onChoice;     // callback ส่ง index ของตัวเลือกทุกครั้งที่มี Choice
    private Action onFinished;        // (ออปชัน) ถ้าต้องการรู้ว่า flow จบแล้ว

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
        if (flow == null || flow.steps.Length == 0)
        {
            Debug.LogWarning("[ItemDialogueFlowManager] Invalid flow");
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

        if (flow.openSfx) AudioSource.PlayClipAtPoint(flow.openSfx, Camera.main.transform.position);

        HideAllChoices();

        isShowing = true;
        ShowCurrentStep();
    }

    void HideAllChoices()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(false);
            optionButtons[i].onClick.RemoveAllListeners();
        }
    }

    void ShowCurrentStep()
    {
        if (stepIndex >= flow.steps.Length)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        var step = flow.steps[stepIndex];
        HideAllChoices();

        if (step.type == ItemDialogueData.StepType.Line)
        {
            if (speakerText) speakerText.text = string.IsNullOrEmpty(step.speaker) ? "" : step.speaker;

            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice));
            }
            else
            {
                if (bodyText) bodyText.text = step.text;
                isTyping = false;
            }
        }
        else // Choice
        {
            if (speakerText) speakerText.text = "";
            if (bodyText) bodyText.text = ""; // หรือใส่หัวข้อเองก็ได้
            ShowChoices(step.options);
        }
    }

    IEnumerator TypeLine(string text, AudioClip voice)
    {
        isTyping = true;
        if (bodyText) bodyText.text = "";

        if (voice) AudioSource.PlayClipAtPoint(voice, Camera.main.transform.position);

        int i = 0;
        float secPerChar = (charsPerSecond <= 0f) ? 0f : (1f / charsPerSecond);

        while (i < text.Length)
        {
            // *** ไม่ให้ Space ข้ามระหว่างพิมพ์ ***
            // Space มีผลเฉพาะหลังพิมพ์จบ (ไปสเต็ปถัดไป)

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

                if (perCharSfx && sfxEveryNChars > 0 && (i % sfxEveryNChars == 0))
                    AudioSource.PlayClipAtPoint(perCharSfx, Camera.main.transform.position, 0.7f);

                if (secPerChar > 0f) yield return new WaitForSeconds(secPerChar);

                if (punctuationPause > 0f && IsPunc(text[i - 1]))
                    yield return new WaitForSeconds(punctuationPause);
            }
        }

        isTyping = false;
    }

    void Append(string s)
    {
        if (bodyText) bodyText.text += s;
    }

    bool IsPunc(char c)
    {
        return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == '…' || c == '，' || c == '。';
    }

    void ShowChoices(string[] options)
    {
        if (options == null || options.Length < 2)
        {
            Debug.Log("ไม่มีตัวเลือก");
            stepIndex++;
            ShowCurrentStep();
            return;
        }

        int count = Mathf.Clamp(options.Length, 2, 4);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool enable = i < count;
            optionButtons[i].gameObject.SetActive(enable);
            optionButtons[i].onClick.RemoveAllListeners();

            if (enable)
            {
                if (i < optionLabels.Length) optionLabels[i].text = options[i];

                int idx = i;
                optionButtons[i].onClick.AddListener(() =>
                {
                    // แจ้ง choiceIdx ออกทุกครั้งที่มีการเลือก
                    onChoice?.Invoke(idx);

                    // ไม่แตกกิ่ง → ไปสเต็ปถัดไปตามลำดับ
                    stepIndex++;
                    ShowCurrentStep();
                });
            }
        }
    }

    void Update()
    {
        if (!isShowing) return;
        if (isTyping) return; // ระหว่างพิมพ์ Space ไม่มีผล

        // สเต็ปเป็น Line และพิมพ์จบแล้ว → Space ไปสเต็ปถัดไป
        if (stepIndex < (flow?.steps?.Length ?? 0))
        {
            var step = flow.steps[stepIndex];
            if (step.type == ItemDialogueData.StepType.Line)
            {
                if (Input.GetKeyDown(advanceKey))
                {
                    stepIndex++;
                    ShowCurrentStep();
                }
            }
        }
    }

    void Close()
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

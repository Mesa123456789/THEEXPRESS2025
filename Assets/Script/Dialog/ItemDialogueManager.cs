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

    private ItemDialogueData flow;
    private int stepIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private Action<int> onChoice;
    private Action onFinished;
    public bool IsShowing => isShowing;

    public bool hasEverTalked = false;

    [Header("Debounce")]
    [Tooltip("เวลาหน่วงเพื่อกันคลิกแรกไหลไปข้าม step0")]
    public float advanceCooldown = 0.12f;
    private bool suppressFirstClick = false;
    private float nextAdvanceAllowed = 0f;

    // choice ที่ผู้เล่นเลือกไว้: key = flowName#stepIndex -> choiceIndex
    private readonly Dictionary<string, int> choiceMemory = new Dictionary<string, int>();

    private bool echoingChoice = false;
    private int pendingGotoIndex = -999;

    // cursor control
    private bool isChoiceActive = false;

    // โหมดทวน (ครั้งเดียวต่อการเรียก ShowReview)
    private bool reviewMode = false;
    private readonly HashSet<int> talkedActorIds = new HashSet<int>();
    private int currentActorId = 0;

    // เรียกจากภายนอกเมื่อนักแสดงถูกทำลาย เพื่อ "ลืม" สถานะของอินสแตนซ์นั้น
    public void ForgetActor(int actorInstanceId)
    {
        talkedActorIds.Remove(actorInstanceId);
        if (currentActorId == actorInstanceId) currentActorId = 0;
    }

    // Helper: ใช้ตอนเปิดบทสนทนา เพื่อกำหนดว่าเป็นครั้งแรกของอินสแตนซ์นี้หรือไม่
    private bool IsFirstTimeForActor(GameObject actor)
    {
        currentActorId = actor ? actor.GetInstanceID() : 0;
        return currentActorId == 0 ? true : !talkedActorIds.Contains(currentActorId);
    }
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

        // ลืมทุกช้อยส์เสมอ (ไม่พกความจำข้ามการทำลายออบเจกต์/ซีน)
        choiceMemory.Clear();
    }


    void Start()
    {
        SetCursor(false);
        if (!player) player = FindFirstObjectByType<FirstPersonController>();
    }

    void SetCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;

        var sai = player ? player.GetComponent<StarterAssetsInputs>() : null;
        if (sai) sai.cursorInputForLook = !show;
    }

    void ResetSessionState(bool clearChoices)
    {
        hasEverTalked = false;
        echoingChoice = false;
        pendingGotoIndex = -999;
        suppressFirstClick = false;
        isTyping = false;
        isChoiceActive = false;
        if (typeCo != null) { StopCoroutine(typeCo); typeCo = null; }
        if (clearChoices) choiceMemory.Clear();
    }

    // เรียกรอบปกติ (อนิเมชัน + เลือกช้อยส์ได้)
    public void Show(GameObject actorOwner, ItemDialogueData flow,
                     Action<int> onChoice = null, Action onFinished = null)
    {
        bool firstTime = IsFirstTimeForActor(actorOwner);

        // ครั้งแรก = เล่นอนิเมชัน + ช้อยส์
        // ครั้งถัดไป = ไม่เล่นอนิเมชัน + ไม่โชว์ช้อยส์ (เล่นแต่บรรทัด)
        bool forceReview = !firstTime;

        InternalShow(flow, onChoice, onFinished, forceReview);
    }



    // เรียกรอบทวน (ไม่อนิเมชัน + ไม่โชว์ช้อยส์/ไม่เปิด cursor + ใช้ช้อยส์เดิม)
    public void ShowReview(ItemDialogueData flow, Action<int> onChoice = null, Action onFinished = null)
    {
        InternalShow(flow, onChoice, onFinished, forceReview: true);
    }

    void InternalShow(ItemDialogueData flow, Action<int> onChoice, Action onFinished, bool forceReview)
    {
        if (flow == null || flow.steps == null || flow.steps.Length == 0)
        {
            Debug.LogWarning("[ItemDialogueManager] Invalid flow");
            return;
        }

        reviewMode = forceReview;

        // รอบปกติ → ล้าง choiceMemory (เริ่มใหม่จริง ๆ)
        // รอบทวน → ไม่ล้าง (ต้องมีช้อยส์เดิมไว้ใช้งาน)
        // ไม่ว่าจะโหมดไหน เคลียร์ช้อยส์/สเตตเสมอ (ห้ามจำ)
        ResetSessionState(clearChoices: true);

        this.flow = flow;
        this.onChoice = onChoice;
        this.onFinished = onFinished;
        stepIndex = 0;

        if (panel) panel.SetActive(true);
        if (flow.lockPlayer && player) player.isMovementLocked = true;
        if (flow.openSfx && Camera.main) AudioSource.PlayClipAtPoint(flow.openSfx, Camera.main.transform.position);

        HideAllChoices();

        if (forceReview)
        {
            // รอบทวน: ไม่เล่นอนิเมชันตัวอักษร, ถือว่าเคยคุยแล้ว, ไม่เปิดเคอร์เซอร์
            enableTyping = false;
            hasEverTalked = true;
            SetCursor(false);
        }
        else
        {
            enableTyping = true;
            hasEverTalked = false;
        }

        suppressFirstClick = true;
        nextAdvanceAllowed = Time.unscaledTime + advanceCooldown;

        isShowing = true;
        reviewMode = forceReview;
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

        if (speakerText) speakerText.text = string.IsNullOrEmpty(step.speaker) ? "" : step.speaker;

        // ถ้าเป็น Choice และอยู่ในโหมดทวน → ข้ามปุ่ม, ซ่อน cursor, ไปตามช้อยส์เดิมทันที
        if (HasChoices(step) && reviewMode)
        {
            // ครั้งถัดไป: ไม่โชว์ช้อยส์, ไม่ echo ข้อความช้อยส์, ไม่เปิดเคอร์เซอร์
            isChoiceActive = false;
            SetCursor(false);

            // เลือกเส้นทางเริ่มต้น: ถ้ามี options ให้ใช้ options[0].gotoIndex
            // ถ้าไม่มี (ป้องกันกรณีข้อมูลผิดรูป) ใช้ step.gotoIndex
            int target = step.options != null && step.options.Length > 0
                ? step.options[0].gotoIndex
                : step.gotoIndex;

            GoTo(target);
            return;
        }


        if (!HasChoices(step))
        {
            isChoiceActive = false;
            SetCursor(false);

            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
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
                }));
            }
            else
            {
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
        else
        {
            // โหมดปกติเท่านั้นที่จะมาถึงตรงนี้ (reviewMode ถูก return ไปแล้วด้านบน)
            isChoiceActive = true;
            SetCursor(true);

            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
                    isChoiceActive = true;
                    SetCursor(true);
                    ShowChoices(step.options);
                }));
            }
            else
            {
                if (bodyText) bodyText.text = step.text ?? "";
                isChoiceActive = true;
                SetCursor(true);
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

    //public void ShowFromItem(ItemScript item, System.Action<int> onChoice = null, System.Action onFinished = null)
    //{
    //    if (item == null || item.itemData == null || item.itemData.dialogueData == null)
    //    {
    //        Debug.LogWarning("[ItemDialogueManager] No dialogueData on itemData.");
    //        return;
    //    }
    //    Show(item.itemData.dialogueData, onChoice, onFinished);
    //}

    void ShowChoices(ItemDialogueData.ChoiceOption[] options)
    {
        isChoiceActive = true;
        SetCursor(true);

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

                    // เก็บช้อยส์ไว้ใช้ในรอบทวน
                    string key = ChoiceKeyFor(stepIndex);
                    if (choiceMemory.ContainsKey(key)) choiceMemory[key] = idx;
                    else choiceMemory.Add(key, idx);

                    isChoiceActive = false;
                    SetCursor(false);

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

        if (isChoiceActive)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                SetCursor(true);
        }

        if (suppressFirstClick)
        {
            if (Input.GetKeyUp(KeyCode.Mouse0) || Time.unscaledTime >= nextAdvanceAllowed)
                suppressFirstClick = false;
            else
                return;
        }

        if (echoingChoice)
        {
            if (isTyping) return;

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                int target = pendingGotoIndex;
                echoingChoice = false;
                pendingGotoIndex = -999;
                GoTo(target);
            }
            return;
        }

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
        if (gotoIndex < 0)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        if (gotoIndex >= 0 && gotoIndex < (flow?.steps?.Length ?? 0))
        {
            stepIndex = gotoIndex;
            ShowCurrentStep();
        }
        else
        {
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
                        gm.totalCaughtPercent = 0;
                        gm.SpendMoney(500);
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

        isChoiceActive = false;
        SetCursor(false);

        suppressFirstClick = true;
        nextAdvanceAllowed = Time.unscaledTime + advanceCooldown;

        if (!hasEverTalked && enableTyping)
        {
            if (typeCo != null) StopCoroutine(typeCo);
            typeCo = StartCoroutine(TypeLine(
                choiceText ?? "",
                null,
                onTypedDone: () => { }
            ));
        }
        else
        {
            isTyping = false;
            if (bodyText) bodyText.text = choiceText ?? "";
        }
    }

    public void Close()
    {
        if (panel) panel.SetActive(false);

        if (player) player.isMovementLocked = false;
        isChoiceActive = false;
        SetCursor(false);

        isShowing = false;
        isTyping = false;
        //if (typeCo != null) { StopCoroutine(typeCo); typeCo = null; }
        if (currentActorId != 0)
            talkedActorIds.Add(currentActorId);
        flow = null;
        stepIndex = 0;
        onChoice = null;
        onFinished = null;

        // รอบถัดไปให้ถือว่า "คุยมาแล้ว"
        hasEverTalked = true;

        // โหมดทวนใช้ครั้งต่อครั้ง
        reviewMode = false;
    }

}

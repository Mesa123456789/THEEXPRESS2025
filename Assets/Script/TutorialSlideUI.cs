using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TutorialSlideUIQueue : MonoBehaviour
{
    public RectTransform panel;
    public TextMeshProUGUI tutorialText;
    public Graphic checkIcon;

    public Vector2 hiddenPosition = new Vector2(600f, 0f);
    public Vector2 shownPosition = new Vector2(0f, 0f);
    public float slideDuration = 0.45f;

    [Range(0f, 1f)] public float checkIconOpacityIdle = 0.35f;
    public float delayAfterComplete = 0.8f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public List<string> tutorialMessages = new List<string>();

    // ---- Gate (ปิดถาวรข้ามซีน) ----
    public const string KEY_TUTORIAL_DISABLED = "TUTORIAL_DISABLED";
    static bool IsDisabled() => PlayerPrefs.GetInt(KEY_TUTORIAL_DISABLED, 0) == 1;

    Queue<string> _queue = new Queue<string>();
    Queue<int> _queueIdx = new Queue<int>();
    HashSet<int> _pendingIndices = new HashSet<int>();
    bool _isVisible = false;
    bool _isAnimating = false;
    string _currentMessage = null;
    Coroutine _running;
    int _currentIndex = -1;
    int _lastCompletedIndex = -1;

    public int CurrentIndex => _currentIndex;
    public int LastCompletedIndex => _lastCompletedIndex;

    void Awake()
    {
        if (panel) panel.anchoredPosition = hiddenPosition;
        SetCheckIconAlpha(checkIconOpacityIdle);

        // ถ้าปิดถาวรแล้ว ให้ซ่อนและ disable ทันที
        if (IsDisabled())
        {
            ClearQueueAndHideImmediate();
            enabled = false;
        }
    }

    public void EnqueueTutorialByIndex(int index)
    {
        if (IsDisabled()) return;

        if (index < 0 || index >= tutorialMessages.Count)
        {
            Debug.LogWarning($"TutorialSlideUIQueue: invalid index {index}");
            return;
        }
        if (index <= _lastCompletedIndex) return;        // กันย้อน
        if (index == _currentIndex || _pendingIndices.Contains(index)) return;

        string msg = tutorialMessages[index];
        _queue.Enqueue(msg);
        _queueIdx.Enqueue(index);
        _pendingIndices.Add(index);
        TryStartNext();
    }

    public void CompleteCurrentByIndex(int index)
    {
        if (IsDisabled()) return;
        if (index != _currentIndex || string.IsNullOrEmpty(_currentMessage)) return;

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Co_CompleteThenHide());
    }

    public void ClearQueueAndHideImmediate()
    {
        _queue.Clear();
        _queueIdx.Clear();
        _pendingIndices.Clear();
        _currentMessage = null;
        _currentIndex = -1;
        if (_running != null) StopCoroutine(_running);
        _isAnimating = false;
        _isVisible = false;
        if (panel) panel.anchoredPosition = hiddenPosition;
        SetCheckIconAlpha(checkIconOpacityIdle);
    }

    void TryStartNext()
    {
        if (_isAnimating || _isVisible) return;
        if (_queue.Count == 0) return;

        _currentMessage = _queue.Dequeue();
        _currentIndex = _queueIdx.Dequeue();
        _pendingIndices.Remove(_currentIndex);

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Co_Show(_currentMessage));
    }

    IEnumerator Co_Show(string message)
    {
        _isAnimating = true;
        SetCheckIconAlpha(checkIconOpacityIdle);
        if (tutorialText) tutorialText.text = message;
        yield return Move(panel, panel.anchoredPosition, shownPosition, slideDuration);
        _isVisible = true;
        _isAnimating = false;
    }

    IEnumerator Co_CompleteThenHide()
    {
        _isAnimating = true;
        SetCheckIconAlpha(1f);
        yield return new WaitForSeconds(delayAfterComplete);
        yield return Move(panel, panel.anchoredPosition, hiddenPosition, slideDuration);

        _isVisible = false;
        _isAnimating = false;
        if (_currentIndex > _lastCompletedIndex) _lastCompletedIndex = _currentIndex;
        _currentMessage = null;
        _currentIndex = -1;

        TryStartNext();
    }

    IEnumerator Move(RectTransform rt, Vector2 from, Vector2 to, float dur)
    {
        if (!rt) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, dur);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    void SetCheckIconAlpha(float a)
    {
        if (!checkIcon) return;
        var c = checkIcon.color; c.a = Mathf.Clamp01(a);
        checkIcon.color = c;
    }

    // ปิดถาวรทั้งเกม (เรียกตอน Sleep Now)
    public static void DisableForeverAndHideAll()
    {
        PlayerPrefs.SetInt(KEY_TUTORIAL_DISABLED, 1);
        PlayerPrefs.Save();

        foreach (var ui in FindObjectsOfType<TutorialSlideUIQueue>())
        {
            ui.ClearQueueAndHideImmediate();
            ui.enabled = false;
        }
    }
}

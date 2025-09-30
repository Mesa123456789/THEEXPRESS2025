using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AddSalesPopupUI : MonoBehaviour
{
    public static AddSalesPopupUI Instance;

    [Header("Refs")]
    public TextMeshProUGUI priceText;           // ��ͤ����Ҥҷ�����
    public RectTransform rect;                   // RectTransform �ͧ����ͧ
    [Tooltip("�����������Ѻ�ҧ popup (��ͧ���١�ͧ Canvas ��� '�����' LayoutGroup)")]
    public RectTransform popupLayer;
    [Tooltip("�ش��ҧ�ԧ��������駢�鹨ҡ�ç��� (�� �ͤ͹�Թ/����ʹ�Թ)")]
    public RectTransform anchorAt;

    [Header("Motion")]
    public Vector2 startOffset = new Vector2(0f, -20f);   // �������ӡ��Ҩش��ҧ�ԧ�Դ�֧
    public Vector2 endOffset = new Vector2(0f, 40f);    // ��Ѻ���
    public float moveDuration = 0.6f;                    // ��������͹���
    public float holdDuration = 0.35f;                   // ��ҧ���
    public float fadeDuration = 0.35f;                   // �ҧ���

    CanvasGroup cg;
    Coroutine co;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);

        if (!rect) rect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        // �ѹⴹ layout ����: ����� LayoutElement ������ ignoreLayout
        var le = GetComponent<LayoutElement>();
        if (!le) le = gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        HideImmediate();
    }

    void HideImmediate()
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (priceText) priceText.text = "";
    }

    public static void ShowNotice(int amount)
    {
        if (Instance == null) return;
        Instance.InternalShow(amount);
    }

    void InternalShow(int amount)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Animate(amount));
    }

    Vector2 GetAnchorScreenPos()
    {
        // �������� anchor ����˹觡�ҧ popupLayer
        if (anchorAt == null)
        {
            var cam = popupLayer != null && popupLayer.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : Camera.main;
            // ��ҧ��
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        var canvas = anchorAt.GetComponentInParent<Canvas>();
        Camera camForUI = null;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            camForUI = canvas.worldCamera;

        return RectTransformUtility.WorldToScreenPoint(camForUI, anchorAt.position);
    }

    IEnumerator Animate(int amount)
    {
        // 1) ��駢�ͤ���
        if (priceText) priceText.text = $"+{amount:N0}";

        // 2) ���� popup ���������� popupLayer (�ѹⴹ LayoutGroup �������)
        if (popupLayer != null && rect.parent != popupLayer)
            rect.SetParent(popupLayer, false);

        // 3) �ӹǳ�ԡѴ local � popupLayer �ҡ���˹觨ͧ͢ anchorAt
        Vector2 screen = GetAnchorScreenPos();
        Vector2 local;
        var cam = popupLayer != null && popupLayer.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : Camera.main;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screen, cam, out local);

        Vector2 startPos = local + startOffset;
        Vector2 endPos = local + endOffset;

        rect.anchoredPosition = startPos;
        cg.alpha = 0f;

        // 4) fade-in + move-up
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / moveDuration);
            float ease = 1f - Mathf.Pow(1f - p, 3f); // ease-out

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            cg.alpha = Mathf.Lerp(0f, 1f, ease);
            yield return null;
        }

        // 5) hold
        yield return new WaitForSecondsRealtime(holdDuration);

        // 6) fade-out (+ �ѹ����ա�Դ)
        t = 0f;
        Vector2 endPos2 = endPos + new Vector2(0f, 15f);
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            rect.anchoredPosition = Vector2.Lerp(endPos, endPos2, p);
            cg.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        HideImmediate();
        co = null;
    }
}

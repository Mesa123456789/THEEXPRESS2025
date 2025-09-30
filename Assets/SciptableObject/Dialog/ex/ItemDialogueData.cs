using UnityEngine;

[CreateAssetMenu(fileName = "ItemDialogueData", menuName = "Dialogue/Item Dialogue Data")]
public class ItemDialogueData : ScriptableObject
{
    // ✅ รวมทุก Action ไว้ที่นี่
    public enum LineAction
    {
        None = 0,
        NPCExit = 1,
        PolicePay = 2,
        KillPlayer = 3
    }

    [System.Serializable]
    public class ChoiceOption
    {
        public string text;
        public int gotoIndex = -1;
    }

    [System.Serializable]
    public class Step
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        public AudioClip voice;

        public int gotoIndex = -1;

        [Header("Action")]
        public LineAction onLineEndAction = LineAction.None;

        [Tooltip("จำนวนเงินใช้เมื่อ Action = SpendMoney")]
        public int moneyAmount = 0;

        [Tooltip("ดีเลย์ก่อนยิงแอ็กชัน (วินาที)")]
        public float onLineEndDelay = 0f;

        [Header("Choice (optional)")]
        public ChoiceOption[] options;
    }

    [Header("Flow")]
    public Step[] steps;

    [Header("Behavior")]
    public bool lockPlayer = true;
    public AudioClip openSfx;
}

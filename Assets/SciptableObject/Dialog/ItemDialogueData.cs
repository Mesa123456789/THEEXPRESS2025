using UnityEngine;

[CreateAssetMenu(fileName = "ItemDialogueData", menuName = "Dialogue/Item Dialogue Data")]
public class ItemDialogueData : ScriptableObject
{
    public enum StepType { Line, Choice }

    [System.Serializable]
    public class Step
    {
        public StepType type = StepType.Line;

        [Header("Line")]
        public string speaker;
        [TextArea(2, 5)] public string text;
        public AudioClip voice;

        [Header("Choice")]
        [Tooltip("2–4 ตัวเลือก (ใช้เฉพาะเมื่อ type = Choice)")]
        public string[] options;
    }

    [Header("Flow")]
    public Step[] steps;

    [Header("Behavior")]
    public bool lockPlayer = true;
    public AudioClip openSfx;
}

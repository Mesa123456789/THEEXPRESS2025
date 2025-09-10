using UnityEngine;

[CreateAssetMenu(fileName = "ItemDialogueData", menuName = "Dialogue/Item Dialogue Data")]
public class ItemDialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Line
    {
        public string speaker;          // ชื่อผู้พูด (ถ้าไม่ใช้ปล่อยว่างได้)
        [TextArea(2, 5)] public string text;
        public AudioClip voice;         // (ออปชัน)
    }

    [Header("Lines (แสดงตามลำดับ)")]
    public Line[] lines;                // หลายประโยค

    [Header("Choices (ตอนจบ)")]
    [Tooltip("2-4 ตัวเลือก (แสดงหลังจบบรรทัดสุดท้าย)")]
    public string[] options;            // ตัวเลือก 2–4 ข้อ

    [Header("Behavior")]
    public bool lockPlayer = true;      // ล็อกการเดินระหว่างแสดง
    public AudioClip openSfx;           // เสียงเปิด (ออปชัน)
}

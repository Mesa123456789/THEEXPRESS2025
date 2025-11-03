using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "Scriptable Objects/NPCData")]
public class NPCData : ScriptableObject
{
    [Header("Correct Answers")]
    public string npcName;
    public string address;
    public GameObject package;

    [Header("Dropdown Options")]
    [Tooltip("4 ตัวเลือกสำหรับ Name (ให้ใส่ถูก 1 ตัว ตำแหน่งตาม correctNameIndex)")]
    public string[] nameOptions = new string[4];

    [Tooltip("index ของคำตอบที่ถูก (0–3) สำหรับ Name")]
    [Range(0, 3)] public int correctNameIndex = 0;

    [Space(8)]
    [Tooltip("4 ตัวเลือกสำหรับ Address")]
    public string[] addressOptions = new string[4];

    [Tooltip("index ของคำตอบที่ถูก (0–3) สำหรับ Address")]
    [Range(0, 3)] public int correctAddressIndex = 0;
}

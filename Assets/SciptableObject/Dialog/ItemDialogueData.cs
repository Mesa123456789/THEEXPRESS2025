using UnityEngine;

[CreateAssetMenu(fileName = "ItemDialogueData", menuName = "Dialogue/Item Dialogue Data")]
public class ItemDialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Line
    {
        public string speaker;          // ���ͼ��ٴ (��������������ҧ��)
        [TextArea(2, 5)] public string text;
        public AudioClip voice;         // (�ͻ�ѹ)
    }

    [Header("Lines (�ʴ�����ӴѺ)")]
    public Line[] lines;                // ���»���¤

    [Header("Choices (�͹��)")]
    [Tooltip("2-4 ������͡ (�ʴ���ѧ����÷Ѵ�ش����)")]
    public string[] options;            // ������͡ 2�4 ���

    [Header("Behavior")]
    public bool lockPlayer = true;      // ��͡����Թ�����ҧ�ʴ�
    public AudioClip openSfx;           // ���§�Դ (�ͻ�ѹ)
}

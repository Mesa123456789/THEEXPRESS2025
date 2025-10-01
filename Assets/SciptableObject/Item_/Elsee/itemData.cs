using UnityEngine;

[CreateAssetMenu(fileName = "itemData", menuName = "Scriptable Objects/itemData")]
public class itemData : ScriptableObject
{
    public int price;
    public int caughtPercent;
    public bool illegal;
    public ItemDialogueData dialogueData;
}

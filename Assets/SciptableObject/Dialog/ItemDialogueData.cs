using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "ItemDialogueData", menuName = "Dialogue/Item Dialogue Data")]
public class ItemDialogueData : ScriptableObject
{
    [System.Serializable]
    public class ChoiceOption
    {

        public string text;


        public int gotoIndex = -1;


        public UnityEvent onSelect;
    }

    [System.Serializable]
    public class Step
    {

        public string speaker;
        [TextArea(2, 5)] public string text;
        public AudioClip voice;


        public int gotoIndex = -1;


        public ChoiceOption[] options;
    }

    [Header("Flow")]
    public Step[] steps;

    [Header("Behavior")]
    public bool lockPlayer = true;
    public AudioClip openSfx;
}

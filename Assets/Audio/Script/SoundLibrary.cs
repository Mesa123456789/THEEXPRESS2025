using UnityEngine;

[System.Serializable]
public struct SoundEffect
{
    public string groupID;         // ���͡�������§ �� "Jump", "Explosion"
    public AudioClip[] clips;      // �����§������� �����������
}

public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] soundEffects;

    // �� AudioClip �ҡ���� group
    public AudioClip GetClipFromName(string name)
    {
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect.groupID == name)
            {
                return soundEffect.clips[Random.Range(0, soundEffect.clips.Length)];
            }
        }
        return null;
    }
}


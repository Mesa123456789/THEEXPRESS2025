using UnityEngine;

[System.Serializable]
public struct SoundEffect
{
    public string groupID;         // ชื่อกลุ่มเสียง เช่น "Jump", "Explosion"
    public AudioClip[] clips;      // เก็บเสียงหลายไฟล์ เพื่อสุ่มเล่น
}

public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] soundEffects;

    // หา AudioClip จากชื่อ group
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


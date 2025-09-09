using UnityEngine;

[System.Serializable]
public struct MusicTrack
{
    public string trackName;   // ชื่อเพลง เช่น "Battle", "Menu"
    public AudioClip clip;     // ไฟล์เพลง
}

public class MusicLibrary : MonoBehaviour
{
    public MusicTrack[] tracks;

    public AudioClip GetClipFromName(string trackName)
    {
        foreach (var track in tracks)
        {
            if (track.trackName == trackName)
            {
                return track.clip;
            }
        }
        return null;
    }
}

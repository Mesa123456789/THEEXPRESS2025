using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private SoundLibrary sfxLibrary;  // ลิงก์ไปที่ SoundLibrary
    [SerializeField] private AudioSource sfx2DSource;  // สำหรับเสียง 2D (UI, เมนู)

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // เล่นเสียง 3D โดยส่ง AudioClip โดยตรง
    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, pos);
    }

    // เล่นเสียง 3D โดยใช้ชื่อ group
    public void PlaySound3D(string soundName, Vector3 pos)
    {
        PlaySound3D(sfxLibrary.GetClipFromName(soundName), pos);
    }

    // เล่นเสียง 2D (เช่นเสียงกดปุ่ม)
    public void PlaySound2D(string soundName)
    {
        sfx2DSource.PlayOneShot(sfxLibrary.GetClipFromName(soundName));
    }
}

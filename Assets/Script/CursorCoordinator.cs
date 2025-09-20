using StarterAssets;
using UnityEngine;

public class CursorCoordinator : MonoBehaviour
{
    public static CursorCoordinator I { get; private set; }

    [Header("State (read-only)")]
    public bool computerOpen;           // true = บังคับเปิดเคอร์เซอร์
    public bool dialogueWantsCursor;    // ความต้องการจากไดอะล็อก (ใช้เมื่อ computerOpen = false)

    private FirstPersonController fpc;
    private StarterAssetsInputs sai;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc) sai = fpc.GetComponent<StarterAssetsInputs>();
        Apply();
    }

    void LateUpdate()
    {
        // เผื่อเปลี่ยนซีน/รีอินสแตนซ์ FPC
        if (!fpc)
        {
            fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc) sai = fpc.GetComponent<StarterAssetsInputs>();
        }
    }

    public void SetComputerOpen(bool open)
    {
        computerOpen = open;
        Apply();
    }

    public void SetDialogueWantsCursor(bool wants)
    {
        dialogueWantsCursor = wants;
        Apply();
    }

    private void Apply()
    {
        bool show = computerOpen ? true : dialogueWantsCursor;

        // ถ้าอยาก “ปิดเมาส์เสมอเมื่อเพิ่งปิดคอม” ให้เพิ่มแฟล็ก & เวลาปิดคอมตั้ง show=false หนึ่งเฟรม
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        if (sai) sai.cursorInputForLook = !show;
    }

}

using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class HoverUIOnLook : MonoBehaviour
{

    public GameObject uiPrefab;    


    [Header("ข้อความของชิ้นนี้ (แก้ได้จาก Inspector)")]
    [TextArea] public string hintMessage = "กดคลิกซ้ายเพื่อหยิบ";

    // runtime
    GameObject uiInstance;
    TMP_Text uiText;

    void Awake()
    {
        if (!uiPrefab)
        {
            Debug.LogWarning($"{name}: ยังไม่ได้ใส่ uiPrefab (ต้องเป็น Prefab ที่มี TMP_Text ข้างใน)", this);
            return;
        }

        // ทำอินสแตนซ์ UI เฉพาะของชิ้นนี้ (จะไม่ไปแชร์กับใคร)
        uiInstance = Instantiate(uiPrefab, transform);
        uiInstance.SetActive(false); // เริ่มจากซ่อน

        // หาตัวหนังสือในอินสแตนซ์นี้เอง
        uiText = uiInstance.GetComponentInChildren<TMP_Text>(true);
        if (!uiText)
            Debug.LogWarning($"{name}: uiPrefab ไม่มี TMP_Text ข้างใน", this);
    }

    // เรียกจาก PlayerRaycaster
    public void ShowUI()
    {
        if (!uiInstance) return;
        if (uiText) uiText.text = hintMessage;   // <- ใช้ข้อความของ "ชิ้นนี้"
        uiInstance.SetActive(true);
    }

    public void HideUI()
    {
        if (!uiInstance) return;
        uiInstance.SetActive(false);
    }
}

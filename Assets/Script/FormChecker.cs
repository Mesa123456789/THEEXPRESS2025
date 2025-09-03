using StarterAssets;
using TMPro;
using UnityEngine;

public class FormChecker : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField addressField;
    // public TMP_InputField phoneField;
    public FirstPersonController playerController;

    [SerializeField] private NPC currentNPC;
    public GameObject receiptPrefab;    // drag Prefab มาไว้ใน Inspector
    public Transform receiptSpawnPoint; // จุดที่ปริ้น (ติดกับ Computer)

    public void OnFormSuccess()
    {
        // เรียกเมื่อฟอร์มผ่าน
        // Instantiate ใบเสร็จที่หน้าคอม
        GameObject receipt = Instantiate(receiptPrefab, receiptSpawnPoint.position, receiptPrefab.transform.rotation);
        Debug.Log("ปริ้นใบเสร็จออกมาแล้ว!");
    }
    private void Start()
    {
        currentNPC = FindFirstObjectByType<NPC>();
    }

    public void OnSubmitButton()
    {
        var npcData = currentNPC.GetData();

        bool result = nameField.text.Trim() == npcData.npcName &&
                      addressField.text.Trim() == npcData.address;

        if (result)
        {
            playerController.isMovementLocked = false;
            OnFormSuccess();
        }
        else
        {
        }
    }
}


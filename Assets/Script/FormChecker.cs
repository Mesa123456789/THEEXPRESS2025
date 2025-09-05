using StarterAssets;
using TMPro;
using UnityEngine;

public class FormChecker : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameField;
    public TMP_InputField addressField;

    [Header("Player Control")]
    public FirstPersonController playerController;

    [Header("Receipt")]
    public GameObject receiptPrefab;
    public Transform receiptSpawnPoint;

    [Header("Detection")]
    public string npcTag = "NPC";  
    
    [SerializeField] private NPC currentNPC;
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(npcTag)) return;

        currentNPC = other.GetComponent<NPC>();
    }

    public void OnSubmitButton()
    {
        if (!currentNPC) return;                

        var data = currentNPC.GetData();
        if (data == null) return;

        bool ok =
            nameField && addressField &&
            nameField.text.Trim() == data.npcName &&
            addressField.text.Trim() == data.address;

        if (ok)
        {
            if (playerController) playerController.isMovementLocked = false;
            OnFormSuccess();
        }
    }

    void OnFormSuccess()
    {
        if (!receiptPrefab || !receiptSpawnPoint) return;

        Instantiate(receiptPrefab, receiptSpawnPoint.position, receiptPrefab.transform.rotation);
    }
}

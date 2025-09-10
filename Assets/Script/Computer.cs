using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class Computer : MonoBehaviour
{
    public Canvas informUI;
    public FirstPersonController playerController;
    private void Start()
    {
        playerController = FindFirstObjectByType<FirstPersonController>();
        informUI.enabled = false;
    }
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("Computer"))
            {
                OnOpenComputer();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCloseComputer();
        }

    }

    public void OnOpenComputer()
    {
        informUI.enabled = true;
        playerController.isMovementLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void OnCloseComputer()
    {
        informUI.enabled = false;
        playerController.isMovementLocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

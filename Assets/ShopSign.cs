using UnityEngine;

public class ShopSign : MonoBehaviour
{
    [Header("Materials")]
    public Material openMaterial; 
    public Material closedMaterial; 

    [Header("Mesh Renderer ของป้าย")]
    public MeshRenderer signRenderer;

    public GameManager gameManager;
    bool isOpen = false;

    void Start()
    {
        isOpen = gameManager.shopIsOpen;
        if (!signRenderer) signRenderer = GetComponent<MeshRenderer>();
        UpdateSign();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("sign"))
            {
                Debug.Log("press");
                isOpen = !isOpen;
                gameManager.shopIsOpen = isOpen;
                UpdateSign();
            }
        }
    }


    void UpdateSign()
    {
        if (isOpen)
        {
            transform.localRotation = Quaternion.Euler(
                transform.localRotation.eulerAngles.x,
                180f,
                transform.localRotation.eulerAngles.z
            );
            if (signRenderer) signRenderer.material = openMaterial;
        }
        else
        {
            gameManager.shopIsOpen = false;
            transform.localRotation = Quaternion.Euler(
                transform.localRotation.eulerAngles.x,
                0f,
                transform.localRotation.eulerAngles.z
            );
            if (signRenderer) signRenderer.material = closedMaterial;
        }
    }
}

using UnityEngine;

public class SphereSpawn : MonoBehaviour
{
    public GameObject spherePrefab;
    public int amountToSpawn = 5;
    public Vector3 spawnOffset = new Vector3(0, -0.5f, 0);

    public BoxScript currentBox; 
    void Start()
    {
        currentBox = FindFirstObjectByType<BoxScript>();
    }

    void Update()
    {
        if (currentBox == null)
        {
            currentBox = FindFirstObjectByType<BoxScript>();
            if (currentBox != null)
            {
                Debug.Log("เชื่อม currentBox สำเร็จ!");
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("Spherespawner"))
            {
                InsertBubble();
            }
        }
    }


    void InsertBubble()
    {
        if (currentBox != null && currentBox.hasItem)
        {
            currentBox.AddBubble(); 
        }
        else
        {
            Debug.Log("ยังไม่มีของในกล่องหรือยังไม่ได้อ้างอิงกล่อง!");
        }
    }
}

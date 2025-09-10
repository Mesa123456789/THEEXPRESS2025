using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform spawPoint;

    public SphereSpawn sphereSpawner;
    public bool hasSpawnedBox = false;

    public void SpawnBox()
    {
        if (hasSpawnedBox) return;

        GameObject newBox = Instantiate(cubePrefab, new Vector3(spawPoint.position.x, spawPoint.position.y, spawPoint.position.z), cubePrefab.transform.rotation);
        hasSpawnedBox = true;

        BoxScript boxScript = newBox.GetComponent<BoxScript>();
        if (boxScript != null && sphereSpawner != null)
        {
            sphereSpawner.currentBox = boxScript;
        }
    }

    public void ResetSpawner()
    {
        hasSpawnedBox = false;
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !hasSpawnedBox)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("Boxspawner"))
            {
                SpawnBox();
            }
        }
    }
}

using UnityEngine;

public class NPC : MonoBehaviour
{
    public NPCData data;
    private NpcBoxcollider npcBoxcollider;
    private bool hasSpawnedPackage = false; 

    public float moveSpeed = 3f;

    private void Start()
    {
        npcBoxcollider = FindFirstObjectByType<NpcBoxcollider>();
    }

    private void Update()
    {
        if (npcBoxcollider == null || hasSpawnedPackage) return;        
        Vector3 target = npcBoxcollider.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        float minDistance = 0.5f;
        if (Vector3.Distance(transform.position, target) < minDistance)
        {
            SpawnPackage();
            hasSpawnedPackage = true;
        }
    }

    void SpawnPackage()
    {
        if (data != null && data.package != null)
        {
            Instantiate(data.package, npcBoxcollider.transform.position, Quaternion.identity);
        }
    }

    public NPCData GetData()
    {
        return data;
    }
}
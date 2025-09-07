using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    public GameObject[] npcPrefabs;

    public Transform[] spawnPoints;

    [Header("Route Assignment")]
    public Transform[] entryWaypoints;

    public Transform exitPoint;

    [Header("Spawn Mode")]
    public bool spawnOnBoxStored = false;

    public bool spawnOneOnStart = true;

    [Header("Continuous Mode Settings")]
    public Vector2 spawnDelayRange = new Vector2(2f, 5f);


    public int maxAlive = 3;

    public bool alignToSpawnPointForward = true;

    private Coroutine loopCo;

    
    void OnEnable()
    {
        if (spawnOnBoxStored)
        {
            WarehouseZone.OnBoxStored += HandleBoxStoredSpawn;
        }
    }

    void OnDisable()
    {
        if (spawnOnBoxStored)
        {
            WarehouseZone.OnBoxStored -= HandleBoxStoredSpawn;
        }
    }

    void Start()
    {
        if (spawnOneOnStart)
        {
            SpawnOne();
        }

        if (!spawnOnBoxStored)
        {
            loopCo = StartCoroutine(SpawnLoop());
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (CountAlive() < Mathf.Max(1, maxAlive))
            {
                SpawnOne();
            }

            float wait = Random.Range(spawnDelayRange.x, spawnDelayRange.y);
            yield return new WaitForSeconds(wait);
        }
    }

    void HandleBoxStoredSpawn()
    {
        SpawnOne();
    }

    public void SpawnOne()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0) { Debug.LogWarning("[NPCSpawner] ไม่มีพรีแฟ็บให้สปอว์น"); return; }

      
        GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        Transform sp = ChooseSpawnPoint();

       
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp && alignToSpawnPointForward ? sp.rotation : Quaternion.identity;

        GameObject go = Instantiate(prefab, pos, rot);

        var npc = go.GetComponent<NPC>();
        if (npc != null)
        {

            npc.entryWaypoints = entryWaypoints;
            npc.exitPoint = exitPoint;
        }
        else
        {
            Debug.LogWarning("[NPCSpawner] พรีแฟ็บไม่มีคอมโพเนนต์ NPC");
        }
    }

    Transform ChooseSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        return null;
    }

    int CountAlive()
    {
        var all = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        return all != null ? all.Length : 0;
    }
}

using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    public GameObject[] npcPrefabs;
    public GameObject policePrefab;
    public Transform[] spawnPoints;

    [Header("Route Assignment")]
    public Transform[] entryWaypoints;
    public Transform exitPoint;

    [Header("Continuous Mode Settings")]
    public Vector2 spawnDelayRange = new Vector2(2f, 5f);
    public int maxAlive = 3;

    [Header("Shop Gate")]
    public bool requireShopOpen = true;
    public bool canSpawn = true;

    private bool forcePoliceNextSpawn = false;
    private GameManager gm;

    public static NPCSpawner Instance { get; private set; }
    public static NPC CurrentNPC { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public NPC Spawn(NPC npcPrefab, Vector3 position, Quaternion rotation)
    {
        var npc = Instantiate(npcPrefab, position, rotation);
        CurrentNPC = npc;
        return npc;
    }

    public void SetCurrent(NPC npc)
    {
        CurrentNPC = npc;
    }

    void Start()
    {
        gm = FindFirstObjectByType<GameManager>();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (requireShopOpen)
            {
                if (!gm || !gm.shopIsOpen || !canSpawn)
                {
                    yield return null;
                    continue;
                }
            }
            else
            {
                if (!canSpawn)
                {
                    yield return null;
                    continue;
                }
            }

            if (CountAlive() < Mathf.Max(1, maxAlive))
            {
                SpawnOne();
                float wait = Random.Range(spawnDelayRange.x, spawnDelayRange.y);
                yield return new WaitForSeconds(wait);
            }
            else
            {
                yield return null;
            }
        }
    }

    public void SpawnOne()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();

        Transform sp = ChooseSpawnPoint();
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp ? sp.rotation : Quaternion.identity;

        GameObject prefabToSpawn = null;

        if (forcePoliceNextSpawn && policePrefab != null)
        {
            prefabToSpawn = policePrefab;
            forcePoliceNextSpawn = false;
        }
        else
        {
            if (gm && gm.totalCaughtPercent >= 90 && policePrefab != null)
            {
                prefabToSpawn = policePrefab;
            }
            else
            {
                if (npcPrefabs == null || npcPrefabs.Length == 0)
                {
                    Debug.LogWarning("[NPCSpawner] No npcPrefabs set.");
                    return;
                }
                prefabToSpawn = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
            }
        }

        var go = Instantiate(prefabToSpawn, pos, rot);
        var npc = go.GetComponent<NPC>();
        if (npc != null)
        {
            npc.entryWaypoints = entryWaypoints;
            npc.exitPoint = exitPoint;
        }
    }

    Transform ChooseSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        return null;
    }

    int CountAlive()
    {
        var all = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        return all != null ? all.Length : 0;
    }

    public void ForcePoliceNext()
    {
        forcePoliceNextSpawn = true;
    }

    public void SetSpawningEnabled(bool enabled)
    {
        canSpawn = enabled;
    }
}

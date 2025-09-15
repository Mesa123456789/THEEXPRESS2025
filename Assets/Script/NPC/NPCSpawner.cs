using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    public GameObject[] npcPrefabs;       // ปกติ
    public GameObject policePrefab;       // พรีแฟบตำรวจ
    public Transform[] spawnPoints;

    [Header("Route Assignment")]
    public Transform[] entryWaypoints;
    public Transform exitPoint;

    [Header("Continuous Mode Settings")]
    public Vector2 spawnDelayRange = new Vector2(2f, 5f);
    public int maxAlive = 3;

    [Header("Shop Gate")]
    [Tooltip("ต้องเปิดร้านก่อนถึงจะสปอว์น (อ่านจาก GameManager.shopIsOpen)")]
    public bool requireShopOpen = true;
    [Tooltip("อนุญาต/ห้ามสปอว์นด้วยตนเอง (เผื่ออยากคุมด้วย UI/ป้าย)")]
    public bool canSpawn = true;

    private bool forcePoliceNextSpawn = false;  // บังคับตำรวจรอบถัดไป
    private GameManager gm;

    void Start()
    {
        gm = FindFirstObjectByType<GameManager>();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            // ---- Gate: ต้องเปิดร้านก่อน? ----
            if (requireShopOpen)
            {
                // ยังไม่มี GM หรือร้านยังไม่เปิด → รอไปก่อน
                if (!gm || !gm.shopIsOpen || !canSpawn)
                {
                    yield return null;
                    continue;
                }
            }
            else
            {
                // ไม่บังคับเปิดร้าน แต่ยังอยากคุมด้วย canSpawn
                if (!canSpawn)
                {
                    yield return null;
                    continue;
                }
            }

            // ---- คุมจำนวนมีชีวิตบนฉาก ----
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

        // ถ้าบังคับตำรวจ → spawn ตำรวจทันที
        if (forcePoliceNextSpawn && policePrefab != null)
        {
            prefabToSpawn = policePrefab;
            forcePoliceNextSpawn = false; // ใช้แล้วรีเซ็ต
        }
        else
        {
            // เงื่อนไขตำรวจตาม totalCaughtPercent
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

    // เรียกจาก GameManager เมื่อ totalCaughtPercent >= 90
    public void ForcePoliceNext()
    {
        forcePoliceNextSpawn = true;
    }

    // เรียกจากป้ายหน้าร้าน / ปุ่ม UI ก็ได้
    public void SetSpawningEnabled(bool enabled)
    {
        canSpawn = enabled;
    }
}

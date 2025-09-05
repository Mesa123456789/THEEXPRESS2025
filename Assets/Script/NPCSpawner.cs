using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    [Tooltip("พรีแฟ็บ NPC ที่เป็นไปได้ (จะสุ่ม 1 ตัวต่อการเกิด)")]
    public GameObject[] npcPrefabs;

    [Tooltip("จุดเกิด NPC (จะสุ่ม 1 จุดต่อการเกิด)")]
    public Transform[] spawnPoints;

    [Header("Route Assignment")]
    [Tooltip("ทางเดินเข้าทีละจุด 1→2→3 ... จะถูกเซ็ตให้ NPC ที่เกิดใหม่")]
    public Transform[] entryWaypoints;

    [Tooltip("จุดออก/ทางเดินออก (ไปถึงแล้วจะ Destroy NPC)")]
    public Transform exitPoint;

    [Header("Spawn Mode")]
    [Tooltip("true = สปอว์นเมื่อเก็บกล่องเข้าคลัง (ฟังอีเวนต์จาก WarehouseZone)\nfalse = สปอว์นต่อเนื่องตามเวลา")]
    public bool spawnOnBoxStored = false;

    [Tooltip("ให้สปอว์น NPC 1 ตัวทันทีตอนเริ่มเกม")]
    public bool spawnOneOnStart = true;

    [Header("Continuous Mode Settings")]
    [Tooltip("ดีเลย์สุ่มระหว่างการเกิด (วินาที) [min,max]")]
    public Vector2 spawnDelayRange = new Vector2(2f, 5f);

    [Tooltip("จำนวน NPC สูงสุดบนฉาก (เฉพาะ Continuous)")]
    public int maxAlive = 3;

    [Tooltip("จัดแนว NPC ให้หันไปตาม Forward ของจุดเกิด")]
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
        // โหมดสปอว์นต่อเนื่อง: เคารพ maxAlive + หน่วงเวลาแบบสุ่ม
        while (true)
        {
            // จำกัดจำนวน NPC บนฉาก
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
        // โหมดอีเวนต์: เก็บเข้าคลังปุ๊บ สปอว์นตัวใหม่ปั๊บ
        SpawnOne();
    }

    public void SpawnOne()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0) { Debug.LogWarning("[NPCSpawner] ไม่มีพรีแฟ็บให้สปอว์น"); return; }

        // เลือกพรีแฟ็บ/จุดเกิดแบบสุ่ม
        GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        Transform sp = ChooseSpawnPoint();

        // ตำแหน่ง + หมุนเริ่มต้น
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp && alignToSpawnPointForward ? sp.rotation : Quaternion.identity;

        GameObject go = Instantiate(prefab, pos, rot);

        // ตั้งค่าเส้นทางให้ NPC ที่เกิดใหม่
        var npc = go.GetComponent<NPC>();
        if (npc != null)
        {
            // เซ็ตเวย์พอยท์เข้า + จุดออก
            npc.entryWaypoints = entryWaypoints;
            npc.exitPoint = exitPoint;
            // ที่เหลือ NPC.cs จะหา NpcBoxcollider เองใน Start()
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
        // เช็กนับ NPC ทั้งฉาก (วิธีง่ายโดยไม่ต้องแก้โค้ด NPC)
        var all = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        return all != null ? all.Length : 0;
    }
}

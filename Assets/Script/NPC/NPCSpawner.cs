using System.Collections;
using System.Collections.Generic;   // === ADD
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

    // === ADD: จุดเข้าคิว (วางไว้เรียงหัวแถว -> ท้ายแถว)
    [Header("Queue Points (front -> back)")]
    public Transform[] queuePoints;

    [Header("Continuous Mode Settings")]
    public Vector2 spawnDelayRange = new Vector2(2f, 5f);
    public int maxAlive = 3;

    [Header("Shop Gate")]
    public bool requireShopOpen = true;
    public bool canSpawn = true;

    private bool forcePoliceNextSpawn = false;
    private GameManager gm;

    [Header("Queue Settings")]
    public int minQueueSize = 3;       // เติมคิวให้มีอย่างน้อยกี่ตัว
    public bool keepQueueFilled = true; // เปิด/ปิดโหมดเติมคิวอัตโนมัติ

    public static NPC CurrentNPC { get; private set; }   // มีอยู่แล้วก็ดี
    public NPC GetCurrent() => CurrentNPC;
    [Header("Queue Fill Tuning")]
    public float fillTickDelay = 0.25f; // หน่วงเวลาระหว่างการเติมคิวทีละตัว
                                        // ==== COUNTS ====
    private int exitingCount = 0; // ตัวที่เริ่มเดินออกแล้ว แต่ยังไม่ Destroy
    [Header("Police Handling")]
    public bool autoCallWhenPoliceFront = true; // โต๊ะว่าง + หัวแถวเป็นตำรวจ => เรียกเข้าทันที
    public static NPCSpawner Instance { get; private set; }
    // === ADD: โครงสร้างคิว
    private readonly List<NPC> queue = new List<NPC>();
    private int EffectiveAliveCount()
    {
        // คนที่มีผลต่อ capacity ของร้าน = current หน้าโต๊ะ + คนในคิว + คนที่กำลังเดินออก
        return (CurrentNPC != null ? 1 : 0) + queue.Count + exitingCount;
    }
    private bool IsPoliceNPC(NPC npc)
    {
        // รองรับทั้ง subclass และมีคอมโพเนนต์ NPCPolice บนตัวเดียวกัน
        return npc is NPCPolice || (npc != null && npc.GetComponent<NPCPolice>() != null);
    }
    private void AutoCallIfPoliceFront()
    {
        if (!autoCallWhenPoliceFront) return;
        if (CurrentNPC != null) return;       // โต๊ะยังไม่ว่าง
        if (queue.Count == 0) return;

        var head = queue[0];
        if (head == null)
        {
            queue.RemoveAt(0);
            RepositionQueue();
            return;
        }

        if (IsPoliceNPC(head))
        {
            // เอาตำรวจหัวแถวเข้าทันที (ข้ามปุ่ม)
            queue.RemoveAt(0);
            head.FlagCalledToTable();
            RepositionQueue();
        }
    }

    public void SetCurrent(NPC npc)
    {
        CurrentNPC = npc;
        // ถ้าโต๊ะว่างแล้ว และหัวแถวเป็นตำรวจ => ให้เข้าทันที
        if (npc == null) AutoCallIfPoliceFront();
    }


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

            // หลังตี 2 ไม่สปาวน์เพิ่ม
            if (gm && IsAfterCloseHour(gm.currentHour, gm.shopCloseHour))
            {
                canSpawn = false;
                yield return null;
                continue;
            }

            // ✅ เติมคิวก่อนเสมอ โดยอิง EffectiveAliveCount (ไม่ติดเคสตัวกำลังเดินออก)
            if (keepQueueFilled && queue != null)
            {
                while (queue.Count < Mathf.Max(0, minQueueSize) &&
                       EffectiveAliveCount() < Mathf.Max(1, maxAlive))
                {
                    SpawnOne(); // ตำรวจจะถูกเลือกอัตโนมัติถ้าเงื่อนไขถึง
                    yield return new WaitForSeconds(Mathf.Max(0.01f, fillTickDelay));
                }

                // ไปเฟรมถัดไป
                yield return null;
                continue;
            }



            // เงื่อนไขสุ่มสปาวน์ตาม maxAlive (ของเดิม)
            if (EffectiveAliveCount() < Mathf.Max(1, maxAlive))
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


    // === ADD: ช่วยเช็คเวลาหลังร้านปิด (รองรับข้ามวัน)
    bool IsAfterCloseHour(int h, int closeH)
    {
        // เปิด 15:00 ถึง 02:00 ตาม GameManager
        // หลัง 02:00 ถือว่า closed window
        int H = ((h % 24) + 24) % 24;
        int C = ((closeH % 24) + 24) % 24;
        // ช่วงเปิดคือ 15->02 (wrap) ดังนั้น "หลังปิด" = [02..14]
        // ง่ายสุด: ถ้าไม่อยู่ในช่วงเปิดของวัน => หลังปิด
        int open = gm ? gm.shopOpenHour : 15;
        int O = ((open % 24) + 24) % 24;
        if (O < C) return !(H >= O && H < C);
        else return !(H >= O || H < C);
    }

    public void SpawnOne()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();

        Transform sp = ChooseSpawnPoint();
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp ? sp.rotation : Quaternion.identity;

        GameObject prefabToSpawn = null;
        bool isPolice = false;

        if (forcePoliceNextSpawn && policePrefab != null)
        {
            prefabToSpawn = policePrefab;
            forcePoliceNextSpawn = false;
            isPolice = true;
        }
        else
        {
            // ✅ เงื่อนไขตำรวจอัตโนมัติ
            if (gm && gm.totalCaughtPercent >= 90 && policePrefab != null)
            {
                prefabToSpawn = policePrefab;
                isPolice = true;
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

            // ตำรวจ → แทรกหัวแถวทันที (ไม่ต้องกดปุ่ม)
            EnqueueNPC(npc, front: isPolice);
        }
    }


    public void EnqueueNPC(NPC npc, bool front = false)
    {
        if (npc == null) return;
        if (front) queue.Insert(0, npc);
        else queue.Add(npc);

        RepositionQueue();

        // ถ้าโต๊ะว่างและหัวแถวเป็นตำรวจ → ให้เข้าทันที
        AutoCallIfPoliceFront();
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

    // ============================
    // === Queue System (NEW)  ====
    // ============================

    // ใส่ท้ายแถว และจัดยืนตาม queuePoints
    public void EnqueueNPC(NPC npc)
    {
        if (npc == null) return;
        queue.Add(npc);
        RepositionQueue();
    }

    // เรียกคิวแรกเข้ามา (ใช้กับปุ่ม CallButton)
    public void CallNext()
    {
        // ถ้ามี currentNPC ยังยืนอยู่หน้าโต๊ะ (ยังไม่ออก) ก็ยังไม่เรียกซ้อน
        if (CurrentNPC != null) return;

        if (queue.Count == 0) return;

        var next = queue[0];
        queue.RemoveAt(0);

        // สั่งให้ตัวนี้ "ถูกเรียก" ไปหน้าโต๊ะ
        next.FlagCalledToTable();

        // จัดคิวที่เหลือขยับขึ้น
        RepositionQueue();
    }

    // NPC ตัวไหนเริ่มเดินออก (accept/decline/ส่งของเสร็จ) ให้คิวขยับ
    public void OnNpcLeaving(NPC npc)
    {
        // ตัวนี้กำลังออก → ให้นับเข้ายอด exiting จนกว่าจะโดน Destroy จริง
        if (npc != null)
        {
            exitingCount++;
            StartCoroutine(TrackExitDestruction(npc));
        }

        // ขยับคิวให้แน่น
        RepositionQueue();
    }

    // รอจนกว่าจะถูก Destroy แล้วค่อยลด exitingCount
    private IEnumerator TrackExitDestruction(NPC npc)
    {
        // รอจนกว่าวัตถุจะหายไปจริง ๆ
        while (npc != null && npc.gameObject != null)
            yield return null;

        exitingCount = Mathf.Max(0, exitingCount - 1);
    }

    // ปิดร้านด้วยมือ (ข้อ 6): ทำลายทุกตัวในคิว ยกเว้น current
    public void HandleShopClosed()
    {
        // กันสปาวน์ใหม่
        canSpawn = false;

        var toKill = new List<NPC>();
        foreach (var n in queue)
        {
            if (n != null) toKill.Add(n);
        }
        queue.Clear();

        foreach (var n in toKill)
        {
            if (n != null && n != CurrentNPC)
            {
                Destroy(n.gameObject);
            }
        }
        // จัดระเบียบ (จะว่างคิว)
        RepositionQueue();
    }

    // จัดตำแหน่งยืนตาม queuePoints (หัวแถว index 0)
    private void RepositionQueue()
    {
        if (queuePoints == null || queuePoints.Length == 0) return;

        for (int i = 0; i < queue.Count; i++)
        {
            var n = queue[i];
            if (n == null) continue;

            var slot = Mathf.Min(i, queuePoints.Length - 1);
            n.AssignQueueTarget(queuePoints[slot]);
        }
    }


}

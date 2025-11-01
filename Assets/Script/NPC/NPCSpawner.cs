using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    public GameObject[] npcPrefabs;
    public GameObject policePrefab;
    public Transform[] spawnPoints;           // ลูกค้าปกติ

    [Header("Police Spawn Points")]
    public Transform[] policeSpawnPoints;     // จุดสปอนตำรวจ (คนละชุด)

    [Header("Route Assignment")]
    public Transform[] entryWaypoints;
    public Transform exitPoint;

    [Header("Queue Points (front -> back)")]
    public Transform[] queuePoints;           // index 0 = หัวแถว

    [Header("Continuous Mode Settings")]
    public Vector2 spawnDelayRange = new Vector2(2f, 5f);
    public int maxAlive = 4;                  // แนะนำ >= minQueueSize + 1

    [Header("Shop Gate")]
    public bool requireShopOpen = true;
    public bool canSpawn = true;              // จะถูกซิงค์กับสถานะร้านอัตโนมัติ

    private bool forcePoliceNextSpawn = false;
    private GameManager gm;

    [Header("Queue Settings")]
    public int minQueueSize = 3;              // โหมดเติมเรื่อย ๆ ให้มีอย่างน้อย
    public bool keepQueueFilled = true;       // เปิดโหมดเติมเรื่อย ๆ

    [Header("Queue Fill Tuning")]
    public float fillTickDelay = 0.25f;       // ดีเลย์ตอนเติมเรื่อย ๆ

    [Header("Police Handling")]
    public bool autoCallWhenPoliceFront = true;       // โต๊ะว่าง + หัวแถวเป็นตำรวจ => เข้าทันที
    public int policeTriggerPercentOverride = -1;     // ถ้าตั้ง >=0 ใช้ค่านี้แทน (ไม่งั้นอ่านจาก GM หรือ fallback=90)

    public static NPCSpawner Instance { get; private set; }
    public static NPC CurrentNPC { get; private set; }   // ตั้ง/เคลียร์จาก NPC.cs ตอนชนโต๊ะ/ออก

    // ===== คิวและตัวช่วย =====
    private readonly List<NPC> waitingQueue = new List<NPC>();
    private int exitingCount = 0;                     // ตัวที่เริ่มเดินออก (ยังไม่ Destroy)
    private bool policeSpawnedThisCycle = false;      // กันสปอนตำรวจถี่ ๆ ภายในรอบเดียว

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();
        SyncGateWithShop();           // ✅ ซิงค์ canSpawn ให้ตรงกับสถานะร้านตั้งแต่เริ่ม
        StartCoroutine(SpawnLoop());
    }

    // ======= API ที่ GameManager/NPC เรียก =======

    public NPC GetCurrent() => CurrentNPC;

    public void SetCurrent(NPC npc)
    {
        CurrentNPC = npc;
        if (npc == null) AutoCallIfPoliceFront();   // โต๊ะว่าง → ถ้าหัวแถวเป็นตำรวจให้เข้าอัตโนมัติ
    }

    public void ForcePoliceNext() => forcePoliceNextSpawn = true;

    public void SetSpawningEnabled(bool enabled) => canSpawn = enabled;

    // ======= เติมคิว "ทันที" =======
    public void FillQueueImmediate(int maxToFill = -1)
    {
        if (queuePoints == null || queuePoints.Length == 0) return;

        SyncGateWithShop();  // ✅ เปิดเกทถ้าร้านเปิดอยู่ (กันเคส canSpawn ค้าง false)

        int target = (maxToFill < 0) ? queuePoints.Length : Mathf.Min(maxToFill, queuePoints.Length);

        // เติมทันที: สนเฉพาะร้านเปิด
        while (waitingQueue.Count < target && ShopOpenGate())
        {
            SpawnToQueueTail();
        }

        RepositionQueue();
        AutoCallIfPoliceFront();   // โต๊ะว่าง + หัวแถวเป็นตำรวจ => เข้าทันที
        TryAutoSpawnPolice();      // เช็คตำรวจหลังเติม
    }

    // เรียกตอนเปิดร้าน (ถ้าชอบชื่อแบบ semantic)
    public void OnShopOpened()
    {
        SyncGateWithShop();     // ✅ ให้เกทเปิดตามสถานะร้าน
        FillQueueImmediate();   // เติมให้เต็มทุกจุดคิว
        policeSpawnedThisCycle = false;
        TryAutoSpawnPolice();
    }

    // ======= สปาวน์ลูปพื้นหลัง (โหมดเติมเรื่อย ๆ) =======
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!GateAllowSpawn()) { yield return null; continue; }

            // หลังตี 2 ไม่สปอนเพิ่ม (ยังเรียกจากคิวได้)
            if (gm && IsAfterCloseHour(gm.currentHour, gm.shopCloseHour))
            {
                canSpawn = false;
                yield return null;
                continue;
            }

            // ตำรวจ: เช็คและสปอนถ้าเข้าเกณฑ์
            TryAutoSpawnPolice();

            // เติมคิวเรื่อย ๆ จนถึง minQueueSize (ถ้าเปิด)
            if (keepQueueFilled)
            {
                while (waitingQueue.Count < Mathf.Max(0, minQueueSize) &&
                       EffectiveAliveCount() < Mathf.Max(1, maxAlive) &&
                       GateAllowSpawn())
                {
                    SpawnToQueueTail();
                    yield return new WaitForSeconds(Mathf.Max(0.01f, fillTickDelay));
                }

                yield return null; // ผ่อนเครื่องให้ระบบอื่นขยับ
                continue;
            }

            // โหมดสุ่มทั่วไป (ถ้าต้องการ)
            if (EffectiveAliveCount() < Mathf.Max(1, maxAlive) && GateAllowSpawn())
            {
                SpawnToQueueTail();
                float wait = Random.Range(spawnDelayRange.x, spawnDelayRange.y);
                yield return new WaitForSeconds(wait);
            }
            else
            {
                yield return null;
            }
        }
    }

    // ======= ลูกค้าปกติ: ต่อท้ายคิว =======
    private void SpawnToQueueTail()
    {
        Transform sp = ChooseSpawnPoint(spawnPoints);
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp ? sp.rotation : Quaternion.identity;

        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner] No npcPrefabs set.");
            return;
        }
        GameObject prefabToSpawn = npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        var go = Instantiate(prefabToSpawn, pos, rot);
        var npc = go.GetComponent<NPC>();
        if (npc != null)
        {
            npc.entryWaypoints = entryWaypoints;
            npc.exitPoint = exitPoint;
            EnqueueNPC(npc, front: false);
        }
    }

    // ======= ตำรวจ: สปอนจากจุดของตำรวจเอง =======
    private void TryAutoSpawnPolice()
    {
        if (!GateAllowSpawn()) return;
        if (policePrefab == null) return;

        // ถ้ามีตำรวจอยู่แล้วในระบบ → ไม่สปอนซ้ำ
        if (HasPoliceInSystem()) { policeSpawnedThisCycle = true; return; }

        // บังคับสปอนครั้งถัดไป
        if (forcePoliceNextSpawn)
        {
            SpawnPoliceNow();
            forcePoliceNextSpawn = false;
            policeSpawnedThisCycle = true;
            return;
        }

        // อ่านเกณฑ์จาก GM หรือ override
        int threshold = GetPoliceTriggerPercent();
        int caught = gm ? gm.totalCaughtPercent : 0;

        if (caught >= threshold && !policeSpawnedThisCycle)
        {
            SpawnPoliceNow();
            policeSpawnedThisCycle = true;
        }

        // ถ้าต่ำกว่า threshold อีกครั้ง → ปลดล็อกสำหรับรอบต่อไป
        if (caught < threshold) policeSpawnedThisCycle = false;
    }

    private void SpawnPoliceNow()
    {
        Transform sp = ChooseSpawnPoint(policeSpawnPoints);
        if (sp == null) sp = ChooseSpawnPoint(spawnPoints); // เผื่อไม่ได้ตั้งจุดตำรวจ
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp ? sp.rotation : Quaternion.identity;

        var go = Instantiate(policePrefab, pos, rot);
        var npc = go.GetComponent<NPC>();
        if (npc != null)
        {
            npc.entryWaypoints = entryWaypoints;
            npc.exitPoint = exitPoint;

            if (CurrentNPC == null)
            {
                // โต๊ะว่าง → เข้าทันที
                npc.FlagCalledToTable();
            }
            else
            {
                // มีคนอยู่หน้าโต๊ะ → แทรกหัวแถว
                EnqueueNPC(npc, front: true);
            }
        }
    }

    private bool HasPoliceInSystem()
    {
        if (CurrentNPC && IsPoliceNPC(CurrentNPC)) return true;
        for (int i = 0; i < waitingQueue.Count; i++)
            if (IsPoliceNPC(waitingQueue[i])) return true;
        return false;
    }

    private int GetPoliceTriggerPercent()
    {
        if (policeTriggerPercentOverride >= 0) return policeTriggerPercentOverride;

        if (gm != null)
        {
            // พยายามอ่านฟิลด์ public/private ชื่อ policeTriggerPercent ถ้ามี
            FieldInfo f = gm.GetType().GetField("policeTriggerPercent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                return (int)f.GetValue(gm);
            }
        }
        return 90; // fallback
    }

    // ======= Queue / Housekeeping =======

    public void CallNext()
    {
        if (CurrentNPC != null) return;
        if (waitingQueue.Count == 0) return;

        var next = waitingQueue[0];
        waitingQueue.RemoveAt(0);

        next.FlagCalledToTable();
        RepositionQueue();
    }

    public void OnNpcLeaving(NPC npc)
    {
        if (npc != null)
        {
            exitingCount++;
            StartCoroutine(TrackExitDestruction(npc));
        }
        RepositionQueue();
    }

    public void HandleShopClosed()
    {
        canSpawn = false;

        var toKill = new List<NPC>();
        foreach (var n in waitingQueue) if (n != null) toKill.Add(n);
        waitingQueue.Clear();

        foreach (var n in toKill)
        {
            if (n != null && n != CurrentNPC)
                Destroy(n.gameObject);
        }

        RepositionQueue();
    }

    private int EffectiveAliveCount()
    {
        return (CurrentNPC != null ? 1 : 0) + waitingQueue.Count + exitingCount;
    }

    private IEnumerator TrackExitDestruction(NPC npc)
    {
        while (npc != null) yield return null; // เมื่อถูก Destroy แล้วตัวแปรจะกลายเป็น null
        exitingCount = Mathf.Max(0, exitingCount - 1);
    }

    // ======= Gates & helpers =======

    private void SyncGateWithShop()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();
        if (!requireShopOpen) { canSpawn = true; return; }
        canSpawn = (gm && gm.shopIsOpen);   // ✅ ถ้าร้านเปิด → เปิดเกท
    }

    // ใช้กับโหมดเติมคิวทันที (สนแค่ว่าร้านเปิด)
    private bool ShopOpenGate()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();
        if (!requireShopOpen) return true;
        return gm && gm.shopIsOpen;
    }

    // ใช้กับลูปพื้นหลัง (ต้องผ่านทั้งร้านเปิด + canSpawn)
    private bool GateAllowSpawn()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();
        if (!requireShopOpen) return canSpawn;
        return canSpawn && gm && gm.shopIsOpen;
    }

    private bool IsAfterCloseHour(int hour, int closeHour)
    {
        int H = ((hour % 24) + 24) % 24;
        int C = ((closeHour % 24) + 24) % 24;
        int O = gm ? gm.shopOpenHour : 15;

        bool openRange;
        if (O < C) openRange = (H >= O && H < C);
        else openRange = (H >= O || H < C);

        return !openRange;
    }

    private Transform ChooseSpawnPoint(Transform[] points)
    {
        if (points != null && points.Length > 0)
            return points[Random.Range(0, points.Length)];
        return null;
    }

    public void EnqueueNPC(NPC npc, bool front = false)
    {
        if (npc == null) return;
        if (front) waitingQueue.Insert(0, npc);
        else waitingQueue.Add(npc);

        RepositionQueue();

        // ถ้าโต๊ะว่างและหัวแถวเป็นตำรวจ → เข้าทันที
        AutoCallIfPoliceFront();
    }

    private void RepositionQueue()
    {
        if (queuePoints == null || queuePoints.Length == 0) return;

        for (int i = 0; i < waitingQueue.Count; i++)
        {
            var n = waitingQueue[i];
            if (n == null) continue;

            var slot = Mathf.Min(i, queuePoints.Length - 1);
            n.AssignQueueTarget(queuePoints[slot]);
        }
    }

    private bool IsPoliceNPC(NPC npc)
    {
        return npc is NPCPolice || (npc != null && npc.GetComponent<NPCPolice>() != null);
    }

    private void AutoCallIfPoliceFront()
    {
        if (!autoCallWhenPoliceFront) return;
        if (CurrentNPC != null) return;
        if (waitingQueue.Count == 0) return;

        var head = waitingQueue[0];
        if (head == null)
        {
            waitingQueue.RemoveAt(0);
            RepositionQueue();
            return;
        }

        if (IsPoliceNPC(head))
        {
            waitingQueue.RemoveAt(0);
            head.FlagCalledToTable();
            RepositionQueue();
        }
    }

    // ---------- (ตัวช่วยเก่า: เผื่อยังถูกอ้างอิง) ----------
    public NPC Spawn(NPC npcPrefab, Vector3 position, Quaternion rotation)
    {
        var npc = Instantiate(npcPrefab, position, rotation);
        return npc;
    }
}

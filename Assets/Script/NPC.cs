using System;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public NPCData data;
    private NpcBoxcollider npcBoxcollider;

    [Header("Move")]
    public float moveSpeed = 3f;
    public float reachThreshold = 0.2f;

    [Header("Path In (waypoints 1→2→3...)")]
    public Transform[] entryWaypoints;   // ใส่จุดทางเดินเข้าทีละจุด

    [Header("Exit")]
    public Transform exitPoint;          // จุดออก ก่อน Destroy

    private int entryIndex = 0;
    private bool hasSpawnedPackage = false;

    private enum State { Entering, Waiting, Exiting, Done }
    private State state = State.Entering;

    private void Start()
    {
        npcBoxcollider = FindFirstObjectByType<NpcBoxcollider>();

        // ฟังอีเวนต์จากโกดัง: เมื่อเก็บเข้าคลัง → ให้ NPC นี้ออก (ถ้าวางของแล้ว)
        WarehouseZone.OnBoxStored += HandleBoxStored;
    }

    private void OnDestroy()
    {
        WarehouseZone.OnBoxStored -= HandleBoxStored;
    }

    void HandleBoxStored()
    {
        // ให้เดินออกเฉพาะกรณีที่วางของแล้ว (รออยู่)
        if (hasSpawnedPackage && state == State.Waiting)
        {
            state = State.Exiting;
        }
    }

    private void Update()
    {
        if (state == State.Done) return;

        switch (state)
        {
            case State.Entering:
                UpdateEntering();
                break;
            case State.Waiting:
                // ยืนรออีเวนต์ OnBoxStored
                break;
            case State.Exiting:
                UpdateExiting();
                break;
        }
    }

    void UpdateEntering()
    {
        // 1) เดินตามทางเข้า 1→2→3 ...
        if (entryWaypoints != null && entryWaypoints.Length > 0 && entryIndex < entryWaypoints.Length)
        {
            MoveTowards(entryWaypoints[entryIndex].position);
            if (IsReached(entryWaypoints[entryIndex].position))
                entryIndex++;
            return;
        }

        // 2) เมื่อครบทางเข้าแล้ว → เดินไป NpcBoxcollider
        if (npcBoxcollider == null)
        {
            // ถ้าไม่มีปลายทาง ให้ถือว่าพร้อม Spawn
            SpawnPackageAndWait();
            return;
        }

        MoveTowards(npcBoxcollider.transform.position);
        if (IsReached(npcBoxcollider.transform.position))
        {
            SpawnPackageAndWait();
        }
    }

    void SpawnPackageAndWait()
    {
        if (!hasSpawnedPackage)
        {
            if (data != null && data.package != null)
            {
                // วางพัสดุที่จุด NpcBoxcollider
                Vector3 dropPos = npcBoxcollider ? npcBoxcollider.transform.position : transform.position;
                Instantiate(data.package, dropPos, Quaternion.identity);
            }
            hasSpawnedPackage = true;
        }
        state = State.Waiting; // ยืนรอจนโกดังเก็บเข้าคลัง → จะโดนสั่ง Exiting ผ่านอีเวนต์
    }

    void UpdateExiting()
    {
        if (exitPoint == null)
        {
            // ไม่มีจุดออกก็ลบทิ้งไปเลย
            Destroy(gameObject);
            state = State.Done;
            return;
        }

        MoveTowards(exitPoint.position);
        if (IsReached(exitPoint.position))
        {
            Destroy(gameObject); // หายไป
            state = State.Done;
        }
    }

    // ---------- helpers ----------
    void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        Vector3 dir = (target - transform.position);
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
        }
    }

    bool IsReached(Vector3 target)
    {
        return Vector3.Distance(transform.position, target) <= reachThreshold;
    }

    public NPCData GetData()
    {
        return data;
    }
}

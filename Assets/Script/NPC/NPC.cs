using System;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public NPCData data;
    protected NpcBoxcollider npcBoxcollider;

    [Header("Move")]
    public float moveSpeed = 3f;
    public float reachThreshold = 0.2f;

    [Header("Path In (waypoints 1→2→3...)")]
    public Transform[] entryWaypoints;
    public Transform SpawnPoint;

    [Header("Exit")]
    public Transform exitPoint;

    protected int entryIndex = 0;
    protected bool hasSpawnedPackage = false;

    protected enum State { Entering, Waiting, Exiting, Done }
    protected State state = State.Entering;

    protected GameObject spawnedPackageRef;
    public ItemDialogueManager itemDialogueManager;

    protected Animator Animation;

    // === ADD: คิว
    protected Transform queueTarget = null;
    protected bool isCalledToTable = false;

    protected virtual void Start()
    {
        Animation = GetComponent<Animator>();
        npcBoxcollider = FindFirstObjectByType<NpcBoxcollider>();
        itemDialogueManager = FindFirstObjectByType<ItemDialogueManager>();
        BoxScript.OnBoxStored += HandleBoxStored;
        Animation.SetBool("TableCollision", false);
    }

    protected virtual void OnDestroy()
    {
        BoxScript.OnBoxStored -= HandleBoxStored;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("table collision");
            if (Animation) Animation.SetBool("TableCollision", true);

            // === ADD: set current npc "เฉพาะตอนชนโต๊ะ"
            NPCSpawner.Instance?.SetCurrent(this);
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("table collision");
            if (Animation) Animation.SetBool("TableCollision", false);
            NPCSpawner.Instance?.OnNpcLeaving(this);
        }
    }

    protected void HandleBoxStored()
    {
        if (hasSpawnedPackage && state == State.Waiting)
        {
            state = State.Exiting;
            // แจ้งให้คิวที่เหลือขยับ (ข้อ 4)
            NPCSpawner.Instance?.OnNpcLeaving(this);
        }
    }

    protected virtual void Update()
    {
        if (state == State.Done) return;

        switch (state)
        {
            case State.Entering:
                UpdateEntering();
                break;
            case State.Waiting:
                if (!isCalledToTable && queueTarget != null && !IsReached(queueTarget.position))
                {
                    MoveTowards(queueTarget.position);
                }
                break;
            case State.Exiting:
                UpdateExiting();
                break;
        }
    }

    protected virtual void UpdateEntering()
    {
        // 1) ถ้ายัง "ไม่ถูกเรียก" ให้เดินไปยืนที่ queueTarget แล้วรอ
        if (!isCalledToTable && queueTarget != null)
        {
            MoveTowards(queueTarget.position);
            if (IsReached(queueTarget.position))
            {
                state = State.Waiting;  // ยืนรอเฉย ๆ ยังไม่ spawn package
            }
            return;
        }

        // 2) ถูกเรียกแล้ว: ถ้ามี waypoint ก่อนไปโต๊ะให้เดินตามก่อน
        if (entryWaypoints != null && entryWaypoints.Length > 0 && entryIndex < entryWaypoints.Length)
        {
            MoveTowards(entryWaypoints[entryIndex].position);
            if (IsReached(entryWaypoints[entryIndex].position))
                entryIndex++;
            return;
        }

        // 3) ไปที่โต๊ะ (BoxCollider) เพื่อวางของ + รอ
        if (npcBoxcollider == null)
        {
            SpawnPackageAndWait();
            return;
        }

        MoveTowards(npcBoxcollider.transform.position);
        if (IsReached(npcBoxcollider.transform.position))
        {
            SpawnPackageAndWait(); // state -> Waiting
        }
    }

    // ทำเป็น virtual เพื่อให้ NPCPolice override ได้ (เดิม)
    protected virtual void SpawnPackageAndWait()
    {
        if (!hasSpawnedPackage)
        {
            if (data != null && data.package != null)
            {
                Vector3 dropPos = npcBoxcollider ? npcBoxcollider.transform.position : transform.position;

                spawnedPackageRef = Instantiate(
                    data.package,
                    SpawnPoint ? SpawnPoint.position : dropPos,
                    Quaternion.identity
                );
            }
            hasSpawnedPackage = true;
        }
        state = State.Waiting;
    }

    protected virtual void UpdateExiting()
    {
        if (exitPoint == null)
        {
            Destroy(gameObject);
            state = State.Done;
            return;
        }

        MoveTowards(exitPoint.position);
        if (IsReached(exitPoint.position))
        {
            Destroy(gameObject);
            state = State.Done;
        }
    }

    protected void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        Vector3 dir = (target - transform.position);
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
        }
    }

    protected bool IsReached(Vector3 target)
    {
        return Vector3.Distance(transform.position, target) <= reachThreshold;
    }

    public NPCData GetData() => data;

    public void ForceExitAndClearItem(GameObject itemOnTable = null)
    {
        if (state == State.Done) return;

        if (itemOnTable) Destroy(itemOnTable);
        else if (spawnedPackageRef) Destroy(spawnedPackageRef);

        // ✅ ปลดท่าเกาะโต๊ะ เพื่อให้เดินได้แม้ใช้ Root Motion
        if (Animation) Animation.SetBool("TableCollision", false);

        // ✅ โต๊ะว่างแล้ว
        NPCSpawner.Instance?.SetCurrent(null);

        state = State.Exiting;
        itemDialogueManager?.Close();

        // ✅ ให้คิวขยับมาแนบแน่น (ถ้ามีระบบคิว)
        NPCSpawner.Instance?.OnNpcLeaving(this);
    }


    public void OnAcceptDelivery()
    {
        // (คงเดิม ไม่ยุ่ง dialog)
    }

    public void OnDeclineDelivery()
    {
        // ปฏิเสธแล้วให้ออก
        ForceExitAndClearItem();
    }

    // === ADD: ใช้โดย Spawner
    public void AssignQueueTarget(Transform t)
    {
        queueTarget = t;
        if (state == State.Waiting) { /* ยืนรออยู่ก็ปล่อย */ }
    }

    // === ADD: ถูกเรียกให้เข้าโต๊ะ
    public void FlagCalledToTable()
    {
        isCalledToTable = true;
        // ให้เข้าสู่ flow เดินเข้าโต๊ะ (Entering)
        state = State.Entering;
        entryIndex = 0;
    }

    protected State GetStateWaiting() => State.Waiting;
}

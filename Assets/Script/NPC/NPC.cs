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

    protected virtual void Start()
    {
        Animation = GetComponent<Animator>();
        npcBoxcollider = FindFirstObjectByType<NpcBoxcollider>();
        itemDialogueManager = FindFirstObjectByType<ItemDialogueManager>();
        BoxScript.OnBoxStored += HandleBoxStored;
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
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("table collision");
            if (Animation) Animation.SetBool("TableCollision", false);
        }
    }

    protected void HandleBoxStored()
    {
        if (hasSpawnedPackage && state == State.Waiting)
        {
            state = State.Exiting;
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
                break;
            case State.Exiting:
                UpdateExiting();
                break;
        }
    }

    protected virtual void UpdateEntering()
    {
        // เดินตาม waypoints ก่อน
        if (entryWaypoints != null && entryWaypoints.Length > 0 && entryIndex < entryWaypoints.Length)
        {
            MoveTowards(entryWaypoints[entryIndex].position);
            if (IsReached(entryWaypoints[entryIndex].position))
                entryIndex++;
            return;
        }

        // จากนั้นเดินไปที่โต๊ะ/คอลลายเดอร์
        if (npcBoxcollider == null)
        {
            SpawnPackageAndWait();
            return;
        }

        MoveTowards(npcBoxcollider.transform.position);
        if (IsReached(npcBoxcollider.transform.position))
        {
            SpawnPackageAndWait();
        }
    }

    // ทำเป็น virtual เพื่อให้ NPCPolice override ได้
    protected virtual void SpawnPackageAndWait()
    {
        if (!hasSpawnedPackage)
        {
            if (data != null && data.package != null)
            {
                // จุดวาง
                Vector3 dropPos = npcBoxcollider ? npcBoxcollider.transform.position : transform.position;

                spawnedPackageRef = Instantiate(
                    data.package,
                    SpawnPoint ? SpawnPoint.position : dropPos,
                    Quaternion.identity
                );

                var item = spawnedPackageRef.GetComponent<ItemScript>();
                if (item) item.ownerNPC = this;
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

        state = State.Exiting;
        itemDialogueManager?.Close();
    }

    public void OnAcceptDelivery()
    {

    }

    public void OnDeclineDelivery()
    {

        ForceExitAndClearItem();
    }

 
    protected State GetStateWaiting() => State.Waiting;
}

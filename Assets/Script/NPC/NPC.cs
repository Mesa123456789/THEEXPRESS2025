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
    public Transform[] entryWaypoints;
    public Transform SpawnPoint;

    [Header("Exit")]
    public Transform exitPoint;

    private int entryIndex = 0;
    private bool hasSpawnedPackage = false;

    private enum State { Entering, Waiting, Exiting, Done }
    private State state = State.Entering;


    private GameObject spawnedPackageRef;  
    public ItemDialogueManager itemDialogueManager;

    Animator Animation;
    private void Start()
    {
        Animation = GetComponent<Animator>();
        npcBoxcollider = FindFirstObjectByType<NpcBoxcollider>();
        itemDialogueManager = FindFirstObjectByType<ItemDialogueManager>();
        BoxScript.OnBoxStored += HandleBoxStored;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("table collision");
            Animation.SetBool("TableCollision", true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("table collision");
            Animation.SetBool("TableCollision", false);
        }
    }

    private void OnDestroy()
    {
        BoxScript.OnBoxStored -= HandleBoxStored;
    }

    void HandleBoxStored()
    {
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
                break;
            case State.Exiting:
                UpdateExiting();
                break;
        }
    }

    void UpdateEntering()
    {
        if (entryWaypoints != null && entryWaypoints.Length > 0 && entryIndex < entryWaypoints.Length)
        {
            MoveTowards(entryWaypoints[entryIndex].position);
            if (IsReached(entryWaypoints[entryIndex].position))
                entryIndex++;
            return;
        }

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

    void SpawnPackageAndWait()
    {
        if (!hasSpawnedPackage)
        {
            if (data != null && data.package != null)
            {
                Vector3 dropPos = npcBoxcollider ? npcBoxcollider.transform.position : transform.position;


                spawnedPackageRef = Instantiate(
                    data.package,
                    new Vector3(SpawnPoint.position.x, SpawnPoint.position.y, SpawnPoint.position.z),
                    Quaternion.identity
                );


                var item = spawnedPackageRef.GetComponent<ItemScript>();
                if (item) item.ownerNPC = this; 
            }
            hasSpawnedPackage = true;
        }
        state = State.Waiting;
    }

    void UpdateExiting()
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

    public NPCData GetData() => data;

    public void ForceExitAndClearItem(GameObject itemOnTable = null) 
    {
        
        if (state == State.Done) return;
        
        if (itemOnTable) Destroy(itemOnTable);
        else if (spawnedPackageRef) Destroy(spawnedPackageRef);

        state = State.Exiting;
        itemDialogueManager.Close();
    }
    public void OnAcceptDelivery()
    {

        Debug.Log("[NPC] Accepted. Waiting for box store or next action.");
    }

    public void OnDeclineDelivery()
    {
        Debug.Log("[NPC] Declined. Walk away and clear item.");
        ForceExitAndClearItem();
        
    }


}

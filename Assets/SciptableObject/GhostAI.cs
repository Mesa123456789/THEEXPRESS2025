using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GhostAI : MonoBehaviour
{
    public enum GhostState { Patrol, Roam, Hunt, Trick, Chase, SearchLost, Cooldown }
    public enum PatrolMode { PatrolPoints, RoamArea }

    [Header("Mode")]
    [SerializeField] private PatrolMode mode = PatrolMode.PatrolPoints;

    [Header("Refs")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Patrol Points (ถ้าใช้โหมด PatrolPoints)")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Roaming Area (สำรอง/ถ้าโหมด RoamArea หรือไม่มีจุด)")]
    [Tooltip("BoxCollider ขอบเขตสุ่มเดิน ถ้าเว้นว่างจะสุ่มรอบตัวรัศมี 5 ม.")]
    [SerializeField] private BoxCollider roamingArea;
    [SerializeField] private float roamMinWait = 0.5f;
    [SerializeField] private float roamMaxWait = 1.8f;

    [Header("Target / Vision")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("เลเยอร์ที่บังสายตา เช่น ผนัง/ตู้/ประตู")]
    [SerializeField] private LayerMask losBlockerMask;
    [Tooltip("เลเยอร์ของผู้เล่น (รวมคอลลิเดอร์ลูก ๆ ให้ Ray ชนได้)")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float fov = 120f;

    [Header("Ranges (meters)")]
    [SerializeField] private float detectRadius = 12f;   // เริ่มเห็น
    [SerializeField] private float trickRadius = 8f;    // ระยะยั่ว/โคจร
    [SerializeField] private float chaseRadius = 16f;   // ระยะเร่งไล่
    [SerializeField] private float loseSightTime = 1.2f; // เวลาหลุดสายตาก่อนเปลี่ยนเป็นค้นหา

    [Header("Speeds")]
    [SerializeField] private float patrolSpeed = 1.6f;
    [SerializeField] private float roamSpeed = 1.6f;
    [SerializeField] private float huntSpeed = 2.2f;
    [SerializeField] private float trickSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 3.2f;

    [Header("Behaviour Timings")]
    [SerializeField] private float trickDuration = 2.5f;
    [SerializeField] private float searchRadius = 6f;
    [SerializeField] private float searchTime = 4.0f;

    [Header("Nav/Anti-Stuck")]
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float navSampleRadius = 2.0f;
    [SerializeField] private float stuckCheckTime = 1.5f;
    [SerializeField] private float stuckMinMove = 0.2f;

    [Header("Patrol Variance (กันจำแพทเทิร์น)")]
    [SerializeField] private float patrolWaitMin = 0.6f;
    [SerializeField] private float patrolWaitMax = 2.0f;
    [SerializeField] private float patrolJitterRadius = 1.0f;   // สุ่มรอบจุด
    [SerializeField] private float patrolNodeCooldown = 12f;     // ห้ามซ้ำภายใน X วินาที
    [SerializeField] private bool avoidImmediateBacktrack = true;
    [SerializeField] private bool useShufflePerLap = true;      // สุ่มเรียงจุดใหม่ทุก “รอบ”
    [SerializeField] private float detourChance = 0.25f;         // โอกาสแวะข้างทาง
    [SerializeField] private float detourRadius = 2.5f;          // ระยะจุดแวะ

    [Header("Close/Rear Awareness")]
    [SerializeField] private float closeProximityRadius = 1.6f; // ระยะใกล้มาก เห็นทันที 360°
    [SerializeField] private float rearAwarenessRadius = 3.0f; // ระยะด้านหลังที่ยอมให้เห็นแม้ไม่เข้า FOV
    [SerializeField] private bool requireLOSForClose = true; // ถ้า true ระยะใกล้ก็ยังต้องไม่มีกำแพงบัง

    // --- Runtime ---
    private GhostState state = GhostState.Patrol;
    private Transform player;
    private Vector3 lastKnownPlayerPos;
    private float lastSeenTimer = 0f;
    private float stateTimer = 0f;
    private float repathTimer = 0f;
    private Vector3 lastPos;
    private float stuckTimer = 0f;

    // Roam
    private bool roamWaiting = false;

    // Patrol variance
    private List<int> patrolOrder = new List<int>();
    private int patrolPtr = -1;
    private int lastPatrolIdx = -1;
    private Dictionary<int, float> nodeCooldownUntil = new Dictionary<int, float>();
    private bool patrolWaiting = false;

    // temporary buffs
    private float detectBonus = 0f, trickBonus = 0f, chaseBonus = 0f;

    void Reset() { agent = GetComponent<NavMeshAgent>(); }

    IEnumerator Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        yield return null; // ให้ NavMesh พร้อม 1 เฟรม

        EnsureOnNavMesh(3f);
        FindPlayerOnce();

        // เลือกโหมดเริ่มต้นอัตโนมัติ
        if (mode == PatrolMode.PatrolPoints && (patrolPoints == null || patrolPoints.Length == 0))
            mode = PatrolMode.RoamArea;

        if (mode == PatrolMode.PatrolPoints)
        {
            BuildPatrolOrder();
            GoNextPatrol();
            state = GhostState.Patrol;
            SetMoveSpeed(patrolSpeed);
        }
        else
        {
            if (!roamingArea) roamingArea = GetComponentInParent<BoxCollider>();
            GoNewRoamPoint();
            state = GhostState.Roam;
            SetMoveSpeed(roamSpeed);
        }

        lastPos = transform.position;
        if (animator) animator.SetInteger("GhostState", (int)state);
    }

    void Update()
    {
        stateTimer += Time.deltaTime;
        repathTimer += Time.deltaTime;
        if (!player) FindPlayerOnce();

        bool canSee = PlayerDetectWithLOS(out float distToPlayer);

        switch (state)
        {
            case GhostState.Patrol:
                PatrolTick(canSee, distToPlayer);
                break;
            case GhostState.Roam:
                RoamTick(canSee, distToPlayer);
                break;
            case GhostState.Hunt:
                HuntTick(canSee, distToPlayer);
                break;
            case GhostState.Trick:
                TrickTick(canSee, distToPlayer);
                break;
            case GhostState.Chase:
                ChaseTick(canSee, distToPlayer);
                break;
            case GhostState.SearchLost:
                SearchLostTick(canSee);
                break;
            case GhostState.Cooldown:
                if (stateTimer >= 0.4f) ChangeState(mode == PatrolMode.PatrolPoints ? GhostState.Patrol : GhostState.Roam,
                                                     mode == PatrolMode.PatrolPoints ? patrolSpeed : roamSpeed);
                break;
        }

        AntiStuckCheck();
#if UNITY_EDITOR
        if (player)
        {
            Color c = Color.gray;
            if (Vector3.Distance(transform.position, player.position) <= closeProximityRadius) c = Color.yellow;
            Debug.DrawLine(transform.position + Vector3.up * 1.7f, player.position + Vector3.up * 1.6f, c, 0.02f);
        }
#endif

    }
    bool PlayerDetectWithLOS(out float dist)
    {
        dist = Mathf.Infinity;
        if (!player) return false;

        Vector3 to = player.position - transform.position;
        dist = to.magnitude;
        float detectR = GetDetectRadius();

        // 0) อยู่นอกระยะตรวจจับใหญ่สุด → ไม่เห็น
        if (dist > detectR) return false;

        Vector3 eye = transform.position + Vector3.up * 1.7f;
        Vector3 head = player.position + Vector3.up * 1.6f;

        // 1) ระยะใกล้มากแบบ 360°: เห็นทันที (เลือกได้ว่าจะบังคับให้ "ต้องไม่มี blocker" ด้วย)
        if (dist <= closeProximityRadius)
        {
            if (!requireLOSForClose || ClearPathNoBlockers(eye, head, losBlockerMask))
                return true;
        }

        // 2) ด้านหลังในระยะสั้น: แม้ไม่เข้า FOV ก็ให้เห็นถ้าทางโล่ง
        // เช็กว่า "ไม่ได้อยู่ใน FOV ปกติ" ก่อน
        float angle = Vector3.Angle(transform.forward, to);
        if (angle > fov * 0.5f)
        {
            if (dist <= rearAwarenessRadius && ClearPathNoBlockers(eye, head, losBlockerMask))
                return true;

            // ไม่เข้า FOV และอยู่นอก rear radius → ไม่เห็น
            return false;
        }

        // 3) เคสปกติ: อยู่ใน FOV แล้ว → เช็ก LOS แบบเข้ม (ชน Player ก่อน/ไม่โดน blocker)
        return HasLineOfSight(player, detectR, playerMask, losBlockerMask, 1.7f);
    }

    // true = ไม่มีสิ่งใน losBlockerMask บังระหว่าง a -> b
    bool ClearPathNoBlockers(Vector3 a, Vector3 b, LayerMask blockers)
    {
        Vector3 dir = (b - a);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return true;
        // ยิง Ray ชนเฉพาะเลเยอร์ที่เป็น "ตัวบังสายตา"
        return !Physics.Raycast(a, dir.normalized, dist, blockers, QueryTriggerInteraction.Ignore);
    }

    void BuildPatrolOrder()
    {
        patrolOrder.Clear();
        if (patrolPoints == null) return;
        for (int i = 0; i < patrolPoints.Length; i++) patrolOrder.Add(i);
        if (useShufflePerLap) FisherYatesShuffle(patrolOrder);
        patrolPtr = -1;
        lastPatrolIdx = -1;
    }

    int PickNextPatrolIndex()
    {
        if (patrolOrder.Count == 0) return -1;

        int tries = patrolOrder.Count + 2;
        while (tries-- > 0)
        {
            patrolPtr++;
            if (patrolPtr >= patrolOrder.Count)
            {
                patrolPtr = 0;
                if (useShufflePerLap) FisherYatesShuffle(patrolOrder);
            }

            int idx = patrolOrder[patrolPtr];

            if (avoidImmediateBacktrack && patrolOrder.Count > 1 && idx == lastPatrolIdx)
                continue;

            if (nodeCooldownUntil.TryGetValue(idx, out float until) && Time.time < until)
                continue;

            nodeCooldownUntil[idx] = Time.time + patrolNodeCooldown;
            lastPatrolIdx = idx;
            return idx;
        }

        int fb = patrolOrder[patrolPtr];
        nodeCooldownUntil[fb] = Time.time + patrolNodeCooldown;
        lastPatrolIdx = fb;
        return fb;
    }

    void GoNextPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        int idx = PickNextPatrolIndex();
        if (idx < 0) return;

        Vector3 dst = patrolPoints[idx].position;

        // Detour
        if (Random.value < detourChance)
        {
            Vector2 r = Random.insideUnitCircle * detourRadius;
            dst += new Vector3(r.x, 0f, r.y);
        }

        // Jitter
        if (patrolJitterRadius > 0f)
        {
            Vector2 r = Random.insideUnitCircle * patrolJitterRadius;
            dst += new Vector3(r.x, 0f, r.y);
        }

        if (NavMesh.SamplePosition(dst, out NavMeshHit hit, patrolJitterRadius + 1.5f, agent.areaMask))
            SetDestinationSmooth(hit.position);
        else
            SetDestinationSmooth(patrolPoints[idx].position);
    }

    IEnumerator IE_PatrolPauseThenNext()
    {
        patrolWaiting = true;

        float wait = Random.Range(patrolWaitMin, patrolWaitMax);
        float t = 0f;
        float yaw0 = transform.eulerAngles.y;
        float yawT = yaw0 + Random.Range(-35f, 35f);
        while (t < wait)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Min(wait, 0.6f));
            float y = Mathf.LerpAngle(yaw0, yawT, a);
            transform.rotation = Quaternion.Euler(0f, y, 0f);
            yield return null;
        }

        GoNextPatrol();
        patrolWaiting = false;
    }

    void PatrolTick(bool canSee, float dist)
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            if (!patrolWaiting) StartCoroutine(IE_PatrolPauseThenNext());
        }

        if (canSee && dist <= GetDetectRadius())
        {
            lastKnownPlayerPos = player.position;
            ChangeState(GhostState.Hunt, huntSpeed);
        }
    }

    /* ===================== ROAM ===================== */
    void RoamTick(bool canSee, float dist)
    {
        if (!roamWaiting && agent.isOnNavMesh && !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            StartCoroutine(IE_WaitThenRoam());
        }

        if (canSee && dist <= GetDetectRadius())
        {
            lastKnownPlayerPos = player.position;
            ChangeState(GhostState.Hunt, huntSpeed);
        }
    }

    IEnumerator IE_WaitThenRoam()
    {
        roamWaiting = true;
        yield return new WaitForSeconds(Random.Range(roamMinWait, roamMaxWait));
        GoNewRoamPoint();
        roamWaiting = false;
    }

    void GoNewRoamPoint()
    {
        Vector3 target = GetRandomPointInArea();
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, navSampleRadius, agent.areaMask))
            SetDestinationSmooth(hit.position);
    }

    /* ===================== HUNT/TRICK/CHASE ===================== */
    void HuntTick(bool canSee, float dist)
    {
        if (canSee)
        {
            lastKnownPlayerPos = player.position;
            lastSeenTimer = 0f;

            if (dist <= GetTrickRadius())
            {
                ChangeState(GhostState.Trick, trickSpeed);
                return;
            }
            if (dist <= GetChaseRadius() && IsFacingPlayer())
            {
                ChangeState(GhostState.Chase, chaseSpeed);
                return;
            }

            SetDestinationSmooth(player.position);
        }
        else
        {
            lastSeenTimer += Time.deltaTime;
            if (lastSeenTimer >= loseSightTime) EnterSearchLost();
            else SetDestinationSmooth(lastKnownPlayerPos);
        }
    }

    void TrickTick(bool canSee, float dist)
    {
        if (!canSee) { EnterSearchLost(); return; }

        if (repathTimer >= repathInterval)
        {
            repathTimer = 0f;
            Vector3 dir = (transform.position - player.position).normalized;
            Vector3 orbit = player.position + Quaternion.Euler(0, Random.Range(-45f, 45f), 0) * dir * Mathf.Clamp(dist, 1.5f, 3f);
            if (NavMesh.SamplePosition(orbit, out NavMeshHit hit, 1.5f, agent.areaMask))
                agent.SetDestination(hit.position);
        }

        if (stateTimer >= trickDuration || dist <= Mathf.Min(GetTrickRadius() * 0.75f, GetChaseRadius()))
            ChangeState(GhostState.Chase, chaseSpeed);
    }

    void ChaseTick(bool canSee, float dist)
    {
        if (canSee)
        {
            lastKnownPlayerPos = player.position;
            SetDestinationSmooth(player.position);

            if (dist <= 1.5f)
            {
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                gameManager.KillPlayerNow();
            }
        }
        else
        {
            EnterSearchLost();
        }
    }


    

    bool HasLineOfSight(Transform player, float maxDist, LayerMask playerMask, LayerMask losBlockerMask, float eyeHeight = 1.7f)
    {
        if (!player) return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        // จุดเป้าหมายหลายตำแหน่งบนตัวผู้เล่น (หัว/อก/เอว) เพิ่มความทนทานเวลามีราวบังพอดี
        Vector3[] targets =
        {
        player.position + Vector3.up * 1.6f, // head
        player.position + Vector3.up * 1.2f, // chest
        player.position + Vector3.up * 0.9f  // waist
    };

        int mask = playerMask | losBlockerMask;

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 dir = (targets[i] - eye);
            float dist = dir.magnitude;
            if (dist > maxDist) continue;

            if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                // ถ้าโดน Player (หรือคอลลิเดอร์ย่อยของ Player) ก่อน → เห็น
                if (hit.collider.CompareTag("Player") ||
                    hit.collider.transform.root.CompareTag("Player") ||
                    (hit.collider.GetComponentInParent<Transform>()?.CompareTag("Player") ?? false))
                {
                    return true;
                }
                // ถ้าโดน LOSBlocker ก่อน → จุดนี้ไม่เห็น ลองจุดถัดไป
            }
            else
            {
                // ไม่โดนอะไรเลยภายในระยะ -> เปิดโล่ง ถือว่าเห็น
                return true;
            }
        }
        return false; // ทุกเส้นโดนตัวบังก่อนหมด → มองไม่เห็น
    }

    void SearchLostTick(bool canSee)
    {
        if (canSee) { ChangeState(GhostState.Hunt, huntSpeed); return; }

        if (repathTimer >= repathInterval)
        {
            repathTimer = 0f;
            Vector2 r = Random.insideUnitCircle * searchRadius;
            Vector3 p = lastKnownPlayerPos + new Vector3(r.x, 0, r.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, agent.areaMask))
                agent.SetDestination(hit.position);
        }
        if (stateTimer >= searchTime)
            ChangeState(mode == PatrolMode.PatrolPoints ? GhostState.Patrol : GhostState.Roam,
                        mode == PatrolMode.PatrolPoints ? patrolSpeed : roamSpeed);
    }

    /* ===================== HELPERS ===================== */
    void ChangeState(GhostState next, float moveSpeed)
    {
        state = next;
        stateTimer = 0f;
        repathTimer = 999f;
        SetMoveSpeed(moveSpeed);
        if (animator) animator.SetInteger("GhostState", (int)state);
    }

    void EnterSearchLost() => ChangeState(GhostState.SearchLost, huntSpeed);

    void SetMoveSpeed(float s)
    {
        agent.speed = s;
        agent.stoppingDistance = (state == GhostState.Chase) ? 1.0f : 0.5f;
        agent.isStopped = false;
    }

    void SetDestinationSmooth(Vector3 target)
    {
        if (repathTimer < repathInterval) return;
        repathTimer = 0f;
        if (!EnsureOnNavMesh(2f)) return;
        agent.SetDestination(target);
    }



    bool IsFacingPlayer()
    {
        if (!player) return false;
        Vector3 to = (player.position - transform.position).normalized;
        return Vector3.Dot(transform.forward, to) > 0.4f;
    }

    void FindPlayerOnce()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
    }

    void AntiStuckCheck()
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckCheckTime)
        {
            float moved = Vector3.Distance(transform.position, lastPos);
            if (moved < stuckMinMove && agent.hasPath)
            {
                agent.ResetPath();
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, agent.areaMask))
                    agent.Warp(hit.position);

                Vector3 nudge = transform.right * Random.Range(-1.5f, 1.5f) + transform.forward * Random.Range(1f, 2f);
                if (NavMesh.SamplePosition(transform.position + nudge, out hit, 2f, agent.areaMask))
                    agent.SetDestination(hit.position);
            }
            stuckTimer = 0f;
            lastPos = transform.position;
        }
    }

    bool EnsureOnNavMesh(float maxSnap)
    {
        if (agent.isOnNavMesh) return true;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, maxSnap, agent.areaMask))
            return agent.Warp(hit.position);
        return false;
    }

    // Bonus ranges API
    public void AddPermanentRange(float d, float t = 0f, float c = 0f)
    { detectRadius = Mathf.Max(0f, detectRadius + d); trickRadius = Mathf.Max(0f, trickRadius + t); chaseRadius = Mathf.Max(0f, chaseRadius + c); }
    public void AddTempRange(float d, float t, float c, float dur) { StartCoroutine(IE_AddTemp(d, t, c, dur)); }
    IEnumerator IE_AddTemp(float d, float t, float c, float dur)
    { detectBonus += d; trickBonus += t; chaseBonus += c; yield return new WaitForSeconds(dur); detectBonus -= d; trickBonus -= t; chaseBonus -= c; }
    float GetDetectRadius() => Mathf.Max(0f, detectRadius + detectBonus);
    float GetTrickRadius() => Mathf.Max(0f, trickRadius + trickBonus);
    float GetChaseRadius() => Mathf.Max(0f, chaseRadius + chaseBonus);

    // Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, GetDetectRadius());
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, GetTrickRadius());
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, GetChaseRadius());

        Vector3 left = Quaternion.Euler(0, -fov / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, fov / 2f, 0) * transform.forward;
        Gizmos.color = new Color(1, 1, 1, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + left * GetDetectRadius());
        Gizmos.DrawLine(transform.position, transform.position + right * GetDetectRadius());
    }

    // Utils
    void FisherYatesShuffle(List<int> a)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }

    Vector3 GetRandomPointInArea()
    {
        if (!roamingArea)
        {
            Vector2 r = Random.insideUnitCircle * 5f;
            return transform.position + new Vector3(r.x, 1f, r.y);
        }
        Vector3 center = roamingArea.transform.TransformPoint(roamingArea.center);
        Vector3 half = Vector3.Scale(roamingArea.size, roamingArea.transform.lossyScale) * 0.5f;
        float x = Random.Range(-half.x, half.x);
        float z = Random.Range(-half.z, half.z);
        Vector3 world = center + roamingArea.transform.right * x + roamingArea.transform.forward * z;
        world.y = center.y + 1f;
        return world;
    }
}

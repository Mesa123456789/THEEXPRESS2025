using UnityEngine;

public class SphereSpawn : MonoBehaviour
{
    [Header("Prefabs & Targets")]
    public GameObject spherePrefab;
    public Transform spawnPoint;        
    public BoxScript currentBox;

    [Header("Spawn Settings")]
    public int burstCount = 1;         
    public Vector3 localOffset = new Vector3(0, -0.5f, 0);
    [Range(0f, 0.5f)] public float spawnRadius = 0.06f;

    void Start()
    {
        currentBox = FindFirstObjectByType<BoxScript>();
    }

    void Update()
    {
        if (currentBox == null)
        {
            currentBox = FindFirstObjectByType<BoxScript>();
            if (currentBox != null) Debug.Log("เชื่อม currentBox สำเร็จ!");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            if (Physics.Raycast(ray, out RaycastHit hit, 3f) && hit.collider.CompareTag("Spherespawner"))
            {
                // สปอว์นเอฟเฟกต์ภาพ
                Transform root = (spawnPoint != null) ? spawnPoint : hit.collider.transform;
                SpawnBurst(root);

                // เพิ่มรอบบับเบิล "หนึ่งครั้ง" ต่อการคลิก
                currentBox.AddBubble();
            }
        }
    }

    void SpawnBurst(Transform root)
    {
        if (spherePrefab == null) { Debug.LogWarning("ยังไม่ได้ใส่ spherePrefab!"); return; }

        for (int i = 0; i < burstCount; i++)
        {
            Vector3 local = localOffset + (Random.insideUnitSphere * spawnRadius);
            Vector3 pos = root.TransformPoint(local);
            Quaternion rot = root.rotation;
            SpawnBubbleAt(pos, rot);
        }
    }

    void SpawnBubbleAt(Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(spherePrefab, pos, rot);

        // ฟิสิกส์ตก
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (go.GetComponent<Collider>() == null) go.AddComponent<SphereCollider>();

        // BubbleDrop — ใช้เฉพาะทำลายตัวเองหลังถึงกล่อง (ไม่เพิ่มบับเบิล)
        BubbleDrop drop = go.GetComponent<BubbleDrop>();
        if (drop == null) drop = go.AddComponent<BubbleDrop>();
        drop.targetBox = currentBox;
        drop.destroyDelay = 1.0f;
        drop.autoAddToBox = false; // สำคัญ! ห้ามเรียก AddBubble เอง
    }
}

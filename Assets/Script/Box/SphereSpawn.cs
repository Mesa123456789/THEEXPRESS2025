using UnityEngine;

public class SphereSpawn : MonoBehaviour
{
    [Header("Prefabs & Targets")]
    public GameObject spherePrefab;
    public Transform spawnPoint;          // จุดสปอว์น (หัวท่อ)
    public BoxScript currentBox;

    [Header("Spawn Settings")]
    public int burstCount = 10;           // จำนวนลูกที่ปล่อยต่อครั้ง
    public Vector3 localOffset = new Vector3(0, -0.5f, 0); // offset จากจุดสปอว์น (โลคัล)
    [Range(0f, 0.5f)] public float spawnRadius = 0.06f;    // กระจายตำแหน่งเล็กน้อย

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
            // ยิงจากจุดกึ่งกลางหน้าจอ
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            if (Physics.Raycast(ray, out RaycastHit hit, 3f) && hit.collider.CompareTag("Spherespawner"))
            {
                // ถ้าไม่ได้ตั้ง spawnPoint จะใช้ทรานสฟอร์มของตัวที่โดนแทน
                Transform root = (spawnPoint != null) ? spawnPoint : hit.collider.transform;
                SpawnBurst(root);
            }
        }
    }

    void SpawnBurst(Transform root)
    {
        for (int i = 0; i < burstCount; i++)
        {
            // จุดสปอว์น = ตำแหน่งโลคัล (offset + สุ่มรัศมีเล็กน้อย) แปลงเป็นเวิลด์ด้วย TransformPoint
            Vector3 local = localOffset + (Random.insideUnitSphere * spawnRadius);
            Vector3 pos = root.TransformPoint(local);
            Quaternion rot = root.rotation;

            SpawnBubbleAt(pos, rot);
        }
    }

    void SpawnBubbleAt(Vector3 pos, Quaternion rot)
    {
        if (spherePrefab == null)
        {
            Debug.LogWarning("ยังไม่ได้ใส่ spherePrefab!");
            return;
        }
        if (currentBox == null)
        {
            Debug.LogWarning("ยังไม่เจอ currentBox!");
            return;
        }

        GameObject go = Instantiate(spherePrefab, pos, rot);

        // ให้ตกด้วยฟิสิกส์
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // กัน prefab ไม่มี Collider
        if (go.GetComponent<Collider>() == null)
        {
            var col = go.AddComponent<SphereCollider>();
            col.material = null;
        }

        // Setup BubbleDrop
        BubbleDrop drop = go.GetComponent<BubbleDrop>();
        if (drop == null) drop = go.AddComponent<BubbleDrop>();
        drop.targetBox = currentBox;
        drop.destroyDelay = 1.0f;     // ทำลายหลังถึงกล่อง ~1 วิ
        drop.autoAddToBox = true;     // เรียก AddBubble() อัตโนมัติเมื่อถึงกล่อง
    }

    // ยังเก็บไว้ใช้กรณีกดเพิ่มตรง ๆ
    void InsertBubble()
    {
        if (currentBox != null && currentBox.hasItem) currentBox.AddBubble();
        else Debug.Log("ยังไม่มีของในกล่องหรือยังไม่ได้อ้างอิงกล่อง!");
    }
}

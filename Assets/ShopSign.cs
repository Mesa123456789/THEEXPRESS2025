using UnityEngine;
using System.Collections;

public class ShopSign : MonoBehaviour
{
    [Header("Materials")]
    public Material openMaterial;
    public Material closedMaterial;

    [Header("Mesh Renderer ของป้าย")]
    public MeshRenderer signRenderer;

    public GameManager gameManager;
    bool isOpen = false;
    Coroutine rotateCo;

    void Start()
    {
        isOpen = gameManager.shopIsOpen;
        if (!signRenderer) signRenderer = GetComponent<MeshRenderer>();
        UpdateSign();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("sign"))
            {
                Debug.Log("press");
                isOpen = !isOpen;
                gameManager.shopIsOpen = isOpen;
                UpdateSign();
            }
        }
    }

    void UpdateSign()
    {
        if (signRenderer)
            signRenderer.material = isOpen ? openMaterial : closedMaterial;

        if (rotateCo != null) StopCoroutine(rotateCo);
        rotateCo = StartCoroutine(RotateThroughPoints());
    }

    IEnumerator RotateThroughPoints()
    {
        // ตั้ง waypoint ตามที่คุณบอก
        Vector3[] points = new Vector3[]
        {
            new Vector3(0, 0, 90),
            new Vector3(0, 0, 130),
            new Vector3(0, 180, 60),
            new Vector3(0, 180, 90)
        };

        float stepDuration = 0.3f; // ความเร็วต่อ segment
        for (int i = 0; i < points.Length - 1; i++)
        {
            Quaternion start = Quaternion.Euler(points[i]);
            Quaternion end = Quaternion.Euler(points[i + 1]);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / stepDuration;
                transform.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }
        }

        // จบที่เป้าหมายสุดท้าย
        transform.localRotation = Quaternion.Euler(points[points.Length - 1]);
    }
}

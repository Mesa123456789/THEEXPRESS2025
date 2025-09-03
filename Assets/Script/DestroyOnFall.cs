using UnityEngine;

public class DestroyOnFall : MonoBehaviour
{
    float thresholdY; // จุดที่อยากให้หายไป

    void Start()
    {
        thresholdY = transform.position.y - 2f; // เมื่อตกลงไป 0.5 จากจุด spawn
    }

    void Update()
    {
        if (transform.position.y <= thresholdY)
        {
            Destroy(gameObject);
        }
    }
}

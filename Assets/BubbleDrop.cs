using System.Collections;
using UnityEngine;

public class BubbleDrop : MonoBehaviour
{
    [Tooltip("อ้างอิงกล่องปลายทาง")]
    public BoxScript targetBox;

    [Tooltip("หน่วงเวลาก่อนทำลายหลังจากตกถึงกล่อง (วินาที)")]
    public float destroyDelay = 1.0f;

    [Tooltip("เรียก targetBox.AddBubble() อัตโนมัติเมื่อถึงกล่อง")]
    public bool autoAddToBox = true;

    private bool counted = false;

    void OnCollisionEnter(Collision collision)
    {
        TryHandleContact(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryHandleContact(other);
    }

    private void TryHandleContact(Collider col)
    {
        if (counted) return;

        // หา BoxScript จากคอลลิดเดอร์หรือพาเรนต์
        BoxScript box = col.GetComponentInParent<BoxScript>();
        if (box == null) return; // ยังไม่ใช่กล่อง

        counted = true;

        if (autoAddToBox && targetBox != null)
        {
            targetBox.AddBubble();
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}

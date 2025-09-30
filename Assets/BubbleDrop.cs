using System.Collections;
using UnityEngine;

public class BubbleDrop : MonoBehaviour
{
    public BoxScript targetBox;
    public float destroyDelay = 1.0f;
    public bool autoAddToBox = false; // ปิดไว้

    private bool hitBox = false;

    void OnCollisionEnter(Collision collision) => TryHit(collision.collider);
    void OnTriggerEnter(Collider other) => TryHit(other);

    void TryHit(Collider col)
    {
        if (hitBox) return;
        if (targetBox == null) return;

        // ชนอะไรที่มี/อยู่ใต้ BoxScript (กล่องเป้าหมาย)
        var box = col.GetComponentInParent<BoxScript>();
        if (box != targetBox) return;

        hitBox = true;
        StartCoroutine(DestroyLater());
    }

    IEnumerator DestroyLater()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}

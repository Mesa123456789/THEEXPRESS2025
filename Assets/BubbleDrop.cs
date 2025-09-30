using System.Collections;
using UnityEngine;

public class BubbleDrop : MonoBehaviour
{
    [Tooltip("��ҧ�ԧ���ͧ���·ҧ")]
    public BoxScript targetBox;

    [Tooltip("˹�ǧ���ҡ�͹�������ѧ�ҡ���֧���ͧ (�Թҷ�)")]
    public float destroyDelay = 1.0f;

    [Tooltip("���¡ targetBox.AddBubble() �ѵ��ѵ�����Ͷ֧���ͧ")]
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

        // �� BoxScript �ҡ����Դ�������;��ù��
        BoxScript box = col.GetComponentInParent<BoxScript>();
        if (box == null) return; // �ѧ�������ͧ

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

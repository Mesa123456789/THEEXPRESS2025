using UnityEngine;

[DisallowMultipleComponent]
public class ReceiptItem : MonoBehaviour
{
    [Header("ขนาดรอยเท้าบนระนาบ XZ (ครึ่งกว้าง/ครึ่งยาว)")]
    public Vector2 halfSizeXZ = new Vector2(0.05f, 0.08f); // 10x16 ซม. ตัวอย่าง

    [Tooltip("ยกสูงจากผิวเล็กน้อยกัน Z-fighting")]
    public float surfaceOffset = 0.001f;

    [Header("Physics")]
    public Rigidbody rb;

    
    void Reset()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }
}

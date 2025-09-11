using UnityEngine;

public class TapeDispenser : MonoBehaviour
{
    public Material tapeMaterial; // 1 ลายต่อเครื่อง

    public Material GetMaterial()
    {
        return tapeMaterial;
    }
}
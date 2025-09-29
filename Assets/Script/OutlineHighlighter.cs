using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class OutlineHighlighter : MonoBehaviour
{
    public Material outlineMaterial;
    [Range(1.0f, 1.1f)] public float outlineScale = 1.03f;

    private readonly List<GameObject> clones = new();

    void Awake()
    {
        if (!outlineMaterial) return;

        foreach (var mf in GetComponentsInChildren<MeshFilter>())
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mr) continue;

            var clone = new GameObject("__outline");
            clone.transform.SetParent(mf.transform, false);
            clone.transform.localScale = Vector3.one * outlineScale;

            var mfClone = clone.AddComponent<MeshFilter>();
            mfClone.sharedMesh = mf.sharedMesh;

            var mrClone = clone.AddComponent<MeshRenderer>();
            mrClone.sharedMaterial = outlineMaterial;
            mrClone.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mrClone.receiveShadows = false;
            mrClone.enabled = false;

            clones.Add(clone);
        }
    }

    public void SetVisible(bool state)
    {
        foreach (var c in clones)
        {
            if (!c) continue;
            var r = c.GetComponent<MeshRenderer>();
            if (r) r.enabled = state;
        }
    }
}

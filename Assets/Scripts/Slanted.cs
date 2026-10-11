using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
// A UI image leaning to the right, optionally fading from one colour at the top to another at the bottom.
public class Slanted : Image
{
    public float slant = 0.12f; // sideways shift per unit of height
    public bool fade;
    public Color bottom = Color.white;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);
        Rect r = rectTransform.rect;
        UIVertex v = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float t = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y);
            v.position.x += (t - 0.5f) * r.height * slant;
            if (fade) v.color = Color.Lerp(bottom, color, t);
            vh.SetUIVertex(v, i);
        }
    }
}



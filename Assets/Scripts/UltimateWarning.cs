using UnityEngine;

// Draw the warning shape without depending on a font's special-symbol coverage.
public class UltimateWarning : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        mesh.AddVert(new Vector3(rect.center.x, rect.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
        mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
    }
}

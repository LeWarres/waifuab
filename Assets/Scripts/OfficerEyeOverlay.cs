using UnityEngine;

// Only the eyelid regions come from the animation frames; the rest stays on the original illustration.
public class OfficerEyeOverlay : UnityEngine.UI.MaskableGraphic
{
    Texture frame;
    public bool alien;
    public override Texture mainTexture => frame ? frame : Texture2D.whiteTexture;

    public void Show(Texture texture)
    {
        if (frame == texture) return;
        frame = texture;
        enabled = frame;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Eye(mesh, alien ? new Rect(0.548f, 0.602f, 0.065f, 0.098f) : new Rect(0.527f, 0.602f, 0.081f, 0.114f));
        Eye(mesh, alien ? new Rect(0.626f, 0.646f, 0.062f, 0.100f) : new Rect(0.613f, 0.644f, 0.077f, 0.120f));
    }

    void Eye(UnityEngine.UI.VertexHelper mesh, Rect uv)
    {
        Rect bounds = rectTransform.rect;
        int start = mesh.currentVertCount;
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float u = x == 0 ? uv.xMin : x == 1 ? uv.xMin + uv.width * 0.13f : x == 2 ? uv.xMax - uv.width * 0.13f : uv.xMax;
            float v = y == 0 ? uv.yMin : y == 1 ? uv.yMin + uv.height * 0.13f : y == 2 ? uv.yMax - uv.height * 0.13f : uv.yMax;
            // Feather the perimeter so tiny skin-colour differences do not leave rectangular seams.
            Color tint = new Color(1f, 1f, 1f, x == 0 || x == 3 || y == 0 || y == 3 ? 0f : 1f);
            mesh.AddVert(new Vector3(bounds.xMin + u * bounds.width, bounds.yMin + v * bounds.height), tint, new Vector2(u, v));
        }
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            int n = start + y * 4 + x;
            mesh.AddTriangle(n, n + 4, n + 5);
            mesh.AddTriangle(n, n + 5, n + 1);
        }
    }
}

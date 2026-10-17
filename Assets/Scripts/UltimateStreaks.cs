using UnityEngine;

// Canvas geometry keeps the glow moving while gameplay time is stopped.
public class UltimateStreaks : UnityEngine.UI.MaskableGraphic
{
    public Color powerColor = new Color(0.35f, 1f, 0.5f);
    public Color accentColor = new Color(0.65f, 1f, 0.75f);
    float age, bandHeight, bandX, bandWidth;

    public void Pose(float seconds, float height, float x, float width)
    {
        age = seconds; bandHeight = height; bandX = x; bandWidth = width;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect screen = rectTransform.rect;
        float fade = Mathf.Min(Mathf.Clamp01(age / 0.07f), Mathf.Clamp01((1f - age) / 0.15f));
        if (fade <= 0f) return;
        float rush = age < 0.18f || age > 0.8f ? 1f : 0.65f;
        for (int i = 0; i < 36; i++)
        {
            float seed = Mathf.Repeat(i * 0.618034f, 1f);
            float x = Mathf.Repeat(seed + age * (1.7f + i % 4 * 0.35f), 1f) * (screen.width + 700f) - (screen.width + 700f) * 0.5f;
            float space = Mathf.Max(30f, (screen.height - bandHeight) * 0.5f - 28f);
            float y = (bandHeight * 0.5f + 16f + Mathf.Repeat(i * 0.381966f, 1f) * space) * (i % 2 == 0 ? 1f : -1f);
            Color tint = i % 3 == 0 ? accentColor : powerColor;
            tint.a = fade * rush * 0.75f;
            Vector2 at = new Vector2(x, y);
            float length = 100f + seed * 220f;
            Glow(mesh, at, length, 7f + seed * 6f, tint);
            Glow(mesh, at, length * 0.85f, 1.6f, new Color(1f, 1f, 1f, fade * rush));
        }

        // Bright rails and travelling starbursts frame the illustration without crossing her eyes.
        for (int side = -1; side <= 1; side += 2)
        {
            float y = side * bandHeight * 0.5f;
            Color glow = powerColor;
            glow.a = fade;
            Glow(mesh, new Vector2(bandX, y), bandWidth * 0.54f, 14f, glow);
            Glow(mesh, new Vector2(bandX, y), bandWidth * 0.52f, 2.5f, new Color(1f, 1f, 1f, fade));
            for (int i = 0; i < 3; i++)
            {
                float x = bandX + (Mathf.Repeat(i / 3f + age * 1.5f, 1f) - 0.5f) * bandWidth;
                float pulse = 0.6f + 0.4f * Mathf.Sin(age * 20f + i * 2f + side);
                Vector2 at = new Vector2(x, y);
                Glow(mesh, at, 60f * pulse, 6f, glow);
                Glow(mesh, at, 5f, 34f * pulse, new Color(1f, 1f, 1f, fade));
            }
        }
    }

    static void Glow(UnityEngine.UI.VertexHelper mesh, Vector2 at, float width, float height, Color tint)
    {
        int n = mesh.currentVertCount;
        Color edge = new Color(tint.r, tint.g, tint.b, 0f);
        mesh.AddVert(at, tint, Vector2.zero);
        mesh.AddVert(at + Vector2.left * width, edge, Vector2.zero);
        mesh.AddVert(at + Vector2.up * height, edge, Vector2.zero);
        mesh.AddVert(at + Vector2.right * width, edge, Vector2.zero);
        mesh.AddVert(at + Vector2.down * height, edge, Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        mesh.AddTriangle(n, n + 3, n + 4); mesh.AddTriangle(n, n + 4, n + 1);
    }
}

using UnityEngine;
using UnityEngine.UI;

// Canvas geometry keeps clouds, atmosphere and stars seamless at every aspect ratio.
public class AscentBackdrop : MaskableGraphic
{
    public float flight, travel;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float space = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, .95f, flight));
        Color bottom = Color.Lerp(new Color(.6f, .84f, .96f), new Color(.09f, .13f, .29f), space);
        Color top = Color.Lerp(new Color(.12f, .46f, .79f), new Color(.015f, .025f, .09f), space);
        Quad(mesh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), bottom, top);
        // Distant stars become visible as the atmosphere thins.
        for (int i = 0; i < 110; i++)
        {
            float x = Mathf.Repeat(i * .618034f + .17f, 1f);
            float y = Mathf.Repeat(i * .414214f + .07f - travel * .006f, 1f);
            Vector2 at = new Vector2(Mathf.Lerp(r.xMin, r.xMax, x), Mathf.Lerp(r.yMin, r.yMax, y));
            float size = i % 9 == 0 ? 3f : 1.5f;
            Color tint = new Color(.8f, .91f, 1f, space * (.5f + .3f * Mathf.Sin(i + travel * 1.2f)));
            Quad(mesh, at - new Vector2(size, size), at + new Vector2(size, size), tint, tint);
            if (i % 9 == 0)
            {
                Quad(mesh, at - new Vector2(size * 3f, .7f), at + new Vector2(size * 3f, .7f), tint, tint);
                Quad(mesh, at - new Vector2(.7f, size * 3f), at + new Vector2(.7f, size * 3f), tint, tint);
            }
        }
        // Cloud banks scroll downward at different speeds, making the train's ascent visible.
        for (int i = 0; i < 13; i++)
        {
            float x = Mathf.Repeat(i * .731f, 1f);
            float y = Mathf.Repeat(i * .271f - travel * (.018f + (i % 3) * .009f), 1.4f) - .2f;
            Vector2 at = new Vector2(Mathf.Lerp(r.xMin, r.xMax, x), Mathf.Lerp(r.yMin, r.yMax, y));
            float scale = 90f + (i % 4) * 35f;
            Color tint = new Color(.92f, .97f, 1f, (1f - space) * (i % 2 == 0 ? .55f : .3f));
            Cloud(mesh, at, scale, tint);
        }
        // Last glimpse of the curved planet below, sliding out of view.
        if (space > 0f)
        {
            Vector2 center = new Vector2(r.center.x, r.yMin - r.width * .56f - space * 210f);
            Oval(mesh, center, new Vector2(r.width * .8f, r.width * .64f), new Color(.28f, .7f, 1f, space * .35f));
            Oval(mesh, center - new Vector2(0f, 14f), new Vector2(r.width * .8f, r.width * .64f), new Color(.12f, .34f, .58f, space));
        }
        for (int i = 0; i < 16; i++)
        {
            float x = Mathf.Repeat(i * .713f, 1f);
            float y = Mathf.Repeat(i * .317f - travel * .35f, 1f);
            Vector2 at = new Vector2(Mathf.Lerp(r.xMin, r.xMax, x), Mathf.Lerp(r.yMin, r.yMax, y));
            Color tint = new Color(.8f, .93f, 1f, .18f);
            Quad(mesh, at, at + new Vector2(1.5f, 35f + (i % 3) * 15f), Color.clear, tint);
        }
    }

    static void Quad(VertexHelper mesh, Vector2 min, Vector2 max, Color bottom, Color top)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(new Vector3(min.x, min.y), bottom, Vector2.zero);
        mesh.AddVert(new Vector3(min.x, max.y), top, Vector2.zero);
        mesh.AddVert(new Vector3(max.x, max.y), top, Vector2.zero);
        mesh.AddVert(new Vector3(max.x, min.y), bottom, Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2);
        mesh.AddTriangle(n, n + 2, n + 3);
    }

    static void Oval(VertexHelper mesh, Vector2 center, Vector2 radius, Color tint)
    {
        const int segments = 40;
        int n = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y), tint, Vector2.zero);
            if (i > 0) mesh.AddTriangle(n, n + i, n + i + 1);
        }
    }

    // One silhouette prevents overlapping transparent circles from looking like glass bubbles.
    static void Cloud(VertexHelper mesh, Vector2 center, float scale, Color tint)
    {
        const int steps = 32;
        int n = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        for (int i = 0; i <= steps * 2; i++)
        {
            bool upper = i <= steps;
            float x = upper ? Mathf.Lerp(-1.9f, 1.9f, i / (float)steps) : Mathf.Lerp(1.9f, -1.9f, (i - steps) / (float)steps);
            float y = .36f * Mathf.Sqrt(Mathf.Max(0f, 1f - x * x / (1.9f * 1.9f)));
            if (upper)
            {
                if (Mathf.Abs(x + .5f) < .8f) y = Mathf.Max(y, .25f + .6f * Mathf.Sqrt(1f - Mathf.Pow((x + .5f) / .8f, 2f)));
                if (Mathf.Abs(x - .35f) < .95f) y = Mathf.Max(y, .35f + .8f * Mathf.Sqrt(1f - Mathf.Pow((x - .35f) / .95f, 2f)));
            }
            else y = -y;
            mesh.AddVert(center + new Vector2(x, y) * scale, tint, Vector2.zero);
            if (i > 0) mesh.AddTriangle(n, n + i, n + i + 1);
        }
    }
}

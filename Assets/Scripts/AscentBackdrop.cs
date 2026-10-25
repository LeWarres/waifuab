using UnityEngine;
using UnityEngine.UI;

// Uses the same painted skies as the gameplay biomes, with a continuous atmospheric crossfade.
public class AscentBackdrop : MaskableGraphic
{
    public float flight, travel;
    public bool descending;
    RawImage sky, space;

    public void Refresh(float progress, float dt)
    {
        flight = descending ? 1f - progress : progress;
        travel += dt;
        if (!sky)
        {
            sky = Painting("CityAtmosphere", "Sky/city", 0);
            space = Painting("SpaceNebula", "Sky/space", 1);
            transform.SetAsLastSibling();
        }
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.58f, .94f, flight));
        sky.color = Color.white;
        space.color = new Color(1f, 1f, 1f, blend);
        Frame(sky, .08f + Mathf.Clamp01(flight) * .15f);
        Frame(space, .06f + Mathf.Clamp01(flight) * .08f);
        SetVerticesDirty();
    }

    RawImage Painting(string name, string path, int order)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(transform.parent, false);
        image.transform.SetSiblingIndex(order);
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        image.texture = Resources.Load<Texture2D>(path);
        image.raycastTarget = false;
        return image;
    }

    void Frame(RawImage painting, float rise)
    {
        if (!painting.texture) return;
        Rect r = rectTransform.rect;
        float aspect = r.width / Mathf.Max(1f, r.height);
        float sourceAspect = (float)painting.texture.width / painting.texture.height;
        float height = .76f, width = height * aspect / sourceAspect;
        if (width > 1f) { width = 1f; height = sourceAspect / aspect; }
        painting.uvRect = new Rect((1f - width) * .5f, Mathf.Min(rise, 1f - height), width, height);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        // Thin peripheral air trails leave the central play area and obstacle silhouettes clear.
        for (int i = 0; i < 20; i++)
        {
            float x = Mathf.Repeat(i * .713f, 1f);
            if (x > .28f && x < .72f) continue;
            float y = Mathf.Repeat(i * .317f + travel * (descending ? .32f : -.32f), 1f);
            Vector2 at = new Vector2(Mathf.Lerp(r.xMin, r.xMax, x), Mathf.Lerp(r.yMin, r.yMax, y));
            int n = mesh.currentVertCount;
            Color tint = new Color(.77f, .9f, 1f, .16f);
            mesh.AddVert(at, Color.clear, Vector2.zero);
            mesh.AddVert(at + new Vector2(0f, 75f), tint, Vector2.zero);
            mesh.AddVert(at + new Vector2(1.4f, 75f), tint, Vector2.zero);
            mesh.AddVert(at + new Vector2(1.4f, 0f), Color.clear, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2);
            mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}

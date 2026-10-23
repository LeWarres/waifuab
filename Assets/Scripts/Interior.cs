using System.Collections.Generic;
using UnityEngine;

// What the two closed rooms are dressed with, the wagon a fight happens in (BrawlEvent) and Faceless's cave
// (CaveEvent): the props of Tools/make_interior_props.py (Resources/Interior), real lamps, and textures drawn here
// (planks, panelling, riveted steel, rock) on the plain boxes of their floors, walls and roofs.
public static class Interior
{
    static readonly Dictionary<string, GameObject> kits = new();

    // A prop from Resources/Interior, standing at `at` (its base), turned and scaled.
    public static Transform Place(string name, Transform parent, Vector3 at, float turn = 0f, float scale = 1f)
    {
        if (!kits.TryGetValue(name, out GameObject kit) || !kit) kits[name] = kit = Resources.Load<GameObject>("Interior/" + name);
        Transform made = Object.Instantiate(kit, parent, false).transform;
        made.localPosition = at;
        made.localRotation = Quaternion.Euler(0f, turn, 0f);
        made.localScale = Vector3.one * scale;
        foreach (Renderer part in made.GetComponentsInChildren<Renderer>()) part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return made;
    }

    // Everything a placed prop draws, in the world.
    public static Bounds Bounds(Transform prop)
    {
        Bounds box = default;
        bool any = false;
        foreach (Renderer part in prop.GetComponentsInChildren<Renderer>())
        {
            if (any) box.Encapsulate(part.bounds); else box = part.bounds;
            any = true;
        }
        return box;
    }

    // A real light (HDRP's needs its own data to be drawn at all); lumen: how much light, range: how far.
    public static Light Lamp(Transform parent, Vector3 at, Color colour, float lumen, float range, bool shadows, LightType type = LightType.Point)
    {
        var go = new GameObject(type == LightType.Spot ? "Spot" : "Lamp");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        var light = go.AddComponent<Light>();
        go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
        light.type = type;
        light.color = colour;
        light.range = range;
        light.lightUnit = UnityEngine.Rendering.LightUnit.Lumen;
        light.intensity = lumen;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        return light;
    }

    // A box with a texture over it, `tiles` times across each face.
    static readonly Dictionary<(Texture2D, Color, Vector2), Material> textured = new();
    public static Material Textured(Texture2D texture, Color tint, Vector2 tiles)
    {
        if (textured.TryGetValue((texture, tint, tiles), out Material m) && m) return m;
        m = TrainSim.Mat(tint);
        m.mainTexture = texture;
        m.mainTextureScale = tiles;
        return textured[(texture, tint, tiles)] = m;
    }

    public static Transform Box(Transform parent, Vector3 at, Vector3 size, Material m, float turn = 0f, bool shadows = false) // no shadow by default: the sun lights the rooms through their walls and roofs
    {
        Transform block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        Object.Destroy(block.GetComponent<Collider>());
        block.SetParent(parent, false);
        block.localPosition = at;
        block.localRotation = Quaternion.Euler(0f, 0f, turn);
        block.localScale = size;
        var look = block.GetComponent<Renderer>();
        look.sharedMaterial = m;
        look.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        return block;
    }

    // ---------------- textures, drawn once ----------------

    static Texture2D planks, panels, riveted, rock;

    static Texture2D Make(int size, System.Func<int, int, Color> paint)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++) px[y * size + x] = paint(x, y);
        texture.SetPixels(px);
        texture.Apply(true);
        return texture;
    }

    // Floor boards running along u: four a tile, each its own shade, with grain, a dark gap between, a joint
    // somewhere along and a pair of nails either side of it.
    public static Texture2D Planks() => planks ? planks : planks = Make(512, (x, y) =>
    {
        const int boards = 4, size = 512;
        int board = y * boards / size;
        float across = (y * boards % size) / (float)size;           // 0..1 across this board
        float joint = Mathf.Repeat(0.37f * board + 0.21f, 1f);       // where this board's two pieces meet
        float u = x / (float)size;
        bool gap = across < 0.035f || Mathf.Abs(u - joint) < 0.004f;
        float shade = 0.82f + 0.18f * Mathf.PerlinNoise(board * 3.1f + (u < joint ? 0f : 7f), 0.5f);
        float grain = Mathf.PerlinNoise(u * 6f + board * 11f, across * 40f) * 0.25f + Mathf.PerlinNoise(u * 40f, across * 4f + board) * 0.1f;
        float v = shade * (0.8f + grain);
        bool nail = false;
        foreach (float n in new[] { joint - 0.02f, joint + 0.02f })
            foreach (float a in new[] { 0.3f, 0.75f })
                if (new Vector2((u - Mathf.Repeat(n, 1f)) * 4f, across - a).magnitude < 0.03f) nail = true;
        if (gap) v = 0.25f;
        if (nail) return new Color(0.25f, 0.25f, 0.28f);
        return new Color(v, v, v);
    });

    // Upright boards with grooves between them and a rail along the top: the wainscot under the windows.
    public static Texture2D Panels() => panels ? panels : panels = Make(256, (x, y) =>
    {
        float u = x / 256f, v = y / 256f;
        float board = Mathf.Repeat(u * 4f, 1f);
        float shade = 0.85f + 0.15f * Mathf.PerlinNoise(Mathf.Floor(u * 4f) * 5f, 1f);
        float grain = Mathf.PerlinNoise(u * 30f, v * 3f) * 0.2f;
        float c = shade * (0.85f + grain);
        if (board < 0.04f || board > 0.96f) c *= 0.45f;     // the grooves
        if (v > 0.86f) c = 0.65f + (v > 0.93f ? 0.15f : 0f); // the rail
        if (v < 0.06f) c = 0.4f;                             // the skirting
        return new Color(c, c, c);
    });

    // Steel plates with seams and rows of rivets along them.
    public static Texture2D Riveted() => riveted ? riveted : riveted = Make(256, (x, y) =>
    {
        float u = x / 256f, v = y / 256f;
        float c = 0.82f + 0.12f * Mathf.PerlinNoise(u * 8f, v * 8f) - 0.06f * Mathf.PerlinNoise(u * 60f, v * 60f);
        float su = Mathf.Repeat(u * 2f, 1f), sv = Mathf.Repeat(v * 2f, 1f);
        if (su < 0.012f || sv < 0.012f) c = 0.45f;                       // the seams
        float ru = Mathf.Repeat(u * 2f, 1f), rv = Mathf.Repeat(v * 2f, 1f);
        for (int k = 0; k < 8; k++)
        {
            float along = (k + 0.5f) / 8f;
            if (new Vector2(ru - 0.05f, rv - along).magnitude < 0.018f || new Vector2(ru - along, rv - 0.05f).magnitude < 0.018f) c = 1f;
        }
        return new Color(c, c, c);
    });

    // Rough stone: blotches, cracks.
    public static Texture2D Rock() => rock ? rock : rock = Make(256, (x, y) =>
    {
        float u = x / 256f * 4f, v = y / 256f * 4f;
        float c = 0.6f + 0.35f * Mathf.PerlinNoise(u, v) + 0.15f * Mathf.PerlinNoise(u * 5f, v * 5f);
        float crack = Mathf.Abs(Mathf.PerlinNoise(u * 2f + 9f, v * 2f) - 0.5f);
        if (crack < 0.02f) c *= 0.45f;
        return new Color(c, c, c);
    });
}

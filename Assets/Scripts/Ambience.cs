using System.Collections.Generic;
using UnityEngine;

// The relief out to the sides of the ride (hills, mesas, dunes, islets, crags): made here, handed to TrainSim's
// scenery as its biggest props, in the game's own toon look.
public static class Ambience
{
    // ---------------- the relief ----------------

    // Templates of each biome's relief (inactive; TrainSim's scenery copies them like any other big prop).
    public static List<GameObject> Relief(string id, Transform parent)
    {
        var made = new List<GameObject>();
        void Add(string name, Mesh mesh)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var look = go.AddComponent<MeshRenderer>();
            look.sharedMaterial = Painted();
            made.Add(go);
        }
        switch (id)
        {
            case "desert":
                Add("Dune", Mound(11, 20f, 4f, 0.35f, 2.2f, new Color(0.92f, 0.74f, 0.45f), new Color(1f, 0.86f, 0.6f), -1f));
                Add("Mesa", Mesa(12, 16f, 8f, new Color(0.78f, 0.42f, 0.26f), new Color(0.95f, 0.62f, 0.38f)));
                Add("Butte", Mesa(13, 9f, 6f, new Color(0.72f, 0.38f, 0.24f), new Color(0.9f, 0.55f, 0.34f)));
                break;
            case "snow":
                Add("SnowHill", Mound(21, 18f, 6f, 0.5f, 1.6f, new Color(0.62f, 0.66f, 0.74f), Color.white, 0.4f));
                Add("SnowPeak", Mound(22, 12f, 9f, 0.6f, 1f, new Color(0.5f, 0.54f, 0.62f), Color.white, 0.55f));
                break;
            case "jungle":
                Add("GreenHill", Mound(31, 20f, 6f, 0.45f, 1.5f, new Color(0.15f, 0.38f, 0.14f), new Color(0.35f, 0.62f, 0.2f), -1f));
                Add("Knoll", Mound(32, 11f, 4f, 0.4f, 1.8f, new Color(0.2f, 0.42f, 0.16f), new Color(0.42f, 0.7f, 0.25f), -1f));
                break;
            case "ocean":
                Add("Islet", Mound(41, 14f, 4f, 0.6f, 1.4f, new Color(0.55f, 0.48f, 0.4f), new Color(0.35f, 0.6f, 0.25f), 0.6f));
                Add("SeaRock", Mound(42, 7f, 5f, 0.7f, 1f, new Color(0.45f, 0.42f, 0.4f), new Color(0.6f, 0.58f, 0.55f), -1f));
                break;
            case "volcano":
                Add("Crag", Mound(51, 12f, 10f, 0.75f, 0.9f, new Color(0.16f, 0.1f, 0.09f), new Color(0.3f, 0.18f, 0.14f), -1f));
                Add("Cinder", Mound(52, 16f, 5f, 0.5f, 1.4f, new Color(0.2f, 0.12f, 0.1f), new Color(1f, 0.4f, 0.1f), 0.85f));
                break;
            case "alien":
                Add("HexMesa", Mesa(61, 14f, 7f, new Color(0.35f, 0.22f, 0.5f), new Color(0.55f, 0.38f, 0.75f), 6));
                Add("Spire", Mound(62, 8f, 11f, 0.8f, 0.8f, new Color(0.3f, 0.2f, 0.45f), new Color(0.3f, 1f, 0.85f), 0.88f));
                break;
        }
        return made;
    }

    static Material painted;
    static Material Painted()
    {
        if (painted) return painted;
        painted = TrainSim.Mat(Color.white);
        painted.SetFloat("_MVCOL", 1f); // RealToon: multiply by the vertex colours
        painted.SetInt("_Culling", 0);  // both faces: the facets need not all wind the same way
        return painted;
    }

    // A hill: rings out from the top, rough, falling away with power `fall`; coloured from foot to top, and above
    // `cap` (0..1 of the height; -1 for none) in the top colour outright (snow, grass, lava).
    static Mesh Mound(int seed, float wide, float high, float rough, float fall, Color foot, Color top, float cap)
    {
        var rng = new System.Random(seed);
        const int rings = 6, around = 14;
        var grid = new Vector3[rings + 1, around];
        for (int r = 0; r <= rings; r++)
            for (int a = 0; a < around; a++)
            {
                float u = r / (float)rings, angle = a * Mathf.PI * 2f / around;
                float radius = wide * 0.5f * u * (1f + ((float)rng.NextDouble() - 0.5f) * rough * 0.4f);
                float h = r == rings ? -0.3f : high * Mathf.Pow(1f - u, fall) * (1f + ((float)rng.NextDouble() - 0.5f) * rough * 0.5f);
                grid[r, a] = new Vector3(Mathf.Cos(angle) * radius, h, Mathf.Sin(angle) * radius * 0.75f);
            }
        return Facets(grid, rings, around, high, foot, top, cap);
    }

    // A mesa: steep sides in bands of rock up to a flat top (`sides` of them: 6 for a hexagon).
    static Mesh Mesa(int seed, float wide, float high, Color foot, Color top, int sides = 12)
    {
        var rng = new System.Random(seed);
        int around = sides;
        float[] heights = { high, high, high * 0.92f, high * 0.55f, high * 0.2f, -0.3f };
        float[] radii = { 0f, 0.55f, 0.62f, 0.72f, 0.88f, 1f };
        int rings = heights.Length - 1;
        var grid = new Vector3[rings + 1, around];
        for (int r = 0; r <= rings; r++)
            for (int a = 0; a < around; a++)
            {
                float angle = a * Mathf.PI * 2f / around + (sides == 6 ? 0f : ((float)rng.NextDouble() - 0.5f) * 0.2f);
                float radius = wide * 0.5f * radii[r] * (sides == 6 ? 1f : 1f + ((float)rng.NextDouble() - 0.5f) * 0.15f);
                grid[r, a] = new Vector3(Mathf.Cos(angle) * radius, heights[r], Mathf.Sin(angle) * radius);
            }
        return Facets(grid, rings, around, high, foot, top, 0.97f, true);
    }

    // Flat-shaded triangles between the rings (each its own vertices, so every facet has its own normal), coloured
    // by height: from foot to top, banded if `bands`, the top colour outright over `cap`.
    static Mesh Facets(Vector3[,] grid, int rings, int around, float high, Color foot, Color top, float cap, bool bands = false)
    {
        var verts = new List<Vector3>();
        var colors = new List<Color>();
        var tris = new List<int>();
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            float h = Mathf.Clamp01(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) / high);
            Color col = cap >= 0f && h >= cap ? top : Color.Lerp(foot, top, bands ? Mathf.Floor(h * 4f) / 4f * 0.6f : h * (cap >= 0f ? 0.5f : 1f));
            col *= 0.92f + 0.16f * Mathf.Repeat(a.x * 13.1f + c.z * 7.3f, 1f); // a little difference from facet to facet
            col.a = 1f;
            int v = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            colors.Add(col); colors.Add(col); colors.Add(col);
            tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
        }
        for (int r = 0; r < rings; r++)
            for (int a = 0; a < around; a++)
            {
                int b = (a + 1) % around;
                Vector3 p00 = grid[r, a], p01 = grid[r, b], p10 = grid[r + 1, a], p11 = grid[r + 1, b];
                if (r == 0) Tri(p00, p01, p11); // the top: a fan
                else { Tri(p00, p01, p11); Tri(p00, p11, p10); }
                if (r == 0) Tri(p00, p11, p10);
            }
        var mesh = new Mesh { name = "Relief" };
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

using System.Collections.Generic;
using UnityEngine;

public partial class GatesEvent
{
    Material cyanMat, pinkMat, limeMat;
    Transform gateLeft, gateRight;
    Renderer gateStatus;
    readonly List<Material> surfaces = new();
    readonly List<Mesh> sceneryMeshes = new();
    readonly List<(Transform body, Vector3 velocity, float until)> fragments = new();

    Material Surface(Color color, bool outline = true)
    {
        Material material = TrainSim.Mat(color, outline);
        surfaces.Add(material);
        return material;
    }

    void ClearArena()
    {
        if (arena) { arena.gameObject.SetActive(false); Destroy(arena.gameObject); }
        foreach (Material material in surfaces) if (material) Destroy(material);
        foreach (Mesh mesh in sceneryMeshes) if (mesh) Destroy(mesh);
        surfaces.Clear();
        sceneryMeshes.Clear();
        flashes.Clear();
        fragments.Clear();
    }

    // The same armour, reactor windows and colour bands as the final trench run.
    void DressApproach()
    {
        cyanMat = Surface(new Color(0.3f, 1.8f, 2.1f), false);
        pinkMat = Surface(new Color(1.8f, 0.35f, 1.2f), false);
        limeMat = Surface(new Color(0.5f, 2f, 0.35f), false);
        Material ground = Surface(Color.white, false);
        ground.mainTexture = Resources.Load<Texture2D>("Ground/fortress");
        ground.mainTextureScale = Vector2.one * 24f;
        float length = Mathf.Max(180f, gates * gateDistance + 100f);
        var corridor = new GameObject("FortressApproach").transform;
        corridor.SetParent(arena, false);
        Block(corridor, new Vector3(0f, -0.15f, length / 2f), new Vector3(160f, 0.2f, length + 100f), ground);
        Block(corridor, new Vector3(0f, -0.02f, length / 2f), new Vector3(4f, 0.1f, length), plateMat);
        for (float z = 0; z < length; z += 2.5f)
            Block(corridor, new Vector3(0f, 0.04f, z), new Vector3(2.7f, 0.1f, 0.32f), doorMat);

        var pylon = Resources.Load<GameObject>("Scenery/fortress/00_fortress_pylon");
        var radar = Resources.Load<GameObject>("Scenery/fortress/02_fortress_radar");
        for (int side = -1; side <= 1; side += 2)
        {
            Material accent = side < 0 ? cyanMat : pinkMat;
            Block(corridor, new Vector3(side * 0.75f, 0.12f, length / 2f), new Vector3(0.14f, 0.16f, length), steelMat);
            Block(corridor, new Vector3(side * 16f, 10.5f, length / 2f), new Vector3(2f, 21f, length), doorMat);
            Block(corridor, new Vector3(side * 13f, 0.45f, length / 2f), new Vector3(3.4f, 0.9f, length), plateMat);
            foreach (float y in new[] { 2.7f, 10f, 19.8f })
                Block(corridor, new Vector3(side * 14.94f, y, length / 2f), new Vector3(0.1f, 0.18f, length), y > 19f ? limeMat : accent);
            for (int k = 0; k * 24f < length; k++)
            {
                float z = k * 24f;
                Block(corridor, new Vector3(side * 14.65f, 10.5f, z), new Vector3(0.8f, 21f, 1.1f), plateMat);
                Block(corridor, new Vector3(side * 14.3f, 6.5f, z + 9f), new Vector3(0.7f, 5.6f, 15f), steelMat);
                Block(corridor, new Vector3(side * 13.9f, 6.5f, z + 9f), new Vector3(0.15f, 4.6f, 13.8f), doorMat);
                Block(corridor, new Vector3(side * 14.2f, 12f, z + 10f), new Vector3(0.45f, 0.45f, 22f), steelMat);
                for (int band = 0; band < 3; band++)
                    Block(corridor, new Vector3(side * 14.15f, 12f, z + 3f + band * 7f), new Vector3(0.65f, 0.65f, 0.35f), plateMat);
                if (k % 2 == 0)
                {
                    Block(corridor, new Vector3(side * 14.8f, 16f, z + 10f), new Vector3(0.3f, 4.2f, 9f), plateMat);
                    Block(corridor, new Vector3(side * 14.58f, 16f, z + 10f), new Vector3(0.1f, 3.4f, 8f), accent);
                    for (int bar = -1; bar <= 1; bar++)
                        Block(corridor, new Vector3(side * 14.4f, 16f, z + 10f + bar * 2.5f), new Vector3(0.3f, 3.8f, 0.3f), plateMat);
                }
                else
                {
                    for (int vent = 0; vent < 5; vent++)
                        Block(corridor, new Vector3(side * 14.8f, 15f + vent * 0.65f, z + 10f), new Vector3(0.4f, 0.25f, 7f), plateMat);
                }
                Block(corridor, new Vector3(side * 4f, 0.07f, z + 7f), new Vector3(0.22f, 0.08f, 8f), limeMat);
                if (pylon && k % 3 == 0) PlaceProp(pylon, new Vector3(side * 18f, 21f, z), corridor, 1.2f);
                if (radar && k % 7 == 2) PlaceProp(radar, new Vector3(side * 19f, 21f, z + 8f), corridor, 2f);
                if (side < 0 && k % 4 == 2)
                {
                    Block(corridor, new Vector3(0f, 23f, z), new Vector3(34f, 1.6f, 4f), plateMat);
                    Block(corridor, new Vector3(0f, 22.1f, z), new Vector3(28f, 0.18f, 0.4f), cyanMat);
                }
            }
        }
        // Batch only the static corridor; the gate and its targets stay independent.
        CombineScenery(corridor);
    }

    void PlaceProp(GameObject prefab, Vector3 at, Transform parent, float scale)
    {
        Transform prop = Instantiate(prefab, parent).transform;
        prop.localPosition = at;
        prop.localScale *= scale;
    }

    void DressGate()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Material accent = side < 0 ? cyanMat : pinkMat;
            Block(gate, new Vector3(side * 12f, 10f, 0.6f), new Vector3(4f, 20f, 4f), plateMat);
            Block(gate, new Vector3(side * 12f, 9f, -1.6f), new Vector3(3.2f, 16f, 0.65f), steelMat);
            Block(gate, new Vector3(side * 12f, 9f, -2f), new Vector3(2.5f, 14.8f, 0.3f), doorMat);
            Block(gate, new Vector3(side * 10.4f, 8.8f, -2f), new Vector3(0.2f, 16f, 0.18f), accent);
            for (int rib = 0; rib < 5; rib++)
                Block(gate, new Vector3(side * 12f, 2f + rib * 3.3f, -2.3f), new Vector3(3.3f, 0.45f, 0.55f), plateMat);
            Transform brace = Block(gate, new Vector3(side * 9.5f, 17.5f, -1.7f), new Vector3(4.5f, 1.1f, 2f), steelMat);
            brace.localRotation = Quaternion.Euler(0f, 0f, side * 40f);
            var leaf = new GameObject(side < 0 ? "LeftLeaf" : "RightLeaf").transform;
            leaf.SetParent(door, false);
            if (side < 0) gateLeft = leaf; else gateRight = leaf;
            Block(leaf, new Vector3(side * 5f, 8.5f, 0f), new Vector3(10f, 17f, 1.2f), doorMat);
            for (int row = 0; row < 4; row++)
            {
                Block(leaf, new Vector3(side * 5f, 2.1f + row * 4.2f, -0.64f), new Vector3(9.2f, 3.65f, 0.08f), plateMat);
                Block(leaf, new Vector3(side * 5f, 2.1f + row * 4.2f, -0.7f), new Vector3(8.8f, 3.3f, 0.08f), doorMat);
            }
            Block(leaf, new Vector3(side * 0.18f, 8.5f, -0.8f), new Vector3(0.2f, 17f, 0.2f), accent);
            for (int bolt = 0; bolt < 8; bolt++)
                Block(leaf, new Vector3(side * 9.4f, 1f + bolt * 2.1f, -0.8f), new Vector3(0.28f, 0.28f, 0.16f), steelMat);
        }
        Block(gate, new Vector3(0f, 19.4f, 0f), new Vector3(29f, 3.4f, 4f), plateMat);
        Block(gate, new Vector3(0f, 19.4f, -2.1f), new Vector3(19f, 1.9f, 0.35f), doorMat);
        gateStatus = Block(gate, new Vector3(0f, 19.4f, -2.35f), new Vector3(9f, 0.28f, 0.12f), ringMat).GetComponent<Renderer>();
        // Illuminated sector marks and warning chevrons are readable without new text.
        for (int i = 0; i <= gateIndex; i++)
            Block(gate, new Vector3(-1.2f * gateIndex + i * 2.4f, 20.05f, -2.35f), new Vector3(0.9f, 0.25f, 0.1f), cyanMat);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                Transform stripe = Block(gate, new Vector3(side * (6f + i * 1.25f), 19.4f, -2.3f), new Vector3(0.38f, 1.3f, 0.1f), lockMat);
                stripe.localRotation = Quaternion.Euler(0f, 0f, -side * 30f);
            }
    }

    void DressLock(Lock target)
    {
        target.halo = new GameObject("TargetBrackets").transform;
        target.halo.SetParent(target.body, false);
        foreach (int x in new[] { -1, 1 })
            foreach (int y in new[] { -1, 1 })
            {
                Block(target.halo, new Vector3(x * 1.5f, 0.4f + y * 1.7f, -0.16f), new Vector3(0.65f, 0.13f, 0.12f), ringMat);
                Block(target.halo, new Vector3(x * 1.76f, 0.4f + y * 1.45f, -0.16f), new Vector3(0.13f, 0.65f, 0.12f), ringMat);
            }
        target.charge = new Renderer[lockHits];
        for (int p = 0; p < lockHits; p++)
            target.charge[p] = Block(target.body, new Vector3((p - (lockHits - 1) / 2f) * 2.3f / Mathf.Max(1, lockHits), -1.05f, -0.2f),
                new Vector3(1.8f / Mathf.Max(1, lockHits), 0.18f, 0.12f), ringMat).GetComponent<Renderer>();
        for (int side = -1; side <= 1; side += 2)
        {
            Block(target.body, new Vector3(side * 0.82f, 0f, -0.34f), new Vector3(0.12f, 1.2f, 0.08f), steelMat);
            Block(target.body, new Vector3(side * 1.2f, 1.6f, 0.05f), new Vector3(0.18f, 0.18f, 0.1f), steelMat);
        }
    }

    void Scatter(Vector3 at, float size)
    {
        for (int i = 0; i < 5; i++)
        {
            Transform body = Block(arena, at - Arena, new Vector3(0.12f, 0.12f, 0.6f) * size, i % 2 == 0 ? burstMat : steelMat);
            Vector3 velocity = Random.onUnitSphere * size * 3f + Vector3.up * 4f;
            body.rotation = Quaternion.LookRotation(velocity);
            fragments.Add((body, velocity, Time.unscaledTime + Random.Range(0.35f, 0.75f)));
        }
    }

    void MoveFragments(float now, float dt)
    {
        for (int i = fragments.Count - 1; i >= 0; i--)
        {
            var bit = fragments[i];
            if (!bit.body || now >= bit.until)
            {
                if (bit.body) Destroy(bit.body.gameObject);
                fragments.RemoveAt(i);
                continue;
            }
            bit.velocity += Vector3.down * (12f * dt);
            bit.body.position += bit.velocity * dt;
            bit.body.Rotate(130f * dt, 220f * dt, 0f);
            fragments[i] = bit;
        }
    }

    void CombineScenery(Transform root)
    {
        var batches = new Dictionary<Material, List<CombineInstance>>();
        var originals = new List<GameObject>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            // Imported fortress props keep their existing mesh and material assignments.
            if (!renderer || !filter.sharedMesh || renderer.sharedMaterials.Length != 1 || !surfaces.Contains(renderer.sharedMaterial)) continue;
            Material material = renderer.sharedMaterial;
            if (!batches.TryGetValue(material, out var batch)) batches[material] = batch = new();
            batch.Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
            renderer.enabled = false;
            originals.Add(filter.gameObject);
        }
        foreach (var batch in batches)
        {
            var mesh = new Mesh { name = "FortressPanels", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.Value.ToArray(), true, true);
            sceneryMeshes.Add(mesh);
            var body = new GameObject("FortressPanels");
            body.transform.SetParent(root, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = batch.Key;
        }
        foreach (GameObject original in originals) Destroy(original);
    }
}

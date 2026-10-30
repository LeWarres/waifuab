using System.Collections.Generic;
using UnityEngine;

// The city race dressed as a grand prix: a start arch with its five lights over a chequered grid, grandstands full of
// a cheering crowd at the start and at the launcher, kerbs and a neon strip along the walls, the racers' posters on
// boards beside the road and on the gantries, chevron boards round the outside of the hairpins, a skyline of lit
// towers (setbacks, neon edges, antennas, giant posters, spinning holograms), flying traffic streaming overhead and
// fireworks going off ahead all race long.
public partial class RaceEvent
{
    readonly List<(Transform t, float y, float phase)> crowd = new();
    readonly List<(Transform t, float s, float off, float high, float speed)> traffic = new();
    readonly List<Renderer> startLights = new();
    readonly List<Transform> holograms = new();
    readonly Dictionary<Vector2Int, Material> facades = new();
    Material lightOff, lightRed, lightGreen;
    Material[] fireworkMats;
    float nextFirework;

    void DressCity()
    {
        float w = roadWidth / 2f;
        crowd.Clear();
        traffic.Clear();
        startLights.Clear();
        holograms.Clear();
        facades.Clear();
        Material cyan = Glow(new Color(0.3f, 2.4f, 2.8f)), pink = Glow(new Color(2.8f, 0.45f, 1.8f)), gold = Glow(new Color(2.8f, 1.9f, 0.4f)), lime = Glow(new Color(0.6f, 2.8f, 0.4f));
        Material[] neons = { cyan, pink, gold, lime, Glow(new Color(1.4f, 0.6f, 3f)) };
        Material steel = TrainSim.Mat(new Color(0.62f, 0.65f, 0.72f)), dark = TrainSim.Mat(new Color(0.08f, 0.08f, 0.1f)), white = TrainSim.Mat(new Color(0.95f, 0.95f, 0.97f));
        Material red = Glow(new Color(3f, 0.3f, 0.2f)), kerbRed = TrainSim.Mat(new Color(0.85f, 0.12f, 0.1f), false), kerbWhite = TrainSim.Mat(new Color(0.95f, 0.95f, 0.95f), false);
        var posters = new List<Material>();
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("Ultimate")) posters.Add(Poster(t));
        var ads = new List<Material>();
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("Menu")) if (t.name.StartsWith("loco_")) ads.Add(Poster(t));

        // The road's edges: red and white kerbs (the barriers carry their own neon: RaceEvent.Barriers).
        for (int s = -1; s <= 1; s += 2)
        {
            float a = s * w, b = s * (w - 1.4f);
            Ribbon("Kerb", Mathf.Min(a, b), Mathf.Max(a, b), 0.06f, kerbRed);
            Ribbon("KerbWhite", Mathf.Min(a, b), Mathf.Max(a, b), 0.065f, kerbWhite, 1);
        }

        // The grid: a chequered line, the arch over it with the five lights and the game's title.
        Vector3 fwd = dirs[0], side0 = Vector3.Cross(Vector3.up, fwd);
        Quaternion look = Quaternion.LookRotation(fwd);
        Vector3 line = Origin + fwd * 72f;
        for (int x = 0; x < 16; x++)
            for (int z = 0; z < 2; z++)
                Block(line + side0 * ((x + 0.5f) / 16f - 0.5f) * roadWidth + fwd * (z - 0.5f) * 2.2f + Vector3.up * 0.07f, look, new Vector3(roadWidth / 16f, 0.05f, 2.2f), (x + z) % 2 == 0 ? white : dark);
        Vector3 arch = Origin + fwd * 82f;
        Kit("arch", arch, look * Quaternion.Euler(0f, 180f, 0f), cyan); // its lights' side (the model's front) towards the grid
        lightOff = TrainSim.Mat(new Color(0.18f, 0.05f, 0.05f), false);
        lightRed = Glow(new Color(4f, 0.25f, 0.15f));
        lightGreen = Glow(new Color(0.3f, 4f, 0.6f));
        for (int k = 0; k < 5; k++) startLights.Add(Ball(arch + side0 * (k - 2f) * 4.2f + Vector3.up * 19f - fwd * 1.4f, 2.6f, lightOff).GetComponent<Renderer>());
        Texture2D title = Resources.Load<Texture2D>("Menu/title");
        if (title) Picture(arch + Vector3.up * 28f, look, 30f, 30f * title.height / title.width, Poster(title));

        // Grandstands: along the grid and along the run into the launcher.
        Stand(75f, 210f, fwd, cyan);
        Stand(total - 95f, 150f, dirs[^1], pink);

        // Boards by the road with the racers' locomotives, leaning to face whoever is coming; posters on the gantries.
        int board = 0;
        for (float s = 120f; s < total - 200f && ads.Count > 0; s += 38f, board++)
        {
            Vector3 c = PointAt(s, out Vector3 d), right = Vector3.Cross(Vector3.up, d);
            PointAt(s + 30f, out Vector3 later);
            if (Vector3.Angle(d, later) > 12f) continue; // not in the bends: chevrons go there
            int side = board % 2 == 0 ? -1 : 1;
            Vector3 at = c + right * side * (w + 4f) + Vector3.up * 4.2f;
            Quaternion face = Quaternion.LookRotation(right * side + d * 0.6f);
            for (int p = -1; p <= 1; p += 2) Block(at + face * new Vector3(p * 3.6f, -2.2f, 0.3f), face, new Vector3(0.4f, 4.4f, 0.4f), steel);
            Block(at + face * new Vector3(0f, 0f, 0.15f), face, new Vector3(8.6f, 5.4f, 0.2f), neons[board % neons.Length]);
            Picture(at, face, 8f, 5f, ads[board % ads.Count]);
        }
        int gantry = 0;
        for (float s = 150f; s < total - 220f && posters.Count > 0; s += 210f, gantry++)
        {
            Vector3 c = PointAt(s, out Vector3 d);
            Quaternion rot = Quaternion.LookRotation(d);
            Block(c + Vector3.up * 21.6f + d * 0.2f, rot, new Vector3(19f, 6.8f, 0.3f), gantry % 2 == 0 ? pink : cyan);
            Picture(c + Vector3.up * 21.6f, rot, 18f, 6f, posters[gantry % posters.Count]);
        }

        // Chevron boards round the outside of every hard bend, pointing the way.
        Material chevron = Glow(new Color(3f, 2.2f, 0.2f));
        for (int i = 2; i < points.Count - 6; i++)
        {
            float turn = Vector3.SignedAngle(dirs[i], dirs[i + 4], Vector3.up);
            if (Mathf.Abs(turn) < 28f) continue;
            float side = -Mathf.Sign(turn), tip = -side; // on the outside, pointing in
            Vector3 right = Vector3.Cross(Vector3.up, dirs[i]), at = points[i] + right * side * (w + 1.6f) + Vector3.up * 3.4f;
            Quaternion rot = Quaternion.LookRotation(dirs[i]);
            Block(at, rot, new Vector3(4.6f, 3.2f, 0.3f), dark);
            Block(at + Vector3.down * 2.5f, rot, new Vector3(0.4f, 2f, 0.4f), steel);
            for (int b = -1; b <= 1; b += 2)
                Block(at + Vector3.up * b * 0.55f - dirs[i] * 0.2f, rot * Quaternion.Euler(0f, 0f, b * tip * 45f), new Vector3(0.6f, 1.7f, 0.15f), chevron);
        }

        Skyline(neons, steel, red, posters);
        Blimps(posters);
        Traffic(neons);
        fireworkMats = new[] { Glow(new Color(4f, 0.6f, 0.3f)), Glow(new Color(0.4f, 3.6f, 4f)), Glow(new Color(4f, 3.2f, 0.6f)), Glow(new Color(3.6f, 0.6f, 3.4f)), Glow(new Color(0.7f, 4f, 0.8f)), Glow(new Color(3.4f, 3.4f, 4f)) };
        nextFirework = 0f;
    }

    // A grandstand along the road, both sides: four tiers of a crowd that jumps, under a roof lined with light.
    void Stand(float centre, float length, Vector3 d, Material trim)
    {
        Vector3 c = PointAt(centre, out _), right = Vector3.Cross(Vector3.up, d);
        Quaternion rot = Quaternion.LookRotation(d);
        Material seat = TrainSim.Mat(new Color(0.3f, 0.32f, 0.4f)), steel = TrainSim.Mat(new Color(0.55f, 0.58f, 0.64f));
        Color[] shirts = { new(0.9f, 0.2f, 0.2f), new(0.2f, 0.6f, 0.9f), new(0.95f, 0.8f, 0.2f), new(0.85f, 0.3f, 0.75f), new(0.3f, 0.8f, 0.3f), new(0.92f, 0.92f, 0.92f), new(0.5f, 0.3f, 0.9f) };
        var people = System.Array.ConvertAll(shirts, s => TrainSim.Mat(s));
        for (int side = -1; side <= 1; side += 2)
        {
            float x0 = roadWidth / 2f + 5f;
            // The models (Tools/make_race_kit.py) face the road along their +x, and run back along -z: turned round
            // on the right, where the road is to the left.
            Quaternion facing = side < 0 ? rot : rot * Quaternion.Euler(0f, 180f, 0f);
            for (int t = 0; t < 4; t++)
            {
                float top = 0.8f + t * 1.4f, x = x0 + t * 2.4f;
                Block(c + right * side * x + Vector3.up * top / 2f, rot, new Vector3(2.4f, top, length), seat);
                for (float z = -length / 2f; z < length / 2f - 1f; z += 10f) // a row of seats on each step, 10 m a piece
                    Kit("seats", c + right * side * (x - 1.2f) + d * (side < 0 ? z + 10f : z) + Vector3.up * top, facing, trim);
                for (float z = -length / 2f + 1f; z < length / 2f; z += Random.Range(1.4f, 2.2f))
                {
                    Vector3 at = c + right * side * (x - 0.5f + Random.Range(-0.2f, 0.2f)) + d * z + Vector3.up * top;
                    GameObject guy = Kit(Random.value < 0.5f ? "fan" : "fan2", at, facing * Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f), trim);
                    var look = guy.GetComponentInChildren<Renderer>();
                    var mats = look.sharedMaterials;
                    for (int k = 0; k < mats.Length; k++) if (mats[k].name == "Shirt") mats[k] = people[Random.Range(0, people.Length)];
                    look.sharedMaterials = mats;
                    look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    crowd.Add((guy.transform, at.y, Random.value * 10f));
                }
            }
            float back = x0 + 4 * 2.4f;
            for (float z = -length / 2f; z <= length / 2f; z += 20f) Block(c + right * side * back + d * z + Vector3.up * 6f, rot, new Vector3(0.6f, 12f, 0.6f), steel);
            Block(c + right * side * (x0 + 4.8f) + Vector3.up * 12f, rot * Quaternion.Euler(0f, 0f, side * 8f), new Vector3(11f, 0.5f, length), steel);
            Block(c + right * side * (x0 - 0.6f) + Vector3.up * 11.3f, rot, new Vector3(0.4f, 0.4f, length), trim);
        }
    }

    // The towers round the city: lit windows, a setback on top, neon edges and crowns, an antenna with its red light;
    // the nearer ones a giant poster facing the road, a few a hologram spinning over the roof.
    void Skyline(Material[] neons, Material steel, Material red, List<Material> posters)
    {
        Material windows = Facade(24f, 28f); // one tile of the window texture every 24 m, as the models' UVs are laid out
        int towers = Mathf.Min(700, Mathf.RoundToInt(240f * length / 2600f));
        for (int k = 0; k < towers; k++)
        {
            Vector3 c = PointAt(Random.Range(0f, total), out Vector3 d);
            float side = Random.value < 0.5f ? -1f : 1f, far = Random.Range(110f, 540f);
            Vector3 at = c + Vector3.Cross(Vector3.up, d) * side * far;
            float wide = Random.Range(16f, 42f), high = Mathf.Lerp(40f, 220f, Mathf.Pow(Random.value, 1.6f)), deep = wide * Random.Range(0.7f, 1.3f);
            bool clear = true;
            for (int q = 0; q < points.Count && clear; q += 3) if ((points[q] - at).sqrMagnitude < (roadWidth + wide) * (roadWidth + wide)) clear = false;
            if (!clear) continue;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
            Material neon = neons[Random.Range(0, neons.Length)];
            // One of the modelled towers (Tools/make_race_kit.py, about 100 m), stretched to this one's height and
            // girth, its windows the shared facade, its bands of light this one's neon.
            GameObject tower = Kit("tower" + Random.Range(1, 5), at, rot, neon);
            float girth = Mathf.Clamp(wide / 30f, 0.6f, 1.4f);
            tower.transform.localScale = new Vector3(girth, high / 100f, girth);
            Renderer look = tower.GetComponentInChildren<Renderer>();
            var mats = look.sharedMaterials;
            for (int q = 0; q < mats.Length; q++) if (mats[q].name == "Facade") mats[q] = windows;
            look.sharedMaterials = mats;
            float top = high * 1.05f;
            if (top > 110f)
            {
                Block(at + Vector3.up * (top + 9f), rot, new Vector3(0.7f, 18f, 0.7f), steel);
                Ball(at + Vector3.up * (top + 18.5f), 2.6f, red);
            }
            if (far < 320f && posters.Count > 0 && Random.value < 0.3f) // a giant poster facing the road
            {
                Vector3 away = (at - c).normalized;
                float pw = Mathf.Min(wide * 1.5f, 54f);
                Picture(at - away * (Mathf.Max(wide, deep) * 0.75f + 1f) + Vector3.up * high * Random.Range(0.45f, 0.7f), Quaternion.LookRotation(away), pw, pw / 3f, posters[Random.Range(0, posters.Count)]);
            }
            if (Random.value < 0.1f) // a hologram over the roof
            {
                var holo = new GameObject("Hologram").transform;
                holo.SetParent(course, false);
                holo.position = at + Vector3.up * (top + wide * 0.5f + 4f);
                float r = wide * 0.45f;
                for (int b = 0; b < 16; b++)
                {
                    float a = b * Mathf.PI / 8f;
                    Block(holo, holo.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), new Vector3(0.6f, 0.6f, r * 0.42f), neon);
                }
                Block(holo, holo.position, Quaternion.Euler(45f, 0f, 45f), Vector3.one * r * 0.7f, neon);
                holograms.Add(holo);
            }
        }
    }

    // A few blimps over the course, a poster down each side.
    void Blimps(List<Material> posters)
    {
        Material hull = TrainSim.Mat(new Color(0.85f, 0.86f, 0.9f));
        for (float s = 400f; s < total - 400f && posters.Count > 0; s += Random.Range(1300f, 1900f))
        {
            Vector3 c = PointAt(s, out Vector3 d), right = Vector3.Cross(Vector3.up, d);
            var blimp = new GameObject("Blimp").transform;
            blimp.SetParent(course, false);
            blimp.SetPositionAndRotation(c + right * Random.Range(-90f, 90f) + Vector3.up * Random.Range(75f, 110f), Quaternion.LookRotation(right));
            Transform body = Ball(blimp.position, 1f, hull);
            body.SetParent(blimp, true);
            body.localRotation = Quaternion.identity;
            body.localScale = new Vector3(20f, 20f, 64f);
            Material poster = posters[Random.Range(0, posters.Count)];
            for (int side = -1; side <= 1; side += 2)
                Picture(blimp.position + blimp.right * side * 10.4f, blimp.rotation * Quaternion.Euler(0f, -side * 90f, 0f), 36f, 12f, poster).SetParent(blimp, true);
            holograms.Add(blimp); // turns slowly, like the holograms
        }
    }

    // Lanes of flying cars over the city on both sides, some each way, kept round wherever ours is.
    void Traffic(Material[] neons)
    {
        Material shell = TrainSim.Mat(new Color(0.2f, 0.22f, 0.3f)), tail = Glow(new Color(4f, 0.3f, 0.2f)), head = Glow(new Color(3.5f, 3.5f, 3f));
        for (int lane = 0; lane < 8; lane++)
        {
            float off = (lane % 2 == 0 ? -1f : 1f) * Random.Range(60f, 240f), high = Random.Range(28f, 95f), speed = (lane % 4 < 2 ? 1f : -1f) * Random.Range(35f, 80f);
            Material trim = neons[lane % neons.Length];
            for (int k = 0; k < 9; k++)
            {
                var car = new GameObject("FlyingCar").transform;
                car.SetParent(course, false);
                Block(car, Vector3.zero, Quaternion.identity, new Vector3(4.4f, 1.6f, 9f), shell);
                Block(car, new Vector3(0f, -0.9f, 0f), Quaternion.identity, new Vector3(4.8f, 0.25f, 9.4f), trim);
                Block(car, new Vector3(0f, 0.2f, -4.6f), Quaternion.identity, new Vector3(3.8f, 0.6f, 0.3f), tail);
                Block(car, new Vector3(0f, 0.2f, 4.6f), Quaternion.identity, new Vector3(3.8f, 0.6f, 0.3f), head);
                traffic.Add((car, k * 160f + Random.Range(-40f, 40f) - 300f, off, high, speed * Random.Range(0.9f, 1.1f)));
            }
        }
    }

    Material Poster(Texture2D t)
    {
        Material m = TrainSim.Mat(new Color(1.35f, 1.35f, 1.35f), false);
        m.mainTexture = t;
        m.SetInt("_Culling", 0);
        return m;
    }

    // A flat picture, readable from in front (its back to where it faces).
    Transform Picture(Vector3 at, Quaternion turn, float wide, float high, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(course, false);
        go.transform.SetPositionAndRotation(at, turn);
        go.transform.localScale = new Vector3(wide, high, 1f);
        var look = go.GetComponent<Renderer>();
        look.sharedMaterial = m;
        look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    // The towers' walls, windows lit here and there; one material per size, so the windows keep theirs.
    Material Facade(float wide, float high)
    {
        var key = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(wide / 12f)), Mathf.Max(1, Mathf.RoundToInt(high / 14f)));
        if (facades.TryGetValue(key, out Material m)) return m;
        m = TrainSim.Mat(new Color(1.5f, 1.5f, 1.6f));
        m.mainTexture = FacadeTexture();
        m.mainTextureScale = new Vector2(key.x * 0.5f, key.y * 0.5f);
        facades[key] = m;
        return m;
    }

    static Texture2D facadeTex;
    static Texture2D FacadeTexture()
    {
        if (facadeTex) return facadeTex;
        const int size = 256, cell = 32, cells = size / cell; // eight by eight windows
        facadeTex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var rng = new System.Random(11);
        var glass = new Color32[cells * cells];
        for (int i = 0; i < glass.Length; i++)
        {
            double r = rng.NextDouble();
            glass[i] = r < 0.32 ? new Color32(255, 196, 120, 255) : r < 0.46 ? new Color32(150, 205, 255, 255) : new Color32(30, 34, 50, 255);
        }
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int lx = x % cell, ly = y % cell;
                bool window = lx >= 4 && lx < 28 && ly >= 7 && ly < 26;
                px[y * size + x] = window ? glass[(y / cell) * cells + x / cell] : new Color32(22, 24, 34, 255);
            }
        facadeTex.SetPixels32(px);
        facadeTex.Apply(true);
        return facadeTex;
    }

    // The show round the race: the start lights, the crowd jumping, the holograms turning, the traffic flying past,
    // and fireworks ahead (more of them once ours has won).
    void CityUpdate(float now, bool going, float dt)
    {
        int lit = briefing.gameObject.activeSelf ? 0 : 5 - Mathf.FloorToInt((startsAt - now) / 0.6f);
        for (int k = 0; k < startLights.Count; k++) if (startLights[k]) startLights[k].sharedMaterial = going ? lightGreen : k < lit ? lightRed : lightOff;
        foreach (var c in crowd)
        {
            if (!c.t) continue;
            Vector3 p = c.t.position;
            p.y = c.y + Mathf.Abs(Mathf.Sin(now * 6f + c.phase)) * 0.5f;
            c.t.position = p;
        }
        foreach (Transform h in holograms) if (h) h.Rotate(0f, (h.name == "Blimp" ? 2f : 40f) * dt, 0f, Space.World);
        float from = me.progress - 300f, to = me.progress + 1200f;
        for (int i = 0; i < traffic.Count; i++)
        {
            var c = traffic[i];
            if (!c.t) continue;
            c.s += c.speed * dt;
            if (c.s > to) c.s = from + Random.Range(0f, 60f);
            else if (c.s < from) c.s = to - Random.Range(0f, 60f);
            Vector3 at = PointAt(c.s, out Vector3 d);
            c.t.SetPositionAndRotation(at + Vector3.Cross(Vector3.up, d) * c.off + Vector3.up * (c.high + Mathf.Sin(now + i) * 1.5f), Quaternion.LookRotation(c.speed > 0f ? d : -d));
            traffic[i] = c;
        }
        if (now >= nextFirework)
        {
            nextFirework = now + (winner == me && winner != null ? Random.Range(0.12f, 0.3f) : Random.Range(0.35f, 0.9f));
            Vector3 c = PointAt(me.progress + Random.Range(120f, 520f), out Vector3 d);
            Firework(c + Vector3.Cross(Vector3.up, d) * (Random.value < 0.5f ? -1f : 1f) * Random.Range(30f, 220f) + Vector3.up * Random.Range(60f, 150f), now);
        }
    }

    void Firework(Vector3 at, float now)
    {
        Material m = fireworkMats[Random.Range(0, fireworkMats.Length)], m2 = fireworkMats[Random.Range(0, fireworkMats.Length)];
        float speed = Random.Range(24f, 34f);
        for (int k = 0; k < 40; k++)
            fireballs.Add((at, Random.onUnitSphere * speed * Random.Range(0.85f, 1f), now, now + Random.Range(1.4f, 2f), Random.Range(2.6f, 4f), k % 3 == 0 ? m2 : m));
    }
}

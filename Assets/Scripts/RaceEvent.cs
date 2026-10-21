using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A chapter's finale as a race instead of a duel (Story.Part.race): the radio says the launcher has power left for
// one more locomotive to be shot into space, so six of them (ours, the chapter's rival and four picked at random)
// race from A to B for it. Only the first one there is launched. Driven like a podracer, seen from behind: throttle
// (R2), a turbo (cross) that heats the engine until it stalls for a moment, the brake (square) that with the stick
// over throws it into a drift (a sharp turn, sliding, and a kick of speed coming out of a long one), and a shove to
// the side. The course has a few hairpins. A crash into a wreck, or a wall hit head on, blows it up: it is put back
// on the road crawling and blinking yellow for a moment, then races on. Outside, time stands still until it is
// over, like the boarding (BrawlEvent). The course is built far from the run.
public class RaceEvent : MonoBehaviour
{
    public float length = 2600f, roadWidth = 26f;
    public float topSpeed = 64f, boostSpeed = 98f, accel = 24f, brakeForce = 30f, drag = 0.22f;
    public float turnRate = 92f;                        // degrees a second, slow; less at speed, far more in a drift
    public float driftTurn = 2.1f, driftGrip = 1.6f, driftKick = 12f; // a drift: turn times this, slides (grip: how fast the way follows the nose), a kick out of a long one
    public float crashSpeed = 20f, respawnAfter = 0.9f, recoverSeconds = 2f, crawlSpeed = 14f; // into a wall faster than this: a crash
    public float heatPerSecond = 0.42f, coolPerSecond = 0.25f, overheatSeconds = 2.4f;
    public float shove = 10f, shoveCooldown = 1.4f;
    public int obstacles = 16;

    public bool Active { get; private set; }
    public Camera View => view;

    static readonly Vector3 Origin = new Vector3(8000f, 0f, 0f);
    const float Step = 20f, Radius = 2.2f, Hover = 1.1f;

    class Racer
    {
        public Transform body, glow;
        public int loco;
        public string name;
        public bool player;
        public Vector3 pos, push;
        public float yaw, way, speed, heat, overheatUntil, shoveReady, lane, skill, roll, finishedAt = -1f, progress, steer, boost;
        public float driftFor, crashedUntil = -1f, recoverUntil = -1f;
        public int seg;
        public Renderer[] parts;
    }

    TrainSim train;
    InputAction move;
    Camera view;
    Transform course;
    readonly List<Vector3> points = new();
    readonly List<Vector3> dirs = new();
    readonly List<(Vector3 at, float radius)> wrecks = new();
    readonly List<Racer> racers = new();
    Racer me, winner;
    System.Action<bool> done;
    float startsAt, endsAt, shake, total;
    Vector3 camVelocity;

    Canvas board;
    Text place, speedo, countdown, standings, hint, message;
    Slanted heatFill, track;
    readonly List<Slanted> dots = new();

    void Awake()
    {
        train = GetComponent<TrainSim>();
        move = InputSystem.actions.FindAction("Player/Move", true);
        view = new GameObject("RaceCamera").AddComponent<Camera>();
        view.fieldOfView = 62f;
        view.nearClipPlane = 0.3f;
        view.farClipPlane = 1800f;
        view.depth = 10f;
        var hd = view.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        hd.customRenderingSettings = true; // no motion blur: time outside stands still and HDRP's history stalls with it
        hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur] = true;
        hd.renderingPathCustomFrameSettings.SetEnabled(UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur, false);
        view.enabled = false;

        board = UiKit.NewCanvas("Race", 6);
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.62f);
        Slanted left = UiKit.Panel(board.transform, new Vector2(170f, -80f), new Vector2(300f, 110f), glass, glass, 0.12f);
        left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0f, 1f);
        place = UiKit.Label(left.transform, UiKit.Heading, 46, UiKit.Gold, TextAnchor.MiddleLeft, new Vector2(10f, 18f), new Vector2(260f, 56f), true);
        speedo = UiKit.Label(left.transform, UiKit.Body, 22, Color.white, TextAnchor.MiddleLeft, new Vector2(10f, -18f), new Vector2(260f, 30f), true);
        UiKit.Panel(left.transform, new Vector2(0f, -44f), new Vector2(240f, 10f), new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), 0f);
        heatFill = UiKit.Panel(left.transform, new Vector2(-120f, -44f), new Vector2(0f, 8f), UiKit.Rose, UiKit.Rose, 0f);
        heatFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        track = UiKit.Panel(board.transform, new Vector2(0f, -40f), new Vector2(620f, 12f), glass, glass, 0f);
        track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        Slanted right = UiKit.Panel(board.transform, new Vector2(-170f, -150f), new Vector2(300f, 220f), glass, glass, 0.12f);
        right.rectTransform.anchorMin = right.rectTransform.anchorMax = new Vector2(1f, 1f);
        standings = UiKit.Label(right.transform, UiKit.Body, 20, Color.white, TextAnchor.UpperLeft, Vector2.zero, new Vector2(270f, 200f));
        standings.supportRichText = true;
        countdown = UiKit.Label(board.transform, UiKit.Heading, 120, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 60f), new Vector2(600f, 160f), true);
        message = UiKit.Label(board.transform, UiKit.Heading, 38, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 160f), new Vector2(1200f, 60f), true);
        Slanted help = UiKit.Panel(board.transform, new Vector2(0f, 50f), new Vector2(1000f, 40f), glass, glass, 0.12f);
        help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint = UiKit.Label(help.transform, UiKit.Body, 19, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(980f, 34f), true);
        board.gameObject.SetActive(false);
    }

    // ---------------- the course ----------------

    void Build()
    {
        if (course) Destroy(course.gameObject);
        course = new GameObject("RaceCourse").transform;
        points.Clear();
        dirs.Clear();
        wrecks.Clear();
        // The centre line: straight off the grid and into the launcher, bending in between, smoothly and both ways.
        int n = Mathf.CeilToInt(length / Step);
        float heading = 0f, seed = Random.value * 100f;
        // A few hairpins, podracer style: a long hard turn one way, the next the other way, so the course runs on.
        var hairpins = new List<(int at, int steps, float per)>();
        float sign = Random.value < 0.5f ? 1f : -1f;
        for (int h = 0; h < 4; h++)
        {
            int steps = Random.Range(6, 10);
            hairpins.Add((Mathf.RoundToInt(Mathf.Lerp(18f, n - 26f, (h + 0.5f) / 4f)), steps, sign * Random.Range(120f, 165f) / steps));
            sign = -sign;
        }
        Vector3 p = Origin;
        for (int i = 0; i <= n; i++)
        {
            points.Add(p);
            float bend = i > 10 && i < n - 14 ? (Mathf.PerlinNoise(seed, i * 0.045f) * 2f - 1f) * 5f : 0f;
            foreach (var h in hairpins) if (i >= h.at && i < h.at + h.steps) bend = h.per;
            heading += bend;
            Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            dirs.Add(dir);
            p += dir * Step;
        }
        total = n * Step;

        // The road: a ribbon along the line, with glowing edges; the city's ground painting all round.
        Material asphalt = TrainSim.Mat(new Color(0.24f, 0.25f, 0.29f), false), edge = TrainSim.Mat(new Color(0.3f, 2.2f, 2.6f), false);
        Ribbon("Road", -roadWidth / 2f, roadWidth / 2f, 0.05f, asphalt);
        Ribbon("EdgeL", -roadWidth / 2f - 0.6f, -roadWidth / 2f, 0.08f, edge);
        Ribbon("EdgeR", roadWidth / 2f, roadWidth / 2f + 0.6f, 0.08f, edge);
        Ribbon("Centre", -0.25f, 0.25f, 0.07f, TrainSim.Mat(new Color(0.55f, 0.56f, 0.6f), false), 6);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(ground.GetComponent<Collider>());
        ground.transform.SetParent(course, false);
        ground.transform.SetPositionAndRotation(Origin + Vector3.forward * total * 0.3f + Vector3.down * 0.02f, Quaternion.Euler(90f, 0f, 0f));
        ground.transform.localScale = Vector3.one * 6000f;
        Material groundMat = TrainSim.Mat(Color.white, false);
        groundMat.mainTexture = Resources.Load<Texture2D>("Ground/city");
        groundMat.mainTextureScale = Vector2.one * (6000f / 54f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // The city along the way: its buildings on both sides, and its cars and barriers wrecked on the road.
        var tall = new List<GameObject>();
        var junk = new List<GameObject>();
        foreach (GameObject kind in Resources.LoadAll<GameObject>("Scenery/city"))
        {
            Bounds box = Bounds(kind);
            if (box.size.y > 4f) tall.Add(kind);
            else if (kind.name.Contains("car") || kind.name.Contains("Taxi") || kind.name.Contains("Police") || kind.name.Contains("barrier")) junk.Add(kind);
        }
        for (float s = 30f; s < total - 160f && tall.Count > 0; s += Random.Range(26f, 40f))
            for (int side = -1; side <= 1; side += 2)
            {
                if (Random.value < 0.25f) continue;
                Vector3 at = PointAt(s, out Vector3 dir) + Vector3.Cross(Vector3.up, dir) * side * (roadWidth / 2f + Random.Range(10f, 26f));
                Instantiate(tall[Random.Range(0, tall.Count)], at, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f), course);
            }
        for (int i = 0; i < obstacles && junk.Count > 0; i++)
        {
            float s = Mathf.Lerp(220f, total - 260f, (i + Random.value) / obstacles);
            Vector3 at = PointAt(s, out Vector3 dir) + Vector3.Cross(Vector3.up, dir) * Random.Range(-roadWidth * 0.38f, roadWidth * 0.38f);
            Instantiate(junk[Random.Range(0, junk.Count)], at, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f), course);
            wrecks.Add((at, 2.2f));
        }

        // The launcher at B: a ramp up a rail to a glowing ring, between two towers.
        Vector3 end = points[^1], fwd = dirs[^1];
        Quaternion look = Quaternion.LookRotation(fwd);
        Material steel = TrainSim.Mat(new Color(0.62f, 0.65f, 0.72f)), glowMat = TrainSim.Mat(new Color(2.6f, 1.6f, 0.4f), false);
        Block(end + fwd * 40f + Vector3.up * 12f, look * Quaternion.Euler(-22f, 0f, 0f), new Vector3(roadWidth * 0.6f, 1f, 70f), steel);
        Block(end + fwd * 40f + Vector3.up * 12.8f, look * Quaternion.Euler(-22f, 0f, 0f), new Vector3(1.2f, 0.4f, 70f), glowMat);
        for (int side = -1; side <= 1; side += 2)
            Block(end + fwd * 70f + Vector3.Cross(Vector3.up, fwd) * side * 16f + Vector3.up * 30f, look, new Vector3(6f, 60f, 6f), steel);
        var ring = new GameObject("LaunchRing").transform;
        ring.SetParent(course, false);
        ring.SetPositionAndRotation(end + fwd * 76f + Vector3.up * 30f, look);
        for (int k = 0; k < 16; k++)
        {
            float a = k * Mathf.PI / 8f;
            Block(ring.position + ring.rotation * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 11f, ring.rotation * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), new Vector3(1.4f, 4.6f, 1.4f), glowMat);
        }
        Block(end + Vector3.up * 0.06f, look, new Vector3(roadWidth, 0.05f, 2f), glowMat); // the finish line
        course.gameObject.SetActive(false);
    }

    void Ribbon(string name, float from, float to, float y, Material m, int dashEvery = 0)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (dashEvery > 0 && i % 2 == 1) continue;
            Vector3 r0 = Vector3.Cross(Vector3.up, dirs[i]), r1 = Vector3.Cross(Vector3.up, dirs[i + 1]);
            Vector3 a = points[i], b = dashEvery > 0 ? points[i] + dirs[i] * Step * 0.5f : points[i + 1];
            int v = verts.Count;
            verts.Add(a + r0 * from + Vector3.up * y); verts.Add(a + r0 * to + Vector3.up * y);
            verts.Add(b + r1 * from + Vector3.up * y); verts.Add(b + r1 * to + Vector3.up * y);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1));
            tris.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
        }
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        var go = new GameObject(name);
        go.transform.SetParent(course, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
    }

    void Block(Vector3 at, Quaternion turn, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(course, false);
        go.transform.SetPositionAndRotation(at, turn);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Bounds Bounds(GameObject kind)
    {
        Bounds box = default;
        bool any = false;
        foreach (MeshRenderer part in kind.GetComponentsInChildren<MeshRenderer>())
        {
            if (any) box.Encapsulate(part.bounds); else box = part.bounds;
            any = true;
        }
        return box;
    }

    // Where the line is s metres along, and its way there.
    Vector3 PointAt(float s, out Vector3 dir)
    {
        s = Mathf.Clamp(s, 0f, total);
        int i = Mathf.Min(points.Count - 2, (int)(s / Step));
        float t = s / Step - i;
        dir = Vector3.Slerp(dirs[i], dirs[i + 1], t).normalized;
        return Vector3.Lerp(points[i], points[i + 1], t);
    }

    // ---------------- start and end ----------------

    // done(true): ours was first to the launcher.
    public void Begin(System.Action<bool> done)
    {
        this.done = done;
        Build();
        foreach (Racer r in racers) if (r.body) Destroy(r.body.gameObject);
        racers.Clear();
        winner = null;
        // Ours, the chapter's rival, and four more of the locomotives there are, at random.
        var field = new List<int> { Locomotives.Selected };
        if (Story.Active && !field.Contains(Story.Current.rival)) field.Add(Story.Current.rival);
        var others = new List<int>();
        for (int i = 0; i < Locomotives.Models.Length; i++) if (!field.Contains(i)) others.Add(i);
        while (field.Count < 6 && others.Count > 0) { int k = Random.Range(0, others.Count); field.Add(others[k]); others.RemoveAt(k); }
        // On the grid, two abreast; ours in the middle of it.
        int[] slot = { 2, 0, 1, 3, 4, 5 };
        float hard = Mathf.Clamp01(train.Difficulty / 30f);
        for (int i = 0; i < field.Count; i++)
        {
            var r = new Racer { loco = field[i], player = i == 0, name = L10n.T(Locomotives.NameKeys[field[i]]) };
            r.skill = r.player ? 1f : Random.Range(0.86f, 0.95f) + hard * 0.06f + (i == 1 ? 0.03f : 0f); // the rival a little quicker
            r.lane = Random.Range(-0.25f, 0.25f) * roadWidth;
            int row = slot[i] / 2, col = slot[i] % 2;
            r.pos = Origin + Vector3.forward * (60f - row * 16f) + Vector3.right * (col == 0 ? -5.5f : 5.5f);
            r.progress = 60f - row * 16f;
            r.seg = Mathf.Max(0, (int)(r.progress / Step));
            r.body = new GameObject("Racer_" + r.loco).transform;
            r.body.SetParent(course, false);
            Transform model = Locomotives.Create(r.body, r.loco);
            model.localRotation = Quaternion.Euler(0f, 90f, 0f); // their noses are their -x
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(glow.GetComponent<Collider>());
            glow.GetComponent<Renderer>().sharedMaterial = TrainSim.Mat(new Color(0.6f, 2.4f, 3f), false);
            r.glow = glow.transform;
            r.glow.SetParent(r.body, false);
            r.glow.localPosition = new Vector3(0f, 1.2f, -4.4f);
            r.parts = model.GetComponentsInChildren<Renderer>();
            racers.Add(r);
        }
        me = racers[0];
        startsAt = Time.unscaledTime + 3.6f;
        endsAt = -1f;
        message.text = "";
        hint.text = L10n.T("race.hint");
        dots.Clear();
        foreach (Transform c in track.transform) Destroy(c.gameObject);
        foreach (Racer r in racers)
        {
            Color c = r.player ? UiKit.Gold : r == racers[1] ? UiKit.Rose : UiKit.Ice;
            Slanted dot = UiKit.Panel(track.transform, Vector2.zero, r.player ? new Vector2(16f, 24f) : new Vector2(10f, 18f), c, c, 0f);
            dots.Add(dot);
        }
        course.gameObject.SetActive(true);
        view.enabled = true;
        board.gameObject.SetActive(true);
        Active = true;
        Time.timeScale = 0f;
        foreach (Racer r in racers) Track(r, Time.unscaledTime);
        UpdateCamera(1f, true);
    }

    void End()
    {
        Active = false;
        view.enabled = false;
        board.gameObject.SetActive(false);
        if (course) Destroy(course.gameObject);
        racers.Clear();
        fireballs.Clear();
        Time.timeScale = 1f;
        effects.Clear();
        foreach (ParticleSystem system in unscaled) if (system) { var main = system.main; main.useUnscaledTime = false; } // the pool's copies, back to game time
        unscaled.Clear();
        System.Action<bool> finished = done;
        done = null;
        finished?.Invoke(winner == me);
    }

    // ---------------- the race ----------------

    void Update()
    {
        // Testing aid: N on the keyboard starts a race on the spot.
        if (!Active && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame && !train.Paused && !train.AtStation && !train.InEvent && !train.BossFight)
            Begin(won => train.Toast(L10n.T(won ? "race.won" : "race.lost.short")));
        if (!Active || train.Paused) return;
        Time.timeScale = 0f; // everything outside waits (a pause and back would have let it run)
        float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        bool going = now >= startsAt;
        countdown.text = now < startsAt ? Mathf.CeilToInt(startsAt - now).ToString() : now < startsAt + 1f ? L10n.T("race.go") : "";

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (!effects[i].graph || now > effects[i].until) { effects.RemoveAt(i); continue; }
            effects[i].graph.Simulate(dt);
        }
        Fireballs(now);
        foreach (Racer r in racers)
        {
            if (Crashed(r, now)) continue;
            float throttle = 0f, steer = 0f;
            bool brake = false, boost = false;
            int shoveWay = 0;
            if (going && r.finishedAt < 0f)
            {
                if (r.player) Controls(out throttle, out steer, out brake, out boost, out shoveWay);
                else Drive(r, now, out throttle, out steer, out brake, out boost, out shoveWay);
            }
            else if (r.finishedAt >= 0f) throttle = 1f; // over the line: on up the launcher
            Run(r, now, dt, throttle, steer, brake, boost, shoveWay);
        }
        Collide(dt);
        foreach (Racer r in racers) Track(r, now);

        // Positions: by how far along; whoever is over the line first is first.
        racers.Sort((a, b) => a.finishedAt >= 0f && b.finishedAt >= 0f ? a.finishedAt.CompareTo(b.finishedAt)
            : a.finishedAt >= 0f ? -1 : b.finishedAt >= 0f ? 1 : b.progress.CompareTo(a.progress));
        if (winner == null && racers[0].finishedAt >= 0f)
        {
            winner = racers[0];
            message.text = winner == me ? L10n.T("race.won") : L10n.T("race.lost", winner.name);
            endsAt = now + 4.5f;
        }
        Show(now);
        UpdateCamera(dt, false);
        if (endsAt > 0f && now >= endsAt) End();
    }

    void Controls(out float throttle, out float steer, out bool brake, out bool boost, out int shoveWay)
    {
        Vector2 stick = move.ReadValue<Vector2>();
        var pad = Gamepad.current;
        var keys = Keyboard.current;
        steer = Mathf.Clamp(stick.x, -1f, 1f);
        // R2 (or W) is the throttle, cross (or Shift) the turbo, square (or S, or L2) the brake and the drift.
        throttle = Mathf.Max(keys != null && (keys.wKey.isPressed || keys.upArrowKey.isPressed) ? 1f : 0f, pad != null ? pad.rightTrigger.ReadValue() : 0f);
        brake = (keys != null && (keys.sKey.isPressed || keys.downArrowKey.isPressed || keys.spaceKey.isPressed))
                || (pad != null && (pad.buttonWest.isPressed || pad.leftTrigger.ReadValue() > 0.4f));
        boost = (keys != null && keys.leftShiftKey.isPressed) || (pad != null && pad.buttonSouth.isPressed);
        shoveWay = 0;
        if ((keys != null && keys.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) shoveWay = -1;
        if ((keys != null && keys.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) shoveWay = 1;
        if (boost) throttle = 1f;
    }

    // A rival: follows the line on a lane of its own, slows for the bends ahead, boosts on the straights, goes
    // round the wrecks, shoves whoever is alongside; a little quicker when far behind ours, a little slower ahead.
    void Drive(Racer r, float now, out float throttle, out float steer, out bool brake, out bool boost, out int shoveWay)
    {
        Vector3 ahead = PointAt(r.progress + 18f + r.speed * 0.5f, out Vector3 dir);
        PointAt(r.progress, out Vector3 here);
        PointAt(r.progress + 90f, out Vector3 later);
        float bendAhead = Vector3.Angle(here, later);
        float lane = r.lane + Mathf.Sin(now * 0.35f + r.loco) * roadWidth * 0.12f;
        foreach (var w in wrecks)
        {
            Vector3 to = w.at - r.pos;
            float along = Vector3.Dot(to, here);
            if (along > 0f && along < 45f)
            {
                float side = Vector3.Dot(to, Vector3.Cross(Vector3.up, here));
                if (Mathf.Abs(side - lane) < 5f) lane = side + (side > 0f ? -6.5f : 6.5f); // round it
            }
        }
        Vector3 target = ahead + Vector3.Cross(Vector3.up, dir) * Mathf.Clamp(lane, -roadWidth * 0.4f, roadWidth * 0.4f);
        Vector3 way = target - r.pos;
        float want = Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg;
        steer = Mathf.Clamp(Mathf.DeltaAngle(r.yaw, want) / 20f, -1f, 1f);
        float gap = me.progress - r.progress;
        float skill = r.skill + Mathf.Clamp(gap / 500f, -0.07f, 0.07f);
        float targetSpeed = topSpeed * skill * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(bendAhead / 70f));
        throttle = r.speed < targetSpeed ? 1f : 0f;
        brake = r.speed > targetSpeed + 8f;
        boost = bendAhead < 10f && r.heat < 0.55f && now > r.overheatUntil && Mathf.PerlinNoise(r.loco * 3.1f, now * 0.4f) > 0.45f;
        shoveWay = 0;
        foreach (Racer o in racers)
        {
            if (o == r || now < r.shoveReady) continue;
            Vector3 d = o.pos - r.pos;
            if (d.magnitude < 7f && Mathf.Abs(Vector3.Dot(d, here)) < 4f && Random.value < 0.02f)
                shoveWay = Vector3.Dot(d, Vector3.Cross(Vector3.up, here)) > 0f ? 1 : -1;
        }
    }

    void Run(Racer r, float now, float dt, float throttle, float steer, bool brake, bool boost, int shoveWay)
    {
        bool stalled = now < r.overheatUntil;
        bool boosting = boost && !stalled && r.finishedAt < 0f;
        if (boosting)
        {
            r.heat += heatPerSecond * dt;
            if (r.heat >= 1f) { r.heat = 1f; r.overheatUntil = now + overheatSeconds; if (r.player) shake = 0.3f; }
        }
        else r.heat = Mathf.Max(0f, r.heat - coolPerSecond * dt);
        bool crawling = now < r.recoverUntil;
        float max = crawling ? crawlSpeed : stalled ? topSpeed * 0.45f : boosting ? boostSpeed : topSpeed;
        if (r.finishedAt >= 0f) max = boostSpeed;
        // The brake with the stick over and some speed on: a drift. It turns far sharper and slides, losing little;
        // held long enough, it gives a kick of speed on the way out.
        bool drifting = brake && Mathf.Abs(steer) > 0.3f && r.speed > 25f && !crawling;
        if (drifting) r.driftFor += dt;
        else
        {
            if (r.driftFor > 0.8f) r.speed += driftKick;
            r.driftFor = 0f;
        }
        if (throttle > 0f) r.speed += accel * throttle * (boosting ? 1.8f : 1f) * dt;
        r.speed -= r.speed * drag * dt * (throttle > 0f ? 0.2f : 1f);
        if (brake) r.speed -= brakeForce * (drifting ? 0.25f : 1f) * dt;
        if (r.speed > max) r.speed = Mathf.MoveTowards(r.speed, max, (crawling ? 80f : 30f) * dt);
        r.speed = Mathf.Max(0f, r.speed);
        float turn = turnRate * Mathf.Lerp(1f, 0.5f, r.speed / boostSpeed) * (drifting ? driftTurn : brake ? 1.4f : 1f);
        r.steer = Mathf.MoveTowards(r.steer, steer, 5f * dt);
        r.yaw += r.steer * turn * dt;
        r.way = Mathf.MoveTowardsAngle(r.way, r.yaw, Mathf.Abs(Mathf.DeltaAngle(r.way, r.yaw)) * (drifting ? driftGrip : 9f) * dt + 1f);
        if (shoveWay != 0 && now >= r.shoveReady)
        {
            r.shoveReady = now + shoveCooldown;
            r.push += Quaternion.Euler(0f, r.yaw, 0f) * Vector3.right * shoveWay * shove;
            // whoever is on that side gets knocked the same way, harder
            foreach (Racer o in racers)
            {
                if (o == r) continue;
                Vector3 d = o.pos - r.pos;
                if (d.magnitude < 7.5f && Vector3.Dot(d, Quaternion.Euler(0f, r.yaw, 0f) * Vector3.right) * shoveWay > 0f)
                {
                    o.push += d.normalized * shove * 1.8f;
                    o.speed *= 0.85f;
                    if (o.player) shake = 0.35f;
                }
            }
        }
        r.pos += Quaternion.Euler(0f, r.way, 0f) * Vector3.forward * r.speed * dt + r.push * dt; // along its way: in a drift, not where its nose is
        r.push = Vector3.MoveTowards(r.push, Vector3.zero, 22f * dt);
        r.boost = Mathf.MoveTowards(r.boost, boosting ? 1f : throttle > 0f ? 0.45f : 0.15f, 4f * dt);
        r.roll = Mathf.Lerp(r.roll, -r.steer * (drifting ? 34f : 22f), 6f * dt);
    }

    // Where each one is on the course: its progress, the walls, the wrecks, the line.
    void Track(Racer r, float now)
    {
        float best = float.MaxValue;
        int at = r.seg;
        for (int i = Mathf.Max(0, r.seg - 3); i < Mathf.Min(points.Count - 1, r.seg + 8); i++)
        {
            float d = (points[i] - r.pos).sqrMagnitude;
            if (d < best) { best = d; at = i; }
        }
        r.seg = at;
        Vector3 dir = dirs[at], right = Vector3.Cross(Vector3.up, dir);
        float along = Mathf.Clamp(Vector3.Dot(r.pos - points[at], dir), -Step, Step);
        r.progress = r.finishedAt >= 0f ? total + Vector3.Dot(r.pos - points[^1], dirs[^1]) : at * Step + along;
        if (r.finishedAt < 0f)
        {
            float side = Vector3.Dot(r.pos - points[at], right), limit = roadWidth / 2f - Radius;
            if (Mathf.Abs(side) > limit)
            {
                // A wall hit head on is a crash; a glancing one only scrapes: back on the road, slowed.
                float into = Vector3.Dot(Quaternion.Euler(0f, r.way, 0f) * Vector3.forward * r.speed, right * Mathf.Sign(side));
                if (into > crashSpeed && now >= r.recoverUntil) { Crash(r, now); return; }
                r.pos -= right * (side - Mathf.Sign(side) * limit);
                r.speed *= 0.97f;
                r.yaw = Mathf.MoveTowardsAngle(r.yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 40f * Time.unscaledDeltaTime);
                if (r.player) shake = Mathf.Max(shake, 0.12f);
            }
            foreach (var w in wrecks)
            {
                Vector3 d = r.pos - w.at;
                d.y = 0f;
                if (d.magnitude < w.radius + Radius)
                {
                    r.pos = w.at + d.normalized * (w.radius + Radius);
                    if (r.speed > crashSpeed && now >= r.recoverUntil) { Crash(r, now); return; }
                    r.speed *= 0.4f;
                    if (r.player) shake = 0.45f;
                }
            }
            if (r.progress >= total) r.finishedAt = now;
        }
        // On the ground (hovering, bobbing, leaning into the turn), or once over the line up the launcher's ramp.
        float lift = Hover + Mathf.Sin(now * 3f + r.loco) * 0.12f, pitch = 0f;
        if (r.finishedAt >= 0f)
        {
            float past = Mathf.Max(0f, r.progress - total);
            lift += Mathf.Min(past, 70f) * Mathf.Tan(22f * Mathf.Deg2Rad) + Mathf.Max(0f, past - 70f) * 1.2f;
            pitch = -22f;
        }
        r.body.SetPositionAndRotation(new Vector3(r.pos.x, lift, r.pos.z), Quaternion.Euler(pitch, r.yaw, r.roll));
        Blink(r, now < r.recoverUntil && (int)(now * 8f) % 2 == 0);
        r.glow.localScale = new Vector3(0.45f, 0.45f, 0.4f + r.boost * 2.4f) * (now < r.overheatUntil ? 0.3f : 1f); // the thruster's flame, long on a boost
    }

    // Bumping: two that touch push apart, the one behind loses a little.
    void Collide(float dt)
    {
        for (int i = 0; i < racers.Count; i++)
            for (int j = i + 1; j < racers.Count; j++)
            {
                Racer a = racers[i], b = racers[j];
                Vector3 d = b.pos - a.pos;
                d.y = 0f;
                float dist = d.magnitude, min = Radius * 2f;
                if (dist >= min || dist < 0.001f) continue;
                Vector3 n = d / dist;
                a.pos -= n * (min - dist) * 0.5f;
                b.pos += n * (min - dist) * 0.5f;
                Racer behind = a.progress < b.progress ? a : b;
                behind.speed *= 1f - 0.8f * dt;
                if (a.player || b.player) shake = Mathf.Max(shake, 0.15f);
            }
    }

    void Show(float now)
    {
        int mine = racers.IndexOf(me);
        place.text = L10n.T("race.position", mine + 1, racers.Count);
        speedo.text = now < me.overheatUntil ? L10n.T("race.overheat") : L10n.T("race.speed", Mathf.RoundToInt(me.speed * 4f));
        speedo.color = now < me.overheatUntil ? UiKit.Rose : Color.white;
        heatFill.rectTransform.sizeDelta = new Vector2(240f * me.heat, 8f);
        heatFill.color = me.heat > 0.75f ? UiKit.Rose : UiKit.Gold;
        var lines = new System.Text.StringBuilder();
        for (int i = 0; i < racers.Count; i++)
        {
            Racer r = racers[i];
            string line = $"{i + 1}. {r.name}";
            lines.AppendLine(r.player ? $"<color=#FFCC1A>{line}</color>" : line);
        }
        standings.text = lines.ToString();
        int k = 0;
        foreach (Racer r in BySlot())
        {
            float t = Mathf.Clamp01(r.progress / total);
            dots[k].rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-305f, 305f, t), 0f);
            k++;
        }
    }

    IEnumerable<Racer> BySlot()
    {
        // the dots keep their order of the grid (ours first), whatever the positions
        var list = new List<Racer>(racers);
        list.Sort((a, b) => a.player ? -1 : b.player ? 1 : a.loco.CompareTo(b.loco));
        return list;
    }

    void UpdateCamera(float dt, bool snap)
    {
        if (me == null) return;
        Vector3 fwd = Quaternion.Euler(0f, Mathf.LerpAngle(me.way, me.yaw, 0.5f), 0f) * Vector3.forward;
        Vector3 body = me.body.position;
        Vector3 want = body - fwd * 13f + Vector3.up * 5.2f;
        view.transform.position = snap ? want : Vector3.SmoothDamp(view.transform.position, want, ref camVelocity, 0.12f, Mathf.Infinity, dt);
        view.transform.LookAt(body + fwd * 12f + Vector3.up * 1.5f);
        view.fieldOfView = Mathf.Lerp(view.fieldOfView, Mathf.Lerp(60f, 84f, me.speed / boostSpeed), 4f * dt);
        if (shake > 0f)
        {
            view.transform.position += Random.insideUnitSphere * shake * 0.6f;
            shake = Mathf.Max(0f, shake - dt);
        }
    }

    // ---------------- crashes ----------------

    // A crash: it blows up, and a moment later is put back on the road where it was, crawling and blinking.
    void Crash(Racer r, float now)
    {
        Effect("grenade", r.body.position + Vector3.up, 6f);
        Effect("smoke", r.body.position + Vector3.up, 4f);
        Fireball(r.body.position + Vector3.up * 1.5f, now);
        Sound.Play("vagonroto");
        r.crashedUntil = now + respawnAfter;
        r.recoverUntil = r.crashedUntil + recoverSeconds;
        r.speed = 0f;
        r.push = Vector3.zero;
        r.heat = 0f;
        r.driftFor = 0f;
        r.body.gameObject.SetActive(false);
        if (r.player) shake = 0.8f;
    }

    // Still blown up: nothing to drive yet. When the moment is over, back on the line, facing along it.
    bool Crashed(Racer r, float now)
    {
        if (r.crashedUntil < 0f) return false;
        if (now < r.crashedUntil) return true;
        r.crashedUntil = -1f;
        Vector3 at = PointAt(r.progress, out Vector3 dir);
        r.pos = new Vector3(at.x, 0f, at.z) + Vector3.Cross(Vector3.up, dir) * Mathf.Clamp(r.lane * 0.3f, -4f, 4f);
        r.yaw = r.way = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        r.speed = crawlSpeed * 0.5f;
        r.body.gameObject.SetActive(true);
        return false;
    }

    // Blinking yellow while it gets going again (a property block: the shared materials stay as they are).
    static MaterialPropertyBlock yellow;
    static readonly int MainColor = Shader.PropertyToID("_MainColor");
    void Blink(Racer r, bool on)
    {
        if (yellow == null) { yellow = new MaterialPropertyBlock(); yellow.SetColor(MainColor, new Color(2.2f, 1.8f, 0.2f)); }
        foreach (Renderer part in r.parts) if (part) part.SetPropertyBlock(on ? yellow : null);
    }

    // The blast itself, drawn here so it shows whatever the effects do on real time: a burst of balls of fire
    // flying out and burning down, and puffs of smoke rising after them.
    readonly List<(Transform ball, Vector3 velocity, float from, float until, float size)> fireballs = new();
    Material[] fireMats;
    void Fireball(Vector3 at, float now)
    {
        fireMats ??= new[] { TrainSim.Mat(new Color(3.2f, 1.4f, 0.2f), false), TrainSim.Mat(new Color(3f, 2.6f, 0.6f), false), TrainSim.Mat(new Color(0.3f, 0.3f, 0.33f), false) };
        for (int k = 0; k < 22; k++)
        {
            bool smoke = k >= 14;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ball.GetComponent<Collider>());
            var look = ball.GetComponent<Renderer>();
            look.sharedMaterial = fireMats[smoke ? 2 : k % 2];
            look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ball.transform.SetParent(course, false);
            ball.transform.position = at;
            Vector3 v = Random.onUnitSphere;
            v.y = Mathf.Abs(v.y);
            float from = now + (smoke ? 0.4f : 0f);
            fireballs.Add((ball.transform, smoke ? v * 2f + Vector3.up * 4f : v * Random.Range(7f, 14f), from, from + (smoke ? Random.Range(1.2f, 1.8f) : Random.Range(0.8f, 1.3f)), smoke ? Random.Range(1.6f, 2.6f) : Random.Range(1f, 2.4f)));
        }
    }
    void Fireballs(float now)
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            var f = fireballs[i];
            if (!f.ball || now >= f.until) { if (f.ball) Destroy(f.ball.gameObject); fireballs.RemoveAt(i); continue; }
            float t = Mathf.Clamp01((now - f.from) / (f.until - f.from));
            f.ball.localScale = Vector3.one * (now < f.from ? 0f : f.size * (1f - t) * (0.6f + t));
            f.ball.position += f.velocity * dt;
            fireballs[i] = (f.ball, f.velocity * (1f - 2.5f * dt), f.from, f.until, f.size); // slowing as it burns
        }
    }

    // An effect out here: time stands still outside, so it is played by hand, on real time (as in BrawlEvent).
    readonly List<(UnityEngine.VFX.VisualEffect graph, float until)> effects = new();
    readonly HashSet<ParticleSystem> unscaled = new();
    void Effect(string name, Vector3 at, float size)
    {
        GameObject copy = Combat.Instance ? Combat.Instance.Burst(name, at, size) : null;
        if (!copy) return;
        foreach (var graph in copy.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>()) effects.Add((graph, Time.unscaledTime + 3f));
        foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>())
        {
            var main = system.main;
            main.useUnscaledTime = true;
            unscaled.Add(system);
        }
    }

    void OnDestroy()
    {
        if (course) Destroy(course.gameObject);
    }
}

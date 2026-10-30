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
public partial class RaceEvent : MonoBehaviour // the trench run, its other mode: RaceTrench.cs
{
    public float length = 12500f, roadWidth = 44f; // about three minutes of racing
    public float topSpeed = 64f, boostSpeed = 98f, accel = 24f, brakeForce = 30f, drag = 0.22f;
    public float turnRate = 92f;                        // degrees a second, slow; less at speed, far more in a drift
    public float driftTurn = 2.1f, driftGrip = 1.6f, driftKick = 12f; // a drift: turn times this, slides (grip: how fast the way follows the nose), a kick out of a long one
    public float crashSpeed = 20f, respawnAfter = 0.9f, recoverSeconds = 2f, crawlSpeed = 14f; // into a wall faster than this: a crash
    public float heatPerSecond = 0.15f, coolPerSecond = 0.2f; // nearly seven seconds of turbo from cold; to the top and the engine blows; let go and it has to cool right down before the next
    public float shove = 10f, shoveCooldown = 1.4f;
    public int obstacles = 16; // every 2600 m of road

    public bool Active { get; private set; }
    public bool Showing => view && view.enabled;
    [System.NonSerialized] public bool autoDrive; // testing aid: ours driven like a rival (with no catching up), to measure the rivals against
    public Camera View => view;

    static readonly Vector3 Origin = new Vector3(8000f, 0f, 0f);
    const float Step = 20f, Radius = 2.2f, Hover = 1.1f, MaxHeading = 80f;

    class Racer
    {
        public Transform body, glow;
        public int loco;
        public string name;
        public bool player;
        public Vector3 pos, push;
        public float yaw, way, speed, heat, overheatUntil, shoveReady, lane, skill, roll, finishedAt = -1f, progress, steer, boost;
        public float driftFor, crashedUntil = -1f, recoverUntil = -1f, rampS, pace = 1f;
        public bool turboUsed, turboLocked; // let go of a turbo: none again until the heat is all gone
        public int crashes;
        public int seg;
        public Renderer[] parts;
    }

    TrainSim train;
    RaceAudio raceSound;
    bool playerBraking;
    InputAction move;
    Camera view;
    Transform course;
    readonly List<Vector3> points = new();
    readonly List<Vector3> dirs = new();
    readonly List<(Vector3 at, float radius)> wrecks = new();
    readonly List<(Vector3 at, float pitch)> ramp = new(); // the launcher's rail, from the finish line up
    const int RampSteps = 40;
    const float RampStep = 5f;
    Vector3 launchDir;
    bool launched;
    bool LaunchDenied => winner != null && Story.Active && Story.Hero == "xiuhtecuhtli" && winner == me && Story.Chapter == 1;
    Racer LaunchActor => LaunchDenied ? racers.Find(r => r.loco == 0) : winner;
    float sparkIn;
    GameObject sky;
    UnityEngine.Rendering.Volume haze;
    readonly List<Transform> streaks = new(); // speed lines round the camera
    readonly List<Racer> racers = new();
    Racer me, winner;
    System.Action<bool> done;
    float startsAt, endsAt, shake, total;
    Vector3 camVelocity;

    Canvas board;
    Text place, placeOf, speedo, countdown, standings, hint, message, turboLabel;
    Slanted track, trackFill, briefing, band;
    RectTransform markers;
    const int HeatCells = 14;
    readonly List<Slanted> heatCells = new();
    readonly List<(Slanted strip, Text rank, Text name)> rows = new();
    readonly List<(Slanted tag, Text rank)> dots = new();

    void Awake()
    {
        train = GetComponent<TrainSim>();
        raceSound = GetComponent<RaceAudio>() ?? gameObject.AddComponent<RaceAudio>();
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

        // The sky: the city's painting on a shell round the camera (as from the gunner's seat, SwarmEvent), and a warm
        // haze low over the ground so the far city fades into it.
        sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sky.GetComponent<Collider>());
        sky.transform.localScale = Vector3.one * 3000f;
        Mesh round = Instantiate(sky.GetComponent<MeshFilter>().sharedMesh);
        round.normals = System.Array.ConvertAll(round.normals, _ => Vector3.up);
        sky.GetComponent<MeshFilter>().sharedMesh = round;
        Material skyPaint = TrainSim.Mat(new Color(1.05f, 1.05f, 1.05f), false);
        skyPaint.SetInt("_Culling", 0);
        skyPaint.mainTexture = Resources.Load<Texture2D>("Sky/city");
        skyPaint.mainTextureScale = new Vector2(2f, 2f);
        skyPaint.mainTextureOffset = new Vector2(0f, -1f);
        var shell = sky.GetComponent<Renderer>();
        shell.sharedMaterial = skyPaint;
        shell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shell.receiveShadows = false;
        sky.SetActive(false);
        haze = new GameObject("RaceHaze").AddComponent<UnityEngine.Rendering.Volume>();
        haze.isGlobal = true;
        haze.priority = 50f;
        var fog = haze.profile.Add<UnityEngine.Rendering.HighDefinition.Fog>(true);
        fog.enabled.value = true;
        fog.meanFreePath.value = 520f;
        fog.baseHeight.value = 0f;
        fog.maximumHeight.value = 70f;
        fog.colorMode.value = UnityEngine.Rendering.HighDefinition.FogColorMode.ConstantColor;
        fog.color.value = new Color(1f, 0.74f, 0.58f);
        haze.enabled = false;
        Material streak = TrainSim.Mat(new Color(1.6f, 1.7f, 1.9f), false);
        for (int k = 0; k < 36; k++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(line.GetComponent<Collider>());
            line.GetComponent<Renderer>().sharedMaterial = streak;
            line.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.SetActive(false);
            streaks.Add(line.transform);
        }

        // The HUD, Persona 3 style (the race's blues, UiKit "race"): hard slanted plates, big leaning numbers with a hard
        // shadow, the standings a staircase of strips with ours lit white, a band sweeping across for the count and the end.
        board = UiKit.NewCanvas("Race", 6);
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.62f);
        // Top left: the place, huge, and out of how many.
        RectTransform left = UiKit.Group(board.transform, new Vector2(0f, 1f), new Vector2(40f, -20f));
        UiKit.Paint(UiKit.Panel(left, new Vector2(150f, -92f), new Vector2(300f, 34f), Color.white, Color.white, 0.5f), UiKit.Ink.Main).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
        place = Leaning(left, 170, new Vector2(80f, -70f), new Vector2(200f, 170f), TextAnchor.MiddleCenter);
        placeOf = Leaning(left, 48, new Vector2(220f, -92f), new Vector2(160f, 60f), TextAnchor.MiddleLeft);
        // Bottom right: the speed, big, its unit, and under it the turbo's heat.
        Slanted gauge = UiKit.Persona(UiKit.Panel(board.transform, new Vector2(-230f, 110f), new Vector2(380f, 150f), glass, glass, 0.14f), 4f, 10f);
        gauge.rectTransform.anchorMin = gauge.rectTransform.anchorMax = new Vector2(1f, 0f);
        speedo = Leaning(gauge.transform, 84, new Vector2(-30f, 26f), new Vector2(260f, 96f), TextAnchor.MiddleRight);
        UiKit.Paint(UiKit.Label(gauge.transform, UiKit.Display, 26, Color.white, TextAnchor.MiddleLeft, new Vector2(150f, 10f), new Vector2(80f, 34f), true), UiKit.Ink.Hot).text = "KM/H";
        turboLabel = UiKit.Label(gauge.transform, UiKit.Display, 20, UiKit.Ice, TextAnchor.MiddleLeft, new Vector2(-30f, -34f), new Vector2(300f, 26f), true);
        // The turbo's heat: a row of slanted cells that light up blue, then white, then red as it climbs.
        for (int i = 0; i < HeatCells; i++)
            heatCells.Add(UiKit.Panel(gauge.transform, new Vector2(-140f + i * 21.5f, -58f), new Vector2(17f, 20f), Color.white, Color.white, 0.5f));
        // The course, grid to launcher: ours filled in so far, a tick every tenth, the finish chequered, and over it
        // a tag a racer with their place in it (ours white, bigger, pointing down at the strip).
        track = UiKit.Persona(UiKit.Panel(board.transform, new Vector2(0f, -58f), new Vector2(700f, 24f), glass, glass, 0.4f), 3f, 6f);
        track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        trackFill = UiKit.Paint(UiKit.Panel(track.transform, new Vector2(-340f, 0f), new Vector2(0f, 12f), Color.white, Color.white, 0.4f), UiKit.Ink.Main);
        trackFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        for (int q = 1; q < 10; q++)
            UiKit.Panel(track.transform, new Vector2(-340f + q * 68f, 0f), new Vector2(3f, 16f), new Color(1f, 1f, 1f, 0.35f), new Color(1f, 1f, 1f, 0.35f), 0f);
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 2; y++)
            {
                Color sq = (x + y) % 2 == 0 ? Color.white : Color.black;
                UiKit.Panel(track.transform, new Vector2(334f + x * 7f, (y - 0.5f) * 7f), new Vector2(7f, 7f), sq, sq, 0f).sprite = null;
            }
        markers = UiKit.Group(track.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        // Top right: the standings, one strip a racer, each a little further in than the one above.
        RectTransform right = UiKit.Group(board.transform, new Vector2(1f, 1f), new Vector2(-30f, -110f));
        standings = UiKit.Label(right, UiKit.Display, 24, Color.white, TextAnchor.MiddleRight, Vector2.zero, Vector2.zero); // holds the group (hidden in the trench)
        for (int i = 0; i < 6; i++)
        {
            Slanted strip = UiKit.Panel(right, new Vector2(-170f - i * 12f, -i * 46f), new Vector2(320f, 40f), glass, glass, 0.35f);
            Text rank = UiKit.Label(strip.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(-130f, 2f), new Vector2(50f, 40f), true);
            Text name = UiKit.Label(strip.transform, UiKit.Body, 20, Color.white, TextAnchor.MiddleLeft, new Vector2(30f, 0f), new Vector2(230f, 34f), true);
            rows.Add((strip, rank, name));
        }
        // The count and the result: a band in the race's blue sweeping across the screen behind them.
        band = UiKit.Paint(UiKit.Panel(board.transform, Vector2.zero, new Vector2(2600f, 190f), Color.white, Color.white, 0f), UiKit.Ink.Main, 0.92f);
        band.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        band.gameObject.SetActive(false);
        countdown = Leaning(board.transform, 150, new Vector2(0f, 10f), new Vector2(900f, 190f), TextAnchor.MiddleCenter);
        message = Leaning(board.transform, 64, new Vector2(0f, 10f), new Vector2(1600f, 120f), TextAnchor.MiddleCenter);
        // The controls, shown once on the grid before the countdown (not all race long).
        briefing = UiKit.Persona(UiKit.Panel(board.transform, new Vector2(0f, 20f), new Vector2(760f, 420f), glass, glass, 0.06f), 6f, 14f);
        UiKit.Label(briefing.transform, UiKit.Display, 40, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 160f), new Vector2(700f, 56f), true).text = L10n.T("race.controls.title");
        hint = UiKit.Label(briefing.transform, UiKit.Body, 24, Color.white, TextAnchor.MiddleLeft, new Vector2(20f, -10f), new Vector2(640f, 260f));
        UiKit.Label(briefing.transform, UiKit.Display, 24, UiKit.Ice, TextAnchor.MiddleCenter, new Vector2(0f, -172f), new Vector2(700f, 40f), true).text = L10n.T("race.controls.start");
        board.gameObject.SetActive(false);
    }

    // A big number or word leaning into the race: white, a hard shadow in the race's blue, a dark outline.
    static Text Leaning(Transform parent, int size, Vector2 at, Vector2 box, TextAnchor anchor)
    {
        Text text = UiKit.Label(parent, UiKit.Display, size, Color.white, anchor, at, box, true);
        text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
        UiKit.Tones blue = UiKit.Biome("race");
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = blue.main;
        shadow.effectDistance = new Vector2(1f, -1f) * Mathf.Max(3f, size * 0.05f);
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = blue.deep;
        outline.effectDistance = Vector2.one * 2f;
        return text;
    }

    // ---------------- the course ----------------

    void Build()
    {
        if (course) Destroy(course.gameObject);
        course = new GameObject("RaceCourse").transform;
        points.Clear();
        dirs.Clear();
        wrecks.Clear();
        foreground.Clear();
        // The centre line: straight off the grid and into the launcher, bending in between, smoothly and both ways.
        int n = Mathf.CeilToInt((crateRun ? 4600f : trench ? trenchLength : length) / Step);
        float heading = 0f, seed = Random.value * 100f;
        // Hairpins, podracer style: a long hard swing from one side to the other (up to 160 degrees), the next one
        // back. The heading never strays more than MaxHeading from straight on, so the course always moves forward
        // and never runs back across itself.
        var hairpins = new List<(int at, int steps, float target)>();
        float sign = Random.value < 0.5f ? 1f : -1f;
        int pins = Mathf.Max(4, Mathf.RoundToInt(length / 700f));
        for (int h = 0; h < pins; h++)
        {
            hairpins.Add((Mathf.RoundToInt(Mathf.Lerp(18f, n - 40f, (h + 0.5f) / pins)), Random.Range(6, 10), sign * Random.Range(55f, MaxHeading)));
            sign = -sign;
        }
        if (trench || crateRun) hairpins.Clear(); // the trench runs straight on, bending only a little
        Vector3 p = Origin;
        for (int i = 0; i <= n; i++)
        {
            points.Add(p);
            float bend = i > 10 && i < n - 14 ? (Mathf.PerlinNoise(seed, i * 0.045f) * 2f - 1f) * (crateRun ? .3f : trench ? 2f : 5f) : 0f;
            foreach (var h in hairpins) if (i >= h.at && i < h.at + h.steps) bend = (h.target - heading) / (h.at + h.steps - i);
            if (i >= n - 24) bend = i < n - 8 ? -heading / (n - 8 - i) : 0f; // straightened out for the run into the launcher
            heading = Mathf.Clamp(heading + bend, -MaxHeading, MaxHeading);
            Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            dirs.Add(dir);
            p += dir * Step;
        }
        total = n * Step;

        // The road: asphalt painted with its lanes, between low walls topped with light (cyan on the left, pink on
        // the right), gantries over it now and then and lamps along it; the city's ground painting all round, and
        // far out a skyline of towers fading into the haze.
        Material asphalt = TrainSim.Mat(Color.white, false), wall = TrainSim.Mat(new Color(0.5f, 0.52f, 0.6f)), steel = TrainSim.Mat(new Color(0.62f, 0.65f, 0.72f));
        asphalt.mainTexture = crateRun ? Resources.Load<Texture2D>("Ground/hell") : trench ? Resources.Load<Texture2D>("Ground/trench") : AsphaltTexture();
        if (trench) asphalt.mainTextureScale = new Vector2(1f, 0.5f); // the trench's deck: its pattern once across, a repeat every 40 m
        wall.SetInt("_Culling", 0);
        Material cyan = TrainSim.Mat(new Color(0.3f, 2.2f, 2.6f), false), pink = TrainSim.Mat(new Color(2.6f, 0.45f, 1.7f), false), gold = TrainSim.Mat(new Color(2.8f, 1.9f, 0.4f), false);
        if (!crateRun) { Tint(cyan, new Color(0.45f, 1f, 1.35f)); Tint(pink, new Color(0.2f, 0.5f, 1.5f)); } // Persona 3's blues, ice and electric, and dimmer
        if (crateRun) { Tint(cyan,new Color(2.4f,.35f,.08f)); Tint(pink,new Color(2.6f,.85f,.12f)); Tint(steel,new Color(.17f,.1f,.12f)); Tint(wall,new Color(.2f,.09f,.12f)); }
        Ribbon("Road", -roadWidth / 2f, roadWidth / 2f, 0.05f, asphalt);
        float wallHigh = trench ? 22f : 1.2f; // the trench: sheer walls either side
        bool kit = !trench && !crateRun; // the city's roadside is modelled (Tools/make_race_kit.py)
        if (kit || trench) // the trench's walls modelled too, 22 m of armour
        {
            Barriers(-1, cyan, trench ? "trenchwall" : "barrier");
            Barriers(1, pink, trench ? "trenchwall" : "barrier");
        }
        else
        {
            Wall("WallL", -roadWidth / 2f - 0.8f, -roadWidth / 2f, wallHigh, wall);
            Wall("WallR", roadWidth / 2f, roadWidth / 2f + 0.8f, wallHigh, wall);
            Ribbon("EdgeL", -roadWidth / 2f - 0.8f, -roadWidth / 2f, wallHigh + 0.02f, cyan);
            Ribbon("EdgeR", roadWidth / 2f, roadWidth / 2f + 0.8f, wallHigh + 0.02f, pink);
        }
        int gantry = 0;
        for (float s = 150f; s < total - 220f && !trench; s += 210f, gantry++) // the trench has its bridges instead (RaceTrench)
        {
            Vector3 c = PointAt(s, out Vector3 d), right = Vector3.Cross(Vector3.up, d);
            Quaternion rot = Quaternion.LookRotation(d);
            if (kit)
            {
                Kit("gantry", c, rot, gantry % 2 == 0 ? cyan : pink);
                continue;
            }
            for (int side = -1; side <= 1; side += 2) Block(c + right * side * (roadWidth / 2f + 3f) + Vector3.up * 8f, rot, new Vector3(1.8f, 16f, 1.8f), steel);
            Block(c + Vector3.up * 16.5f, rot, new Vector3(roadWidth + 8f, 1.8f, 2f), steel);
            Block(c + Vector3.up * 15.4f, rot, new Vector3(roadWidth + 6f, 0.4f, 0.8f), gantry % 2 == 0 ? cyan : pink);
            if (trench) Block(c + Vector3.up * 17.8f, rot, new Vector3(roadWidth * 0.45f, 3.2f, 0.5f), gantry % 2 == 0 ? pink : cyan); // its sign (the city's: a poster, RaceCity)
        }
        for (float s = 20f; s < total && !trench; s += 45f) // nor lamps: its walls are lit
        {
            Vector3 c = PointAt(s, out Vector3 d), right = Vector3.Cross(Vector3.up, d);
            Quaternion rot = Quaternion.LookRotation(d);
            for (int side = -1; side <= 1; side += 2)
            {
                if (kit)
                {
                    // Its arm reaches out over the road (the model's +x).
                    Kit("lamp", c + right * side * (roadWidth / 2f + 2.2f), side < 0 ? rot : rot * Quaternion.Euler(0f, 180f, 0f), side < 0 ? cyan : pink);
                    continue;
                }
                Block(c + right * side * (roadWidth / 2f + 2.2f) + Vector3.up * 4.5f, rot, new Vector3(0.35f, 9f, 0.35f), steel);
                Block(c + right * side * (roadWidth / 2f + 0.8f) + Vector3.up * 9f, rot, new Vector3(3f, 0.45f, 0.9f), side < 0 ? cyan : pink);
            }
        }
        var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(ground.GetComponent<Collider>());
        ground.transform.SetParent(course, false);
        var area = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 q in points) area.Encapsulate(q);
        float groundSize = Mathf.Max(area.size.x, area.size.z) + 4000f; // under the whole course and out to the haze
        ground.transform.SetPositionAndRotation(area.center + Vector3.down * 0.02f, Quaternion.Euler(90f, 0f, 0f));
        ground.transform.localScale = Vector3.one * groundSize;
        Material groundMat = TrainSim.Mat(Color.white, false);
        groundMat.mainTexture = Resources.Load<Texture2D>(crateRun ? "Ground/hell" : trench ? "Ground/fortress" : "Ground/race"); // the city race: its own night plaza (Tools/make_race_ground.py)
        groundMat.mainTextureScale = Vector2.one * (groundSize / 54f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // The city along the way: its buildings on both sides, and its cars and barriers wrecked on the road.
        var tall = new List<GameObject>();
        var junk = new List<GameObject>();
        beacons.Clear();
        Material[] carPaints = System.Array.ConvertAll(new[] { new Color(0.85f, 0.12f, 0.14f), new Color(0.15f, 0.35f, 0.85f), new Color(0.95f, 0.95f, 0.97f), new Color(0.12f, 0.12f, 0.14f), new Color(1f, 0.75f, 0.1f), new Color(0.4f, 0.8f, 0.6f) }, c => TrainSim.Mat(c));
        Material beacon = TrainSim.Mat(new Color(2.2f, 0.3f, 0.15f), false); // red: danger, the one warm light
        foreach (GameObject kind in Resources.LoadAll<GameObject>(crateRun ? "Scenery/hell" : trench ? "Scenery/fortress" : "Scenery/city"))
        {
            Bounds box = Bounds(kind);
            if (trench) { if (box.size.y > 1f && box.size.y < 4f) junk.Add(kind); } // the base's crates, cores and radars in the trench
            else if (box.size.y > 4f) tall.Add(kind);
            else if (kind.name.Contains("car") || kind.name.Contains("Taxi") || kind.name.Contains("Police") || kind.name.Contains("barrier")) junk.Add(kind);
        }
        for (float s = 30f; s < total - 160f && tall.Count > 0 && !crateRun; s += Random.Range(26f, 40f))
            for (int side = -1; side <= 1; side += 2)
            {
                if (Random.value < 0.25f) continue;
                Vector3 at = PointAt(s, out Vector3 dir) + Vector3.Cross(Vector3.up, dir) * side * (roadWidth / 2f + Random.Range(10f, 26f));
                if (!OffRoad(at, 12f)) continue; // a bend's other leg runs close by: nothing on its road
                Instantiate(tall[Random.Range(0, tall.Count)], at, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f), course);
            }
        for (int i = 0, wreckCount = Mathf.RoundToInt(obstacles * (trench ? trenchLength * 0.5f : length) / 2600f); i < wreckCount && junk.Count > 0 && !crateRun; i++)
        {
            float s = Mathf.Lerp(220f, total - 260f, (i + Random.value) / wreckCount);
            Vector3 at = PointAt(s, out Vector3 dir) + Vector3.Cross(Vector3.up, dir) * Random.Range(-roadWidth * 0.38f, roadWidth * 0.38f);
            Quaternion flat = Quaternion.LookRotation(dir);
            // In the city a car slewed across the lane (Tools/make_race_kit.py), in the trench the base's junk at twice
            // its size; either way ringed with cones, under a warning sign on a pole (on the side the racers come from)
            // with a beacon flashing on it.
            if (trench)
            {
                Transform junkBody = Instantiate(junk[Random.Range(0, junk.Count)], at, flat * Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f), course).transform;
                junkBody.localScale *= 2f;
                foreground.Add(junkBody);
            }
            else
            {
                GameObject car = Kit("car", at, flat * Quaternion.Euler(0f, Random.Range(30f, 150f), Random.Range(-8f, 8f)), null);
                foreground.Add(car.transform);
                car.transform.localScale *= 1.6f;
                Renderer body = car.GetComponentInChildren<Renderer>();
                var mats = body.sharedMaterials;
                for (int k = 0; k < mats.Length; k++) if (mats[k].name == "CarPaint") mats[k] = carPaints[Random.Range(0, carPaints.Length)];
                body.sharedMaterials = mats;
            }
            Transform warning = Kit("hazard", at, flat * Quaternion.Euler(0f, 180f, 0f), null).transform;
            warning.localScale *= 1.3f;
            foreground.Add(warning);
            beacons.Add(Ball(at - dir * 2.8f * 1.3f + Vector3.up * 10.2f, 1.1f, beacon));
            wrecks.Add((at, 3.4f));
        }

        if (crateRun) { BuildCrates(); course.gameObject.SetActive(false); return; }
        if (trench) { BuildVent(); Freeze(); course.gameObject.SetActive(false); return; } // the trench ends at the vent, not a launcher
        DressCity();
        // The launcher at B: chevrons on the road point at it, a chequered line, then a ramp that curves up to the
        // sky through rings of light, between two striped towers.
        Vector3 end = points[^1], fwd = dirs[^1], side0 = Vector3.Cross(Vector3.up, fwd);
        Quaternion look = Quaternion.LookRotation(fwd);
        Material dark = TrainSim.Mat(new Color(0.08f, 0.08f, 0.1f)), white = TrainSim.Mat(new Color(0.95f, 0.95f, 0.97f)), stripe = TrainSim.Mat(new Color(0.85f, 0.2f, 0.18f));
        for (int k = 1; k <= 6; k++)
        {
            Vector3 c = PointAt(total - k * 22f, out Vector3 d);
            Quaternion rot = Quaternion.LookRotation(d);
            Vector3 right = Vector3.Cross(Vector3.up, d);
            for (int s = -1; s <= 1; s += 2) Block(c + right * s * 3.4f + Vector3.up * 0.07f, rot * Quaternion.Euler(0f, -s * 38f, 0f), new Vector3(1.1f, 0.05f, 10f), gold);
        }
        for (int x = 0; x < 16; x++)
            for (int z = 0; z < 2; z++)
                Block(end + side0 * ((x + 0.5f) / 16f - 0.5f) * roadWidth + fwd * (z - 0.5f) * 2.6f + Vector3.up * 0.06f, look, new Vector3(roadWidth / 16f, 0.05f, 2.6f), (x + z) % 2 == 0 ? white : dark);
        ramp.Clear();
        Vector3 q0 = end;
        for (int k = 0; k <= RampSteps; k++)
        {
            float pitch = 58f * Mathf.SmoothStep(0f, 1f, k / (float)RampSteps);
            ramp.Add((q0, pitch));
            Vector3 along = fwd * Mathf.Cos(pitch * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad);
            Vector3 mid = q0 + along * RampStep * 0.5f;
            Quaternion tilt = look * Quaternion.Euler(-pitch, 0f, 0f);
            Block(mid - Vector3.up * 0.4f, tilt, new Vector3(18f, 0.8f, RampStep + 0.3f), steel);
            for (int s = -1; s <= 1; s += 2) Block(mid + side0 * s * 8.6f + Vector3.up * 0.4f, tilt, new Vector3(0.6f, 0.9f, RampStep + 0.3f), s < 0 ? cyan : pink);
            if (k % 4 == 2 && mid.y > 2f) for (int s = -1; s <= 1; s += 2) Block(new Vector3(mid.x, mid.y / 2f, mid.z) + side0 * s * 6f, look, new Vector3(1.6f, mid.y, 1.6f), steel);
            if (k % 7 == 6) // a ring of light round the rail
            {
                float radius = 12f + k * 0.12f;
                for (int b = 0; b < 20; b++)
                {
                    float a = b * Mathf.PI / 10f;
                    Block(mid + tilt * new Vector3(Mathf.Cos(a), Mathf.Sin(a) + 0.6f, 0f) * radius, tilt * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), new Vector3(1.2f, radius * 0.34f, 1.2f), (k / 7) % 2 == 0 ? gold : cyan);
                }
            }
            q0 += along * RampStep;
        }
        launchDir = fwd * Mathf.Cos(58f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(58f * Mathf.Deg2Rad);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 foot = end + fwd * 70f + side0 * s * 34f;
            for (int y = 0; y < 10; y++) Block(foot + Vector3.up * (y * 10f + 5f), look, new Vector3(7f, 10f, 7f), y % 2 == 0 ? white : stripe);
            Ball(foot + Vector3.up * 102f, 3f, gold);
        }
        Freeze();
        course.gameObject.SetActive(false);
    }

    // Everything of the course that never moves, batched together: thousands of blocks drawn as a few big meshes.
    // What moves (the crowd, the traffic, the holograms and blimps, the beacons, the laser gates) stays as it is.
    // The course is backdrop, drawn the way the ride draws its scenery (TrainSim.Fade): no outline, its colours a part
    // of the way to a cool haze, so the racers and the wrecks, outlined at full colour, read first. Lights (HDR) and
    // pictures keep their colour.
    readonly HashSet<Transform> foreground = new();
    readonly Dictionary<Material, Material> backdrop = new();
    void Backdrop()
    {
        backdrop.Clear();
        Color haze = (trench ? new Color(0.14f, 0.17f, 0.26f) : new Color(0.55f, 0.6f, 0.72f)).linear; // the trench's steel kept dark
        foreach (Renderer part in course.GetComponentsInChildren<Renderer>(true))
        {
            bool kept = false;
            for (Transform up = part.transform; up && up != course && !kept; up = up.parent) kept = foreground.Contains(up);
            if (kept) continue;
            Material[] mats = part.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i] || !mats[i].HasProperty("_MainColor")) continue;
                if (!backdrop.TryGetValue(mats[i], out Material twin))
                {
                    twin = new Material(mats[i]) { name = mats[i].name + " (backdrop)" };
                    Color c = mats[i].GetColor("_MainColor");
                    if (c.maxColorComponent <= 1.05f && !mats[i].mainTexture) twin.SetColor("_MainColor", Color.Lerp(c, haze, 0.35f));
                    twin.DisableKeyword("N_F_O_ON");
                    twin.SetShaderPassEnabled("SRPDefaultUnlit", false); // the outline's pass
                    backdrop[mats[i]] = twin;
                }
                mats[i] = twin;
            }
            part.sharedMaterials = mats;
        }
    }

    void Freeze()
    {
        Backdrop();
        var moving = new HashSet<Transform>();
        foreach (var c in crowd) moving.Add(c.t);
        foreach (var c in traffic) moving.Add(c.t);
        foreach (Transform h in holograms) moving.Add(h);
        foreach (Transform b in beacons) moving.Add(b);
        foreach (var l in lasers) moving.Add(l.beam);
        var still = new List<GameObject>();
        foreach (MeshRenderer part in course.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter shape = part.GetComponent<MeshFilter>();
            if (!shape || !shape.sharedMesh || !shape.sharedMesh.isReadable) continue;
            bool moves = false;
            for (Transform up = part.transform; up && up != course && !moves; up = up.parent) moves = moving.Contains(up);
            if (!moves) still.Add(part.gameObject);
        }
        StaticBatchingUtility.Combine(still.ToArray(), course.gameObject);
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

    readonly List<Transform> beacons = new(); // over the wrecks, pulsing

    // Clear of every stretch of road by this much, beyond its edge.
    bool OffRoad(Vector3 at, float margin)
    {
        float clear = roadWidth / 2f + margin;
        for (int q = 0; q < points.Count; q += 2) if ((points[q] - at).sqrMagnitude < clear * clear) return false;
        return true;
    }

    Transform Ball(Vector3 at, float size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(course, false);
        go.transform.position = at;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    // The lanes painted on the asphalt: across the road u, along one stretch v (Ribbon).
    static Texture2D asphaltTex;
    static Texture2D AsphaltTexture()
    {
        if (asphaltTex) return asphaltTex;
        const int w = 512, h = 128;
        asphaltTex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
        var px = new Color32[w * h];
        var rng = new System.Random(5);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                int g = 62 + rng.Next(-7, 8) + (int)((Mathf.PerlinNoise(x * 0.015f, y * 0.04f) - 0.5f) * 18f);
                Color32 c = new Color32((byte)g, (byte)g, (byte)(g + 7), 255);
                if ((u > 0.022f && u < 0.034f) || (u > 0.966f && u < 0.978f)) c = new Color32(228, 228, 234, 255);       // edge lines
                else if (v < 0.55f && (Mathf.Abs(u - 0.25f) < 0.004f || Mathf.Abs(u - 0.75f) < 0.004f)) c = new Color32(210, 210, 216, 255); // lane dashes
                else if (Mathf.Abs(u - 0.5f) < 0.01f && Mathf.Abs(u - 0.5f) > 0.003f) c = new Color32(228, 178, 58, 255); // double yellow
                px[y * w + x] = c;
            }
        asphaltTex.SetPixels32(px);
        asphaltTex.Apply(true);
        return asphaltTex;
    }

    // A wall along the road from offset from to offset to across it, h high: its two faces (the top is a Ribbon).
    void Wall(string name, float from, float to, float h, Material m)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 r0 = Vector3.Cross(Vector3.up, dirs[i]), r1 = Vector3.Cross(Vector3.up, dirs[i + 1]);
            foreach (float off in new[] { from, to })
            {
                int v = verts.Count;
                verts.Add(points[i] + r0 * off); verts.Add(points[i] + r0 * off + Vector3.up * h);
                verts.Add(points[i + 1] + r1 * off); verts.Add(points[i + 1] + r1 * off + Vector3.up * h);
                tris.AddRange(new[] { v, v + 1, v + 2, v + 2, v + 1, v + 3 });
            }
        }
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        var go = new GameObject(name);
        go.transform.SetParent(course, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
    }

    // A piece of the city's roadside kit (Resources/Models/Race_<name>), toon materials, GlowAccent lit in accent.
    GameObject Kit(string name, Vector3 at, Quaternion turn, Material accent)
    {
        var go = Instantiate(Resources.Load<GameObject>("Models/Race_" + name), at, turn, course);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) Dress(r, accent);
        return go;
    }

    static void Dress(Renderer r, Material accent)
    {
        TrainSim.Toon(r);
        var mats = r.sharedMaterials;
        for (int k = 0; k < mats.Length; k++) if (mats[k].name == "GlowAccent") mats[k] = accent;
        r.sharedMaterials = mats;
    }

    // A wall along one side (1 right, -1 left): the city's concrete barrier or the trench's, one model a stretch, bent
    // to follow the course, all in one mesh. The model runs 0..-Step along z with its road face at x = 0 and its back
    // towards -x; the left side's is mirrored.
    void Barriers(int side, Material accent, string name = "barrier")
    {
        var model = Resources.Load<GameObject>("Models/Race_" + name);
        Mesh src = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        Vector3[] sv = src.vertices, sn = src.normals;
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var tris = new List<int>[src.subMeshCount];
        for (int k = 0; k < tris.Length; k++) tris[k] = new List<int>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            int start = verts.Count;
            for (int v = 0; v < sv.Length; v++)
            {
                float t = Mathf.Clamp01(-sv[v].z / Step), out_ = -sv[v].x;
                Vector3 dir = Vector3.Slerp(dirs[i], dirs[i + 1], t).normalized, right = Vector3.Cross(Vector3.up, dir);
                verts.Add(Vector3.Lerp(points[i], points[i + 1], t) + right * side * (roadWidth / 2f + out_) + Vector3.up * sv[v].y);
                normals.Add((right * side * -sn[v].x + Vector3.up * sn[v].y - dir * sn[v].z).normalized);
            }
            for (int k = 0; k < tris.Length; k++)
            {
                int[] st = src.GetTriangles(k);
                for (int j = 0; j < st.Length; j += 3) // mirrored on the left: wound the other way round
                    tris[k].AddRange(side > 0 ? new[] { start + st[j], start + st[j + 1], start + st[j + 2] } : new[] { start + st[j], start + st[j + 2], start + st[j + 1] });
            }
        }
        var mesh = new Mesh { name = "Barrier", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, subMeshCount = tris.Length };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        for (int k = 0; k < tris.Length; k++) mesh.SetTriangles(tris[k], k);
        mesh.RecalculateBounds();
        var go = new GameObject(side < 0 ? "BarrierL" : "BarrierR");
        go.transform.SetParent(course, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterials = model.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        Dress(r, accent);
    }

    // On the launcher's rail s metres past the line, and its pitch; past the top, off along the launch.
    Vector3 RampAt(float s, out float pitch)
    {
        float top = RampSteps * RampStep;
        if (s >= top) { pitch = ramp[^1].pitch; return ramp[^1].at + launchDir * (s - top); }
        int k = Mathf.Clamp((int)(s / RampStep), 0, ramp.Count - 2);
        float f = s / RampStep - k;
        pitch = Mathf.Lerp(ramp[k].pitch, ramp[k + 1].pitch, f);
        return Vector3.Lerp(ramp[k].at, ramp[k + 1].at, f);
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
        if (Story.Active && Story.Hero == "xiuhtecuhtli" && Story.Current.race && !field.Contains(0)) field.Add(0); // BBA receives his launch slot.
        var others = new List<int>();
        for (int i = 0; i < Locomotives.Models.Length; i++) if (!field.Contains(i)) others.Add(i);
        while (field.Count < 6 && others.Count > 0) { int k = Random.Range(0, others.Count); field.Add(others[k]); others.RemoveAt(k); }
        if (trench || crateRun) field.RemoveRange(1, field.Count - 1); // alone down the trench
        // On the grid, two abreast; ours in the middle of it.
        int[] slot = { 2, 0, 1, 3, 4, 5 };
        float hard = Mathf.Clamp01(train.Difficulty / 30f);
        for (int i = 0; i < field.Count; i++)
        {
            var r = new Racer { loco = field[i], player = i == 0, name = L10n.T(Locomotives.NameKeys[field[i]]) };
            r.skill = r.player ? 1f : Random.Range(1f, 1.06f) + hard * 0.05f + (i == 1 ? 0.05f : 0f); // the rival a little quicker
            r.lane = Random.Range(-0.25f, 0.25f) * roadWidth;
            int row = slot[i] / 2, col = slot[i] % 2;
            r.pos = Origin + Vector3.forward * (60f - row * 16f) + Vector3.right * (col == 0 ? -8f : 8f);
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
        launched = false;
        sky.SetActive(true);
        haze.enabled = true;
        startsAt = float.MaxValue; // the countdown waits for the controls to be read
        endsAt = -1f;
        message.text = "";
        hint.text = L10n.T(crateRun ? "giger.crates.controls" : trench ? "trench.controls" : "race.controls");
        sky.GetComponent<Renderer>().sharedMaterial.mainTexture = Resources.Load<Texture2D>(crateRun ? "Sky/hell" : trench ? "Sky/trench" : "Sky/race");
        // The city race at blue hour, the trench under open space (Tools/make_race_sky.py), each haze to match; the crates keep their warm one.
        if (haze.profile.TryGet(out UnityEngine.Rendering.HighDefinition.Fog fog)) fog.color.value = crateRun ? new Color(1f, 0.74f, 0.58f) : trench ? new Color(0.07f, 0.1f, 0.22f) : new Color(0.32f, 0.46f, 0.78f);
        standings.transform.parent.gameObject.SetActive(!trench && !crateRun); // alone in the trench: no standings
        if (trench) TrenchBegin();
        briefing.gameObject.SetActive(true);
        dots.Clear();
        foreach (Transform c in markers) Destroy(c.gameObject);
        UiKit.Tones blue = UiKit.Biome("race");
        foreach (Racer r in BySlot())
        {
            Color c = r.player ? Color.white : racers.Count > 1 && r == racers[1] ? UiKit.Rose : blue.main;
            Slanted tag = UiKit.Panel(markers, new Vector2(0f, 26f), r.player ? new Vector2(40f, 30f) : new Vector2(30f, 22f), c, c, 0.3f);
            if (r.player) UiKit.Panel(tag.transform, new Vector2(0f, -17f), new Vector2(10f, 10f), c, c, 0f).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Text rank = UiKit.Label(tag.transform, UiKit.Display, r.player ? 24 : 18, r.player ? blue.deep : Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 1f), tag.rectTransform.sizeDelta, true);
            dots.Add((tag, rank));
        }
        course.gameObject.SetActive(true);
        view.enabled = true;
        board.gameObject.SetActive(true);
        Active = true;
        raceSound.Begin();
        Time.timeScale = 0f;
        foreach (Racer r in racers) Track(r, Time.unscaledTime);
        UpdateCamera(1f, true);
    }

    public void TestWin()
    {
        if (!Active) return;
        winner = me;
        if (crateRun) { CratesBroken = CrateCount; CratesSeen = CrateCount; }
        trenchWon = true;
        End();
    }

    void End()
    {
        Active = false;
        raceSound.Stop();
        board.gameObject.SetActive(false);
        foreach (Transform s in streaks) s.gameObject.SetActive(false);
        bool won = crateRun ? CratesPassed(CratesBroken) : trench ? trenchWon : winner == me;
        if (won)
        {
            // lost, it stays on screen while the player is asked whether to try again (Begin builds it anew)
            view.enabled = false;
            sky.SetActive(false);
            haze.enabled = false;
            if (course) Destroy(course.gameObject);
        }
        racers.Clear();
        fireballs.Clear();
        Time.timeScale = 1f;
        effects.Clear();
        foreach (ParticleSystem system in unscaled) if (system) { var main = system.main; main.useUnscaledTime = false; } // the pool's copies, back to game time
        unscaled.Clear();
        System.Action<bool> finished = done;
        done = null;
        trench = false;
        crateRun = false;
        finished?.Invoke(won);
    }

    // ---------------- the race ----------------

    void Update()
    {
        // Testing aid: N on the keyboard starts a race on the spot.
        if (!Active && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame && !(Story.Active ? Story.Current.eyeBoss || Story.Current.cave || (Story.Current.gates && !Story.GatesPassed) : train.BiomeId == "space") && !train.Racing && !train.Paused && !train.AtStation && !train.InEvent && !train.BossFight)
        {
            if (Story.Active && Story.Current.crates) BeginCrates(train.RaceFinished);
            else if (Story.Active && Story.Current.trench) BeginTrench(train.RaceFinished); // the base's chapter: the trench run, as its real finale
            else Begin(won => { if (Story.Active && Story.Current.race) train.RaceFinished(won); else train.Toast(L10n.T(won ? "race.won" : "race.lost.short")); }); // in the race's chapter, as the real finale
        }
        raceSound.Pause(train.Paused);
        if (!Active || train.Paused) return;
        Time.timeScale = 0f; // everything outside waits (a pause and back would have let it run)
        float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (briefing.gameObject.activeSelf)
        {
            // On the grid, the controls up: âœ•, Space or Enter and the countdown starts.
            bool ready = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                         || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
            if (ready)
            {
                briefing.gameObject.SetActive(false);
                startsAt = now + 3.6f;
                Sound.Play("confirm");
            }
        }
        bool going = now >= startsAt;
        if (!briefing.gameObject.activeSelf) raceSound.Countdown(going ? 0 : Mathf.CeilToInt(startsAt - now));
        countdown.text = briefing.gameObject.activeSelf ? "" : now < startsAt ? Mathf.CeilToInt(startsAt - now).ToString() : now < startsAt + 1f ? L10n.T("race.go") : "";
        bool banner = countdown.text != "" || message.text != "";
        if (band.gameObject.activeSelf != banner) band.gameObject.SetActive(banner);
        if (banner) // sweeps in from the side, then a slight pulse on each count
            band.rectTransform.anchoredPosition = new Vector2(0f, 10f) + Vector2.right * 2600f * (1f - UiKit.Back(Mathf.Repeat(Mathf.Min(now - startsAt + 3.6f, 99f), 1f) * 4f)) * (countdown.text != "" ? 1f : 0f);

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (!effects[i].graph || now > effects[i].until) { effects.RemoveAt(i); continue; }
            effects[i].graph.Simulate(dt);
        }
        Fireballs(now);
        foreach (Transform b in beacons) if (b) b.localScale = Vector3.one * (1.1f + 0.5f * Mathf.Abs(Mathf.Sin(now * 5f)));
        foreach (Racer r in racers)
        {
            if (Crashed(r, now)) continue;
            float throttle = 0f, steer = 0f;
            bool brake = false, boost = false;
            int shoveWay = 0;
            if (going && r.finishedAt < 0f && !aiming)
            {
                if (r.player && !autoDrive) Controls(out throttle, out steer, out brake, out boost, out shoveWay);
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
        if (!crateRun && winner == null && racers[0].finishedAt >= 0f)
        {
            winner = racers[0];
            if (LaunchDenied && LaunchActor != null) LaunchActor.finishedAt = now + .01f;
            raceSound.Cue(winner == me ? "race_win" : "race_lose");
            message.text = LaunchDenied ? L10n.T("story.xiuhtecuhtli.race.denied") : winner == me ? L10n.T("race.won") : L10n.T("race.lost", winner.name);
            endsAt = now + 7f;
        }
        Show(now);
        UpdateCamera(dt, false);
        if (trench && going) TrenchUpdate(now, dt);
        if (crateRun) CratesUpdate(now, going);
        else if (!trench) CityUpdate(now, going, dt);
        UpdateSound(now);
        if (endsAt > 0f && now >= endsAt) End();
    }

    void UpdateSound(float now)
    {
        if (winner != null || (crateRun && endsAt > 0f))
        {
            raceSound.StopDriving();
            return;
        }
        raceSound.Drive(me.speed / Mathf.Max(1f, boostSpeed), me.turboUsed, me.driftFor > 0f, playerBraking, now < me.crashedUntil, me.heat);
        Racer first = null, second = null;
        float near = 45f, next = 45f;
        foreach (var r in racers)
        {
            if (r == me || now < r.crashedUntil || !r.body) continue;
            float distance = Vector3.Distance(me.body.position, r.body.position);
            if (distance < near) { second = first; next = near; first = r; near = distance; }
            else if (distance < next) { second = r; next = distance; }
        }
        raceSound.Rival(0, near, first == null ? 0f : Vector3.Dot(first.body.position - me.body.position, view.transform.right) / 18f, first == null ? 0f : first.speed / Mathf.Max(1f, boostSpeed));
        raceSound.Rival(1, next, second == null ? 0f : Vector3.Dot(second.body.position - me.body.position, view.transform.right) / 18f, second == null ? 0f : second.speed / Mathf.Max(1f, boostSpeed));
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
            if (along > 0f && along < 25f + r.speed * 1.2f) // seen in time at speed
            {
                float side = Vector3.Dot(to, Vector3.Cross(Vector3.up, here));
                if (Mathf.Abs(side - lane) < 9f) lane = side + (side > 0f ? -12f : 12f); // round it, wide
            }
        }
        Vector3 target = ahead + Vector3.Cross(Vector3.up, dir) * Mathf.Clamp(lane, -roadWidth * 0.4f, roadWidth * 0.4f);
        Vector3 way = target - r.pos;
        float want = Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg;
        steer = Mathf.Clamp(Mathf.DeltaAngle(r.yaw, want) / 20f, -1f, 1f);
        // Ours far ahead: they push harder (up to a fifth quicker, turbo kept hotter); ours behind: a touch easier.
        float gap = me.progress - r.progress;
        r.pace = r.skill + Mathf.Clamp(gap / 180f, 0f, 0.45f); // ours ahead: far harder the further, never easier
        boost = bendAhead < 22f && !r.turboLocked && r.heat < 0.8f; // in one long burst, let go short of the top
        // Flat out everywhere but the hairpins; brakes only for those (off the turbo it just coasts down).
        float bendFactor = Mathf.Lerp(1f, 0.78f, Mathf.Clamp01(bendAhead / 90f));
        float targetSpeed = (boost ? boostSpeed : topSpeed) * r.pace * bendFactor;
        throttle = r.speed < targetSpeed ? 1f : 0f;
        brake = bendAhead > 45f && r.speed > topSpeed * r.pace * bendFactor + 10f;
        shoveWay = 0;
        foreach (Racer o in racers)
        {
            if (o == r || now < r.shoveReady) continue;
            Vector3 d = o.pos - r.pos;
            if (d.magnitude < 7.5f && Mathf.Abs(Vector3.Dot(d, here)) < 4.5f && Random.value < (o.player ? 0.09f : 0.03f)) // ours above all
                shoveWay = Vector3.Dot(d, Vector3.Cross(Vector3.up, here)) > 0f ? 1 : -1;
        }
    }

    void Run(Racer r, float now, float dt, float throttle, float steer, bool brake, bool boost, int shoveWay)
    {
        if (r.player) playerBraking = brake;
        if (r.finishedAt >= 0f)
        {
            if (r == LaunchActor)
            {
                // Up the rail ever faster, sparks off it, and at the top shot off into the sky.
                r.speed = Mathf.MoveTowards(r.speed, boostSpeed * 1.6f, 40f * dt);
                r.rampS += r.speed * dt;
                r.boost = 1f;
                if ((sparkIn -= dt) <= 0f) { sparkIn = 0.06f; Spark(r.body.position - r.body.forward * 4f, now); }
                if (!launched && r.rampS > RampSteps * RampStep)
                {
                    launched = true;
                    raceSound.Cue("race_launch");
                    Fireball(r.body.position, now);
                    if (r.player) shake = 0.6f;
                }
            }
            else
            {
                r.speed = Mathf.MoveTowards(r.speed, 0f, 30f * dt);
                r.pos += Quaternion.Euler(0f, r.way, 0f) * Vector3.forward * r.speed * dt;
            }
            return;
        }
        bool stalled = now < r.overheatUntil;
        bool boosting = boost && !stalled && !r.turboLocked && r.finishedAt < 0f;
        if (boosting)
        {
            r.heat += heatPerSecond * dt;
            r.turboUsed = true;
            if (r.heat >= 1f) { Crash(r, now); return; } // run to the top: the engine blows up
        }
        else
        {
            if (r.turboUsed) r.turboLocked = true; // let go: it cools all the way down before the next one
            r.turboUsed = false;
            r.heat = Mathf.Max(0f, r.heat - coolPerSecond * dt);
            if (r.heat <= 0f) r.turboLocked = false;
        }
        bool crawling = now < r.recoverUntil;
        float pace = r.player ? 1f : Mathf.Max(1f, r.pace); // a rival pushing hard goes a little past ours' top
        float max = crawling ? crawlSpeed : stalled ? topSpeed * 0.45f : (boosting ? boostSpeed : topSpeed) * pace;
        if (r.finishedAt >= 0f) max = boostSpeed;
        // The brake with the stick over and some speed on: a drift. It turns far sharper and slides, losing little;
        // held long enough, it gives a kick of speed on the way out.
        bool drifting = brake && Mathf.Abs(steer) > 0.3f && r.speed > 25f && !crawling;
        if (drifting) r.driftFor += dt;
        else
        {
            if (r.driftFor > 0.8f)
            {
                r.speed += driftKick;
                if (r.player) raceSound.Cue("race_drift_release", .8f);
            }
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
            if (r.player) raceSound.Cue("ascent_pass_1", .45f);
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
                    if (r.player || o.player) raceSound.Bump(Vector3.Dot(d, view.transform.right) / 7.5f);
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
                bool near = r.player || Mathf.Abs(me.progress - r.progress) < 70f; // rivals only come to grief where ours can see it
                if (near && into > crashSpeed * (r.player ? 1f : 3f) && now >= r.recoverUntil) { Crash(r, now); return; } // and take the walls far better
                r.pos -= right * (side - Mathf.Sign(side) * limit);
                r.speed *= 0.97f;
                r.yaw = Mathf.MoveTowardsAngle(r.yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 40f * Time.unscaledDeltaTime);
                if (r.player)
                {
                    shake = Mathf.Max(shake, 0.12f);
                    raceSound.Bump(Mathf.Sign(side), .6f, true);
                }
            }
            bool ghost = !r.player && Mathf.Abs(me.progress - r.progress) > 70f; // a rival away from ours, out of sight: no wrecks to hold it back
            foreach (var w in wrecks)
            {
                if (now < r.recoverUntil || ghost) break; // getting going again: through them
                Vector3 d = r.pos - w.at;
                d.y = 0f;
                if (d.magnitude < w.radius + Radius)
                {
                    r.pos = w.at + d.normalized * (w.radius + Radius);
                    if (r.speed > crashSpeed * (r.player ? 1f : 3f) && now >= r.recoverUntil) { Crash(r, now); return; } // a rival only blows up hitting one flat out
                    r.speed *= r.player ? 0.4f : 0.75f;
                    if (r.player) { shake = 0.45f; raceSound.Bump(0f); }
                }
            }
            if (r.progress >= total) r.finishedAt = now;
        }
        // On the ground: hovering, bobbing, leaning into the turn. The winner rides the launcher's rail.
        if (!crateRun && r == LaunchActor && r.finishedAt >= 0f)
        {
            Vector3 on = RampAt(r.rampS, out float rise);
            r.progress = total + r.rampS;
            r.yaw = Mathf.Atan2(dirs[^1].x, dirs[^1].z) * Mathf.Rad2Deg;
            r.body.SetPositionAndRotation(on + Vector3.up * Hover, Quaternion.Euler(-rise, r.yaw, 0f));
            r.glow.localScale = new Vector3(0.6f, 0.6f, 3.4f);
            return;
        }
        float lift = Hover + Mathf.Sin(now * 3f + r.loco) * 0.12f, pitch = 0f;
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
                if (a.player || b.player)
                {
                    shake = Mathf.Max(shake, 0.15f);
                    raceSound.Bump(Vector3.Dot((a.player ? b.pos : a.pos) - me.pos, view.transform.right) / 7.5f, .7f);
                }
            }
    }

    void Show(float now)
    {
        int mine = racers.IndexOf(me);
        place.text = (mine + 1).ToString();
        placeOf.text = "/ " + racers.Count;
        bool hot = me.heat > 0.8f && (int)(now * 6f) % 2 == 0; // near the top: the warning flashes
        speedo.text = me.heat > 0.8f ? L10n.T("race.overheat") : Mathf.RoundToInt(me.speed * 4f).ToString();
        speedo.color = hot ? UiKit.Rose : Color.white;
        UiKit.Tones blue = UiKit.Biome("race");
        int lit = Mathf.CeilToInt(me.heat * HeatCells - 0.01f);
        for (int i = 0; i < HeatCells; i++)
        {
            float share = i / (float)HeatCells;
            Color on = me.turboLocked ? UiKit.Slate : share < 0.55f ? blue.hot : share < 0.8f ? Color.white : UiKit.Rose; // grey: cooling, not ready yet
            Slanted cell = heatCells[i];
            cell.color = cell.bottom = i < lit ? on : new Color(0f, 0f, 0f, 0.55f);
            cell.rectTransform.localScale = new Vector3(1f, i == lit - 1 && !me.turboLocked ? 1.25f + 0.1f * Mathf.Sin(now * 20f) : 1f, 1f); // the newest one stands up, fizzing
        }
        turboLabel.text = L10n.T(me.turboLocked ? "race.turbo.cooling" : "race.turbo");
        // Ours lit white with dark writing, the leader in the race's blue, the rest dark glass.
        for (int i = 0; i < rows.Count; i++)
        {
            var (strip, rank, name) = rows[i];
            bool shown = i < racers.Count;
            if (strip.gameObject.activeSelf != shown) strip.gameObject.SetActive(shown);
            if (!shown) continue;
            Racer r = racers[i];
            Color back = r.player ? Color.white : i == 0 ? blue.main : new Color(0.04f, 0.08f, 0.2f, 0.78f);
            strip.color = strip.bottom = back;
            rank.text = (i + 1).ToString();
            name.text = r.name;
            rank.color = name.color = r.player ? blue.deep : Color.white;
            strip.rectTransform.localScale = Vector3.one * (r.player ? 1.06f : 1f);
        }
        int k = 0;
        foreach (Racer r in BySlot())
        {
            float t = Mathf.Clamp01(r.progress / total);
            dots[k].tag.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-340f, 340f, t), r.player ? 30f : 24f);
            dots[k].rank.text = (racers.IndexOf(r) + 1).ToString();
            if (r.player)
            {
                trackFill.rectTransform.sizeDelta = new Vector2(680f * t, 12f);
                dots[k].tag.transform.SetAsLastSibling(); // ours on top of the pack
            }
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
        if (me == null || (trench && aiming)) return; // at the hatch: RaceTrench.AimCamera
        if (winner != null && ramp.Count > 0)
        {
            Racer filmed = LaunchActor ?? winner;
            // Whoever won: filmed from beside the launcher going up it and off into the sky.
            Vector3 side0 = Vector3.Cross(Vector3.up, dirs[^1]);
            Vector3 spot = filmed.body.position + side0 * 36f - dirs[^1] * 18f + Vector3.up * 4f; // alongside it as it climbs
            view.transform.position = Vector3.SmoothDamp(view.transform.position, spot, ref camVelocity, 0.45f, Mathf.Infinity, dt);
            view.transform.rotation = Quaternion.Slerp(view.transform.rotation, Quaternion.LookRotation(filmed.body.position - view.transform.position), 4f * dt);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, 50f, 2f * dt);
            sky.transform.position = view.transform.position;
            foreach (Transform s in streaks) s.gameObject.SetActive(false);
            return;
        }
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
        sky.transform.position = view.transform.position;
        // Speed lines: thin streaks rushing past the camera, more of them the faster ours goes.
        float rush = Mathf.InverseLerp(58f, boostSpeed, me.speed);
        Transform cam = view.transform;
        for (int k = 0; k < streaks.Count; k++)
        {
            Transform s = streaks[k];
            bool on = k < 14 * rush; // a few, round the edges of the picture
            if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
            if (!on) continue;
            Vector3 local = cam.InverseTransformPoint(s.position);
            if (local.z < -2f || local.z > 80f || snap)
            {
                Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(9f, 15f);
                local = new Vector3(ring.x * 1.4f, ring.y * 0.7f + 2f, Random.Range(25f, 70f));
            }
            local.z -= me.speed * 1.4f * dt;
            s.SetPositionAndRotation(cam.TransformPoint(local), cam.rotation);
            s.localScale = new Vector3(0.025f, 0.025f, 2f + rush * 3.5f);
        }
    }

    // ---------------- crashes ----------------

    // A crash: it blows up, and a moment later is put back on the road where it was, crawling and blinking.
    void Crash(Racer r, float now)
    {
        if (trench && r.player) TrenchHurt(15); // in the trench a crash costs her health
        Effect("grenade", r.body.position + Vector3.up, 6f);
        Effect("smoke", r.body.position + Vector3.up, 4f);
        Fireball(r.body.position + Vector3.up * 1.5f, now);
        float distance = Vector3.Distance(me.body.position, r.body.position);
        if (r.player || distance < 60f)
            raceSound.Crash(Vector3.Dot(r.body.position - me.body.position, view.transform.right) / 20f, r.player ? 1f : Mathf.Lerp(.5f, .1f, distance / 60f), r.player);
        r.crashedUntil = now + respawnAfter;
        r.crashes++;
        r.recoverUntil = r.crashedUntil + recoverSeconds;
        r.speed = 0f;
        r.push = Vector3.zero;
        r.heat = 0f;
        r.turboUsed = r.turboLocked = false;
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
        // Put back well past where it crashed, on whichever lane is furthest from the wrecks just ahead.
        float s = Mathf.Min(r.progress + 30f, total - 5f);
        Vector3 at = PointAt(s, out Vector3 dir), right = Vector3.Cross(Vector3.up, dir);
        float lane = 0f, clearest = -1f;
        for (float l = -roadWidth * 0.35f; l <= roadWidth * 0.35f + 0.1f; l += roadWidth * 0.175f)
        {
            float gap = float.MaxValue;
            foreach (var w in wrecks)
            {
                Vector3 d = w.at - (at + right * l);
                float along = Vector3.Dot(d, dir);
                if (along > -10f && along < 60f) gap = Mathf.Min(gap, Mathf.Abs(Vector3.Dot(d, right)));
            }
            if (gap > clearest) { clearest = gap; lane = l; }
        }
        r.pos = new Vector3(at.x, 0f, at.z) + right * lane;
        r.progress = s;
        r.seg = Mathf.Min(points.Count - 2, (int)(s / Step));
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
    // flying out and burning down, and puffs of smoke rising after them. No objects: each ball is a spot in a list,
    // all drawn instanced, a batch per material (fireworks throw hundreds of them).
    readonly List<(Vector3 at, Vector3 velocity, float from, float until, float size, Material look)> fireballs = new();
    readonly Dictionary<Material, List<Matrix4x4>> fireDraws = new();
    Material[] fireMats;
    static Mesh sphere;
    // 0 orange, 1 yellow, 2 smoke (cool grey), 3 white-hot, 4 the drones' and turrets' red, 5 her blue.
    Material[] FireMats => fireMats ??= new[] { TrainSim.Mat(new Color(2.8f, 1.1f, 0.15f), false), TrainSim.Mat(new Color(2.8f, 2.2f, 0.7f), false), TrainSim.Mat(new Color(0.32f, 0.35f, 0.44f), false),
                                                TrainSim.Mat(new Color(4f, 3.8f, 3.4f), false), TrainSim.Mat(new Color(2.6f, 0.25f, 0.15f), false), TrainSim.Mat(new Color(0.6f, 1.6f, 3f), false) };
    // Streaks: sparks stretched along their way, drawn with the fire.
    readonly List<(Vector3 at, Vector3 velocity, float from, float until, float size, Material look)> flares = new();

    // A blast: a white flash, a burst of fire and sparks streaking out. No smoke and no shock ring: solid balls and
    // discs of them read as blobs up close, and the picture stays clean.
    void Fireball(Vector3 at, float now)
    {
        fireballs.Add((at, Vector3.zero, now, now + 0.14f, 5f, FireMats[3]));
        for (int k = 0; k < 12; k++)
        {
            Vector3 v = Random.onUnitSphere;
            v.y = Mathf.Abs(v.y);
            fireballs.Add((at, v * Random.Range(6f, 12f), now, now + Random.Range(0.5f, 0.9f), Random.Range(1.2f, 2.6f), FireMats[k % 2]));
        }
        for (int k = 0; k < 14; k++)
            flares.Add((at, Random.onUnitSphere * Random.Range(22f, 36f) + Vector3.up * 4f, now, now + Random.Range(0.25f, 0.45f), Random.Range(0.25f, 0.4f), FireMats[k % 3 == 0 ? 3 : 1]));
    }
    void Spark(Vector3 at, float now) =>
        fireballs.Add((at, Random.insideUnitSphere * 3f - Vector3.up * 2f, now, now + 0.5f, Random.Range(0.8f, 1.6f), FireMats[Random.Range(0, 2)]));
    // A small burst in one colour (a shot landing, a muzzle): a flash and a few streaks.
    void Burst(Vector3 at, float now, Material look, float size = 1f)
    {
        fireballs.Add((at, Vector3.zero, now, now + 0.08f, 1.6f * size, FireMats[3]));
        for (int k = 0; k < 6; k++) flares.Add((at, Random.onUnitSphere * 18f * size, now, now + 0.22f, 0.2f * size, look));
    }

    void Fireballs(float now)
    {
        float dt = Time.unscaledDeltaTime;
        foreach (var list in fireDraws.Values) list.Clear();
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            var f = fireballs[i];
            if (now >= f.until) { fireballs[i] = fireballs[^1]; fireballs.RemoveAt(fireballs.Count - 1); continue; }
            float t = Mathf.Clamp01((now - f.from) / (f.until - f.from));
            f.at += f.velocity * dt;
            f.velocity *= 1f - 2.5f * dt; // slowing as it burns
            fireballs[i] = f;
            if (now < f.from) continue;
            if (!fireDraws.TryGetValue(f.look, out var draws)) fireDraws[f.look] = draws = new List<Matrix4x4>();
            draws.Add(Matrix4x4.TRS(f.at, Quaternion.identity, Vector3.one * f.size * (1f - t) * (0.6f + t)));
        }
        for (int i = flares.Count - 1; i >= 0; i--)
        {
            var f = flares[i];
            if (now >= f.until) { flares[i] = flares[^1]; flares.RemoveAt(flares.Count - 1); continue; }
            float t = Mathf.Clamp01((now - f.from) / (f.until - f.from));
            f.at += f.velocity * dt;
            f.velocity *= 1f - 3f * dt;
            flares[i] = f;
            if (!fireDraws.TryGetValue(f.look, out var draws)) fireDraws[f.look] = draws = new List<Matrix4x4>();
            if (f.velocity.sqrMagnitude > 0.01f) // long while fast, thinning out
                draws.Add(Matrix4x4.TRS(f.at, Quaternion.LookRotation(f.velocity), new Vector3(f.size, f.size, f.size + f.velocity.magnitude * 0.12f) * (1f - t)));
        }
        if (!sphere) sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        foreach (var pair in fireDraws)
            if (pair.Value.Count > 0)
                Graphics.RenderMeshInstanced(new RenderParams(pair.Key) { worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000f), shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off }, sphere, 0, pair.Value);
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
        if (raceSound) raceSound.Stop();
        if (course) Destroy(course.gameObject);
    }

    void OnDisable() { if (raceSound) raceSound.Stop(); }
}

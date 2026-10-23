using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A chapter's finale as a boss fight in the style of Klonoa (Story.Part.eyeBoss): the hero runs round a ring of
// wagons floating in space, and in the middle hangs a purple ball covered in eyes. In turn: it attacks (first beams
// from its eyes at where she stands, then a beam swept round the ring that she has to jump), then a turret comes up
// on one of the wagons; she gets to it and takes it, and the fight is seen from the gunner's seat (as against the
// swarm), shooting its eyes shut; until it charges and blasts the turret, and she is back on the ring for the next
// attack. The third attack hurls great burning rocks that smash the wagons one by one, until only one is left
// under her, where the turret comes up; once the turret is blasted the ring is whole again. Every eye shut, it is beaten. Out of hearts, the chapter is lost. Outside, time stands still, like the
// boarding (BrawlEvent).
public class EyeBossEvent : MonoBehaviour
{
    public int hearts = 3;                             // counted in halves: a beam, the sweep or a ship takes half, a fall a whole one
    public float gapWidth = 2.4f;                      // the ring's two gaps, an easy jump; walk into one and she falls
    public int ships = 3;                              // little ships drifting round the ring all fight long: jump them
    public float shipSpeed = 3.2f, heartSpeed = 2.4f, heartEvery = 20f, heartChance = 0.15f; // a heart: rare, once a fight at most, and only from the third attack on
    public float ringRadius = 24f, heroSpeed = 9f, jumpSpeed = 8.5f, gravity = 24f;
    public int eyeCount = 22, eyeHits = 3;              // eyes all round it; shots to shut each
    public float attackSeconds = 13f, turretWait = 14f, gunnerSeconds = 10f, chargeSeconds = 1.8f;
    public float fireRate = 9f, aimSpeed = 90f, mouseAim = 0.12f;

    public bool Active { get; private set; }
    public Camera View => view;
    public bool Showing => view && view.enabled; // lost, it stays on screen while the player is asked whether to try again

    static readonly Vector3 Arena = new Vector3(-9000f, 0f, 0f);
    const float BossHeight = 9f, BossRadius = 6f, EyeRadius = 1.05f;
    enum Phase { Intro, Attack, Turret, Gunner, Blasted, Won, Lost }

    class Eye { public Transform root, iris, lid; public Vector3 normal; public int hits; public bool Shut => hits <= 0; }
    class Beam { public Transform line; public Vector3 from, to; public float fireAt, until; public bool hit; }
    class Rock { public Transform ball, warn; public int car; public Vector3 from; public float from0, land; }
    // On the ring after a hit: a burning patch where a beam struck (stand in it and it hurts), or a shock wave
    // running both ways round the ring from where a rock fell (jump it).
    class Hazard { public Transform body; public float at, way, from, until; public bool wave; }
    // In the turret's seat: an orb fired at her from an open eye; shoot it down before it gets there.
    class Orb { public Transform body; public Vector3 from; public float from0, land; }

    TrainSim train;
    EyeBossAudio bossSound;
    float nextStep;
    bool charging;
    InputAction move, look;
    BrawlClips clips;
    Camera view;
    Transform arena, boss, heroRoot, turret, sweep, barrels, marker;
    BrawlEvent.Rig hero;
    readonly List<Eye> eyes = new();
    readonly List<Beam> beams = new();
    readonly List<Transform> cars = new();  // the ring's wagons, in order round it
    readonly List<Rock> rocks = new();
    int survivor;                            // the wagon the rocks leave standing
    readonly List<float> carAngles = new();  // where each wagon stands round the ring
    readonly List<(Transform body, float angle, float way)> fleet = new(); // the little ships
    Transform pickup;                        // a heart that turns up now and then, walking round to a gap
    float pickupAngle, pickupWay, nextPickup, pickupFall = -1f;
    bool heartGiven;
    float fallUntil = -1f, blinkUntil, prevAngle, spin; // a fall down a gap; blinking yellow once back up; how fast she goes round
    int beamCount;
    readonly List<Hazard> hazards = new();
    readonly List<Orb> orbs = new();
    Transform sweep2;
    float sweepAngle2, sweepWay = 1f, nextFlip, flipAt = -1f, nextOrb, shake;
    Material scorchMat, waveMat, orbMat;
    float Warn => Mathf.Lerp(0.85f, 0.5f, attack / 6f); // how long a beam's mark shows before it fires: less each round
    Renderer[] heroParts;
    static MaterialPropertyBlock yellow;
    float nextRock;
    Material rockMat;
    readonly List<(Transform t, float until)> flashes = new();
    System.Action<bool> done;
    Phase phase;
    int attack, left, lastHeroClip = -1;
    float phaseUntil, nextBeam, angle, height, vertical, facing = 1f, hurtUntil, fireIn, yaw, pitch, turretAngle, sweepAngle, sweepSpeed, roofY;
    float wagonLength = 5f;
    Material beamWarn, beamHot, glowMat, lidMat;

    Canvas board;
    Text heartsText, hint, message, crosshair;
    Slanted bossFill;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        bossSound = GetComponent<EyeBossAudio>() ?? gameObject.AddComponent<EyeBossAudio>();
        move = InputSystem.actions.FindAction("Player/Move", true);
        look = InputSystem.actions.FindAction("Player/Look", true);
        clips = Resources.Load<BrawlClips>("Brawl/BrawlClips");
        view = new GameObject("EyeBossCamera").AddComponent<Camera>();
        view.fieldOfView = 60f;
        view.nearClipPlane = 0.2f;
        view.farClipPlane = 3000f;
        view.depth = 10f;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = Color.black;
        var hd = view.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        hd.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = Color.black;
        hd.customRenderingSettings = true; // no motion blur: time outside stands still and HDRP's history stalls with it
        hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur] = true;
        hd.renderingPathCustomFrameSettings.SetEnabled(UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur, false);
        view.enabled = false;

        board = UiKit.NewCanvas("EyeBoss", 6);
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.62f);
        Slanted top = UiKit.Panel(board.transform, new Vector2(0f, -50f), new Vector2(640f, 26f), glass, glass, 0.1f);
        UiKit.Persona(top);
        top.rectTransform.anchorMin = top.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        bossFill = UiKit.Panel(top.transform, new Vector2(-310f, 0f), new Vector2(620f, 16f), new Color(0.75f, 0.3f, 1f), new Color(0.75f, 0.3f, 1f), 0f);
        bossFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        Slanted left = UiKit.Panel(board.transform, new Vector2(150f, -60f), new Vector2(260f, 60f), glass, glass, 0.12f);
        UiKit.Persona(left);
        left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0f, 1f);
        heartsText = UiKit.Label(left.transform, UiKit.Display, 34, UiKit.Rose, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(240f, 50f), true);
        Slanted help = UiKit.Panel(board.transform, new Vector2(0f, 50f), new Vector2(900f, 40f), glass, glass, 0.12f);
        UiKit.Persona(help);
        help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint = UiKit.Label(help.transform, UiKit.Body, 20, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(880f, 34f), true);
        message = UiKit.Label(board.transform, UiKit.Display, 40, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(1200f, 60f), true);
        crosshair = UiKit.Label(board.transform, UiKit.Display, 46, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80f, 80f), true);
        crosshair.text = "+";
        board.gameObject.SetActive(false);
    }

    // ---------------- the arena ----------------

    void Build()
    {
        if (arena) Destroy(arena.gameObject);
        arena = new GameObject("EyeBossArena").transform;
        arena.position = Arena;
        eyes.Clear();
        beams.Clear();
        cars.Clear();
        rocks.Clear();
        rockMat = TrainSim.Mat(new Color(3f, 1.1f, 0.25f), false);
        beamWarn = TrainSim.Mat(new Color(2.6f, 0.2f, 0.25f), false);
        beamHot = TrainSim.Mat(new Color(3f, 1.6f, 3f), false);
        glowMat = TrainSim.Mat(new Color(1f, 2.6f, 2.8f), false);
        lidMat = TrainSim.Mat(new Color(0.42f, 0.14f, 0.58f));

        // The ring: wagons nose to tail round a circle, their roofs the path.
        GameObject wagon = Resources.Load<GameObject>("Models/Wagon");
        Bounds box = Bounds(wagon);
        wagonLength = Mathf.Max(box.size.x, box.size.z);
        roofY = box.max.y;
        // Nose to tail, and two gaps a little wider, opposite each other: arc lengths round the circle.
        carAngles.Clear();
        float round = 2f * Mathf.PI * ringRadius;
        int count = Mathf.Max(10, Mathf.FloorToInt((round - 2f * gapWidth) / (wagonLength + 0.6f)));
        float joint = (round - 2f * gapWidth - count * wagonLength) / count, along = wagonLength * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float a = along / ringRadius;
            carAngles.Add(a);
            along += wagonLength + joint;
            if (i == count / 2 - 1 || i == count - 1) along += gapWidth; // a gap after this one
            Transform car = Instantiate(wagon, arena).transform;
            car.position = Arena + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ringRadius;
            car.rotation = Quaternion.Euler(0f, 90f - a * Mathf.Rad2Deg, 0f); // its length along the ring
            foreach (Renderer part in car.GetComponentsInChildren<Renderer>()) TrainSim.Toon(part);
            // a light along the top of each, alternating, for the path to read in the dark
            Block(car, Vector3.up * (roofY + 0.03f), new Vector3(wagonLength * 0.8f, 0.05f, 0.25f), i % 2 == 0 ? glowMat : TrainSim.Mat(new Color(2.8f, 2.2f, 0.4f), false));
            cars.Add(car);
        }

        // The boss: a purple ball covered in eyes, each with a lid that shuts when it is shot enough.
        boss = new GameObject("EyeBall").transform;
        boss.SetParent(arena, false);
        boss.localPosition = Vector3.up * BossHeight;
        Ball(boss, Vector3.zero, BossRadius, TrainSim.Mat(new Color(0.5f, 0.16f, 0.66f)));
        Material white = TrainSim.Mat(new Color(0.97f, 0.96f, 0.94f)), iris = TrainSim.Mat(new Color(0.3f, 1.6f, 0.6f), false), pupil = TrainSim.Mat(new Color(0.04f, 0.03f, 0.05f));
        for (int k = 0; k < eyeCount; k++)
        {
            // spread evenly over the ball (a Fibonacci sphere)
            float y = 1f - 2f * (k + 0.5f) / eyeCount, r = Mathf.Sqrt(1f - y * y), phi = k * 2.39996f;
            Vector3 n = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
            var e = new Eye { normal = n, hits = eyeHits };
            e.root = new GameObject("Eye").transform;
            e.root.SetParent(boss, false);
            e.root.localPosition = n * (BossRadius - 0.25f);
            e.root.localRotation = Quaternion.LookRotation(n);
            float size = Random.Range(0.75f, 1.25f);
            e.root.localScale = Vector3.one * size;
            Ball(e.root, Vector3.zero, EyeRadius, white).localScale = new Vector3(EyeRadius, EyeRadius, EyeRadius * 0.7f) * 2f;
            e.iris = Ball(e.root, new Vector3(0f, 0f, EyeRadius * 0.55f), EyeRadius * 0.55f, iris);
            Ball(e.iris, new Vector3(0f, 0f, 0.4f), 0.32f, pupil);
            e.lid = Ball(e.root, Vector3.zero, EyeRadius * 1.08f, lidMat);
            e.lid.gameObject.SetActive(false);
            eyes.Add(e);
        }

        // The hero, the turret and the sweeping beam.
        heroRoot = new GameObject("RingHero").transform;
        heroRoot.SetParent(arena, false);
        string who = "Characters/" + Locomotives.Hero + "/" + Locomotives.Hero;
        Animator body = BrawlEvent.Body(Resources.Load<GameObject>(who), heroRoot, 1.6f);
        Player.Skin(body.transform, who);
        hero?.Free();
        hero = new BrawlEvent.Rig(heroRoot, body);
        heroParts = heroRoot.GetComponentsInChildren<Renderer>();
        turret = Instantiate(Resources.Load<GameObject>("Models/StationGun"), arena).transform;
        foreach (Renderer part in turret.GetComponentsInChildren<Renderer>()) TrainSim.Toon(part);
        turret.localScale = Vector3.one * 0.8f;
        turret.gameObject.SetActive(false);
        marker = new GameObject("TurretMarker").transform; // a pillar of light over it, to see it from across the ring
        marker.SetParent(arena, false);
        Block(marker, Vector3.up * 12f, new Vector3(0.6f, 24f, 0.6f), TrainSim.Mat(new Color(2.8f, 2.2f, 0.3f), false));
        marker.gameObject.SetActive(false);
        // In the seat: the turret's twin barrels in front of the camera.
        if (barrels) Destroy(barrels.gameObject);
        barrels = new GameObject("Barrels").transform;
        barrels.SetParent(view.transform, false);
        Material steel = TrainSim.Mat(new Color(0.55f, 0.58f, 0.66f));
        for (int s = -1; s <= 1; s += 2)
        {
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(barrel.GetComponent<Collider>());
            barrel.transform.SetParent(barrels, false);
            barrel.transform.localPosition = new Vector3(0.6f * s, -0.7f, 1.6f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);
            barrel.GetComponent<Renderer>().sharedMaterial = steel;
        }
        barrels.gameObject.SetActive(false);
        sweep = new GameObject("Sweep").transform;
        sweep.SetParent(arena, false);
        Block(sweep, new Vector3(0f, roofY + 0.7f, ringRadius * 0.5f + 3f), new Vector3(1.1f, 1.1f, ringRadius + 8f), beamHot);
        sweep.gameObject.SetActive(false);
        sweep2 = new GameObject("Sweep2").transform;
        sweep2.SetParent(arena, false);
        Block(sweep2, new Vector3(0f, roofY + 0.7f, ringRadius * 0.5f + 3f), new Vector3(1.1f, 1.1f, ringRadius + 8f), TrainSim.Mat(new Color(0.4f, 2.6f, 2.8f), false));
        sweep2.gameObject.SetActive(false);
        scorchMat = TrainSim.Mat(new Color(3f, 0.9f, 1.6f), false);
        waveMat = TrainSim.Mat(new Color(3f, 1.8f, 0.3f), false);
        orbMat = TrainSim.Mat(new Color(1.2f, 3f, 0.8f), false);
        hazards.Clear();
        orbs.Clear();

        // Space round it: the painted sky far off, and a few planets hanging below and around.
        var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sky.GetComponent<Collider>());
        sky.transform.SetParent(arena, false);
        sky.transform.localScale = Vector3.one * 2400f;
        Material skyPaint = TrainSim.Mat(Color.white, false);
        skyPaint.SetInt("_Culling", 0);
        skyPaint.mainTexture = Resources.Load<Texture2D>("Sky/space");
        sky.GetComponent<Renderer>().sharedMaterial = skyPaint;
        sky.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var planets = Resources.LoadAll<GameObject>("Scenery/space");
        for (int k = 0; k < 7 && planets.Length > 0; k++)
        {
            float a = k * 0.9f + Random.value;
            Transform pl = Instantiate(planets[Random.Range(0, planets.Length)], arena).transform;
            pl.position = Arena + new Vector3(Mathf.Cos(a) * Random.Range(160f, 320f), Random.Range(-120f, 60f), Mathf.Sin(a) * Random.Range(160f, 320f));
            pl.localScale = Vector3.one * Random.Range(2.5f, 5f);
        }
        arena.gameObject.SetActive(false);
    }

    Transform Ball(Transform parent, Vector3 at, float radius, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    void Block(Vector3 at, Quaternion turn, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(arena, false);
        go.transform.SetPositionAndRotation(at, turn);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    void Block(Transform parent, Vector3 at, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Bounds Bounds(GameObject kind)
    {
        Bounds box = default;
        bool any = false;
        foreach (Renderer part in kind.GetComponentsInChildren<Renderer>())
        {
            if (any) box.Encapsulate(part.bounds); else box = part.bounds;
            any = true;
        }
        return box;
    }

    // ---------------- start and end ----------------

    // done(true): every eye shut.
    public void Begin(System.Action<bool> done)
    {
        bossSound.Stop();
        nextStep = 0f;
        this.done = done;
        Build();
        left = hearts * 2;
        fallUntil = -1f;
        blinkUntil = 0f;
        fleet.Clear();
        nextPickup = Time.unscaledTime + heartEvery;
        heartGiven = false;
        angle = carAngles[carAngles.Count * 3 / 4]; // on a wagon, away from the gaps
        height = vertical = 0f;
        attack = 0;
        hurtUntil = -1f;
        Active = true;
        arena.gameObject.SetActive(true);
        view.enabled = true;
        board.gameObject.SetActive(true);
        Time.timeScale = 0f;
        Enter(Phase.Intro, 2.5f);
        message.text = L10n.T("eyeboss.intro");
    }

    // Over: it stays on screen (for the last words, or the question whether to try again) until Close.
    public void TestWin() { if (Active) End(true); }

    void End(bool won)
    {
        bossSound.Stop();
        Active = false;
        board.gameObject.SetActive(false);
        Time.timeScale = 1f;
        System.Action<bool> finished = done;
        done = null;
        finished?.Invoke(won);
    }

    public void Close()
    {
        bossSound.Stop();
        view.enabled = false;
        hero?.Free();
        hero = null;
        if (arena) Destroy(arena.gameObject);
    }

    void OnDestroy()
    {
        hero?.Free();
        if (arena) Destroy(arena.gameObject);
    }

    void OnDisable() { if (bossSound) bossSound.Stop(); }

    void Enter(Phase next, float seconds)
    {
        phase = next;
        charging = false;
        bossSound.Phase(next == Phase.Attack && Kind == 1);
        phaseUntil = Time.unscaledTime + seconds;
        foreach (Beam b in beams) if (b.line) Destroy(b.line.gameObject);
        beams.Clear();
        foreach (Rock r in rocks) { if (r.ball) Destroy(r.ball.gameObject); if (r.warn) Destroy(r.warn.gameObject); }
        rocks.Clear();
        foreach (Hazard h in hazards) if (h.body) Destroy(h.body.gameObject);
        hazards.Clear();
        foreach (Orb o in orbs) if (o.body) Destroy(o.body.gameObject);
        orbs.Clear();
        if (sweep2) sweep2.gameObject.SetActive(false);
        sweep.gameObject.SetActive(false);
        crosshair.gameObject.SetActive(next == Phase.Gunner);
        heroRoot.gameObject.SetActive(next != Phase.Gunner);
        barrels.gameObject.SetActive(next == Phase.Gunner);
        turret.gameObject.SetActive(next == Phase.Turret); // in the seat it is not seen; blasted, it is gone
        marker.gameObject.SetActive(next == Phase.Turret);
        switch (next)
        {
            case Phase.Intro:
                bossSound.Cue("eye_intro");
                break;
            case Phase.Won:
                bossSound.Cue("race_win", .6f);
                break;
            case Phase.Lost:
                bossSound.Cue("race_lose", .6f);
                break;
            case Phase.Attack:
                foreach (Transform car in cars) car.gameObject.SetActive(true); // the ring whole again
                survivor = (CarAt(angle) + cars.Count / 2 + Random.Range(-2, 3) + cars.Count) % cars.Count; // across from her
                nextRock = Time.unscaledTime + 1.2f;
                message.text = L10n.T(Kind == 0 ? "eyeboss.beams" : Kind == 1 ? "eyeboss.sweep" : "eyeboss.wreck");
                hint.text = L10n.T("eyeboss.hint.ring");
                nextBeam = Time.unscaledTime + 1f;
                sweepAngle = angle * Mathf.Rad2Deg + 180f;
                sweepAngle2 = sweepAngle + 180f;
                sweepSpeed = 65f + attack * 10f;
                sweepWay = Random.value < 0.5f ? 1f : -1f;
                nextFlip = Time.unscaledTime + Random.Range(3f, 5f);
                flipAt = -1f;
                break;
            case Phase.Turret:
                bossSound.Cue("eye_turret");
                // up on the wagon across the ring from her, or on the last one standing
                turretAngle = Kind == 2 ? CarAngle(survivor) : angle + Mathf.PI * Random.Range(0.6f, 1.4f);
                Vector3 at = RingPoint(turretAngle);
                turret.position = at + Vector3.up * roofY;
                turret.rotation = Quaternion.LookRotation(new Vector3(at.x - boss.position.x, 0f, at.z - boss.position.z)); // its barrels are its -z: they point at the ball
                marker.position = turret.position;
                message.text = L10n.T("eyeboss.turret");
                hint.text = L10n.T("eyeboss.hint.turret");
                break;
            case Phase.Gunner:
                bossSound.Cue("eye_turret", .5f);
                yaw = pitch = 0f;
                message.text = "";
                nextOrb = Time.unscaledTime + 1.5f;
                hint.text = L10n.T("eyeboss.hint.gunner");
                fireIn = 0f;
                break;
            case Phase.Blasted:
                bossSound.Cue("eye_blast");
                message.text = L10n.T("eyeboss.blasted");
                Effect(turret.position + Vector3.up * 1.5f);
                angle = turretAngle - 0.12f; // knocked off it, beside where it stood
                foreach (Transform car in cars) car.gameObject.SetActive(true); // and the ring is whole again
                break;
        }
    }

    // ---------------- the fight ----------------

    void Update()
    {
        if (!Active && Keyboard.current != null && (Keyboard.current.uKey.wasPressedThisFrame || (Keyboard.current.nKey.wasPressedThisFrame && (Story.Active ? Story.Current.eyeBoss : train.BiomeId == "space"))) && !train.Paused && !train.AtStation && !train.InEvent && !train.BossFight && !train.Racing)
            Begin(won => { if (Story.Active && Story.Current.eyeBoss) train.EyeBossFinished(won); else { Close(); train.Toast(L10n.T(won ? "eyeboss.won" : "eyeboss.lost.short")); } }); // testing aid: U starts it
        if (Active) bossSound.Pause(train.Paused);
        if (!Active || train.Paused) return;
        Time.timeScale = 0f;
        float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        // The ball bobs and turns, its eyes on her (or on the turret's seat).
        boss.localPosition = Vector3.up * (BossHeight + Mathf.Sin(now * 1.3f) * 0.6f);
        boss.Rotate(0f, (phase == Phase.Gunner ? 14f : 22f) * dt, 0f, Space.World);
        Vector3 target = phase == Phase.Gunner ? view.transform.position : heroRoot.position + Vector3.up;
        foreach (Eye e in eyes)
        {
            Vector3 local = e.root.InverseTransformDirection((target - e.root.position).normalized);
            e.iris.localPosition = new Vector3(Mathf.Clamp(local.x, -1f, 1f) * 0.35f, Mathf.Clamp(local.y, -1f, 1f) * 0.35f, EyeRadius * 0.55f);
        }
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            if (!flashes[i].t || now > flashes[i].until) { if (flashes[i].t) Destroy(flashes[i].t.gameObject); flashes.RemoveAt(i); continue; }
            flashes[i].t.localScale *= 1f + 3f * dt;
        }

        switch (phase)
        {
            case Phase.Intro:
                RunHero(dt, now, false);
                if (now >= phaseUntil) Enter(Phase.Attack, attackSeconds);
                break;
            case Phase.Attack:
                RunHero(dt, now, true);
                if (Kind == 0) Beams(now); else if (Kind == 1) Sweep(dt, now); else Wreck(now);
                if (Kind == 2 ? Standing() == 1 && rocks.Count == 0 : now >= phaseUntil) Enter(Phase.Turret, turretWait);
                break;
            case Phase.Turret:
                RunHero(dt, now, true);
                Beams(now, 2.2f); // a few, slower, while she runs for it
                if (Near(turretAngle) && Interact()) Enter(Phase.Gunner, gunnerSeconds + chargeSeconds);
                else if (now >= phaseUntil) { attack++; Enter(Phase.Attack, attackSeconds); }
                break;
            case Phase.Gunner:
                Gunner(dt, now);
                break;
            case Phase.Blasted:
                RunHero(dt, now, false);
                if (now >= phaseUntil) { attack++; Enter(Phase.Attack, attackSeconds); }
                break;
            case Phase.Won:
                Blowing(dt, now);
                if (now >= phaseUntil) End(true);
                break;
            case Phase.Lost:
                if (now >= phaseUntil) End(false);
                break;
        }
        if (phase != Phase.Gunner && phase != Phase.Won && phase != Phase.Lost) { Ships(dt, now); Pickup(dt, now); Hazards(now); }
        UpdateCamera(dt);
        int open = OpenEyes;
        bossFill.rectTransform.sizeDelta = new Vector2(620f * open / Mathf.Max(1, eyes.Count * 1f), 16f);
        // whole hearts, then a half one (paler), then the ones lost
        heartsText.text = new string('♥', Mathf.Max(0, left / 2)) + (left % 2 == 1 ? "<color=#ffb0bc80>♥</color>" : "") + "<color=#ffffff30>" + new string('♥', Mathf.Max(0, hearts - (left + 1) / 2)) + "</color>";
        heartsText.supportRichText = true;
        if (phase != Phase.Won && open == 0) { message.text = L10n.T("eyeboss.won"); Enter(Phase.Won, 4f); wonAt = now; }
        if (phase != Phase.Lost && phase != Phase.Won && left <= 0) { message.text = L10n.T("eyeboss.lost"); Enter(Phase.Lost, 3f); }
    }

    // Beaten: it shakes and swells while bursts break out all over it, then goes up in one great blast.
    float wonAt, nextBurst;
    void Blowing(float dt, float now)
    {
        float t = now - wonAt;
        RunHero(dt, now, false);
        if (t < 2.6f)
        {
            boss.localPosition += Random.insideUnitSphere * 0.35f;
            boss.localScale = Vector3.one * (1f + t * 0.12f);
            if (now >= nextBurst)
            {
                nextBurst = now + Mathf.Lerp(0.16f, 0.05f, t / 2.6f);
                Effect(boss.position + Random.onUnitSphere * BossRadius * boss.localScale.x, Random.Range(1.6f, 3f), 0.45f);
                shake = Mathf.Max(shake, 0.15f);
                if (Random.value < 0.3f) Sound.Play("vagonroto");
            }
        }
        else if (boss.gameObject.activeSelf)
        {
            boss.gameObject.SetActive(false);
            Effect(boss.position, 9f, 0.9f);
            Effect(boss.position, 5f, 1.3f);
            for (int k = 0; k < 10; k++) Effect(boss.position + Random.onUnitSphere * BossRadius, 3f, 1f);
            shake = 1f;
            Sound.Play("vagonroto");
        }
    }

    int OpenEyes { get { int n = 0; foreach (Eye e in eyes) if (!e.Shut) n++; return n; } }
    int Kind => attack % 3; // 0 beams, 1 the sweep, 2 the rocks
    // The wagon nearest that angle round the ring.
    int CarAt(float a)
    {
        int best = 0;
        float bestGap = float.MaxValue;
        for (int i = 0; i < carAngles.Count; i++)
        {
            float gap = Mathf.Abs(Mathf.DeltaAngle(carAngles[i] * Mathf.Rad2Deg, a * Mathf.Rad2Deg));
            if (gap < bestGap) { bestGap = gap; best = i; }
        }
        return best;
    }
    float CarAngle(int i) => carAngles[i];
    // Something on the ring at that angle has a wagon under it (the joints between wagons are stepped across).
    bool Solid(float a)
    {
        int i = CarAt(a);
        if (!cars[i].gameObject.activeSelf) return false;
        return Mathf.Abs(Mathf.DeltaAngle(carAngles[i] * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) * Mathf.Deg2Rad * ringRadius < wagonLength * 0.5f + 0.5f;
    }
    int Standing() { int n = 0; foreach (Transform c in cars) if (c.gameObject.activeSelf) n++; return n; }

    // The third attack: a great burning rock at one wagon after another (its roof marked red first), smashing it;
    // all but one. On a wagon as it goes, she is hurt and lands on the nearest still there.
    void Wreck(float now)
    {
        if (now >= nextRock && rocks.Count < 3)
        {
            nextRock = now + Mathf.Lerp(0.75f, 0.45f, attack / 6f);
            var left = new List<int>();
            for (int i = 0; i < cars.Count; i++) if (i != survivor && cars[i].gameObject.activeSelf && !rocks.Exists(r => r.car == i)) left.Add(i);
            if (left.Count > 0)
            {
                // the nearer ones to her first, more often than not, so the ground goes from under her
                left.Sort((x, y) => Mathf.Abs(Mathf.DeltaAngle(CarAngle(x) * Mathf.Rad2Deg, angle * Mathf.Rad2Deg)).CompareTo(Mathf.Abs(Mathf.DeltaAngle(CarAngle(y) * Mathf.Rad2Deg, angle * Mathf.Rad2Deg))));
                int pick = left[Random.value < 0.6f ? Random.Range(0, Mathf.Min(3, left.Count)) : Random.Range(0, left.Count)];
                var rock = new Rock { car = pick, from = boss.position, from0 = now, land = now + 1.4f };
                rock.ball = Ball(arena, Vector3.zero, 1.6f, rockMat);
                rock.ball.position = rock.from;
                rock.warn = new GameObject("RockMark").transform;
                rock.warn.SetParent(cars[pick], false);
                Block(rock.warn, Vector3.up * (roofY + 0.06f), new Vector3(wagonLength * 0.95f, 0.04f, 2.6f), beamWarn);
                rocks.Add(rock);
                bossSound.Cue("ascent_pass_2", .4f, .3f);
            }
        }
        for (int i = rocks.Count - 1; i >= 0; i--)
        {
            Rock rock = rocks[i];
            Transform car = cars[rock.car];
            float t = Mathf.InverseLerp(rock.from0, rock.land, now);
            Vector3 to = car.position + Vector3.up * roofY;
            rock.ball.position = Vector3.Lerp(rock.from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 6f;
            rock.ball.localScale = Vector3.one * (3.2f + t * 1.5f);
            rock.warn.gameObject.SetActive((int)(now * 8f) % 2 == 0);
            if (t < 1f) continue;
            // the hit: the wagon smashed, and her with it if she was on it
            Effect(to);
            Effect(to + Vector3.up * 1.5f);
            Sound.Play("vagonroto");
            shake = 0.35f;
            Wave(CarAngle(rock.car), now);
            Destroy(rock.ball.gameObject);
            Destroy(rock.warn.gameObject);
            car.gameObject.SetActive(false);
            rocks.RemoveAt(i);
        }
    }

    // Off the end of a wagon that is gone, or into one of the gaps: hurt, and back up on the nearest one still there.
    void Ground(float now)
    {
        if (fallUntil > 0f || height > 0.15f || cars.Count == 0 || Solid(angle)) return;
        hurtUntil = 0f;
        Hurt(now, 2); // a fall: a whole heart
        fallUntil = now + 1f; // seen dropping away below the ring first (RunHero), then back up
        vertical = 0f;
        hurtUntil = fallUntil + 2f;
        Sound.Play("vagonroto");
    }

    // Back up after a fall: on the nearest wagon still there, blinking yellow a moment.
    void Land(float now)
    {
        fallUntil = -1f;
        int best = -1;
        float bestGap = float.MaxValue;
        for (int i = 0; i < cars.Count; i++)
        {
            if (!cars[i].gameObject.activeSelf) continue;
            float gap = Mathf.Abs(Mathf.DeltaAngle(CarAngle(i) * Mathf.Rad2Deg, angle * Mathf.Rad2Deg));
            if (gap < bestGap) { bestGap = gap; best = i; }
        }
        if (best >= 0) angle = CarAngle(best);
        height = 0f;
        vertical = jumpSpeed * 0.5f; // a hop up onto it
        blinkUntil = now + 2f;
    }

    Vector3 RingPoint(float a) => Arena + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ringRadius;

    bool Near(float a) => Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) * Mathf.Deg2Rad * ringRadius < 3f;

    static bool Interact() => (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

    // The little ships: a few drifting round the ring at her height, slow, each its own way; touch one and it takes
    // half a heart. Jumped, they go by underneath.
    void Ships(float dt, float now)
    {
        if (fleet.Count == 0)
        {
            Material hull = TrainSim.Mat(new Color(0.85f, 0.88f, 0.95f)), dome = TrainSim.Mat(new Color(0.4f, 2.2f, 2.6f), false), light = TrainSim.Mat(new Color(2.8f, 0.6f, 0.4f), false);
            for (int k = 0; k < ships; k++)
            {
                var body = new GameObject("Ship").transform;
                body.SetParent(arena, false);
                Ball(body, Vector3.zero, 0.7f, hull).localScale = new Vector3(1.4f, 0.45f, 1.4f);
                Ball(body, Vector3.up * 0.2f, 0.32f, dome);
                for (int l = 0; l < 4; l++) Ball(body, Quaternion.Euler(0f, l * 90f, 0f) * Vector3.forward * 0.68f, 0.1f, light);
                fleet.Add((body, angle + Mathf.PI * (0.5f + k * 0.5f), k % 2 == 0 ? 1f : -1f));
            }
        }
        for (int k = 0; k < fleet.Count; k++)
        {
            var f = fleet[k];
            float a = f.angle + f.way * shipSpeed / ringRadius * dt;
            fleet[k] = (f.body, a, f.way);
            f.body.position = RingPoint(a) + Vector3.up * (roofY + 0.55f + Mathf.Sin(now * 3f + k) * 0.08f);
            f.body.Rotate(0f, 160f * dt, 0f, Space.Self);
            float oldGap = Mathf.DeltaAngle(f.angle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg);
            float newGap = Mathf.DeltaAngle(a * Mathf.Rad2Deg, angle * Mathf.Rad2Deg);
            if (oldGap * newGap <= 0f && Mathf.Abs(newGap) < 10f) bossSound.Cue("ascent_pass_1", .35f, .35f);
            if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad * ringRadius < 1.1f && height < 0.9f) Hurt(now);
        }
    }

    // Now and then, at random, a heart: it walks round the ring and drops through the next gap unless she gets it.
    void Pickup(float dt, float now)
    {
        if (pickup == null && now >= nextPickup)
        {
            nextPickup = now + heartEvery * Random.Range(0.7f, 1.4f);
            if (!heartGiven && attack >= 2 && left < hearts * 2 && Random.value < heartChance)
            {
                heartGiven = true;
                pickup = new GameObject("Heart").transform;
                pickup.SetParent(arena, false);
                Material red = TrainSim.Mat(new Color(3f, 0.35f, 0.5f), false);
                Ball(pickup, new Vector3(-0.22f, 0.15f, 0f), 0.3f, red);
                Ball(pickup, new Vector3(0.22f, 0.15f, 0f), 0.3f, red);
                Block(pickup, new Vector3(0f, -0.1f, 0f), new Vector3(0.42f, 0.42f, 0.3f), red);
                pickup.GetChild(2).localRotation = Quaternion.Euler(0f, 0f, 45f);
                pickupAngle = angle + Mathf.PI * Random.Range(0.4f, 0.8f) * (Random.value < 0.5f ? 1f : -1f);
                pickupWay = Random.value < 0.5f ? 1f : -1f;
                pickupFall = -1f;
            }
        }
        if (pickup == null) return;
        if (pickupFall < 0f)
        {
            pickupAngle += pickupWay * heartSpeed / ringRadius * dt;
            pickup.position = RingPoint(pickupAngle) + Vector3.up * (roofY + 0.9f + Mathf.Abs(Mathf.Sin(now * 5f)) * 0.3f); // hopping along
            pickup.rotation = Quaternion.LookRotation(pickup.position - view.transform.position);
            if (!Solid(pickupAngle)) pickupFall = now; // over a gap: down it goes
            else if (Mathf.Abs(Mathf.DeltaAngle(pickupAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad * ringRadius < 1.3f && height < 2f)
            {
                left = Mathf.Min(hearts * 2, left + 2);
                Effect(pickup.position);
                Sound.Play("healthup");
                Destroy(pickup.gameObject);
                pickup = null;
            }
        }
        else
        {
            pickup.position += Vector3.down * (now - pickupFall) * 18f * dt;
            if (now - pickupFall > 1.5f) { Destroy(pickup.gameObject); pickup = null; }
        }
    }

    // Round the ring: left and right along it, a jump; the camera sees it from outside, behind her.
    void RunHero(float dt, float now, bool control)
    {
        bool falling = now < fallUntil;
        if (!falling && fallUntil > 0f) Land(now);
        float x = control && !falling ? move.ReadValue<Vector2>().x : 0f;
        angle += x * heroSpeed / ringRadius * dt; // increasing the angle follows screen-right from the outside camera
        if (Mathf.Abs(x) > 0.2f) facing = Mathf.Sign(x);
        spin = Mathf.Lerp(spin, (angle - prevAngle) / Mathf.Max(dt, 0.001f), 0.15f); // round the ring, radians a second
        prevAngle = angle;
        if (falling)
        {
            // down through the gap, out of sight below the ring
            vertical -= gravity * dt;
            height += vertical * dt;
        }
        else
        {
            bool jump = control && height <= 0f && ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame));
            if (jump) { vertical = jumpSpeed; bossSound.Cue("eye_jump", .7f); }
            float previousHeight = height;
            vertical -= gravity * dt;
            height = Mathf.Max(0f, height + vertical * dt);
            if (height <= 0f) vertical = Mathf.Max(0f, vertical);
            if (height <= 0f && Solid(angle))
            {
                if (previousHeight > 0f) bossSound.Cue("eye_land", .7f);
                else if (Mathf.Abs(x) > .2f && now >= nextStep)
                {
                    nextStep = now + .3f;
                    bossSound.Cue("eye_land", .25f);
                }
            }
        }
        Vector3 at = RingPoint(angle);
        Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * facing;
        heroRoot.SetPositionAndRotation(at + Vector3.up * (roofY + height), Quaternion.LookRotation(tangent));
        Ground(now);
        int clipNow = height > 0.05f ? 2 : Mathf.Abs(x) > 0.2f ? 1 : 0;
        if (hero != null && clips)
        {
            if (clipNow != lastHeroClip) hero.Play(clipNow == 2 ? clips.dodge : clipNow == 1 ? clips.run : clips.idle, false);
            lastHeroClip = clipNow;
            hero.Tick(dt);
        }
        // Hurt: blinking out; back up after a fall: blinking yellow.
        bool flash = (int)(now * 10f) % 2 == 0;
        bool back = now < blinkUntil;
        heroRoot.gameObject.SetActive(back || falling || !(now < hurtUntil && flash));
        if (yellow == null) { yellow = new MaterialPropertyBlock(); yellow.SetColor("_MainColor", new Color(2.4f, 1.9f, 0.2f)); }
        if (heroParts != null) foreach (Renderer part in heroParts) if (part) part.SetPropertyBlock(back && flash ? yellow : null);
    }

    void Hurt(float now, int halves = 1)
    {
        if (now < hurtUntil) return;
        left -= halves;
        hurtUntil = now + 1.6f;
        Sound.Play("wagonbad");
    }

    // The first attack: one eye after another marks where she is (a thin red line), then fires down it.
    void Beams(float now, float every = 0.9f)
    {
        if (now >= nextBeam)
        {
            nextBeam = now + every * Mathf.Lerp(1f, 0.6f, attack / 4f);
            // Where she is, where she will be if she keeps running (so running one way for ever is no escape), and
            // now and then one each side of her: a cage she has to jump or turn back out of.
            // And a march: five in a row coming at her from ahead, one after another, to be jumped or run back from.
            int pattern = beamCount++ % 4;
            float lead = spin * Warn * Random.Range(0.8f, 1.3f), way = spin >= 0f ? 1f : -1f, side = 4f / ringRadius * way;
            if (pattern == 0) FireBeam(angle, now, 0f);
            else if (pattern == 1) FireBeam(angle + lead + side * 0.3f, now, 0f);
            else if (pattern == 2) { FireBeam(angle + lead + side, now, 0f); FireBeam(angle - side, now, 0f); }
            else for (int k = 0; k < 5; k++) FireBeam(angle + way * (12f - k * 3f) / ringRadius, now, k * 0.2f);
            if (pattern == 3) nextBeam += 1.2f; // room to breathe after the march
        }
        for (int i = beams.Count - 1; i >= 0; i--)
        {
            Beam b = beams[i];
            if (now > b.until) { Destroy(b.line.gameObject); beams.RemoveAt(i); continue; }
            bool hot = now >= b.fireAt;
            Vector3 mid = (b.from + b.to) * 0.5f;
            b.line.SetPositionAndRotation(mid, Quaternion.LookRotation(b.to - b.from));
            b.line.localScale = new Vector3(hot ? 1.2f : 0.08f, hot ? 1.2f : 0.08f, (b.to - b.from).magnitude);
            b.line.GetComponentInChildren<Renderer>().sharedMaterial = hot ? beamHot : beamWarn;
            if (hot && !b.hit)
            {
                b.hit = true;
                bossSound.Cue("eye_beam", .65f, .08f);
                Effect(b.to);
                Vector3 feet = heroRoot.position;
                if ((new Vector3(feet.x, 0f, feet.z) - new Vector3(b.to.x, 0f, b.to.z)).magnitude < 2f && height < 1.5f) Hurt(now);
                Scorch(b.to, now);
            }
        }
    }

    void FireBeam(float at, float now, float delay)
    {
        Eye from = null;
        for (int tries = 0; tries < 6 && (from == null || from.Shut); tries++) from = eyes[Random.Range(0, eyes.Count)];
        if (from == null || from.Shut) return;
        var b = new Beam { from = from.root.position, to = RingPoint(at) + Vector3.up * roofY, fireAt = now + Warn + delay, until = now + Warn + delay + 0.3f };
        b.line = new GameObject("Beam").transform;
        b.line.SetParent(arena, false);
        Block(b.line, Vector3.zero, Vector3.one, beamWarn);
        beams.Add(b);
        bossSound.Cue("eye_mark", .65f, .2f);
    }

    // The second attack: a beam swept round the ring at knee height; jump it. A second joins from the other side,
    // the two now and then turn back all at once (both flash red first), and they speed up as it goes on.
    void Sweep(float dt, float now)
    {
        sweep.gameObject.SetActive(true);
        bool two = now > phaseUntil - attackSeconds + 3f;
        sweep2.gameObject.SetActive(two);
        if (now >= nextFlip && flipAt < 0f) { flipAt = now + 0.6f; bossSound.Cue("eye_mark", .9f); }
        bool warning = flipAt > 0f && now < flipAt;
        if (flipAt > 0f && now >= flipAt) { sweepWay = -sweepWay; flipAt = -1f; nextFlip = now + Random.Range(2.5f, 4.5f); }
        sweepSpeed += 3f * dt;
        float step = (warning ? 0.25f : 1f) * sweepSpeed * dt * sweepWay;
        sweepAngle += step;
        sweepAngle2 -= step;
        sweep.localRotation = Quaternion.Euler(0f, -sweepAngle + 90f, 0f);
        sweep2.localRotation = Quaternion.Euler(0f, -sweepAngle2 + 90f, 0f);
        sweep.GetComponentInChildren<Renderer>().sharedMaterial = warning && (int)(now * 12f) % 2 == 0 ? beamWarn : beamHot;
        float her = -angle * Mathf.Rad2Deg + 90f, reach = Mathf.Abs(step) + 2.5f;
        if (height < 1.3f && (Mathf.Abs(Mathf.DeltaAngle(sweep.localEulerAngles.y, her)) < reach || (two && Mathf.Abs(Mathf.DeltaAngle(sweep2.localEulerAngles.y, her)) < reach))) Hurt(now);
    }

    // A burning patch on a wagon's roof where a beam struck, for a couple of seconds.
    void Scorch(Vector3 at, float now)
    {
        var h = new Hazard { at = Mathf.Atan2(at.z - Arena.z, at.x - Arena.x), from = now, until = now + 2.2f };
        h.body = Ball(arena, Vector3.zero, 1f, scorchMat);
        h.body.position = new Vector3(at.x, Arena.y + roofY + 0.05f, at.z);
        h.body.localScale = new Vector3(3f, 0.08f, 3f);
        hazards.Add(h);
    }

    // A shock wave from where a rock fell: two low walls of light running round the ring, one each way.
    void Wave(float at, float now)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var h = new Hazard { at = at, way = s, from = now, until = now + 2.6f, wave = true };
            h.body = new GameObject("Wave").transform;
            h.body.SetParent(arena, false);
            Block(h.body, Vector3.zero, new Vector3(3.6f, 0.55f, 0.35f), waveMat);
            hazards.Add(h);
        }
    }

    void Hazards(float now)
    {
        for (int i = hazards.Count - 1; i >= 0; i--)
        {
            Hazard h = hazards[i];
            if (now > h.until || !h.body) { if (h.body) Destroy(h.body.gameObject); hazards.RemoveAt(i); continue; }
            float a = h.at;
            if (h.wave)
            {
                a = h.at + h.way * 10f * (now - h.from) / ringRadius; // ten metres a second round the ring
                h.body.position = RingPoint(a) + Vector3.up * (roofY + 0.3f);
                h.body.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))); // across the ring
                h.body.gameObject.SetActive(Solid(a)); // nothing to run along over a gap
            }
            else h.body.localScale = new Vector3(3f, 0.08f, 3f) * (1f - 0.3f * (now - h.from) / 2.2f);
            float gap = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad * ringRadius;
            if (gap < (h.wave ? 0.9f : 1.3f) && height < (h.wave ? 0.75f : 0.3f) && h.body.gameObject.activeSelf) Hurt(now);
        }
    }

    // At the turret: aim at its eyes and shoot them shut, until it charges and blasts the turret.
    void Gunner(float dt, float now)
    {
        Vector2 stick = move.ReadValue<Vector2>(), mouse = Mouse.current != null ? look.ReadValue<Vector2>() : Vector2.zero;
        yaw = Mathf.Clamp(yaw + stick.x * aimSpeed * dt + mouse.x * mouseAim, -45f, 45f);
        pitch = Mathf.Clamp(pitch - stick.y * aimSpeed * dt - mouse.y * mouseAim, -35f, 30f);
        bool fire = (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.3f) || (Mouse.current != null && Mouse.current.leftButton.isPressed)
                    || (Keyboard.current != null && Keyboard.current.jKey.isPressed);
        bossSound.Firing(fire);
        float charge = phaseUntil - now;
        if (charge < chargeSeconds)
        {
            if (!charging) { charging = true; bossSound.Charge(chargeSeconds); }
            message.text = L10n.T("eyeboss.charge");
            boss.localScale = Vector3.one * (1f + Mathf.PingPong(now * 4f, 0.08f));
        }
        else boss.localScale = Vector3.one;
        // Its eyes blink, each on its own beat: shut for that moment, a shot does nothing to it.
        for (int k = 0; k < eyes.Count; k++)
        {
            Eye e = eyes[k];
            if (e.Shut) continue;
            bool blinking = Mathf.Sin(now * (1.6f + (k % 5) * 0.35f) + k * 1.7f) > 0.72f;
            e.lid.gameObject.SetActive(blinking);
        }
        // It fires orbs at the seat; shot down they burst, let through they take half a heart.
        if (now >= nextOrb && charge > chargeSeconds)
        {
            nextOrb = now + Mathf.Lerp(1.5f, 0.8f, attack / 6f);
            Eye from = eyes[Random.Range(0, eyes.Count)];
            if (!from.Shut)
            {
                var o = new Orb { from = from.root.position, from0 = now, land = now + 1.8f };
                o.body = Ball(arena, Vector3.zero, 0.7f, orbMat);
                o.body.position = o.from;
                orbs.Add(o);
                bossSound.Cue("eye_orb", .7f);
            }
        }
        for (int k = orbs.Count - 1; k >= 0; k--)
        {
            Orb o = orbs[k];
            float u = Mathf.InverseLerp(o.from0, o.land, now);
            o.body.position = Vector3.Lerp(o.from, view.transform.position + view.transform.forward * 1.5f, u * u);
            if (u >= 1f) { bossSound.Cue("eye_hit", .9f); Destroy(o.body.gameObject); orbs.RemoveAt(k); Hurt(now); shake = 0.4f; }
        }
        if (fire && (fireIn -= dt) <= 0f)
        {
            fireIn = 1f / fireRate;
            Ray ray = new Ray(view.transform.position, view.transform.forward);
            for (int k = orbs.Count - 1; k >= 0; k--)
            {
                Orb o = orbs[k];
                float along = Vector3.Dot(o.body.position - ray.origin, ray.direction);
                if (along > 0f && (ray.GetPoint(along) - o.body.position).sqrMagnitude < 1.6f) { bossSound.Cue("eye_hit", .65f); Effect(o.body.position); Destroy(o.body.gameObject); orbs.RemoveAt(k); }
            }
            Eye best = null;
            float bestAt = float.MaxValue;
            foreach (Eye e in eyes)
            {
                if (e.Shut || e.lid.gameObject.activeSelf) continue;
                float r = EyeRadius * e.root.lossyScale.x * 1.15f, at = Vector3.Dot(e.root.position - ray.origin, ray.direction);
                if (at > 0f && at < bestAt && (ray.GetPoint(at) - e.root.position).sqrMagnitude < r * r) { best = e; bestAt = at; }
            }
            Vector3 end = best != null ? ray.GetPoint(bestAt) : ray.GetPoint(Mathf.Max(10f, Vector3.Dot(boss.position - ray.origin, ray.direction)));
            Tracer(view.transform.position + view.transform.right * 0.6f - view.transform.up * 0.5f, end, now);
            if (best != null)
            {
                best.hits--;
                if (best.Shut) { best.lid.gameObject.SetActive(true); best.iris.gameObject.SetActive(false); Effect(best.root.position); bossSound.Cue("eye_shut"); }
                else bossSound.Cue("eye_hit", .7f);
            }
        }
        if (now >= phaseUntil)
        {
            foreach (Eye e in eyes) e.lid.gameObject.SetActive(e.Shut); // the blinking stops with the seat
            Enter(Phase.Blasted, 1.6f);
        }
    }

    void Tracer(Vector3 from, Vector3 to, float now)
    {
        var t = new GameObject("Tracer").transform;
        t.SetParent(arena, false);
        Block(t, Vector3.zero, Vector3.one, beamHot);
        t.SetPositionAndRotation((from + to) * 0.5f, Quaternion.LookRotation(to - from));
        t.localScale = new Vector3(0.12f, 0.12f, (to - from).magnitude);
        flashes.Add((t, now + 0.05f));
    }

    // A burst: a glowing ball that swells and goes.
    void Effect(Vector3 at, float size = 1f, float life = 0.35f)
    {
        Transform ball = Ball(arena, Vector3.zero, size, TrainSim.Mat(new Color(3f, 1.4f, 2.6f), false));
        ball.position = at;
        flashes.Add((ball, Time.unscaledTime + life));
    }

    void UpdateCamera(float dt)
    {
        if (phase == Phase.Gunner)
        {
            // In the turret's seat, the ball ahead.
            Vector3 seat = turret.position + Vector3.up * 2.2f;
            Quaternion ahead = Quaternion.LookRotation(boss.position - seat) * Quaternion.Euler(pitch, yaw, 0f);
            view.transform.SetPositionAndRotation(seat, ahead);
            view.fieldOfView = 55f;
            Shake(dt);
            return;
        }
        // From outside the ring, behind her, the ball beyond her.
        Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        Vector3 want = RingPoint(angle) + outward * 11f + Vector3.up * (roofY + 5.5f);
        view.transform.position = Vector3.Lerp(view.transform.position, want, 1f - Mathf.Exp(-8f * dt));
        view.transform.rotation = Quaternion.Slerp(view.transform.rotation, Quaternion.LookRotation(Vector3.Lerp(heroRoot.position, boss.position, 0.35f) - view.transform.position), 1f - Mathf.Exp(-8f * dt));
        view.fieldOfView = 62f;
        Shake(dt);
    }

    void Shake(float dt)
    {
        if (shake <= 0f) return;
        view.transform.position += Random.insideUnitSphere * shake * 0.5f;
        shake = Mathf.Max(0f, shake - dt);
    }
}

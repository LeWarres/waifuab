using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Before the alien base (Story.Part.gates): the way in is shut by giant gates, each held by glowing padlocks. Seen
// from beside the locomotive as it rolls up to them, the hero shoots the locks off before the train gets there;
// all of a gate's locks gone, it blows open and the next comes into sight. The base's guards fly in at her: shot
// down they burst, let through they hurt her. Three gates and she is in; out of health, or the train reaching a
// gate still shut, and she is asked whether to try again (no: back to the chapter's opening pages). Outside, time
// stands still, like the other set pieces (CaveEvent, RaceEvent).
public partial class GatesEvent : MonoBehaviour
{
    public int gates = 4, locksPerGate = 5, lockHits = 5, health = 100;
    public float gateDistance = 115f, trainSpeed = 6.5f, aimSpeed = 900f, fireRate = 7f;
    public float guardEvery = 1.05f, guardSpeed = 12f, guardDamage = 15f;

    public bool Active { get; private set; }

    static readonly Vector3 Arena = new Vector3(12000f, 0f, 0f);
    enum Step { Rolling, Opening, Lost, Won }
    class Lock { public Transform body, halo; public int hits; public Vector3 home; public float sway, hitAt = -1f; public Renderer casing; public Renderer[] charge; }
    class Guard { public Transform body; public Vector3 from; public float t, speed; }

    TrainSim train;
    InputAction move;
    Camera view;
    AudioSource gunSound;
    Transform arena, engine, gate, door;
    readonly List<Lock> locks = new();
    readonly List<Guard> guards = new();
    readonly List<(Transform t, float until)> flashes = new();
    Step step;
    int gateIndex, life, choice;
    float travelled, gateAt, stepUntil, fireIn, nextGuard, shake;
    Vector2 aim;
    Material lockMat, lockHot, plateMat, steelMat, ringMat, doorMat, guardMat, tracerMat, burstMat;
    Mesh guardMesh;

    Canvas board;
    Text title, lifeText, ask;
    RawImage crosshair; // the scope reticle the gunner's seat uses (SwarmEvent)
    Slanted lifeFill, askPanel;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        gunSound = Sound.Effect(gameObject, "machinegunsound", 0.6f);
        gunSound.loop = true;
        move = InputSystem.actions.FindAction("Player/Move", true);
        view = new GameObject("GatesCamera").AddComponent<Camera>();
        view.fieldOfView = 55f;
        view.nearClipPlane = 0.2f;
        view.farClipPlane = 1500f;
        view.depth = 10f;
        var hd = view.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        hd.customRenderingSettings = true; // no motion blur: time outside stands still and HDRP's history stalls with it
        hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur] = true;
        hd.renderingPathCustomFrameSettings.SetEnabled(UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur, false);
        view.enabled = false;

        board = UiKit.NewCanvas("Gates", 6);
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.7f);
        Slanted top = UiKit.Panel(board.transform, new Vector2(0f, -60f), new Vector2(760f, 56f), glass, glass, 0.12f);
        UiKit.Persona(top);
        top.rectTransform.anchorMin = top.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title = UiKit.Label(top.transform, UiKit.Display, 26, UiKit.Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(740f, 48f), true);
        Slanted left = UiKit.Panel(board.transform, new Vector2(170f, -60f), new Vector2(300f, 56f), glass, glass, 0.12f);
        UiKit.Persona(left);
        left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0f, 1f);
        UiKit.Panel(left.transform, new Vector2(0f, -12f), new Vector2(260f, 14f), new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), 0f);
        lifeFill = UiKit.Panel(left.transform, new Vector2(-130f, -12f), new Vector2(260f, 10f), UiKit.Mint, UiKit.Mint, 0f);
        lifeFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        lifeText = UiKit.Label(left.transform, UiKit.Body, 18, Color.white, TextAnchor.MiddleLeft, new Vector2(10f, 12f), new Vector2(260f, 24f), true);
        crosshair = UiKit.Picture(board.transform, "sight", Vector2.zero, 150f);
        Slanted help = UiKit.Panel(board.transform, new Vector2(0f, 50f), new Vector2(900f, 40f), glass, glass, 0.12f);
        UiKit.Persona(help);
        help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        UiKit.Label(help.transform, UiKit.Body, 19, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(880f, 34f), true).text = L10n.T("gates.hint");
        askPanel = UiKit.Persona(UiKit.Panel(board.transform, new Vector2(0f, 40f), new Vector2(700f, 180f), UiKit.White, UiKit.Ice, 0.06f), 6f, 14f);
        ask = UiKit.Label(askPanel.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(660f, 160f));
        ask.supportRichText = true;
        askPanel.gameObject.SetActive(false);
        board.gameObject.SetActive(false);
    }

    // ---------------- the way in ----------------

    void Build()
    {
        ClearArena();
        arena = new GameObject("Gates").transform;
        arena.position = Arena;
        lockMat = Surface(new Color(1.3f, 0.85f, 0.18f));
        lockHot = Surface(new Color(3f, 3f, 3f), false);
        plateMat = Surface(new Color(0.08f, 0.1f, 0.14f));
        steelMat = Surface(new Color(0.55f, 0.62f, 0.7f));
        ringMat = Surface(new Color(2.3f, 0.65f, 0.15f), false);
        doorMat = Surface(new Color(0.24f, 0.28f, 0.34f));
        guardMat = Surface(Color.white);
        guardMat.SetFloat("_MVCOL", 1f);
        tracerMat = Surface(new Color(0.5f, 2.6f, 3f), false);
        burstMat = Surface(new Color(3f, 1.6f, 0.5f), false);
        guardMesh = Resources.Load<Mesh>("Models/Enemy_warden");
        DressApproach();
        engine = Locomotives.Create(arena, Locomotives.Selected);
        engine.localRotation = Quaternion.Euler(0f, 90f, 0f); // its nose is its -x: down the track
        guards.Clear();
        gateIndex = 0;
        travelled = 0f;
        NewGate();
    }

    // The next gate: two great doors in a frame across the track, padlocks on them.
    void NewGate()
    {
        if (gate) Destroy(gate.gameObject);
        gateAt = travelled + gateDistance;
        gate = new GameObject("Gate").transform;
        gate.SetParent(arena, false);
        gate.localPosition = new Vector3(0f, 0f, gateAt);
        door = new GameObject("Door").transform;
        door.SetParent(gate, false);
        DressGate();
        locks.Clear();
        int count = locksPerGate + gateIndex; // one more each gate
        for (int i = 0; i < count; i++)
        {
            var l = new Lock { hits = lockHits, sway = gateIndex == 0 ? 0f : Random.Range(1.5f, 3f) * (Random.value < 0.5f ? -1f : 1f) };
            l.body = new GameObject("Lock").transform;
            l.body.SetParent(door, false);
            Vector3 at;
            int tries = 0;
            do at = new Vector3(Random.Range(-7.5f, 7.5f), Random.Range(3f, 14.5f), -0.9f);
            while (tries++ < 30 && locks.Exists(o => (o.home - at).magnitude < 3.4f));
            l.home = at;
            l.body.localPosition = at;
            Block(l.body, new Vector3(0f, 0.4f, 0.2f), new Vector3(3f, 3.4f, 0.1f), plateMat);         // dark plate behind it
            l.casing = Block(l.body, Vector3.zero, new Vector3(2f, 1.7f, 0.6f), lockMat).GetComponent<Renderer>();
            for (int s = -1; s <= 1; s += 2) Block(l.body, new Vector3(s * 0.62f, 1.25f, 0f), new Vector3(0.32f, 1.1f, 0.32f), steelMat); // the shackle
            Block(l.body, new Vector3(0f, 1.82f, 0f), new Vector3(1.56f, 0.32f, 0.32f), steelMat);
            Block(l.body, new Vector3(0f, 0.1f, -0.32f), new Vector3(0.3f, 0.55f, 0.05f), plateMat);   // the keyhole
            DressLock(l);
            locks.Add(l);
        }
        title.text = L10n.T("gates.gate", gateIndex + 1, gates);
    }

    Transform Block(Vector3 at, Vector3 size, Material m) => Block(arena, at, size, m);

    Transform Block(Transform parent, Vector3 at, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    // ---------------- start and end ----------------

    System.Action<bool> done;

    // done(true): through all the gates; done(false): given up (no to trying again).
    public void Begin(System.Action<bool> done)
    {
        this.done = done;
        Build();
        life = health;
        Active = true;
        arena.gameObject.SetActive(true);
        view.enabled = true;
        board.gameObject.SetActive(true);
        askPanel.gameObject.SetActive(false);
        crosshair.gameObject.SetActive(true);
        aim = new Vector2(Screen.width / 2f, Screen.height * 0.6f);
        nextGuard = Time.unscaledTime + 2f;
        step = Step.Rolling;
        Time.timeScale = 0f;
    }

    public void TestWin() { if (Active) Finish(true); }

    void Finish(bool won)
    {
        gunSound.Stop();
        Active = false;
        view.enabled = false;
        board.gameObject.SetActive(false);
        ClearArena();
        Time.timeScale = 1f;
        System.Action<bool> finished = done;
        done = null;
        finished?.Invoke(won);
    }

    void Lose(string why)
    {
        gunSound.Stop();
        step = Step.Lost;
        choice = 0;
        crosshair.gameObject.SetActive(false);
        askPanel.gameObject.SetActive(true);
        ask.text = L10n.T(why) + "\n\n" + Choices();
        Sound.Play("wagonbad");
        shake = 0.8f;
    }

    string Choices() => (choice == 0 ? "<color=#E8A000>▶ " : "") + L10n.T("menu.yes") + (choice == 0 ? "</color>" : "") + "        "
                        + (choice == 1 ? "<color=#E8A000>▶ " : "") + L10n.T("menu.no") + (choice == 1 ? "</color>" : "");

    // At the chapter's start, before the biome: her words, the gates, her words again, then the biome.
    void Start()
    {
        if (Story.Active && Story.Current.gates && !Story.GatesPassed) Invoke(nameof(Intro), 0.4f);
    }

    void Intro()
    {
        Time.timeScale = 0f;
        string her = L10n.T(Locomotives.HeroNameKey(Locomotives.Selected));
        train.Say(new[] { her }, new[] { L10n.T("gates.before") }, new[] { false }, () => Begin(won =>
        {
            if (!won) { train.FinaleToStory(); return; }
            Story.GatesPassed = true;
            Time.timeScale = 0f;
            train.Say(new[] { her }, new[] { L10n.T("gates.after") }, new[] { false }, () => Time.timeScale = 1f);
        }));
    }

    // ---------------- each frame ----------------

    void Update()
    {
        // Testing aid: N in the base's chapter, the gates on their own.
        if (!Active && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame && Story.Active && Story.Current.gates && !train.Paused && !train.AtStation && !train.InEvent && !train.Racing)
            Begin(won => train.Toast(L10n.T(won ? "gates.open" : "gates.down")));
        if (!Active || train.Paused) { gunSound.Stop(); return; }
        Time.timeScale = 0f;
        float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        MoveFragments(now, dt);
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            if (!flashes[i].t || now > flashes[i].until) { if (flashes[i].t) Destroy(flashes[i].t.gameObject); flashes.RemoveAt(i); continue; }
            flashes[i].t.localScale *= 1f + 3f * dt;
        }
        if (step == Step.Lost) { Asking(); return; }

        // the train rolls on; she aims from beside its cab
        if (step == Step.Rolling) travelled += trainSpeed * (1f + gateIndex * 0.15f) * dt;
        engine.localPosition = new Vector3(0f, 0f, travelled);
        Vector3 seat = Arena + new Vector3(-4.6f, 5.6f, travelled - 9f); // beside and behind the cab: its side in the picture
        view.transform.position = seat + (shake > 0f ? Random.insideUnitSphere * shake * 0.3f : Vector3.zero);
        Vector2 stick = move.ReadValue<Vector2>();
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f) aim = Mouse.current.position.ReadValue();
        aim += stick * aimSpeed * dt;
        aim = new Vector2(Mathf.Clamp(aim.x, 0f, Screen.width), Mathf.Clamp(aim.y, 0f, Screen.height));
        view.transform.rotation = Quaternion.Euler(-3f, 5f, 0f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)board.transform, aim, null, out Vector2 local);
        crosshair.rectTransform.anchoredPosition = local;
        shake = Mathf.Max(0f, shake - dt);

        bool fire = (Mouse.current != null && Mouse.current.leftButton.isPressed) || (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.3f)
                    || (Keyboard.current != null && (Keyboard.current.jKey.isPressed || Keyboard.current.spaceKey.isPressed));
        if (fire != gunSound.isPlaying)
        {
            if (fire) gunSound.Play(); else gunSound.Stop();
        }
        if (fire && (fireIn -= dt) <= 0f) { fireIn = 1f / fireRate; Shoot(now); }
        Guards(dt, now);

        if (step == Step.Rolling)
        {
            float left = gateAt - travelled - 6f;
            title.text = L10n.T("gates.gate", gateIndex + 1, gates) + "   " + Mathf.Max(0, Mathf.RoundToInt(left)) + " m";
            if (left <= 0f) Lose("gates.crash");
        }
        else if (step == Step.Opening && now >= stepUntil)
        {
            gateIndex++;
            if (gateIndex >= gates) { step = Step.Won; Sound.Play("race_win", 0.45f); Finish(true); return; }
            NewGate();
            step = Step.Rolling;
        }
        else if (step == Step.Opening)
        {
            // Armoured leaves retract into their side housings.
            float u = 1f - (stepUntil - now) / 1.4f;
            float slide = Mathf.SmoothStep(0f, 11f, u);
            gateLeft.localPosition = Vector3.left * slide;
            gateRight.localPosition = Vector3.right * slide;
        }
        foreach (Lock l in locks)
        {
            if (l.hits <= 0) continue;
            l.body.localPosition = l.home + Vector3.right * Mathf.Sin(now * 0.9f + l.home.y) * l.sway; // later gates: sliding
            l.casing.sharedMaterial = now - l.hitAt < 0.06f ? lockHot : lockMat;
            l.halo.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(now * 6f));
            for (int p = 0; p < l.charge.Length; p++) l.charge[p].sharedMaterial = p < l.hits ? ringMat : plateMat;
        }
        lifeFill.rectTransform.sizeDelta = new Vector2(260f * life / health, 10f);
        lifeFill.color = life > health * 0.3f ? UiKit.Mint : UiKit.Rose;
        lifeText.text = L10n.T("gates.life", Mathf.Max(0, life));
        if (life <= 0 && step != Step.Lost) Lose("gates.down");
    }

    void Asking()
    {
        var keys = Keyboard.current;
        var pad = Gamepad.current;
        float x = move.ReadValue<Vector2>().x;
        if ((keys != null && (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame)) || (pad != null && pad.dpad.left.wasPressedThisFrame)) choice = 0;
        if ((keys != null && (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame)) || (pad != null && pad.dpad.right.wasPressedThisFrame)) choice = 1;
        ask.text = ask.text.Substring(0, ask.text.IndexOf("\n\n")) + "\n\n" + Choices();
        bool ok = (keys != null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if (!ok) return;
        Sound.Play("confirm");
        if (choice == 0) Begin(done);
        else Finish(false);
    }

    void OnDisable() { if (gunSound) gunSound.Stop(); }

    void OnDestroy() { ClearArena(); if (view) Destroy(view.gameObject); if (board) Destroy(board.gameObject); }

    void Shoot(float now)
    {
        Ray ray = view.ScreenPointToRay(aim);
        Vector3 end = ray.GetPoint(200f);
        // a guard first, if one is in the way
        for (int i = guards.Count - 1; i >= 0; i--)
        {
            Guard g = guards[i];
            float along = Vector3.Dot(g.body.position - ray.origin, ray.direction);
            if (along > 0f && (ray.GetPoint(along) - g.body.position).sqrMagnitude < 2.2f)
            {
                end = g.body.position;
                Sound.Play("ascent_impact_1", 0.35f);
                Burst(g.body.position, 2f);
                Destroy(g.body.gameObject);
                guards.RemoveAt(i);
                Tracer(ray.origin + view.transform.right * 0.5f - view.transform.up * 0.4f, end, now);
                return;
            }
        }
        if (step == Step.Rolling)
            foreach (Lock l in locks)
            {
                if (l.hits <= 0) continue;
                Bounds b = l.body.GetChild(0).GetComponent<Renderer>().bounds;
                b.Expand(0.4f);
                if (!b.IntersectRay(ray, out float at)) continue;
                end = ray.GetPoint(at);
                l.hits--;
                l.hitAt = now;
                if (l.hits > 0) Sound.Play("race_scrape", 0.18f);
                if (l.hits <= 0)
                {
                    Burst(l.body.position, 2.4f);
                    l.body.gameObject.SetActive(false);
                    Sound.Play("vagonroto", 0.4f);
                    if (locks.TrueForAll(k => k.hits <= 0)) Open();
                }
                break;
            }
        Tracer(ray.origin + view.transform.right * 0.5f - view.transform.up * 0.4f, end, now);
    }

    // Every lock gone: the gate blows open.
    void Open()
    {
        step = Step.Opening;
        gateStatus.sharedMaterial = limeMat;
        stepUntil = Time.unscaledTime + 1.4f;
        for (int k = 0; k < 8; k++) Burst(gate.position + new Vector3(Random.Range(-9f, 9f), Random.Range(2f, 16f), -1f), Random.Range(3f, 6f));
        Sound.Play("vagonroto");
        shake = 0.6f;
        title.text = L10n.T("gates.open");
    }

    // The base's guards: out from the gate, at her, weaving a little.
    void Guards(float dt, float now)
    {
        if (step == Step.Rolling && now >= nextGuard)
        {
            nextGuard = now + guardEvery * Mathf.Lerp(1f, 0.6f, gateIndex / 2f);
            var g = new Guard { from = Arena + new Vector3(Random.Range(-10f, 10f), Random.Range(3f, 14f), gateAt - 2f), speed = guardSpeed * (1f + gateIndex * 0.15f) };
            var go = new GameObject("Guard");
            go.transform.SetParent(arena, false);
            go.AddComponent<MeshFilter>().sharedMesh = guardMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = guardMat;
            go.transform.localScale = Vector3.one * 2f;
            g.body = go.transform;
            g.body.position = g.from;
            guards.Add(g);
        }
        Vector3 target = view.transform.position + view.transform.forward * 1.5f;
        for (int i = guards.Count - 1; i >= 0; i--)
        {
            Guard g = guards[i];
            Vector3 to = target - g.body.position;
            if (to.magnitude < 1.5f)
            {
                life -= Mathf.RoundToInt(guardDamage);
                shake = 0.5f;
                Sound.Play("wagonbad");
                Burst(g.body.position, 1.5f);
                Destroy(g.body.gameObject);
                guards.RemoveAt(i);
                continue;
            }
            g.t += dt;
            g.body.position += to.normalized * g.speed * dt + new Vector3(Mathf.Sin(g.t * 3f + i), Mathf.Cos(g.t * 2.3f + i), 0f) * 2f * dt;
            g.body.rotation = Quaternion.LookRotation(-to);
        }
    }

    void Tracer(Vector3 from, Vector3 to, float now)
    {
        Transform t = Block(arena, Vector3.zero, Vector3.one, tracerMat);
        t.SetPositionAndRotation((from + to) * 0.5f, Quaternion.LookRotation(to - from));
        t.localScale = new Vector3(0.08f, 0.08f, (to - from).magnitude);
        flashes.Add((t, now + 0.05f));
    }

    void Burst(Vector3 at, float size)
    {
        Scatter(at, size);
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(arena, false);
        go.transform.position = at;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<Renderer>().sharedMaterial = burstMat;
        flashes.Add((go.transform, Time.unscaledTime + 0.35f));
    }
}

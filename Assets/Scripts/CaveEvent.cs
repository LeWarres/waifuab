using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The alien chapter's duel turns, in place of the driver's fist fight (BrawlEvent.BeginDriver): Faceless runs into
// a cave and the hero goes in after it. It is dark; only her flashlight shows a little, round where she aims. Faceless
// hides behind one of the rocks, crates and barrels there, and now and then its eyes glint in the light. She gets one
// shot a round: the right one and it is hit; the wrong one, or too slow, and it leaps out at her with a scream.
// Three rounds: two found, Faceless is hurt (its train with it); two missed, our wagons are. Outside, time stands
// still, like the boarding.
public class CaveEvent : MonoBehaviour
{
    public int rounds = 3, hides = 6;
    public float roundSeconds = 12f, aimSpeed = 900f, lightRadius = 150f;

    public bool Active { get; private set; }

    static readonly Vector3 Inside = new Vector3(0f, 0f, -12000f);
    enum Step { Entering, Seeking, Found, Scare, Done }

    TrainSim train;
    InputAction move;
    BrawlClips clips;
    Camera view;
    Transform cave, faceless;
    BrawlEvent.Rig rig;
    readonly List<Transform> covers = new();
    System.Action<bool> done;
    Step step;
    int round, found, behind, lastClip = -1;
    float stepUntil, nextGlint, shake, nextShudder;
    int clue; // this round's giveaway: 0 a bit of it showing, 1 its cover shuddering
    Vector2 aim;
    Vector3 scareFrom;
    Transform eyes;
    Light torch; // the flashlight
    readonly List<Light> suns = new();

    Canvas board;
    RawImage dark, flash;
    Text title, hint, score, crosshair;
    AudioSource voice, gunSound;
    float gunUntil;
    AudioClip scream;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        gunSound = Sound.Effect(gameObject, "machinegunsound", 0.6f);
        move = InputSystem.actions.FindAction("Player/Move", true);
        clips = Resources.Load<BrawlClips>("Brawl/BrawlClips");
        view = new GameObject("CaveCamera").AddComponent<Camera>();
        view.fieldOfView = 58f;
        view.nearClipPlane = 0.1f;
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

        // The dark: a black sheet with a soft hole in it where the flashlight points, over everything.
        board = UiKit.NewCanvas("Cave", 6);
        dark = new GameObject("Dark", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        dark.transform.SetParent(board.transform, false);
        dark.texture = Hole();
        dark.raycastTarget = false;
        dark.rectTransform.sizeDelta = new Vector2(lightRadius * 16f, lightRadius * 16f);
        // and black all round it, so wherever the light goes the rest of the picture stays dark
        for (int k = 0; k < 4; k++)
        {
            var edge = new GameObject("DarkEdge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            edge.transform.SetParent(dark.transform, false);
            edge.color = new Color(0f, 0f, 0f, Shade);
            edge.raycastTarget = false;
            float sheet = lightRadius * 16f, far = 8000f;
            edge.rectTransform.sizeDelta = k < 2 ? new Vector2(far, far * 2f) : new Vector2(sheet, far);
            edge.rectTransform.anchoredPosition = k == 0 ? new Vector2(-(sheet + far) / 2f, 0f) : k == 1 ? new Vector2((sheet + far) / 2f, 0f)
                : k == 2 ? new Vector2(0f, (sheet + far) / 2f) : new Vector2(0f, -(sheet + far) / 2f);
        }
        flash = new GameObject("Flash", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        flash.transform.SetParent(board.transform, false);
        flash.raycastTarget = false;
        flash.rectTransform.anchorMin = Vector2.zero;
        flash.rectTransform.anchorMax = Vector2.one;
        flash.rectTransform.sizeDelta = Vector2.zero;
        flash.color = new Color(1f, 0f, 0f, 0f);
        crosshair = UiKit.Label(board.transform, UiKit.Display, 40, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, Vector2.zero, new Vector2(70f, 70f), true);
        crosshair.text = "+";
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.7f);
        Slanted top = UiKit.Panel(board.transform, new Vector2(0f, -60f), new Vector2(720f, 56f), glass, glass, 0.12f);
        UiKit.Persona(top);
        top.rectTransform.anchorMin = top.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title = UiKit.Label(top.transform, UiKit.Display, 26, UiKit.Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(700f, 48f), true);
        Slanted right = UiKit.Panel(board.transform, new Vector2(-140f, -60f), new Vector2(220f, 56f), glass, glass, 0.12f);
        UiKit.Persona(right);
        right.rectTransform.anchorMin = right.rectTransform.anchorMax = new Vector2(1f, 1f);
        score = UiKit.Label(right.transform, UiKit.Display, 26, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200f, 48f), true);
        score.supportRichText = true;
        Slanted help = UiKit.Panel(board.transform, new Vector2(0f, 64f), new Vector2(980f, 76f), glass, glass, 0.08f);
        UiKit.Persona(help);
        help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint = UiKit.Label(help.transform, UiKit.Body, 19, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(940f, 68f));
        board.gameObject.SetActive(false);

        voice = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), 1f);
        voice.playOnAwake = false;
        scream = Scream();
    }

    // How dark the dark is: not quite black, so the crystals' glow and the shapes of the rocks show faintly.
    const float Shade = 0.9f;

    // A black texture, clear in a soft round middle.
    static Texture2D Hole()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude / (size / 16f); // 1 = the light's edge
                byte a = (byte)(Shade * 255f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.15f, d)));
                px[y * size + x] = new Color32(0, 0, 0, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // A scream made here: one short cry, a voice-like tone (a few soft harmonics) that leaps up and falls away.
    static AudioClip Scream()
    {
        const int rate = 44100;
        const float length = 0.55f;
        int n = (int)(rate * length);
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate, u = t / length;
            float pitch = (u < 0.18f ? Mathf.Lerp(420f, 760f, u / 0.18f) : Mathf.Lerp(760f, 520f, (u - 0.18f) / 0.82f)) * (1f + 0.015f * Mathf.Sin(t * 34f));
            phase += pitch / rate;
            float p = phase * Mathf.PI * 2f;
            float tone = Mathf.Sin(p) + 0.5f * Mathf.Sin(2f * p) + 0.3f * Mathf.Sin(3f * p) + 0.15f * Mathf.Sin(5f * p);
            float env = Mathf.Min(1f, t * 40f) * Mathf.Pow(1f - u, 1.6f);
            data[i] = tone * env * 0.38f;
        }
        AudioClip clip = AudioClip.Create("Scream", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ---------------- the cave ----------------

    void Build(int rival)
    {
        if (cave) Destroy(cave.gameObject);
        cave = new GameObject("Cave").transform;
        cave.position = Inside;
        covers.Clear();
        // A real cave: a floor of rough stone, walls of piled boulders all round the back and sides, a low roof with
        // stalactites hanging from it, stalagmites, and alien crystals glowing in the dark, each throwing a little
        // coloured light about. The sun is off in here (Begin): the flashlight (a real spot light) and the crystals
        // are all there is.
        Material floor = Interior.Textured(Interior.Rock(), new Color(0.3f, 0.27f, 0.36f), new Vector2(8f, 6f));
        Material roof = Interior.Textured(Interior.Rock(), new Color(0.18f, 0.16f, 0.24f), new Vector2(8f, 6f));
        Interior.Box(cave, new Vector3(0f, -0.05f, 8f), new Vector3(44f, 0.1f, 34f), floor);
        Interior.Box(cave, new Vector3(0f, 7.6f, 8f), new Vector3(44f, 0.2f, 34f), roof);
        for (int k = 0; k < 11; k++) // the walls: chunks of piled rock on an arc round the back and sides
        {
            float a = Mathf.Lerp(-105f, 105f, k / 10f);
            Vector3 at = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 16f, -0.5f, 9f + Mathf.Cos(a * Mathf.Deg2Rad) * 12f);
            Interior.Place("wall", cave, at, a + Random.Range(-15f, 15f), Random.Range(1f, 1.3f));
        }
        for (int k = 0; k < 16; k++) // stalactites, their tops in the roof
        {
            float scale = Random.Range(0.7f, 1.4f);
            Interior.Place("stalactite", cave, new Vector3(Random.Range(-13f, 13f), 7.6f - 2.4f * scale, Random.Range(3f, 19f)), Random.Range(0f, 360f), scale);
        }
        for (int k = 0; k < 7; k++)
            Interior.Place("stalagmite", cave, new Vector3(Random.Range(-14f, 14f), 0f, Random.Range(14f, 19f)), Random.Range(0f, 360f), Random.Range(0.8f, 1.5f));
        Color[] glows = { new Color(0.3f, 1f, 0.85f), new Color(0.95f, 0.35f, 0.85f) };
        for (int k = 0; k < 7; k++) // the crystals, each lighting the rock round it
        {
            Vector3 at = new Vector3((k % 2 == 0 ? -1f : 1f) * Random.Range(5f, 14f), 0f, Random.Range(4f, 18f));
            Interior.Place("crystals", cave, at, Random.Range(0f, 360f), Random.Range(0.8f, 1.4f));
            Interior.Lamp(cave, at + Vector3.up * 1f, glows[k % 2], 500000f, 7f, false);
        }

        // Faceless, behind one of them.
        string id = Locomotives.HeroId(rival);
        var root = new GameObject("Faceless").transform;
        root.SetParent(cave, false);
        Animator body = BrawlEvent.Body(Resources.Load<GameObject>("Characters/" + id + "/" + id), root, 1.8f);
        Player.Skin(body.transform, "Characters/" + id + "/" + id);
        rig?.Free();
        rig = new BrawlEvent.Rig(root, body);
        faceless = root;
        eyes = new GameObject("Glint").transform; // its eyes, glinting over the edge of where it hides
        eyes.SetParent(cave, false);
        Material glint = TrainSim.Mat(new Color(3f, 0.4f, 0.3f), false);
        for (int s = -1; s <= 1; s += 2) Shape(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.12f, glint).SetParent(eyes, false);
        eyes.GetChild(0).localPosition = new Vector3(-0.14f, 0f, 0f);
        eyes.GetChild(1).localPosition = new Vector3(0.14f, 0f, 0f);
        cave.gameObject.SetActive(false);
    }

    // Where it can hide, new every round: rocks, crates and barrels (each big enough for it) in new places, a new
    // number of them, none on top of another.
    readonly List<Vector3> rest = new(); // each cover's own place, to shudder about
    readonly List<Vector3> sizes = new(); // and how big it is
    void Shuffle()
    {
        foreach (Transform c in covers) if (c) Destroy(c.gameObject);
        covers.Clear();
        rest.Clear();
        sizes.Clear();
        int count = Random.Range(hides - 1, hides + 2);
        for (int tries = 0; covers.Count < count && tries < 200; tries++)
        {
            Vector3 at = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(6.5f, 12.5f));
            if (rest.Exists(r => Mathf.Abs(r.x - at.x) < 2.9f)) continue; // side by side from where she stands: none hidden behind another
            // each big enough for it to crouch behind: rocks most often, the miners' crates and barrels too
            string[] kinds = { "boulder_a", "boulder_b", "boulder_c", "crate", "barrel" };
            string kind = kinds[Random.Range(0, kinds.Length)];
            float grow = (kind == "crate" ? 1.9f : kind == "barrel" ? 1.7f : 1f) * Random.Range(0.95f, 1.15f);
            Transform c = Interior.Place(kind, cave, at, Random.Range(0f, 360f), grow);
            covers.Add(c);
            rest.Add(c.localPosition);
            sizes.Add(Interior.Bounds(c).size);
        }
    }

    Transform Shape(PrimitiveType type, Vector3 at, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(cave, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    // ---------------- start and end ----------------

    // done(true): found it in two rounds out of three.
    public void Begin(int rival, System.Action<bool> done)
    {
        this.done = done;
        Build(rival);
        round = found = 0;
        Active = true;
        cave.gameObject.SetActive(true);
        view.enabled = true;
        // In here only the flashlight and the crystals give light: the sun goes off until she is out.
        suns.Clear();
        foreach (Light sun in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (sun.type == LightType.Directional && sun.enabled) { sun.enabled = false; suns.Add(sun); }
        if (!torch)
        {
            torch = Interior.Lamp(view.transform, Vector3.zero, new Color(1f, 0.95f, 0.85f), 3500000f, 40f, true, LightType.Spot);
            torch.spotAngle = 30f;
            torch.innerSpotAngle = 14f;
            torch.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>().enableSpotReflector = true; // all its light in the cone
        }
        view.transform.SetPositionAndRotation(Inside + new Vector3(0f, 1.7f, -2f), Quaternion.Euler(6f, 0f, 0f));
        board.gameObject.SetActive(true);
        Time.timeScale = 0f;
        aim = new Vector2(Screen.width / 2f, Screen.height / 2f);
        title.text = L10n.T("cave.enter");
        hint.text = L10n.T("cave.hint");
        Go(Step.Entering, 2.4f);
        Score();
    }

    public void TestWin()
    {
        if (!Active) return;
        found = rounds;
        End();
    }

    void End()
    {
        gunSound.Stop();
        Active = false;
        view.enabled = false;
        foreach (Light sun in suns) if (sun) sun.enabled = true;
        suns.Clear();
        board.gameObject.SetActive(false);
        rig?.Free();
        rig = null;
        if (cave) Destroy(cave.gameObject);
        Time.timeScale = 1f;
        System.Action<bool> finished = done;
        done = null;
        bool won = found * 2 > rounds;
        train.Toast(L10n.T(won ? "cave.won" : "cave.lost"));
        finished?.Invoke(won);
    }

    void OnDestroy() => rig?.Free();

    void OnDisable() { if (gunSound) gunSound.Stop(); }

    void Go(Step next, float seconds)
    {
        step = next;
        stepUntil = Time.unscaledTime + seconds;
        if (next == Step.Seeking)
        {
            // new hiding places; it crouches behind one, and gives itself away somehow: a hand or a shoulder just
            // showing past the edge of its cover, or the cover giving a shudder now and then (and its eyes glint
            // in the light either way)
            Shuffle();
            behind = Random.Range(0, covers.Count);
            Transform cover = covers[behind];
            clue = Random.Range(0, 2);
            float edge = sizes[behind].x * 0.5f - 0.1f;
            faceless.localPosition = new Vector3(rest[behind].x, 0f, rest[behind].z) + new Vector3(clue == 0 ? edge * (Random.value < 0.5f ? -1f : 1f) : 0f, 0f, 1.2f);
            nextShudder = Time.unscaledTime + Random.Range(1f, 2f);
            faceless.localRotation = Quaternion.Euler(0f, 180f, 0f);
            faceless.gameObject.SetActive(true);
            Play(clips ? clips.guard : null);
            title.text = L10n.T("cave.round", round + 1, rounds);
            nextGlint = Time.unscaledTime + Random.Range(1.2f, 2.5f);
        }
    }

    void Play(AnimationClip clip)
    {
        if (rig == null || !clip) return;
        rig.Play(clip, true);
    }

    // ---------------- each frame ----------------

    void Update()
    {
        // Testing aid: N in the alien chapter, the cave on its own.
        if (!Active && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame && Story.Active && Story.Current.cave && !train.Paused && !train.AtStation && !train.InEvent && !train.Racing)
            Begin(Story.Current.rival, won => { });
        if (!Active || train.Paused) { gunSound.Stop(); return; }
        if (gunSound.isPlaying && Time.unscaledTime >= gunUntil) gunSound.Stop();
        Time.timeScale = 0f;
        float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        rig?.Tick(dt);

        // The flashlight: the mouse, or the stick moving it.
        Vector2 stick = move.ReadValue<Vector2>();
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f) aim = Mouse.current.position.ReadValue();
        aim += stick * aimSpeed * dt;
        aim = new Vector2(Mathf.Clamp(aim.x, 0f, Screen.width), Mathf.Clamp(aim.y, 0f, Screen.height));
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)board.transform, aim, null, out local);
        dark.rectTransform.anchoredPosition = local;
        crosshair.rectTransform.anchoredPosition = local;
        torch.transform.rotation = Quaternion.LookRotation(view.ScreenPointToRay(aim).direction); // where the hole is
        // a little sway, as a hand holds it
        view.transform.localRotation = Quaternion.Euler(6f + Mathf.Sin(now * 1.3f) * 0.4f, Mathf.Sin(now * 0.9f) * 0.6f, 0f);
        bool shoot = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame)
                     || (Keyboard.current != null && (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));

        switch (step)
        {
            case Step.Entering:
                // walking in: the light closes in from wide open
                dark.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 4f, (stepUntil - now) / 2.4f);
                view.transform.position = Inside + new Vector3(0f, 1.7f, Mathf.Lerp(-2f, -6f, (stepUntil - now) / 2.4f));
                if (now >= stepUntil) { dark.rectTransform.localScale = Vector3.one; Go(Step.Seeking, roundSeconds); }
                break;
            case Step.Seeking:
                Glint(now);
                Shudder(now);
                title.text = L10n.T("cave.round", round + 1, rounds) + "   " + Mathf.CeilToInt(stepUntil - now) + "s";
                if (shoot) Shoot(now);
                else if (now >= stepUntil) Scare(now, -1); // too slow: it comes for her
                break;
            case Step.Found:
                if (now >= stepUntil) Next();
                break;
            case Step.Scare:
                // out from the edge of the picture, at her, filling it
                float u = 1f - (stepUntil - now) / 1.1f;
                Vector3 face = view.transform.position + view.transform.forward * 1.1f - Vector3.up * 1.4f;
                faceless.position = Vector3.Lerp(scareFrom, face, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, u * 2.2f)));
                faceless.rotation = Quaternion.LookRotation(view.transform.position - faceless.position) * Quaternion.Euler(0f, 0f, Mathf.Sin(now * 40f) * 4f);
                flash.color = new Color(0.6f, 0f, 0f, Mathf.Clamp01(0.6f - Mathf.Abs(u - 0.45f)));
                dark.rectTransform.localScale = Vector3.one * (1f + u * 2f);
                if (now >= stepUntil) { flash.color = new Color(1f, 0f, 0f, 0f); dark.rectTransform.localScale = Vector3.one; Next(); }
                break;
        }
        if (shake > 0f)
        {
            view.transform.position += Random.insideUnitSphere * shake * 0.15f;
            shake = Mathf.Max(0f, shake - dt);
        }
    }

    // Now and then, if the light is near, its eyes glint over the edge of its cover.
    void Glint(float now)
    {
        Transform cover = covers[behind];
        Vector3 top = cover.position + Vector3.up * (sizes[behind].y * 0.9f) + Vector3.forward * 0.2f;
        bool lit = (view.WorldToScreenPoint(top) - (Vector3)aim).magnitude < lightRadius * 1.4f;
        bool on = lit && now >= nextGlint && now < nextGlint + 0.35f;
        if (now >= nextGlint + 0.35f) nextGlint = now + Random.Range(1.4f, 2.8f);
        eyes.gameObject.SetActive(on);
        if (on) eyes.position = top;
    }

    void Shudder(float now)
    {
        Transform cover = covers[behind];
        bool on = clue == 1 && now >= nextShudder && now < nextShudder + 0.35f;
        if (now >= nextShudder + 0.35f) nextShudder = now + Random.Range(1.6f, 3f);
        cover.localPosition = rest[behind] + (on ? new Vector3(Mathf.Sin(now * 70f) * 0.08f, Mathf.Abs(Mathf.Sin(now * 55f)) * 0.05f, 0f) : Vector3.zero);
    }

    void Shoot(float now)
    {
        Ray ray = view.ScreenPointToRay(aim);
        int hit = -1;
        float best = float.MaxValue;
        for (int i = 0; i < covers.Count; i++)
        {
            Bounds b = Interior.Bounds(covers[i]);
            if (b.IntersectRay(ray, out float at) && at < best) { best = at; hit = i; }
        }
        gunSound.Play();
        gunUntil = now + 0.12f; // one short shot from the swarm turret clip
        shake = 0.2f;
        eyes.gameObject.SetActive(false);
        if (hit < 0) return; // into the dark: try again
        covers[hit].gameObject.SetActive(false);
        Combat.Instance?.Burst("spark", covers[hit].position, 1.5f);
        if (hit == behind)
        {
            // found: it staggers out, hit
            found++;
            faceless.localPosition = new Vector3(rest[hit].x, 0f, rest[hit].z - 0.6f); // out in front of where it hid
            Play(clips && clips.hit ? clips.hit : null);
            title.text = L10n.T("cave.found");
            Sound.Play("confirm");
            Score();
            Go(Step.Found, 1.6f);
        }
        else Scare(now, hit);
    }

    // The jump scare: from one edge of the picture straight at her, with a scream; the round is lost.
    void Scare(float now, int wrong)
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        scareFrom = view.transform.position + view.transform.right * side * 4.5f + view.transform.forward * 3f - Vector3.up * 1.6f;
        faceless.position = scareFrom;
        Play(clips && clips.heavy ? clips.heavy : null);
        voice.PlayOneShot(scream);
        shake = 0.8f;
        title.text = L10n.T(wrong >= 0 ? "cave.wrong" : "cave.slow");
        Score();
        Go(Step.Scare, 1.1f);
    }

    void Next()
    {
        round++;
        Score();
        if (round >= rounds || found * 2 > rounds || (round - found) * 2 > rounds) { End(); return; }
        Go(Step.Seeking, roundSeconds);
    }

    void Score()
    {
        var line = new System.Text.StringBuilder();
        for (int i = 0; i < rounds; i++)
            line.Append(i < found ? "<color=#5CE08A>●</color>" : i < round + (step == Step.Scare ? 1 : 0) ? "<color=#FF5566>●</color>" : "<color=#ffffff40>●</color>");
        score.text = line.ToString();
    }
}

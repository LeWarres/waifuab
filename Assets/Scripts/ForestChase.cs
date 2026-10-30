using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Faceless's last chapter, Slender turned round: at night in a dark forest it is Faceless who stalks, and the curious
// little thieves who carry the lights. Eight of her drawings are out there, like Slender's eight pages: five in the
// hands of children wandering with torches, three pinned to trees that they keep an eye on. She takes them back from
// behind or out of the light (never by force); a child's torch on her fills the panic, and full panic sends them all
// running out of the forest with the drawings. Each drawing taken makes them warier (faster, their torches reaching
// further and sweeping more), and at dawn the night is over.
public partial class ForestChase : MonoBehaviour
{
    enum Phase { Intro, Playing, Won, Lost }
    class Thief
    {
        public Transform root, paper, pool, cone; public BrawlEvent.Rig rig; public Light torch;
        public Vector3 heading, wonder; public bool carrying = true; public float nextTurn, scaredUntil, sweep, lookBackAt, wonderUntil, turnUntil; public Text marker;
        public Transform guard; // the pinned drawing it keeps an eye on, if any
    }
    [System.NonSerialized] public float seconds = 200f; // till dawn (not the old 90 saved with the scene)
    const int Carried = 5, Pinned = 3, Drawings = Carried + Pinned;
    const float Edge = 42f, Reach = 2.5f; // a child notices her at 1.4 m: the hand reaches a little further
    readonly List<Thief> thieves = new();
    readonly List<Material> materials = new();
    readonly List<Vector3> trunks = new();
    readonly List<Transform> returned = new();
    readonly List<Transform> onTrees = new();          // the pinned ones still out there
    BrawlEvent.Rig hero;
    BrawlClips clips;
    Transform world, player, artTree;
    Camera view;
    Light moon;
    InputAction move;
    Canvas hud;
    Slanted card, panicBar;
    Image seenVeil;
    Text title, instructions, button, score, clock, hint;
    Phase phase;
    float remaining, age, stamina = 1f, panic, yaw, seenFor, now; // now: the game's own clock (a test bot runs it faster)
    int recovered, lastConfirm = -1;
    bool paused, leaving;
    public int Recovered => recovered;
    public bool Playing => phase == Phase.Playing && !paused;
    public Vector3 PlayerPosition => player.position;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        UiKit.SetBiome("jungle");
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        clips = Resources.Load<BrawlClips>("Brawl/BrawlClips");
        BuildWorld(); Night(); BuildUI();
        remaining = seconds;
        Dialog("forest.title", "forest.intro", "forest.begin");
    }

    Material Paint(Color color) { var m = TrainSim.Mat(color, false); materials.Add(m); return m; }

    Transform Block(string name, Transform parent, Vector3 at, Vector3 scale, Material material)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        t.name = name; Destroy(t.GetComponent<Collider>());
        t.SetParent(parent, false); t.localPosition = at; t.localScale = scale;
        t.GetComponent<Renderer>().sharedMaterial = material;
        return t;
    }

    Transform Prop(GameObject prefab, Vector3 at, float height)
    {
        var t = Instantiate(prefab, world).transform;
        t.position = at; t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var bounds = Interior.Bounds(t);
        t.localScale *= height / Mathf.Max(.01f, bounds.size.y);
        bounds = Interior.Bounds(t); t.position += Vector3.up * (at.y - bounds.min.y);
        foreach (var r in t.GetComponentsInChildren<Renderer>()) TrainSim.Toon(r);
        foreach (var c in t.GetComponentsInChildren<Collider>()) Destroy(c);
        return t;
    }

    // A forest round her art tree: a ring of tall trees at its edge, and many more scattered through it, with room to
    // walk between; three of them with a drawing pinned on the trunk.
    void BuildWorld()
    {
        world = new GameObject("DrawingForest").transform;
        var ground = Block("MossFloor", world, new Vector3(0f, -.2f, 0f), new Vector3(200f, .4f, 200f), Paint(new Color(.35f, .46f, .23f)));
        ground.GetComponent<Renderer>().sharedMaterial.mainTexture = Resources.Load<Texture2D>("Ground/jungle");
        ground.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(24f, 24f);
        var trees = new[] { Resources.Load<GameObject>("Scenery/jungle/00_Broadleaf_Tree_01A"), Resources.Load<GameObject>("Scenery/jungle/01_Broadleaf_Tree_03A") };
        artTree = Prop(trees[0], new Vector3(0f, 0f, 5f), 9f);
        trunks.Add(artTree.position);
        for (int i = 0; i < 52; i++)
        {
            float a = i * Mathf.PI * 2f / 52f;
            Prop(trees[i % 2], new Vector3(Mathf.Sin(a) * (Edge + 3f), 0f, Mathf.Cos(a) * (Edge + 3f)), Random.Range(8f, 12f));
        }
        for (int tries = 0; trunks.Count < 60 && tries < 4000; tries++)
        {
            Vector2 spot = Random.insideUnitCircle.normalized * Random.Range(10f, Edge - 2f);
            Vector3 at = new Vector3(spot.x, 0f, spot.y);
            if (trunks.Exists(o => (o - at).sqrMagnitude < 5f * 5f)) continue;
            Prop(trees[trunks.Count % 2], at, Random.Range(14f, 18f)); // tall: their crowns high over the camera's head
            trunks.Add(at);
        }
        var bush = Resources.Load<GameObject>("Scenery/jungle/07_Flower_Bush_01A");
        for (int i = 0; i < 40; i++)
            Prop(bush, new Vector3(Random.Range(-Edge, Edge), 0f, Random.Range(-Edge, Edge)), Random.Range(.5f, .9f));
        // three drawings pinned to trunks out in the dark, each in a faint glow so it can be found
        var far = trunks.FindAll(o => o.magnitude > 20f);
        for (int i = 0; i < Pinned && far.Count > 0; i++)
        {
            Vector3 trunk = far[Random.Range(0, far.Count)];
            far.RemoveAll(o => (o - trunk).sqrMagnitude < 18f * 18f);
            Vector3 face = (-trunk).normalized;
            var paper = Paper(world, trunk + face * .55f + Vector3.up * 1.7f, i);
            paper.rotation = Quaternion.LookRotation(-face) * Quaternion.Euler(-90f, 0f, 0f);
            Interior.Lamp(paper, Vector3.up * .3f, new Color(1f, .95f, .75f), 600f, 4f, false);
            onTrees.Add(paper);
        }
        player = new GameObject("FacelessArtist").transform;
        player.SetParent(world, false); player.position = new Vector3(0f, 0f, -3f);
        const string who = "Characters/faceless/faceless";
        var body = BrawlEvent.Body(Resources.Load<GameObject>(who), player, 2.3f);
        Player.Skin(body.transform, who);
        hero = new BrawlEvent.Rig(player, body); hero.Play(clips.idle);
        Interior.Lamp(player, new Vector3(0f, 2.6f, 0f), new Color(.55f, .65f, 1f), 900f, 7f, false); // just enough to see her way
        // the children, each with a torch and one of the drawings, spread through the forest
        var coat = Paint(new Color(.25f, .62f, .85f));
        Material warm = Paint(new Color(3f, 2.6f, 1.6f)), poolLook = Paint(new Color(1.5f, 1.25f, .75f)), beamLook = Beam();
        for (int i = 0; i < Carried; i++)
        {
            var t = new GameObject("CuriousThief_" + i).transform;
            t.SetParent(world, false);
            float a = (i + .5f) * Mathf.PI * 2f / Carried;
            t.position = AvoidTrunks(Vector3.zero, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * Random.Range(16f, 30f));
            var small = BrawlEvent.Body(clips.boarder, t, 1.1f);
            foreach (var r in small.GetComponentsInChildren<Renderer>()) { TrainSim.Toon(r); r.sharedMaterial = coat; }
            var rig = new BrawlEvent.Rig(t, small); rig.Play(clips.boarderIdle);
            var paper = Paper(t, new Vector3(.3f, .8f, .1f), Pinned + i);
            var torch = Interior.Lamp(t, new Vector3(0f, .9f, .25f), new Color(1f, .92f, .7f), 40000f, 16f, false, LightType.Spot);
            torch.spotAngle = 50f;
            torch.innerSpotAngle = 20f;
            torch.transform.localRotation = Quaternion.Euler(12f, 0f, 0f); // a little down, onto the path ahead
            // the torch itself glowing in the hand, and the pool of light it throws on the ground ahead (what she must keep out of)
            Block("TorchBulb", t, new Vector3(0f, .9f, .3f), Vector3.one * .16f, warm);
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            Destroy(pool.GetComponent<Collider>());
            pool.SetParent(world, false);
            pool.GetComponent<Renderer>().sharedMaterial = poolLook;
            pool.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var cone = new GameObject("TorchBeam", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            cone.SetParent(t, false);
            cone.localPosition = new Vector3(0f, .9f, .3f);
            cone.GetComponent<MeshFilter>().sharedMesh = Cone();
            cone.GetComponent<MeshRenderer>().sharedMaterial = beamLook;
            cone.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            thieves.Add(new Thief { root = t, paper = paper, rig = rig, torch = torch, pool = pool, cone = cone, lookBackAt = Random.Range(3f, 8f), heading = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)), sweep = Random.value * 10f });
        }
        for (int i = 0; i < onTrees.Count && i < thieves.Count; i++) thieves[i].guard = onTrees[i]; // one child keeps watch by each tree
        view = Camera.main;
        view.orthographic = false; view.fieldOfView = 58f; view.nearClipPlane = .1f; view.farClipPlane = 140f;
        yaw = 0f;
        PlaceCamera(1f);
    }

    // Night: a dim blue moon instead of the sun, haze that swallows the far trees, a fixed exposure so the torches glow.
    void Night()
    {
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) { moon = light; light.color = new Color(.55f, .65f, 1f); light.intensity = 150f; light.transform.rotation = Quaternion.Euler(38f, 30f, 0f); }
        var look = new GameObject("ForestNight").AddComponent<UnityEngine.Rendering.Volume>();
        look.isGlobal = true;
        look.priority = 20f;
        var exposure = look.profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(true);
        exposure.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.Fixed;
        exposure.fixedExposure.value = 10.5f;
        // the daylight sky must not light the forest from every side
        var indirect = look.profile.Add<UnityEngine.Rendering.HighDefinition.IndirectLightingController>(true);
        indirect.indirectDiffuseLightingMultiplier.value = 0.08f;
        indirect.reflectionLightingMultiplier.value = 0.1f;
        var fog = look.profile.Add<UnityEngine.Rendering.HighDefinition.Fog>(true);
        fog.enabled.value = true;
        fog.meanFreePath.value = 22f;
        fog.albedo.value = new Color(.05f, .07f, .14f);
        fog.color.value = new Color(.04f, .06f, .12f);
        fog.colorMode.value = UnityEngine.Rendering.HighDefinition.FogColorMode.ConstantColor;
        fog.enableVolumetricFog.value = true;
        var hd = view.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        if (hd) { hd.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color; hd.backgroundColorHDR = new Color(.01f, .015f, .035f); }
    }

    // A cone one long and one across at its open end, its point at the origin, pointing down +z; open, both faces drawn.
    static Mesh Cone()
    {
        const int sides = 20;
        var v = new List<Vector3> { Vector3.zero };
        var tris = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 1f));
            int next = 1 + (i + 1) % sides;
            tris.AddRange(new[] { 0, 1 + i, next, 0, next, 1 + i });
        }
        var mesh = new Mesh { name = "TorchCone" };
        mesh.SetVertices(v);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // The beam's look: HDRP's unlit, transparent and added onto what is behind, a faint warm light.
    Material Beam()
    {
        var m = new Material(Shader.Find("HDRP/Unlit"));
        m.SetColor("_UnlitColor", new Color(.16f, .13f, .07f, 1f));
        m.SetFloat("_SurfaceType", 1f);  // transparent
        m.SetFloat("_BlendMode", 1f);    // additive
        m.SetFloat("_DoubleSidedEnable", 1f);
        m.SetFloat("_ZWrite", 0f);
        UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
        materials.Add(m);
        return m;
    }

    Transform Paper(Transform parent, Vector3 at, int number)
    {
        var root = new GameObject("Drawing_" + number).transform;
        root.SetParent(parent, false); root.localPosition = at;
        Block("Paper", root, Vector3.zero, new Vector3(.85f, .04f, 1.1f), Paint(new Color(1f, .97f, .82f)));
        var ink = Paint(new Color(.1f, .12f, .16f));
        // Little house/train doodles are physical ink strokes on the page, so they remain crisp while carried.
        for (int i = 0; i < 4; i++)
            Block("PencilStroke", root, new Vector3(-.25f + i * .16f, .028f, 0f), new Vector3(.035f, .012f, .48f - i * .06f), ink);
        root.localRotation = Quaternion.Euler(-45f, 0f, 0f);
        return root;
    }

    void BuildUI()
    {
        hud = UiKit.NewCanvas("ForestDrawings", 30);
        // a red veil at the edges while a torch is on her
        seenVeil = new GameObject("SeenVeil", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        seenVeil.transform.SetParent(hud.transform, false);
        seenVeil.rectTransform.anchorMin = Vector2.zero; seenVeil.rectTransform.anchorMax = Vector2.one;
        seenVeil.rectTransform.offsetMin = seenVeil.rectTransform.offsetMax = Vector2.zero;
        seenVeil.raycastTarget = false;
        seenVeil.color = new Color(1f, .9f, .6f, 0f);
        var top = UiKit.Persona(UiKit.Panel(hud.transform, new Vector2(0f, 460f), new Vector2(1680f, 100f), UiKit.Navy, UiKit.Navy, .04f));
        score = UiKit.Label(top.transform, UiKit.Display, 36, Color.white, TextAnchor.MiddleLeft, new Vector2(-470f, 0f), new Vector2(650f, 70f), true);
        clock = UiKit.Label(top.transform, UiKit.Body, 28, Color.white, TextAnchor.MiddleRight, new Vector2(560f, 0f), new Vector2(480f, 70f), true);
        // the panic: a bar in the middle of the top band, filling red
        UiKit.Label(top.transform, UiKit.Display, 26, Color.white, TextAnchor.MiddleRight, new Vector2(-110f, 0f), new Vector2(200f, 50f), true).text = L10n.T("forest.panic");
        UiKit.Panel(top.transform, new Vector2(140f, 0f), new Vector2(300f, 26f), new Color(0f, 0f, 0f, .5f), new Color(0f, 0f, 0f, .5f), .1f);
        panicBar = UiKit.Panel(top.transform, new Vector2(-10f, 0f), new Vector2(0f, 20f), new Color(1f, .3f, .3f), new Color(.8f, .1f, .2f), .1f);
        panicBar.rectTransform.pivot = new Vector2(0f, .5f);
        score.text = L10n.T("forest.score", 0, Drawings);
        hint = UiKit.Label(hud.transform, UiKit.Body, 25, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -460f), new Vector2(1750f, 90f), true);
        hint.text = L10n.T("forest.controls");
        hint.GetComponent<RectTransform>().SetAsLastSibling();
        card = UiKit.Persona(UiKit.Panel(hud.transform, Vector2.zero, new Vector2(1460f, 380f), UiKit.Navy, UiKit.Navy, .025f));
        title = UiKit.Label(card.transform, UiKit.Display, 48, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(1300f, 80f), true);
        instructions = UiKit.Label(card.transform, UiKit.Body, 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 5f), new Vector2(1340f, 170f), true);
        var action = UiKit.Paint(UiKit.Panel(card.transform, new Vector2(0f, -135f), new Vector2(520f, 60f), Color.white, Color.white, .2f), UiKit.Ink.Main);
        action.gameObject.AddComponent<Clicks>().click = _ => Confirm();
        button = UiKit.Label(action.transform, UiKit.Display, 32, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(490f, 55f), true);
        foreach (var t in thieves)
        {
            t.marker = UiKit.Label(hud.transform, UiKit.Body, 30, UiKit.Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80f, 60f), true);
            t.marker.text = "✎";
            t.marker.transform.SetAsFirstSibling();
        }
    }

    void Dialog(string name, string text, string accept)
    {
        card.gameObject.SetActive(true); age = 0f;
        title.text = L10n.T(name); instructions.text = L10n.T(text); button.text = L10n.T(accept);
    }

    void Confirm()
    {
        if (leaving || age < .3f || lastConfirm == Time.frameCount) return;
        lastConfirm = Time.frameCount; Sound.Play("confirm");
        if (paused) { paused = false; card.gameObject.SetActive(false); return; }
        if (phase == Phase.Intro) { phase = Phase.Playing; card.gameObject.SetActive(false); return; }
        if (phase == Phase.Playing) return;
        leaving = true;
        if (Story.Active)
        {
            if (phase == Phase.Won) Story.Win(null, null, 0);
            else Story.Play(Story.Chapter);
            Wipe.Load("historiaDialogos");
        }
        else Wipe.Load("MainMenu");
    }

    Vector3 AvoidTrunks(Vector3 from, Vector3 target)
    {
        foreach (var trunk in trunks)
        {
            Vector3 away = target - trunk; away.y = 0f;
            if (away.sqrMagnitude < 1.2f * 1.2f) target = trunk + (away.sqrMagnitude < .01f ? (from - trunk).normalized : away.normalized) * 1.2f;
        }
        target.y = 0f;
        return Vector3.ClampMagnitude(target, Edge);
    }

    // A trunk between two points blocks a torch's light.
    bool Clear(Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a; ab.y = 0f;
        float length = ab.magnitude;
        if (length < .01f) return true;
        ab /= length;
        foreach (var trunk in trunks)
        {
            Vector3 to = trunk - a; to.y = 0f;
            float along = Vector3.Dot(to, ab);
            if (along > .5f && along < length - .5f && (to - ab * along).sqrMagnitude < .8f * .8f) return false;
        }
        return true;
    }

    // How wary they are: each drawing taken back makes them quicker, their torches reach further and they look round more.
    float Wary => recovered / (float)Drawings;
    float Range => 11f + Wary * 6f; // how far a torch reaches

    void PlaceCamera(float blend)
    {
        Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
        Vector3 want = player.position + turn * new Vector3(0f, 3.1f, -6.2f);
        // pulled in front of a trunk that would stand between it and her
        Vector3 back = want - player.position; back.y = 0f;
        float length = back.magnitude;
        foreach (var trunk in trunks)
        {
            Vector3 to = trunk - player.position; to.y = 0f;
            float along = Vector3.Dot(to, back / length);
            if (along > 0f && along < length && (to - back / length * along).sqrMagnitude < 1.3f * 1.3f) length = Mathf.Min(length, along - 1.2f);
        }
        want = player.position + back.normalized * Mathf.Max(1.5f, length) + Vector3.up * 3.1f;
        view.transform.position = Vector3.Lerp(view.transform.position, want, blend);
        view.transform.rotation = Quaternion.Slerp(view.transform.rotation, Quaternion.LookRotation(player.position + Vector3.up * 1.6f + turn * Vector3.forward * 3f - view.transform.position), blend);
    }

    void Update()
    {
        if (leaving) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f); age += dt;
        var k = Keyboard.current; var p = Gamepad.current;
        bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) || (p != null && p.buttonSouth.wasPressedThisFrame);
        if (Story.TakeTestAdvance()) { if (phase == Phase.Won) Confirm(); else TestWin(); return; }
        if (phase == Phase.Playing && ((k != null && k.escapeKey.wasPressedThisFrame) || (p != null && (p.startButton.wasPressedThisFrame || p.buttonEast.wasPressedThisFrame))))
        {
            paused = !paused; Sound.Play("Pause");
            if (paused) Dialog("ascent.pause", "ascent.pause.text", "ascent.resume"); else card.gameObject.SetActive(false);
            return;
        }
        Cursor.lockState = phase == Phase.Playing && !paused ? CursorLockMode.Locked : CursorLockMode.None; // the mouse turns the view while playing
        if (phase != Phase.Playing || paused) { if (confirm) Confirm(); return; }
        for (int step = 0; step < (Botting ? BotSpeed : 1) && phase == Phase.Playing; step++) Step(dt, k, p, confirm && step == 0);
    }

    void Step(float dt, Keyboard k, Gamepad p, bool confirm)
    {
        now += dt;
        remaining -= dt;
        // the night lightens toward dawn
        if (moon) moon.intensity = Mathf.Lerp(150f, 6000f, Mathf.Pow(1f - remaining / seconds, 3f));

        // she moves the way the camera looks; the mouse or the right stick turns it
        float turn = (Mouse.current != null ? Mouse.current.delta.ReadValue().x * .15f : 0f) + (p != null ? p.rightStick.ReadValue().x * 140f * dt : 0f)
                     + (k != null ? ((k.eKey.isPressed ? 1f : 0f) - (k.qKey.isPressed ? 1f : 0f)) * 110f * dt : 0f);
        yaw += turn;
        var input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        bool sprint = stamina > .03f && ((k != null && k.leftShiftKey.isPressed) || (p != null && p.rightTrigger.isPressed));
        if (Botting) { BotThink(dt); yaw = botYaw; input = botMove; sprint = stamina > .03f && botSprint; confirm = botTake; }
        stamina = Mathf.Clamp01(stamina + dt * (sprint && input.sqrMagnitude > .1f ? -.28f : .16f));
        Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
        player.position = AvoidTrunks(player.position, player.position + direction * ((sprint ? 8.5f : 5.2f) * dt));
        if (direction.sqrMagnitude > .01f) player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(direction), dt * 12f);
        hero.Play(direction.sqrMagnitude > .01f ? clips.run : clips.idle, false, sprint ? 1.3f : 1f); hero.Tick(dt);
        PlaceCamera(1f - Mathf.Exp(-8f * dt));

        // the children: wandering, sweeping their torches round, fleeing her when she is seen or heard
        bool seen = false;
        Thief nearest = null; float distance = Reach;
        foreach (var t in thieves)
        {
            Vector3 toHer = player.position - t.root.position; toHer.y = 0f;
            float d = toHer.magnitude, range = Range;
            bool scared = now < t.scaredUntil;
            if (!scared && now >= t.nextTurn)
            {
                t.nextTurn = now + Random.Range(1.2f, 2.6f) * (1f - Wary * .4f);
                // wander; one keeping watch over a drawing on a tree stays within a few steps of it
                Vector3 lean = Vector3.zero;
                if (t.guard && onTrees.Contains(t.guard)) { Vector3 to = t.guard.position - t.root.position; to.y = 0f; if (to.magnitude > 7f) lean = to.normalized * 2f; }
                t.heading = (Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * t.heading + lean).normalized;
                if (t.root.position.magnitude > Edge - 4f) t.heading = (-t.root.position.normalized + Random.insideUnitSphere * .4f).normalized;
                t.heading.y = 0f;
            }
            if (scared) t.heading = (-toHer).normalized;
            // now and then, or at a sound (her running near, or a step right behind), it stops ("?") and turns to look
            bool heard = d < (sprint ? 9f : 2.6f);
            if (!scared && now >= t.turnUntil && (now >= t.lookBackAt || heard))
            {
                Vector3 back = -t.root.forward; back.y = 0f;
                t.wonder = heard ? toHer.normalized : back.normalized;
                t.wonderUntil = now + (heard ? .35f : .6f);
                t.turnUntil = t.wonderUntil + 1.3f;
                t.lookBackAt = t.turnUntil + Random.Range(2.5f, 5f) * (1f - Wary * .4f);
            }
            bool wondering = !scared && now < t.turnUntil;
            if (t.marker) t.marker.text = wondering ? "?" : "✎";
            float speed = scared ? 5.6f + Wary * 1.2f : wondering ? 0f : 2.6f + Wary * 1.6f;
            Vector3 next = AvoidTrunks(t.root.position, t.root.position + t.heading * (speed * dt));
            Vector3 heading = next - t.root.position; t.root.position = next;
            // the torch swings from side to side as they walk, wider and quicker as they get wary
            t.sweep += dt * (1.2f + Wary * 1.8f);
            Vector3 look = scared ? -toHer.normalized : wondering ? (now < t.wonderUntil ? t.root.forward : Quaternion.Euler(0f, Mathf.Sin(now * 3f) * 25f, 0f) * t.wonder) : Quaternion.Euler(0f, Mathf.Sin(t.sweep) * (35f + Wary * 30f), 0f) * (heading.sqrMagnitude > .0001f ? heading.normalized : t.root.forward);
            t.root.rotation = Quaternion.Slerp(t.root.rotation, Quaternion.LookRotation(look), dt * 6f);
            t.torch.range = range + 4f;
            // the pool of light: an oval on the ground as long as the torch reaches, stopping short at a trunk
            Vector3 ahead = t.root.forward; ahead.y = 0f; ahead.Normalize();
            float reach = range;
            foreach (var trunk in trunks)
            {
                Vector3 to = trunk - t.root.position; to.y = 0f;
                float along = Vector3.Dot(to, ahead);
                if (along > .5f && along < reach && (to - ahead * along).sqrMagnitude < .8f * .8f) reach = along;
            }
            float wide = Mathf.Tan(t.torch.spotAngle * .5f * Mathf.Deg2Rad) * reach * 1.2f;
            t.pool.position = t.root.position + ahead * (reach * .5f + .4f) + Vector3.up * .03f;
            t.pool.rotation = Quaternion.LookRotation(ahead);
            t.pool.localScale = new Vector3(wide, .005f, reach);
            t.cone.localRotation = Quaternion.Euler(8f, 0f, 0f);
            t.cone.localScale = new Vector3(wide * .5f, wide * .5f, reach);
            t.rig.Play(clips.boarderWalk, false, scared ? 2.6f : 1.3f); t.rig.Tick(dt);
            if (t.carrying) t.paper.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            // in the torch's light, with no trunk between, or right beside one: she is seen
            float angle = Vector3.Angle(t.root.forward, toHer);
            bool lit = d < range && angle < t.torch.spotAngle * .5f && Clear(t.root.position, player.position);
            if (lit || d < 1.4f)
            {
                seen = true;
                if (!scared) { Sound.Play("race_warning"); t.scaredUntil = now + 2.5f; }
            }
            // behind one carrying a drawing, out of its light: hers to take back
            if (t.carrying && !lit && d < distance) { distance = d; nearest = t; }
        }
        Transform tree = null;
        foreach (var pinned in onTrees) { Vector3 to = pinned.position - player.position; to.y = 0f; if (to.magnitude < Reach + .8f) tree = pinned; }
        if (seen) { seenFor += dt; panic += dt * (.28f + Wary * .12f); botSeen += dt; }
        else { seenFor = 0f; panic = Mathf.Max(0f, panic - dt * .025f); }
        bool take = confirm || (!Botting && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (take && nearest != null) Recover(nearest);
        else if (take && tree != null) Unpin(tree);
        hint.text = L10n.T(nearest != null ? "forest.catch" : tree != null ? "forest.unpin" : "forest.controls");
        score.text = L10n.T("forest.score", recovered, Drawings);
        clock.text = L10n.T("forest.time", Mathf.CeilToInt(remaining), Mathf.RoundToInt(stamina * 100f));
        panicBar.rectTransform.sizeDelta = new Vector2(290f * Mathf.Clamp01(panic), 20f);
        seenVeil.color = new Color(1f, .85f, .55f, seen ? .12f + Mathf.Sin(now * 20f) * .04f : Mathf.MoveTowards(seenVeil.color.a, 0f, dt));
        foreach (var paper in returned) paper.position = Vector3.Lerp(paper.position, artTree.position + new Vector3(-2.6f + returned.IndexOf(paper) * .75f, 2.7f, -1.6f), dt * 5f);
        botPanic = Mathf.Max(botPanic, panic);
        if (recovered == Drawings) End(true);
        else if (panic >= 1f || remaining <= 0f) End(false);
    }

    // From a child's hands, from behind: it jumps, lets go of the page and runs; it stays in the forest, warier.
    void Recover(Thief thief)
    {
        if (!thief.carrying) return;
        thief.carrying = false; recovered++;
        if (thief.marker) thief.marker.gameObject.SetActive(false);
        thief.paper.SetParent(world, true); returned.Add(thief.paper);
        thief.scaredUntil = now + 3f;
        Sound.Play("wagongood");
    }

    void Unpin(Transform paper)
    {
        onTrees.Remove(paper);
        recovered++;
        paper.SetParent(world, true); returned.Add(paper);
        Sound.Play("wagongood");
    }

    void End(bool won)
    {
        if (Botting) BotEnd(won);
        phase = won ? Phase.Won : Phase.Lost;
        hero.Play(clips.idle); Sound.Play(won ? "wagongood" : "menuback");
        Dialog(won ? "forest.won" : "forest.lost", won ? "forest.won.text" : panic >= 1f ? "forest.lost.panic" : "forest.lost.text", "quiz.ok");
    }

    public void TestWin() { foreach (var t in thieves) Recover(t); foreach (var paper in onTrees.ToArray()) Unpin(paper); End(true); }

    // A mark over each child still carrying a drawing, kept on the screen's edge when out of sight.
    void LateUpdate()
    {
        if (!view || !hud) return;
        var rect = (RectTransform)hud.transform;
        foreach (var t in thieves)
        {
            if (!t.marker || !t.carrying) continue;
            Vector3 screen = view.WorldToViewportPoint(t.root.position + Vector3.up * 2.1f);
            if (screen.z < 0f) { screen.x = 1f - screen.x; screen.y = 0.1f; }
            t.marker.rectTransform.anchoredPosition = new Vector2((Mathf.Clamp(screen.x, .05f, .95f) - .5f) * rect.rect.width, (Mathf.Clamp(screen.y, .12f, .85f) - .5f) * rect.rect.height);
        }
    }

    void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        hero?.Free(); foreach (var t in thieves) t.rig?.Free();
        foreach (var m in materials) if (m) Destroy(m);
    }
}

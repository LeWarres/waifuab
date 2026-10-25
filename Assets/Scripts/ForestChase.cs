using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Faceless's last chapter: recover the drawings, without hurting their curious little thieves.
public class ForestChase : MonoBehaviour
{
    enum Phase { Intro, Playing, Won, Lost }
    class Thief { public Transform root, paper; public BrawlEvent.Rig rig; public Vector3 heading; public bool caught; public float nextTurn; public Text marker; }
    public float seconds = 90f;
    const int Count = 5;
    const float Edge = 21f;
    readonly List<Thief> thieves = new();
    readonly List<Material> materials = new();
    readonly List<Vector3> trunks = new();
    readonly List<Transform> returned = new();
    readonly List<Transform> pinned = new();
    BrawlEvent.Rig hero;
    BrawlClips clips;
    Transform world, player, artTree;
    Camera view;
    InputAction move;
    Canvas hud;
    Slanted card;
    Text title, instructions, button, score, clock, hint;
    Phase phase;
    float remaining, age, stamina = 1f, theft;
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
        BuildWorld(); BuildUI();
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

    void BuildWorld()
    {
        world = new GameObject("DrawingClearing").transform;
        var ground = Block("MossClearing", world, new Vector3(0f, -.2f, 0f), new Vector3(180f, .4f, 180f), Paint(new Color(.35f, .46f, .23f)));
        ground.GetComponent<Renderer>().sharedMaterial.mainTexture = Resources.Load<Texture2D>("Ground/jungle");
        ground.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(18f, 18f);
        var clearing = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        clearing.SetParent(world, false); Destroy(clearing.GetComponent<Collider>());
        clearing.localPosition = new Vector3(0f, .015f, 0f); clearing.localScale = new Vector3(47f, .025f, 47f);
        clearing.GetComponent<Renderer>().sharedMaterial = Paint(new Color(.43f, .48f, .29f));
        var trees = new[] { Resources.Load<GameObject>("Scenery/jungle/00_Broadleaf_Tree_01A"), Resources.Load<GameObject>("Scenery/jungle/01_Broadleaf_Tree_03A") };
        artTree = Prop(trees[0], new Vector3(0f, 0f, 5f), 9f);
        trunks.Add(artTree.position);
        for (int i = 0; i < Count; i++) pinned.Add(Paper(world, new Vector3(-1.2f + i * .6f, 2f + (i % 2) * .5f, 3.5f), i));
        for (int i = 0; i < 28; i++)
        {
            float a = i * Mathf.PI * 2f / 28f;
            Prop(trees[i % 2], new Vector3(Mathf.Sin(a) * 26f, 0f, Mathf.Cos(a) * 26f), Random.Range(7f, 10f));
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            Vector3 at = new Vector3(Mathf.Sin(a) * 14f, 0f, Mathf.Cos(a) * 14f);
            Prop(trees[i % 2], at, 6f); trunks.Add(at);
        }
        var bush = Resources.Load<GameObject>("Scenery/jungle/07_Flower_Bush_01A");
        for (int i = 0; i < 14; i++)
            Prop(bush, new Vector3(Random.Range(-23f, 23f), 0f, Random.Range(-23f, 23f)), .65f);
        player = new GameObject("FacelessArtist").transform;
        player.SetParent(world, false); player.position = new Vector3(0f, 0f, -5f);
        const string who = "Characters/faceless/faceless";
        var body = BrawlEvent.Body(Resources.Load<GameObject>(who), player, 2.3f);
        Player.Skin(body.transform, who);
        hero = new BrawlEvent.Rig(player, body); hero.Play(clips.idle);
        var coat = Paint(new Color(.25f, .62f, .85f));
        for (int i = 0; i < Count; i++)
        {
            var t = new GameObject("CuriousThief_" + i).transform;
            t.SetParent(world, false);
            float a = i * Mathf.PI * 2f / Count;
            t.position = new Vector3(Mathf.Sin(a) * 7f, 0f, Mathf.Cos(a) * 7f + 3f);
            var small = BrawlEvent.Body(clips.boarder, t, 1.1f);
            foreach (var r in small.GetComponentsInChildren<Renderer>()) { TrainSim.Toon(r); r.sharedMaterial = coat; }
            var rig = new BrawlEvent.Rig(t, small); rig.Play(clips.boarderIdle);
            var paper = Paper(t, new Vector3(0f, 1.35f, -.25f), i);
            paper.gameObject.SetActive(false);
            thieves.Add(new Thief { root = t, paper = paper, rig = rig, heading = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) });
        }
        view = Camera.main;
        view.orthographic = true; view.orthographicSize = 11.5f; view.farClipPlane = 180f;
        view.transform.position = player.position + new Vector3(0f, 19f, -15f);
        view.transform.LookAt(player.position + Vector3.forward * 2f);
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
        root.rotation = Quaternion.Euler(-45f, 0f, 0f);
        return root;
    }

    void BuildUI()
    {
        hud = UiKit.NewCanvas("ForestDrawings", 30);
        var top = UiKit.Persona(UiKit.Panel(hud.transform, new Vector2(0f, 460f), new Vector2(1680f, 100f), UiKit.Navy, UiKit.Navy, .04f));
        score = UiKit.Label(top.transform, UiKit.Display, 36, Color.white, TextAnchor.MiddleLeft, new Vector2(-420f, 0f), new Vector2(750f, 70f), true);
        clock = UiKit.Label(top.transform, UiKit.Body, 30, Color.white, TextAnchor.MiddleRight, new Vector2(480f, 0f), new Vector2(550f, 70f), true);
        score.text = L10n.T("forest.score", 0, Count);
        clock.text = L10n.T("forest.time", Mathf.CeilToInt(seconds), 100);
        hint = UiKit.Label(hud.transform, UiKit.Body, 25, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -460f), new Vector2(1750f, 90f), true);
        hint.text = L10n.T("forest.controls");
        hint.GetComponent<RectTransform>().SetAsLastSibling();
        card = UiKit.Persona(UiKit.Panel(hud.transform, Vector2.zero, new Vector2(1460f, 350f), UiKit.Navy, UiKit.Navy, .025f));
        title = UiKit.Label(card.transform, UiKit.Display, 48, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 108f), new Vector2(1300f, 80f), true);
        instructions = UiKit.Label(card.transform, UiKit.Body, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1320f, 140f), true);
        var action = UiKit.Paint(UiKit.Panel(card.transform, new Vector2(0f, -120f), new Vector2(520f, 60f), Color.white, Color.white, .2f), UiKit.Ink.Main);
        action.gameObject.AddComponent<Clicks>().click = _ => Confirm();
        button = UiKit.Label(action.transform, UiKit.Display, 32, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(490f, 55f), true);
        for (int i = 0; i < thieves.Count; i++)
        {
            thieves[i].marker = UiKit.Label(hud.transform, UiKit.Body, 36, UiKit.Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80f, 60f), true);
            thieves[i].marker.text = "!";
            thieves[i].marker.transform.SetAsFirstSibling();
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
        if (phase == Phase.Intro)
        {
            phase = Phase.Playing; theft = 1f; card.gameObject.SetActive(false);
            foreach (var t in thieves) t.paper.gameObject.SetActive(true);
            return;
        }
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
            if (away.sqrMagnitude < 1.7f * 1.7f) target = trunk + (away.sqrMagnitude < .01f ? (from - trunk).normalized : away.normalized) * 1.7f;
        }
        target.y = 0f;
        return Vector3.ClampMagnitude(target, Edge);
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
        if (phase != Phase.Playing || paused) { if (confirm) Confirm(); return; }
        if (theft > 0f)
        {
            theft -= dt;
            for (int i = 0; i < Count; i++)
            {
                pinned[i].position = Vector3.Lerp(pinned[i].position, thieves[i].paper.position, dt * 6f);
                if (theft <= 0f) pinned[i].gameObject.SetActive(false);
            }
            return;
        }
        remaining -= dt;
        var input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        bool sprint = stamina > .03f && ((k != null && k.leftShiftKey.isPressed) || (p != null && p.rightTrigger.isPressed));
        stamina = Mathf.Clamp01(stamina + dt * (sprint && input.sqrMagnitude > .1f ? -.3f : .2f));
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        player.position = AvoidTrunks(player.position, player.position + direction * ((sprint ? 11f : 7.5f) * dt));
        if (direction.sqrMagnitude > .01f) player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(direction), dt * 14f);
        hero.Play(direction.sqrMagnitude > .01f ? clips.run : clips.idle, false); hero.Tick(dt);
        view.transform.position = Vector3.Lerp(view.transform.position, player.position + new Vector3(0f, 19f, -15f), dt * 5f);
        Thief nearest = null; float distance = 2.1f;
        foreach (var t in thieves)
        {
            if (t.caught) continue;
            Vector3 away = t.root.position - player.position;
            if (Time.unscaledTime >= t.nextTurn)
            {
                t.nextTurn = Time.unscaledTime + Random.Range(.6f, 1.3f);
                t.heading = Quaternion.Euler(0f, Random.Range(-50f, 50f), 0f) * (away.magnitude < 8f ? away.normalized : t.heading);
                if (t.root.position.magnitude > Edge - 2f) t.heading = (-t.root.position.normalized + Vector3.Cross(Vector3.up, t.root.position.normalized) * .8f).normalized;
            }
            Vector3 next = AvoidTrunks(t.root.position, t.root.position + t.heading * (4.1f * dt));
            Vector3 heading = next - t.root.position; t.root.position = next;
            if (heading.sqrMagnitude > .001f) t.root.rotation = Quaternion.Slerp(t.root.rotation, Quaternion.LookRotation(heading), dt * 9f);
            t.rig.Play(clips.boarderWalk, false, 1.7f); t.rig.Tick(dt);
            t.paper.rotation = Quaternion.LookRotation(view.transform.forward) * Quaternion.Euler(90f, 0f, 0f);
            float d = Vector3.Distance(t.root.position, player.position);
            if (d < distance) { distance = d; nearest = t; }
        }
        if (nearest != null && (confirm || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))) Recover(nearest);
        hint.text = L10n.T(nearest != null ? "forest.catch" : "forest.controls");
        score.text = L10n.T("forest.score", recovered, Count);
        clock.text = L10n.T("forest.time", Mathf.CeilToInt(remaining), Mathf.RoundToInt(stamina * 100f));
        foreach (var paper in returned) paper.position = Vector3.Lerp(paper.position, new Vector3(-2f + returned.IndexOf(paper), 2.7f, 3.4f), dt * 5f);
        if (recovered == Count) End(true);
        else if (remaining <= 0f) End(false);
    }

    void Recover(Thief thief)
    {
        if (thief.caught) return;
        thief.caught = true; recovered++;
        if (thief.marker) thief.marker.gameObject.SetActive(false);
        thief.paper.SetParent(world, true); returned.Add(thief.paper);
        thief.rig.Play(clips.boarderIdle); Sound.Play("wagongood");
        // They hand it back and leave quietly. Nobody is attacked or hurt.
        thief.root.gameObject.SetActive(false);
    }

    void End(bool won)
    {
        phase = won ? Phase.Won : Phase.Lost;
        hero.Play(clips.idle); Sound.Play(won ? "wagongood" : "menuback");
        Dialog(won ? "forest.won" : "forest.lost", won ? "forest.won.text" : "forest.lost.text", "quiz.ok");
    }

    public void TestWin() { foreach (var t in thieves) Recover(t); End(true); }

    void LateUpdate()
    {
        if (!view || !hud) return;
        var rect = (RectTransform)hud.transform;
        foreach (var t in thieves)
        {
            if (!t.marker || t.caught) continue;
            Vector3 screen = view.WorldToViewportPoint(t.root.position + Vector3.up * 2.1f);
            t.marker.rectTransform.anchoredPosition = new Vector2((Mathf.Clamp(screen.x, .07f, .93f) - .5f) * rect.rect.width, (Mathf.Clamp(screen.y, .14f, .83f) - .5f) * rect.rect.height);
        }
    }

    void OnDestroy()
    {
        hero?.Free(); foreach (var t in thieves) t.rig?.Free();
        foreach (var m in materials) if (m) Destroy(m);
    }
}

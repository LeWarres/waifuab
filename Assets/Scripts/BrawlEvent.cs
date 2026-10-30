using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

// The boarding: robots jump onto an armed wagon and the hero goes in after them. The game is then seen from
// behind the hero, inside the wagon: a light combo, a heavy kick, a dodge and a grab into a throw (a suplex,
// a powerbomb...), until every boarder is down or the hero is. The boarders take turns, cover up, chain blows,
// get round behind her, and grab her too: a headlock she breaks by pressing fast, or a DDT. Outside, time
// stands still (the train, the enemies, the road) until it is over: won, and all goes on; lost (the hero down),
// and the boarders wreck a wagon. TrainSim starts it as the leg's event. The moves are clips of the two animation packs (BrawlClips), played through a small playable graph each.
public class BrawlEvent : MonoBehaviour
{
    public Vector2Int boarderCount = new Vector2Int(3, 4); // 3: a quick fight
    // Before the fight, seen from outside: the boarders ride in on speeders (Tools/make_speeder.py), two from one
    // side and one from the other, pull level with the wagon, the alarm goes and they leap onto it.
    public float approachSeconds = 2.8f, alarmSeconds = 1.6f;
    public float heroSpeed = 5.5f, dodgeSpeed = 12f, dodgeTime = 0.38f;
    public float reach = 1.9f, heavyReach = 2.5f, heavyCooldown = 1.2f, grabCooldown = 6f; // the grab's wait counts from the end of the throw
    // A boarder takes two throws at most, at any difficulty (a throw grows with its health); the hero's damage bonus
    // multiplies these.
    public float lightDamage = 12f, heavyDamage = 28f, throwDamage = 45f, boarderHealth = 80f;
    public float blow = 2.4f * Balance.Scale;  // a boarder's hit on the hero, grown with the difficulty like the enemies'
    public float slamBlows = 3.5f;             // the DDT at the end of a headlock not broken, in blows
    public float boarderSpeed = 2.8f;
    public int maxAttackers = 2;               // the others wait their turn around the hero
    public int moneyPerBoarder = 4;
    public float stickSpeed = 140f, mouseSpeed = 0.15f;
    [System.NonSerialized] public bool testAtStart = false; // testing aid: true makes the boarding open every run

    public bool Active { get; private set; }       // from the warning to the end
    public bool Incoming => Active && Time.unscaledTime < startsAt;
    public bool Fighting => Active && !Incoming;
    public bool Alarm => Incoming && Time.unscaledTime >= approachUntil; // they are alongside: the warning is up
    public Camera View => view;

    // The wagon's inside, built once far over the world, so nothing of the run shows through its windows but the
    // sky and a ground painting going by under them.
    static readonly Vector3 Inside = new Vector3(0f, 500f, 0f);
    const float HalfLength = 11f, HalfWidth = 3.8f, Height = 4.4f;
    static readonly Color[] Paint = // the boarders' colours
    {
        new Color(0.95f, 0.25f, 0.25f), new Color(0.2f, 0.75f, 1f), new Color(0.5f, 0.95f, 0.3f),
        new Color(0.7f, 0.35f, 1f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.9f, 0.25f), new Color(1f, 0.4f, 0.8f),
    };

    // What a body is doing. The hero: Free, Attacking, Kicking, Dodging, Hurt, Throwing, Held, Floored, Down. A boarder:
    // Free, Attacking, Grabbing, Holding (the hero in its headlock), Guarding, Hurt, Thrown, Down.
    const int Free = 0, Attacking = 1, Hurt = 2, Down = 3, Dodging = 4, Kicking = 5, Throwing = 6, Held = 7, Floored = 8,
              Guarding = 9, Grabbing = 10, Holding = 11, Thrown = 12, Rising = 13;

    // A humanoid body played by hand: a mixer of every clip it has used, fading to the one it plays now.
    public class Rig
    {
        public readonly Transform root;
        public readonly Animator animator;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly List<AnimationClip> clips = new();
        readonly List<AnimationClipPlayable> playing = new();
        int current = -1;
        float fade = 0.1f, speed = 1f, time;
        public AnimationClip Clip => clips[current];
        public float Done => time / Clip.length; // how far through the clip: 0 to 1 for a move that does not loop

        public Rig(Transform root, Animator animator)
        {
            this.root = root;
            this.animator = animator;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            graph = PlayableGraph.Create("Brawl");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual); // Tick: the fight runs on real time, and stops with the pause
            mixer = AnimationMixerPlayable.Create(graph, 0);
            AnimationPlayableOutput.Create(graph, "Body", animator).SetSourcePlayable(mixer);
        }

        // again false: carries on if it is already playing this one (a walk, an idle).
        public void Play(AnimationClip clip, bool again = true, float rate = 1f, float fadeIn = 0.1f)
        {
            int i = clips.IndexOf(clip);
            if (i < 0)
            {
                i = clips.Count;
                clips.Add(clip);
                playing.Add(AnimationClipPlayable.Create(graph, clip));
                mixer.SetInputCount(i + 1);
                graph.Connect(playing[i], 0, mixer, i);
                if (current < 0) mixer.SetInputWeight(i, 1f);
            }
            if (i == current && !again) return;
            current = i;
            fade = fadeIn;
            speed = rate;
            time = 0f;
            playing[i].SetTime(0d);
            playing[i].SetSpeed(rate);
        }

        public void Tick(float dt)
        {
            time += dt * speed;
            for (int i = 0; i < playing.Count; i++)
                mixer.SetInputWeight(i, Mathf.MoveTowards(mixer.GetInputWeight(i), i == current ? 1f : 0f, dt / Mathf.Max(fade, 0.01f)));
            graph.Evaluate(dt);
        }

        public void Free() { if (graph.IsValid()) graph.Destroy(); }
    }

    class Boarder
    {
        public Rig rig;
        public Material skin;
        public Color colour;
        public float health, until, nextAttack, side, riseAt; // riseAt: off the floor, getting up
        public int move;
        public bool landed;      // this attack's blow is dealt
        public bool chain;       // a second blow follows this one if it lands
        public Vector3 push;     // knocked back, sliding to a stop
        public int taken;        // hits taken in a row (each within a moment of the last)
        public float takenAt, armourUntil; // the last of them; until when hits no longer stagger it
        public Vector3 Position { get => rig.root.position; set => rig.root.position = value; }
    }

    TrainSim train;
    BrawlClips moves;
    InputAction move, look;
    Camera view;
    Transform room, ground;
    Material groundPaint;
    readonly List<Rect> crates = new(); // what nobody walks through, on the floor (x, z about the room's middle)
    Rig hero;
    Transform heroBody;  // her model, under the root: grown back to an android's size while in a throw or a hold
    Vector3 heroSize;
    readonly List<Boarder> boarders = new();
    Canvas board;
    Text counter, hint;
    int wagon, kills, heroMove, combo;
    bool queued, heroLanded;
    float startsAt, heroUntil, safeUntil, kickReady, endsAt, yaw, pitch;
    Boarder partner;                     // the one in the hero's throw, or holding her
    int throwPick, holdPhase;
    float level = 1f;                    // the boarders' health against the difficulty's
    float nextStrike, nextGrab, grabReady, stopUntil, mash, mashUntil, distance = 5.2f;
    Slanted mashPanel, mashFill, mashTime;
    Text mashText;
    Vector3 dodgeWay;
    string ending;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        move = InputSystem.actions.FindAction("Player/Move", true);
        look = InputSystem.actions.FindAction("Player/Look", true);
        moves = Resources.Load<BrawlClips>("Brawl/BrawlClips");

        view = new GameObject("BrawlCamera").AddComponent<Camera>();
        view.fieldOfView = 62f;
        view.nearClipPlane = 0.1f;
        view.depth = 10f;
        // No motion blur in here: with time stopped outside, HDRP's last-frame history stalls and smears everything.
        var hd = view.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        hd.customRenderingSettings = true;
        hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur] = true;
        hd.renderingPathCustomFrameSettings.SetEnabled(UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur, false);
        view.enabled = false;

        board = UiKit.NewCanvas("Brawl", 6);
        // Persona plates: the count up top, the moves (their buttons drawn) at the foot, the headlock's panel.
        Slanted bar = UiKit.Plate(board.transform, new Vector2(0f, -70f), new Vector2(560f, 56f), 0.12f, 4f, 8f);
        UiKit.Root(bar).anchorMin = UiKit.Root(bar).anchorMax = new Vector2(0.5f, 1f);
        counter = UiKit.Label(bar.transform, UiKit.Display, 32, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(540f, 48f), true);
        Slanted help = UiKit.Plate(board.transform, new Vector2(0f, 64f), new Vector2(900f, 52f), 0.12f, 4f, 8f);
        UiKit.Root(help).anchorMin = UiKit.Root(help).anchorMax = new Vector2(0.5f, 0f);
        hint = UiKit.Label(help.transform, UiKit.Body, 20, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(740f, 34f), true);
        hint.enabled = false;
        hintLine = Prompt.Make(help.transform, Vector2.zero, 21, Color.white);
        // Breaking a headlock: the button to press, how far along, how long is left.
        Slanted mash = UiKit.Plate(board.transform, new Vector2(0f, 230f), new Vector2(620f, 120f), 0.12f, 5f, 12f);
        mashPanel = UiKit.Root(mash).GetComponent<Slanted>();
        mashText = UiKit.Label(mash.transform, UiKit.Display, 38, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 24f), new Vector2(600f, 56f), true);
        mashText.enabled = false;
        mashLine = Prompt.Make(mash.transform, new Vector2(0f, 24f), 36, Color.white, UiKit.Display);
        UiKit.Panel(mash.transform, new Vector2(0f, -20f), new Vector2(484f, 22f), new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0.6f), 0f).sprite = null;
        mashFill = UiKit.Paint(UiKit.Panel(mash.transform, new Vector2(-240f, -20f), new Vector2(0f, 18f), Color.white, Color.white, 0f), UiKit.Ink.Main);
        mashTime = UiKit.Panel(mash.transform, new Vector2(-240f, -42f), new Vector2(480f, 6f), UiKit.Rose, UiKit.Rose, 0f);
        mashFill.sprite = mashTime.sprite = null;
        mashFill.rectTransform.pivot = mashTime.rectTransform.pivot = new Vector2(0f, 0.5f);
        mashPanel.gameObject.SetActive(false);
        board.gameObject.SetActive(false);
    }

    void Start()
    {
        if (testAtStart) Invoke(nameof(TestBegin), 1f);
    }

    void TestBegin() => Begin();

    // False if there is nothing to play it with: no armed wagon left, or the clips are missing.
    // The story duel's driver (RivalEvent): the rival hero leaps from its engine onto one of our wagons and fights the
    // hero alone in there, far tougher than a boarder. done gets whether the hero won.
    public float driverToughness = 2.5f;
    public bool DriverFight { get; private set; }
    System.Action<bool> driverDone;
    Vector3 driverFrom;
    readonly Dictionary<string, Boarder> drivers = new();
    readonly List<Boarder> crew = new();     // the boarders, put aside while the driver fights
    Transform leaper;                        // the driver in mid air, seen from outside
    Vector3 leaperHips;

    public bool BeginDriver(int rival, Vector3 from, System.Action<bool> done)
    {
        if (!moves || !train.Player.Alive) return false;
        int alive = 0;
        wagon = -1;
        for (int i = 0; i < train.CarCount; i++) if (train.CarAlive(i) && Random.Range(0, ++alive) == 0) wagon = i;
        if (wagon < 0) return false;
        if (!room) Build();
        string id = Locomotives.HeroId(rival);
        if (!drivers.TryGetValue(id, out Boarder driver)) drivers[id] = driver = NewDriver(id);
        if(Story.ShadowRival) Locomotives.Shadow(driver.rig.root);
        crew.Clear();
        crew.AddRange(boarders);
        foreach (Boarder b in boarders) b.rig.root.gameObject.SetActive(false);
        boarders.Clear();
        boarders.Add(driver);
        DriverFight = true;
        speeders.Clear(); // it leaps across: no speeders (nor any left over from a boarding)
        driverDone = done;
        driverFrom = from;
        // In mid air on the way over: the same character, standing, nothing animates it.
        if (leaper) Destroy(leaper.gameObject);
        leaper = new GameObject("Leaper").transform;
        string who = "Characters/" + id + "/" + id;
        Animator rig = Body(Resources.Load<GameObject>(who), leaper);
        Player.Skin(rig.transform, who);
        if(Story.ShadowRival) Locomotives.Shadow(leaper);
        rig.enabled = false;
        leaperHips = leaper.InverseTransformPoint(rig.GetBoneTransform(HumanBodyBones.Hips).position);
        return Begin(true);
    }

    // The driver: the rival hero's own model, in its own colours (a copy of its skin, so its glow is its own).
    Boarder NewDriver(string id)
    {
        var root = new GameObject("Driver").transform;
        root.SetParent(room, false);
        string who = "Characters/" + id + "/" + id;
        Animator body = Body(Resources.Load<GameObject>(who), root);
        Player.Skin(body.transform, who);
        Renderer[] parts = body.GetComponentsInChildren<Renderer>();
        var skin = new Material(parts[0].sharedMaterial);
        foreach (Renderer part in parts) part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => skin);
        return new Boarder { rig = new Rig(root, body), colour = Color.white, skin = skin };
    }

    public bool Begin() => Begin(false);

    bool Begin(bool driverFight)
    {
        if (!driverFight)
        {
            DriverFight = false;
            if (!moves || !train.Player.Alive || !train.Gunner(out wagon)) return false;
        }
        if (!room) Build();
        Active = true;
        ending = null;
        kills = 0;
        approachUntil = Time.unscaledTime + approachSeconds;
        startsAt = approachUntil + alarmSeconds;
        groundPaint.mainTexture = Resources.Load<Texture2D>("Ground/" + train.BiomeId);

        // The hero comes in at the back end, the boarders spread through the rest of the wagon.
        hero.root.position = Inside + new Vector3(-HalfLength + 2f, 0f, 0f);
        hero.root.rotation = Quaternion.Euler(0f, 90f, 0f);
        heroMove = Free;
        partner = null;
        safeUntil = grabReady = 0f;
        nextStrike = startsAt + 1f;
        nextGrab = startsAt + 4f;
        hero.Play(moves.idle);
        yaw = 90f;
        pitch = 20f;
        int count = driverFight ? 1 : Random.Range(boarderCount.x, boarderCount.y);
        Mount(driverFight ? 0 : count);
        while (boarders.Count < count) boarders.Add(NewBoarder(boarders.Count));
        level = 1f + Combat.Instance.healthPerLevel * train.Difficulty;
        for (int i = 0; i < boarders.Count; i++)
        {
            Boarder b = boarders[i];
            b.rig.root.gameObject.SetActive(i < count);
            if (i >= count) continue;
            b.health = boarderHealth * level * (driverFight ? driverToughness : 1f);
            b.move = Free;
            b.push = Vector3.zero;
            b.side = Random.value < 0.5f ? -1f : 1f;
            b.nextAttack = startsAt + 1f + Random.value * 1.5f;
            b.Position = Inside + Clear(new Vector3(Random.Range(-1f, HalfLength - 1.5f), 0f, Random.Range(-HalfWidth + 0.8f, HalfWidth - 0.8f)), 0.5f);
            b.rig.root.rotation = Quaternion.Euler(0f, -90f, 0f);
            b.rig.Play(moves.boarderIdle);
            TrainSim.Tint(b.skin, b.colour);
        }
        return true;
    }

    Boarder NewBoarder(int i)
    {
        var root = new GameObject("Boarder").transform;
        root.SetParent(room, false);
        Animator body = Body(moves.boarder, root, out pairScale);
        var b = new Boarder { rig = new Rig(root, body), colour = Paint[i % Paint.Length] };
        b.skin = TrainSim.Mat(b.colour);
        foreach (Renderer part in body.GetComponentsInChildren<Renderer>())
            part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => b.skin);
        return b;
    }

    // A humanoid model under root, its feet on root and about this tall. (scale: how much it was grown.)
    // The heroes come long-legged: 1.6 puts them about level with the androids at 1.8.
    public static Animator Body(GameObject model, Transform root, float height = 1.8f) => Body(model, root, out _, height);
    public static Animator Body(GameObject model, Transform root, out float scale, float height = 1.8f)
    {
        Transform body = Instantiate(model, root).transform;
        body.localPosition = Vector3.zero;
        body.localRotation = Quaternion.identity;
        Animator rig = body.GetComponentInChildren<Animator>();
        float tall = rig.GetBoneTransform(HumanBodyBones.Head).position.y - rig.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
        scale = height / (tall * 1.12f);
        body.localScale *= scale;
        return rig;
    }

    // ---------------------------------------------------------------- the wagon's inside
    void Build()
    {
        room = new GameObject("WagonInside").transform;
        room.position = Inside;
        // A freight wagon fitted out: boards underfoot, wood panelling up to the windows and riveted green steel over
        // them, a roof of dark boards on steel ribs, three lamps hanging from it (real lights, shadows and all), doors
        // at the ends inside hazard stripes, and its cargo along the walls: crates, a stack under a tarp, barrels,
        // sacks. The sun still comes in through the windows and the roof; the lamps add their warm pools over it.
        float length = HalfLength * 2f, width = HalfWidth * 2f;
        Material floor = Interior.Textured(Interior.Planks(), new Color(0.72f, 0.5f, 0.32f), new Vector2(length / 3f, width / 1.9f));
        Material panel = Interior.Textured(Interior.Panels(), new Color(0.6f, 0.4f, 0.25f), new Vector2(length / 1.6f, 1f));
        Material plate = Interior.Textured(Interior.Riveted(), new Color(0.36f, 0.56f, 0.5f), new Vector2(length / 2.4f, 1f));
        Material endPlate = Interior.Textured(Interior.Riveted(), new Color(0.36f, 0.56f, 0.5f), new Vector2(width / 2.4f, Height / 2.4f));
        Material roofBoards = Interior.Textured(Interior.Planks(), new Color(0.38f, 0.27f, 0.2f), new Vector2(length / 3f, width / 1.9f));
        Material steel = TrainSim.Mat(new Color(0.42f, 0.45f, 0.5f)), steelDark = TrainSim.Mat(new Color(0.2f, 0.21f, 0.24f));
        Material brass = TrainSim.Mat(new Color(1f, 0.78f, 0.35f)), postWood = TrainSim.Mat(new Color(0.38f, 0.24f, 0.14f));
        Material yellow = TrainSim.Mat(new Color(1f, 0.78f, 0.1f), false), black = TrainSim.Mat(new Color(0.08f, 0.08f, 0.09f), false);
        Material glass = TrainSim.Mat(new Color(0.6f, 0.85f, 1f) * 1.6f, false);

        Interior.Box(room, new Vector3(0f, -0.1f, 0f), new Vector3(length, 0.2f, width), floor);
        foreach (float side in new[] { -1f, 1f }) // steel kick plates where the floor meets the walls
            Interior.Box(room, new Vector3(0f, 0.04f, side * (HalfWidth - 0.12f)), new Vector3(length, 0.08f, 0.24f), steel);
        // The roof: boards on ribs, a knee brace where each rib meets the wall.
        Interior.Box(room, new Vector3(0f, Height + 0.1f, 0f), new Vector3(length + 0.4f, 0.2f, width + 0.4f), roofBoards);
        for (int k = 0; k <= 8; k++)
        {
            float x = -HalfLength + k * length / 8f;
            Interior.Box(room, new Vector3(x, Height - 0.12f, 0f), new Vector3(0.22f, 0.24f, width), steelDark);
            foreach (float side in new[] { -1f, 1f })
            {
                Transform brace = Interior.Box(room, new Vector3(x, Height - 0.45f, side * (HalfWidth - 0.35f)), new Vector3(0.16f, 0.9f, 0.16f), steelDark);
                brace.localRotation = Quaternion.Euler(side * 45f, 0f, 0f);
                Interior.Box(room, new Vector3(x, (2.7f + Height) * 0.5f, side * (HalfWidth - 0.02f)), new Vector3(0.18f, Height - 2.7f, 0.1f), steelDark); // the rib down the wall
            }
        }
        // The sides: panelling up to the windows, a brass rail, posts between the windows, riveted plate over them.
        const int Windows = 7;
        foreach (float side in new[] { -1f, 1f })
        {
            float z = side * (HalfWidth + 0.1f);
            Interior.Box(room, new Vector3(0f, 0.65f, z), new Vector3(length, 1.3f, 0.2f), panel);
            Interior.Box(room, new Vector3(0f, (2.7f + Height) * 0.5f, z), new Vector3(length, Height - 2.7f, 0.2f), plate);
            for (int k = 0; k <= Windows; k++)
                Interior.Box(room, new Vector3(-HalfLength + k * length / Windows, 2f, z), new Vector3(0.7f, 1.4f, 0.22f), postWood);
            foreach (float y in new[] { 1.33f, 2.68f }) Interior.Box(room, new Vector3(0f, y, z - side * 0.12f), new Vector3(length, 0.08f, 0.08f), y < 2f ? brass : steel);
        }
        // The ends: riveted steel, a door with a lit window, hazard stripes round it.
        foreach (float end in new[] { -1f, 1f })
        {
            Interior.Box(room, new Vector3(end * (HalfLength + 0.1f), Height * 0.5f, 0f), new Vector3(0.2f, Height, width), endPlate);
            Interior.Box(room, new Vector3(end * HalfLength, 1.15f, 0f), new Vector3(0.1f, 2.3f, 1.3f), steel);
            Interior.Box(room, new Vector3(end * (HalfLength - 0.06f), 1.6f, 0f), new Vector3(0.05f, 0.6f, 0.8f), glass, 0f, false);
            for (int k = 0; k < 9; k++) // the stripes: up both sides of the door and across its top
            {
                Material stripe = k % 2 == 0 ? yellow : black;
                foreach (float s in new[] { -1f, 1f })
                    Interior.Box(room, new Vector3(end * (HalfLength - 0.04f), 0.15f + k * 0.28f, s * 0.82f), new Vector3(0.04f, 0.28f, 0.22f), stripe, 0f, false);
            }
            Interior.Box(room, new Vector3(end * (HalfLength - 0.9f), 0.012f, 0f), new Vector3(1.4f, 0.02f, 1.8f), yellow, 0f, false); // the floor in front of it, marked
            for (int k = 0; k < 5; k++)
                Interior.Box(room, new Vector3(end * (HalfLength - 0.9f), 0.024f, -0.72f + k * 0.36f), new Vector3(1.4f, 0.02f, 0.14f), black, 0f, false);
        }
        // The lamps: hanging from the roof, each a real warm light with shadows.
        foreach (float x in new[] { -HalfLength * 0.6f, 0f, HalfLength * 0.6f })
        {
            Interior.Place("lamp", room, new Vector3(x, Height - 2.28f * 0.6f, 0f), 0f, 0.6f);
            Interior.Lamp(room, new Vector3(x, Height - 1.4f, 0f), new Color(1f, 0.8f, 0.55f), 260000f, 11f, true);
        }
        // The cargo, along the walls: cover, and in the way (each one's footprint blocks the floor).
        void Cargo(string kit, Vector3 at, float turn, float scale, float wide, float deep)
        {
            Interior.Place(kit, room, at, turn, scale);
            crates.Add(new Rect(at.x - wide * 0.5f, at.z - deep * 0.5f, wide, deep));
        }
        Cargo("stack", new Vector3(-6.5f, 0f, HalfWidth - 0.8f), 0f, 1f, 2.4f, 1.4f);
        Cargo("crate", new Vector3(-1.5f, 0f, -HalfWidth + 0.75f), 8f, 1.1f, 1.45f, 1.45f);
        Interior.Place("crate", room, new Vector3(-1.45f, 1.2f, -HalfWidth + 0.75f), -12f, 0.7f); // a small one on top
        Cargo("barrel", new Vector3(3.5f, 0f, HalfWidth - 0.65f), 0f, 1f, 1.1f, 1.1f);
        Cargo("barrel", new Vector3(4.65f, 0f, HalfWidth - 0.65f), 40f, 1f, 1.1f, 1.1f);
        Cargo("sacks", new Vector3(8.3f, 0f, -HalfWidth + 0.6f), 0f, 1f, 2.6f, 1f);
        Cargo("barrel", new Vector3(-HalfLength + 0.9f, 0f, -HalfWidth + 0.7f), 0f, 1f, 1.1f, 1.1f);
        Cargo("crate", new Vector3(HalfLength - 1f, 0f, HalfWidth - 0.75f), -6f, 1.1f, 1.45f, 1.45f);
        // Outside: the ground going by, under the windows.
        ground = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        Destroy(ground.GetComponent<Collider>());
        ground.SetParent(room, false);
        ground.localPosition = new Vector3(0f, -1.6f, 0f);
        ground.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ground.localScale = Vector3.one * 900f;
        groundPaint = TrainSim.Mat(Color.white, false);
        groundPaint.mainTextureScale = new Vector2(90f, 90f);
        var paint = ground.GetComponent<Renderer>();
        paint.sharedMaterial = groundPaint;
        paint.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var heroRoot = new GameObject("BrawlHero").transform;
        heroRoot.SetParent(room, false);
        string who = "Characters/" + Locomotives.Hero + "/" + Locomotives.Hero;
        Animator body = Body(Resources.Load<GameObject>(who), heroRoot, 1.6f);
        Player.Skin(body.transform, who);
        hero = new Rig(heroRoot, body);
        heroBody = body.transform;
        heroSize = heroBody.localScale;
        room.gameObject.SetActive(false);
    }

    readonly Dictionary<Color, Material> paints = new();
    void Block(Vector3 at, Vector3 size, Color colour, bool outline = true)
    {
        Transform block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        Destroy(block.GetComponent<Collider>());
        block.SetParent(room, false);
        block.localPosition = at;
        block.localScale = size;
        if (!paints.TryGetValue(colour, out Material paint)) paints[colour] = paint = TrainSim.Mat(colour, outline);
        var look = block.GetComponent<Renderer>();
        look.sharedMaterial = paint;
        look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // A spot on the floor (about the room's middle) moved out of the walls and crates, keeping room around it.
    Vector3 Clear(Vector3 at, float margin)
    {
        at.x = Mathf.Clamp(at.x, -HalfLength + margin, HalfLength - margin);
        at.z = Mathf.Clamp(at.z, -HalfWidth + margin, HalfWidth - margin);
        foreach (Rect c in crates)
        {
            if (at.x <= c.xMin - margin || at.x >= c.xMax + margin || at.z <= c.yMin - margin || at.z >= c.yMax + margin) continue;
            // Out by the nearest side.
            float left = at.x - (c.xMin - margin), right = c.xMax + margin - at.x, back = at.z - (c.yMin - margin), front = c.yMax + margin - at.z;
            float least = Mathf.Min(Mathf.Min(left, right), Mathf.Min(back, front));
            if (least == left) at.x = c.xMin - margin;
            else if (least == right) at.x = c.xMax + margin;
            else if (least == back) at.z = c.yMin - margin;
            else at.z = c.yMax + margin;
        }
        return at;
    }

    void Place(Transform body, Vector3 to) { to.y = Inside.y; body.position = Inside + Clear(to - Inside, 0.85f); } // on the floor, inside, with room for the arms

    // ---------------------------------------------------------------- the fight
    // How hard it is grows with the difficulty: blows come closer together, the boarders cover up and chain hits
    // more, and a headlock takes more presses to break.
    float Hardness => Mathf.Clamp01(train.Difficulty / 30f);
    float StrikeGap => Mathf.Lerp(0.9f, 0.4f, Hardness);      // seconds between two boarders starting an attack
    float GuardChance => Mathf.Lerp(0.35f, 0.6f, Hardness) + (DriverFight ? 0.2f : 0f); // a boarder in front covering up from a light swing
    // No locking one down with the combo: this many hits in a row and it bursts free (a shove that knocks the hero
    // back) and shrugs off staggering for a moment. Mashing into a guard is punished: it answers with a quick blow.
    int BurstAfter => DriverFight ? 3 : 4;
    const float BurstArmour = 1.4f, RiposteRate = 1.6f;
    float ChainChance => Mathf.Lerp(0.25f, 0.6f, Hardness);   // a second blow straight after the first
    float Rest => Random.Range(1f, 2f) * Mathf.Lerp(1f, 0.6f, Hardness); // a boarder's own pause after attacking
    int MashNeed => 9 + Mathf.Min(11, train.Difficulty / 3);

    void Update()
    {
        // Testing aid: M on the keyboard starts a boarding on the spot.
        if (!Active && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame && !train.Paused && !train.AtStation && !train.InEvent && !train.BossFight) Begin();
        if (!Active || train.Paused) return;
        if (train.AtStation) { End(null); return; }
        if (Incoming) { Arrive(); return; }
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f), now = Time.unscaledTime;
        if (!view.enabled)
        {
            view.enabled = true;
            room.gameObject.SetActive(true);
            foreach (Rider r in riders) r.body.gameObject.SetActive(false);
            if (leaper) leaper.gameObject.SetActive(false);
            board.gameObject.SetActive(true);
            Time.timeScale = 0f; // everything outside waits: the fight runs on real time
        }
        // The effects in here play by hand: with time stopped outside they would stand still.
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (!effects[i].graph || now > effects[i].until) { effects.RemoveAt(i); continue; }
            effects[i].graph.Simulate(dt);
        }

        if (ending != null)
        {
            if (now >= endsAt) End(ending);
        }
        else if (!train.CarAlive(wagon)) Finish("brawl.lost", now);
        else if (!train.Player.Alive && heroMove != Down)
        {
            if (partner != null) { Settle(partner.rig, moves.boarderIdle); partner.move = Free; partner = null; }
            heroMove = Down;
            hero.Play(moves.down);
            // Lost: the boarders wreck a wagon, any one still standing.
            int alive = 0, wreck = -1;
            for (int i = 0; i < train.CarCount; i++) if (train.CarAlive(i) && Random.Range(0, ++alive) == 0) wreck = i;
            if (wreck >= 0) train.HurtCar(wreck, DriverFight ? 0.5f : 1f); // the driver comes back more than once: half a wagon
            Finish("brawl.lost", now);
        }

        HeroUpdate(dt, now);
        if (heroMove == Held) Hold(dt, now);
        // The nearest one leads, the next tries to get round behind, the rest wait their turn further off.
        var order = boarders.FindAll(b => b.rig.root.gameObject.activeSelf && b.move != Down);
        order.Sort((a, c) => Flat(a.Position - hero.root.position).sqrMagnitude.CompareTo(Flat(c.Position - hero.root.position).sqrMagnitude));
        int attacking = order.FindAll(b => b.move == Attacking || b.move == Grabbing || b.move == Holding).Count;
        for (int i = 0; i < order.Count; i++) BoarderUpdate(order[i], i, ref attacking, dt, now);
        // Nobody stands inside anybody else (those locked in a hold or a throw excepted).
        for (int i = 0; i < order.Count; i++)
        {
            if (Paired(order[i])) continue;
            Vector3 apart = Flat(order[i].Position - hero.root.position);
            if (apart.sqrMagnitude < 0.81f && heroMove != Held && heroMove != Throwing) Place(order[i].rig.root, hero.root.position + apart.normalized * 0.9f);
            for (int j = i + 1; j < order.Count; j++)
            {
                if (Paired(order[j])) continue;
                Vector3 gap = Flat(order[j].Position - order[i].Position);
                if (gap.sqrMagnitude < 0.64f) Place(order[j].rig.root, order[i].Position + (gap.sqrMagnitude > 0.0001f ? gap.normalized : Vector3.right) * 0.8f);
            }
        }
        if (ending == null && order.Count == 0) Finish("brawl.won", now);

        // A blow that lands holds everything still for a moment: it gives the hits their weight.
        float play = now < stopUntil ? dt * 0.12f : dt;
        // The paired moves are made for two bodies of a size: in one, she is the androids' size (1.8 against her 1.6).
        bool paired = heroMove == Throwing || heroMove == Held;
        heroBody.localScale = Vector3.MoveTowards(heroBody.localScale, heroSize * (paired ? 1.8f / 1.6f : 1f), heroSize.x * dt);
        hero.Tick(play);
        foreach (Boarder b in boarders) if (b.rig.root.gameObject.activeSelf) b.rig.Tick(play);
        Follow(dt);
        counter.text = L10n.T("brawl.hud", order.Count, Mathf.RoundToInt(train.Player.Health / train.Player.maxHealth * 100f));
        // The grab's key shows its wait while it cannot be used.
        float wait = grabReady - now;
        string grabKey = heroMove != Throwing && wait > 0f ? Key(3) + " " + Mathf.CeilToInt(wait) + "s" : Key(3);
        hintLine.Set(L10n.T("brawl.hint", Key(0), Key(1), Key(2), grabKey));
        bool mashing = heroMove == Held && holdPhase == 1;
        if (mashPanel.gameObject.activeSelf != mashing) mashPanel.gameObject.SetActive(mashing);
        if (mashing)
        {
            mashLine.Set(Prompt.Upper(L10n.T("brawl.mash", Key(2))));
            mashFill.rectTransform.sizeDelta = new Vector2(480f * Mathf.Clamp01(mash / MashNeed), 16f);
            mashTime.rectTransform.sizeDelta = new Vector2(480f * Mathf.Clamp01((mashUntil - now) / MashSeconds), 6f);
            mashPanel.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(now * 30f)); // shaking with the struggle
        }
    }

    // The button for a move, as the device in use names it: 0 combo, 1 kick, 2 dodge (and breaking a hold), 3 grab.
    // A move's button, as a glyph to draw (Prompt): "[J]", "[□]", "[xA]"...
    Prompt hintLine, mashLine; // the moves and the headlock's line, their buttons drawn

    string Key(int which)
    {
        bool pad = train.Player.UsingGamepad, ps = Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad;
        if (!pad) return "[" + (which == 0 ? "J" : which == 1 ? "K" : which == 2 ? L10n.T("key.space") : "L") + "]";
        return "[" + (which == 0 ? (ps ? "□" : "xX") : which == 1 ? (ps ? "△" : "xY") : which == 2 ? (ps ? "✕" : "xA") : (ps ? "○" : "xB")) + "]";
    }

    // An effect in here: time stands still outside, so it is played by hand, on real time.
    readonly List<(UnityEngine.VFX.VisualEffect graph, float until)> effects = new();
    readonly HashSet<ParticleSystem> unscaled = new(); // the pool's copies set to real time (each once)
    void Effect(string name, Vector3 at, float size)
    {
        Combat combat = Combat.Instance;
        GameObject copy = combat.Burst(name, at, size);
        if (!copy) return;
        foreach (var graph in copy.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>()) effects.Add((graph, Time.unscaledTime + 3f));
        foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>())
        {
            var main = system.main;
            main.useUnscaledTime = true;
            unscaled.Add(system);
        }
    }

    void Finish(string key, float now)
    {
        ending = key;
        endsAt = now + 1.6f;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    static bool Paired(Boarder b) => b.move == Thrown || b.move == Holding;
    bool Standing(Boarder b) => b.rig.root.gameObject.activeSelf && b.move != Down && !Paired(b);

    Boarder Nearest(float within, Vector3 from)
    {
        Boarder best = null;
        float least = within * within;
        foreach (Boarder b in boarders)
        {
            if (!Standing(b)) continue;
            float d = Flat(b.Position - from).sqrMagnitude;
            if (d < least) { least = d; best = b; }
        }
        return best;
    }

    // Two bodies in one move, spaced as the pack's demo scene has each pair (Assets/Wrestler_Finishers_Volume2's
    // Demo, before it was trimmed): the one it is done to stands this far off the doer, in the doer's own frame
    // (x right, z ahead), turned to face it. Each clip's root is its body's centre at the start (BrawlClipsSetup).
    static readonly Dictionary<string, Vector3> PairSpacing = new()
    {
        { "Wrestling_Paired_BackControl_Suplex_Att", new Vector3(0.035f, 0f, -0.457f) },
        { "Wrestling_Paired_HipThrow_Att", new Vector3(-0.02f, 0f, 0.834f) },
        { "Wrestling_Paired_PowerBomb_Att", new Vector3(0f, 0f, 1.271f) },
        { "Wrestling_Paired_Underarm_FlipToss_Att", new Vector3(-0.027f, 0f, 0.837f) },
        { "Wrestling_Paired_BehindScoopSlam_Att", new Vector3(-0.027f, 0f, 0.837f) },
        { "Wrestling_Paired_HeadLock_Start_Att", new Vector3(-0.126f, 0f, 0.981f) },
        { "Wrestling_Paired_HeadLock_Loop_Att", new Vector3(0.122f, 0f, 0.469f) },
        { "Wrestling_Paired_HeadLock_End_Att", new Vector3(0.122f, 0f, 0.404f) },
        { "Wrestling_Paired_HeadLock_DDT_Att", new Vector3(0.122f, 0f, 0.469f) },
    };
    static float pairScale = 1f; // the androids' size here against the demo's
    const float PairRoom = 1.4f;  // how far off the walls and crates a pair keeps

    void Pair(Rig by, Rig of, AnimationClip byClip, AnimationClip ofClip, float fade = 0.12f, float rate = 1f)
    {
        // The doer's spot is where its body is now (out of one move into the next, it has moved off its root).
        Vector3 hips = by.animator.GetBoneTransform(HumanBodyBones.Hips).position;
        by.root.position = new Vector3(hips.x, by.root.position.y, hips.z);
        Vector3 spacing = by.root.rotation * (PairSpacing.TryGetValue(byClip.name, out Vector3 s) ? s : new Vector3(0f, 0f, 0.8f)) * pairScale;
        // Both well inside the wagon, the move swinging bodies about: the pair is shifted off a wall or a crate as one.
        by.root.position = Inside + Clear(by.root.position - Inside, PairRoom);
        Vector3 other = by.root.position + spacing, kept = Inside + Clear(other - Inside, PairRoom);
        by.root.position = Inside + Clear(by.root.position + kept - other - Inside, PairRoom);
        of.root.SetPositionAndRotation(by.root.position + spacing, by.root.rotation * Quaternion.Euler(0f, 180f, 0f));
        by.Play(byClip, true, rate, fade);
        of.Play(ofClip, true, rate, fade);
    }
    // The wrestling clips are slow for a fight this quick: the throws, the DDT and getting up are played faster.
    const float ThrowRate = 1.7f, SlamRate = 1.3f, GetUpRate = 2.5f;
    float GetUpTime => moves.getUp.length / GetUpRate;

    // Out of a paired move: the body's spot becomes where the clip left it, then the next clip at once (a fade would
    // drag the body back towards the old spot).
    void Settle(Rig r, AnimationClip next)
    {
        Vector3 hips = r.animator.GetBoneTransform(HumanBodyBones.Hips).position;
        r.root.position = Inside + Clear(new Vector3(hips.x, Inside.y, hips.z) - Inside, 1f); // lying, a body reaches further
        r.Play(next, true, next == moves.getUp ? GetUpRate : 1f, 0.01f);
    }

    // The hero can be hit or grabbed: on her feet and not in the middle of a dodge, a throw or a hold.
    bool Open(float now) => (heroMove == Free || heroMove == Attacking || heroMove == Kicking || heroMove == Hurt) && now >= safeUntil && ending == null;

    void HeroUpdate(float dt, float now)
    {
        Gamepad pad = Gamepad.current;
        Keyboard keys = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool light = (pad != null && pad.buttonWest.wasPressedThisFrame) || (keys != null && keys.jKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool heavy = (pad != null && pad.buttonNorth.wasPressedThisFrame) || (keys != null && keys.kKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame);
        bool dodge = (pad != null && pad.buttonSouth.wasPressedThisFrame) || (keys != null && keys.spaceKey.wasPressedThisFrame);
        bool grab = (pad != null && pad.buttonEast.wasPressedThisFrame) || (!Story.Active && keys != null && keys.lKey.wasPressedThisFrame);
        Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        if (input.magnitude < 0.2f) input = Vector2.zero;
        Vector3 way = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
        Vector3 at = hero.root.position;
        if (Player.Bot != null) // AutoPlay: walk up to the nearest and punch, grab now and then, dodge a grab coming
        {
            Boarder target = Nearest(30f, at);
            way = target != null ? Flat(target.Position - at) : Vector3.zero;
            float far = way.magnitude;
            way = far > 1.5f ? way / far : Vector3.zero;
            light = far <= 1.8f;
            heavy = light && Random.value < 0.02f;
            grab = far <= 1.5f && Random.value < 0.004f;
            dodge = boarders.Exists(b => b.move == Grabbing && Flat(b.Position - at).magnitude < 2f) && Random.value < 0.05f;
            if (far <= 1.5f && far > 0f) hero.root.rotation = Quaternion.LookRotation(Flat(target.Position - at));
        }
        if (ending != null) { light = heavy = dodge = grab = false; way = Vector3.zero; }

        void Face(Vector3 toward, float rate)
        {
            if (toward.sqrMagnitude > 0.0001f)
                hero.root.rotation = Quaternion.RotateTowards(hero.root.rotation, Quaternion.LookRotation(toward), rate * dt);
        }
        void Lock() // the blows go to the nearest boarder in reach, whichever way the hero was facing
        {
            Boarder target = Nearest(3.5f, at);
            if (target != null) Face(Flat(target.Position - at), 900f);
        }
        void Strike(float range, float cone, float damage, bool knock)
        {
            Vector3 facing = hero.root.forward;
            foreach (Boarder b in boarders)
            {
                if (!Standing(b)) continue;
                Vector3 to = Flat(b.Position - at);
                if (to.magnitude > range || Vector3.Angle(facing, to) > cone) continue;
                HitBoarder(b, damage, knock, to.normalized, now);
                stopUntil = now + (knock ? 0.09f : 0.05f);
            }
        }
        bool Grab() // the nearest boarder in arm's reach: thrown, whatever it was doing (a guard does not help)
        {
            Boarder b = now >= grabReady ? Nearest(1.7f, at) : null;
            if (b == null || b.move == Rising) return false; // not off the floor
            grabReady = float.MaxValue; // until the throw is over
            hero.root.rotation = Quaternion.LookRotation(Flat(b.Position - at));
            partner = b;
            b.move = Thrown;
            throwPick = Random.Range(0, moves.throwBy.Length);
            Pair(hero, b.rig, moves.throwBy[throwPick], moves.throwOf[throwPick], 0.12f, ThrowRate);
            TrainSim.Tint(b.skin, b.colour);
            heroMove = Throwing;
            heroLanded = false;
            safeUntil = now + moves.throwBy[throwPick].length / ThrowRate * 0.6f; // nobody hits her while she lifts it (the landing is open)
            return true;
        }

        switch (heroMove)
        {
            case Down:
            case Held:
                return;
            case Hurt:
            case Floored:
                if (now >= heroUntil) heroMove = Free;
                return;
            case Throwing:
                if (!heroLanded && hero.Done >= 0.68f) // the body hits the floor
                {
                    heroLanded = true;
                    Vector3 slammed = partner.rig.animator.GetBoneTransform(HumanBodyBones.Hips).position;
                    Wound(partner, throwDamage * level * train.Player.damageBonus, slammed + Vector3.up * 0.5f);
                    Effect("dust", Flat(slammed) + Vector3.up * Inside.y, 0.7f);
                    Effect("spark", slammed, 1.3f);
                    stopUntil = now + 0.14f;
                }
                if (hero.Done >= 0.97f)
                {
                    Settle(hero, moves.idle);
                    bool alive = partner.health > 0f;
                    Settle(partner.rig, alive ? moves.getUp : moves.lying);
                    partner.move = alive ? Rising : Down;
                    partner.riseAt = now + GetUpTime * 0.45f;
                    partner.until = now + GetUpTime * 0.85f;
                    partner.nextAttack = partner.until + 0.6f;
                    partner = null;
                    heroMove = Free;
                    grabReady = now + grabCooldown;
                }
                return;
            case Dodging:
                float left = (heroUntil - now) / dodgeTime;
                Place(hero.root, at + dodgeWay * (dodgeSpeed * Mathf.Clamp01(left * 1.6f) * dt));
                if (now >= heroUntil) heroMove = Free;
                return;
            case Attacking:
            case Kicking:
                bool kick = heroMove == Kicking;
                if (hero.Done < 0.3f) Lock();
                if (hero.Done < 0.45f) Place(hero.root, at + hero.root.forward * (1.6f * dt)); // a step into it
                if (!heroLanded && hero.Done >= (kick ? 0.42f : 0.35f))
                {
                    heroLanded = true;
                    Strike(kick ? heavyReach : reach, kick ? 100f : 70f, kick ? heavyDamage : lightDamage, kick || combo == 2);
                }
                if (light && !kick && hero.Done > 0.2f) queued = true;
                if (dodge && heroLanded) { StartDodge(way); return; } // a dodge cuts a move short once its blow is dealt
                if (grab && heroLanded && Grab()) return;             // and so does a grab: punches into a throw
                if (queued && combo < moves.combo.Length - 1 && hero.Done >= 0.6f) { StartCombo(combo + 1, now); return; }
                if (hero.Done >= (kick ? 0.85f : 0.8f)) heroMove = Free;
                return;
        }
        // Free.
        if (dodge) { StartDodge(way); return; }
        if (grab && Grab()) return;
        if (light) { StartCombo(0, now); return; }
        if (heavy && now >= kickReady)
        {
            heroMove = Kicking;
            heroLanded = false;
            kickReady = now + heavyCooldown;
            hero.Play(moves.heavy, true, 1.2f);
            return;
        }
        Place(hero.root, at + way * (heroSpeed * dt));
        Face(way, 720f);
        hero.Play(way.sqrMagnitude > 0.01f ? moves.run : moves.idle, false, 1f, 0.15f);
    }

    void StartCombo(int step, float now)
    {
        heroMove = Attacking;
        combo = step;
        queued = false;
        heroLanded = false;
        hero.Play(moves.combo[step], true, step == 2 ? 1.05f : 1.35f, 0.08f); // the last blow of the three is a heavy one: a moment open after it
        // Whoever stands in front of the swing may cover up: punches on a guard do little, a kick or a grab breaks it.
        foreach (Boarder b in boarders)
        {
            if (!Standing(b) || b.move != Free) continue;
            Vector3 to = Flat(b.Position - hero.root.position);
            if (to.magnitude > reach + 0.6f || Vector3.Angle(hero.root.forward, to) > 70f || Random.value >= GuardChance) continue;
            b.move = Guarding;
            b.until = now + 0.9f;
            b.rig.Play(moves.guard, true, 1f, 0.06f);
        }
    }

    void StartDodge(Vector3 way)
    {
        heroMove = Dodging;
        dodgeWay = way.sqrMagnitude > 0.01f ? way.normalized : -hero.root.forward; // standing still: back out of it
        hero.root.rotation = Quaternion.LookRotation(way.sqrMagnitude > 0.01f ? dodgeWay : hero.root.forward);
        heroUntil = Time.unscaledTime + dodgeTime;
        safeUntil = heroUntil + 0.05f;
        hero.Play(moves.dodge, true, 1.4f, 0.05f);
    }

    // The damage itself, and a boarder brought down by it; true if that was the end of it.
    bool Wound(Boarder b, float damage, Vector3 at)
    {
        damage = train.CriticalDamage(damage, out bool critical);
        b.health -= damage;
        train.DamageNumber(at + Vector3.up * 0.6f, damage, false, critical);
        if (b.health > 0f) return false;
        Effect("enemy", at, 1.4f);
        kills++;
        train.Charge(); // a kill like any other: the counter and the ultimate
        train.Earn(moneyPerBoarder);
        return true;
    }

    void HitBoarder(Boarder b, float damage, bool knock, Vector3 away, float now)
    {
        Vector3 chest = b.Position + Vector3.up * 1.2f;
        if (b.move == Rising)
        {
            if (now < b.riseAt) return; // on the floor: the blow goes over it
            b.move = Guarding;            // getting up: it covers up by itself
            b.until = now + 0.7f;
            b.rig.Play(moves.guard, true, 1f, 0.05f);
        }
        TrainSim.Tint(b.skin, b.colour);
        if (b.move == Guarding && !knock) // on the guard: a little, and it answers at once
        {
            Wound(b, damage * train.Player.damageBonus * 0.15f, chest);
            Effect("spark", chest - away * 0.4f, 0.5f);
            b.push = away * 1.5f;
            if (b.health <= 0f) { b.move = Down; b.rig.Play(moves.boarderDown, true, 1f, 0.08f); return; }
            b.move = Attacking; // the riposte: quick, no wind-up glow to speak of, dodge it or eat it
            b.landed = false;
            b.chain = false;
            b.rig.Play(moves.boarderAttacks[Random.Range(0, moves.boarderAttacks.Length)], true, RiposteRate, 0.04f);
            return;
        }
        // Hit after hit: it bursts free once too many come in a row, and is not staggered for a moment after.
        if (now - b.takenAt > 1.4f) b.taken = 0;
        b.taken++;
        b.takenAt = now;
        if (now < b.armourUntil || (b.taken >= BurstAfter && !knock))
        {
            bool bursting = now >= b.armourUntil;
            Effect("spark", chest - away * 0.3f, bursting ? 1.6f : 0.7f);
            if (Wound(b, damage * train.Player.damageBonus * (bursting ? 1f : 0.6f), chest)) { b.move = Down; b.rig.Play(moves.boarderDown, true, 1f, 0.08f); return; }
            if (!bursting) return;
            b.armourUntil = now + BurstArmour;
            b.taken = 0;
            b.move = Free;
            b.nextAttack = now + 0.3f;
            TrainSim.Tint(b.skin, new Color(1f, 0.4f, 0.3f) * 1.8f);
            if (Open(now))
            {
                HitHero(-away, now); // the shove: away from it, knocked out of the combo
                Place(hero.root, hero.root.position - away * 1.4f);
            }
            stopUntil = now + 0.12f;
            return;
        }
        if (b.move == Attacking && b.rig.Done >= 0.15f && !knock) // a blow already thrown is not stopped by a punch: only a kick or a grab
        {
            Effect("spark", chest - away * 0.3f, 0.7f);
            if (Wound(b, damage * train.Player.damageBonus, chest)) { b.move = Down; b.rig.Play(moves.boarderDown, true, 1f, 0.08f); }
            return;
        }
        bool broken = b.move == Guarding; // a kick through the guard: staggered for longer
        Effect("spark", chest - away * 0.3f, knock ? 1.2f : 0.8f);
        b.push = away * (knock ? 7f : 2.5f);
        if (Wound(b, damage * train.Player.damageBonus, chest))
        {
            b.move = Down;
            b.rig.Play(moves.boarderDown, true, 1f, 0.08f);
            b.rig.root.rotation = Quaternion.LookRotation(-away);
            return;
        }
        b.move = Hurt;
        b.until = now + (broken ? 1f : knock ? 0.75f : 0.45f);
        b.nextAttack = Mathf.Max(b.nextAttack, b.until + 0.4f);
        b.rig.Play(moves.boarderHits[Random.Range(0, moves.boarderHits.Length)], true, 1.2f, 0.05f);
    }

    // rank: how near the hero this one is among those standing. The nearest leads, the second circles round to her
    // back, the others keep off until a place opens. One strike at a time is started, StrikeGap apart.
    void BoarderUpdate(Boarder b, int rank, ref int attacking, float dt, float now)
    {
        if (Paired(b)) return; // the throw or the hold moves it
        b.push = Vector3.MoveTowards(b.push, Vector3.zero, 14f * dt);
        if (b.push.sqrMagnitude > 0f) Place(b.rig.root, b.Position + b.push * dt);
        Vector3 to = Flat(hero.root.position - b.Position);
        float far = to.magnitude;
        if (b.move == Hurt || b.move == Rising)
        {
            if (now >= b.until) b.move = Free;
            return;
        }
        if (far > 0.01f) b.rig.root.rotation = Quaternion.RotateTowards(b.rig.root.rotation, Quaternion.LookRotation(to), 300f * dt);
        if (b.move == Guarding)
        {
            if (now >= b.until) b.move = Free;
            return;
        }
        if (b.move == Attacking)
        {
            // A step in while winding up, and it glows hotter until the blow.
            if (b.rig.Done < 0.38f && far > 1.1f) Place(b.rig.root, b.Position + to / far * (3.2f * dt));
            float windup = Mathf.Clamp01(b.rig.Done / 0.42f);
            TrainSim.Tint(b.skin, Color.Lerp(b.colour, new Color(1f, 0.95f, 0.9f) * 2.2f, b.landed ? 0f : windup * windup));
            if (!b.landed && b.rig.Done >= 0.42f)
            {
                b.landed = true;
                bool hit = far <= reach && Vector3.Angle(b.rig.root.forward, to) < 60f && Open(now);
                if (hit) HitHero(to, now);
                if (hit && b.chain && Train(b)) return; // straight into a second blow
            }
            if (b.rig.Done >= 0.9f)
            {
                b.move = Free;
                b.nextAttack = now + Rest;
                attacking--;
            }
            return;
        }
        if (b.move == Grabbing)
        {
            // Purple and coming in fast, arms out: dodge it or hit it before it gets hold.
            TrainSim.Tint(b.skin, Color.Lerp(b.colour, new Color(0.8f, 0.3f, 1.6f), 0.5f + 0.5f * Mathf.Sin(now * 20f)));
            if (far > 1.05f) Place(b.rig.root, b.Position + to / far * (4.2f * dt));
            b.rig.Play(moves.boarderWalk, false, 1.5f, 0.1f);
            if (far <= 1.2f && Open(now)) { Seize(b, now); return; }
            if (now >= b.until) // missed: off balance a moment, open to a punish
            {
                TrainSim.Tint(b.skin, b.colour);
                b.move = Hurt;
                b.until = now + 0.6f;
                b.nextAttack = now + Rest;
                b.rig.Play(moves.boarderHits[0], true, 1f, 0.1f);
                attacking--;
            }
            return;
        }
        if (ending != null || heroMove == Down) { b.rig.Play(moves.boarderIdle, false); return; }
        // Free: its turn to strike, when it is near the front, has rested, and the last strike was long enough ago.
        bool behind = Vector3.Angle(hero.root.forward, -to) > 100f; // at her back: it does not wait for the gap while she is busy
        bool turn = rank < 2 && now >= b.nextAttack && (now >= nextStrike || (behind && heroMove == Attacking)) && attacking < maxAttackers && Open(now);
        if (turn && far <= 2.3f)
        {
            nextStrike = now + StrikeGap * Random.Range(0.8f, 1.3f);
            attacking++;
            if (now >= nextGrab && Random.value < 0.3f)
            {
                b.move = Grabbing;
                b.until = now + 0.9f;
                nextGrab = now + Mathf.Lerp(7f, 4f, Hardness);
                return;
            }
            b.chain = Random.value < ChainChance;
            b.move = Attacking;
            b.landed = false;
            b.rig.Play(moves.boarderAttacks[Random.Range(0, moves.boarderAttacks.Length)], true, 1f, 0.1f);
            return;
        }
        // Where to stand: the leader in front of her, the second round at her back, the rest in a ring further off.
        Vector3 spot;
        if (rank == 0) spot = b.Position + (far > 1.4f ? to / far * (far - 1.4f) : -to / Mathf.Max(far, 0.01f) * (1.4f - far));
        else if (rank == 1) spot = hero.root.position + Quaternion.Euler(0f, b.side * 140f, 0f) * hero.root.forward * 1.7f;
        else spot = hero.root.position - to / Mathf.Max(far, 0.01f) * 3.6f + Vector3.Cross(Vector3.up, to / Mathf.Max(far, 0.01f)) * (b.side * 0.8f);
        Vector3 go = Flat(spot - b.Position);
        if (Random.value < dt * 0.25f) b.side = -b.side;
        if (go.magnitude < 0.25f) { b.rig.Play(moves.boarderIdle, false, 1f, 0.2f); return; }
        Vector3 step = go.normalized * Mathf.Min(boarderSpeed * dt, go.magnitude);
        Place(b.rig.root, b.Position + step);
        // The walk that matches the way it goes, seen from where it faces (the hero).
        float ahead = Vector3.Dot(go.normalized, b.rig.root.forward), across = Vector3.Dot(go.normalized, b.rig.root.right);
        AnimationClip walk = Mathf.Abs(ahead) >= Mathf.Abs(across) ? (ahead > 0f ? moves.boarderWalk : moves.boarderBack) : across > 0f ? moves.boarderRight : moves.boarderLeft;
        b.rig.Play(walk, false, 1f, 0.2f);
    }

    // Straight into a second blow of a chain: a different one, no rest between.
    bool Train(Boarder b)
    {
        b.chain = false;
        b.landed = false;
        AnimationClip next = moves.boarderAttacks[Random.Range(0, moves.boarderAttacks.Length)];
        b.rig.Play(next, true, 1.15f, 0.08f);
        return true;
    }

    // ---------------------------------------------------------------- a boarder's headlock
    const float MashSeconds = 2.4f;

    void Seize(Boarder b, float now)
    {
        TrainSim.Tint(b.skin, b.colour);
        b.move = Holding;
        partner = b;
        heroMove = Held;
        holdPhase = 0;
        mash = 0f;
        Pair(b.rig, hero, moves.holdBy, moves.holdOf);
        Sound.Play("wagonbad", 0.3f);
    }

    // 0 getting her in it, 1 held (press to break free before time runs out), 2 let go, 3 the DDT.
    void Hold(float dt, float now)
    {
        Boarder b = partner;
        switch (holdPhase)
        {
            case 0:
                if (b.rig.Done < 0.95f) return;
                holdPhase = 1;
                mashUntil = now + MashSeconds;
                Pair(b.rig, hero, moves.heldBy, moves.heldOf, 0.15f);
                return;
            case 1:
                bool press = (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                          || (Player.Bot != null && Random.value < dt * 7f);
                if (press) { mash += 1f; stopUntil = now + 0.03f; }
                mash = Mathf.Max(0f, mash - dt * 1.5f); // stop pressing and the hold tightens again
                if (mash >= MashNeed)
                {
                    holdPhase = 2;
                    Pair(b.rig, hero, moves.letGoBy, moves.letGoOf, 0.1f);
                    train.Toast(L10n.T("brawl.free"));
                }
                else if (now >= mashUntil)
                {
                    holdPhase = 3;
                    heroLanded = false;
                    Pair(b.rig, hero, moves.slamBy, moves.slamOf, 0.1f, SlamRate);
                }
                return;
            case 2:
                if (b.rig.Done < 0.9f) return;
                Settle(hero, moves.idle);
                Settle(b.rig, moves.boarderHits[1]);
                heroMove = Free;
                safeUntil = now + 0.6f;
                b.move = Hurt; // shoved off: a moment to hit back
                b.until = now + 0.9f;
                b.nextAttack = now + Rest + 0.8f;
                b.push = Flat(b.Position - hero.root.position).normalized * 4f;
                partner = null;
                return;
            case 3:
                if (!heroLanded && b.rig.Done >= 0.55f) // her head into the floor
                {
                    heroLanded = true;
                    Vector3 head = hero.animator.GetBoneTransform(HumanBodyBones.Head).position;
                    train.Player.Damage(blow * slamBlows * (1f + 0.035f * train.Difficulty));
                    Effect("spark", head, 1.4f);
                    Effect("dust", Flat(head) + Vector3.up * Inside.y, 0.7f);
                    Sound.Play("wagonbad", 0.5f);
                    stopUntil = now + 0.16f;
                }
                if (b.rig.Done < 0.95f) return;
                Settle(b.rig, moves.boarderIdle);
                b.move = Free;
                b.nextAttack = now + Rest + 1f;
                partner = null;
                if (!train.Player.Alive) return; // Update lays her down
                Settle(hero, moves.getUp);
                heroMove = Floored;
                heroUntil = now + GetUpTime * 0.8f;
                safeUntil = heroUntil + 0.5f;
                return;
        }
    }

    void HitHero(Vector3 to, float now)
    {
        train.Player.Damage(blow * (1f + 0.035f * train.Difficulty));
        Effect("spark", hero.root.position + Vector3.up * 1.3f, 0.9f);
        Sound.Play("wagonbad", 0.35f);
        stopUntil = now + 0.08f;
        if (!train.Player.Alive) return; // Update takes it from here
        heroMove = Hurt;
        heroUntil = now + 0.4f;
        safeUntil = now + 0.6f; // a moment to answer before the next one lands
        hero.Play(moves.hit, true, 1.2f, 0.05f);
        Place(hero.root, hero.root.position + to.normalized * 0.4f);
    }

    // Behind the hero, turned by the right stick or the mouse, never out through the walls.
    void Follow(float dt)
    {
        Vector2 aim = look.ReadValue<Vector2>();
        bool pointer = look.activeControl != null && look.activeControl.device is Pointer;
        yaw += pointer ? aim.x * mouseSpeed : aim.x * stickSpeed * dt;
        pitch = Mathf.Clamp(pitch - (pointer ? aim.y * mouseSpeed : aim.y * stickSpeed * dt * 0.5f), 5f, 40f);
        // Over the right shoulder, so the hero does not hide whoever is in front of her.
        Vector3 target = hero.root.position + Vector3.up * 1.7f + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 0.8f;
        // Further back for a throw or a hold: two bodies in one move need the room.
        distance = Mathf.MoveTowards(distance, heroMove == Held || heroMove == Throwing ? 6.8f : 5.2f, 4f * dt);
        Vector3 eye = target - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance - Inside;
        eye.x = Mathf.Clamp(eye.x, -HalfLength + 0.3f, HalfLength - 0.3f);
        eye.z = Mathf.Clamp(eye.z, -HalfWidth + 0.3f, HalfWidth - 0.3f);
        eye.y = Mathf.Clamp(eye.y, 0.6f, Height - 0.35f);
        view.transform.position = Inside + eye;
        view.transform.LookAt(target);
    }

    // ---------------------------------------------------------------- the ride in
    class Rider { public Transform body; public Vector3 hips; public float side, along; public int look; }
    readonly List<Rider> riders = new();
    Combat.Batch speederLook;
    readonly List<(Vector3 at, Quaternion turn)> speeders = new();
    float approachUntil, nextDust;
    int riding;
    const float SpeederSize = 3.4f;                                   // the model is a unit long
    static readonly Vector3 Saddle = new Vector3(0f, 0.225f, -0.14f); // on the model (Unity's axes: +z ahead)

    // One rider per boarder, in its colours, sat astride (bones set once, nothing animates it).
    void Mount(int count)
    {
        speederLook ??= new Combat.Batch(Color.white, true, Resources.Load<Mesh>("Models/Speeder"));
        while (riders.Count < count)
        {
            var root = new GameObject("Rider").transform;
            Animator rig = Body(moves.boarder, root);
            Material skin = TrainSim.Mat(Paint[riders.Count % Paint.Length]);
            foreach (Renderer part in rig.GetComponentsInChildren<Renderer>()) part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => skin);
            rig.runtimeAnimatorController = null;
            rig.enabled = false;
            Sit(rig);
            riders.Add(new Rider { body = root, hips = root.InverseTransformPoint(rig.GetBoneTransform(HumanBodyBones.Hips).position) });
        }
        riding = count;
        float side = Random.value < 0.5f ? -1f : 1f;
        for (int i = 0; i < riders.Count; i++)
        {
            riders[i].body.gameObject.SetActive(i < count);
            riders[i].side = i < 2 ? side : -side;              // two from one side, one from the other
            riders[i].along = i == 0 ? -2.8f : i == 1 ? 2.8f : 0f;
            riders[i].look = i;
        }
    }

    // Sat on a speeder: leaning into the wind, thighs forward, feet on the pedals, hands on the bars.
    static void Sit(Animator rig)
    {
        Transform Bone(HumanBodyBones b) => rig.GetBoneTransform(b);
        void Point(HumanBodyBones from, HumanBodyBones to, Vector3 along)
        {
            Transform bone = Bone(from), end = Bone(to);
            if (bone && end) bone.rotation = Quaternion.FromToRotation(end.position - bone.position, along) * bone.rotation;
        }
        Point(HumanBodyBones.Hips, HumanBodyBones.Head, new Vector3(0f, 1f, 0.45f));
        foreach (float s in new[] { -1f, 1f })
        {
            bool left = s < 0f;
            Point(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg, left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, new Vector3(s * 0.25f, -0.35f, 1f));
            Point(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, new Vector3(s * 0.05f, -1f, -0.1f));
            Point(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm, left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm, new Vector3(s * 0.3f, -0.35f, 1f));
            Point(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm, left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand, new Vector3(0f, 0.25f, 1f));
        }
    }

    // In from far out on the flanks, curving in to run level with the wagon; at the alarm each rider leaps onto
    // its roof and the empty speeders peel away.
    void Arrive()
    {
        float now = Time.unscaledTime;
        if (DriverFight && leaper)
        {
            // A long leap from the rival's engine onto the wagon's roof, tumbling once on the way.
            float k = Mathf.Clamp01(1f - (approachUntil - now) / approachSeconds);
            Vector3 roof = train.CarPosition(wagon) + Vector3.up * 2.4f;
            Vector3 hipsAt = Vector3.Lerp(driverFrom, roof, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 9f);
            Vector3 way = Flat(roof - driverFrom);
            Quaternion turn = (way.sqrMagnitude > 0.01f ? Quaternion.LookRotation(way) : Quaternion.identity) * Quaternion.Euler(k * 360f, 0f, 0f);
            leaper.SetPositionAndRotation(hipsAt - turn * leaperHips, turn);
            return;
        }
        Vector3 wagonAt = train.CarPosition(wagon), ahead = Flat(train.Forward(wagon)).normalized, across = Vector3.Cross(Vector3.up, ahead);
        float t = Mathf.Clamp01(1f - (approachUntil - now) / approachSeconds), eased = 1f - (1f - t) * (1f - t) * (1f - t);
        float leap = Mathf.Clamp01((now - approachUntil) / (alarmSeconds * 0.6f));
        bool dust = now >= nextDust;
        if (dust) nextDust = now + 0.07f;
        speeders.Clear();
        for (int i = 0; i < riding; i++)
        {
            Rider r = riders[i];
            Vector3 to = wagonAt + across * (r.side * 3.8f) + ahead * r.along + Vector3.up * 0.8f;
            Vector3 from = to + across * (r.side * 34f) - ahead * 10f;
            Vector3 at = Vector3.Lerp(from, to, eased) + Vector3.up * (Mathf.Sin(now * 6f + r.look) * 0.08f);
            at += across * (r.side * 14f * leap * leap); // riderless, off again
            Vector3 heading = Vector3.Slerp((to - from).normalized, ahead, eased);
            Quaternion turn = Quaternion.LookRotation(heading) * Quaternion.Euler(0f, 0f, r.side * 25f * (1f - eased));
            speeders.Add((at, turn));
            if (dust && t < 1f) Combat.Instance.Burst("dust", new Vector3(at.x, 0.1f, at.z), 0.9f);
            if (leap >= 1f) { r.body.gameObject.SetActive(false); continue; }
            // On the saddle; at the alarm up in an arc onto the wagon's roof.
            Vector3 seat = at + turn * (Saddle * SpeederSize), roof = wagonAt + ahead * (r.along * 0.4f) + Vector3.up * 2.4f;
            Vector3 hips = Vector3.Lerp(seat, roof, leap) + Vector3.up * (Mathf.Sin(leap * Mathf.PI) * 2.2f);
            r.body.SetPositionAndRotation(hips - turn * r.hips, turn);
        }
    }

    void LateUpdate()
    {
        if (!Incoming || speederLook == null) return;
        foreach (var s in speeders) speederLook.Add(s.at, Vector3.one * SpeederSize, s.turn);
        speederLook.Flush();
    }

    public void TestWin() { if (Active) End("brawl.won"); }

    void End(string key)
    {
        Active = false;
        view.enabled = false;
        if (room) room.gameObject.SetActive(false);
        board.gameObject.SetActive(false);
        foreach (Rider r in riders) r.body.gameObject.SetActive(false);
        if (leaper) leaper.gameObject.SetActive(false);
        // Lost in there: thrown out of the wagon, hurt but still in the fight (the wrecked wagon is the price).
        if (key != null && !train.Player.Alive) train.Player.Heal(train.Player.maxHealth * 0.3f, true);
        if (DriverFight)
        {
            // The boarders back as they were; the driver's verdict goes to the duel.
            foreach (Boarder b in boarders) b.rig.root.gameObject.SetActive(false);
            boarders.Clear();
            boarders.AddRange(crew);
            crew.Clear();
            DriverFight = false;
            System.Action<bool> done = driverDone;
            driverDone = null;
            if (key != null) key = key == "brawl.won" ? "brawl.driver.won" : "brawl.driver.lost";
            done?.Invoke(key == "brawl.driver.won");
        }
        Time.timeScale = 1f;
        effects.Clear();
        foreach (ParticleSystem system in unscaled) if (system) { var main = system.main; main.useUnscaledTime = false; } // the pool's copies, back to game time
        unscaled.Clear();
        if (key != null) train.Toast(L10n.T(key, kills, kills * moneyPerBoarder));
    }

    void OnDestroy()
    {
        hero?.Free();
        foreach (Boarder b in boarders) b.rig.Free();
    }
}

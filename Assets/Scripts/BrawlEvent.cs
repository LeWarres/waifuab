using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

// The boarding: robots jump onto an armed wagon and the hero goes in after them. The game is then seen from
// behind the hero, inside the wagon: a light combo, a heavy kick and a dodge, until every boarder is down or the
// hero is. Time runs slow outside, like the swarm's (SwarmEvent); TrainSim starts it as the leg's event. The
// moves are clips of the two animation packs (BrawlClips), played through a small playable graph each.
public class BrawlEvent : MonoBehaviour
{
    public Vector2Int boarderCount = new Vector2Int(5, 7); // 5 or 6
    public float warnSeconds = 2.2f, slowMotion = 0.12f;
    public float heroSpeed = 5.5f, dodgeSpeed = 12f, dodgeTime = 0.38f;
    public float reach = 1.9f, heavyReach = 2.5f, heavyCooldown = 1.2f;
    public float lightDamage = 12f, heavyDamage = 28f, boarderHealth = 100f; // the hero's damage bonus multiplies these
    public float blow = 1.4f * Balance.Scale;  // a boarder's hit on the hero, grown with the difficulty like the enemies'
    public float boarderSpeed = 2.8f;
    public int maxAttackers = 2;               // the others wait their turn around the hero
    public int moneyPerBoarder = 4;
    public float lostShare = 0.35f;            // what the wagon loses if the hero goes down in there
    public float stickSpeed = 140f, mouseSpeed = 0.15f;
    [System.NonSerialized] public bool testAtStart = false; // testing aid: true makes the boarding open every run

    public bool Active { get; private set; }       // from the warning to the end
    public bool Incoming => Active && Time.unscaledTime < startsAt;
    public bool Fighting => Active && !Incoming;
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

    const int Free = 0, Attacking = 1, Hurt = 2, Down = 3, Dodging = 4, Kicking = 5;

    // A humanoid body played by hand: a mixer of every clip it has used, fading to the one it plays now.
    class Rig
    {
        public readonly Transform root;
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
        public float health, until, nextAttack, side;
        public int move;
        public bool landed;      // this attack's blow is dealt
        public Vector3 push;     // knocked back, sliding to a stop
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
    readonly List<Boarder> boarders = new();
    Canvas board;
    Text counter, hint;
    int wagon, kills, heroMove, combo;
    bool queued, heroLanded;
    float startsAt, heroUntil, safeUntil, kickReady, endsAt, yaw, pitch;
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
        view.enabled = false;

        board = UiKit.NewCanvas("Brawl", 6);
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.62f);
        Slanted bar = UiKit.Panel(board.transform, new Vector2(0f, -70f), new Vector2(560f, 52f), glass, glass, 0.12f);
        bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        counter = UiKit.Label(bar.transform, UiKit.Heading, 26, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(540f, 44f), true);
        Slanted help = UiKit.Panel(board.transform, new Vector2(0f, 60f), new Vector2(760f, 40f), glass, glass, 0.12f);
        help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint = UiKit.Label(help.transform, UiKit.Body, 20, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(740f, 34f), true);
        board.gameObject.SetActive(false);
    }

    void Start()
    {
        if (testAtStart) Invoke(nameof(TestBegin), 1f);
    }

    void TestBegin() => Begin();

    // False if there is nothing to play it with: no armed wagon left, or the clips are missing.
    public bool Begin()
    {
        if (!moves || !train.Player.Alive || !train.Gunner(out wagon)) return false;
        if (!room) Build();
        Active = true;
        ending = null;
        kills = 0;
        startsAt = Time.unscaledTime + warnSeconds;
        groundPaint.mainTexture = Resources.Load<Texture2D>("Ground/" + train.BiomeId);

        // The hero comes in at the back end, the boarders spread through the rest of the wagon.
        hero.root.position = Inside + new Vector3(-HalfLength + 2f, 0f, 0f);
        hero.root.rotation = Quaternion.Euler(0f, 90f, 0f);
        heroMove = Free;
        hero.Play(moves.idle);
        yaw = 90f;
        pitch = 20f;
        int count = Random.Range(boarderCount.x, boarderCount.y);
        while (boarders.Count < count) boarders.Add(NewBoarder(boarders.Count));
        float level = 1f + Combat.Instance.healthPerLevel * train.Difficulty;
        for (int i = 0; i < boarders.Count; i++)
        {
            Boarder b = boarders[i];
            b.rig.root.gameObject.SetActive(i < count);
            if (i >= count) continue;
            b.health = boarderHealth * level;
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
        Animator body = Body(moves.boarder, root);
        var b = new Boarder { rig = new Rig(root, body), colour = Paint[i % Paint.Length] };
        b.skin = TrainSim.Mat(b.colour);
        foreach (Renderer part in body.GetComponentsInChildren<Renderer>())
            part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => b.skin);
        return b;
    }

    // A humanoid model under root, its feet on root and 1.8 tall.
    static Animator Body(GameObject model, Transform root)
    {
        Transform body = Instantiate(model, root).transform;
        body.localPosition = Vector3.zero;
        body.localRotation = Quaternion.identity;
        Animator rig = body.GetComponentInChildren<Animator>();
        float tall = rig.GetBoneTransform(HumanBodyBones.Head).position.y - rig.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
        body.localScale *= 1.8f / (tall * 1.12f);
        return rig;
    }

    // ---------------------------------------------------------------- the wagon's inside
    void Build()
    {
        room = new GameObject("WagonInside").transform;
        room.position = Inside;
        Color wood = new Color(0.62f, 0.4f, 0.22f), woodDark = new Color(0.46f, 0.29f, 0.16f);
        Color wall = new Color(0.78f, 0.28f, 0.22f), wallLow = new Color(0.36f, 0.16f, 0.14f), steel = new Color(0.5f, 0.53f, 0.6f);
        Color gold = new Color(1f, 0.8f, 0.3f), roof = new Color(0.9f, 0.85f, 0.75f), crate = new Color(0.75f, 0.55f, 0.3f);
        // Floor of planks, a roof with steel ribs and lamps.
        for (int k = 0; k < 8; k++)
            Block(new Vector3(0f, -0.1f, -HalfWidth + (k + 0.5f) * HalfWidth / 4f), new Vector3(HalfLength * 2f, 0.2f, HalfWidth / 4f - 0.04f), k % 2 == 0 ? wood : woodDark);
        Block(new Vector3(0f, Height + 0.1f, 0f), new Vector3(HalfLength * 2f + 0.4f, 0.2f, HalfWidth * 2f + 0.4f), roof);
        for (int k = 0; k < 6; k++)
        {
            float x = -HalfLength + (k + 0.5f) * HalfLength / 3f;
            Block(new Vector3(x, Height - 0.1f, 0f), new Vector3(0.25f, 0.22f, HalfWidth * 2f), steel);
            Block(new Vector3(x + HalfLength / 6f, Height - 0.06f, 0f), new Vector3(1.2f, 0.1f, 0.4f), new Color(1f, 0.92f, 0.7f) * 2.5f, false); // a lamp: bright enough to bloom
        }
        // The ends, each with a door.
        foreach (float end in new[] { -1f, 1f })
        {
            Block(new Vector3(end * (HalfLength + 0.1f), Height * 0.5f, 0f), new Vector3(0.2f, Height, HalfWidth * 2f), wall);
            Block(new Vector3(end * HalfLength, 1.15f, 0f), new Vector3(0.1f, 2.3f, 1.3f), steel);
            Block(new Vector3(end * (HalfLength - 0.06f), 1.6f, 0f), new Vector3(0.05f, 0.6f, 0.8f), new Color(0.6f, 0.85f, 1f) * 1.6f, false);
        }
        // The sides: dark panelling up to the windows, the red wall over them, posts between, a gold trim.
        const int Windows = 7;
        foreach (float side in new[] { -1f, 1f })
        {
            float z = side * (HalfWidth + 0.1f);
            Block(new Vector3(0f, 0.65f, z), new Vector3(HalfLength * 2f, 1.3f, 0.2f), wallLow);
            Block(new Vector3(0f, (2.7f + Height) * 0.5f, z), new Vector3(HalfLength * 2f, Height - 2.7f, 0.2f), wall);
            for (int k = 0; k <= Windows; k++)
                Block(new Vector3(-HalfLength + k * HalfLength * 2f / Windows, 2f, z), new Vector3(0.7f, 1.4f, 0.22f), wall);
            Block(new Vector3(0f, 1.33f, z - side * 0.12f), new Vector3(HalfLength * 2f, 0.07f, 0.06f), gold);
            Block(new Vector3(0f, 2.68f, z - side * 0.12f), new Vector3(HalfLength * 2f, 0.07f, 0.06f), gold);
        }
        // Crates by the walls: cover, and in the way.
        foreach (Vector3 at in new[] { new Vector3(-6.5f, 0f, 2.9f), new Vector3(-1.5f, 0f, -2.9f), new Vector3(4f, 0f, 2.9f), new Vector3(8.5f, 0f, -2.9f) })
        {
            Block(at + Vector3.up * 0.55f, new Vector3(1.6f, 1.1f, 1.1f), crate);
            Block(at + new Vector3(0.2f, 1.45f, 0f), new Vector3(0.9f, 0.7f, 0.9f), crate * 0.85f);
            crates.Add(new Rect(at.x - 0.8f, at.z - 0.55f, 1.6f, 1.1f));
        }
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
        Animator body = Body(Resources.Load<GameObject>(who), heroRoot);
        Player.Skin(body.transform, who);
        hero = new Rig(heroRoot, body);
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
        look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // the sun comes in through the roof: no dark inside
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

    void Place(Transform body, Vector3 to) => body.position = Inside + Clear(to - Inside, 0.45f);

    // ---------------------------------------------------------------- the fight
    void Update()
    {
        // Testing aid: M on the keyboard starts a boarding on the spot.
        if (!Active && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame && !train.Paused && !train.AtStation && !train.InEvent && !train.BossFight) Begin();
        if (!Active || train.Paused) return;
        if (train.AtStation) { End(null); return; }
        if (Incoming) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f), now = Time.unscaledTime;
        if (!view.enabled)
        {
            view.enabled = true;
            room.gameObject.SetActive(true);
            board.gameObject.SetActive(true);
            Time.timeScale = slowMotion; // the train all but stops while this is played
        }
        groundPaint.mainTextureOffset += new Vector2(dt * 0.9f, 0f); // the ride goes on outside

        if (ending != null)
        {
            if (now >= endsAt) End(ending);
        }
        else if (!train.CarAlive(wagon)) Finish("brawl.lost", now);
        else if (!train.Player.Alive && heroMove != Down)
        {
            heroMove = Down;
            hero.Play(moves.down);
            train.HurtCar(wagon, lostShare);
            Finish("brawl.lost", now);
        }

        HeroUpdate(dt, now);
        // The nearest ones may attack; the others wait their turn further off.
        var order = boarders.FindAll(b => b.rig.root.gameObject.activeSelf && b.move != Down);
        order.Sort((a, c) => Flat(a.Position - hero.root.position).sqrMagnitude.CompareTo(Flat(c.Position - hero.root.position).sqrMagnitude));
        int attacking = order.FindAll(b => b.move == Attacking).Count;
        for (int i = 0; i < order.Count; i++) BoarderUpdate(order[i], i < maxAttackers, ref attacking, dt, now);
        // Nobody stands inside anybody else.
        for (int i = 0; i < order.Count; i++)
        {
            Vector3 apart = Flat(order[i].Position - hero.root.position);
            if (apart.sqrMagnitude < 0.81f) Place(order[i].rig.root, hero.root.position + apart.normalized * 0.9f);
            for (int j = i + 1; j < order.Count; j++)
            {
                Vector3 gap = Flat(order[j].Position - order[i].Position);
                if (gap.sqrMagnitude < 0.64f) Place(order[j].rig.root, order[i].Position + (gap.sqrMagnitude > 0.0001f ? gap.normalized : Vector3.right) * 0.8f);
            }
        }
        if (ending == null && order.Count == 0) Finish("brawl.won", now);

        hero.Tick(dt);
        foreach (Boarder b in boarders) if (b.rig.root.gameObject.activeSelf) b.rig.Tick(dt);
        Follow(dt);
        counter.text = L10n.T("brawl.hud", order.Count, Mathf.RoundToInt(train.Player.Health / train.Player.maxHealth * 100f));
        bool pad = train.Player.UsingGamepad, ps = Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad;
        hint.text = L10n.T("brawl.hint", !pad ? "J" : ps ? "□" : "X", !pad ? "K" : ps ? "△" : "Y", !pad ? L10n.T("key.space") : ps ? "✕" : "A");
    }

    void Finish(string key, float now)
    {
        ending = key;
        endsAt = now + 1.6f;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    Boarder Nearest(float within, Vector3 from)
    {
        Boarder best = null;
        float least = within * within;
        foreach (Boarder b in boarders)
        {
            if (!b.rig.root.gameObject.activeSelf || b.move == Down) continue;
            float d = Flat(b.Position - from).sqrMagnitude;
            if (d < least) { least = d; best = b; }
        }
        return best;
    }

    void HeroUpdate(float dt, float now)
    {
        Gamepad pad = Gamepad.current;
        Keyboard keys = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool light = (pad != null && pad.buttonWest.wasPressedThisFrame) || (keys != null && keys.jKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool heavy = (pad != null && pad.buttonNorth.wasPressedThisFrame) || (keys != null && keys.kKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame);
        bool dodge = (pad != null && pad.buttonSouth.wasPressedThisFrame) || (keys != null && keys.spaceKey.wasPressedThisFrame);
        Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        if (input.magnitude < 0.2f) input = Vector2.zero;
        Vector3 way = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
        Vector3 at = hero.root.position;
        if (Player.Bot != null) // AutoPlay: walk up to the nearest and punch, dodge now and then
        {
            Boarder target = Nearest(30f, at);
            way = target != null ? Flat(target.Position - at) : Vector3.zero;
            float far = way.magnitude;
            way = far > 1.5f ? way / far : Vector3.zero;
            light = far <= 1.8f;
            heavy = light && Random.value < 0.02f;
            dodge = false;
            if (far <= 1.5f && far > 0f) hero.root.rotation = Quaternion.LookRotation(Flat(target.Position - at));
        }
        if (ending != null) { light = heavy = dodge = false; way = Vector3.zero; }

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
                if (!b.rig.root.gameObject.activeSelf || b.move == Down) continue;
                Vector3 to = Flat(b.Position - at);
                if (to.magnitude > range || Vector3.Angle(facing, to) > cone) continue;
                HitBoarder(b, damage, knock, to.normalized, now);
            }
        }

        switch (heroMove)
        {
            case Down:
                return;
            case Hurt:
                if (now >= heroUntil) heroMove = Free;
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
                if (queued && combo < moves.combo.Length - 1 && hero.Done >= 0.6f) { StartCombo(combo + 1); return; }
                if (hero.Done >= (kick ? 0.85f : 0.8f)) heroMove = Free;
                return;
        }
        // Free.
        if (dodge) { StartDodge(way); return; }
        if (light) { StartCombo(0); return; }
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

    void StartCombo(int step)
    {
        heroMove = Attacking;
        combo = step;
        queued = false;
        heroLanded = false;
        hero.Play(moves.combo[step], true, 1.35f, 0.08f);
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

    void HitBoarder(Boarder b, float damage, bool knock, Vector3 away, float now)
    {
        damage = train.CriticalDamage(damage * train.Player.damageBonus, out bool critical);
        b.health -= damage;
        Vector3 chest = b.Position + Vector3.up * 1.2f;
        train.DamageNumber(chest + Vector3.up * 0.6f, damage, false, critical);
        Combat.Instance.Burst("spark", chest - away * 0.3f, knock ? 1.2f : 0.8f);
        TrainSim.Tint(b.skin, b.colour);
        b.push = away * (knock ? 7f : 2.5f);
        if (b.health <= 0f)
        {
            b.move = Down;
            b.rig.Play(moves.boarderDown, true, 1f, 0.08f);
            b.rig.root.rotation = Quaternion.LookRotation(-away);
            Combat.Instance.Burst("enemy", chest, 1.4f);
            kills++;
            train.Charge(); // a kill like any other: the counter and the ultimate
            train.Earn(moneyPerBoarder);
            return;
        }
        b.move = Hurt;
        b.until = now + (knock ? 0.75f : 0.45f);
        b.nextAttack = Mathf.Max(b.nextAttack, b.until + 0.4f);
        b.rig.Play(moves.boarderHits[Random.Range(0, moves.boarderHits.Length)], true, 1.2f, 0.05f);
    }

    void BoarderUpdate(Boarder b, bool close, ref int attacking, float dt, float now)
    {
        b.push = Vector3.MoveTowards(b.push, Vector3.zero, 14f * dt);
        if (b.push.sqrMagnitude > 0f) Place(b.rig.root, b.Position + b.push * dt);
        Vector3 to = Flat(hero.root.position - b.Position);
        float far = to.magnitude;
        if (b.move == Hurt)
        {
            if (now >= b.until) b.move = Free;
            return;
        }
        if (far > 0.01f) b.rig.root.rotation = Quaternion.RotateTowards(b.rig.root.rotation, Quaternion.LookRotation(to), 300f * dt);
        if (b.move == Attacking)
        {
            // The wind-up shows: it glows hotter until the blow.
            float windup = Mathf.Clamp01(b.rig.Done / 0.42f);
            TrainSim.Tint(b.skin, Color.Lerp(b.colour, new Color(1f, 0.95f, 0.9f) * 2.2f, b.landed ? 0f : windup * windup));
            if (!b.landed && b.rig.Done >= 0.42f)
            {
                b.landed = true;
                if (far <= reach && Vector3.Angle(b.rig.root.forward, to) < 60f && now >= safeUntil && heroMove != Down && ending == null) HitHero(b, to, now);
            }
            if (b.rig.Done >= 0.9f)
            {
                b.move = Free;
                b.nextAttack = now + Random.Range(0.9f, 1.8f);
                attacking--;
            }
            return;
        }
        if (ending != null) { b.rig.Play(moves.boarderIdle, false); return; }
        // Free: the near ones come in close and strike in turn, the others circle further off.
        if (close && far <= 1.6f && now >= b.nextAttack && attacking < maxAttackers && heroMove != Down)
        {
            b.move = Attacking;
            b.landed = false;
            attacking++;
            b.rig.Play(moves.boarderAttacks[Random.Range(0, moves.boarderAttacks.Length)], true, 1f, 0.1f);
            return;
        }
        float want = close ? 1.3f : 3.4f;
        Vector3 step;
        AnimationClip walk;
        if (far > want + 0.3f) { step = to / far; walk = moves.boarderWalk; }
        else if (far < want - 0.4f) { step = -to / Mathf.Max(far, 0.01f) * 0.7f; walk = moves.boarderWalk; }
        else
        {
            step = Vector3.Cross(Vector3.up, to / Mathf.Max(far, 0.01f)) * (b.side * 0.5f);
            walk = b.side > 0f ? moves.boarderLeft : moves.boarderRight;
            if (Random.value < dt * 0.3f) b.side = -b.side;
        }
        Place(b.rig.root, b.Position + step * (boarderSpeed * dt));
        b.rig.Play(walk, false, 1f, 0.2f);
    }

    void HitHero(Boarder b, Vector3 to, float now)
    {
        train.Player.Damage(blow * (1f + Combat.Instance.damagePerLevel * train.Difficulty));
        Combat.Instance.Burst("spark", hero.root.position + Vector3.up * 1.3f, 0.9f);
        Sound.Play("wagonbad", 0.35f);
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
        Vector3 eye = target - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * 5.2f - Inside;
        eye.x = Mathf.Clamp(eye.x, -HalfLength + 0.3f, HalfLength - 0.3f);
        eye.z = Mathf.Clamp(eye.z, -HalfWidth + 0.3f, HalfWidth - 0.3f);
        eye.y = Mathf.Clamp(eye.y, 0.6f, Height - 0.35f);
        view.transform.position = Inside + eye;
        view.transform.LookAt(target);
    }

    void End(string key)
    {
        Active = false;
        view.enabled = false;
        if (room) room.gameObject.SetActive(false);
        board.gameObject.SetActive(false);
        Time.timeScale = 1f;
        if (key != null) train.Toast(L10n.T(key, kills, kills * moneyPerBoarder));
    }

    void OnDestroy()
    {
        hero?.Free();
        foreach (Boarder b in boarders) b.rig.Free();
    }
}

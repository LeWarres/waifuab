using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.SceneManagement;

// The train never moves: it sits at the origin and the track scrolls under it.
// Enemies and bullets live in a fixed frame, and no track is ever built beyond the view.
public class TrainSim : MonoBehaviour
{
    [Header("Train")]
    public int wagons = 2;             // armed wagons at the start; the first one is the locomotive. More are bought, one per biome
    public Vector3 wagonSize = new Vector3(5f, 2f, 2f);
    public float wagonGap = 0.5f;
    public float maxSpeed = 20f;       // cruise speed of a normal leg
    public float acceleration = 6f;
    public float wagonMaxHealth = 50f; // train health is the sum of its armed wagons
    public Turret turretPrefab;

    [Header("Upgrades")]
    public int money;
    public int moneyReward = 50;
    public float repairReward = 25f;                    // health given back to every wagon
    public int repairCost = 75;                         // first paid repair; each one bought raises the price by this much
    [Range(0f, 1f)] public float stationRepair = 0.25f; // share of health a destroyed wagon gets back at a station
    public float healReward = 10f;                      // health the hero gets per purchase
    public int healCost = 40;
    public int skipMoney = 25;                          // skipping the upgrade gives this...
    public float skipHealth = 10f;                      // ...or this much health to every wagon and the hero
    public float vitalityReward = 10f;                  // max health per hero vitality upgrade
    public int biomeDifficulty = 3;                     // a biome change is worth this many stations of difficulty
    [Range(0f, 1f)] public float cargoCost = 0.4f;      // share of its pay a cargo wagon costs to load
    [Range(0f, 1f)] public float bonusOfferChance = 0.25f; // a wagon offer turns into a free cargo wagon or an escort
    public int maxCargo = 3;                            // cargo wagons loaded at a station
    public int maxCars = 12;                            // longest the train ever gets

    [Header("Shop")]
    public int kitCost = 60;
    public float kitHeal = 20f;                         // a kit repairs every standing wagon and the hero, on the road
    public int maxKits = 3;
    public int wagonCost = 200, wagonCostStep = 100;       // first armed wagon, and how much dearer each next one is
    public int containerCost = 150, containerCostStep = 75; // same for the permanent cargo wagons

    [Header("Track")]
    public float viewRange = 80f;
    public float sleeperSpacing = 1.5f;
    public float poleSpacing = 15f;
    public float stationGap = 400f;    // length of a normal leg

    [Header("Camera")]
    public float cameraSize = 14f;

    // One wagon of the train. Armed wagons carry a weapon; cargo wagons carry goods for the next station.
    class Car
    {
        public Transform body;
        public Material[] skin;     // its materials while it is alive
        public float lift;          // height of the body's origin over the track
        public Vector3 pos;         // on the ground, under its middle
        public float heading;       // degrees around Y; not 0 only while it is past a curve's knee
        public float health, maxHealth;
        public Turret mount;
        public int cargo = -1;      // index into Cargos, -1 for an armed wagon
        public bool permanent;      // bought container: stays for good, pays at every station, never fined
        public bool temporary;      // won as an upgrade (free cargo or armed escort): leaves at the next station
        public int reward, penalty; // fixed when the cargo is loaded
        public bool Alive => health > 0f;
    }

    // Goods: (text key, colour, wagon health, pay on delivery, fine if destroyed).
    // Wood is the safe one, rock is tough, weapons pay best and break easily.
    static readonly (string id, Color color, float health, int reward, int penalty)[] Cargos =
    {
        ("wood", new Color(0.55f, 0.35f, 0.15f), 40f, 15, 15),
        ("rock", new Color(0.5f, 0.5f, 0.5f), 80f, 20, 25),
        ("arms", new Color(0.2f, 0.35f, 0.2f), 25f, 45, 45),
        ("container", new Color(0.15f, 0.3f, 0.6f), 60f, 10, 0), // the bought, permanent one: never loaded at a station
    };
    const int Loadable = 3, Container = 3;

    public float Speed { get; private set; }
    public bool AtStation { get; private set; }
    public int Stations { get; private set; } // stations reached so far, drives the difficulty
    public Player Player { get; private set; }
    public int Biome { get; private set; }
    public int Difficulty => Stations + Biome * biomeDifficulty;
    // Half length (x) and half width (z) of the whole train.
    public Vector2 HalfExtents => new Vector2((cars.Count * Step - wagonGap) * 0.5f, wagonSize.z * 0.5f);
    // Only armed wagons count: the run ends when all of them are down, whatever happens to the cargo.
    public float Health { get { float sum = 0f; foreach (Car c in cars) if (c.cargo < 0) sum += c.health; return sum; } }
    // For the HUD.
    public string BiomeLabel => BiomeName(Biome);
    public string LegLabel => LegName(leg);
    public float LegProgress => AtStation ? 1f : 1f - remaining / LegLength;
    public int Kits => kits;
    public string KitButton => !Player.UsingGamepad ? "R" : Gamepad.current is DualShockGamepad ? "△" : "Y";
    public float CarFill(int i) => cars[i].health / cars[i].maxHealth;
    public Vector3 CarTop(int i) => cars[i].pos + Vector3.up * (1.5f + wagonSize.y);
    public bool MenuShown => menuView.Visible;
    public bool ShowCarNumbers => MenuOpen && !Paused && (menuKind == 3 || menuKind == 6);
    public EventPanel Panel { get; private set; } // the card the button games are played on
    public void Toast(string text) => hud.Toast(text);
    Hud hud;
    public float MaxHealth { get { float sum = 0f; foreach (Car c in cars) if (c.cargo < 0) sum += c.maxHealth; return sum; } }
    public bool CanTakeLoot => cars.Count < maxCars;
    public bool Damaged => Health < MaxHealth || Player.Health < Player.maxHealth;
    public bool InEvent => turn.Active || jump.Active || zig.Active; // a button game is on: the hero waits
    public bool Bending => knees.Count > 0; // a curve or zigzag is somewhere on screen
    public bool Zigzag => eventKind == 4 && Bending;

    // How demanding the button games are, 0 (start) to 1 (hardest): they speed up with the difficulty, up to a limit.
    public int hardestAt = 30;
    public float Tempo => Mathf.Clamp01(Difficulty / (float)hardestAt);
    float Step => wagonSize.x + wagonGap;

    // A looping row of scenery pieces (sleepers, poles) that can follow the track around a curve.
    class Strip
    {
        public Transform parent;
        public Vector3[] bases; // where each piece sits on a straight track
        public float spacing, offset;
        public bool bent;
    }

    Strip sleepers, poles;
    Transform station;
    Transform[] rails;                   // one straight stretch each: up to the first knee, then one per knee
    float remaining, stationX;

    // Curves. The track runs straight to the first knee, turns there, runs to the next one, and so on.
    // Each knee is (distance ahead of the train's middle, turn in degrees). They scroll back through the train;
    // once the last one is out of sight behind, the world is rotated so the train lies along X again and the
    // camera keeps whatever view that leaves. A single curve is one knee, a zigzag is several.
    const int MaxKnees = 8;
    readonly List<Vector2> knees = new();
    readonly Vector3[] kneePos = new Vector3[MaxKnees], kneeDir = new Vector3[MaxKnees];
    readonly float[] kneeHeading = new float[MaxKnees];
    Vector3 pathOrigin;                  // where the train's middle falls on that polyline
    float camYaw = 45f;                  // view of the track: 45 up-right, 0 across, -45 down-right, 90 up, -90 down
    static readonly float[] Views = { -90f, -45f, 0f, 45f, 90f };
    int eventKind, nextEvent;            // this trip's event: 1 curve, 2 ramp jump, 3 needle jump, 4 zigzag
    ZigzagEvent zig;

    [Header("Zigzag")]
    public float zigAngle = 40f;         // how far each stretch leans off the original heading
    public float zigSpacing = 24f;       // track between one turn and the next
    public float zigStart = 55f;         // the first turn appears this far ahead
    public float zigSpeed = 1.3f;        // the train runs this much faster through it
    public float zigZoom = 1.6f;         // and the camera pulls back this much

    public int KneeCount => knees.Count;
    public float KneeS(int i) => knees[i].x;
    public float KneeTurn(int i) => knees[i].y;
    public Vector3 KneePoint(int i) => kneePos[i] - pathOrigin;
    public float FrontS => WagonX(0) + wagonSize.x * 0.5f; // nose of the locomotive
    bool sequenceStarted;

    // The jump: a gap in the track with a ramp, scrolled in like a curve's knee. JumpEvent runs the button game
    // and reads the train through the members below; here the wagons just hop as they cross it.
    JumpEvent jump;
    Transform gap;
    float gapS;                          // distance from the train's middle to the ramp, ahead positive
    public bool Jumping { get; private set; }
    public float GapS => gapS;
    public int CarCount => cars.Count;
    public float CarSpacing => Step;
    public float CarS(int i) => WagonX(i);
    public bool CarAlive(int i) => cars[i].Alive;
    public void HurtCar(int i, float share) => Damage(i, cars[i].maxHealth * share);
    readonly List<Car> cars = new();     // front to back
    Material wagonMat, deadMat, groundMat, sceneryMat;
    Material[] cargoMats;
    // index into Weapon.Wagon, its Length for money, FreeCargo + cargo, or Escort + weapon
    readonly int[] offers = new int[3];
    const int FreeCargo = 100, Escort = 200;
    Material escortMat;
    GameObject wagonModel;
    Transform loco; // the locomotive: only for show, it rides the track one step ahead of the first wagon
    float moneyCarry; // fraction of a coin left over by the hero's money bonus
    readonly int[] heroOffers = new int[3]; // index into Weapon.Hero (ammo), then speed, armor, vitality, damage, money, evolve
    readonly List<int> offerPool = new();
    Weapon pending;                      // weapon chosen at the station, waiting for a wagon
    int pendingCargo = -1;               // cargo chosen at the station, waiting for a position
    int cargoEarned, cargoFined;         // what the last delivery paid and cost
    int repairsBought, wagonsBought, containersBought, kits;
    // One of each per biome reached, so a longer train means moving on.
    bool WagonForSale => wagonsBought <= Biome && cars.Count < maxCars;
    bool ContainerForSale => containersBought <= Biome && cars.Count < maxCars;
    int WagonPrice => wagonCost + wagonCostStep * wagonsBought;
    int ContainerPrice => containerCost + containerCostStep * containersBought;
    int RepairPrice => repairCost * (repairsBought + 1);
    int stage;                           // station menus in order: 0 wagon upgrade, 1 hero upgrade, 2 cargo, 3 shop, 4 route
    int skipGift;                        // what the last skip paid: 0 nothing, 1 money, 2 health

    // Kinds of trip between stations: (text key, length x, speed x). Rolled on arrival, shown before leaving.
    static readonly (string id, float length, float speed)[] Legs =
    {
        ("normal", 1f, 1f), ("long", 2f, 1f), ("express", 1.5f, 1.7f), ("slow", 0.75f, 0.5f),
    };
    int leg, nextLeg;
    TurnEvent turn;
    bool turned;                         // this leg's curve already happened
    [Range(0f, 1f)] public float turnAt = 0.5f; // share of the trip where the curve comes
    public float warnSeconds = 1.5f;                     // how long before a curve or jump the warning shows
    [Range(0.1f, 1f)] public float approachSlow = 0.7f;  // time scale reached just before it starts
    public AudioClip warningSound;                       // played once when the warning appears, if assigned
    bool warned;
    float LegLength => stationGap * Legs[leg].length;
    static string LegName(int leg) => L10n.T("leg." + Legs[leg].id);

    // The open menu as a flat list, so mouse, keyboard and gamepad all drive the same options.
    public struct Option
    {
        public string icon, label;
        public System.Action run;
        public bool enabled;
        public Vector3? anchor; // world spot this option is about; framed in yellow while focused
        public Vector2 extent;  // half size (x, z) of that frame
    }
    readonly Combat.Batch highlight = new Combat.Batch(new Color(1f, 0.82f, 0.1f), false);
    readonly List<Option> options = new();
    StationMenu menuView; // draws the station menus and the game-over question
    string menuTitle;
    int menuKind = -1, focus;
    bool navHeld;
    InputAction navigate, confirm, stepRight, stepLeft;
    bool MenuOpen => AtStation || Health <= 0f || Paused;

    // Pause: Start on a gamepad, Esc on the keyboard. Freezes time and opens its own menu.
    public bool Paused { get; private set; }
    float scaleBeforePause; // a curve or jump may have been running in slow motion
    int confirming;         // pause menu asking "are you sure?": 0 no, 1 restart, 2 quit

    void SetPaused(bool pause)
    {
        if (pause == Paused) return;
        Paused = pause;
        confirming = 0;
        if (pause) scaleBeforePause = Time.timeScale;
        Time.timeScale = pause ? 0f : scaleBeforePause;
    }
    static string BiomeName(int biome) => L10n.T("biome." + Biomes[biome].id);

    // In travel order; the last one is the final world.
    static readonly (string id, Color ground, Color scenery)[] Biomes =
    {
        ("desert", new Color(0.76f, 0.66f, 0.42f), new Color(0.3f, 0.2f, 0.1f)),
        ("snow", new Color(0.9f, 0.93f, 0.97f), new Color(0.1f, 0.3f, 0.2f)),
        ("jungle", new Color(0.15f, 0.4f, 0.12f), new Color(0.25f, 0.15f, 0.05f)),
        ("volcano", new Color(0.2f, 0.08f, 0.06f), new Color(1f, 0.35f, 0f)),
        ("space", new Color(0.03f, 0.03f, 0.08f), new Color(0.7f, 0.7f, 0.8f)),
        ("alien", new Color(0.4f, 0.15f, 0.5f), new Color(0.1f, 0.9f, 0.8f)),
        ("fortress", new Color(0.12f, 0.16f, 0.16f), new Color(0.3f, 1f, 0.2f)),
    };

    void Awake()
    {
        Material wood = Mat(new Color(0.3f, 0.2f, 0.1f)), steel = Mat(Color.gray), stone = Mat(Color.white);
        groundMat = Mat(Biomes[0].ground, false);
        sceneryMat = Mat(Biomes[0].scenery);
        wagonMat = Mat(new Color(0.6f, 0.15f, 0.15f));
        deadMat = Mat(Color.black);
        escortMat = Mat(new Color(0.85f, 0.65f, 0.2f));
        cargoMats = new Material[Cargos.Length];
        for (int i = 0; i < Cargos.Length; i++) cargoMats[i] = Mat(Cargos[i].color);

        float length = viewRange * 4f;
        Box(transform, new Vector3(0f, -0.5f, 0f), new Vector3(length * 2f, 1f, length * 2f), groundMat, true);
        // Rails are uniform, so they never scroll: one pair up to the knee, one pair beyond it.
        rails = new Transform[MaxKnees + 1];
        for (int i = 0; i < rails.Length; i++)
        {
            // One unit long from its pivot; stretched to the length of its part of the track.
            rails[i] = new GameObject("Rails").transform;
            rails[i].SetParent(transform, false);
            Box(rails[i], new Vector3(0.5f, 0.15f, 0.8f), new Vector3(1f, 0.1f, 0.15f), steel);
            Box(rails[i], new Vector3(0.5f, 0.15f, -0.8f), new Vector3(1f, 0.1f, 0.15f), steel);
        }
        nextEvent = Random.Range(1, 5);

        zig = gameObject.AddComponent<ZigzagEvent>();
        menuView = gameObject.AddComponent<StationMenu>();
        hud = gameObject.AddComponent<Hud>();
        Panel = gameObject.AddComponent<EventPanel>();
        turn = gameObject.AddComponent<TurnEvent>();
        jump = gameObject.AddComponent<JumpEvent>();
        wagonModel = Resources.Load<GameObject>("Models/Wagon");
        loco = Instantiate(Resources.Load<GameObject>("Models/Locomotive"), transform).transform;
        loco.localScale = Vector3.one * 1.3f; // as wide as a wagon
        Toon(loco.GetComponentInChildren<Renderer>());
        for (int i = 0; i < wagons; i++) AddCar(i, -1);
        Mount(0, Weapon.Wagon[0]);
        Player = new GameObject("Player").AddComponent<Player>();
        Player.Init(this);
        navigate = InputSystem.actions.FindAction("Player/Move", true);
        confirm = InputSystem.actions.FindAction("Player/Jump", true);
        stepRight = InputSystem.actions.FindAction("Player/Next", true);
        stepLeft = InputSystem.actions.FindAction("Player/Previous", true);

        sleepers = Row("Sleepers", sleeperSpacing, new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.1f, 3f), wood);
        poles = Row("Poles", poleSpacing, new Vector3(0f, 2f, 10f), new Vector3(0.3f, 4f, 0.3f), sceneryMat);

        station = new GameObject("Station").transform;
        station.SetParent(transform, false);
        Box(station, new Vector3(0f, 0.3f, 5.5f), new Vector3(30f, 0.6f, 5f), stone);
        Box(station, new Vector3(0f, 2.1f, 7f), new Vector3(12f, 3f, 2f), sceneryMat);
        gap = new GameObject("Gap").transform;
        gap.SetParent(transform, false);
        Box(gap, new Vector3(3f, 0.16f, 0f), new Vector3(6f, 0.3f, 4f), deadMat); // the missing stretch of track
        Box(gap, new Vector3(-1.6f, 0.5f, 0f), new Vector3(3.4f, 0.2f, 2.6f), steel).localRotation = Quaternion.Euler(0f, 0f, 14f); // ramp up to it
        gap.gameObject.SetActive(false);

        remaining = stationX = stationGap;
        PlaceTrack();

        Camera.main.orthographic = true;
        Camera.main.orthographicSize = cameraSize;
        AimCamera();
    }

    void AimCamera()
    {
        Transform cam = Camera.main.transform;
        cam.rotation = Quaternion.Euler(30f, camYaw, 0f);
        cam.position = -cam.forward * 60f;
    }

    // Works out where every knee is and which way the track runs after it. Cheap; run whenever the knees move.
    void ShapeTrack()
    {
        float heading = 0f;
        Vector3 at = knees.Count > 0 ? new Vector3(knees[0].x, 0f, 0f) : Vector3.zero;
        for (int i = 0; i < knees.Count; i++)
        {
            kneeHeading[i] = heading += knees[i].y;
            kneeDir[i] = Quaternion.Euler(0f, heading, 0f) * Vector3.right;
            kneePos[i] = at;
            if (i + 1 < knees.Count) at += kneeDir[i] * (knees[i + 1].x - knees[i].x);
        }
        pathOrigin = Vector3.zero;
        pathOrigin = Path(0f, out _);
    }

    // Ground position of the track `s` units ahead of the train's middle (negative = behind), and its heading there.
    Vector3 Path(float s, out float heading)
    {
        heading = 0f;
        if (knees.Count == 0 || s <= knees[0].x) return new Vector3(s, 0f, 0f) - pathOrigin;
        int i = 0;
        while (i + 1 < knees.Count && s > knees[i + 1].x) i++;
        heading = kneeHeading[i];
        return kneePos[i] + kneeDir[i] * (s - knees[i].x) - pathOrigin;
    }

    void SetRails(int piece, Vector3 from, float heading, float length)
    {
        rails[piece].gameObject.SetActive(true);
        rails[piece].SetLocalPositionAndRotation(from, Quaternion.Euler(0f, heading, 0f));
        rails[piece].localScale = new Vector3(length, 1f, 1f);
    }

    // Puts rails, sleepers, poles, station and (in a curve) the wagons on the track's current shape.
    void PlaceTrack()
    {
        ShapeTrack();
        float far = viewRange * 4f;
        float firstEnd = knees.Count > 0 ? knees[0].x : far * 0.5f;
        SetRails(0, Path(firstEnd - far, out _), 0f, far);
        for (int i = 0; i < MaxKnees; i++)
        {
            if (i >= knees.Count) rails[i + 1].gameObject.SetActive(false);
            else SetRails(i + 1, kneePos[i] - pathOrigin, kneeHeading[i], i + 1 < knees.Count ? knees[i + 1].x - knees[i].x : far);
        }
        Place(sleepers);
        Place(poles);
        station.localPosition = Path(stationX, out float stationHeading);
        station.localRotation = Quaternion.Euler(0f, stationHeading, 0f);
        if (Jumping)
        {
            gap.localPosition = Path(gapS, out float gapHeading);
            gap.localRotation = Quaternion.Euler(0f, gapHeading, 0f);
        }
        if (Bending || Jumping) Layout();
    }

    void Place(Strip strip)
    {
        if (!Bending)
        {
            // Straight track: the pieces never move, only their parent slides by up to one spacing.
            if (strip.bent)
            {
                for (int i = 0; i < strip.bases.Length; i++)
                    strip.parent.GetChild(i).SetLocalPositionAndRotation(strip.bases[i], Quaternion.identity);
                strip.bent = false;
            }
            strip.parent.localPosition = new Vector3(-strip.offset, 0f, 0f);
            return;
        }
        strip.bent = true;
        strip.parent.localPosition = Vector3.zero;
        for (int i = 0; i < strip.bases.Length; i++)
        {
            Vector3 b = strip.bases[i];
            Vector3 onTrack = Path(b.x - strip.offset, out float heading);
            Quaternion turn = Quaternion.Euler(0f, heading, 0f);
            strip.parent.GetChild(i).SetLocalPositionAndRotation(onTrack + turn * new Vector3(0f, b.y, b.z), turn);
        }
    }

    // The last moments before a curve, jump or zigzag starts: 0 the warning has just come up, 1 the button game begins.
    // -1 when nothing is that close.
    float Approach()
    {
        float now, start;
        if (eventKind == 1 && Bending && !sequenceStarted) { now = knees[0].x; start = HalfExtents.x; }
        else if (Zigzag && !sequenceStarted) { now = knees[0].x; start = FrontS + Speed * 0.6f; }
        else if (Jumping && jump.Pending) { now = gapS; start = jump.StartS; }
        else return -1f;
        float window = Mathf.Max(Speed, 1f) * warnSeconds * 0.85f; // 0.85: about the average time scale on the way in
        return now - start > window ? -1f : Mathf.Clamp01(1f - (now - start) / window);
    }
    public bool Warning => Approach() >= 0f;

    void EndJump()
    {
        Jumping = false;
        gap.gameObject.SetActive(false);
        Layout(); // wagons back on the rails
    }

    // A curve appears ahead, towards one of the other views (never more than a quarter turn away).
    void StartBend()
    {
        float target;
        do target = Views[Random.Range(0, Views.Length)];
        while (target == camYaw || Mathf.Abs(target - camYaw) > 90f);
        knees.Add(new Vector2(viewRange, camYaw - target));
        sequenceStarted = false;
        turn.Prepare();
    }

    // 4 to 6 sharp turns in a row, leaning one way then the other; the last one puts the track back on its heading.
    void StartZigzag()
    {
        int count = Random.Range(4, 7);
        float lean = Random.value < 0.5f ? zigAngle : -zigAngle, heading = 0f;
        for (int i = 0; i < count; i++)
        {
            float turnBy = i == count - 1 ? -heading : i == 0 ? lean : -2f * heading;
            heading += turnBy;
            knees.Add(new Vector2(zigStart + i * zigSpacing, turnBy));
        }
        sequenceStarted = false;
        zig.Prepare(count);
    }

    // The last knee is out of sight behind: turn the whole world so the train lies along X again.
    // The camera turns with it, so nothing moves on screen and the new view simply stays.
    void EndBend()
    {
        ShapeTrack();
        float total = kneeHeading[knees.Count - 1];
        knees.Clear();
        if (total != 0f)
        {
            Quaternion back = Quaternion.Euler(0f, -total, 0f);
            Combat.Instance.Rotate(back);
            Player.Rotate(back);
            camYaw -= total;
            AimCamera();
        }
        PlaceTrack();
        Layout();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) L10n.NextLocale();
        // ponytail: reads the devices directly, like the medkit and the button games.
        if (Health > 0f && ((Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) ||
                            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
            SetPaused(!Paused);

        // A long train pulls the camera back so it always fits, whichever way the track runs on screen.
        Camera view = Camera.main;
        float wanted = Mathf.Max(cameraSize, (HalfExtents.x + 5f) / view.aspect) * (Zigzag ? zigZoom : 1f);
        view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, wanted, (Paused ? 0f : 9f) * Time.unscaledDeltaTime);

        // Repair kit: triangle / Y on a gamepad, R on the keyboard. Not at stations and not mid-sequence.
        // ponytail: reads the devices directly, like the curve's button sequence.
        if (kits > 0 && !AtStation && !InEvent && !Paused && Health > 0f &&
            ((Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)))
        {
            kits--;
            Repair(kitHeal);
            Player.Heal(kitHeal);
        }

        if (MenuOpen)
        {
            BuildMenu();
            Navigate();
        }
        string help = Paused ? (Player.UsingGamepad ? "pause.help.gamepad" : "pause.help.keyboard")
                             : (Player.UsingGamepad ? "menu.help.gamepad" : "menu.help.keyboard");
        menuView.Render(MenuOpen ? options : null, menuTitle, focus, L10n.T(help), i => options[i].run(), i => focus = i, Paused);

        if (AtStation || Paused) return;

        // Brake curve: fastest speed that can still stop exactly on the station.
        float target = Mathf.Clamp(Mathf.Sqrt(2f * acceleration * remaining), 0.5f, maxSpeed * Legs[leg].speed * (Zigzag ? zigSpeed : 1f));
        Speed = Mathf.MoveTowards(Speed, target, acceleration * Time.deltaTime);
        float step = Mathf.Min(Speed * Time.deltaTime, remaining);
        remaining -= step;

        sleepers.offset = Mathf.Repeat(sleepers.offset + step, sleepers.spacing);
        poles.offset = Mathf.Repeat(poles.offset + step, poles.spacing);

        stationX -= step;
        if (stationX < -viewRange) stationX = remaining; // reuse the one station for the next stop

        // One event per trip, early enough that it is fully behind before the station.
        float room = nextEvent == 4 ? zigStart + 5f * zigSpacing + viewRange + 30f : viewRange * 2f + 20f;
        if (!turned && remaining <= Mathf.Max(LegLength * (1f - turnAt), room))
        {
            turned = true;
            eventKind = nextEvent;
            if (eventKind == 1) StartBend();
            else if (eventKind == 4) StartZigzag();
            else
            {
                Jumping = true;
                gapS = viewRange;
                gap.gameObject.SetActive(true);
                jump.Arm(eventKind - 1);
            }
        }
        if (Jumping)
        {
            gapS -= step;
            if (gapS < -viewRange) EndJump();
        }
        if (Bending)
        {
            for (int i = 0; i < knees.Count; i++) knees[i] -= new Vector2(step, 0f);
            float trainLength = HalfExtents.x * 2f;
            if (!sequenceStarted && eventKind == 1 && knees[0].x <= trainLength * 0.5f)
            {
                // The button sequence lasts exactly as long as the train takes to go through the knee.
                sequenceStarted = true;
                turn.Begin(trainLength / Mathf.Max(Speed, 1f));
            }
            if (!sequenceStarted && eventKind == 4 && knees[0].x <= FrontS + Speed * 0.6f)
            {
                sequenceStarted = true;
                zig.Begin(knees.Count);
            }
            if (knees[knees.Count - 1].x < -viewRange) EndBend();
        }
        PlaceTrack();

        // Time eases down on the way in, so a distracted player has a moment to get ready.
        float approach = Approach();
        if (approach >= 0f && Health > 0f)
        {
            if (eventKind != 4) Time.timeScale = Mathf.Lerp(1f, approachSlow, approach); // the zigzag is taken at full speed
            if (!warned && warningSound) AudioSource.PlayClipAtPoint(warningSound, Camera.main.transform.position);
        }
        warned = approach >= 0f;

        if (remaining <= 0f) Arrive();
    }

    void Arrive()
    {
        if (Bending) EndBend();
        if (Jumping) EndJump();
        Speed = 0f;
        AtStation = true;
        Stations++;

        // Cargo is uncoupled here: paid if it made it, fined if it burned. Armed wagons get patched up.
        cargoEarned = cargoFined = 0;
        for (int i = cars.Count - 1; i >= 0; i--)
        {
            Car car = cars[i];
            if (car.permanent && car.Alive) cargoEarned += Mathf.RoundToInt(Cargos[Container].reward * Legs[leg].length) * (Biome + 1);
            if ((car.cargo < 0 && !car.temporary) || car.permanent)
            {
                // Stays with the train: back on its wheels if it was knocked out.
                if (car.Alive) continue;
                car.health = car.maxHealth * stationRepair;
                SetAlive(car, true);
                continue;
            }
            if (car.Alive) cargoEarned += car.reward; else cargoFined += car.penalty; // both 0 for an escort
            if (car.mount) Destroy(car.mount.gameObject);
            Destroy(car.body.gameObject);
            cars.RemoveAt(i);
        }
        money = Mathf.Max(0, money + cargoEarned - cargoFined);
        Layout();

        Player.OnStation();
        RollOffers();
        stage = 0;
        nextLeg = Random.Range(0, Legs.Length);
    }

    float WagonX(int i) => ((cars.Count - 1) * 0.5f - i) * Step;

    // Inserts a wagon at a position in the train and closes the train up around it.
    void AddCar(int index, int cargo, bool permanent = false)
    {
        var car = new Car { cargo = cargo, permanent = permanent, health = wagonMaxHealth, maxHealth = wagonMaxHealth };
        Vector3 size = wagonSize;
        Material mat = wagonMat;
        if (cargo >= 0)
        {
            size.y *= 0.6f; // flatbed: lower than an armed wagon
            mat = cargoMats[cargo];
            car.health = car.maxHealth = Cargos[cargo].health;
            car.reward = Mathf.RoundToInt(Cargos[cargo].reward * Legs[AtStation ? nextLeg : leg].length) * (Biome + 1);
            car.penalty = Cargos[cargo].penalty * (Biome + 1);
        }
        // Armed wagons are the model (its origin is on the rail); cargo is still a flatbed box.
        car.body = cargo < 0 ? Instantiate(wagonModel, transform).transform : Box(transform, Vector3.zero, size, mat);
        car.lift = cargo < 0 ? 0.1f : 0.2f + size.y * 0.5f;
        if (cargo < 0) Toon(car.body.GetComponentInChildren<Renderer>());
        car.skin = car.body.GetComponentInChildren<Renderer>().sharedMaterials;
        cars.Insert(index, car);
        Layout();
    }

    // Money from kills: the hero's money upgrades multiply it, fractions carry over to the next kill.
    public void Earn(float amount)
    {
        moneyCarry += amount * Player.moneyBonus;
        int whole = (int)moneyCarry;
        money += whole;
        moneyCarry -= whole;
    }

    int CargoReward(int cargo) => Mathf.RoundToInt(Cargos[cargo].reward * Legs[nextLeg].length) * (Biome + 1);
    int CargoPrice(int cargo) => Mathf.RoundToInt(CargoReward(cargo) * cargoCost);

    // Lowest level among the mounted weapons: an escort comes no better than the weakest wagon.
    int WeakestLevel
    {
        get
        {
            int lowest = Weapon.MaxLevel + 1;
            foreach (Car c in cars) if (c.mount) lowest = Mathf.Min(lowest, c.mount.Level);
            return lowest > Weapon.MaxLevel ? 1 : lowest;
        }
    }

    // Upgrade prizes that last one trip, coupled at the tail: a cargo wagon at no cost, or an armed escort.
    void AddTemporary(int cargo, Weapon weapon, int level)
    {
        AddCar(cars.Count, cargo);
        Car car = cars[^1];
        car.temporary = true;
        if (weapon == null) return;
        car.skin = System.Array.ConvertAll(car.skin, _ => escortMat);
        SetAlive(car, true);
        Mount(cars.Count - 1, weapon);
        while (car.mount.Level < level) car.mount.LevelUp();
    }

    // Goods the hero shot down on the way: coupled anywhere behind the locomotive.
    public void AddLoot(int cargo)
    {
        if (CanTakeLoot) AddCar(Random.Range(1, cars.Count + 1), cargo);
    }

    void Layout()
    {
        ShapeTrack();
        for (int i = 0; i < cars.Count; i++)
        {
            Car car = cars[i];
            car.pos = Path(WagonX(i), out car.heading);
            float hop = Hop(WagonX(i));
            car.body.SetLocalPositionAndRotation(car.pos + Vector3.up * (car.lift + hop), Quaternion.Euler(0f, car.heading, 0f));
            if (car.mount) car.mount.transform.position = car.pos + Vector3.up * (0.45f + wagonSize.y + hop);
        }
        float ahead = WagonX(-1) + 0.9f; // longer than a wagon
        Vector3 head = Path(ahead, out float turn);
        loco.SetLocalPositionAndRotation(head + Vector3.up * (0.1f + Hop(ahead)), Quaternion.Euler(0f, turn + 180f, 0f)); // the model's nose is its -x
    }

    // Over the gap (its middle is 3 past the ramp's foot) each wagon flies an arc 5 long either way.
    float Hop(float s)
    {
        if (!Jumping) return 0f;
        float off = (s - gapS - 3f) / 5f;
        return Mathf.Max(0f, 1f - off * off) * 2.2f;
    }

    int CargoCount { get { int n = 0; foreach (Car c in cars) if (c.cargo >= 0 && !c.permanent && !c.temporary) n++; return n; } } // loaded for this trip

    // Ground vector from p to the footprint of a wagon (zero when p is inside it).
    Vector3 ToWagon(int i, Vector3 p)
    {
        Car car = cars[i];
        float halfX = wagonSize.x * 0.5f, halfZ = wagonSize.z * 0.5f;
        Vector3 local = p - car.pos;
        if (car.heading == 0f)
            return new Vector3(Mathf.Clamp(local.x, -halfX, halfX) - local.x, 0f, Mathf.Clamp(local.z, -halfZ, halfZ) - local.z);
        // Wagon turned by a curve: measure in its own axes, answer in the world's.
        Quaternion turn = Quaternion.Euler(0f, car.heading, 0f);
        local = Quaternion.Inverse(turn) * local;
        return turn * new Vector3(Mathf.Clamp(local.x, -halfX, halfX) - local.x, 0f, Mathf.Clamp(local.z, -halfZ, halfZ) - local.z);
    }

    // Direction a wagon is travelling in: +X, except for wagons already past a curve's knee.
    public Vector3 Forward(int wagon) =>
        cars[wagon].heading == 0f ? Vector3.right : Quaternion.Euler(0f, cars[wagon].heading, 0f) * Vector3.right;

    // Closest wagon that is still standing, and the ground vector from p to it. -1 if none.
    public int ClosestWagon(Vector3 p, out Vector3 toWagon)
    {
        int closest = -1;
        float best = float.MaxValue;
        toWagon = default;
        for (int i = 0; i < cars.Count; i++)
        {
            if (!cars[i].Alive) continue;
            Vector3 d = ToWagon(i, p);
            if (d.sqrMagnitude >= best) continue;
            best = d.sqrMagnitude;
            toWagon = d;
            closest = i;
        }
        return closest;
    }

    // Enemy explosion: hurts every standing wagon it reaches.
    public void DamageArea(Vector3 p, float radius, float amount)
    {
        for (int i = 0; i < cars.Count; i++)
            if (cars[i].Alive && ToWagon(i, p).sqrMagnitude <= radius * radius) Damage(i, amount);
    }

    public void DamageAll(float amount)
    {
        for (int i = 0; i < cars.Count; i++)
            if (cars[i].Alive) Damage(i, amount);
    }

    public void Stun(int wagon, float seconds)
    {
        if (cars[wagon].mount) cars[wagon].mount.stunnedUntil = Time.time + seconds;
    }

    public void Damage(int wagon, float amount)
    {
        Car car = cars[wagon];
        car.health = Mathf.Max(0f, car.health - amount);
        if (car.Alive) return;
        SetAlive(car, false);
        if (Health <= 0f) Time.timeScale = 0f; // game over, OnGUI offers the restart
    }

    // A destroyed wagon turns black; its weapon stops until the next station, its cargo is lost.
    void SetAlive(Car car, bool alive)
    {
        car.body.GetComponentInChildren<Renderer>().sharedMaterials = alive ? car.skin : System.Array.ConvertAll(car.skin, _ => deadMat);
        if (car.mount) car.mount.enabled = alive;
    }

    // Same weapon on the same wagon levels it up, anything else replaces it at level 1.
    void Mount(int wagon, Weapon weapon)
    {
        Car car = cars[wagon];
        if (car.mount && car.mount.Weapon == weapon)
        {
            car.mount.LevelUp();
            return;
        }
        if (car.mount) Destroy(car.mount.gameObject);
        car.mount = Instantiate(turretPrefab, car.pos + Vector3.up * (0.45f + wagonSize.y), Quaternion.identity, transform);
        car.mount.Init(weapon);
    }

    // Three different options each: wagon weapons unlocked in this biome plus money (and now and then
    // a one-trip prize instead of one of them), and for the hero mostly stats, plus one other ammo.
    void RollOffers()
    {
        offerPool.Clear();
        for (int i = 0; i < Weapon.Wagon.Length; i++)
            if (Weapon.Wagon[i].firstBiome <= Biome) offerPool.Add(i);
        offerPool.Add(Weapon.Wagon.Length);
        Draw(offers);
        if (cars.Count < maxCars && Random.value < bonusOfferChance)
            offers[Random.Range(0, offers.Length)] = Random.value < 0.5f ? FreeCargo + Random.Range(0, Loadable)
                : Escort + offerPool[Random.Range(0, offerPool.Count - 1)]; // the pool's last entry is money

        offerPool.Clear();
        for (int i = 0; i < Weapon.Hero.Length; i++)
            if (Weapon.Hero[i].firstBiome <= Biome && Weapon.Hero[i] != Player.ammo) offerPool.Add(i);
        int ammo = offerPool.Count > 0 ? offerPool[Random.Range(0, offerPool.Count)] : -1;
        offerPool.Clear();
        if (ammo >= 0) offerPool.Add(ammo);                                        // one other ammo at most
        offerPool.Add(Weapon.Hero.Length);                                        // speed
        if (Player.armor < Player.MaxArmor) offerPool.Add(Weapon.Hero.Length + 1); // armor
        offerPool.Add(Weapon.Hero.Length + 2);                                    // vitality
        offerPool.Add(Weapon.Hero.Length + 3);                                    // damage
        offerPool.Add(Weapon.Hero.Length + 4);                                    // money
        if (Player.level < Weapon.MaxLevel) offerPool.Add(Weapon.Hero.Length + 5); // evolve
        Draw(heroOffers);
    }

    void Draw(int[] into)
    {
        for (int i = 0; i < into.Length; i++)
        {
            int pick = Random.Range(0, offerPool.Count);
            into[i] = offerPool[pick];
            offerPool.RemoveAt(pick);
        }
    }

    // Closes the current station menu and opens the next one.
    void Next()
    {
        skipGift = 0;
        pending = null;
        pendingCargo = -1;
        stage++;
    }

    void Repair() => Repair(repairReward);

    public void Repair(float amount)
    {
        foreach (Car car in cars)
            if (car.Alive) car.health = Mathf.Min(car.maxHealth, car.health + amount); // a wreck waits for the station
    }

    // No upgrade taken: a coin flip between some money and some health for train and hero.
    void Skip()
    {
        bool full = Health >= MaxHealth && Player.Health >= Player.maxHealth; // health would be wasted
        skipGift = full || Random.value < 0.5f ? 1 : 2;
        if (skipGift == 1) money += skipMoney;
        else
        {
            Repair(skipHealth);
            Player.Heal(skipHealth);
        }
        pending = null;
        stage++;
    }

    void Depart(bool nextBiome = false)
    {
        if (nextBiome)
        {
            Biome++;
            Tint(groundMat, Biomes[Biome].ground);
            Tint(sceneryMat, Biomes[Biome].scenery);
        }
        skipGift = 0;
        pending = null;
        leg = nextLeg;
        turned = false;
        nextEvent = Random.Range(1, 5);
        remaining = LegLength;
        AtStation = false;
    }

    // Menu title, led by what the last skip paid (if the previous menu was skipped).
    string Noted(string title) =>
        skipGift == 1 ? L10n.T("menu.skip.money", skipMoney) + "   ·   " + title
        : skipGift == 2 ? L10n.T("menu.skip.health", skipHealth) + "   ·   " + title : title;

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Add(string icon, string label, System.Action run, bool enabled = true, Vector3? anchor = null, Vector2 extent = default) =>
        options.Add(new Option { icon = icon, label = label, run = run, enabled = enabled, anchor = anchor, extent = extent });

    // Rebuilt every frame the menu is open; the state decides which menu it is.
    void BuildMenu()
    {
        options.Clear();
        int kind;
        if (Paused && confirming != 0)
        {
            // "No" comes first, so it is the one focused when the question opens.
            kind = 9;
            menuTitle = L10n.T(confirming == 1 ? "pause.restart.ask" : "pause.quit.ask");
            Add("no", L10n.T("menu.no"), () => confirming = 0);
            if (confirming == 1) Add("yes", L10n.T("menu.yes"), Restart);
            else Add("yes", L10n.T("menu.yes"), Application.Quit);
        }
        else if (Paused)
        {
            kind = 8;
            menuTitle = L10n.T("menu.pause");
            Add("resume", L10n.T("pause.resume"), () => SetPaused(false));
            Add("language", L10n.T("language"), L10n.NextLocale);
            Add("restart", L10n.T("pause.restart"), () => confirming = 1);
            Add("quit", L10n.T("pause.quit"), () => confirming = 2);
        }
        else if (Health <= 0f)
        {
            kind = 0;
            menuTitle = L10n.T("menu.gameover");
            Add("yes", L10n.T("menu.yes"), Restart);
            Add("no", L10n.T("menu.no"), Application.Quit);
        }
        else if (stage >= 4)
        {
            kind = 1;
            menuTitle = L10n.T("menu.route", LegName(nextLeg));
            Add("stay", L10n.T("menu.stay", BiomeName(Biome)), () => Depart());
            if (Biome < Biomes.Length - 1)
                Add("biome", L10n.T("menu.change", BiomeName(Biome + 1), biomeDifficulty, Biome + 2), () => Depart(true));
        }
        else if (stage == 3)
        {
            // Shop: health now, health for later, and a longer train (one wagon of each kind per biome).
            kind = 7;
            menuTitle = L10n.T("menu.shop");
            Add("repair", L10n.T("menu.repair", repairReward, RepairPrice), () => { money -= RepairPrice; repairsBought++; Repair(); },
                money >= RepairPrice && Health < MaxHealth);
            Add("heal", L10n.T("menu.heal", healReward, healCost), () => { money -= healCost; Player.Heal(healReward); },
                money >= healCost && Player.Health < Player.maxHealth);
            Add("medkit", L10n.T("shop.kit", kitHeal, kitCost, kits, maxKits), () => { money -= kitCost; kits++; }, money >= kitCost && kits < maxKits);
            Add("wagon", WagonForSale ? L10n.T("shop.wagon", WagonPrice) : L10n.T("shop.wagon.limit"),
                () => { money -= WagonPrice; wagonsBought++; AddCar(cars.Count, -1); }, WagonForSale && money >= WagonPrice);
            Add("container", ContainerForSale ? L10n.T("shop.container", ContainerPrice) : L10n.T("shop.container.limit"),
                () => { money -= ContainerPrice; containersBought++; AddCar(cars.Count, Container, true); }, ContainerForSale && money >= ContainerPrice);
            Add("yes", L10n.T("shop.done"), Next);
        }
        else if (stage == 2 && pendingCargo < 0)
        {
            // Cargo: up to maxCargo wagons, each paid at the next station if it survives.
            kind = 5;
            int loaded = CargoCount;
            menuTitle = Noted(L10n.T("menu.cargo", loaded, maxCargo));
            for (int i = 0; i < Loadable; i++)
            {
                int cargo = i;
                Add(Cargos[i].id, L10n.T("cargo.card", L10n.T("cargo." + Cargos[i].id), CargoReward(i), Cargos[i].health, Cargos[i].penalty * (Biome + 1), CargoPrice(i)),
                    () => pendingCargo = cargo, loaded < maxCargo && money >= CargoPrice(i));
            }
            Add("yes", L10n.T("cargo.done"), Next);
        }
        else if (stage == 2)
        {
            // Where to couple it: behind any wagon, never ahead of the locomotive.
            kind = 6;
            menuTitle = L10n.T("cargo.place", L10n.T("cargo." + Cargos[pendingCargo].id));
            for (int i = cars.Count; i >= 1; i--)
            {
                int index = i;
                Add("slot", L10n.T("cargo.slot", i), () =>
                    {
                        money -= CargoPrice(pendingCargo);
                        AddCar(index, pendingCargo);
                        pendingCargo = -1;
                        if (CargoCount >= maxCargo) Next();
                    },
                    true, new Vector3(WagonX(i - 1) - Step * 0.5f, 1f, 0f), new Vector2(0.3f, wagonSize.z * 0.5f + 0.15f));
            }
            Add("back", L10n.T("menu.back"), () => pendingCargo = -1);
        }
        else if (stage == 1)
        {
            kind = 4;
            menuTitle = Noted(L10n.T("menu.hero"));
            foreach (int offer in heroOffers)
            {
                int stat = offer - Weapon.Hero.Length;
                if (stat < 0)
                {
                    Weapon weapon = Weapon.Hero[offer];
                    Add(weapon.id, L10n.T("hero.ammo", weapon.Name, weapon.Info), () => { Player.ammo = weapon; Next(); });
                }
                else if (stat == 0) Add("speed", L10n.T("hero.speed"), () => { Player.SpeedUp(); Next(); });
                else if (stat == 1) Add("armor", L10n.T("hero.armor"), () => { Player.ArmorUp(); Next(); });
                else if (stat == 2) Add("vitality", L10n.T("hero.vitality", vitalityReward), () => { Player.VitalityUp(vitalityReward); Next(); });
                else if (stat == 3) Add("arms", L10n.T("hero.damage"), () => { Player.DamageUp(); Next(); });
                else if (stat == 4) Add("money", L10n.T("hero.greed"), () => { Player.GreedUp(); Next(); });
                else Add(Player.ammo.id, L10n.T("hero.evolve", Player.ammo.Name, Player.level + 1), () => { Player.level++; Next(); });
            }
            Add("skip", L10n.T("menu.skip"), Skip);
        }
        else if (pending == null)
        {
            kind = 2;
            menuTitle = L10n.T("menu.upgrade");
            if (cargoEarned > 0 || cargoFined > 0) menuTitle = L10n.T("cargo.result", cargoEarned, cargoFined) + "   ·   " + menuTitle;
            foreach (int offer in offers)
            {
                if (offer < Weapon.Wagon.Length)
                {
                    Weapon weapon = Weapon.Wagon[offer];
                    Add(weapon.id, weapon.Name + "\n" + weapon.Info, () => pending = weapon);
                }
                else if (offer == Weapon.Wagon.Length) Add("money", L10n.T("menu.money", moneyReward), () => { money += moneyReward; Next(); });
                else if (offer < Escort)
                {
                    int cargo = offer - FreeCargo;
                    Add(Cargos[cargo].id, L10n.T("offer.cargo", L10n.T("cargo." + Cargos[cargo].id), CargoReward(cargo), Cargos[cargo].penalty * (Biome + 1)),
                        () => { AddTemporary(cargo, null, 0); Next(); });
                }
                else
                {
                    Weapon weapon = Weapon.Wagon[offer - Escort];
                    int level = WeakestLevel;
                    Add(weapon.id, L10n.T("offer.escort", weapon.Name, level), () => { AddTemporary(-1, weapon, level); Next(); });
                }
            }
            Add("skip", L10n.T("menu.skip"), Skip);
        }
        else
        {
            kind = 3;
            menuTitle = L10n.T("menu.place", pending.Name);
            // Listed as the wagons appear on screen, left to right, so "right" moves the yellow frame right.
            for (int i = cars.Count - 1; i >= 0; i--)
            {
                if (cars[i].cargo >= 0) continue;
                int wagon = i;
                Turret mount = cars[i].mount;
                bool same = mount && mount.Weapon == pending;
                string current = !mount ? L10n.T("menu.empty") : !same ? L10n.T("menu.replace", mount.Weapon.Name, mount.Level)
                    : mount.Level < Weapon.MaxLevel ? L10n.T("menu.levelup", mount.Level + 1) : L10n.T("menu.maxlevel");
                Add(mount ? mount.Weapon.id : "wagon", L10n.T("menu.wagon", i + 1, current), () => { Mount(wagon, pending); Next(); },
                    !same || mount.Level < Weapon.MaxLevel, new Vector3(WagonX(i), 0.4f + wagonSize.y, 0f),
                    new Vector2(wagonSize.x * 0.5f + 0.15f, wagonSize.z * 0.5f + 0.15f));
            }
            Add("back", L10n.T("menu.back"), () => pending = null);
        }

        if (kind != menuKind)
        {
            menuKind = kind;
            focus = 0;
        }
        focus = Mathf.Min(focus, options.Count - 1);
        for (int i = 0; i < options.Count && !options[focus].enabled; i++) focus = (focus + 1) % options.Count;
    }

    // Left/right moves the focus (stick, d-pad, A/D, arrows or Q/E); Jump (cross / A button, Space) confirms.
    void Navigate()
    {
        // The pause menu is a vertical list, so up/down work there too.
        Vector2 move = navigate.ReadValue<Vector2>();
        float x = Paused && Mathf.Abs(move.y) > Mathf.Abs(move.x) ? -move.y : move.x;
        int step = Mathf.Abs(x) > 0.5f ? (int)Mathf.Sign(x) : 0;
        bool tapped = stepRight.WasPressedThisFrame() || stepLeft.WasPressedThisFrame(); // d-pad and Q/E
        if (tapped) step = stepRight.WasPressedThisFrame() ? 1 : -1;
        if (step != 0 && (tapped || !navHeld))
        {
            do focus = (focus + step + options.Count) % options.Count;
            while (!options[focus].enabled);
        }
        navHeld = step != 0 && !tapped;
        if (confirm.WasPressedThisFrame() && options[focus].enabled) options[focus].run();
    }

    // Yellow frame and arrow in the world on whatever the focused option refers to.
    void LateUpdate()
    {
        // Warning for the curve, jump or zigzag ahead: what is coming and which button it takes.
        if (Paused || Approach() < 0f) hud.Warning(null, null);
        else
        {
            string button = JumpEvent.ButtonName(Player.UsingGamepad);
            hud.Warning(L10n.T(eventKind == 1 ? "warn.curve.title" : eventKind == 4 ? "warn.zig.title" : "warn.jump.title"),
                eventKind == 1 ? L10n.T("warn.curve") : eventKind == 4 ? L10n.T("warn.zig", L10n.T(Player.UsingGamepad ? "zig.stick" : "zig.keys"))
                : L10n.T(eventKind == 2 ? "warn.ramp" : "warn.needle", button));
        }

        if (!MenuOpen || options.Count == 0 || options[focus].anchor == null) return;
        Vector3 c = options[focus].anchor.Value;
        Vector2 e = options[focus].extent;
        highlight.Add(c + Vector3.forward * e.y, new Vector3(e.x * 2f + 0.3f, 0.3f, 0.3f));
        highlight.Add(c + Vector3.back * e.y, new Vector3(e.x * 2f + 0.3f, 0.3f, 0.3f));
        highlight.Add(c + Vector3.right * e.x, new Vector3(0.3f, 0.3f, e.y * 2f + 0.3f));
        highlight.Add(c + Vector3.left * e.x, new Vector3(0.3f, 0.3f, e.y * 2f + 0.3f));
        highlight.Add(c + Vector3.up * (3.5f + Mathf.Sin(Time.unscaledTime * 6f) * 0.4f), new Vector3(0.6f, 2f, 0.6f));
        highlight.Flush();
    }

    // A row of identical pieces looks endless if it wraps by one spacing.
    Strip Row(string name, float spacing, Vector3 offset, Vector3 size, Material mat)
    {
        var strip = new Strip { parent = new GameObject(name).transform, spacing = spacing };
        strip.parent.SetParent(transform, false);
        var bases = new List<Vector3>();
        for (float x = -viewRange; x <= viewRange + spacing; x += spacing)
        {
            bases.Add(offset + Vector3.right * x);
            Box(strip.parent, bases[bases.Count - 1], size, mat);
        }
        strip.bases = bases.ToArray();
        return strip;
    }

    // Every material in the game is a copy of Resources/Toon.mat (RealToon): tune the look there.
    static Material toon;
    static readonly Dictionary<Material, Material> toons = new();
    static readonly int MainColor = Shader.PropertyToID("_MainColor");

    public static Material Mat(Color color, bool outline = true)
    {
        if (!toon) toon = Resources.Load<Material>("Toon");
        var mat = new Material(toon) { enableInstancing = true };
        mat.SetColor(MainColor, color.linear); // an HDR property: Unity does not convert it for us
        if (!outline)
        {
            mat.DisableKeyword("N_F_O_ON");
            mat.SetShaderPassEnabled("SRPDefaultUnlit", false); // the outline's pass
        }
        return mat;
    }

    public static void Tint(Material mat, Color color) => mat.SetColor(MainColor, color.linear); // an HDR property: Unity does not convert it for us

    // An imported model comes with HDRP/Lit materials: swaps each for its toon twin (same colour and texture).
    public static void Toon(Renderer renderer)
    {
        Material[] mats = renderer.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (!toon || mats[i].shader == toon.shader) continue;
            if (!toons.TryGetValue(mats[i], out Material twin))
            {
                twin = Mat(mats[i].color);
                twin.name = mats[i].name;
                twin.mainTexture = mats[i].mainTexture;
                toons[mats[i]] = twin;
            }
            mats[i] = twin;
        }
        renderer.sharedMaterials = mats;
    }

    public static Transform Box(Transform parent, Vector3 position, Vector3 size, Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (!keepCollider) Destroy(go.GetComponent<Collider>()); // scenery must not cost physics
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        return go.transform;
    }
}

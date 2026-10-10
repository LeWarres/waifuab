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
    [System.NonSerialized] public float exposure = 13.7f;                      // fixed camera exposure (EV100), the same in every biome
    public float groundTile = 18f;                      // size on the ground of one repeat of the ground texture
    public float viewRange = 80f;
    public float sleeperSpacing = 1.5f;
    public float stationGap = 400f;    // length of a normal leg

    [Header("Camera")]
    public float cameraSize = 14f;

    // One wagon of the train. Armed wagons carry a weapon; cargo wagons carry goods for the next station.
    class Car
    {
        public Transform body;
        public Material[][] skins;  // its materials while it is alive, per renderer
        public float lift;          // height of the body's origin over the track
        public GameObject fire;     // burning while it is a wreck
        public bool flying;         // over the ravine right now
        public Vector2Int[] painted; // where in skins the body paint is (renderer, material)
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
        ("wood", new Color(0.55f, 0.35f, 0.15f), 40f * Balance.Scale, 15, 15),
        ("rock", new Color(0.5f, 0.5f, 0.5f), 80f * Balance.Scale, 20, 25),
        ("arms", new Color(0.2f, 0.35f, 0.2f), 25f * Balance.Scale, 45, 45),
        ("container", new Color(0.15f, 0.3f, 0.6f), 60f * Balance.Scale, 10, 0), // the bought, permanent one: never loaded at a station
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
    public string BiomeId => Biomes[Biome].id;
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
    public void Flash(string effect) => hud.Flash(effect);
    public void DamageNumber(Vector3 at, float amount, bool onTrain) => hud.DamageNumber(at, amount, onTrain);
    Hud hud;
    public float MaxHealth { get { float sum = 0f; foreach (Car c in cars) if (c.cargo < 0) sum += c.maxHealth; return sum; } }
    public bool CanTakeLoot => cars.Count < maxCars;
    public bool Damaged => Health < MaxHealth || Player.Health < Player.maxHealth;
    public bool InEvent => turn.Active || jump.Active || zig.Active || swarm.Active; // a button game is on: the hero waits
    public bool Swarming => swarm.Aiming;                // seen from the gunner's seat right now
    // The gunner's camera while the swarm is fought, so the HUD can place its damage numbers from that seat.
    public Camera SwarmCamera => swarm != null && swarm.Aiming ? swarm.View : null;
    public Vector3 CarPosition(int i) => cars[i].pos;
    public void Shake(float seconds) => shake = Mathf.Max(shake, seconds);

    // The gunner the swarm is fought from: the first armed wagon still standing (its turret and its number).
    public Transform Gunner(out int wagon)
    {
        for (wagon = 0; wagon < cars.Count; wagon++)
            if (cars[wagon].Alive && cars[wagon].mount) return cars[wagon].mount.transform;
        return null;
    }
    public bool Bending => knees.Count > 0; // a curve or zigzag is somewhere on screen
    // Enemies leave the train alone: a button game is on or about to start, or a curve is passing under the wagons.
    public bool Calm => InEvent || Warning || (Bending && Mathf.Abs(knees[0].x) < HalfExtents.x + 8f);
    public bool Zigzag => eventKind == 4 && Bending;

    // How demanding the button games are, 0 (start) to 1 (hardest): they speed up with the difficulty, up to a limit.
    public int hardestAt = 30;
    public float Tempo => Mathf.Clamp01(Difficulty / (float)hardestAt);
    float Step => wagonSize.x + wagonGap;

    // A looping row of scenery pieces (sleepers, props) that can follow the track around a curve.
    class Strip
    {
        public Transform parent;
        public Vector3[] bases; // where each piece sits on a straight track
        public float spacing, offset;
        public bool bent;
    }

    Strip sleepers;
    Strip[] sleeperStyles;  // one row per sleeper model; the biome picks which one is out
    Material railMat, ballastMat, sleeperMat;

    // The track of each biome, in the order of Biomes: sleeper model, then sleeper, bed and rail colours,
    // and how bright the rails are (above 1 they glow).
    static readonly (int style, Color sleeper, Color bed, Color rail, float glow)[] Tracks =
    {
        (0, new Color(0.42f, 0.27f, 0.14f), new Color(0.66f, 0.56f, 0.38f), new Color(0.45f, 0.47f, 0.5f), 1f),   // desert: wood on sand
        (0, new Color(0.3f, 0.2f, 0.14f), new Color(0.85f, 0.9f, 0.96f), new Color(0.5f, 0.56f, 0.62f), 1f),     // snow: dark wood on snow
        (0, new Color(0.3f, 0.33f, 0.14f), new Color(0.25f, 0.2f, 0.12f), new Color(0.4f, 0.38f, 0.33f), 1f),    // jungle: mossy wood on earth
        (0, new Color(0.5f, 0.34f, 0.2f), new Color(0.42f, 0.29f, 0.17f), new Color(0.5f, 0.52f, 0.56f), 1f),    // ocean: a wooden causeway over the water
        (1, new Color(0.12f, 0.1f, 0.1f), new Color(0.3f, 0.1f, 0.07f), new Color(1f, 0.45f, 0.1f), 2.2f),       // volcano: basalt, red-hot rails
        (1, new Color(0.72f, 0.72f, 0.7f), new Color(0.38f, 0.38f, 0.42f), new Color(0.5f, 0.52f, 0.56f), 1f),   // city: concrete on gravel
        (2, new Color(0.3f, 0.33f, 0.42f), new Color(0.08f, 0.09f, 0.14f), new Color(0.3f, 0.8f, 1f), 2.5f),     // space: metal, blue light
        (2, new Color(0.45f, 0.2f, 0.55f), new Color(0.2f, 0.08f, 0.28f), new Color(0.2f, 1f, 0.8f), 2.5f),      // alien: violet, teal light
        (2, new Color(0.1f, 0.12f, 0.12f), new Color(0.05f, 0.07f, 0.07f), new Color(0.35f, 1f, 0.25f), 2.5f),   // fortress: black, green light
    };
    float groundScroll;
    // Scenery: a fixed number of props scattered along the track. One that scrolls out behind comes back ahead as
    // something else, somewhere else, so the landscape never repeats.
    class Prop
    {
        public Transform body;
        public GameObject kind;  // the prefab it is a copy of
        public float s, side, yaw;
        public int size;         // 0 clutter, 1 ordinary, 2 landmark
    }
    public int[] sceneryCount = { 90, 34, 12 }; // most of each size that can be out at once
    // The landscape comes in stretches, each with its own look: (share of the small, ordinary and big slots in use).
    static readonly Vector3[] Stretches =
    {
        new Vector3(0.75f, 0.7f, 0.6f),   // ordinary
        new Vector3(0.3f, 0.15f, 0.25f),  // open ground
        new Vector3(1f, 1f, 0.5f),        // thick
        new Vector3(0.45f, 0.5f, 1f),     // big things
        new Vector3(1f, 0.25f, 0.1f),     // carpet of small ones
    };
    public Vector2 stretchLength = new Vector2(70f, 150f);
    Vector3 stretch = Stretches[0];
    float stretchLeft;
    GameObject[][] stretchKinds = new GameObject[3][]; // the few kinds, per size, this stretch is made of
    GameObject[][][] sceneryKinds;              // [biome][size]: what Resources/Scenery/<biome> offers
    readonly List<Prop> props = new();
    readonly Dictionary<GameObject, Stack<Transform>> spareProps = new();
    Transform sceneryRoot;
    Transform station;
    GameObject[] stationLooks; // by biome
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
    float camYaw = 45f;                  // view of the track: 45 up-right, 0 across, -45 down-right, 90 up, 180 leftwards...
    static readonly float[] Views = { -135f, -90f, -45f, 0f, 45f, 90f, 135f, 180f }; // all the way round
    // Which side of the screen the driver's right hand is on: below zero the track's "right" looks left.
    public float ScreenSide => Vector3.Dot(Camera.main.transform.right, Vector3.back);
    int eventKind, nextEvent;            // this trip's event: 1 curve, 2 ramp jump, 3 needle jump, 4 zigzag
    ZigzagEvent zig;
    SwarmEvent swarm;

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
    public float FrontS => WagonX(0) + wagonSize.x * 0.5f; // front of the first wagon
    public float NoseS => WagonX(-1) + 4.4f;               // nose of the locomotive (0.9 ahead of its slot, 3.5 half long)
    public float flyRoom = 6f;                             // clear space the picture keeps past both ends of the train
    // Middle of the whole train, locomotive included: what the camera looks at.
    public Vector3 Focus => Vector3.right * ((NoseS - HalfExtents.x) * 0.5f);
    public float Reach => (NoseS + HalfExtents.x) * 0.5f + flyRoom; // from Focus to the edge of that clear space
    // 1 curve, 2 ramp jump, 3 needle jump, 4 zigzag, 5 swarm. The curve twice as likely: it is what changes the view.
    static readonly int[] EventBag = { 1, 1, 2, 3, 4, 5 };
    bool sequenceStarted;

    // The jump: a gap in the track with a ramp, scrolled in like a curve's knee. JumpEvent runs the button game
    // and reads the train through the members below; here the wagons just hop as they cross it.
    JumpEvent jump;
    Transform gap;
    public float jumpHeight = 3.2f;      // top of the arc over the ravine
    float shake;                         // seconds of camera jolt left
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
    GameObject[] cargoModels; // Resources/Models/Cargo_<id>, by cargo
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
        public System.Action<int> adjust; // a setting: left / right change it by a step (-1 or +1)
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
    static bool firstLoad = true, backToMenu; // the title screen shows on the first load, and when we ask for it back
    bool mainMenu, settingsMenu;              // the title screen, and its settings page
    InputAction navigate, confirm, stepRight, stepLeft;
    bool ListMenu => Paused || settingsMenu;  // both draw their options as a vertical list
    bool MenuOpen => mainMenu || AtStation || Health <= 0f || Paused;

    // Pause: Start on a gamepad, Esc on the keyboard. Freezes time and opens its own menu.
    public bool Paused { get; private set; }
    float scaleBeforePause; // a curve or jump may have been running in slow motion
    int confirming;         // pause menu asking "are you sure?": 0 no, 1 restart, 2 quit
    bool soundMenu, sideHeld; // pause menu showing the volumes

    void SetPaused(bool pause)
    {
        if (pause == Paused) return;
        Paused = pause;
        confirming = 0;
        soundMenu = false;
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
        ("ocean", new Color(0.15f, 0.54f, 0.84f), new Color(0.5f, 0.36f, 0.2f)),
        ("volcano", new Color(0.2f, 0.08f, 0.06f), new Color(1f, 0.35f, 0f)),
        ("city", new Color(0.4f, 0.42f, 0.5f), new Color(0.2f, 0.22f, 0.3f)),
        ("space", new Color(0.03f, 0.03f, 0.08f), new Color(0.7f, 0.7f, 0.8f)),
        ("alien", new Color(0.4f, 0.15f, 0.5f), new Color(0.1f, 0.9f, 0.8f)),
        ("fortress", new Color(0.12f, 0.16f, 0.16f), new Color(0.3f, 1f, 0.2f)),
    };

    void Awake()
    {
        // The numbers are designed small and scaled here, once per run: the scene carries the unscaled values,
        // so this is what makes every health pool and payout read ten times bigger. See Balance.Scale.
        wagonMaxHealth *= Balance.Scale;
        repairReward *= Balance.Scale;
        healReward *= Balance.Scale;
        skipHealth *= Balance.Scale;
        vitalityReward *= Balance.Scale;
        kitHeal *= Balance.Scale;

        Material steel = Mat(Color.gray);
        groundMat = Mat(Biomes[0].ground, false);
        groundMat.mainTextureScale = Vector2.one * (viewRange * 8f / groundTile);
        PaintGround();
        sceneryMat = Mat(Biomes[0].scenery);
        wagonMat = Mat(new Color(0.62f, 0.66f, 0.72f)); // an armed wagon with no weapon yet
        deadMat = Mat(Color.black);
        escortMat = Mat(new Color(0.85f, 0.65f, 0.2f));
        cargoMats = new Material[Cargos.Length];
        for (int i = 0; i < Cargos.Length; i++) cargoMats[i] = Mat(Cargos[i].color);

        float length = viewRange * 4f;
        Box(transform, new Vector3(0f, -0.5f, 0f), new Vector3(length * 2f, 1f, length * 2f), groundMat, true);
        // Rails are uniform, so they never scroll: one pair up to the knee, one pair beyond it.
        railMat = Mat(Color.gray);
        ballastMat = Mat(Color.gray, false);
        sleeperMat = Mat(Color.gray, false);
        GameObject railModel = Resources.Load<GameObject>("Models/Track_rail"); // one unit long, from its pivot
        rails = new Transform[MaxKnees + 1];
        for (int i = 0; i < rails.Length; i++)
        {
            // One unit long from its pivot; stretched to the length of its part of the track.
            rails[i] = new GameObject("Rails").transform;
            rails[i].SetParent(transform, false);
            Dress(Instantiate(railModel, rails[i]));
        }
        nextEvent = EventBag[Random.Range(0, EventBag.Length)];

        zig = gameObject.AddComponent<ZigzagEvent>();
        swarm = gameObject.AddComponent<SwarmEvent>();
        menuView = gameObject.AddComponent<StationMenu>();
        hud = gameObject.AddComponent<Hud>();
        Panel = gameObject.AddComponent<EventPanel>();
        turn = gameObject.AddComponent<TurnEvent>();
        jump = gameObject.AddComponent<JumpEvent>();
        wagonModel = Resources.Load<GameObject>("Models/Wagon");
        cargoModels = System.Array.ConvertAll(Cargos, c => Resources.Load<GameObject>("Models/Cargo_" + c.id));
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

        sleeperStyles = new Strip[3];
        for (int i = 0; i < sleeperStyles.Length; i++)
        {
            sleeperStyles[i] = Row("Sleepers", sleeperSpacing, Vector3.zero, Vector3.one, null, Resources.Load<GameObject>("Models/Sleeper_" + i));
            sleeperStyles[i].parent.gameObject.SetActive(false);
        }
        sleepers = sleeperStyles[0];
        PaintTrack();

        // Sort each biome's props by size: small things go near the track and often, big ones far out and seldom.
        sceneryRoot = new GameObject("Scenery").transform;
        sceneryRoot.SetParent(transform, false);
        sceneryKinds = new GameObject[Biomes.Length][][];
        for (int b = 0; b < Biomes.Length; b++)
        {
            var bySize = new[] { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
            foreach (GameObject kind in Resources.LoadAll<GameObject>("Scenery/" + Biomes[b].id))
            {
                Bounds box = default;
                bool any = false;
                foreach (MeshRenderer part in kind.GetComponentsInChildren<MeshRenderer>())
                {
                    if (any) box.Encapsulate(part.bounds); else box = part.bounds;
                    any = true;
                }
                float tall = box.size.y, wide = Mathf.Max(box.size.x, box.size.z);
                bySize[tall < 1.3f && wide < 3f ? 0 : tall > 4.2f || wide > 6f ? 2 : 1].Add(kind);
            }
            sceneryKinds[b] = System.Array.ConvertAll(bySize, list => list.ToArray());
        }
        for (int size = 0; size < sceneryCount.Length; size++)
            for (int i = 0; i < sceneryCount[size]; i++)
            {
                var prop = new Prop { size = size, s = -viewRange + (i + Random.value) * (viewRange * 2f / sceneryCount[size]) };
                props.Add(prop);
                Dress(prop);
            }

        // HDRP adapts exposure to the picture by default, which washes the train out over dark ground.
        var look = new GameObject("Look").AddComponent<UnityEngine.Rendering.Volume>();
        look.isGlobal = true;
        look.priority = 10f;
        var fixedExposure = look.profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(true);
        fixedExposure.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.Fixed;
        fixedExposure.fixedExposure.value = exposure;
        // Bright, clean anime picture: no filmic curve crushing the colours, a little glow, a little more colour.
        look.profile.Add<UnityEngine.Rendering.HighDefinition.Tonemapping>(true).mode.value = UnityEngine.Rendering.HighDefinition.TonemappingMode.Neutral;
        var bloom = look.profile.Add<UnityEngine.Rendering.HighDefinition.Bloom>(true);
        bloom.intensity.value = 0.15f;
        bloom.threshold.value = 1f;
        var grade = look.profile.Add<UnityEngine.Rendering.HighDefinition.ColorAdjustments>(true);
        grade.saturation.value = 8f;
        grade.contrast.value = 6f;

        station = new GameObject("Station").transform;
        station.SetParent(transform, false);
        // One station per biome (Tools/make_stations.py): platform and hall in one model, the biome's own out.
        stationLooks = new GameObject[Biomes.Length];
        for (int b = 0; b < Biomes.Length; b++)
        {
            stationLooks[b] = Instantiate(Resources.Load<GameObject>("Models/Station_" + Biomes[b].id), station);
            Toon(stationLooks[b].GetComponentInChildren<Renderer>());
            stationLooks[b].SetActive(b == 0);
        }
        gap = new GameObject("Gap").transform;
        gap.SetParent(transform, false);
        // A ravine across the track with a launch ramp before it and a landing ramp after (Tools/make_jump.py);
        // its rocks take the colour of the biome's track bed.
        Material hazard = Mat(new Color(1f, 0.72f, 0.05f));
        Renderer jumpSkin = Instantiate(Resources.Load<GameObject>("Models/Jump"), gap).GetComponentInChildren<Renderer>();
        jumpSkin.sharedMaterials = System.Array.ConvertAll(jumpSkin.sharedMaterials, m =>
            m.name == "JumpRock" ? ballastMat : m.name == "JumpSteel" ? steel : m.name == "JumpHazard" ? hazard : deadMat);
        gap.gameObject.SetActive(false);

        remaining = stationX = stationGap;
        PlaceTrack();

        Camera.main.orthographic = true;
        Camera.main.orthographicSize = cameraSize;
        AimCamera();

        // The title screen shows on the first load and whenever we come back to it; otherwise the run starts at once.
        bool boot = firstLoad;
        mainMenu = firstLoad || backToMenu;
        firstLoad = false;
        backToMenu = false;
        Time.timeScale = mainMenu ? 0f : 1f;
        if (boot) Display.Apply();
    }

    void AimCamera()
    {
        Transform cam = Camera.main.transform;
        cam.rotation = Quaternion.Euler(30f, camYaw, 0f);
        cam.position = Focus - cam.forward * 60f;
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

    // Puts rails, sleepers, scenery, station and (in a curve) the wagons on the track's current shape.
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
        foreach (Prop prop in props)
        {
            if (!prop.body) continue;
            Vector3 on = Path(prop.s, out float heading);
            Quaternion turn = Quaternion.Euler(0f, heading, 0f);
            prop.body.SetLocalPositionAndRotation(on + turn * new Vector3(0f, 0f, prop.side), Quaternion.Euler(0f, heading + prop.yaw, 0f));
            // Not through the station: its platform and hall stand on that strip.
            bool show = !(prop.side > 2f && prop.side < 16.5f && Mathf.Abs(prop.s - stationX) < 19f);
            if (prop.body.gameObject.activeSelf != show) prop.body.gameObject.SetActive(show);
        }
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
        float by;
        do by = Mathf.DeltaAngle(Views[Random.Range(0, Views.Length)], camYaw);
        while (Mathf.Abs(by) < 1f || Mathf.Abs(by) > 91f);
        knees.Add(new Vector2(viewRange, by));
        sequenceStarted = false;
        turn.Prepare();
    }

    // 4 to 6 sharp turns in a row, leaning one way then the other; the last one swings on past the old
    // heading, so the train comes out of it going a new way (an eighth or a quarter turn off).
    void StartZigzag()
    {
        int count = Random.Range(4, 7);
        float lean = Random.value < 0.5f ? zigAngle : -zigAngle, heading = 0f;
        for (int i = 0; i < count; i++)
        {
            float turnBy = i == count - 1 ? -heading - Mathf.Sign(heading) * (Random.value < 0.5f ? 45f : 90f) : i == 0 ? lean : -2f * heading;
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
            camYaw = Mathf.DeltaAngle(0f, camYaw - total); // kept within half a turn either way
            AimCamera();
        }
        PlaceTrack();
        Layout();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) L10n.NextLocale();
        // ponytail: reads the devices directly, like the medkit and the button games.
        if (!mainMenu && Health > 0f && ((Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) ||
                            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
            SetPaused(!Paused);

        // A long train pulls the camera back so it always fits, whichever way the track runs on screen.
        Camera view = Camera.main;
        // Big enough for the train from tail to nose plus flyRoom past each end and to each side, whichever way
        // the track runs across the picture.
        float half = Reach, fit = 0f;
        Vector3 right = view.transform.right, up = view.transform.up;
        for (int corner = 0; corner < 4; corner++)
        {
            var point = new Vector3(corner < 2 ? half : -half, 0f, corner % 2 == 0 ? flyRoom : -flyRoom);
            fit = Mathf.Max(fit, Mathf.Abs(Vector3.Dot(point, up)), Mathf.Abs(Vector3.Dot(point, right)) / view.aspect);
        }
        float wanted = Mathf.Max(cameraSize, fit) * (Zigzag ? zigZoom : 1f);
        view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, wanted, (Paused ? 0f : 9f) * Time.unscaledDeltaTime);
        shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime);
        view.transform.position = Focus - view.transform.forward * 60f + Random.insideUnitSphere * shake * 1.5f;

        // Repair kit: triangle / Y on a gamepad, R on the keyboard. Not at stations and not mid-sequence.
        // ponytail: reads the devices directly, like the curve's button sequence.
        if (kits > 0 && !AtStation && !InEvent && !Paused && !mainMenu && Health > 0f &&
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
        string help = soundMenu ? (Player.UsingGamepad ? "sound.help.gamepad" : "sound.help.keyboard")
            : settingsMenu ? (Player.UsingGamepad ? "settings.help.gamepad" : "settings.help.keyboard")
            : Paused ? (Player.UsingGamepad ? "pause.help.gamepad" : "pause.help.keyboard")
                             : (Player.UsingGamepad ? "menu.help.gamepad" : "menu.help.keyboard");
        menuView.Render(MenuOpen ? options : null, menuTitle, focus, L10n.T(help), i => options[i].run(), i => focus = i, ListMenu);

        if (AtStation || Paused || mainMenu) return;

        // Brake curve: fastest speed that can still stop exactly on the station.
        float target = Mathf.Clamp(Mathf.Sqrt(2f * acceleration * remaining), 0.5f, maxSpeed * Legs[leg].speed * (Zigzag ? zigSpeed : 1f));
        Speed = Mathf.MoveTowards(Speed, target, acceleration * Time.deltaTime);
        float step = Mathf.Min(Speed * Time.deltaTime, remaining);
        remaining -= step;

        if (!AtStation && !Zigzag && Speed > maxSpeed) Flash("wind"); // an express leg (not the zigzag: its zoom outgrows the effect)
        // The ground itself never moves: its texture slides back under the train instead.
        groundScroll = Mathf.Repeat(groundScroll + step / groundTile, 1f);
        groundMat.mainTextureOffset = new Vector2(-groundScroll, 0f); // the cube's top face has u running towards -x
        sleepers.offset = Mathf.Repeat(sleepers.offset + step, sleepers.spacing);
        // A new stretch of landscape starts ahead, out of sight: props coming round from now on belong to it.
        stretchLeft -= step;
        if (stretchLeft <= 0f) NewStretch();
        foreach (Prop prop in props)
        {
            prop.s -= step;
            if (prop.s >= -viewRange) continue;
            prop.s += viewRange * 2f + Random.Range(0f, 6f);
            Dress(prop);
        }

        stationX -= step;
        if (stationX < -viewRange)
        {
            // Reuse the one station for the next stop; out of sight is also the moment to change its look.
            stationX = remaining;
            for (int b = 0; b < stationLooks.Length; b++) stationLooks[b].SetActive(b == Biome);
        }

        // One event per trip, early enough that it is fully behind before the station.
        float room = nextEvent == 4 ? zigStart + 5f * zigSpacing + viewRange + 30f : viewRange * 2f + 20f;
        float eventAt = Mathf.Max(LegLength * (1f - turnAt), room);
        if (!turned && remaining <= eventAt)
        {
            turned = true;
            eventKind = nextEvent;
            if (eventKind == 5 && !swarm.Begin()) eventKind = 1; // nobody left to man a gun: a curve instead
            if (eventKind == 1) StartBend();
            else if (eventKind == 4) StartZigzag();
            else if (eventKind == 5) { } // SwarmEvent runs itself from here
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
        // Wagons are models with their origin on the rail; a cargo with no model falls back to a flatbed box.
        GameObject model = cargo < 0 ? wagonModel : cargoModels[cargo];
        car.body = model ? Instantiate(model, transform).transform : Box(transform, Vector3.zero, size, mat);
        car.lift = model ? 0.1f : 0.2f + size.y * 0.5f;
        if (model) Toon(car.body.GetComponentInChildren<Renderer>());
        car.skins = System.Array.ConvertAll(car.body.GetComponentsInChildren<MeshRenderer>(), r => r.sharedMaterials);
        if (cargo < 0) Paint(car, wagonMat);
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
        car.skins = System.Array.ConvertAll(car.skins, skin => System.Array.ConvertAll(skin, _ => escortMat));
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
            car.body.SetLocalPositionAndRotation(car.pos + Vector3.up * (car.lift + hop), Quaternion.Euler(0f, car.heading, Pitch(WagonX(i))));
            if (car.mount) car.mount.transform.position = car.pos + Vector3.up * (0.45f + wagonSize.y + hop);
            // Touchdown: dust and a jolt.
            bool flying = hop > 0.9f;
            if (car.flying && !flying && Jumping)
            {
                Combat.Instance.Burst("grenade", car.pos, 2f);
                shake = 0.3f;
            }
            car.flying = flying;
        }
        float ahead = WagonX(-1) + 0.9f; // longer than a wagon
        Vector3 head = Path(ahead, out float turn);
        loco.SetLocalPositionAndRotation(head + Vector3.up * (0.1f + Hop(ahead)), Quaternion.Euler(0f, turn + 180f, -Pitch(ahead))); // the model's nose is its -x
    }

    // Over the gap (its middle is 3 past the ramp's foot) each wagon flies an arc 5 long either way.
    // Matching Tools/make_jump.py: the ravine is 9 long from gapS, the launch ramp climbs 1.06 over the 3.6
    // before it, the landing ramp drops 0.8 over the 3 after it.
    const float GapLength = 9f, RampLength = 3.6f, RampRise = 1.06f, LandingLength = 3f, LandingRise = 0.8f;

    // Height of a wagon at s: up the ramp, a long arc over the ravine, down the landing ramp.
    float Hop(float s)
    {
        if (!Jumping) return 0f;
        float x = s - gapS, off = (x - GapLength * 0.5f) / (GapLength * 0.5f + 1.5f);
        float arc = (1f - off * off) * jumpHeight;
        float up = x >= -RampLength && x <= 0f ? (x + RampLength) / RampLength * RampRise : 0f;
        float down = x >= GapLength && x <= GapLength + LandingLength ? (GapLength + LandingLength - x) / LandingLength * LandingRise : 0f;
        return Mathf.Max(arc, up, down);
    }

    // Nose up on the way up, down on the way down: the slope of the hop, in degrees.
    float Pitch(float s) => Jumping ? Mathf.Atan((Hop(s + 0.4f) - Hop(s - 0.4f)) / 0.8f) * Mathf.Rad2Deg * 0.8f : 0f;

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
        if (amount > 0f) DamageNumber(CarTop(wagon), amount, true); // the number pops over the wagon that took it
        if (car.Alive) return;
        SetAlive(car, false);
        if (Health <= 0f) Time.timeScale = 0f; // game over, OnGUI offers the restart
    }

    // A destroyed wagon turns black; its weapon stops until the next station, its cargo is lost.
    void SetAlive(Car car, bool alive)
    {
        MeshRenderer[] parts = car.body.GetComponentsInChildren<MeshRenderer>(); // not the fire's own renderer
        for (int i = 0; i < parts.Length; i++)
            parts[i].sharedMaterials = alive ? car.skins[i] : System.Array.ConvertAll(car.skins[i], _ => deadMat);
        if (car.mount) car.mount.enabled = alive;
        if (car.fire) Destroy(car.fire);
        GameObject flames = alive ? null : Resources.Load<GameObject>("Vfx/fire");
        if (flames)
        {
            car.fire = Instantiate(flames, car.body);
            car.fire.transform.localPosition = Vector3.up * 1.2f;
            car.fire.transform.localScale = Vector3.one * 1.5f;
        }
    }

    // An armed wagon wears its weapon's colour (plain grey with none): swaps the model's body paint,
    // the material the model calls WagonYellow, wherever it is.
    void Paint(Car car, Material paint)
    {
        if (car.painted == null)
        {
            var spots = new List<Vector2Int>();
            for (int i = 0; i < car.skins.Length; i++)
                for (int k = 0; k < car.skins[i].Length; k++)
                    if (car.skins[i][k].name == "WagonYellow") spots.Add(new Vector2Int(i, k));
            car.painted = spots.ToArray();
        }
        foreach (Vector2Int spot in car.painted) car.skins[spot.x][spot.y] = paint;
        SetAlive(car, car.Alive);
    }

    // Same weapon on the same wagon levels it up, anything else replaces it at level 1.
    void Mount(int wagon, Weapon weapon)
    {
        Car car = cars[wagon];
        if (car.mount && car.mount.Weapon == weapon)
        {
            car.mount.LevelUp();
            Combat.Instance.Burst("levelup", car.pos, 2f);
            return;
        }
        if (car.mount) Destroy(car.mount.gameObject);
        car.mount = Instantiate(turretPrefab, car.pos + Vector3.up * (0.45f + wagonSize.y), Quaternion.identity, transform);
        car.mount.Init(weapon);
        Paint(car, weapon.Material);
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
            PaintGround();
            PaintTrack();
            NewStretch();
            foreach (Prop prop in props) Dress(prop); // the whole landscape at once, behind the station menu
            Tint(sceneryMat, Biomes[Biome].scenery);
        }
        skipGift = 0;
        pending = null;
        leg = nextLeg;
        turned = false;
        nextEvent = EventBag[Random.Range(0, EventBag.Length)];
        remaining = LegLength;
        AtStation = false;
    }

    // Menu title, led by what the last skip paid (if the previous menu was skipped).
    string Noted(string title) =>
        skipGift == 1 ? L10n.T("menu.skip.money", skipMoney) + "   ·   " + title
        : skipGift == 2 ? L10n.T("menu.skip.health", skipHealth) + "   ·   " + title : title;

    void Restart()
    {
        backToMenu = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Back to the title screen, dropping the run: reload so a fresh start waits behind the menu.
    void GoToMainMenu()
    {
        backToMenu = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Leaves the title screen. What is behind it is always a fresh run, so this only starts the clock.
    void Play()
    {
        mainMenu = false;
        settingsMenu = false;
        Time.timeScale = 1f;
    }

    // Ends the run for good: closes the build, or stops play mode when testing in the editor.
    // Application.Quit does nothing in the editor, which made the game-over "No" look dead.
    void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Add(string icon, string label, System.Action run, bool enabled = true, Vector3? anchor = null, Vector2 extent = default, System.Action<int> adjust = null) =>
        options.Add(new Option { icon = icon, label = label, run = run, enabled = enabled, anchor = anchor, extent = extent, adjust = adjust });

    // A volume in the sound menu: left / right move it a tenth, confirming it (or a click) raises it and wraps round.
    void AddVolume(string icon, string key, float value, int which) =>
        options.Add(new Option { icon = icon, label = L10n.T(key, Mathf.RoundToInt(value * 100f)), enabled = true,
            run = () => Sound.Step(which, 1), adjust = steps => Sound.Step(which, steps) });

    // Rebuilt every frame the menu is open; the state decides which menu it is.
    void BuildMenu()
    {
        options.Clear();
        int kind;
        if (mainMenu && settingsMenu)
        {
            kind = 12;
            menuTitle = L10n.T("menu.settings");
            Add("monitor", L10n.T("settings.mode", L10n.T(Display.ModeKeys[Display.ModeIndex])), () => Display.StepMode(1), true, null, default, Display.StepMode);
            Add("monitor", L10n.T("settings.resolution", Display.Width, Display.Height), () => Display.StepResolution(1), true, null, default, Display.StepResolution);
            Add("speed", L10n.T("settings.fps", Display.FpsLabel), () => Display.StepFps(1), true, null, default, Display.StepFps);
            Add("restart", L10n.T("settings.vsync", L10n.T(Display.VSync ? "menu.yes" : "menu.no")), Display.ToggleVSync, true, null, default, _ => Display.ToggleVSync());
            Add("back", L10n.T("menu.back"), () => settingsMenu = false);
        }
        else if (mainMenu)
        {
            kind = 11;
            menuTitle = L10n.T("menu.title");
            Add("resume", L10n.T("menu.play"), Play);
            Add("settings", L10n.T("menu.settings"), () => settingsMenu = true);
            Add("language", L10n.T("language"), L10n.NextLocale);
            Add("quit", L10n.T("pause.quit"), Quit);
        }
        else if (Paused && confirming != 0)
        {
            // "No" comes first, so it is the one focused when the question opens.
            kind = 9;
            menuTitle = L10n.T(confirming == 1 ? "pause.restart.ask" : "pause.menu.ask");
            Add("no", L10n.T("menu.no"), () => confirming = 0);
            if (confirming == 1) Add("yes", L10n.T("menu.yes"), Restart);
            else Add("yes", L10n.T("menu.yes"), GoToMainMenu);
        }
        else if (Paused && soundMenu)
        {
            kind = 10;
            menuTitle = L10n.T("pause.sound");
            AddVolume("sound", "sound.master", Sound.Master, 0);
            AddVolume("music", "sound.music", Sound.Music, 1);
            AddVolume("sfx", "sound.sfx", Sound.Effects, 2);
            Add("mute", L10n.T("sound.mute", L10n.T(Sound.Muted ? "menu.yes" : "menu.no")), Sound.ToggleMute);
            Add("back", L10n.T("menu.back"), () => soundMenu = false);
        }
        else if (Paused)
        {
            kind = 8;
            menuTitle = L10n.T("menu.pause");
            Add("resume", L10n.T("pause.resume"), () => SetPaused(false));
            Add("sound", L10n.T("pause.sound"), () => soundMenu = true);
            Add("language", L10n.T("language"), L10n.NextLocale);
            Add("restart", L10n.T("pause.restart"), () => confirming = 1);
            Add("quit", L10n.T("pause.menu"), () => confirming = 2);
        }
        else if (Health <= 0f)
        {
            kind = 0;
            menuTitle = L10n.T("menu.gameover");
            Add("yes", L10n.T("menu.yes"), Restart);
            Add("no", L10n.T("menu.no"), GoToMainMenu);
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
                else Add(Player.ammo.id, L10n.T("hero.evolve", Player.ammo.Name, Player.level + 1), () => { Player.level++; Combat.Instance.Burst("levelup", Player.Position + Vector3.down * 2f, 1.5f); Next(); });
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
        // The pause and settings menus are vertical lists, so up/down work there too.
        Vector2 move = navigate.ReadValue<Vector2>();
        float x = ListMenu && Mathf.Abs(move.y) > Mathf.Abs(move.x) ? -move.y : move.x;
        int step = Mathf.Abs(x) > 0.5f ? (int)Mathf.Sign(x) : 0;
        bool tapped = stepRight.WasPressedThisFrame() || stepLeft.WasPressedThisFrame(); // d-pad and Q/E
        if (tapped) step = stepRight.WasPressedThisFrame() ? 1 : -1;
        if (ListMenu && options[focus].adjust != null)
        {
            // On a setting, left and right (and Q / E) change its value instead of moving the selection.
            int side = tapped ? step : Mathf.Abs(move.x) > 0.5f && Mathf.Abs(move.x) > Mathf.Abs(move.y) ? (int)Mathf.Sign(move.x) : 0;
            if (side != 0 && (tapped || !sideHeld)) options[focus].adjust(side);
            sideHeld = side != 0 && !tapped;
            if (tapped || side != 0) step = 0;
            tapped = false;
        }
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
        if (!Paused && swarm.Incoming) hud.Warning(L10n.T("swarm.title"), L10n.T("swarm.warn", JumpEvent.ButtonName(Player.UsingGamepad)));
        else if (Paused || Approach() < 0f) hud.Warning(null, null);
        else
        {
            string button = JumpEvent.ButtonName(Player.UsingGamepad);
            hud.Warning(L10n.T(eventKind == 1 ? "warn.curve.title" : eventKind == 4 ? "warn.zig.title" : "warn.jump.title"),
                eventKind == 1 ? L10n.T("warn.curve") : eventKind == 4 ? L10n.T("warn.zig", L10n.T(Player.UsingGamepad ? "zig.stick" : "zig.keys"))
                : eventKind == 2 ? L10n.T("warn.ramp", button) : ""); // the needle: just the warning, then it starts
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
    // model: a track piece to use instead of a box (it takes the biome's track materials).
    Strip Row(string name, float spacing, Vector3 offset, Vector3 size, Material mat, GameObject model = null)
    {
        var strip = new Strip { parent = new GameObject(name).transform, spacing = spacing };
        strip.parent.SetParent(transform, false);
        var bases = new List<Vector3>();
        for (float x = -viewRange; x <= viewRange + spacing; x += spacing)
        {
            bases.Add(offset + Vector3.right * x);
            if (model) Dress(Instantiate(model, strip.parent)).transform.localPosition = bases[bases.Count - 1];
            else Box(strip.parent, bases[bases.Count - 1], size, mat);
        }
        strip.bases = bases.ToArray();
        return strip;
    }

    // Track models name their materials TrackRail, TrackBallast and TrackSleeper: swapped here for the three
    // shared ones, so a biome change repaints every piece at once.
    GameObject Dress(GameObject piece)
    {
        Renderer skin = piece.GetComponentInChildren<Renderer>();
        skin.sharedMaterials = System.Array.ConvertAll(skin.sharedMaterials,
            m => m.name == "TrackRail" ? railMat : m.name == "TrackBallast" ? ballastMat : sleeperMat);
        skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // flat on the ground: nothing to cast
        return piece;
    }

    void PaintTrack()
    {
        var look = Tracks[Biome];
        Tint(sleeperMat, look.sleeper);
        Tint(ballastMat, look.bed);
        Tint(railMat, look.rail * look.glow);
        Strip style = sleeperStyles[look.style];
        style.offset = sleepers.offset;
        sleepers.parent.gameObject.SetActive(false);
        style.parent.gameObject.SetActive(true);
        sleepers = style;
    }

    // Another kind of stretch, made of up to three kinds of prop of each size picked from the biome's.
    void NewStretch()
    {
        Vector3 before = stretch;
        do stretch = Stretches[Random.Range(0, Stretches.Length)]; while (stretch == before);
        stretchLeft = Random.Range(stretchLength.x, stretchLength.y);
        for (int size = 0; size < 3; size++)
        {
            var pool = new List<GameObject>(sceneryKinds[Biome][size]);
            while (pool.Count > 3) pool.RemoveAt(Random.Range(0, pool.Count));
            stretchKinds[size] = pool.ToArray();
        }
    }

    // Gives a scenery slot a new look: another prop of its size from this biome, another distance from the
    // track, another side, turn and scale. The copy it had goes back to the spares.
    void Dress(Prop prop)
    {
        if (prop.body)
        {
            prop.body.gameObject.SetActive(false);
            spareProps[prop.kind].Push(prop.body);
            prop.body = null;
        }
        GameObject[] kinds = stretchKinds[prop.size] ?? sceneryKinds[Biome][prop.size];
        if (kinds.Length == 0 || Random.value > stretch[prop.size]) return; // this stretch leaves the slot empty
        prop.kind = kinds[Random.Range(0, kinds.Length)];
        if (!spareProps.TryGetValue(prop.kind, out Stack<Transform> spare)) spareProps[prop.kind] = spare = new Stack<Transform>();
        prop.body = spare.Count > 0 ? spare.Pop() : Instantiate(prop.kind, sceneryRoot).transform;
        prop.body.gameObject.SetActive(true);
        prop.body.localScale = Vector3.one * Random.Range(0.8f, 1.25f);
        prop.yaw = Random.Range(0f, 360f);
        float away = prop.size == 0 ? Random.Range(4.5f, 14f) : prop.size == 1 ? Random.Range(9f, 20f) : Random.Range(19f, 30f);
        prop.side = Random.value < 0.5f ? away : -away;
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

    // The biome's ground painting from Resources/Ground (Tools/make_ground.py); its plain colour if there is none.
    void PaintGround()
    {
        Texture2D painting = Resources.Load<Texture2D>("Ground/" + Biomes[Biome].id);
        groundMat.mainTexture = painting;
        Tint(groundMat, painting ? Color.white : Biomes[Biome].ground);
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
                twin = Mat(mats[i].name.StartsWith("Glow") ? mats[i].color * 2.5f : mats[i].color); // Glow...: bright enough to bloom
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

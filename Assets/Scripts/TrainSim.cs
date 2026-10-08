using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// The train never moves: it sits at the origin and the track scrolls under it.
// Enemies and bullets live in a fixed frame, and no track is ever built beyond the view.
public class TrainSim : MonoBehaviour
{
    [Header("Train")]
    public int wagons = 4;             // armed wagons; the first one is the locomotive
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
    public int biomeDifficulty = 2;                     // a biome change is worth this many stations of difficulty
    public int maxCargo = 3;                            // cargo wagons loaded at a station
    public int maxCars = 10;                            // longest the train gets with goods won on the way

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
        public Vector3 pos;         // on the ground, under its middle
        public float heading;       // degrees around Y; not 0 only while it is past a curve's knee
        public float health, maxHealth;
        public Turret mount;
        public int cargo = -1;      // index into Cargos, -1 for an armed wagon
        public int reward, penalty; // fixed when the cargo is loaded
        public bool Alive => health > 0f;
    }

    // Goods: (text key, colour, wagon health, pay on delivery, fine if destroyed).
    // Wood is the safe one, rock is tough, weapons pay best and break easily.
    static readonly (string id, Color color, float health, int reward, int penalty)[] Cargos =
    {
        ("wood", new Color(0.55f, 0.35f, 0.15f), 40f, 40, 20),
        ("rock", new Color(0.5f, 0.5f, 0.5f), 80f, 60, 40),
        ("arms", new Color(0.2f, 0.35f, 0.2f), 25f, 120, 80),
    };

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
    float MaxHealth => wagonMaxHealth * wagons;
    public bool CanTakeLoot => cars.Count < maxCars;
    public bool Damaged => Health < MaxHealth || Player.Health < Player.maxHealth;
    public bool InEvent => turn.Active;    // the curve's button sequence is on: the hero waits
    public bool Bending => bendAngle != 0f; // a curve is somewhere on screen
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
    Transform station, railsBack, railsFront;
    float remaining, stationX;

    // The curve. The track is straight up to `knee` (distance ahead of the train's middle) and turned by
    // `bendAngle` beyond it. The knee scrolls back through the train; once it is out of sight behind, the
    // whole world is rotated so the train lies along X again and the camera keeps the new view.
    float knee, bendAngle;
    float camYaw = 45f;                  // view of the track: 45 up-right, 0 across, -45 down-right, 90 up, -90 down
    static readonly float[] Views = { -90f, -45f, 0f, 45f, 90f };
    bool sequenceStarted;
    readonly List<Car> cars = new();     // front to back
    Material wagonMat, deadMat, groundMat, sceneryMat;
    Material[] cargoMats;
    readonly int[] offers = new int[3];  // index into Weapon.Wagon, or its Length for money
    readonly int[] heroOffers = new int[3]; // index into Weapon.Hero, then speed, armor, vitality
    readonly List<int> offerPool = new();
    Weapon pending;                      // weapon chosen at the station, waiting for a wagon
    int pendingCargo = -1;               // cargo chosen at the station, waiting for a position
    int cargoEarned, cargoFined;         // what the last delivery paid and cost
    int repairsBought;
    int RepairPrice => repairCost * (repairsBought + 1);
    int stage;                           // station menus in order: 0 wagon upgrade, 1 hero upgrade, 2 cargo, 3 route
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
    float LegLength => stationGap * Legs[leg].length;
    static string LegName(int leg) => L10n.T("leg." + Legs[leg].id);

    // The open menu as a flat list, so mouse, keyboard and gamepad all drive the same options.
    struct Option
    {
        public string label;
        public System.Action run;
        public bool enabled;
        public Vector3? anchor; // world spot this option is about; framed in yellow while focused
        public Vector2 extent;  // half size (x, z) of that frame
    }
    readonly Combat.Batch highlight = new Combat.Batch(new Color(1f, 0.82f, 0.1f), false);
    readonly List<Option> options = new();
    string menuTitle;
    int menuKind = -1, focus;
    bool navHeld;
    InputAction navigate, confirm, stepRight, stepLeft;
    bool MenuOpen => AtStation || Health <= 0f;
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
        groundMat = Mat(Biomes[0].ground);
        sceneryMat = Mat(Biomes[0].scenery);
        wagonMat = Mat(new Color(0.6f, 0.15f, 0.15f));
        deadMat = Mat(Color.black);
        cargoMats = new Material[Cargos.Length];
        for (int i = 0; i < Cargos.Length; i++) cargoMats[i] = Mat(Cargos[i].color);

        float length = viewRange * 4f;
        Box(transform, new Vector3(0f, -0.5f, 0f), new Vector3(length * 2f, 1f, length * 2f), groundMat, true);
        // Rails are uniform, so they never scroll: one pair up to the knee, one pair beyond it.
        railsBack = new GameObject("RailsBack").transform;
        railsFront = new GameObject("RailsFront").transform;
        foreach (Transform rails in new[] { railsBack, railsFront })
        {
            rails.SetParent(transform, false);
            float middle = rails == railsBack ? -length * 0.5f : length * 0.5f;
            Box(rails, new Vector3(middle, 0.15f, 0.8f), new Vector3(length, 0.1f, 0.15f), steel);
            Box(rails, new Vector3(middle, 0.15f, -0.8f), new Vector3(length, 0.1f, 0.15f), steel);
        }
        knee = viewRange;

        turn = gameObject.AddComponent<TurnEvent>();
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

    // Ground position of the track `s` units ahead of the train's middle (negative = behind), and its heading there.
    Vector3 Path(float s, out float heading)
    {
        heading = s <= knee ? 0f : bendAngle;
        if (bendAngle == 0f) return new Vector3(s, 0f, 0f);
        Quaternion turn = Quaternion.Euler(0f, bendAngle, 0f);
        Vector3 Raw(float d) => d <= knee ? new Vector3(d, 0f, 0f) : new Vector3(knee, 0f, 0f) + turn * new Vector3(d - knee, 0f, 0f);
        return Raw(s) - Raw(0f);
    }

    // Puts rails, sleepers, poles, station and (in a curve) the wagons on the track's current shape.
    void PlaceTrack()
    {
        railsBack.localPosition = railsFront.localPosition = Path(knee, out _);
        railsFront.localRotation = Quaternion.Euler(0f, bendAngle, 0f);
        Place(sleepers);
        Place(poles);
        station.localPosition = Path(stationX, out float stationHeading);
        station.localRotation = Quaternion.Euler(0f, stationHeading, 0f);
        if (Bending) Layout();
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

    // A curve appears ahead, towards one of the other views (never more than a quarter turn away).
    void StartBend()
    {
        turned = true;
        float target;
        do target = Views[Random.Range(0, Views.Length)];
        while (target == camYaw || Mathf.Abs(target - camYaw) > 90f);
        bendAngle = camYaw - target;
        knee = viewRange;
        sequenceStarted = false;
    }

    // The old stretch is out of sight: turn the whole world so the train lies along X again.
    // The camera turns with it, so nothing moves on screen and the new view simply stays.
    void EndBend()
    {
        Quaternion back = Quaternion.Euler(0f, -bendAngle, 0f);
        Combat.Instance.Rotate(back);
        Player.Rotate(back);
        camYaw -= bendAngle;
        bendAngle = 0f;
        knee = viewRange;
        AimCamera();
        Layout();
        PlaceTrack();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) L10n.NextLocale();

        // A long train pulls the camera back so it always fits, whichever way the track runs on screen.
        Camera view = Camera.main;
        float wanted = Mathf.Max(cameraSize, (HalfExtents.x + 5f) / view.aspect);
        view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, wanted, 6f * Time.unscaledDeltaTime);

        if (MenuOpen)
        {
            BuildMenu();
            Navigate();
        }
        if (AtStation) return;

        // Brake curve: fastest speed that can still stop exactly on the station.
        float target = Mathf.Clamp(Mathf.Sqrt(2f * acceleration * remaining), 0.5f, maxSpeed * Legs[leg].speed);
        Speed = Mathf.MoveTowards(Speed, target, acceleration * Time.deltaTime);
        float step = Mathf.Min(Speed * Time.deltaTime, remaining);
        remaining -= step;

        sleepers.offset = Mathf.Repeat(sleepers.offset + step, sleepers.spacing);
        poles.offset = Mathf.Repeat(poles.offset + step, poles.spacing);

        stationX -= step;
        if (stationX < -viewRange) stationX = remaining; // reuse the one station for the next stop

        // One curve per trip, early enough that it is fully behind before the station.
        if (!turned && remaining <= Mathf.Max(LegLength * (1f - turnAt), viewRange * 2f + 20f)) StartBend();
        if (Bending)
        {
            knee -= step;
            float trainLength = HalfExtents.x * 2f;
            if (!sequenceStarted && knee <= trainLength * 0.5f)
            {
                // The button sequence lasts exactly as long as the train takes to go through the knee.
                sequenceStarted = true;
                turn.Begin(trainLength / Mathf.Max(Speed, 1f));
            }
            if (knee < -viewRange) EndBend();
        }
        PlaceTrack();

        if (remaining <= 0f) Arrive();
    }

    void Arrive()
    {
        if (Bending) EndBend();
        Speed = 0f;
        AtStation = true;
        Stations++;

        // Cargo is uncoupled here: paid if it made it, fined if it burned. Armed wagons get patched up.
        cargoEarned = cargoFined = 0;
        for (int i = cars.Count - 1; i >= 0; i--)
        {
            Car car = cars[i];
            if (car.cargo < 0)
            {
                if (car.Alive) continue;
                car.health = car.maxHealth * stationRepair;
                SetAlive(car, true);
                continue;
            }
            if (car.Alive) cargoEarned += car.reward; else cargoFined += car.penalty;
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
    void AddCar(int index, int cargo)
    {
        var car = new Car { cargo = cargo, health = wagonMaxHealth, maxHealth = wagonMaxHealth };
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
        car.body = Box(transform, Vector3.zero, size, mat);
        cars.Insert(index, car);
        Layout();
    }

    // Goods the hero shot down on the way: coupled anywhere behind the locomotive.
    public void AddLoot(int cargo)
    {
        if (CanTakeLoot) AddCar(Random.Range(1, cars.Count + 1), cargo);
    }

    void Layout()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            Car car = cars[i];
            car.pos = Path(WagonX(i), out car.heading);
            car.body.SetLocalPositionAndRotation(car.pos + Vector3.up * (0.2f + car.body.localScale.y * 0.5f), Quaternion.Euler(0f, car.heading, 0f));
            if (car.mount) car.mount.transform.position = car.pos + Vector3.up * (0.45f + wagonSize.y);
        }
    }

    int CargoCount { get { int n = 0; foreach (Car c in cars) if (c.cargo >= 0) n++; return n; } }

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
        car.body.GetComponent<Renderer>().sharedMaterial = !alive ? deadMat : car.cargo < 0 ? wagonMat : cargoMats[car.cargo];
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

    // Three different options each: wagon weapons unlocked in this biome plus money,
    // and hero weapons that can still be taken or levelled plus the three stat upgrades.
    void RollOffers()
    {
        offerPool.Clear();
        for (int i = 0; i < Weapon.Wagon.Length; i++)
            if (Weapon.Wagon[i].firstBiome <= Biome) offerPool.Add(i);
        offerPool.Add(Weapon.Wagon.Length);
        Draw(offers);

        offerPool.Clear();
        for (int i = 0; i < Weapon.Hero.Length; i++)
            if (Weapon.Hero[i].firstBiome <= Biome && Player.CanTake(Weapon.Hero[i])) offerPool.Add(i);
        offerPool.Add(Weapon.Hero.Length);                                        // speed
        if (Player.armor < Player.MaxArmor) offerPool.Add(Weapon.Hero.Length + 1); // armor
        offerPool.Add(Weapon.Hero.Length + 2);                                    // vitality
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
        foreach (Car car in cars) car.health = Mathf.Min(car.maxHealth, car.health + amount);
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
            groundMat.color = Biomes[Biome].ground;
            sceneryMat.color = Biomes[Biome].scenery;
        }
        skipGift = 0;
        pending = null;
        leg = nextLeg;
        turned = false;
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

    void Add(string label, System.Action run, bool enabled = true, Vector3? anchor = null, Vector2 extent = default) =>
        options.Add(new Option { label = label, run = run, enabled = enabled, anchor = anchor, extent = extent });

    // Rebuilt every frame the menu is open; the state decides which menu it is.
    void BuildMenu()
    {
        options.Clear();
        int kind;
        if (Health <= 0f)
        {
            kind = 0;
            menuTitle = L10n.T("menu.gameover");
            Add(L10n.T("menu.yes"), Restart);
            Add(L10n.T("menu.no"), Application.Quit);
        }
        else if (stage >= 3)
        {
            kind = 1;
            menuTitle = L10n.T("menu.route", LegName(nextLeg));
            Add(L10n.T("menu.repair", repairReward, RepairPrice), () => { money -= RepairPrice; repairsBought++; Repair(); },
                money >= RepairPrice && Health < MaxHealth);
            Add(L10n.T("menu.heal", healReward, healCost), () => { money -= healCost; Player.Heal(healReward); },
                money >= healCost && Player.Health < Player.maxHealth);
            Add(L10n.T("menu.stay", BiomeName(Biome)), () => Depart());
            if (Biome < Biomes.Length - 1)
                Add(L10n.T("menu.change", BiomeName(Biome + 1), biomeDifficulty, Biome + 2), () => Depart(true));
        }
        else if (stage == 2 && pendingCargo < 0)
        {
            // Cargo: up to maxCargo wagons, each paid at the next station if it survives.
            kind = 5;
            int loaded = CargoCount;
            menuTitle = Noted(L10n.T("menu.cargo", loaded, maxCargo));
            for (int i = 0; i < Cargos.Length; i++)
            {
                int cargo = i;
                int reward = Mathf.RoundToInt(Cargos[i].reward * Legs[nextLeg].length) * (Biome + 1);
                Add(L10n.T("cargo.card", L10n.T("cargo." + Cargos[i].id), reward, Cargos[i].health, Cargos[i].penalty * (Biome + 1)),
                    () => pendingCargo = cargo, loaded < maxCargo);
            }
            Add(L10n.T("cargo.done"), Next);
        }
        else if (stage == 2)
        {
            // Where to couple it: behind any wagon, never ahead of the locomotive.
            kind = 6;
            menuTitle = L10n.T("cargo.place", L10n.T("cargo." + Cargos[pendingCargo].id));
            for (int i = cars.Count; i >= 1; i--)
            {
                int index = i;
                Add(L10n.T("cargo.slot", i), () =>
                    {
                        AddCar(index, pendingCargo);
                        pendingCargo = -1;
                        if (CargoCount >= maxCargo) Next();
                    },
                    true, new Vector3(WagonX(i - 1) - Step * 0.5f, 1f, 0f), new Vector2(0.3f, wagonSize.z * 0.5f + 0.15f));
            }
            Add(L10n.T("menu.back"), () => pendingCargo = -1);
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
                    Add(weapon.Name + "\n" + weapon.Info + "\n" + Player.TakeLabel(weapon), () => { Player.Take(weapon); Next(); });
                }
                else if (stat == 0) Add(L10n.T("hero.speed"), () => { Player.SpeedUp(); Next(); });
                else if (stat == 1) Add(L10n.T("hero.armor"), () => { Player.ArmorUp(); Next(); });
                else Add(L10n.T("hero.vitality", vitalityReward), () => { Player.VitalityUp(vitalityReward); Next(); });
            }
            Add(L10n.T("menu.skip"), Skip);
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
                    Add(weapon.Name + "\n" + weapon.Info, () => pending = weapon);
                }
                else Add(L10n.T("menu.money", moneyReward), () => { money += moneyReward; Next(); });
            }
            Add(L10n.T("menu.skip"), Skip);
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
                Add(L10n.T("menu.wagon", i + 1, current), () => { Mount(wagon, pending); Next(); },
                    !same || mount.Level < Weapon.MaxLevel, new Vector3(WagonX(i), 0.4f + wagonSize.y, 0f),
                    new Vector2(wagonSize.x * 0.5f + 0.15f, wagonSize.z * 0.5f + 0.15f));
            }
            Add(L10n.T("menu.back"), () => pending = null);
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
        float x = navigate.ReadValue<Vector2>().x;
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

    void OnGUI()
    {
        Ui.Begin();
        float health = Health;

        // Top-left card: where we are, train health, money, and how far the next station is.
        Ui.Panel(Ui.R(8f, 8f, 300f, 76f));
        GUI.Label(Ui.R(16f, 56f, 284f, 14f), L10n.T("hud.leg", LegName(leg)), Ui.Small);
        Ui.Progress(Ui.R(16f, 72f, 284f, 6f), AtStation ? 1f : 1f - remaining / LegLength);
        GUI.Label(Ui.R(16f, 10f, 290f, 20f), L10n.T("hud.status", Stations, BiomeName(Biome), Difficulty), Ui.Text);
        Ui.Bar(Ui.R(16f, 34f, 170f, 16f), health / MaxHealth);
        GUI.Label(Ui.R(16f, 33f, 170f, 18f), L10n.T("hud.train", Mathf.Round(health), Mathf.Round(MaxHealth)), Ui.Small);
        GUI.Label(Ui.R(192f, 32f, 108f, 20f), $"$ {money}", Ui.Money);
        if (GUI.Button(Ui.R(8f, 88f, 170f, 20f), L10n.T("language"), Ui.Normal)) L10n.NextLocale();
        for (int i = 0; i < cars.Count; i++)
        {
            Vector2 top = Ui.Point(cars[i].pos + Vector3.up * (1.5f + wagonSize.y));
            Ui.Bar(Ui.R(top.x - 18f, top.y - 6f, 36f, 4f), cars[i].health / cars[i].maxHealth);
        }
        if (!MenuOpen || options.Count == 0) return;

        // One wide card along the bottom, clear of the train: title, big option buttons, how to operate it.
        float w = Mathf.Min(Ui.Width - 40f, 860f), x = (Ui.Width - w) * 0.5f, y = 540f - 158f;
        Ui.Panel(Ui.R(x, y, w, 150f));
        GUI.Label(Ui.R(x + 10f, y + 6f, w - 20f, 30f), menuTitle, Ui.Title);
        const float gap = 8f;
        float buttonWidth = (w - 20f - gap * (options.Count - 1)) / options.Count;
        int clicked = -1;
        for (int i = 0; i < options.Count; i++)
            if (Ui.Button(Ui.R(x + 10f + i * (buttonWidth + gap), y + 44f, buttonWidth, 72f), options[i].label, i == focus, options[i].enabled))
                clicked = i;
        string how = L10n.T(Player.UsingGamepad ? "menu.help.gamepad" : "menu.help.keyboard");
        GUI.Label(Ui.R(x + 10f, y + 124f, w - 20f, 20f), how, Ui.Small);

        // While placing a weapon or a cargo wagon, every wagon shows its number so the cards can be matched to the train.
        for (int i = 0; (menuKind == 3 || menuKind == 6) && i < cars.Count; i++)
        {
            Vector2 tag = Ui.Point(new Vector3(WagonX(i), 1f + wagonSize.y, 0f));
            Rect rect = Ui.R(tag.x - 14f, tag.y - 34f, 28f, 26f);
            Ui.Panel(rect);
            GUI.Label(rect, (i + 1).ToString(), Ui.Title);
        }
        if (clicked >= 0) options[clicked].run();
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

    public static Material Mat(Color color) => new Material(Shader.Find("HDRP/Lit")) { color = color, enableInstancing = true };

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

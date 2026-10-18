using System.Collections.Generic;
using System.Collections;
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
    // The numbers of the balance are not kept by the scene: what is written here is what the game plays with.
    [System.NonSerialized] public float wagonMaxHealth = 40f; // train health is the sum of its armed wagons
    public Turret turretPrefab;

    [Header("Upgrades")]
    public float fireRate = 1f; // shared by the hero and wagon weapons
    [Range(0f, 1f)] public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;
    public int money;
    [System.NonSerialized] public int moneyReward = 35;
    public float repairReward = 25f;                    // health given back to every wagon
    [System.NonSerialized] public int repairCost = 90;                         // first paid repair; each one bought raises the price by this much
    [Range(0f, 1f)] [System.NonSerialized] public float stationRepair = 0.15f; // share of health a destroyed wagon gets back at a station
    public float healReward = 10f;                      // health the hero gets per purchase
    [System.NonSerialized] public int healCost = 50;
    [System.NonSerialized] public int skipMoney = 20;                          // skipping the upgrade gives this...
    public float skipHealth = 10f;                      // ...or this much health to every wagon and the hero
    public float vitalityReward = 10f;                  // max health per hero vitality upgrade
    public int biomeDifficulty = 3;                     // a biome change is worth this many stations of difficulty
    [Range(0f, 1f)] [System.NonSerialized] public float cargoCost = 0.5f;      // share of its pay a cargo wagon costs to load
    [Range(0f, 1f)] [System.NonSerialized] public float bonusOfferChance = 0.18f; // a wagon offer turns into a free cargo wagon or an escort
    public int maxCargo = 3;                            // cargo wagons loaded at a station
    public int maxCars = 12;                            // longest the train ever gets

    [Header("Shop")]
    [System.NonSerialized] public int kitCost = 80;
    public float kitHeal = 20f;                         // a kit repairs every standing wagon and the hero, on the road
    public const bool KitsInUse = false;                // medkits are put away for now (not sold, not shown, the button is the ultimate's)
    public int maxKits = 3;
    [System.NonSerialized] public int wagonCost = 260, wagonCostStep = 140;       // first armed wagon, and how much dearer each next one is
    [System.NonSerialized] public int containerCost = 190, containerCostStep = 95; // same for the permanent cargo wagons

    [Header("Track")]
    [System.NonSerialized] public float exposure = 13.7f;                      // fixed camera exposure (EV100), the same in every biome
    public float groundTile = 18f;                      // size on the ground of one repeat of the ground texture
    public float viewRange = 80f;
    public float sleeperSpacing = 1.5f;
    [System.NonSerialized] public float stationGap = 600f;     // the shortest a normal leg can be: about half a minute of riding
    [System.NonSerialized] public float stationGapMax = 1200f; // the longest: each leg is somewhere in between, at random
    float legScale = 1f;                                       // this leg's share of that, 1 to stationGapMax / stationGap

    [Header("Camera")]
    public float cameraSize = 14f;
    [System.NonSerialized] public float cameraZoom = 1f; // extra pull-back, set by a boss's intro and released after

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
        public int guise = -1;      // an eternal container on the road: the goods it is carrying this trip
        public bool Alive => health > 0f;
    }

    // Goods: (text key, colour, wagon health, pay on delivery, fine if destroyed).
    // Wood is the safe one, rock is tough, weapons pay best and break easily.
    static readonly (string id, Color color, float health, int reward, int penalty)[] Cargos =
    {
        ("wood", new Color(0.55f, 0.35f, 0.15f), 40f * Balance.Scale, 15, 15),
        ("rock", new Color(0.5f, 0.5f, 0.5f), 80f * Balance.Scale, 20, 25),
        ("arms", new Color(0.2f, 0.35f, 0.2f), 25f * Balance.Scale, 45, 45),
        ("fuel", new Color(0.85f, 0.12f, 0.1f), 0.1f * Balance.Scale, 110, 30), // goes up at the first scratch, pays a fortune
        ("container", new Color(0.15f, 0.3f, 0.6f), 60f * Balance.Scale, 10, 0), // the bought, permanent one: never loaded at a station
    };
    const int Loadable = 4, Fuel = 3, Container = 4;
    public float fuelBlast = 12f, fuelBlastRadius = 7f; // what a fuel wagon does to its neighbours when it blows (x Balance.Scale)

    public float Speed { get; private set; }
    // How far the world slid backwards past the train this frame: the distance the ground, rails and
    // everything on them scroll by. Debris (Combat's stains) needs it to stay glued to the ground.
    public float Scroll { get; private set; }
    // How fast the world looks to go by: the ground, sleepers, scenery and whatever lies on the ground slide this
    // many times faster than the train really runs, while distances and times (legs, stations, events) stay true.
    // Curves, ramps and the junction slide with the ground, so all of it keeps together; the station cannot (the
    // train has to stop on it), so while it is in sight the rush eases off to the true speed, like a train braking.
    [System.NonSerialized] public float rush = 2.4f;
    float rushNow = 1f;
    public float GroundScroll => Scroll * rushNow;
    public float RunSpeed => Speed * rushNow; // how fast the world goes by, for the button games' timings
    public bool AtStation { get; private set; }
    public int Stations { get; private set; } // stations reached so far, drives the difficulty
    public Player Player { get; private set; }
    public int Biome { get; private set; }
    // A story chapter grows harder with its clock (and each chapter starts harder than the one before) instead of
    // with stations and biome changes.
    // In a story it climbs gently for the first minutes, while the weapons are few, and ever faster after: 2 at 4
    // minutes, 10 at 10, 29 at the end of the chapter.
    public int Difficulty => Story.Active ? Story.Chapter * 3 + (int)(storyClock / 90f + 4f * Mathf.Pow(storyClock / 600f, 2f)) : Stations + Biome * biomeDifficulty;
    float storyClock;           // seconds ridden in this story chapter (not counting stations or pauses)
    bool duel, duelCalled, chapterWon; // the rival is out / was called for / is beaten and the upgrade to keep is being chosen
    Talk talk;                  // the words with the rival, before the duel and after it
    RivalEvent rival;           // the rival's locomotive, on a track of its own
    readonly List<string> taken = new(); // every upgrade taken in this chapter, as Story tokens

    // The hero's upgrades (Story tokens), each up to MaxPassive levels, like Vampire Survivors' passive items. One
    // of them, owned with a weapon at its top level, evolves that weapon (Weapon.evolvesWith).
    // regen: the train and the hero mend slowly; area: blasts and sweeps wider; amount: more shots; luck: more prizes
    // and golden chests; duration: slows, boomerangs and cuts last longer.
    public static readonly string[] Passives = { "speed", "armor", "vitality", "damage", "greed", "fireRate", "critChance", "critDamage", "regen", "area", "amount", "luck", "duration" };
    public static readonly string[] PassiveIcons = { "speed", "armor", "vitality", "arms", "money", "minigun", "sniper", "rail", "regen", "area", "amount", "luck", "duration" };
    public float regenPerLevel = 0.15f * Balance.Scale; // health a second, per level of regeneration, to every wagon (half to the hero)
    public const int MaxPassive = 5;
    public const int MaxOwnedPassives = 5;      // different upgrades the hero can hold at once
    const int HeroMoney = 20, HeroHealth = 21;  // the hero's cards when nothing else is left: money, health
    readonly Dictionary<string, int> passiveLevels = new();
    public readonly List<string> passiveOrder = new(); // in the order they were first taken, for the HUD
    public int PassiveLevel(string id) => id != null && passiveLevels.TryGetValue(id, out int n) ? n : 0;
    // The hero's weapon is at its top level and its partner upgrade is owned: a boss's chest evolves it.
    bool CanEvolve => Player.level == Weapon.MaxLevel && PassiveLevel(Player.ammo.evolvesWith) > 0;
    // The first wagon whose weapon is at its top level with its partner upgrade owned, or null.
    Turret Evolvable => cars.Find(c => c.mount && c.mount.Level == Weapon.MaxLevel && PassiveLevel(c.mount.Weapon.evolvesWith) > 0)?.mount;
    bool evolutionsMenu; // the pause menu's list of every evolution

    // Story: a Y junction at JunctionAt seconds of the chapter. One way leads to the mid boss (the truck, BossEvent),
    // the other goes on without it. Beaten, the truck leaves its wagon (GiveTruck).
    const float JunctionAt = 15f * 60f;
    bool junction, junctionAsked;
    Car truckCar;      // the truck's wagon on this train, if any
    bool truckCarried; // it came from the chapter before: it goes no further
    public float junctionHard = 14f;   // the junction's truck: a real fight, with this many times the health
    Transform junctionLook, lever; // the branch line leaving the track, and the lever at the switch
    float junctionS;               // where along the track the switch is (as the knees: it slides back as the train runs)
    float leverTo;                 // where the lever is thrown to (degrees)
    public bool EvolutionsOpen => Paused && evolutionsMenu;
    string keptPassive; // the upgrade handed on to the next chapter, with its level: drawn when the chapter is won
    void Took(string token) { if (Story.Active) taken.Add(token); }
    // What the HUD shows of a chapter's clock: the time still to ride, then the duel.
    public string StoryTime => duel ? L10n.T("story.duel") + rival.StatusLine() : $"{(int)(Left / 60f):0}:{(int)(Left % 60f):00}";
    float Left => Mathf.Max(0f, Story.Seconds - storyClock);
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

    // The ultimate: charged by enemies shot down (not by the station's guns), fired with the kit's old button.
    [System.NonSerialized] public float ultimateKills = 450f; // a strong attack: it takes a good while to earn again
    float ultimateCharge;
    UltimateCutIn ultimateCutIn;
    public bool UltimatePresenting => ultimateCutIn && ultimateCutIn.Active;
    public float UltimateFill => Mathf.Clamp01(ultimateCharge / ultimateKills);
    public bool UltimateReady => ultimateCharge >= ultimateKills;
    public int Kills { get; private set; } // enemies shot down this run (the station's guns do not count)
    public void Charge()
    {
        Kills++;
        if (!UltimateReady && ++ultimateCharge >= ultimateKills) { Sound.Play("newcard"); ultimateAnnounce = true; }
    }
    bool ultimateAnnounce; // "ultimate ready" is still to be said, as soon as nothing else is on screen
    // A button game, a warning, a swarm coming or a talk is on: the ultimate (and its announcement) waits.
    public bool Alerting => InEvent || Warning || swarm.Incoming || brawl.Incoming || talk.Open;
    public string KitButton => !Player.UsingGamepad ? "R" : Gamepad.current is DualShockGamepad ? "△" : "Y";
    public float CarFill(int i) => cars[i].health / cars[i].maxHealth;
    public Vector3 CarTop(int i) => cars[i].pos + Vector3.up * (1.5f + wagonSize.y);
    public bool MenuShown => menuView.Visible || (deliveryReport && deliveryReport.Visible);
    public bool ShowCarNumbers => MenuOpen && !Paused && menuKind == 3;
    public EventPanel Panel { get; private set; } // the card the button games are played on
    public void Toast(string text) => hud.Toast(text);
    public void Flash(string effect) => hud.Flash(effect);
    public void DamageNumber(Vector3 at, float amount, bool onTrain, bool critical = false) => hud.DamageNumber(at, amount, onTrain, critical);
    public float CriticalDamage(float damage, out bool critical)
    {
        critical = damage > 0f && Random.value < criticalChance;
        return critical ? damage * criticalMultiplier : damage;
    }
    Hud hud;
    public float MaxHealth { get { float sum = 0f; foreach (Car c in cars) if (c.cargo < 0) sum += c.maxHealth; return sum; } }
    public bool CanTakeLoot => cars.Count < maxCars;
    public bool Damaged => Health < MaxHealth || Player.Health < Player.maxHealth;
    public bool InEvent => turn.Active || jump.Active || zig.Active || swarm.Active || brawl.Active; // a button game is on: the hero waits
    public bool BossFight => (boss != null && boss.Active) || (rival != null && rival.Active); // a boss or the rival is on the field: no other enemies join in
    public void BossDefeated()
    {
        bossBeaten = Biome;
        GiveTruck(1, false);
        OpenChest();
    }

    // The truck's wagon, at the tail: its rockets, a look of its own (Turret), and it levels up like any other.
    void GiveTruck(int level, bool carried)
    {
        AddCar(cars.Count, -1);
        int at = cars.Count - 1;
        for (int l = 0; l < level; l++) Mount(at, Weapon.Truck);
        truckCar = cars[at];
        truckCarried = carried;
        if (!carried) hud.Toast(L10n.T("truck.won"), 4f);
    }

    // The branch line, ahead on the track: rails and sleepers leaving it at an angle, one side or the other, and a
    // switch lever on the far side. It slides back with the ground (Update) and the choice opens at the switch.
    void LayJunction()
    {
        junctionLook = new GameObject("Junction").transform;
        junctionS = NoseS + 70f;
        float side = Random.value < 0.5f ? 1f : -1f;
        Transform branch = new GameObject("Branch").transform;
        branch.SetParent(junctionLook, false);
        branch.localRotation = Quaternion.Euler(0f, -side * BranchAngle, 0f); // +x on: it bends off towards +z * side
        this.branch = branch;
        Dress(Instantiate(Resources.Load<GameObject>("Models/Track_rail"), branch)).transform.localScale = new Vector3(110f, 1f, 1f);
        GameObject sleeper = Resources.Load<GameObject>("Models/Sleeper_0");
        for (float x = 1.5f; x < 110f; x += 1.5f)
            Dress(Instantiate(sleeper, branch)).transform.localPosition = Vector3.right * x;
        Transform Part(PrimitiveType shape, Transform parent, Vector3 at, Vector3 size, Color colour)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = Mat(colour);
            return part.transform;
        }
        Transform stand = new GameObject("Lever").transform;
        stand.SetParent(junctionLook, false);
        stand.localPosition = new Vector3(-1.5f, 0f, -side * 4f);
        stand.localScale = Vector3.one * 1.7f;
        Part(PrimitiveType.Cube, stand, new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.7f, 0.9f), new Color(0.25f, 0.26f, 0.3f));
        Part(PrimitiveType.Cube, stand, new Vector3(0f, 0.72f, 0f), new Vector3(1.5f, 0.06f, 1f), new Color(0.95f, 0.75f, 0.1f));
        lever = new GameObject("Arm").transform;
        lever.SetParent(stand, false);
        lever.localPosition = Vector3.up * 0.7f;
        Part(PrimitiveType.Cube, lever, new Vector3(0f, 0.9f, 0f), new Vector3(0.16f, 1.8f, 0.16f), new Color(0.7f, 0.72f, 0.78f));
        Part(PrimitiveType.Sphere, lever, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.5f, new Color(0.95f, 0.15f, 0.12f));
        lever.localRotation = Quaternion.identity; // upright until thrown
        leverSide = side;
        PlaceJunction();
    }
    float leverSide;
    Transform branch;
    const float BranchAngle = 24f;

    // On the track at the switch, turned as the track is there.
    void PlaceJunction()
    {
        Vector3 at = Path(junctionS, out float heading);
        junctionLook.SetPositionAndRotation(at, Quaternion.Euler(0f, heading, 0f));
    }

    // Going on means going off along the branch: a knee at the switch turns the real track (and the train) that way,
    // and what stays drawn is the old line running straight on, left behind.
    void TakeBranch()
    {
        if (!junctionLook) return;
        knees.Add(new Vector2(junctionS, -leverSide * BranchAngle));
        sequenceStarted = true; // no button game for this one
        eventKind = 0;
        turned = true;          // and no other event on this leg
        branch.localRotation = Quaternion.identity;
    }

    void Junction(bool fight)
    {
        junction = false;
        Time.timeScale = 1f;
        leverTo = (fight ? 1f : -1f) * leverSide * 35f; // thrown one way for the branch, the other for the line
        // The junction's truck matches the train: the more weapon levels on board, the more health it has.
        int power = 0;
        foreach (var a in Arsenal()) power += a.level;
        boss.hard = junctionHard * Mathf.Max(1f, power / 24f);
        if (fight && boss.Begin())
        {
            turned = true;                                      // the truck instead of this leg's event
            remaining = Mathf.Max(remaining, stationGapMax * 1.5f); // and road enough to fight it before the station
        }
        else
        {
            boss.hard = 1f;
            Toast(L10n.T("junction.went"));
            if (!Bending) TakeBranch();
        }
    }

    // A boss's or mini boss's chest, as in Vampire Survivors, opened with some show (Chest): one prize, three, or with
    // much luck five. Each is an evolution if one is earned (top level and its partner upgrade owned; the hero's
    // first, then a wagon's), the only way to evolve; otherwise a level on something already owned, at random (the
    // hero's weapon, a wagon's weapon or one of the hero's upgrades); with nothing left to raise, money.
    public int Chests { get; private set; } // opened this run, for the bot's notes
    public void OpenChest()
    {
        Chests++;
        Combat.Instance.Burst("levelup", Player.Position + Vector3.down * 2f, 3f);
        // One prize mostly; three now and then; five only with a lot of luck.
        int luck = PassiveLevel("luck");
        int rolls = Random.value < 0.03f + 0.01f * luck ? 5 : Random.value < 0.22f + 0.03f * luck ? 3 : 1;
        var prizes = new List<Chest.Prize>();
        for (int roll = 0; roll < rolls; roll++)
        {
            // An evolution first, if one is earned (the hero's, then a wagon's): the only way to evolve.
            if (CanEvolve)
            {
                Player.level = Weapon.Evolved;
                prizes.Add(new Chest.Prize { icon = Player.ammo.id, label = Player.ammo.EvolvedName, evolution = true });
                continue;
            }
            Turret ready = Evolvable;
            if (ready)
            {
                ready.Evolve();
                Combat.Instance.Burst("levelup", ready.transform.position, 3f);
                prizes.Add(new Chest.Prize { icon = ready.Weapon.id, label = ready.Weapon.EvolvedName, evolution = true });
                continue;
            }
            // Else a level on something already owned; weapons count twice, as in Vampire Survivors.
            var choices = new List<System.Func<Chest.Prize>>();
            for (int twice = 0; twice < 2; twice++)
            {
                if (Player.level < Weapon.MaxLevel)
                    choices.Add(() => { Player.level++; return new Chest.Prize { icon = Player.ammo.id, label = L10n.T("chest.level", Player.ammo.Name, Player.level) }; });
                for (int i = 0; i < cars.Count; i++)
                {
                    int at = i;
                    Turret gun = cars[i].mount;
                    if (gun && gun.Level < Weapon.MaxLevel)
                        choices.Add(() => { Mount(at, gun.Weapon); return new Chest.Prize { icon = gun.Weapon.id, label = L10n.T("chest.level", gun.Weapon.Name, gun.Level) }; });
                }
            }
            foreach (string id in passiveOrder)
                if (PassiveLevel(id) < MaxPassive)
                {
                    string pick = id;
                    choices.Add(() =>
                    {
                        Apply(pick);
                        return new Chest.Prize { icon = PassiveIcons[System.Array.IndexOf(Passives, pick)], label = L10n.T("chest.level", L10n.T("passive." + pick), PassiveLevel(pick)) };
                    });
                }
            if (choices.Count == 0)
            {
                money += moneyReward;
                prizes.Add(new Chest.Prize { icon = "money", label = L10n.T("chest.money", moneyReward) });
                continue;
            }
            prizes.Add(choices[Random.Range(0, choices.Count)]());
        }
        if (!chest) chest = gameObject.AddComponent<Chest>();
        chest.Show(prizes);
    }
    Chest chest;
    public bool ChestOpen => chest && chest.Open;
    public void CloseChest() { if (chest) chest.Close(); }

    // What the train and the hero are armed with, for the HUD: the hero's weapon first, then every wagon's, front
    // to back (two wagons with the same weapon are two, each at its own level).
    public List<(Weapon weapon, int level)> Arsenal()
    {
        arsenal.Clear();
        arsenal.Add((Player.ammo, Player.level));
        foreach (Car car in cars)
            if (car.mount) arsenal.Add((car.mount.Weapon, car.mount.Level));
        return arsenal;
    }
    readonly List<(Weapon weapon, int level)> arsenal = new();

    // For AutoPlay, the bot that plays fast to measure the difficulty: what is on screen and a way to answer it.
    public static bool NoEvents;                            // it cannot play the button games: none come
    public int MenuKind => MenuOpen && options.Count > 0 ? menuKind : -1;
    public int OptionCount => options.Count;
    public string OptionIcon(int i) => options[i].icon;
    public bool OptionEnabled(int i) => i >= 0 && i < options.Count && options[i].enabled;
    public void Choose(int i) => ActivateOption(i);
    public bool Talking => talk.Open;
    public void SkipTalk() => talk.Advance();
    public bool DeliveryOpen => deliveryReport && deliveryReport.Visible;
    public void CloseDelivery() => deliveryReport.Close();
    public float StoryClock => storyClock;
    public bool Dueling => rival.Active;
    public bool Confused => rival && rival.OnUs(RivalEvent.Mirror); // the rival's hex: the hero's controls reversed

    // The story duel is won: the game stops, the rival has a last word, and an upgrade of this chapter is picked
    // to take along.
    public void RivalBeaten()
    {
        Time.timeScale = 0f;
        Story.Duel(false, out string[] names, out string[] lines, out bool[] rival);
        talk.Show(names, lines, rival, () => chapterWon = true);
    }
    public void BossBar(string name, float fraction) => hud.Boss(name, fraction);
    public bool Swarming => swarm.Aiming || brawl.Fighting; // seen from the gunner's seat (or from inside a boarded wagon) right now
    // The gunner's camera while the swarm is fought, so the HUD can place its damage numbers from that seat.
    public Camera SwarmCamera => swarm != null && swarm.Aiming ? swarm.View : brawl != null && brawl.Fighting ? brawl.View : null;
    public Vector3 CarPosition(int i) => cars[i].pos;
    public void Shake(float seconds) => shake = Mathf.Max(shake, seconds);

    // The gunner the swarm is fought from: the first armed wagon still standing (its turret and its number).
    // Any armed wagon still standing, a different one each time: the swarm does not always fall on the first.
    public Transform Gunner(out int wagon)
    {
        int standing = 0;
        wagon = -1;
        for (int i = 0; i < cars.Count; i++)
            if (cars[i].Alive && cars[i].mount && Random.Range(0, ++standing) == 0) wagon = i;
        return wagon < 0 ? null : cars[wagon].mount.transform;
    }
    public bool Bending => knees.Count > 0; // a curve or zigzag is somewhere on screen
    // Enemies leave the train alone: a button game is on or about to start, or a curve is passing under the wagons.
    public bool Calm => InEvent || Warning || (Bending && Mathf.Abs(knees[0].x) < HalfExtents.x + 8f);
    public bool Zigzag => eventKind == 4 && Bending;

    // How demanding the button games are, 0 (start) to 1 (hardest): they speed up with the difficulty, up to a limit.
    public int hardestAt = 30;
    public float Tempo => Mathf.Clamp01(Difficulty / (float)hardestAt);
    float Step => wagonSize.x + wagonGap + verticalRoom;
    // Extra room between wagons while the train runs up and down the screen: the camera looks down at a slant and
    // squashes that way, so the gaps closed up and the hero could not get between them.
    float verticalRoom;

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
    Vector2 groundScroll;
    Transform ground;
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
    readonly Transform[] stationGuns = new Transform[2]; // the heavy guns at both ends of the platform
    // On arrival the station's guns finish off whatever was still chasing the train; the menu waits for them.
    public bool Clearing => AtStation && Health > 0f && Combat.Instance.EnemyCount > 0;
    public Transform StationGun(int i) => stationGuns[i];
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
    int eventKind, nextEvent;            // this trip's event: 1 curve, 2 ramp jump, 3 needle jump, 4 zigzag, 5 swarm, 6 boss
    ZigzagEvent zig;
    SwarmEvent swarm;
    BrawlEvent brawl;
    BossEvent boss;

    [Header("Bosses")]

    public float bossChance = 0.10f;      // chance a desert leg turns into the boss, at the first station there
    public float bossChanceStep = 0.09f;  // more for every station survived this run, since the way out is barred
    public float bossChanceMax = 0.85f;
    int bossBeaten = -1;                  // biome whose boss is already down this run; -1 none yet
    bool bossNext;                        // this leg was rolled to be the boss's

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
    public Vector3 Focus => Path((NoseS - HalfExtents.x) * 0.5f, out _); // on the track, also round a curve
    public float Reach => (NoseS + HalfExtents.x) * 0.5f + flyRoom; // from Focus to the edge of that clear space
    // 1 curve, 2 ramp jump, 3 needle jump, 4 zigzag, 5 swarm. The curve twice as likely: it is what changes the view.
    static readonly int[] EventBag = { 1, 1, 2, 3, 4, 5, 7 }; // 7: robots board a wagon (BrawlEvent)
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
    public float CarMaxHealth(int i) => cars[i].maxHealth;
    public void HurtCar(int i, float share) => Damage(i, cars[i].maxHealth * share);
    readonly List<Car> cars = new();     // front to back
    Material wagonMat, deadMat, groundMat, sceneryMat;
    Material[] cargoMats;
    // index into Weapon.Wagon, its Length for money, FreeCargo + cargo, or Escort + weapon
    readonly int[] offers = new int[3];
    const int FreeCargo = 100, Escort = 200, Eternal = 300; // Eternal: a permanent container, free: the rarest card
    public float eternalOfferChance = 0.04f;
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
    bool deliveryPending, delivering;
    DeliveryReport deliveryReport;
    int repairsBought, wagonsBought, containersBought, kits;
    // One of each per biome reached, so a longer train means moving on.
    // Three armed wagons for sale in each biome (in a story, in the chapter), each dearer than the last.
    const int WagonsPerBiome = 3;
    bool WagonForSale => wagonsBought < WagonsPerBiome * (Story.Active ? 1 : Biome + 1) && cars.Count < maxCars;
    public const int MaxEternal = 3;
    int Eternals { get { int n = 0; foreach (Car c in cars) if (c.permanent) n++; return n; } }
    bool ContainerForSale => Eternals < MaxEternal && cars.Count < maxCars;
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
    AudioSource whistleSound, wagonBrokenSound;
    bool whistlePending = true;
    bool warned;
    float LegLength => stationGap * legScale * Legs[leg].length;
    static string LegName(int leg) => L10n.T("leg." + Legs[leg].id);

    // The open menu as a flat list, so mouse, keyboard and gamepad all drive the same options.
    public struct Option
    {
        public string icon, label;
        public System.Action run;
        public System.Action<int> adjust; // a setting: left / right change it by a step (-1 or +1)
        public string badge;    // short tag on the card's corner: "FREE", a price, "FULL"...
        public int tone;        // of that tag: 1 free (green), 2 price (gold), 3 cannot (red)
        public bool minor;      // not an offer but a way out (skip, done, back): drawn small and apart
        public int shelf;       // which row of cards it is on, in a menu with two (the market)
        public bool enabled;
        public Vector3? anchor; // world spot this option is about; framed in yellow while focused
        public Vector2 extent;  // half size (x, z) of that frame
    }
    readonly Combat.Batch highlight = new Combat.Batch(new Color(1f, 0.82f, 0.1f), false);
    readonly List<Option> options = new();
    StationMenu menuView; // draws the station menus and the game-over question
    string menuTitle;
    // Which of the station's five steps the menu is (0 train, 1 player, 2 cargo, 3 shop, 4 route; -1 none), the
    // one-line explanation under its title, and whether money matters in it (then the purse is shown).
    int menuStep;
    string menuSub;
    bool menuMoney;
    string[] menuLines;                      // names of the menu's tabs, when it has them (the market)
    int marketTab, menuTab;                  // 0 cargo, 1 shop
    bool menuCompact;             // compact: only small buttons low on the screen, the train in view
    Car ghost;                               // the cargo wagon being placed: in the train already, painted red
    int ghostAt;
    Material ghostMat;
    Turret weaponGhost;                      // the weapon being given a wagon: shown on the train, in red
    readonly List<StationMenu.Slot> trainSlots = new(); // the train as the menu draws it while something is being placed

    Camera sideCam;          // films the train from the side for that drawing
    RenderTexture sideView;

    // The side camera, square on to the train with the engine on the left, far enough off to take all of it in.
    // It sees only a thin slice of the world around the track: the train, the rails and the ground under them.
    void AimSideCamera()
    {
        if (!sideCam)
        {
            sideView = new RenderTexture((int)StationMenu.PictureWide, (int)StationMenu.PictureTall, 24);
            sideCam = new GameObject("SideView").AddComponent<Camera>();
            sideCam.orthographic = true;
            sideCam.targetTexture = sideView;
            var data = sideCam.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            data.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
            data.backgroundColorHDR = new Color(0.66f, 0.84f, 1f);
        }
        Vector3 along = loco.position - cars[0].pos;
        along.y = 0f;
        along.Normalize();
        Vector3 head = loco.position + along * 4.2f, tail = cars[cars.Count - 1].pos - along * (wagonSize.x * 0.5f + 0.6f);
        float aspect = StationMenu.PictureWide / StationMenu.PictureTall;
        sideCam.orthographicSize = Mathf.Max(2.5f, (head - tail).magnitude * 0.5f / aspect);
        sideCam.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, along), Vector3.up) * Quaternion.Euler(8f, 0f, 0f);
        sideCam.transform.position = (head + tail) * 0.5f + Vector3.up * (sideCam.orthographicSize * 0.72f) - sideCam.transform.forward * 30f;
        sideCam.nearClipPlane = 30f - 2.6f;
        sideCam.farClipPlane = 30f + 2.6f;
    }

    // One tag of that drawing per wagon. chosen: the wagon being picked; what: what would go there.
    void DrawTrain(int chosen, string what, string effect, bool weapons)
    {
        AimSideCamera();
        for (int i = 0; i < cars.Count; i++)
        {
            Car car = cars[i];
            var slot = new StationMenu.Slot { tag = (i + 1).ToString(), x = sideCam.WorldToViewportPoint(car.pos).x };
            if (i == chosen) { slot.icon = what; slot.label = effect; slot.state = 1; }
            else if (car.cargo >= 0) { slot.icon = Cargos[car.cargo].id; slot.state = weapons ? 2 : 0; } // cargo carries no weapon
            else if (car.mount) { slot.icon = car.mount.Weapon.id; slot.label = car.mount.Level >= Weapon.Evolved ? "EVO" : L10n.T("strip.level", car.mount.Level); }
            else { slot.icon = "slot"; slot.label = L10n.T("menu.empty"); }
            trainSlots.Add(slot);
        }
    }

    int weaponAt;
    // The very first station of all walks the player through: an arrow on the cards and a louder hint, once.
    bool Guiding => AtStation && Stations == 1 && PlayerPrefs.GetInt("guide.station", 0) == 0;
    int menuKind = -1, focus;
    bool navHeld;
    const bool mainMenu = false, settingsMenu = false; // the title screen is its own scene now (MainMenu.cs)
    InputAction navigate, confirm, stepRight, stepLeft;
    bool ListMenu => Paused || settingsMenu;  // both draw their options as a vertical list
    bool MenuOpen => mainMenu || chapterWon || junction || (AtStation && !Clearing && !deliveryPending) || Health <= 0f || Paused;

    // Pause: Start on a gamepad, Esc on the keyboard. Freezes time and opens its own menu.
    public bool Paused { get; private set; }
    float scaleBeforePause; // a curve or jump may have been running in slow motion
    int confirming;         // pause menu asking "are you sure?": 0 no, 1 restart, 2 quit
    bool soundMenu, sideHeld; // pause menu showing the volumes

    void SetPaused(bool pause)
    {
        if (pause == Paused) return;
        Sound.Play("Pause");
        Paused = pause;
        confirming = 0;
        soundMenu = false;
        evolutionsMenu = false;
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
        whistleSound = Sound.Effect(gameObject, "whistle", 0.55f);
        wagonBrokenSound = Sound.Effect(gameObject, "vagonroto", 0.6f);
        // The numbers are designed small and scaled here, once per run: the scene carries the unscaled values,
        // so this is what makes every health pool and payout read ten times bigger. See Balance.Scale.
        wagonMaxHealth *= Balance.Scale;
        repairReward *= Balance.Scale;
        healReward *= Balance.Scale;
        skipHealth *= Balance.Scale;
        vitalityReward *= Balance.Scale;
        kitHeal *= Balance.Scale;

        if (Story.Active) Biome = Story.Current.biome; // a story chapter is ridden in one biome, start to end
        Material steel = Mat(Color.gray);
        groundMat = Mat(Biomes[0].ground, false);
        groundMat.mainTextureScale = Vector2.one * (viewRange * 8f / groundTile);
        PaintGround();
        sceneryMat = Mat(Biomes[Biome].scenery);
        wagonMat = Mat(new Color(0.62f, 0.66f, 0.72f)); // an armed wagon with no weapon yet
        deadMat = Mat(Color.black);
        escortMat = Mat(new Color(0.85f, 0.65f, 0.2f));
        cargoMats = new Material[Cargos.Length];
        for (int i = 0; i < Cargos.Length; i++) cargoMats[i] = Mat(Cargos[i].color);

        float length = viewRange * 4f;
        ground = Box(transform, new Vector3(0f, -0.5f, 0f), new Vector3(length * 2f, 1f, length * 2f), groundMat, true);
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
        RollEvent(); // the first leg rolls for a boss too, same as every other

        zig = gameObject.AddComponent<ZigzagEvent>();
        swarm = gameObject.AddComponent<SwarmEvent>();
        brawl = gameObject.AddComponent<BrawlEvent>();
        boss = gameObject.AddComponent<BossEvent>();
        talk = gameObject.AddComponent<Talk>();
        rival = gameObject.AddComponent<RivalEvent>();
        menuView = gameObject.AddComponent<StationMenu>();
        hud = gameObject.AddComponent<Hud>();
        Panel = gameObject.AddComponent<EventPanel>();
        turn = gameObject.AddComponent<TurnEvent>();
        jump = gameObject.AddComponent<JumpEvent>();
        wagonModel = Resources.Load<GameObject>("Models/Wagon");
        cargoModels = System.Array.ConvertAll(Cargos, c => Resources.Load<GameObject>("Models/Cargo_" + c.id));
        loco = Locomotives.Create(transform, Locomotives.Selected);
        for (int i = 0; i < wagons; i++) AddCar(i, -1);
        Mount(0, Weapon.Wagon[0]); // two wagons to start with, one armed: the other waits for the first weapon won
        Player = new GameObject("Player").AddComponent<Player>();
        Player.Init(this);
        if (Story.Active) foreach (string token in Story.Kept()) Apply(token); // what earlier chapters handed down
        if (Story.Active && Story.CarriedTruck > 0) GiveTruck(Story.CarriedTruck, true); // the truck's wagon, one chapter on
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
        bloom.intensity.value = 0.6f;
        bloom.scatter.value = 0.7f; // shots and flashes are pushed past white: they glow, the rest stays clean
        bloom.threshold.value = 1f;
        var grade = look.profile.Add<UnityEngine.Rendering.HighDefinition.ColorAdjustments>(true);
        grade.saturation.value = 8f;
        grade.contrast.value = 6f;

        station = new GameObject("Station").transform;
        station.SetParent(transform, false);
        GameObject gunModel = Resources.Load<GameObject>("Models/StationGun");
        for (int i = 0; i < stationGuns.Length; i++)
        {
            // A pivot that looks along +z with the model turned round inside it (its barrels point down its -z).
            stationGuns[i] = new GameObject("StationGun").transform;
            stationGuns[i].SetParent(station, false);
            stationGuns[i].localPosition = new Vector3(i == 0 ? -14.5f : 14.5f, 0.6f, 6f);
            stationGuns[i].localRotation = Quaternion.Euler(0f, 180f, 0f); // at rest: over the track
            Transform body = Instantiate(gunModel, stationGuns[i]).transform;
            body.localRotation = Quaternion.Euler(0f, 180f, 0f);
            body.localScale = Vector3.one * 0.8f;
            Toon(body.GetComponentInChildren<Renderer>());
        }
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
        Time.timeScale = 1f;
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
        else if (Zigzag && !sequenceStarted) { now = knees[0].x; start = FrontS + RunSpeed * 0.6f; }
        else if (Jumping && jump.Pending) { now = gapS; start = jump.StartS; }
        else return -1f;
        float window = Mathf.Max(RunSpeed, 1f) * warnSeconds * 0.85f; // 0.85: about the average time scale on the way in
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
            ground.localRotation = back * ground.localRotation; // the ground is part of that world
            camYaw = Mathf.DeltaAngle(0f, camYaw - total); // kept within half a turn either way
            AimCamera();
        }
        PlaceTrack();
        Layout();
    }

    void Update()
    {
        Scroll = 0f; // the ground only slides while the train is running (below)
        if (UltimatePresenting) return;
        // Temporary shortcut for trying the ultimate without waiting for kills.
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame && !Paused && Health > 0f && !UltimateReady)
        {
            ultimateCharge = ultimateKills;
            ultimateAnnounce = true;
            Sound.Play("newcard");
        }
        // Regeneration: the wagons and the hero mend a little every moment, between stations too.
        float regen = regenPerLevel * PassiveLevel("regen") * Time.deltaTime;
        if (regen > 0f && Health > 0f && !Paused)
        {
            Repair(regen, true);
            if (Player && Player.Alive) Player.Heal(regen * 0.5f, true);
        }
        if (Story.Active && !AtStation && !Paused && !chapterWon && Health > 0f)
        {
            // The chapter's clock; when it runs out, the rival comes alongside as soon as nothing else is on.
            storyClock += Time.deltaTime;
            if (!junctionLook && !junctionAsked && storyClock >= JunctionAt && !BossFight && !InEvent && !Warning && !Bending && !duelCalled)
                LayJunction();
            if (junctionLook && !junctionAsked && junctionS - NoseS < -15f)
            {
                // It went by while something else was on: it is laid again further along.
                Destroy(junctionLook.gameObject);
                junctionLook = null;
            }
            if (junctionLook && !junctionAsked && junctionS - NoseS <= 4f && !BossFight && !InEvent)
            {
                // The Y junction: the game stops at the lever until a way is chosen.
                junctionAsked = junction = true;
                Time.timeScale = 0f;
                Sound.Play("Pause");
            }
            if (!duelCalled && storyClock >= Story.Seconds && !BossFight && !InEvent && !Warning && !Bending)
            {
                // Time is up: the rival pulls alongside, a few words are traded over the stopped game, then the duel.
                duelCalled = true;
                Time.timeScale = 0f;
                Story.Duel(true, out string[] names, out string[] lines, out bool[] rival);
                talk.Show(names, lines, rival, () => { Time.timeScale = 1f; this.rival.Begin(Story.Current.rival); duel = true; });
            }
        }
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) L10n.NextLocale();
        // ponytail: reads the devices directly, like the medkit and the button games.
        if (junction && ((Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)))
            Junction(false); // the junction's second way, on its own button
        if (!mainMenu && Health > 0f && !talk.Open && ((Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) ||
                            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
            SetPaused(!Paused);

        // A long train pulls the camera back so it always fits, whichever way the track runs on screen.
        Camera view = Camera.main;
        // Big enough for the train from tail to nose plus flyRoom past each end and to each side, whichever way
        // the track runs across the picture.
        float half = Reach, fit = 0f;
        Vector3 right = view.transform.right, up = view.transform.up;
        // The wagons' tops count too (running up the screen, their height reaches further up it), and up and down the
        // picture a fifth more: the HUD across the top, and room for the hero to fly round the ends.
        for (int corner = 0; corner < 8; corner++)
        {
            var point = new Vector3(corner % 4 < 2 ? half : -half, corner < 4 ? 0f : 3.5f, corner % 2 == 0 ? flyRoom : -flyRoom);
            fit = Mathf.Max(fit, Mathf.Abs(Vector3.Dot(point, up)) * 1.2f, Mathf.Abs(Vector3.Dot(point, right)) / view.aspect);
        }
        float wanted = Mathf.Max(cameraSize, fit) * (Zigzag ? zigZoom : 1f) * cameraZoom;
        view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, wanted, (Paused ? 0f : 9f) * Time.unscaledDeltaTime);
        shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime);
        view.transform.position = Focus - view.transform.forward * 60f + Random.insideUnitSphere * shake * 1.5f;

        // Triangle / Y on a gamepad, R on the keyboard: the ultimate once charged (or, were they in use, a repair kit).
        // Not at stations and not mid-sequence. ponytail: reads the devices directly, like the curve's button sequence.
        if (ultimateAnnounce && UltimateReady && !Alerting && !hud.Toasting && !AtStation && !Paused)
        {
            ultimateAnnounce = false;
            hud.UltimateReady();
        }
        bool special = (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);
        if (special && !AtStation && !InEvent && !Paused && !mainMenu && Health > 0f)
        {
            if (KitsInUse && kits > 0)
            {
                kits--;
                Repair(kitHeal);
                Player.Heal(kitHeal);
            }
            else if (UltimateReady && Player.Alive && !Alerting)
            {
                ultimateCharge = 0f;
                if (!ultimateCutIn) ultimateCutIn = gameObject.AddComponent<UltimateCutIn>();
                ultimateCutIn.Play(() => Combat.Instance.Ultimate(Player.Position, Player.ammo));
                if (UltimatePresenting) return;
            }
        }

        if (deliveryReport) deliveryReport.SetVisible(!Paused);
        if (deliveryPending && !delivering && !Clearing && !Paused && Health > 0f)
            StartCoroutine(DeliverCargo());
        if (MenuOpen)
        {
            BuildMenu();
            Navigate();
        }
        string help = soundMenu ? (Player.UsingGamepad ? "sound.help.gamepad" : "sound.help.keyboard")
            : settingsMenu ? (Player.UsingGamepad ? "settings.help.gamepad" : "settings.help.keyboard")
            : Paused ? (Player.UsingGamepad ? "pause.help.gamepad" : "pause.help.keyboard")
                             : (Player.UsingGamepad ? "menu.help.gamepad" : "menu.help.keyboard");
        if (MenuOpen && trainSlots.Count > 0) menuView.Train(trainSlots, sideView);
        if (sideCam) sideCam.enabled = MenuOpen && trainSlots.Count > 0; // it films only while its picture is on show
        menuView.Render(MenuOpen ? options : null, menuTitle, focus, L10n.T(help), ActivateOption, i => { if (focus != i) Sound.Play("click"); focus = i; }, ListMenu,
            menuStep, menuSub, menuMoney ? money : -1, menuLines, Guiding && !Paused && Health > 0f, menuTab, tab => { Sound.Play("confirm"); marketTab = tab; focus = 0; }, menuCompact);

        if (AtStation || Paused || mainMenu) return;

        // Brake curve: fastest speed that can still stop exactly on the station. A boss on the field is the
        // exception: the train runs on at cruise but makes no headway, so the station is never reached mid-fight.
        float step;
        if (BossFight)
        {
            Speed = Mathf.MoveTowards(Speed, maxSpeed, acceleration * Time.deltaTime);
            step = Speed * Time.deltaTime;
        }
        else
        {
            float target = Mathf.Clamp(Mathf.Sqrt(2f * acceleration * remaining), 0.5f, maxSpeed * Legs[leg].speed * (Zigzag ? zigSpeed : 1f));
            Speed = Mathf.MoveTowards(Speed, target, acceleration * Time.deltaTime);
            step = Mathf.Min(Speed * Time.deltaTime, remaining);
            remaining -= step;
        }
        if (whistlePending && Speed > 0f)
        {
            whistlePending = false;
            if (whistleSound.clip) whistleSound.PlayOneShot(whistleSound.clip);
        }
        Scroll = step;
        // The camera turns on after a curve is over: keep the gaps in step with how upright the train looks now.
        if (!Bending && Camera.main)
        {
            Path(0f, out float facing);
            Vector3 dir = transform.rotation * (Quaternion.Euler(0f, facing, 0f) * Vector3.right);
            float upright = Mathf.Abs(Vector3.Dot(dir, Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized));
            if (Mathf.Abs(1.2f * upright * upright - verticalRoom) > 0.02f) Layout();
        }
        // Only on the way in: once the train has left, the station slides back with the ground (where it is no
        // longer matters) and the rush comes straight back.
        bool stationAhead = stationX < viewRange * 1.3f && stationX > 0.5f;
        rushNow = Mathf.MoveTowards(rushNow, stationAhead ? 1f : rush, (stationAhead ? 1.2f : 2.5f) * Time.deltaTime);
        float slideBy = step * rushNow;
        if (junctionLook)
        {
            junctionS -= slideBy;
            PlaceJunction();
            lever.localRotation = Quaternion.RotateTowards(lever.localRotation, Quaternion.Euler(leverTo, 0f, 0f), 240f * Time.unscaledDeltaTime);
            if (junctionS < -viewRange * 0.9f) { Destroy(junctionLook.gameObject); junctionLook = null; } // gone before the world turns back
        }

        if (!AtStation && !Zigzag && Speed > maxSpeed * 0.6f) Flash("wind"); // the wind of speed (not the zigzag: its zoom outgrows the effect)
        // The ground itself never moves: its texture slides back under the train instead.
        // It slides against the way the train's middle is going (round a curve that is not along x), in the
        // ground's own axes; the cube's top face has u and v running towards -x and -z.
        Path(0f, out float going);
        Vector3 slide = Quaternion.Inverse(ground.localRotation) * (Quaternion.Euler(0f, going, 0f) * Vector3.right) * (slideBy / groundTile);
        groundScroll = new Vector2(Mathf.Repeat(groundScroll.x - slide.x, 1f), Mathf.Repeat(groundScroll.y - slide.z, 1f));
        groundMat.mainTextureOffset = groundScroll;
        sleepers.offset = Mathf.Repeat(sleepers.offset + slideBy, sleepers.spacing);
        // A new stretch of landscape starts ahead, out of sight: props coming round from now on belong to it.
        stretchLeft -= step;
        if (stretchLeft <= 0f) NewStretch();
        foreach (Prop prop in props)
        {
            prop.s -= slideBy;
            if (prop.s >= -viewRange) continue;
            prop.s += viewRange * 2f + Random.Range(0f, 6f);
            Dress(prop);
        }

        if (!BossFight) stationX -= stationX <= 0f ? slideBy : step; // the station waits off ahead for as long as a boss holds the leg
        if (stationX < -viewRange)
        {
            // Reuse the one station for the next stop; out of sight is also the moment to change its look.
            stationX = remaining;
            for (int b = 0; b < stationLooks.Length; b++) stationLooks[b].SetActive(b == Biome);
        }

        // One event per trip, early enough that it is fully behind before the station.
        float room = nextEvent == 4 ? zigStart + 5f * zigSpacing + viewRange + 30f : viewRange * 2f + 20f;
        float eventAt = Mathf.Max(LegLength * (1f - turnAt), room);
        if (!turned && remaining <= eventAt && !rival.Active && !NoEvents) // no curve or jump under the duel
        {
            turned = true;
            eventKind = nextEvent;
            if (eventKind == 5 && !swarm.Begin()) eventKind = 1; // nobody left to man a gun: a curve instead
            if (eventKind == 6 && !boss.Begin()) eventKind = 1;  // the rig would not load: a curve instead
            if (eventKind == 7 && !brawl.Begin()) eventKind = 1; // no armed wagon to board, or the hero is down: a curve instead
            if (eventKind == 1) StartBend();
            else if (eventKind == 4) StartZigzag();
            else if (eventKind >= 5) { } // they run themselves from here
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
            gapS -= slideBy;
            if (gapS < -viewRange) EndJump();
        }
        if (Bending)
        {
            for (int i = 0; i < knees.Count; i++) knees[i] -= new Vector2(slideBy, 0f);
            float trainLength = HalfExtents.x * 2f;
            if (!sequenceStarted && eventKind == 1 && knees[0].x <= trainLength * 0.5f)
            {
                // The button sequence lasts exactly as long as the train takes to go through the knee.
                sequenceStarted = true;
                turn.Begin(trainLength / Mathf.Max(RunSpeed, 1f));
            }
            if (!sequenceStarted && eventKind == 4 && knees[0].x <= FrontS + RunSpeed * 0.6f)
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

        if (remaining <= 0f && !BossFight) Arrive(); // never arrive with a boss still on the field
    }

    void Arrive()
    {
        if (Bending) EndBend();
        if (Jumping) EndJump();
        Speed = 0f;
        AtStation = true;
        Stations++;
        deliveryPending = true;
        delivering = false;
        nextLeg = Random.Range(0, Legs.Length);
        stage = 0;
    }

    // Settle after the station guns finish, including cargo recovered from balloons during the cleanup.
    IEnumerator DeliverCargo()
    {
        delivering = true;
        var results = new List<DeliveryReport.Entry>();
        cargoEarned = cargoFined = 0;
        for (int i = cars.Count - 1; i >= 0; i--)
        {
            Car car = cars[i];
            bool stays = (car.cargo < 0 && !car.temporary) || car.permanent;
            if (car.cargo >= 0 || car.temporary)
            {
                int value = car.permanent ? (car.Alive ? (car.guise >= 0 ? car.reward : Mathf.RoundToInt(Cargos[Container].reward * Legs[leg].length * CargoShare) * (Biome + 1)) : 0)
                    : car.Alive ? car.reward : -car.penalty;
                string id = car.guise >= 0 ? Cargos[car.guise].id : car.cargo >= 0 ? Cargos[car.cargo].id : "wagon";
                results.Add(new DeliveryReport.Entry { icon = "delivery_" + id, name = car.cargo >= 0 ? L10n.T("cargo." + id) : L10n.T("delivery.escort"), value = value, destroyed = !car.Alive });
                if (value >= 0) cargoEarned += value; else cargoFined -= value;
                Vector3 start = car.body.localPosition, scale = car.body.localScale;
                hud.DeliveryNumber(car.pos + Vector3.up * 3f, value);
                Sound.Play(car.Alive ? "wagongood" : "wagonbad");
                Combat.Instance.Burst(car.Alive ? "levelup" : "enemy", car.pos + Vector3.up, 1.5f);
                for (float t = 0f; t < 1f;)
                {
                    if (!Paused)
                    {
                        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.8f);
                        float ease = t * t * (3f - 2f * t);
                        car.body.localPosition = start + Vector3.up * (ease * (stays ? 1.5f * Mathf.Sin(t * Mathf.PI) : 5f));
                        if (!stays) car.body.localScale = scale * Mathf.Lerp(1f, 0.05f, ease);
                        if (car.mount) car.mount.transform.position = car.body.position + Vector3.up * (0.45f + wagonSize.y - car.lift);
                    }
                    yield return null;
                }
                car.body.localPosition = start;
                car.body.localScale = scale;
                if (car.guise >= 0) Guise(car, -1); // delivered: an eternal container again until the train leaves
            }
            if (stays)
            {
                // Stays with the train: back on its wheels if it was knocked out.
                if (car.Alive) continue;
                car.health = car.maxHealth * stationRepair;
                if (car.health > 0f) Sound.Play("healthup");
                SetAlive(car, true);
                continue;
            }
            if (car.mount) Destroy(car.mount.gameObject);
            Destroy(car.body.gameObject);
            cars.RemoveAt(i);
            // Close the gap smoothly instead of snapping the remaining train to its new length.
            var from = new Vector3[cars.Count];
            for (int j = 0; j < cars.Count; j++) from[j] = cars[j].body.localPosition;
            Layout();
            var to = new Vector3[cars.Count];
            for (int j = 0; j < cars.Count; j++) to[j] = cars[j].body.localPosition;
            for (float t = 0f; t < 1f;)
            {
                if (!Paused)
                {
                    t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.35f);
                    for (int j = 0; j < cars.Count; j++)
                    {
                        cars[j].body.localPosition = Vector3.Lerp(from[j], to[j], Mathf.SmoothStep(0f, 1f, t));
                        if (cars[j].mount) cars[j].mount.transform.position = cars[j].body.position + Vector3.up * (0.45f + wagonSize.y - cars[j].lift);
                    }
                }
                yield return null;
            }
        }
        money = Mathf.Max(0, money + cargoEarned - cargoFined);
        Layout();

        Player.OnStation();
        RollOffers();
        stage = 0;
        if (!deliveryReport) deliveryReport = gameObject.AddComponent<DeliveryReport>();
        deliveryReport.Show(results, cargoEarned - cargoFined, money, () => StartCoroutine(CompleteDelivery()));
    }

    IEnumerator CompleteDelivery()
    {
        // Let the report's confirm input expire before enabling upgrade navigation.
        yield return null;
        deliveryPending = false;
        delivering = false;
        focus = 0;
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
            car.reward = Mathf.RoundToInt(Cargos[cargo].reward * Legs[AtStation ? nextLeg : leg].length * CargoShare * CargoBoost(cargo)) * (Biome + 1); // what the card said
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

    // An eternal container takes on goods for the trip (look, toughness and pay of that load at its level), or (-1)
    // goes back to being a container. What share of its health it had, it keeps.
    void Guise(Car car, int goods)
    {
        int look = goods >= 0 ? goods : Container;
        float share = car.maxHealth > 0f ? car.health / car.maxHealth : 1f;
        car.guise = goods;
        car.maxHealth = Cargos[look].health;
        car.health = car.maxHealth * share;
        if (goods >= 0) car.reward = Mathf.RoundToInt(Cargos[goods].reward * Legs[leg].length * CargoShare * CargoBoost(goods)) * (Biome + 1);
        Destroy(car.body.gameObject);
        GameObject model = cargoModels[look];
        car.body = model ? Instantiate(model, transform).transform : Box(transform, Vector3.zero, new Vector3(wagonSize.x, wagonSize.y * 0.6f, wagonSize.z), cargoMats[look]);
        car.lift = model ? 0.1f : 0.2f + wagonSize.y * 0.3f;
        if (model) Toon(car.body.GetComponentInChildren<Renderer>());
        car.skins = System.Array.ConvertAll(car.body.GetComponentsInChildren<MeshRenderer>(), r => r.sharedMaterials);
        if (!car.Alive) SetAlive(car, false);
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

    // The legs are short now and the stations many: each delivery pays this share of what a long leg did.
    const float CargoShare = 0.6f;
    int CargoReward(int cargo) => Mathf.RoundToInt(Cargos[cargo].reward * Legs[nextLeg].length * CargoShare * CargoBoost(cargo)) * (Biome + 1);
    // Each goods type levels up every third load of it this run: it costs more and pays better (the same share).
    readonly int[] cargoLoads = new int[Loadable];
    public int CargoLevel(int cargo) => cargo < Loadable ? 1 + Mathf.Min(4, cargoLoads[cargo] / 3) : 1;
    float CargoBoost(int cargo) => 1f + 0.35f * (CargoLevel(cargo) - 1);
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

    // Goods shot down by the hero or station guns: coupled anywhere behind the locomotive.
    public void AddLoot(int cargo)
    {
        if (CanTakeLoot) AddCar(Random.Range(1, cars.Count + 1), cargo);
    }

    void Layout()
    {
        ShapeTrack();
        if (Camera.main)
        {
            Path(0f, out float going);
            Vector3 along = transform.rotation * (Quaternion.Euler(0f, going, 0f) * Vector3.right);
            Vector3 look = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            float upScreen = Mathf.Abs(Vector3.Dot(along, look));
            verticalRoom = 1.2f * upScreen * upScreen;
        }
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

    // The standing wagon an enemy has picked for itself (pick: any number of its own), so a crowd spreads along the
    // whole train instead of heaping up on the nearest wagon and moving on together when it falls.
    public int WagonFor(int pick, Vector3 p, out Vector3 toWagon)
    {
        int standing = 0;
        foreach (Car car in cars) if (car.Alive) standing++;
        toWagon = default;
        if (standing == 0) return -1;
        int nth = pick % standing;
        for (int i = 0; i < cars.Count; i++)
            if (cars[i].Alive && nth-- == 0) { toWagon = ToWagon(i, p); return i; }
        return -1;
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
        if (rival && rival.OnUs(RivalEvent.Weak)) amount *= 2f; // the rival broke our armour
        Car car = cars[wagon];
        bool stood = car.Alive;
        car.health = Mathf.Max(0f, car.health - amount);
        if (amount > 0f) DamageNumber(CarTop(wagon), amount, true); // the number pops over the wagon that took it
        if (car.Alive) return;
        SetAlive(car, false);
        if (stood && wagonBrokenSound.clip) wagonBrokenSound.PlayOneShot(wagonBrokenSound.clip);
        if (stood && car.cargo == Fuel)
        {
            // The fuel goes up: a big blast that also hurts the wagons next to it (and can set off another tank).
            Combat.Instance.Burst("rocket", car.pos + Vector3.up, fuelBlastRadius * 0.6f);
            Shake(0.5f);
            for (int i = 0; i < cars.Count; i++)
                if (cars[i] != car && cars[i].Alive && (cars[i].pos - car.pos).sqrMagnitude <= fuelBlastRadius * fuelBlastRadius)
                    Damage(i, fuelBlast * Balance.Scale);
        }
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
        // Only weapons the train can take: a new one needs a wagon with no weapon, one it has needs a copy below the
        // top. Whatever is missing is made up with money and health.
        offerPool.Clear();
        bool free = cars.Exists(c => c.cargo < 0 && !c.mount);
        for (int i = 0; i < Weapon.Wagon.Length; i++)
        {
            Turret owned = Owned(Weapon.Wagon[i]);
            if ((Story.Active || Weapon.Wagon[i].firstBiome <= Biome) && (free || (owned && owned.Level < Weapon.MaxLevel))) // the story offers every weapon from the start
                offerPool.Add(i);
        }
        offerPool.Add(Weapon.Wagon.Length);     // money
        offerPool.Add(Weapon.Wagon.Length + 1); // health
        while (offerPool.Count < offers.Length) offerPool.Add(offerPool.Count % 2 == 0 ? Weapon.Wagon.Length : Weapon.Wagon.Length + 1);
        Draw(offers);
        if (cars.Count < maxCars && Random.value < bonusOfferChance + 0.04f * PassiveLevel("luck"))
            offers[Random.Range(0, offers.Length)] = Random.value < 0.5f ? FreeCargo + Random.Range(0, Loadable)
                : Escort + Random.Range(0, Weapon.Wagon.Length); // an escort comes with its own wagon
        if (cars.Count < maxCars && Random.value < eternalOfferChance) offers[Random.Range(0, offers.Length)] = Eternal;

        offerPool.Clear(); // the hero's weapon is her own for good: only her upgrades and its levels are offered
        // The upgrades: speed, armor, vitality, damage, money, (5: a level on the weapon), arsenal fire rate, critical
        // chance, critical damage; none past its top level.
        // At most MaxOwnedPassives different ones: once she has that many, only those are raised further. Whatever
        // is missing is made up with money and health.
        bool full = passiveOrder.Count >= MaxOwnedPassives;
        for (int stat = 0; stat <= Passives.Length; stat++)
        {
            if (stat == 5) { if (Player.level < Weapon.MaxLevel) offerPool.Add(Weapon.Hero.Length + 5); continue; }
            string id = Passives[stat < 5 ? stat : stat - 1];
            if (PassiveLevel(id) >= MaxPassive || (full && PassiveLevel(id) == 0)) continue;
            if ((stat == 1 && Player.armor >= Player.MaxArmor) || (stat == 7 && criticalChance >= 1f)) continue;
            offerPool.Add(Weapon.Hero.Length + stat);
        }
        offerPool.Add(Weapon.Hero.Length + HeroMoney);
        offerPool.Add(Weapon.Hero.Length + HeroHealth);
        while (offerPool.Count < heroOffers.Length) offerPool.Add(Weapon.Hero.Length + (offerPool.Count % 2 == 0 ? HeroMoney : HeroHealth));
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
        CancelGhost();
        if (weaponGhost) CancelWeapon();
        skipGift = 0;
        pending = null;
        pendingCargo = -1;
        stage++;
    }

    void Repair() => Repair(repairReward);

    public void Repair(float amount, bool quiet = false) // quiet: a trickle (regeneration), no sound
    {
        bool healed = false;
        foreach (Car car in cars)
            if (car.Alive) {
                healed |= amount > 0f && car.health < car.maxHealth;
                car.health = Mathf.Min(car.maxHealth, car.health + amount); // a wreck waits for the station
            }
        if (healed && !quiet) Sound.Play("healthup");
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
        if (Guiding) PlayerPrefs.SetInt("guide.station", 1); // the walk-through was seen
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
        RollEvent();
        // Never the same ride twice; the first three legs stay short so the first upgrades and repairs come soon.
        legScale = Stations < 3 ? 1f : Random.Range(1f, stationGapMax / stationGap);
        remaining = LegLength;
        foreach (Car car in cars) if (car.permanent) Guise(car, Random.Range(0, Fuel)); // wood, rock or arms (never fuel)
        AtStation = false;
        whistlePending = true;
    }

    // Which event this leg gets. The desert boss is the special case: nobody may leave the desert until it is down.
    // Only the desert rig is built so far; every leg there is another roll, more likely the longer the player has
    // had to get ready, and once beaten it never comes back for the rest of the run.
    void RollEvent()
    {
        bossNext = !Story.Active && Biome == 0 && bossBeaten != Biome && !duelCalled && // in a story it waits at the junction instead
                   Stations >= 3 && // at least 3 stations reached before first boss
                   Random.value < Mathf.Min(bossChance + bossChanceStep * (Stations - 3), bossChanceMax);
        nextEvent = bossNext ? 6 : EventBag[Random.Range(0, EventBag.Length)];
    }

    List<string> keepOffer; // the three upgrades shown when the chapter is won, fixed once drawn

    // Hands back an upgrade kept from an earlier chapter (a Story token).
    void Apply(string token)
    {
        string[] part = token.Split(':');
        if (System.Array.IndexOf(Passives, part[0]) >= 0)
        {
            if (PassiveLevel(part[0]) == 0) passiveOrder.Add(part[0]);
            passiveLevels[part[0]] = PassiveLevel(part[0]) + 1;
        }
        switch (part[0])
        {
            case "ammo": break; // from before weapons were fixed: nothing to hand back
            case "evolve": Player.level = Mathf.Min(Weapon.MaxLevel, Player.level + 1); break;
            case "speed": Player.SpeedUp(); break;
            case "armor": Player.ArmorUp(); break;
            case "vitality": Player.VitalityUp(vitalityReward); break;
            case "damage": Player.DamageUp(); break;
            case "greed": Player.GreedUp(); break;
            case "fireRate": fireRate += 0.2f; break;
            case "critChance": criticalChance = Mathf.Min(1f, criticalChance + 0.05f); break;
            case "critDamage": criticalMultiplier += 0.25f; break;
            case "wagon":
                Weapon weapon = System.Array.Find(Weapon.Wagon, w => w.id == part[1]);
                if (weapon == null) break;
                // On a wagon with no weapon if there is one, else on the one carrying the same (a level up).
                int at = cars.FindIndex(c => c.cargo < 0 && !c.mount);
                if (at < 0) at = cars.FindIndex(c => c.cargo < 0 && c.mount && c.mount.Weapon == weapon);
                if (at >= 0) Mount(at, weapon);
                break;
        }
    }

    // The upgrade to keep was picked: the story goes on with the chapter's ending and the next one's opening.
    void EndChapter(string keep)
    {
        // Saved: what this chapter hands on (the truck's wagon only if won here), and the next one to continue with.
        Story.Win(keep, keptPassive, truckCar != null && truckCar.mount && !truckCarried ? truckCar.mount.Level : 0);
        Time.timeScale = 1f;
        SceneManager.LoadScene("historiaDialogos");
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Back to the title screen, dropping the run.
    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
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

    // Dresses the option just added: a free pick, something with a price, or a way out of the menu.
    void Tag(string badge, int tone)
    {
        Option o = options[^1];
        o.badge = badge;
        o.tone = tone;
        options[^1] = o;
    }
    void Free() => Tag(L10n.T("badge.free"), 1);
    void Priced(int price, bool sold = false) => Tag(sold ? L10n.T("badge.sold") : price.ToString(), sold || money < price ? 3 : 2);
    Material GhostMat => ghostMat ? ghostMat : ghostMat = Mat(new Color(1f, 0.12f, 0.12f));

    // The train's copy of a weapon that has most to gain from another: the lowest level one. null: it has none.
    Turret Owned(Weapon weapon)
    {
        Turret found = null;
        foreach (Car car in cars)
            if (car.cargo < 0 && car.mount && car.mount.Weapon == weapon && (found == null || car.mount.Level < found.Level)) found = car.mount;
        return found;
    }

    // The next wagon that can carry a weapon, going that way along the train from this one. -1: none.
    int ArmedFrom(int from, int by)
    {
        for (int i = from + by; i >= 0 && i < cars.Count; i += by)
            if (cars[i].cargo < 0) return i;
        return -1;
    }

    // A weapon was picked: a red copy of it goes on the train, on the wagon where it does most: the one that
    // has it already and can still level up, else one with no weapon, else the first.
    void BeginMounting(Weapon weapon)
    {
        pending = weapon;
        Turret owned = Owned(weapon);
        weaponAt = owned && owned.Level < Weapon.MaxLevel ? cars.FindIndex(c => c.mount == owned) : cars.FindIndex(c => c.cargo < 0 && !c.mount);
        if (weaponAt < 0) weaponAt = cars.FindIndex(c => c.cargo < 0);
        weaponGhost = Instantiate(turretPrefab, transform);
        weaponGhost.Init(weapon);
        weaponGhost.enabled = false; // for show: it does not fire
        foreach (Renderer part in weaponGhost.GetComponentsInChildren<Renderer>())
            part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => GhostMat);
        focus = 0;
        ShowWeaponGhost();
    }

    // The red weapon stands where the wagon's own would (that one steps aside while it does), and the wagon
    // under it turns red too, so it is plain which one is being chosen. Every other wagon has its own colours.
    void ShowWeaponGhost()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            bool chosen = weaponGhost && i == weaponAt;
            if (cars[i].mount) cars[i].mount.gameObject.SetActive(!chosen);
            if (cars[i].cargo >= 0) continue;
            SetAlive(cars[i], cars[i].Alive);
            if (chosen)
                foreach (MeshRenderer part in cars[i].body.GetComponentsInChildren<MeshRenderer>())
                    part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => GhostMat);
        }
        if (weaponGhost) weaponGhost.transform.position = cars[weaponAt].pos + Vector3.up * (0.45f + wagonSize.y);
    }

    void MoveWeapon(int by)
    {
        int to = ArmedFrom(weaponAt, by);
        if (to >= 0) weaponAt = to;
        ShowWeaponGhost();
    }

    void MountGhost()
    {
        Weapon weapon = pending;
        int wagon = weaponAt;
        CancelWeapon();
        Mount(wagon, weapon);
        Took("wagon:" + weapon.id);
        Combat.Instance.Burst("levelup", cars[wagon].pos, 1.6f);
        Next();
    }

    void CancelWeapon()
    {
        if (weaponGhost) Destroy(weaponGhost.gameObject);
        weaponGhost = null;
        pending = null;
        ShowWeaponGhost();
    }

    // Buying cargo: the wagon goes into the train at once, at the tail and in red, to be slid into place.
    void BeginPlacing(int cargo)
    {
        pendingCargo = cargo;
        ghostAt = cars.Count;
        AddCar(ghostAt, cargo);
        ghost = cars[ghostAt];
        foreach (MeshRenderer part in ghost.body.GetComponentsInChildren<MeshRenderer>())
            part.sharedMaterials = System.Array.ConvertAll(part.sharedMaterials, _ => GhostMat);
        focus = 0;
    }

    void MoveGhost(int by)
    {
        int to = Mathf.Clamp(ghostAt + by, 1, cars.Count - 1);
        if (to == ghostAt) return;
        cars.RemoveAt(ghostAt);
        cars.Insert(ghostAt = to, ghost);
        Layout();
    }

    // The cargo step's Auto: as many wagons as the money buys, up to three (and the train's room), the most
    // valuable that still leaves enough for the others: arms first, then rock, then wood; never fuel.
    void AutoCargo()
    {
        int[] prefer = { 2, 1, 0 }; // arms, rock, wood
        int cheapest = CargoPrice(0), bought = 0;
        for (int room = Mathf.Min(3, maxCargo - CargoCount); room > 0; room--)
        {
            int pick = System.Array.FindIndex(prefer, c => money - CargoPrice(c) >= cheapest * (room - 1));
            if (pick < 0) pick = System.Array.FindIndex(prefer, c => money >= CargoPrice(c)); // fewer, then
            if (pick < 0) break;
            pick = prefer[pick];
            money -= CargoPrice(pick);
            int at = Random.Range(1, cars.Count + 1); // anywhere behind the first wagon, as a hand-placed one can go
            AddCar(at, pick);
            cargoLoads[pick]++;
            Combat.Instance.Burst("levelup", cars[at].pos, 1.6f);
            bought++;
        }
        if (bought == 0) Sound.Play("menuback");
    }

    void PlaceGhost()
    {
        money -= CargoPrice(pendingCargo);
        cargoLoads[pendingCargo]++;
        SetAlive(ghost, true); // its own colours back
        Combat.Instance.Burst("levelup", ghost.pos, 1.6f);
        ghost = null;
        pendingCargo = -1; // back to the market: there may be more to buy
    }

    void CancelGhost()
    {
        if (ghost == null) return;
        cars.Remove(ghost);
        Destroy(ghost.body.gameObject);
        ghost = null;
        pendingCargo = -1;
        Layout();
    }

    void Minor()
    {
        Option o = options[^1];
        o.minor = true;
        options[^1] = o;
    }

    // A volume in the sound menu: left / right move it a tenth, confirming it (or a click) raises it and wraps round.
    void AddVolume(string icon, string key, float value, int which) =>
        options.Add(new Option { icon = icon, label = L10n.T(key, Mathf.RoundToInt(value * 100f)), enabled = true,
            run = () => Sound.Step(which, 1), adjust = steps => Sound.Step(which, steps) });

    // Rebuilt every frame the menu is open; the state decides which menu it is.
    void BuildMenu()
    {
        options.Clear();
        menuStep = -1;
        trainSlots.Clear();
        menuSub = null;
        menuMoney = false;
        menuLines = null;
        menuCompact = false;
        int kind;
        if (Paused && confirming != 0)
        {
            // "No" comes first, so it is the one focused when the question opens.
            kind = 9;
            menuTitle = L10n.T(confirming == 1 ? "pause.restart.ask" : "pause.menu.ask");
            Add("no", L10n.T("menu.no"), () => confirming = 0);
            if (confirming == 1) Add("yes", L10n.T("menu.yes"), Restart);
            else Add("yes", L10n.T("menu.yes"), GoToMainMenu);
        }
        else if (junction)
        {
            kind = 11;
            menuTitle = L10n.T("junction.title");
            menuSub = L10n.T("junction.sub", Player.UsingGamepad ? (Gamepad.current is DualShockGamepad ? "✕" : "A") : L10n.T("junction.confirm"),
                             Player.UsingGamepad ? (Gamepad.current is DualShockGamepad ? "□" : "X") : "Q");
            Add("truck", L10n.T("junction.fight"), () => Junction(true));
            Add("lever", L10n.T("junction.skip"), () => Junction(false));
        }
        else if (Paused && evolutionsMenu)
        {
            // The list itself is drawn by the HUD over everything (Hud.Evolutions); here only the way back.
            kind = 10;
            menuTitle = L10n.T("pause.evolutions");
            Add("back", L10n.T("menu.back"), () => evolutionsMenu = false);
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
            Add("evolutions", L10n.T("pause.evolutions"), () => evolutionsMenu = true);
            Add("language", L10n.T("language"), L10n.NextLocale);
            Add("restart", L10n.T("pause.restart"), () => confirming = 1);
            Add("quit", L10n.T("pause.menu"), () => confirming = 2);
        }
        else if (chapterWon)
        {
            // Three of the upgrades taken in this chapter, at random: the one picked goes on to the next chapters.
            kind = 8;
            menuTitle = L10n.T("story.won");
            if (keptPassive == null && passiveOrder.Count > 0)
            {
                string id = passiveOrder[Random.Range(0, passiveOrder.Count)];
                keptPassive = id + "*" + PassiveLevel(id);
            }
            menuSub = L10n.T("story.keep") + (keptPassive == null ? "" : "     " + L10n.T("keep.passive", L10n.T("passive." + keptPassive.Split('*')[0]), keptPassive.Split('*')[1]));
            var pool = new List<string>(new HashSet<string>(taken));
            while (pool.Count > 3) pool.RemoveAt(Random.Range(0, pool.Count));
            if (keepOffer == null) keepOffer = pool;
            foreach (string token in keepOffer)
            {
                string label = Story.Label(token, out string icon);
                Add(icon, label, () => EndChapter(token));
                Free();
            }
            if (keepOffer.Count == 0) Add("yes", L10n.T("story.next"), () => EndChapter(null));
        }
        else if (Health <= 0f)
        {
            if (Story.Active) Story.Lose(); // saved: this chapter again, from its start
            kind = 0;
            menuTitle = L10n.T("menu.gameover");
            Add("yes", L10n.T("menu.yes"), Restart);
            Add("no", L10n.T("menu.no"), GoToMainMenu);
        }
        else if (stage >= 4)
        {
            kind = 1;
            menuStep = 4;
            menuTitle = L10n.T("menu.route");
            menuSub = L10n.T("sub.route", LegName(nextLeg));
            Add("stay", L10n.T("menu.stay", BiomeName(Biome)), () => Depart());
            if (Biome < Biomes.Length - 1 && !Story.Active) // a story chapter stays in its biome
            {
                // The desert's boss stands in the way: no leaving until it is down (and then it is gone for good).
                // The other biomes have no boss yet, so their route out is always open.
                bool locked = Biome == 0 && bossBeaten != Biome;
                Add("biome", L10n.T(locked ? "menu.change.locked" : "menu.change", BiomeName(Biome + 1), biomeDifficulty, Biome + 2),
                    () => Depart(true), !locked);
                if (locked) menuSub = L10n.T("sub.route.locked", BiomeName(Biome + 1));
            }
        }
        else if (stage == 2 && pendingCargo < 0)
        {
            // CARGO to carry, a step of its own: paid now, sold at the next station if it survives; up to
            // maxCargo wagons. Then, as the next step, the shop.
            kind = 5;
            menuStep = 2;
            menuMoney = true;
            menuTitle = L10n.T("menu.cargo", CargoCount, maxCargo);
            menuSub = L10n.T("sub.cargo");
            int loaded = CargoCount;
            for (int i = 0; i < Loadable; i++)
            {
                int cargo = i;
                Add(Cargos[i].id, L10n.T("cargo.card", L10n.T("player.slot", L10n.T("cargo." + Cargos[i].id), CargoLevel(i)), CargoReward(i), Cargos[i].health, Cargos[i].penalty * (Biome + 1), CargoPrice(i)),
                    () => BeginPlacing(cargo), loaded < maxCargo && money >= CargoPrice(i));
                Priced(CargoPrice(i), loaded >= maxCargo);
            }
            Add("auto", L10n.T("cargo.auto"), AutoCargo, loaded < maxCargo && money >= CargoPrice(0));
            Add("yes", L10n.T("cargo.done"), Next);
            Minor();
        }
        else if (stage == 3)
        {
            // The SHOP: health now, medkits, a longer train.
            {
                kind = 7;
                menuStep = 3;
                menuMoney = true;
                menuTitle = L10n.T("menu.shop");
                menuSub = L10n.T("sub.shop");
                Add("repair", L10n.T("menu.repair", repairReward, RepairPrice), () => { money -= RepairPrice; repairsBought++; Repair(); },
                    money >= RepairPrice && Health < MaxHealth);
                Priced(RepairPrice);
                Add("heal", L10n.T("menu.heal", healReward, healCost), () => { money -= healCost; Player.Heal(healReward); },
                    money >= healCost && Player.Health < Player.maxHealth);
                Priced(healCost);
                if (KitsInUse)
                {
                    Add("medkit", L10n.T("shop.kit", kitHeal, kitCost, kits, maxKits), () => { money -= kitCost; kits++; }, money >= kitCost && kits < maxKits);
                    Priced(kitCost, kits >= maxKits);
                }
                Add("wagon", WagonForSale ? L10n.T("shop.wagon", WagonPrice) : L10n.T("shop.wagon.limit"),
                    () => { money -= WagonPrice; wagonsBought++; AddCar(cars.Count, -1); }, WagonForSale && money >= WagonPrice);
                Priced(WagonPrice, !WagonForSale);
                Add("container", ContainerForSale ? L10n.T("shop.container", ContainerPrice) : L10n.T("shop.container.limit"),
                    () => { money -= ContainerPrice; containersBought++; AddCar(cars.Count, Container, true); }, ContainerForSale && money >= ContainerPrice);
                Priced(ContainerPrice, !ContainerForSale);
            }
            Add("yes", L10n.T("shop.done"), Next);
            Minor();
        }
        else if (stage == 2)
        {
            // Where to couple it: the wagon is already in the train, in red, and left / right move it along
            // (never ahead of the first wagon). Confirming couples it there and paints it; back takes it out.
            kind = 6;
            menuStep = 2;
            menuCompact = true;
            menuTitle = L10n.T("cargo.place", L10n.T("cargo." + Cargos[pendingCargo].id));
            menuSub = L10n.T(Player.UsingGamepad ? "sub.place.gamepad" : "sub.place.keyboard");
            DrawTrain(ghostAt, Cargos[pendingCargo].id, L10n.T("cargo." + Cargos[pendingCargo].id), false);
            Add("yes", L10n.T("place.here"), PlaceGhost);
            Minor();
            Add("back", L10n.T("menu.back"), CancelGhost);
            Minor();
        }
        else if (stage == 1)
        {
            kind = 4;
            menuStep = 1;
            menuTitle = L10n.T("menu.hero");
            menuSub = L10n.T("sub.hero");
            foreach (int offer in heroOffers)
            {
                int stat = offer - Weapon.Hero.Length;
                if (stat == HeroMoney) { Add("money", L10n.T("menu.money", moneyReward), () => { money += moneyReward; Next(); }); Free(); }
                else if (stat == HeroHealth)
                {
                    float mend = skipHealth * 2.5f;
                    Add("heal", L10n.T("menu.repair", Mathf.RoundToInt(mend)), () => { Repair(mend); Player.Heal(mend); Next(); });
                    Free();
                }
                else if (stat == 5) { Add(Player.ammo.id, L10n.T("hero.evolve", Player.ammo.Name, Player.level + 1), () => { Player.level++; Took("evolve"); Combat.Instance.Burst("levelup", Player.Position + Vector3.down * 2f, 1.5f); Next(); }); Free(); }
                else
                {
                    string id = Passives[stat < 5 ? stat : stat - 1];
                    string label = stat switch
                    {
                        0 => L10n.T("hero.speed"), 1 => L10n.T("hero.armor"), 2 => L10n.T("hero.vitality", vitalityReward), 3 => L10n.T("hero.damage"),
                        4 => L10n.T("hero.greed"), 6 => L10n.T("hero.fireRate", Mathf.RoundToInt(fireRate * 100f)),
                        7 => L10n.T("hero.criticalChance", Mathf.RoundToInt(criticalChance * 100f)), 8 => L10n.T("hero.criticalDamage", Mathf.RoundToInt(criticalMultiplier * 100f)),
                        _ => L10n.T("hero." + id),
                    };
                    Add(PassiveIcons[System.Array.IndexOf(Passives, id)], label, () => { Apply(id); Took(id); Next(); });
                    Tag(L10n.T("badge.upgrade", PassiveLevel(id) + 1), 1);
                }
            }
            Add("skip", L10n.T("menu.skip"), Skip);
            Minor();
        }
        else if (pending == null)
        {
            kind = 2;
            menuStep = 0;
            menuTitle = L10n.T("menu.upgrade");
            menuSub = cargoEarned > 0 || cargoFined > 0 ? L10n.T("cargo.result", cargoEarned, cargoFined) + "     " + L10n.T("sub.train") : L10n.T("sub.train");
            foreach (int offer in offers)
            {
                if (offer < Weapon.Wagon.Length)
                {
                    // A wagon's weapon stays for good: a card raises a copy the train has, or goes on a wagon with
                    // no weapon (another copy too, levelled on its own). The tag says which; with neither, it is shut.
                    Weapon weapon = Weapon.Wagon[offer];
                    Turret owned = Owned(weapon);
                    bool raise = owned && owned.Level < Weapon.MaxLevel, free = cars.Exists(c => c.cargo < 0 && !c.mount);
                    Add(weapon.id, weapon.Name + "\n" + weapon.Info, () => BeginMounting(weapon), raise || free);
                    if (raise) Tag(L10n.T("badge.upgrade", owned.Level + 1), 2);
                    else if (free) Tag(L10n.T(owned == null ? "badge.new" : "badge.copy"), 1);
                    else Tag(L10n.T(owned == null ? "badge.nowagon" : "badge.maxed"), 3);
                }
                else if (offer == Weapon.Wagon.Length) { Add("money", L10n.T("menu.money", moneyReward), () => { money += moneyReward; Next(); }); Free(); }
                else if (offer == Weapon.Wagon.Length + 1)
                {
                    float mend = skipHealth * 2.5f;
                    Add("repair", L10n.T("menu.repair", Mathf.RoundToInt(mend)), () => { Repair(mend); Player.Heal(mend); Next(); });
                    Free();
                }
                else if (offer == Eternal && Eternals < MaxEternal)
                {
                    Add("container", L10n.T("offer.eternal"), () => { AddCar(cars.Count, Container, true); Next(); });
                    Tag(L10n.T("badge.rare"), 2);
                }
                else if (offer < Escort)
                {
                    int cargo = offer - FreeCargo;
                    Add(Cargos[cargo].id, L10n.T("offer.cargo", L10n.T("cargo." + Cargos[cargo].id), CargoReward(cargo), Cargos[cargo].penalty * (Biome + 1)),
                        () => { AddTemporary(cargo, null, 0); Next(); });
                    Tag(L10n.T("badge.bonus"), 2);
                }
                else
                {
                    Weapon weapon = Weapon.Wagon[offer - Escort];
                    int level = WeakestLevel;
                    Add(weapon.id, L10n.T("offer.escort", weapon.Name, level), () => { AddTemporary(-1, weapon, level); Next(); }); Tag(L10n.T("badge.bonus"), 2);
                }
            }
            Add("skip", L10n.T("menu.skip"), Skip);
            Minor();
        }
        else
        {
            // Which wagon gets the weapon: it sits on the train already, in red, on the wagon it would go to.
            // Left / right move it from wagon to wagon; the line under the title says what it would do there.
            kind = 3;
            menuStep = 0;
            menuCompact = true;
            Turret mount = cars[weaponAt].mount;
            bool same = mount && mount.Weapon == pending, can = !mount || (same && mount.Level < Weapon.MaxLevel); // a weapon is never swapped
            string effect = !mount ? L10n.T("menu.empty") : !same ? L10n.T("menu.locked", mount.Weapon.Name, mount.Level)
                : mount.Level < Weapon.MaxLevel ? L10n.T("menu.levelup", mount.Level + 1) : L10n.T("menu.maxlevel");
            menuTitle = L10n.T("menu.place", pending.Name);
            menuSub = L10n.T("mount.where", weaponAt + 1, effect) + "     " + L10n.T(Player.UsingGamepad ? "sub.mount.gamepad" : "sub.mount.keyboard");
            DrawTrain(weaponAt, pending.id, effect, true);
            Add("yes", L10n.T("mount.here"), MountGhost, can);
            Minor();
            Add("back", L10n.T("menu.back"), CancelWeapon);
            Minor();
        }

        // What skipping the last step gave, said once at the head of the next one.
        if (skipGift != 0 && menuSub != null && AtStation)
            menuSub = L10n.T(skipGift == 1 ? "menu.skip.money" : "menu.skip.health", skipGift == 1 ? skipMoney : skipHealth) + "     " + menuSub;

        if (kind != menuKind)
        {
            menuKind = kind;
            focus = 0;
        }
        focus = Mathf.Min(focus, options.Count - 1);
        for (int i = 0; i < options.Count && !options[focus].enabled; i++) focus = (focus + 1) % options.Count;
    }

    // Left/right moves the focus (stick, d-pad, A/D, arrows or Q/E); Jump (cross / A button, Space) confirms.
    void ActivateOption(int index)
    {
        if (index < 0 || index >= options.Count || !options[index].enabled) return;
        Sound.Play(options[index].icon == "back" || options[index].icon == "no" ? "menuback" : "confirm");
        options[index].run();
    }

    void Navigate()
    {
        if ((Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame))
        {
            // Cancel the current submenu or placement before leaving its parent menu.
            int back = options.FindIndex(option => option.enabled && (option.icon == "back" || option.icon == "no"));
            if (back >= 0) ActivateOption(back);
            else if (Paused) { Sound.Play("menuback"); SetPaused(false); }
            return;
        }
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
            if (side != 0 && (tapped || !sideHeld)) { options[focus].adjust(side); Sound.Play("confirm"); }
            sideHeld = side != 0 && !tapped;
            if (tapped || side != 0) step = 0;
            tapped = false;
        }
        if ((ghost != null || weaponGhost) && !Paused)
        {
            // Placing a wagon or a weapon: left and right move it along the row of tiles the menu draws, engine
            // on the left, whichever way the train itself runs on the screen. Nothing moves onto the buttons:
            // the confirm button places it, the back button (circle / B, Backspace) takes it away.
            int slide = tapped ? step : Mathf.Abs(move.x) > 0.5f ? (int)Mathf.Sign(move.x) : 0;
            if (slide != 0 && (tapped || !navHeld))
            {
                if (ghost != null) MoveGhost(slide); else MoveWeapon(slide); // right: towards the tail
                Sound.Play("click");
            }
            navHeld = slide != 0 && !tapped;
            focus = 0;
            if (confirm.WasPressedThisFrame()) ActivateOption(focus);
            return;
        }
        if (step != 0 && (tapped || !navHeld))
        {
            do focus = (focus + step + options.Count) % options.Count;
            while (!options[focus].enabled);
            Sound.Play("click");
        }
        navHeld = step != 0 && !tapped;
        if (confirm.WasPressedThisFrame()) ActivateOption(focus);
    }

    // Yellow frame and arrow in the world on whatever the focused option refers to.
    void LateUpdate()
    {
        // Warning for the curve, jump or zigzag ahead: what is coming and which button it takes.
        if (!Paused && swarm.Incoming) hud.Warning(L10n.T("swarm.title"), L10n.T("swarm.warn", JumpEvent.ButtonName(Player.UsingGamepad)));
        if (!Paused && brawl.Alarm) hud.Warning(L10n.T("brawl.title"), L10n.T("brawl.warn"));
        else if (!Paused && boss.Incoming) hud.Warning(L10n.T("boss.title"), L10n.T("boss.warn", boss.Name));
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
    public GameObject DressTrack(GameObject piece) => Dress(piece); // for the rival's track
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
        float share = stretch[prop.size] * (Biomes[Biome].id == "ocean" ? 0.4f : 1f); // open water: most of it stays empty
        if (kinds.Length == 0 || Random.value > share) return; // this stretch leaves the slot empty
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
        if (!toon) toon = Resources.Load<Material>("Toon");
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

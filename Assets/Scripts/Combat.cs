using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

// Enemies and bullets are plain data moved from this one Update and drawn with GPU instancing:
// no GameObjects, no colliders, no physics, and nothing is allocated after warm-up.
[RequireComponent(typeof(TrainSim))]
public class Combat : MonoBehaviour
{
    // One instanced draw call per 1023 cubes that share a material.
    public class Batch
    {
        static Mesh cube;
        readonly Matrix4x4[] matrices = new Matrix4x4[1023];
        readonly Color color;
        readonly bool shadows;
        readonly Mesh mesh; // null: a cube
        readonly bool round; // ground marks: a flat disc instead of a cube
        readonly float glow; // > 1: the colour is pushed past white this many times so the bloom lights it up, no outline
        readonly bool plain; // a model in its batch's colour alone, its vertex colours ignored (the hit flash)
        Material material;
        int count;

        // mesh: a model painted with vertex colours (Tools/make_enemies.py); the batch's colour tints it.
        public Batch(Color color, bool shadows, Mesh mesh = null, bool round = false, float glow = 1f, bool plain = false)
        {
            this.plain = plain;
            this.glow = glow;
            this.mesh = mesh;
            this.round = round;
            this.color = color;
            this.shadows = shadows;
        }

        public void Add(Vector3 position, Vector3 size) => Add(position, size, Quaternion.identity);

        public void Add(Vector3 position, Vector3 size, Quaternion rotation)
        {
            if (count == matrices.Length) Flush();
            matrices[count++] = Matrix4x4.TRS(position, rotation, size);
        }

        public void Flush()
        {
            if (count == 0) return;
            if (!cube) cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Mesh shape = mesh ? mesh : round ? Disc() : cube;
            if (!material)
            {
                material = glow > 1f ? TrainSim.Mat(new Color(color.r * glow, color.g * glow, color.b * glow), false) : TrainSim.Mat(color);
                if (shape != cube && !plain) material.SetFloat("_MVCOL", 1f); // RealToon: multiply by the vertex colours
            }
            var parameters = new RenderParams(material)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 500f),
                shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = shadows
            };
            Graphics.RenderMeshInstanced(parameters, shape, 0, matrices, count);
            count = 0;
        }
    }

    // ponytail: enemy stats live in code, same as Weapon. Behaviours are opt-in fields, 0 = off.
    public class EnemyType
    {
        public string name;
        public Color color;
        public float health, speed, damage, size;
        public int reward = 1;   // money per kill
        public bool Fodder => fodder || health <= 6f; // the rank and file: they come in crowds, each one weak (Combat.Crowd)
        public bool fodder;                             // set once its numbers were cut down for that
        public int biome = -1;   // only spawns in this biome; -1 = everywhere
        public int firstLevel;   // difficulty at which this type starts to appear
        public float weight;     // share of the spawns once it appears; 0 = never spawned directly
        public int loot = -1;    // floating goods: index of the cargo wagon the hero wins by destroying it

        public float explode, explodeDamage;    // on death: hurts enemies and wagons within this radius
        public float chargeRange, chargeSpeed;  // speed multiplier once this close to the train
        public float regen;                     // health per second
        public int pack = 1;                    // spawns in groups
        public int split;                       // larvae released when killed
        public float armor;                     // removed from every hit (a hit always keeps 20%)
        public bool slowImmune;
        public bool heroOnly;                   // only the hero's shots hurt it, and the turrets never aim at it (the truck's bombs)
        public bool projectile;                 // a boss's own piece (the rig, its rockets): Combat never moves or
                                                // draws it, but the hero and the turrets can still lock and shoot it
        public float stun;                      // seconds the wagon's weapon stops after contact
        // One timed ability per type, fired every `interval` seconds:
        public float interval;
        public float ranged;                    // stops this far from the train and shoots it
        public float blink;                     // teleports this far towards the train
        public float heal, healRadius;          // health per second given to enemies around it
        public bool spawner;                    // releases a larva

        public Mesh customMesh;                  // override the mesh used to draw this enemy type (projectiles, etc.)
        public string model;                     // a model of its own in its own colours (Models/<model>): drawn untinted
        public bool flies;                       // it hovers (else it walks on the ground); a balloon that flies also spins
        public Color tint = Color.white;         // over a model's own colours: the big, giant and elite variants
        public int tier;                         // 0 the type itself, 1 big, 2 giant, 3 elite (Combat.Variant)

        Batch batch;
        // Enemies are robots with a model each, loot is a balloon carrying its cargo (Tools/make_mechs.py, Tools/make_cargo.py).
        public Batch Batch => batch ??= new Batch(model != null ? tint : color, true, Shape);
        Mesh Shape => customMesh ? customMesh : Resources.Load<Mesh>(model != null ? "Models/" + model : "Models/Balloon_" + loot);
        public bool Spins => model == null && flies; // a model of its own faces its way
        public EnemyType Copy() { var c = (EnemyType)MemberwiseClone(); c.batch = null; return c; } // its own batch: its own tint
    }

    // As in Vampire Survivors, a run shows new faces as it goes: bigger, tougher versions of each enemy in colours of
    // their own come in more and more often (big: blue, giant: crimson, elite: gold), and each wave is led by a few
    // types of its own instead of the whole bestiary at once.
    static readonly (float size, float health, float damage, int reward, Color tint)[] Tiers =
    {
        (1f, 1f, 1f, 1, Color.white),
        (1.3f, 2.5f, 1.2f, 2, new Color(0.45f, 0.8f, 1.9f)),
        (1.7f, 4.5f, 1.5f, 4, new Color(1.9f, 0.3f, 0.35f)),
        (2.1f, 9f, 2f, 8, new Color(2.2f, 1.6f, 0.25f)),
    };
    readonly Dictionary<(EnemyType, int), EnemyType> variants = new();
    EnemyType Variant(EnemyType type, int tier)
    {
        if (tier <= 0 || type.model == null) return type;
        if (!variants.TryGetValue((type, tier), out EnemyType v))
        {
            var t = Tiers[tier];
            v = type.Copy();
            v.tier = tier;
            v.size *= t.size;
            v.health *= t.health;
            v.damage *= t.damage;
            v.explodeDamage *= t.damage;
            v.reward = Mathf.Max(1, type.reward) * t.reward;
            v.tint = t.tint;
            v.fodder = false;
            variants[(type, tier)] = v;
        }
        return v;
    }
    // Which version comes now: big ones from early on, giants past the middle of a run, a rare elite late.
    int RollTier()
    {
        float roll = Random.value;
        float elite = Mathf.Clamp((level - 18) * 0.004f, 0f, 0.05f), giant = Mathf.Clamp((level - 7) * 0.01f, 0f, 0.14f), big = Mathf.Clamp(level * 0.018f, 0f, 0.32f);
        return roll < elite ? 3 : roll < elite + giant ? 2 : roll < elite + giant + big ? 1 : 0;
    }
    // The wave's own: a few types picked every rosterEvery seconds, newly unlocked ones first.
    readonly HashSet<EnemyType> roster = new();
    float rosterTimer;
    int rosterLevel = -1;
    [System.NonSerialized] public float rosterEvery = 50f;
    void NewRoster()
    {
        roster.Clear();
        int biome = train.Biome;
        var open = new List<EnemyType>(System.Array.FindAll(Types, t => t.weight > 0f && t.firstLevel <= level && Here(t, biome)));
        if (open.Count == 0) return;
        open.Sort((a, b) => b.firstLevel.CompareTo(a.firstLevel)); // the newest first, then at random
        if (open[0].firstLevel > 0 && open[0].firstLevel >= level - 3) { roster.Add(open[0]); open.RemoveAt(0); }
        while (roster.Count < 3 && open.Count > 0) { int k = Random.Range(0, open.Count); roster.Add(open[k]); open.RemoveAt(k); }
        roster.Add(RankAndFile); // the rank and file always come too: waves without them hit far harder early on
    }

    static readonly EnemyType Larva = new EnemyType { name = "Larva", color = new Color(0.7f, 0.9f, 0.3f), health = 1.5f, speed = 7f, damage = 2f, size = 0.5f, model = "Enemy_larva" };

    // A mini boss, like Vampire Survivors' elites: a big armoured walker that comes every so often (miniBossEvery
    // seconds of riding). It does not break on the train: it rams one wagon, backs off, then goes for another (or
    // now and then for the hero), so there is time to shoot it. The hero can taunt it (MiniBossMove) to draw it away
    // from the wagons. Shot down, it drops a chest (TrainSim.OpenChest).
    static readonly EnemyType MiniBoss = new EnemyType
    {
        name = "MiniBoss", color = new Color(1f, 0.72f, 0.15f), health = 40f, speed = 2.6f, damage = 8f, size = 2.8f,
        reward = 40, armor = 1f, slowImmune = true,
        model = "Enemy_miniboss", // Tools/make_mechs.py
    };
    // The other look a mini boss comes in, half the time: the winged demon (Tools/make_demon.py), hovering. The same
    // fight in every other way.
    static readonly EnemyType MiniDemon = new EnemyType
    {
        name = "MiniBoss", color = new Color(1f, 0.72f, 0.15f), health = 40f, speed = 2.6f, damage = 8f, size = 2.8f,
        reward = 40, armor = 1f, slowImmune = true,
        model = "Enemy_demon", flies = true,
    };
    static bool IsMiniBoss(EnemyType t) => t == MiniBoss || t == MiniDemon;
    [System.NonSerialized] public float miniBossEvery = 70f;
    float miniBossTimer = 170f; // the first comes near three minutes in, once the train has a few upgrades to meet it with
    Enemy miniBoss;
    int miniBossId;
    float miniBossLeft = 1f; // share of its health a withdrawn mini boss comes back with
    EnemyType miniBossLook = MiniBoss;
    const int Ram = 0, BackOff = 1, Chase = 2;
    int bossMode, bossWagon;
    float bossModeLeft;
    public int MiniBossesBeaten { get; private set; }

    // firstLevel goes by how dangerous a type is, never by its biome: a story may open in any biome, so each one's
    // bestiary has to start gentle (the rank and file) and grow, like the desert's.
    public static readonly EnemyType[] Types =
    {
        // Everywhere
        new EnemyType { name = "Normal", color = new Color(0.2f, 0.6f, 0.2f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f, model = "Enemy_gunship", flies = true }, // Tools/make_gunship.py
        new EnemyType { name = "Rápido", color = new Color(1f, 0.8f, 0.1f), health = 2f, speed = 8f, damage = 3f, size = 0.7f, firstLevel = 2, weight = 0.5f, model = "Enemy_fast", flies = true },
        new EnemyType { name = "Tanque", color = new Color(0.5f, 0.1f, 0.6f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 4, weight = 0.2f, model = "Enemy_brute" }, // Tools/make_brute.py
        // Desierto
        new EnemyType { name = "Bomba", biome = 0, color = new Color(1f, 0.3f, 0.1f), health = 4f, speed = 5f, damage = 4f, size = 0.9f, firstLevel = 1, weight = 0.3f, explode = 3.5f, explodeDamage = 4.5f, model = "Enemy_bomb" },
        new EnemyType { name = "Embestidor", biome = 0, color = new Color(0.45f, 0.3f, 0.15f), health = 6f, speed = 3f, damage = 8f, size = 1.1f, firstLevel = 3, weight = 0.25f, chargeRange = 14f, chargeSpeed = 4f, model = "Enemy_charger" },
        // Nieve
        new EnemyType { name = "Yeti", biome = 1, color = new Color(0.8f, 0.9f, 1f), health = 18f, speed = 3f, damage = 10f, size = 1.5f, reward = 3, firstLevel = 5, weight = 0.2f, regen = 3f, model = "Enemy_yeti" },
        new EnemyType { name = "Lobo", biome = 1, color = new Color(0.45f, 0.5f, 0.55f), health = 1.5f, speed = 9f, damage = 2f, size = 0.6f, firstLevel = 2, weight = 0.2f, pack = 4, model = "Enemy_wolf", flies = true },
        // Jungla: a bestiary of its own too (OwnBestiary), bright poison-frog and beetle colours on the green (Tools/make_mechs.py)
        new EnemyType { name = "Libélula", biome = 2, color = new Color(1f, 0.45f, 0.12f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f, model = "Enemy_dragonfly", flies = true },
        new EnemyType { name = "Avispa", biome = 2, color = new Color(1f, 0.85f, 0.12f), health = 2f, speed = 8.5f, damage = 3f, size = 0.7f, firstLevel = 2, weight = 0.5f, pack = 3, model = "Enemy_wasp", flies = true },
        new EnemyType { name = "Gorila", biome = 2, color = new Color(0.42f, 0.24f, 0.14f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 4, weight = 0.2f, model = "Enemy_ape" },
        new EnemyType { name = "Escarabajo", biome = 2, color = new Color(0.2f, 0.35f, 0.95f), health = 18f, speed = 2.5f, damage = 3.5f, size = 1.5f, reward = 4, firstLevel = 7, weight = 0.1f, ranged = 14f, interval = 3.5f, armor = 0.6f, model = "Enemy_beetle" },
        new EnemyType { name = "Nido", biome = 2, color = new Color(0.1f, 0.3f, 0.1f), health = 10f, speed = 3f, damage = 6f, size = 1.4f, reward = 2, firstLevel = 4, weight = 0.25f, split = 3, model = "Enemy_nest" },
        new EnemyType { name = "Escupidor", biome = 2, color = new Color(0.6f, 1f, 0.2f), health = 4f, speed = 4f, damage = 3f, size = 0.9f, firstLevel = 6, weight = 0.25f, ranged = 12f, interval = 1.5f, model = "Enemy_spitter" },
        // Volcán
        new EnemyType { name = "Pez volador", biome = 3, color = new Color(0.2f, 0.85f, 0.95f), health = 2f, speed = 9f, damage = 2f, size = 0.6f, firstLevel = 2, weight = 0.3f, pack = 3, model = "Enemy_fish", flies = true },
        new EnemyType { name = "Boya mina", biome = 3, color = new Color(0.95f, 0.3f, 0.2f), health = 7f, speed = 3f, damage = 5f, size = 1.1f, firstLevel = 3, weight = 0.22f, explode = 3f, explodeDamage = 7f, model = "Enemy_buoy" },

        new EnemyType { name = "Gólem", biome = 4, color = new Color(0.35f, 0.05f, 0.02f), health = 25f, speed = 2f, damage = 15f, size = 1.8f, reward = 4, firstLevel = 6, weight = 0.2f, armor = 1f, model = "Enemy_golem" },
        new EnemyType { name = "Diablillo", biome = 4, color = new Color(1f, 0.1f, 0.1f), health = 3f, speed = 9f, damage = 3f, size = 0.6f, firstLevel = 2, weight = 0.35f, explode = 2.5f, explodeDamage = 5f, slowImmune = true, model = "Enemy_imp", flies = true },
        // Ciudad, taken by invaders: a bestiary of its own (OwnBestiary), none of the ones from everywhere (Tools/make_mechs.py, in the invaders' colours)
        new EnemyType { name = "Invasor", biome = 5, color = new Color(0.45f, 0.85f, 0.3f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f, model = "Enemy_invader", flies = true },
        new EnemyType { name = "Rasante", biome = 5, color = new Color(0.55f, 0.25f, 0.85f), health = 2f, speed = 8.5f, damage = 3f, size = 0.7f, firstLevel = 3, weight = 0.5f, model = "Enemy_skimmer", flies = true },
        new EnemyType { name = "Enjambre", biome = 5, color = new Color(0.45f, 0.85f, 0.3f), health = 3f, speed = 8f, damage = 3f, size = 0.7f, firstLevel = 4, weight = 0.3f, pack = 3, model = "Enemy_gang" },
        new EnemyType { name = "Aplastador", biome = 5, color = new Color(0.7f, 0.73f, 0.8f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 5, weight = 0.2f, model = "Enemy_crusher" },
        new EnemyType { name = "Acorazado", biome = 5, color = new Color(0.72f, 0.75f, 0.82f), health = 18f, speed = 2.5f, damage = 3.5f, size = 1.5f, reward = 4, firstLevel = 8, weight = 0.1f, ranged = 14f, interval = 3.5f, armor = 0.6f, model = "Enemy_armored" },
        // Espacio

        // Espacio: its own too, all bright against the black (Tools/make_mechs.py)
        new EnemyType { name = "Sonda", biome = 6, color = new Color(1f, 0.55f, 0.12f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f, model = "Enemy_probe", flies = true },
        new EnemyType { name = "Cometa", biome = 6, color = new Color(0.3f, 0.95f, 1f), health = 2f, speed = 8.5f, damage = 3f, size = 0.7f, firstLevel = 2, weight = 0.5f, model = "Enemy_comet", flies = true },
        new EnemyType { name = "Titán", biome = 6, color = new Color(1f, 0.3f, 0.75f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 4, weight = 0.2f, model = "Enemy_titan" },
        new EnemyType { name = "Fantasma", biome = 6, color = new Color(0.7f, 1f, 1f), health = 6f, speed = 3f, damage = 8f, size = 1f, firstLevel = 3, weight = 0.3f, blink = 7f, interval = 2f, model = "Enemy_ghost", flies = true },
        new EnemyType { name = "Dron sanador", biome = 6, color = new Color(1f, 0.5f, 0.8f), health = 8f, speed = 3.5f, damage = 2f, size = 0.8f, reward = 2, firstLevel = 5, weight = 0.2f, heal = 4f, healRadius = 7f, interval = 0.5f, model = "Enemy_healer", flies = true },
        // Planeta alienígena
        // Planeta alienígena: "from fluffy things you want to hug to hexagons" (Tools/make_alien_enemies.py)
        new EnemyType { name = "Pelusa", biome = 7, color = new Color(1f, 0.55f, 0.8f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f, model = "Enemy_fluff" },
        new EnemyType { name = "Hexa", biome = 7, color = new Color(0.35f, 1f, 0.85f), health = 2f, speed = 8.5f, damage = 3f, size = 0.7f, firstLevel = 2, weight = 0.5f, model = "Enemy_hexa", flies = true },
        new EnemyType { name = "Hexágono mayor", biome = 7, color = new Color(1f, 0.82f, 0.25f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 4, weight = 0.2f, model = "Enemy_hexgiant" },
        new EnemyType { name = "Colmena", biome = 7, color = new Color(0.4f, 0.5f, 0.1f), health = 30f, speed = 1.5f, damage = 10f, size = 2f, reward = 5, firstLevel = 7, weight = 0.15f, spawner = true, interval = 4f, model = "Enemy_hive" },
        new EnemyType { name = "Saboteador", biome = 7, color = new Color(0f, 0.6f, 0.6f), health = 5f, speed = 7f, damage = 2f, size = 0.8f, firstLevel = 3, weight = 0.3f, stun = 5f, model = "Enemy_saboteur", flies = true },
        // Fortaleza alienígena
        // Base alienígena: its guards (Tools/make_mechs.py)
        new EnemyType { name = "Vigía", biome = 8, color = new Color(0.92f, 0.94f, 0.96f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f, model = "Enemy_warden", flies = true },
        new EnemyType { name = "Cazador", biome = 8, color = new Color(0.5f, 1f, 0.3f), health = 2f, speed = 8.5f, damage = 3f, size = 0.7f, firstLevel = 2, weight = 0.5f, model = "Enemy_hunter", flies = true },
        new EnemyType { name = "Guardián", biome = 8, color = new Color(0.92f, 0.94f, 0.96f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 4, weight = 0.2f, model = "Enemy_guardian" },
        new EnemyType { name = "Centinela", biome = 8, color = new Color(0.5f, 0.55f, 0.6f), health = 15f, speed = 3f, damage = 5f, size = 1.2f, reward = 3, firstLevel = 8, weight = 0.3f, ranged = 16f, interval = 1.2f, armor = 1f, model = "Enemy_sentinel" },
        new EnemyType { name = "Coloso", biome = 8, color = new Color(0.1f, 0.1f, 0.12f), health = 60f, speed = 2f, damage = 25f, size = 2.5f, reward = 8, firstLevel = 10, weight = 0.12f, explode = 6f, explodeDamage = 15f, chargeRange = 10f, chargeSpeed = 2.5f, model = "Enemy_colossus" },
    };

    // A biome with a bestiary of its own gets none of the enemies from everywhere: the city looks nothing like the desert.
    static bool OwnBestiary(int biome) => biome >= 5 || biome == 2; // the jungle, the city and on: their own enemies, not the ones from everywhere
    // The government's security uses the city's human-built drones and machines.
    static bool Here(EnemyType t, int biome) => t.biome == (biome switch {9=>5,10=>4,11=>6,12=>7,13=>7,14=>5,_=>biome}) || (t.biome < 0 && !OwnBestiary(biome));
    // The biome's commonest enemy: what a wave always brings, and what comes when nothing else can.
    EnemyType RankAndFile => System.Array.Find(Types, t => t.weight >= 1f && Here(t, train.Biome)) ?? Types[0];

    // Floating goods, one per kind of cargo. Not enemies: they drift by, ignore the train, and only the hero can break them.
    public static readonly EnemyType[] Loot =
    {
        new EnemyType { name = "Madera", loot = 0, color = new Color(0.9f, 0.5f, 0.18f), health = 20f, size = 1.6f, reward = 0 },
        new EnemyType { name = "Roca", loot = 1, color = new Color(0.55f, 0.65f, 0.85f), health = 30f, size = 1.6f, reward = 0 },
        new EnemyType { name = "Armas", loot = 2, color = new Color(0.4f, 0.7f, 0.25f), health = 14f, size = 1.3f, reward = 0, flies = true },
    };

    public class Enemy
    {
        public Vector3 position, velocity; // velocity: only floating goods use it
        public EnemyType type;
        public float hp, maxHp, slowUntil, timer;
        public float markedUntil; // the Hawk Eye's mark: everything hurts it twice as much meanwhile
        public float punch = 1f;  // its blow on a wagon, times its type's (a stampede's come in hard)
        public int index, pick; // pick: a number of its own, which decides the wagon it goes for
        public int id; // changes when the enemy dies; whoever targets it compares ids to notice
        public System.Func<float, float> onUltimateHit; // a proxy body can account against its owner's actual HP
    }

    class Bullet
    {
        public Vector3 position, velocity, size;
        public Quaternion rotation = Quaternion.identity;
        public Enemy target; // null for a flash (explosion, lightning, beam)
        public bool free;    // aimed at nobody: flies straight and hits the first enemy in its way
        public int targetId;
        public bool hero;    // fired by the hero: the only shots that hurt floating goods
        public Weapon weapon;
        public Batch batch;
        public float life;
        public int level, chain;
        public int style;          // Weapon.style of the shot (0 for flashes)
        public float age, span;    // seconds since fired; a boomerang's or an orbit's whole flight
        public float phase;        // an orbit's angle; a swirl's way of curling, a wobble's beat
        public bool back;          // a boomerang on its way home
        public bool split;         // a Missile Swarm rocket: it bursts into small ones on the way
        public float power = 1f;   // share of the weapon's damage this one carries
        public Vector3 from, dir;  // where it was fired from; a boomerang's way out
        public readonly List<int> hits = new(); // piercing shots: who was already hit on this pass
    }

    struct Blast
    {
        public Vector3 center;
        public EnemyType type;
    }

    struct MuzzleFlash
    {
        public Vector3 at, forward;
        public Batch batch;
        public float size, life;
    }

    // A black mark left on the ground where an enemy fell. It is glued to the ground, so it scrolls
    // back with it, and is dropped once it is behind the train.
    struct Stain
    {
        public Vector3 at;
        public float size, yaw;
    }

    public static Combat Instance { get; private set; }
    // DIAG: train damage by enemy type (name, +tier for big/elite), read and cleared by the chapter probe.
    public static readonly System.Collections.Generic.Dictionary<string, float> TrainHurtBy = new System.Collections.Generic.Dictionary<string, float>();
    static void Tally(string k, float amount) { TrainHurtBy.TryGetValue(k, out float v); TrainHurtBy[k] = v + amount; }
    static void Tally(Enemy e, float amount) => Tally(e.type.name + (e.type.tier > 0 ? "+" + e.type.tier : ""), amount);
    public bool Paused => train.Swarming || train.AtStation || train.UltimatePresenting; // turrets hold their fire too
    public int EnemyCount => enemies.Count;

    // A pack of the plain small ones put down at a spot (the rival's boarders): from there they go for the train.
    // The duel's waves (RivalEvent): only the hero can bring these down, the turrets never aim at them.
    static readonly EnemyType Raider = new EnemyType
    {
        name = "Raider", color = new Color(1f, 0.45f, 0.2f), health = 5f, speed = 4f, damage = 0.7f, size = 1.9f,
        reward = 3, model = "Enemy_imp", flies = true, heroOnly = true,
    };

    public void DropRaiders(Vector3 at, int count)
    {
        for (int i = 0; i < count; i++)
            Add(Raider, at + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f)));
    }

    public void Drop(Vector3 at, int count)
    {
        for (int i = 0; i < count; i++)
            Add(RankAndFile, at + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f)));
    }

    // Everyone off the field at once (not a boss's or the rival's own pieces): the duel is fought alone.
    public void ClearField()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
            if (!enemies[i].type.projectile)
            {
                Burst("smoke", enemies[i].position, 0.8f);
                RemoveEnemy(i);
            }
    }
    public float stationShotEvery = 0.16f; // seconds between shots of the station's guns
    float stationShotIn;
    int stationGunTurn;
    readonly Batch stationShot = new Batch(new Color(4f, 1.6f, 0.2f), false); // their thick bolts
    readonly Batch[] prism =
    {
        new Batch(new Color(1f, 0.2f, 0.35f), false, glow: 4f), new Batch(new Color(0.3f, 1f, 0.45f), false, glow: 4f), new Batch(new Color(0.3f, 0.5f, 1f), false, glow: 4f),
    };
    readonly Batch cometCore = new Batch(new Color(0.85f, 0.95f, 1f), false, glow: 5f);           // the Plasma Comet's head
    readonly Batch shellMark = new Batch(new Color(1f, 0.15f, 0.1f), false, round: true, glow: 2f); // where a Barrage shell will land
    float nextComet;                                                                              // when the hero's next comet may go
    float spiral;                                                                                 // the Lead Hurricane's turn
    readonly Batch reticle = new Batch(new Color(1f, 0.85f, 0.2f), false, glow: 3f);             // the Hawk Eye's mark on an enemy

    // The Drone Squad's two drones, circling the hero.
    Vector3 Drone(int k)
    {
        float a = Time.time * 2.4f + k * Mathf.PI;
        return player.Position + new Vector3(Mathf.Cos(a) * 1.7f, 0.9f, Mathf.Sin(a) * 1.7f);
    }

    // The Power Grid: lightning between the target and up to six enemies around it, each linked to the next and back
    // to the middle, a net that hurts all of them at once.
    void Web(Enemy centre, Weapon weapon, int level)
    {
        source = weapon;
        chainSkip.Clear();
        chainSkip.Add(centre);
        Vector3 last = centre.position;
        float damage = weapon.Damage(level) * Power;
        for (int k = 0; k < 6; k++)
        {
            Enemy next = Nearest(centre.position, 9f * Area, chainSkip);
            if (next == null) break;
            chainSkip.Add(next);
            Bolt(last + Vector3.up, next.position + Vector3.up, weapon.Batch, 3, 0.5f, 0.22f, 0.16f);
            Bolt(centre.position + Vector3.up, next.position + Vector3.up, weapon.Batch, 3, 0.6f, 0.16f, 0.16f);
            last = next.position;
            Hurt(next, damage);
        }
        Burst(weapon.id, centre.position, 2f);
        Hurt(centre, damage);
    }
    // The ultimate (BBA's): a shot into the sky, then five giant balls of the same colour fall one after another,
    // each somewhere else in the picture, and crush everything around where they land.
    struct Meteor { public Vector3 at; public float landAt; public Batch look; public Weapon weapon; }
    readonly List<Meteor> meteors = new();
    readonly Dictionary<Weapon, Batch> meteorLooks = new();
    readonly Batch meteorMark = new Batch(new Color(1f, 0.25f, 0.2f), false, round: true, glow: 2f); // where it will land
    const float MeteorFall = 1.1f, MeteorDelay = 0.7f, MeteorRadius = 9f, MeteorHeight = 45f, MeteorGap = 0.45f;
    const int MeteorCount = 5;

    public void Ultimate(Vector3 from, Weapon weapon)
    {
        // The shot up: a tall glowing column from the hero, a flash at her feet.
        Spawn(from + Vector3.up * 20f, new Vector3(0.7f, 40f, 0.7f), weapon.Batch, 0.35f);
        Burst("levelup", from, 2.5f);
        train.Shake(0.25f);
        if (!meteorLooks.TryGetValue(weapon, out Batch look))
            meteorLooks[weapon] = look = new Batch(weapon.color, false, Resources.GetBuiltinResource<Mesh>("Sphere.fbx"), glow: 3f);
        // On the crowd closest to the train (what is about to hit it); with few enemies about, the four outer corners
        // and the centre, with jitter and shuffled landing order.
        var near = enemies.FindAll(e => !e.type.projectile && e.type.loot < 0);
        near.Sort((a, b) => (a.position - train.Focus).sqrMagnitude.CompareTo((b.position - train.Focus).sqrMagnitude));
        var ground = new Plane(Vector3.up, Vector3.zero);
        int[] order = { 0, 1, 2, 3, 4 };
        for (int k = order.Length - 1; k > 0; k--) { int j = Random.Range(0, k + 1); (order[k], order[j]) = (order[j], order[k]); }
        for (int k = 0; k < MeteorCount; k++)
        {
            if (near.Count >= MeteorCount * 4)
            {
                Vector3 on = near[Random.Range(0, Mathf.Min(near.Count, 30))].position;
                meteors.Add(new Meteor { at = new Vector3(on.x, 0f, on.z), landAt = Time.time + MeteorDelay + MeteorFall + k * MeteorGap, look = look, weapon = weapon });
                continue;
            }
            int zone = order[k];
            float x = zone == 4 ? Random.Range(0.35f, 0.65f)
                : zone % 2 == 0 ? Random.Range(0.08f, 0.24f) : Random.Range(0.76f, 0.92f);
            float y = zone == 4 ? Random.Range(0.35f, 0.65f)
                : zone < 2 ? Random.Range(0.12f, 0.28f) : Random.Range(0.72f, 0.88f);
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(x, y, 0f));
            Vector3 at = ground.Raycast(ray, out float hit) ? ray.GetPoint(hit) : from;
            meteors.Add(new Meteor { at = at, landAt = Time.time + MeteorDelay + MeteorFall + k * MeteorGap, look = look, weapon = weapon });
        }
    }

    void Meteors()
    {
        for (int i = meteors.Count - 1; i >= 0; i--)
        {
            Meteor m = meteors[i];
            float left = m.landAt - Time.time;
            if (left > MeteorFall) continue; // still going up
            if (left > 0f)
            {
                // Falling, the ball and a trail of smaller ones above it; a mark on the ground where it will land.
                float h = MeteorHeight * left / MeteorFall;
                m.look.Add(m.at + Vector3.up * (h + 3f), Vector3.one * 7f, Quaternion.Euler(Time.time * 120f, Time.time * 90f, 0f));
                for (int k = 1; k <= 4; k++) m.look.Add(m.at + Vector3.up * (h + 3f + k * 3.2f), Vector3.one * (7f - k * 1.4f), Quaternion.identity);
                float grow = 1f - left / MeteorFall;
                meteorMark.Add(new Vector3(m.at.x, 0.06f, m.at.z), new Vector3(MeteorRadius, 0.02f, MeteorRadius) * (0.3f + 0.7f * grow), Quaternion.identity);
                continue;
            }
            // Down: everything around is crushed.
            heroHit = true;
            source = m.weapon;
            float damage = 90f * Balance.Scale * (1f + healthPerLevel * level);
            for (int j = enemies.Count - 1; j >= 0; j--)
            {
                Vector3 d = enemies[j].position - m.at;
                if (d.x * d.x + d.z * d.z <= MeteorRadius * MeteorRadius) Hurt(enemies[j], damage, false, true);
            }
            heroHit = false;
            Burst("grenade", m.at, 9f);
            Burst("rocket", m.at, 7f);
            Ring(m.at, MeteorRadius, m.look);
            Ring(m.at, MeteorRadius * 0.6f, m.look);
            train.Shake(0.45f);
            meteors.RemoveAt(i);
        }
        foreach (Batch b in meteorLooks.Values) b.Flush();
        meteorMark.Flush();
    }

    readonly Batch healRing = new Batch(new Color(0.5f, 1f, 0.45f), false, glow: 3f);        // the Solar Heart's mending beat
    readonly Batch dragonDark = new Batch(new Color(0.05f, 0.14f, 0.55f), false);             // the Masamune dragon's deep blue hide, outlined
    readonly Batch dragonScale = new Batch(new Color(0.2f, 0.5f, 1f), false, glow: 1.4f);   // its scales, a lighter blue
    readonly Batch dragonFire = new Batch(new Color(0.35f, 0.7f, 1f), false, glow: 2.6f);   // and the blue flames all over it: only these glow
    readonly Batch stain = new Batch(new Color(0.02f, 0.02f, 0.025f), false, round: true); // round black marks left by the dead
    readonly Batch blob = new Batch(new Color(0.03f, 0.03f, 0.04f), false, round: true); // a dark spot under each robot: it stands out from any ground
    const float StationFlashSize = 2.2f; // the heavy guns leave a much bigger flare than a wagon turret
    public float calmDistance = 15f, calmRetreat = 12f; // how far enemies keep during a button game, and how fast they get there
    const float ModelScale = 1.4f;  // the robots are slimmer than the cubes they replace
    const float BurstSize = 0.6f; // effect scale per unit of blast radius

    // Difficulty = stations reached + biome jumps. Pressure (enemies x health) grows a little slower
    // than a train that takes a good upgrade every station, so skipped or unlucky upgrades get punished.
    [Header("Difficulty")]
    public float spawnRadius = 45f;
    [System.NonSerialized] public float spawnRate = 4f;         // spawns per second at difficulty 0
    [System.NonSerialized] public float spawnRatePerLevel = 0.7f;
    // A screen full of enemies that fall fast: the small ones come this many times over, each with a share of
    // the health, damage and pay their type says, so the whole of them weighs what a fifth as many used to.
    // The big ones keep their numbers and their strength, and stand out in the crowd.
    public const float BigDamage = 0.5f, Crowd = 5f, FodderHealth = 0.24f, FodderDamage = 0.1f, FodderMoney = 0.22f;
    // Every so often a horde: a pack of the small ones all at once, from all round.
    [System.NonSerialized] public Vector2 hordeEvery = new Vector2(12f, 18f);
    [System.NonSerialized] public int hordeSize = 28, hordeMax = 110;
    [System.NonSerialized] public float healthPerLevel = 0.12f;   // composition and density carry most of the pressure
    [System.NonSerialized] public float damagePerLevel = 0.025f;  // slower growth for a train that cannot dodge; capped at +100%
    public float DamageScale => (1f + damagePerLevel * Mathf.Min(level, 40)) * enemyDamage * Balance.Scale;
    // Vampire Survivors' TimeMods (+2 to 2.5% speed a minute): faster crowds reach a train that cannot step aside.
    // Gentler here (+1.2%/min): the chapter's difficulty already climbs faster at its end. 0.012 lost whole chapters.
    [System.NonSerialized] public float speedPerLevel = 0.008f;   // capped at +32%
    float SpeedScale => 1f + speedPerLevel * Mathf.Min(level, 40);
    // Vampire Survivors' waves: a minimum of enemies on the field that grows with time, filled at once whenever the
    // field falls short of it, and a ceiling past which nobody new comes (bosses and events excepted).
    [System.NonSerialized] public int minOnField = 15, minPerLevel = 6, maxOnField = 220;
    // Stampedes (Vampire Survivors' swarm events): every so often a dense column comes in from one side, straight for
    // the train, none of it turning aside for the hero. They grow with the difficulty.
    [System.NonSerialized] public float stampedeEvery = 45f;
    [System.NonSerialized] public int stampedeSize = 10, stampedePerLevel = 4, stampedeMax = 90;
    float stampedeTimer = 90f; // not before the first upgrades: early on the train has little to stop them with
    int MinOnField => Mathf.Min(maxOnField, minOnField + minPerLevel * level);
    int OnField { get { int n = 0; foreach (Enemy e in enemies) if (!e.type.projectile && e.type.loot < 0) n++; return n; } }
    // The three knobs to turn first when the whole game feels too easy or too hard: every enemy's health and
    // damage, and the money a kill pays, against what their types say.
    [System.NonSerialized] public float enemyHealth = 1f, enemyDamage = 0.8f, killMoney = 0.6f;
    public float speedDrift = 0.15f;       // how much a faster or slower train drags enemies back or lets them catch up   // +5% enemy damage per difficulty level

    [Header("Floating goods")]
    public Vector2 lootEvery = new Vector2(12f, 22f); // seconds between one and the next
    public float lootTime = 8f;                       // seconds it hangs around before flying off
    public float lootHeight = 2.5f;
    public float lootDistance = 20f;                  // at least this far from the hero when it appears

    [Header("Bullets")]
    public float bulletLife = 2f;
    public float chainRange = 8f;

    readonly List<Enemy> enemies = new();
    readonly List<Bullet> bullets = new();
    readonly List<Enemy> chainSkip = new(), picked = new();
    readonly List<Blast> blasts = new(); // enemy death explosions, resolved once nothing is being iterated
    readonly List<MuzzleFlash> flashes = new(); // quick barrel flares, drawn in the weapon's own colour
    readonly List<Stain> stains = new(); // black ground marks that outlive the enemies that fell on them
    const float FlashLife = 0.09f;
    readonly Stack<Enemy> enemyPool = new();
    readonly Stack<Bullet> bulletPool = new();

    const float PlayerRadius = 0.5f;

    TrainSim train;
    public float FireRate => train.fireRate;
    Player player;
    float spawnTimer, lootTimer = 8f, hordeTimer = 8f;
    static bool thinned;
    int level;
    bool heroHit; // true while a hit made by the hero is being applied
    AudioSource stationLaserSound;


    void Awake()
    {
        Instance = this;
        train = GetComponent<TrainSim>();
        if (!thinned)
        {
            // Once per run of the program: the types are shared, and a restart comes through here again.
            thinned = true;
            foreach (EnemyType type in Types)
                if (type.Fodder) { type.damage *= FodderDamage; type.explodeDamage *= FodderDamage; type.health *= FodderHealth; type.fodder = true; }
                else { type.damage *= BigDamage; type.explodeDamage *= BigDamage; } // the big ones come among a crowd now: a softer blow each
        }
        stationLaserSound = Sound.Effect(gameObject, "biglaser_soft", 0.4f);
    }

    void Update()
    {
        if (train.UltimatePresenting) return;
        if (train.Swarming) return; // seen from the gunner's seat: the rest of the fight is put away until it is over
        if (train.AtStation)
        {
            // Stations are safe. Whoever was still chasing the train stops dead and the station's two heavy guns
            // pick them off, nearest first, before the menu comes up. No money for these: they are not our kills.
            // Shots still in the air (ours, and the guns' bolts) fade out; nothing else moves.
            for (int i = bullets.Count - 1; i >= 0; i--)
                if ((bullets[i].life -= Time.unscaledDeltaTime * (bullets[i].batch == stationShot ? 1f : 6f)) <= 0f) RemoveBullet(i);
            // A mini boss withdraws rather than face the station's guns; the same one is back as the train leaves,
            // as worn as it was, and so on until it is beaten.
            if (miniBoss != null && miniBoss.id == miniBossId)
            {
                miniBossLeft = miniBoss.hp / miniBoss.maxHp;
                RemoveEnemy(miniBoss.index);
                miniBoss = null;
                miniBossTimer = 1.5f;
            }
            // Only what is in the picture is worth the guns' time: whoever is out of it is simply gone.
            Camera view = Camera.main;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Vector3 seen = view.WorldToViewportPoint(enemies[i].position);
                if (seen.x < -0.03f || seen.x > 1.03f || seen.y < -0.03f || seen.y > 1.03f) RemoveEnemy(i);
            }
            if (enemies.Count == 0) return;
            stationShotIn -= Time.unscaledDeltaTime;
            if (stationShotIn > 0f || enemies.Count == 0) return;
            stationShotIn = stationShotEvery;
            // A crowd produces simultaneous shots; layering the same clip made the first volley too loud.
            if (stationLaserSound.clip) stationLaserSound.PlayOneShot(stationLaserSound.clip);
            for (int shots = Mathf.CeilToInt(enemies.Count / 12f); shots > 0 && enemies.Count > 0; shots--) // a crowd goes faster
            {
                Transform gun = train.StationGun(stationGunTurn++ % 2);
                Enemy target = enemies[0];
                foreach (Enemy e in enemies)
                    if ((e.position - gun.position).sqrMagnitude < (target.position - gun.position).sqrMagnitude) target = e;
                Vector3 muzzle = gun.position + Vector3.up * 1.9f, to = target.position - muzzle;
                Vector3 aim = new Vector3(to.x, 0f, to.z).normalized;
                gun.rotation = Quaternion.LookRotation(aim);
                Spawn(muzzle + to * 0.5f, new Vector3(0.45f, 0.45f, to.magnitude), stationShot, 0.12f, Quaternion.LookRotation(to));
                AddFlash(muzzle, aim, stationShot, StationFlashSize); // a big flare on the heavy guns
                Burst("spark", muzzle + aim * 3.2f, 1.6f);
                Burst("rocket", target.position, 2.2f);
                RemoveEnemy(target.index); // a balloon too: its cargo is only won by the hero shooting it down
            }
            return;
        }

        float dt = Time.deltaTime, now = Time.time;
        level = train.Difficulty;
        player = train.Player;

        // A button game or a curve under the train: nobody new arrives, the rest back off and nothing they do counts.
        bool calm = train.Calm;
        if (!calm && !train.BossFight) spawnTimer -= dt; // a boss blocks the road: nobody else joins in
        // A new wave, with faces of its own: on its timer, and at once when a new kind of enemy unlocks.
        bool unlocked = level != rosterLevel && System.Array.Exists(Types, t => t.firstLevel == level && t.weight > 0f);
        rosterLevel = level;
        if ((rosterTimer -= dt) <= 0f || unlocked) { rosterTimer = rosterEvery; NewRoster(); }
        int onField = OnField;
        while (!calm && !train.BossFight && spawnTimer <= 0f)
        {
            spawnTimer += 1f / (spawnRate + spawnRatePerLevel * level);
            if (onField < maxOnField) SpawnWave();
            onField = OnField;
        }
        // Short of the minimum: topped up fast (a few a frame, so it comes in as a tide and not all in one frame).
        if (!calm && !train.BossFight && !train.AtStation)
            for (int k = 0; k < 6 && onField < MinOnField; k++) { SpawnWave(); onField = OnField; }
        if (!calm && !train.BossFight && !train.AtStation && (hordeTimer -= dt) <= 0f)
        {
            hordeTimer = Random.Range(hordeEvery.x, hordeEvery.y);
            SpawnWave(Mathf.Min(hordeMax, hordeSize + level * 4));
        }
        if (!calm && !train.BossFight && !train.AtStation && (stampedeTimer -= dt) <= 0f)
        {
            stampedeTimer = stampedeEvery * Random.Range(0.85f, 1.15f);
            Stampede(Mathf.Min(stampedeMax, stampedeSize + stampedePerLevel * level));
        }
        if (miniBoss != null && miniBoss.id != miniBossId) miniBoss = null; // shot down, or cleared off for a duel
        if (miniBoss == null && !calm && !train.BossFight && (miniBossTimer -= dt) <= 0f)
        {
            miniBossTimer = miniBossEvery;
            // Always from one side of the train, never ahead of it or behind.
            Vector3 along = train.Forward(train.CarCount / 2);
            along.y = 0f;
            along.Normalize();
            Vector3 across = Vector3.Cross(Vector3.up, along) * (Random.value < 0.5f ? 1f : -1f);
            if (miniBossLeft >= 1f) miniBossLook = Random.value < 0.5f ? MiniDemon : MiniBoss; // one coming back keeps its look
            miniBoss = Add(miniBossLook, train.Focus + across * RingRadius() + along * Random.Range(-8f, 8f));
            miniBossId = miniBoss.id;
            miniBoss.maxHp *= 1f + 0.18f * train.Strength; // Vampire Survivors: a boss's health is its base times the player's level
            miniBoss.hp = miniBoss.maxHp * miniBossLeft; // one that withdrew comes back as worn as it went
            miniBossLeft = 1f;
            bossMode = Ram;
            bossWagon = RandomWagon(-1);
            train.Toast(L10n.T("miniboss.coming", TauntKey));
        }
        if (miniBoss != null && !train.BossFight && OnScreen(miniBoss.position)) train.BossBar(L10n.T("miniboss.name"), miniBoss.hp / miniBoss.maxHp); // once it is in the picture

        lootTimer -= dt;
        if (lootTimer <= 0f)
        {
            lootTimer = Random.Range(lootEvery.x, lootEvery.y);
            if (train.CanTakeLoot) SpawnLoot();
        }

        // Enemies head for the closest wagon that is still standing.
        // Faster than cruise: everyone slides towards the rear, so the front arrives sooner and the rear lags.
        float damageScale = DamageScale, drift = (train.Speed - train.maxSpeed) * speedDrift * dt;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            EnemyType t = e.type;
            if (t.projectile) continue; // the boss drives its own rig and rockets; Combat only lets them be shot
            if (t.loot >= 0)
            {
                // Drifts for a while, then speeds away. It only exists in the picture: out of it, it is gone.
                e.timer -= dt;
                e.position += e.velocity * (dt * (e.timer > 0f ? 1f : 10f));
                Vector3 seen = Camera.main.WorldToViewportPoint(e.position);
                if (seen.x < -0.05f || seen.x > 1.05f || seen.y < -0.05f || seen.y > 1.05f) RemoveEnemy(i);
                continue;
            }
            if (IsMiniBoss(t))
            {
                MiniBossMove(e, dt, damageScale);
                continue;
            }
            // Each goes for a wagon of its own; one that runs into another wagon on the way hits that one.
            int wagon = train.WagonFor(e.pick, e.position, out Vector3 toWagon);
            if (wagon < 0) break; // train destroyed
            // Whatever wagon it reaches first is the one it hits, its own or another on the way: it never goes
            // through the train.
            int near = train.ClosestWagon(e.position, out Vector3 toNear);
            if (near != wagon && toNear.sqrMagnitude < (t.size * 0.5f + 0.8f) * (t.size * 0.5f + 0.8f)) { wagon = near; toWagon = toNear; }
            e.position -= train.Forward(wagon) * drift; // along the track where its wagon is, so it holds through a curve
            float sqr = toWagon.sqrMagnitude, radius = t.size * 0.5f;
            if (calm)
            {
                float away = Mathf.Sqrt(sqr);
                if (away < calmDistance && away > 0.01f) e.position -= toWagon * (Mathf.Min(calmDistance - away, calmRetreat * dt) / away);
                continue;
            }

            // The hero is a target too: for one in two (the hunters), whenever she is closer than the train; for the
            // rest only when she is right in their way. Circling round, she no longer drags the whole crowd off it.
            bool atPlayer = false;
            if (player.Alive)
            {
                Vector3 toPlayer = player.Position - e.position;
                toPlayer.y = 0f;
                float gap = Mathf.Max(0f, toPlayer.magnitude - PlayerRadius);
                bool hunter = e.pick % 2 == 0;
                if (gap * gap < sqr && (hunter || gap < 2.5f))
                {
                    atPlayer = true;
                    toWagon = toPlayer.normalized * gap;
                    sqr = gap * gap;
                }
            }

            if (sqr < radius * radius)
            {
                if (atPlayer) player.Damage(t.damage * damageScale);
                else
                {
                    train.Damage(wagon, t.damage * damageScale * e.punch); Tally(e, t.damage * damageScale * e.punch);
                    if (t.stun > 0f) train.Stun(wagon, t.stun);
                    Burst("smoke", e.position, 0.7f + t.size * 0.4f); // dust where it slams into the wagon
                }
                Die(e, false);
                continue;
            }

            if (t.regen > 0f) e.hp = Mathf.Min(e.maxHp, e.hp + t.regen * Balance.Scale * dt);

            bool inRange = t.ranged > 0f && sqr <= t.ranged * t.ranged;
            if (t.interval > 0f && (e.timer -= dt) <= 0f)
            {
                e.timer = t.interval;
                if (inRange)
                {
                    if (atPlayer) player.Damage(t.damage * damageScale);
                    else
                    {
                        train.Damage(wagon, t.damage * damageScale); Tally(e, t.damage * damageScale);
                        Burst("smoke", e.position + toWagon + Vector3.up * 0.5f, 0.6f + t.size * 0.4f);
                    }
                    Spawn(e.position + toWagon + Vector3.up, Vector3.one * 0.6f, t.Batch, 0.1f);
                }
                else if (t.blink > 0f) e.position += toWagon.normalized * Mathf.Min(t.blink, Mathf.Sqrt(sqr) - radius);
                else if (t.spawner && OnField < maxOnField) Add(Larva, e.position + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
                else if (t.heal > 0f)
                {
                    foreach (Enemy other in enemies)
                        if (other != e && (other.position - e.position).sqrMagnitude <= t.healRadius * t.healRadius)
                            other.hp = Mathf.Min(other.maxHp, other.hp + t.heal * t.interval * Balance.Scale);
                }
            }
            if (inRange) continue; // shooters hold their ground

            float speed = t.speed * SpeedScale;
            if (t.chargeRange > 0f && sqr <= t.chargeRange * t.chargeRange) speed *= t.chargeSpeed;
            if (now < e.slowUntil) speed *= 0.5f;
            // Never further in one step than the wagon ahead, nor than any wagon on the way: a fast one (or a long
            // frame) used to step clean over the train. It stops at the wagon's side and hits it next frame.
            float dist = Mathf.Sqrt(sqr), stop = Mathf.Max(0f, dist - radius * 0.8f);
            if (near != wagon) stop = Mathf.Min(stop, Mathf.Max(0f, toNear.magnitude - radius * 0.8f));
            e.position += toWagon * (Mathf.Min(speed * dt, stop) / dist);
        }
        ResolveBlasts(calm ? 0f : damageScale);

        // Each bullet tracks only its own target: one check per bullet, and it cannot miss or tunnel.
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet b = bullets[i];
            b.age += dt;
            if (b.style == Weapon.Comet)
            {
                // Through everyone in its way, then it bursts in a ring at the end of its run.
                b.position += b.velocity * dt;
                Pierce(b, 1.5f);
                if ((b.life -= dt) > 0f) continue;
                heroHit = b.hero;
                Explode(b.position, 4f, b.weapon.Damage(b.level) * Power * 2f, b.weapon);
                heroHit = false;
                Ring(b.position, 4f * Area, b.batch);
                Burst("levelup", b.position, 2.5f);
                RemoveBullet(i);
                continue;
            }
            if (b.split && b.age > 0.45f)
            {
                // The Missile Swarm: the rocket bursts into five small ones, each after an enemy of its own.
                b.split = false;
                Burst("rocket", b.position, 1.4f);
                picked.Clear();
                for (int k = 0; k < 5; k++)
                {
                    Enemy prey = Nearest(b.position, b.weapon.range, picked);
                    if (prey == null) break;
                    picked.Add(prey);
                    heroHit = b.hero;
                    Fire(b.position, prey, b.weapon, b.level);
                    heroHit = false;
                    Bullet mini = bullets[bullets.Count - 1];
                    mini.size = Vector3.one * 0.14f;
                    mini.power = 0.45f;
                }
            }
            if (b.style == Weapon.Shell)
            {
                // Straight down from the sky; on the ground, a blast.
                b.position += b.velocity * dt;
                if (b.position.y > 0.3f) continue;
                Vector3 ground = new Vector3(b.position.x, 0f, b.position.z);
                Explode(ground, 3f, b.weapon.Damage(b.level) * Power * 3f, b.weapon);
                Burst("rocket", ground, 2.4f);
                train.Shake(0.06f);
                RemoveBullet(i);
                continue;
            }
            if (b.style == Weapon.Boomerang || b.style == Weapon.Orbit || b.style == Weapon.Slash)
            {
                if (b.age >= b.span) { RemoveBullet(i); continue; }
                Vector3 centre = b.hero && player ? player.Position : b.from;
                if (b.style == Weapon.Orbit)
                {
                    float angle = (b.phase + now * OrbitSpeed) * Mathf.Deg2Rad;
                    b.position = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (OrbitRadius + 0.3f * b.level + (b.level >= Weapon.Evolved ? 0.8f : 0f));
                }
                else if (b.style == Weapon.Slash)
                {
                    float along = Along(b);
                    b.position = b.from + b.dir * along + Vector3.Cross(Vector3.up, b.dir) * Zig(b, along);
                }
                else
                {
                    float k = b.age / b.span;
                    if (k >= 0.5f && !b.back) { b.back = true; b.hits.Clear(); } // turned for home: everyone can be hit again
                    b.position = centre + b.dir * (b.weapon.range * Mathf.Sin(Mathf.PI * k));
                    if (b.level >= Weapon.Evolved) // the Chakram: further out, on a wide loop
                        b.position += b.dir * (b.weapon.range * 0.5f * Mathf.Sin(Mathf.PI * k)) + Vector3.Cross(Vector3.up, b.dir) * (b.weapon.range * 0.6f * Mathf.Sin(2f * Mathf.PI * k));
                }
                bool evolved = b.level >= Weapon.Evolved;
                Pierce(b, b.style == Weapon.Orbit ? (evolved ? 1.3f : 0.8f) : b.style == Weapon.Slash ? SlashHalf(b) : evolved ? 1.8f : 1.1f);
                continue;
            }
            Enemy hit = b.target;
            if (b.free)
            {
                // Whoever it runs into first: one pass over the enemies per free bullet.
                hit = null;
                Vector3 flat = new Vector3(b.position.x, 0f, b.position.z);
                foreach (Enemy e in enemies)
                {
                    if (e.type.projectile) continue;
                    float near = b.weapon.speed * dt + e.type.size * 0.5f + 0.3f, dx = e.position.x - flat.x, dz = e.position.z - flat.z;
                    if (dx * dx + dz * dz <= near * near) { hit = e; break; }
                }
                if (hit != null) { b.free = false; b.target = hit; b.targetId = hit.id; }
            }
            if (hit != null && hit.id == b.targetId)
            {
                Vector3 toTarget = hit.position - b.position;
                float reach = b.weapon.speed * dt + hit.type.size * 0.5f + 0.3f;
                if (toTarget.sqrMagnitude <= reach * reach)
                {
                    Vector3 at = hit.position;
                    Burst("smoke", at, 0.6f + hit.type.size * 0.4f); // a puff where the shot lands
                    if (b.weapon.slow > 0f && !hit.type.slowImmune) hit.slowUntil = now + b.weapon.slow * Duration;
                    heroHit = b.hero;
                    source = b.weapon;
                    float damage = b.weapon.Damage(b.level) * Power * b.power, splash = b.weapon.Splash(b.level);
                    bool evolved = b.level >= Weapon.Evolved;
                    if (evolved && b.weapon.style == Weapon.Pellets) splash = Mathf.Max(splash, 2f); // Golden Buckshot: every pellet bursts
                    if (splash > 0f) Explode(at, splash, damage, b.weapon);
                    else Hurt(hit, damage);
                    if (evolved && b.weapon.style == Weapon.Lob) // the Cluster Bomb: four more blasts around the first
                        for (int k = 0; k < 4; k++) Explode(at + Quaternion.Euler(0f, k * 90f + 45f, 0f) * Vector3.forward * splash, splash * 0.6f, damage * 0.5f, b.weapon);
                    if (evolved && b.weapon.slow > 0f) // the Blizzard: a burst of frost, and everyone in it slowed too
                    {
                        Ring(at, 3.5f * Area, b.batch);
                        foreach (Enemy near in enemies)
                            if (!near.type.slowImmune && (near.position - at).sqrMagnitude < 12f * Area * Area) near.slowUntil = now + b.weapon.slow * Duration;
                    }
                    if (evolved && b.weapon.id == "sniper") // the Hawk Eye: everyone around the shot is marked, and takes double
                        foreach (Enemy near in enemies)
                            if ((near.position - at).sqrMagnitude < 25f * Area * Area) near.markedUntil = now + 4f * Duration;
                    heroHit = false;

                    chainSkip.Clear();
                    chainSkip.Add(hit);
                    b.target = b.chain-- > 0 ? Nearest(at, chainRange, chainSkip) : null;
                    if (b.target == null) RemoveBullet(i);
                    else
                    {
                        Bolt(at, b.target.position, b.batch, 3, 0.6f); // the jump shows as a crackling arc
                        b.targetId = b.target.id;
                        b.position = at;
                    }
                    continue;
                }
                Vector3 way = toTarget.normalized;
                if (b.style == Weapon.Swirl) way = Quaternion.Euler(0f, b.phase * 50f * Mathf.Clamp01(toTarget.magnitude / 10f), 0f) * way; // curls in
                b.velocity = way * b.weapon.speed;
            }

            // Target already dead: keep flying straight until the bullet expires.
            b.life -= dt;
            if (b.life <= 0f) RemoveBullet(i);
            else b.position += b.velocity * dt;
        }
        ResolveBlasts(calm ? 0f : damageScale);
    }

    // Drawn after every script has moved, so nothing lags a frame behind.
    void LateUpdate()
    {
        if (train.Swarming) return; // nothing of the ordinary fight is drawn meanwhile
        if (!train.UltimatePresenting) ResolveBlasts(DamageScale); // kills made by turrets after our Update
        // Robots face the train, hover and bob if they fly, and turn on the spot if that is their thing.
        float clock = Time.time;
        foreach (Enemy e in enemies)
        {
            EnemyType t = e.type;
            Vector3 at = e.position, toTrain = new Vector3(-at.x, 0f, -at.z);
            if (t.projectile) continue; // the boss draws its own rig and rockets
            if (t.loot >= 0)
            {
                // Floats well clear of the ground, swaying.
                at.y += 1.6f + Mathf.Sin(clock * 1.5f + e.id) * 0.25f;
                t.Batch.Add(at, Vector3.one * (t.size * 2.6f), Quaternion.Euler(0f, clock * 20f + e.id * 31f, Mathf.Sin(clock * 1.1f + e.id) * 5f));
                continue;
            }
            if (t == MiniBoss && e.velocity.sqrMagnitude > 0.01f) toTrain = bossMode == BackOff ? -e.velocity : e.velocity; // it backs off facing the train
            Quaternion facing = t.Spins ? Quaternion.Euler(0f, clock * 140f + e.id * 47f, 0f)
                : toTrain.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toTrain) : Quaternion.identity;
            blob.Add(new Vector3(at.x, 0.04f, at.z), new Vector3(t.size * 1.1f, 0.02f, t.size * 1.1f));
            if (t.flies) at.y += 0.7f + Mathf.Sin(clock * 3f + e.id) * 0.18f;
            t.Batch.Add(at, Vector3.one * (t.size * ModelScale), facing);
        }
        foreach (Bullet b in bullets) DrawBullet(b, clock);
        Shards(clock);
        Meteors();
        if (player && player.Alive && player.ammo.id == "smg" && player.level >= Weapon.Evolved)
            for (int k = 0; k < 2; k++)
            {
                // Each drone: a body and a spinning rotor.
                Vector3 at = Drone(k);
                player.ammo.Batch.Add(at, new Vector3(0.5f, 0.25f, 0.5f), Quaternion.Euler(0f, clock * 90f, 0f));
                player.ammo.Batch.Add(at + Vector3.up * 0.2f, new Vector3(1.1f, 0.04f, 0.12f), Quaternion.Euler(0f, clock * 1500f, 0f));
            }
        foreach (Enemy e in enemies)
            if (clock < e.markedUntil)
                for (int k = 0; k < 4; k++)
                {
                    // A reticle of four bars round it, turning.
                    Quaternion q = Quaternion.Euler(0f, clock * 120f + k * 90f, 0f);
                    float r = e.type.size * 0.9f + 0.5f;
                    reticle.Add(e.position + Vector3.up * 0.3f + q * Vector3.forward * r, new Vector3(0.6f, 0.08f, 0.12f), q);
                }
        FlashMuzzles(); // barrel flares join the same batches as the shots
        DrawStains(); // the marks the dead left, under everything else
        foreach (EnemyType t in Types) t.Batch.Flush();
        foreach (EnemyType t in variants.Values) t.Batch.Flush();
        foreach (EnemyType t in Loot) t.Batch.Flush();
        Larva.Batch.Flush();
        MiniBoss.Batch.Flush();
        MiniDemon.Batch.Flush();
        Raider.Batch.Flush();
        stationShot.Flush();
        dragonDark.Flush();
        healRing.Flush();
        cometCore.Flush();
        reticle.Flush();
        shellMark.Flush();
        foreach (Batch b in prism) b.Flush();
        dragonScale.Flush();
        dragonFire.Flush();
        stain.Flush();
        blob.Flush();
        foreach (Weapon w in Weapon.Wagon) w.Batch.Flush();
        Weapon.Truck.Batch.Flush();
        foreach (Weapon w in Weapon.Hero) w.Batch.Flush();
    }

    // After a curve the world is turned so the train lies along X again; everything on the field turns with it.
    public void Rotate(Quaternion turn)
    {
        foreach (Enemy e in enemies)
        {
            e.position = turn * e.position;
            e.velocity = turn * e.velocity;
        }
        foreach (Bullet b in bullets)
        {
            b.position = turn * b.position;
            b.velocity = turn * b.velocity;
            b.from = turn * b.from;
            b.dir = turn * b.dir;
        }
        for (int i = 0; i < stains.Count; i++)
        {
            Stain s = stains[i];
            s.at = turn * s.at;
            stains[i] = s;
        }
    }

    // Slides the ground marks with the ground and lays them down: the main blot plus two smaller ones
    // nudged around it, so the mark reads as a lumpy stain and not a clean circle.
    void DrawStains()
    {
        float scroll = train.GroundScroll, behind = -train.viewRange;
        for (int i = stains.Count - 1; i >= 0; i--)
        {
            Stain s = stains[i];
            s.at.x -= scroll;
            if (s.at.x < behind)
            {
                stains.RemoveAt(i);
                continue;
            }
            stains[i] = s;
            Quaternion yaw = Quaternion.Euler(0f, s.yaw, 0f);
            stain.Add(s.at, new Vector3(s.size, 1f, s.size));
            stain.Add(s.at + yaw * new Vector3(s.size * 0.34f, 0f, 0f), new Vector3(s.size * 0.62f, 1f, s.size * 0.62f));
            stain.Add(s.at + yaw * new Vector3(0f, 0f, s.size * 0.34f), new Vector3(s.size * 0.58f, 1f, s.size * 0.58f));
        }
    }

    // A flat, round mesh for the marks on the ground: a triangle fan in the XZ plane, one unit across.
    // Drawn with both windings and an up normal so it can never come out invisible from above.
    static Mesh disc;

    static Mesh Disc()
    {
        if (disc) return disc;
        const int n = 20;
        var verts = new Vector3[n + 1];
        var normals = new Vector3[n + 1];
        var colors = new Color[n + 1];
        var uv = new Vector2[n + 1];
        var tris = new int[n * 6];
        normals[0] = Vector3.up;
        colors[0] = Color.white;
        uv[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < n; i++)
        {
            float a = (float)i / n * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * 0.5f, z = Mathf.Sin(a) * 0.5f;
            verts[i + 1] = new Vector3(x, 0f, z);
            normals[i + 1] = Vector3.up;
            colors[i + 1] = Color.white;
            uv[i + 1] = new Vector2(x + 0.5f, z + 0.5f);
            int here = i + 1, next = (i + 1) % n + 1;
            tris[i * 6] = 0; tris[i * 6 + 1] = next; tris[i * 6 + 2] = here;
            tris[i * 6 + 3] = 0; tris[i * 6 + 4] = here; tris[i * 6 + 5] = next;
        }
        disc = new Mesh { name = "StainDisc", vertices = verts, normals = normals, colors = colors, uv = uv, triangles = tris };
        disc.RecalculateBounds();
        return disc;
    }

    // Weighted pick among the types unlocked at this difficulty and living in this biome.
    // horde: that many of one small kind, together, instead of the usual one (or its pack).
    void SpawnWave(int horde = 0)
    {
        int room = maxOnField - OnField;
        if (room <= 0) return;
        int biome = train.Biome;
        // The big ones are as many as ever, so they take a smaller share of the many more spawns; a horde has none.
        float Share(EnemyType t) => t.firstLevel > level || !Here(t, biome) || (roster.Count > 0 && !roster.Contains(t)) ? 0f
            : t.Fodder ? t.weight : horde > 0 ? 0f : t.weight / Crowd;
        float total = 0f;
        foreach (EnemyType t in Types) total += Share(t);
        float roll = Random.value * total;
        EnemyType type = RankAndFile;
        foreach (EnemyType t in Types)
        {
            if (Share(t) <= 0f) continue;
            type = t;
            if ((roll -= Share(t)) <= 0f) break;
        }
        type = Variant(type, horde > 0 ? 0 : RollTier()); // hordes are fodder; elites arrive individually

        float angle = Random.value * Mathf.PI * 2f;
        // Always out of the picture: the ring goes round what the camera shows of the ground right now (it pulls
        // back for a long train and for the zigzag), centred where the camera looks. A fixed ring round the
        // train's middle put enemies on screen beside the ends of a long train.
        float radius = RingRadius();
        Vector3 center = train.Focus + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        if (horde > 0)
        {
            // From every side at once, each from a point of its own on the ring: a tide closing in, not a clump
            // falling on one end of the train.
            for (int i = 0; i < Mathf.Min(horde, room); i++)
            {
                float from = Random.value * Mathf.PI * 2f, far = radius + Random.Range(0f, 8f);
                Add(type, train.Focus + new Vector3(Mathf.Cos(from) * far, 0f, Mathf.Sin(from) * far));
            }
            return;
        }
        for (int i = 0; i < Mathf.Min(type.pack, room); i++)
            Add(type, center + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)) * (type.pack - 1));
    }

    // The mini boss: rams a wagon, backs off for a moment, then goes for another wagon, or one time in three for the
    // hero for a while. Close to it, the hero can taunt it (F, or the gamepad's west button): it chases her for a
    // good while instead of the wagons.
    void MiniBossMove(Enemy e, float dt, float damageScale)
    {
        Vector3 flat = new Vector3(e.position.x, 0f, e.position.z);
        bool heroUp = player && player.Alive;
        Vector3 hero = heroUp ? new Vector3(player.Position.x, 0f, player.Position.z) : Vector3.zero;
        if (heroUp && (hero - flat).sqrMagnitude < TauntReach * TauntReach && bossMode != Chase)
        {
            train.Panel.Mark(e.position + Vector3.up * (e.type.size + 2.2f), TauntKey, 3);
            bool taunt = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                         || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
            if (taunt)
            {
                bossMode = Chase;
                bossModeLeft = 6f;
                train.Toast(L10n.T("miniboss.taunted"));
            }
        }
        float speed = e.type.speed, reach = e.type.size * 0.5f + 2.2f;
        Vector3 way = Vector3.zero;
        bossModeLeft -= dt;
        if (train.Calm) { bossMode = BackOff; bossModeLeft = Mathf.Max(bossModeLeft, 0.5f); } // a button game or a curve: it keeps off, like the rest
        if (bossMode == Ram)
        {
            if (bossWagon < 0 || bossWagon >= train.CarCount || !train.CarAlive(bossWagon)) bossWagon = RandomWagon(-1);
            if (bossWagon < 0) return;
            Vector3 to = train.CarPosition(bossWagon) - flat;
            to.y = 0f;
            if (to.magnitude <= reach)
            {
                // The ram: one heavy blow and a shockwave through the wagons beside it, then it backs off a moment.
                float early = Mathf.Min(1f, 0.25f + 0.08f * level); // the first ones come while the train has two wagons
                train.Damage(bossWagon, e.type.damage * damageScale * early);
                train.DamageArea(train.CarPosition(bossWagon), 7f, e.type.damage * damageScale * early * 0.35f);
                Burst("smoke", e.position, 2f);
                Burst("grenade", train.CarPosition(bossWagon) + Vector3.up, 2.2f);
                train.Shake(0.4f);
                bossMode = BackOff;
                bossModeLeft = 3.2f;
            }
            else way = to.normalized * speed * 1.3f; // a charge
        }
        else if (bossMode == Chase)
        {
            if (!heroUp || bossModeLeft <= 0f) { bossMode = Ram; bossWagon = RandomWagon(bossWagon); }
            else
            {
                Vector3 to = hero - flat;
                if (to.magnitude <= reach - 0.8f)
                {
                    player.Damage(e.type.damage * damageScale * 0.4f);
                    Burst("smoke", e.position, 1.6f);
                    bossMode = BackOff;
                    bossModeLeft = 2f;
                }
                else way = to.normalized * speed * 1.4f;
            }
        }
        else
        {
            // Backing off, away from the train; then on to the next target.
            Vector3 away = flat - train.Focus;
            away.y = 0f;
            way = away.normalized * speed;
            if (bossModeLeft <= 0f)
            {
                if (heroUp && Random.value < 0.33f) { bossMode = Chase; bossModeLeft = 4f; }
                else { bossMode = Ram; bossWagon = RandomWagon(bossWagon); }
            }
        }
        e.velocity = way;
        e.position += way * dt;
    }

    const float TauntReach = 3.5f; // only right up against it
    string TauntKey => player && player.UsingGamepad ? (Gamepad.current is DualShockGamepad ? "□" : "X") : "F";

    // A standing wagon at random, another than but if there is one; -1 if none stands.
    int RandomWagon(int but)
    {
        int count = 0, pick = -1;
        for (int i = 0; i < train.CarCount; i++)
            if (train.CarAlive(i) && i != but && Random.Range(0, ++count) == 0) pick = i;
        return pick >= 0 ? pick : but >= 0 && but < train.CarCount && train.CarAlive(but) ? but : -1;
    }

    static bool OnScreen(Vector3 at)
    {
        Vector3 seen = Camera.main.WorldToViewportPoint(at);
        return seen.z > 0f && seen.x > 0f && seen.x < 1f && seen.y > 0f && seen.y < 1f;
    }

    // A stampede: out of the picture on one side of the train, in a deep column, every one of them a wagon-seeker
    // (an odd pick is never a hunter).
    void Stampede(int count)
    {
        count = Mathf.Min(count, maxOnField - OnField);
        if (count <= 0) return;
        Vector3 along = train.Forward(train.CarCount / 2);
        along.y = 0f;
        along.Normalize();
        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 across = Vector3.Cross(Vector3.up, along) * side, start = train.Focus + across * RingRadius();
        int biome = train.Biome;
        var pool = System.Array.FindAll(Types, t => t.Fodder && t.firstLevel <= level && Here(t, biome));
        for (int i = 0; i < count; i++)
        {
            EnemyType type = pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : RankAndFile;
            Enemy e = Add(type, start + across * Random.Range(0f, 12f) + along * Random.Range(-14f, 14f));
            e.pick |= 1;
            e.punch = Mathf.Min(3f, 1.2f + 0.12f * level); // harder with the difficulty (soft early: the first one comes with two wagons)
        }
        train.Toast(L10n.T("stampede.warn"));
        train.Shake(0.2f);
    }

    // Out of the picture, this way from where the camera looks (the duel's raiders come in from there).
    public Vector3 OffScreen(Vector3 way) => train.Focus + way.normalized * RingRadius();

    float RingRadius()
    {
        Camera view = Camera.main;
        float across = view.orthographicSize * view.aspect, deep = view.orthographicSize / Mathf.Sin(view.transform.eulerAngles.x * Mathf.Deg2Rad);
        return Mathf.Max(spawnRadius, Mathf.Sqrt(across * across + deep * deep) + 4f);
    }

    // A random kind of goods appears near the train, floating, and drifts sideways.
    void SpawnLoot()
    {
        // Somewhere clearly on screen (whatever the view and zoom) but away from the hero, so he has to fly over and aim.
        // Takes the first candidate far enough, or failing that the farthest one tried.
        Vector3 spot = Vector3.zero;
        float farthest = -1f;
        var ground = new Plane(Vector3.up, Vector3.zero);
        for (int tries = 0; tries < 12 && farthest < lootDistance * lootDistance; tries++)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(Random.Range(0.12f, 0.88f), Random.Range(0.25f, 0.7f), 0f));
            if (!ground.Raycast(ray, out float hit)) continue;
            Vector3 candidate = ray.GetPoint(hit), fromHero = candidate - player.Position;
            float sqr = fromHero.x * fromHero.x + fromHero.z * fromHero.z;
            if (sqr <= farthest) continue;
            farthest = sqr;
            spot = candidate;
        }
        float angle = Random.value * Mathf.PI * 2f;
        Enemy e = Add(Loot[Random.Range(0, Loot.Length)], spot);
        e.position.y = lootHeight;
        e.velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.8f; // drifts slowly, then leaves along the same line
        e.timer = lootTime;
    }

    Enemy Add(EnemyType type, Vector3 ground)
    {
        Enemy e = enemyPool.Count > 0 ? enemyPool.Pop() : new Enemy();
        e.position = new Vector3(ground.x, type.size * 0.5f, ground.z);
        e.type = type;
        e.onUltimateHit = null;
        e.hp = e.maxHp = type.health * (1f + healthPerLevel * level) * enemyHealth * Balance.Scale;
        e.velocity = Vector3.zero;
        e.slowUntil = 0f;
        e.markedUntil = 0f;
        e.punch = 1f;
        e.pick = Random.Range(0, 1000);
        e.timer = type.interval;
        e.index = enemies.Count;
        enemies.Add(e);
        return e;
    }

    // The boss's rig and its rockets: enemies as far as the hero's and the turrets' guns are concerned (they can
    // lock on and shoot them), but positioned by BossEvent, not by the AI above. hp is set by the caller.
    public Enemy AddProjectile(EnemyType type, Vector3 at, float hp)
    {
        Enemy e = enemyPool.Count > 0 ? enemyPool.Pop() : new Enemy();
        e.position = at;
        e.type = type;
        e.onUltimateHit = null;
        e.hp = e.maxHp = hp;
        e.velocity = Vector3.zero;
        e.slowUntil = 0f;
        e.timer = 0f;
        e.index = enemies.Count;
        enemies.Add(e);
        return e;
    }

    // Take a boss piece back out of the field (a rocket that hit, or one that came down).
    public void Despawn(Enemy e)
    {
        if (e != null && e.index >= 0 && e.index < enemies.Count && enemies[e.index] == e) RemoveEnemy(e.index);
    }

    // loot: also consider floating goods (only the hero aims at those).
    public Enemy Nearest(Vector3 from, float range, List<Enemy> except = null, bool loot = false)
    {
        Enemy nearest = null;
        float best = range * range;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (!loot && (enemies[i].type.loot >= 0 || enemies[i].type.heroOnly)) continue;
            float d = (enemies[i].position - from).sqrMagnitude;
            if (d < best && (except == null || !except.Contains(enemies[i]))) { best = d; nearest = enemies[i]; }
        }
        return nearest;
    }

    // The one a wagon's gun picks in its reach, by the weapon's aim (Weapon.AimNearest...). Never loot or hero-only.
    public Enemy Pick(Vector3 from, Weapon weapon)
    {
        if (weapon.aim == Weapon.AimNearest) return Nearest(from, weapon.range);
        Enemy pick = null;
        float best = float.MaxValue, reach = weapon.range * weapon.range;
        int seen = 0;
        foreach (Enemy e in enemies)
        {
            if (e.type.loot >= 0 || e.type.heroOnly || (e.position - from).sqrMagnitude > reach) continue;
            float score;
            if (weapon.aim == Weapon.AimRandom) { if (Random.Range(0, ++seen) == 0) pick = e; continue; } // even odds for each
            else if (weapon.aim == Weapon.AimStrongest) score = -e.hp;
            else { train.ClosestWagon(e.position, out Vector3 toWagon); score = toWagon.sqrMagnitude; } // AimDanger
            if (score < best) { best = score; pick = e; }
        }
        return pick;
    }

    // The nearest of what only the hero can bring down (the truck's bombs), if one is in reach: it goes first.
    public Enemy Threat(Vector3 from, float range)
    {
        Enemy nearest = null;
        float best = range * range;
        foreach (Enemy e in enemies)
        {
            if (!e.type.heroOnly) continue;
            float d = (e.position - from).sqrMagnitude;
            if (d < best) { best = d; nearest = e; }
        }
        return nearest;
    }

    // Enemy the aim line points at: the nearest one inside the aim-assist cone (or right on top of the shooter).
    public Enemy Aimed(Vector3 from, Vector3 direction, float range, float minDot)
    {
        Enemy aimed = null;
        float best = range * range;
        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 to = enemies[i].position - from;
            to.y = 0f;
            float d = to.sqrMagnitude;
            if (d >= best || (d > 4f && Vector3.Dot(to, direction) < minDot * Mathf.Sqrt(d))) continue;
            best = d;
            aimed = enemies[i];
        }
        return aimed;
    }

    // Next enemy in range going clockwise (direction 1) or counter-clockwise (-1) from the current one.
    public Enemy Cycle(Vector3 from, Enemy current, float range, int direction)
    {
        if (current == null) return Nearest(from, range, null, true);
        Vector3 start = current.position - from;
        start.y = 0f;
        Enemy pick = null;
        float best = 360f;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == current) continue;
            Vector3 to = enemies[i].position - from;
            to.y = 0f;
            if (to.sqrMagnitude > range * range) continue;
            float angle = Vector3.SignedAngle(start, to, Vector3.up) * direction;
            if (angle <= 0f) angle += 360f;
            if (angle >= best) continue;
            best = angle;
            pick = enemies[i];
        }
        return pick;
    }

    const float OrbitSpeed = 220f, OrbitRadius = 3.2f; // degrees a second; distance from the hero

    // Each kind of shot its own shape and way of moving, so the weapons read apart at a glance.
    void DrawBullet(Bullet b, float clock)
    {
        if (b.style == Weapon.Orbit)
        {
            // A glowing orb, tumbling, with a smaller one trailing it round the circle; the Orb Shield's are bigger.
            b.batch.Add(b.position, Vector3.one * (b.level >= Weapon.Evolved ? 1.3f : 0.75f), Quaternion.Euler(clock * 300f, clock * 200f, 0f));
            Vector3 behind = b.position - (player ? player.Position : b.from);
            b.batch.Add(b.position + Vector3.Cross(behind.normalized, Vector3.up) * 0.6f, Vector3.one * 0.35f, Quaternion.Euler(0f, clock * 400f, 45f));
            return;
        }
        if (b.style == Weapon.Slash)
        {
            DrawSlash(b, clock);
            return;
        }
        if (b.style == Weapon.Comet)
        {
            // A white-hot head in a blue blaze, a long tail of pieces shrinking and flickering behind it.
            Vector3 back = -b.velocity.normalized;
            Quaternion heading = Quaternion.LookRotation(b.velocity);
            cometCore.Add(b.position, Vector3.one * 1.1f, Quaternion.Euler(clock * 400f, clock * 300f, 0f));
            b.batch.Add(b.position, Vector3.one * 1.7f, Quaternion.Euler(clock * -250f, clock * 180f, 45f));
            for (int k = 1; k <= 7; k++)
            {
                float s = 1.5f * (1f - k / 8f), flick = 0.8f + 0.3f * Mathf.Sin(clock * 30f + k * 1.7f);
                b.batch.Add(b.position + back * (k * 0.9f) + Random.insideUnitSphere * 0.15f, new Vector3(s, s, 1.2f) * flick, heading);
            }
            return;
        }
        if (b.style == Weapon.Shell)
        {
            // A glowing shell falling fast, and a red mark on the ground where it will land, growing as it nears.
            b.batch.Add(b.position, new Vector3(0.6f, 2.2f, 0.6f), Quaternion.identity);
            b.batch.Add(b.position + Vector3.up * 1.8f, new Vector3(0.25f, 2.4f, 0.25f), Quaternion.identity);
            float near = Mathf.Clamp01(1f - b.position.y / 18f);
            shellMark.Add(new Vector3(b.position.x, 0.08f, b.position.z), new Vector3(1f, 0.02f, 1f) * (1.5f + 3f * near), Quaternion.identity);
            return;
        }
        if (b.style == Weapon.Boomerang)
        {
            // A spinning cross blade; the Chakram is a bigger star of four.
            Quaternion spin = Quaternion.Euler(0f, clock * 1100f, 0f);
            float span = b.level >= Weapon.Evolved ? 3.6f : 2.4f;
            b.batch.Add(b.position, new Vector3(span, 0.16f, 0.4f), spin);
            b.batch.Add(b.position, new Vector3(0.4f, 0.16f, span), spin);
            if (b.level >= Weapon.Evolved)
            {
                b.batch.Add(b.position, new Vector3(span * 0.8f, 0.16f, 0.35f), spin * Quaternion.Euler(0f, 45f, 0f));
                b.batch.Add(b.position, new Vector3(0.35f, 0.16f, span * 0.8f), spin * Quaternion.Euler(0f, 45f, 0f));
            }
            return;
        }
        if ((b.target == null && !b.free) || b.velocity.sqrMagnitude <= 1f)
        {
            b.batch.Add(b.position, b.size, b.rotation); // flashes, bolts, rings: as they were made
            return;
        }
        Vector3 at = b.position, forward = b.velocity.normalized;
        Quaternion way = Quaternion.LookRotation(b.velocity);
        float thick = b.size.x * 1.6f, length = thick + b.velocity.magnitude * 0.03f;
        switch (b.style)
        {
            case Weapon.Lob:
            {
                // Up and over: a tumbling ball on an arc as high as a third of the throw.
                float done = Flat(at - b.from).magnitude, left = b.target != null ? Flat(b.target.position - at).magnitude : 0f;
                at.y += Mathf.Sin(Mathf.PI * done / Mathf.Max(0.01f, done + left)) * (done + left) * 0.33f;
                b.batch.Add(at, Vector3.one * 0.55f, Quaternion.Euler(clock * 500f, clock * 300f, 0f));
                return;
            }
            case Weapon.Wobble:
                at += Vector3.Cross(Vector3.up, forward) * (Mathf.Sin(b.age * 30f + b.phase) * 0.45f); // the stream snakes
                break;
            case Weapon.Flame:
            {
                // Puffs that swell, tumble and drift apart as they go.
                float grow = 0.3f + b.age * 2.2f;
                at += Vector3.Cross(Vector3.up, forward) * (b.phase * b.age * 2f) + Vector3.up * (b.age * 1.5f);
                b.batch.Add(at, Vector3.one * grow, Quaternion.Euler(b.phase * 90f + clock * 200f, clock * 160f, 0f));
                return;
            }
            case Weapon.Shard:
            {
                // An ice crystal spinning round its own way.
                Quaternion spin = way * Quaternion.Euler(0f, 0f, clock * 900f);
                b.batch.Add(at, new Vector3(0.22f, 0.5f, 1.3f), spin);
                b.batch.Add(at, new Vector3(0.5f, 0.22f, 0.9f), spin);
                return;
            }
            case Weapon.Swirl:
                Burst("smoke", at - forward * 0.6f, 0.25f); // a rocket leaves a smoke trail
                thick *= 1.5f;
                break;
        }
        // A glowing streak along its way, the faster the longer, and a thin tail behind it.
        b.batch.Add(at, new Vector3(thick, thick, length), way);
        b.batch.Add(at - forward * length, new Vector3(thick * 0.45f, thick * 0.45f, length * 1.6f), way);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // The Masamune's cut (phase 0) or, at its last level, its dragon (phase 1).
    void Cut(Vector3 from, Vector3 dir, Weapon weapon, int level)
    {
        bool dragon = level >= Weapon.Evolved;
        Bullet b = Spawn(from, Vector3.one, weapon.Batch, 9f);
        b.weapon = weapon;
        b.level = level;
        b.hero = heroHit;
        b.style = Weapon.Slash;
        b.dir = dir;
        b.phase = dragon ? 1f : 0f;
        // The dragon flies far and slow, and lives until its tail has gone by too.
        b.span = (dragon ? (weapon.range * 2.5f + DragonLength) / (weapon.speed * DragonPace) : weapon.range / weapon.speed) * Duration;
        Burst("spark", from + dir, dragon ? 4f : 1.4f);
        if (dragon) train.Shake(0.35f);
    }

    const int DragonPieces = 34;
    const float DragonStep = 0.9f, DragonLength = DragonPieces * DragonStep, DragonPace = 0.8f;

    // How far along its way the cut or the dragon's head is.
    static float Along(Bullet b) => b.weapon.speed * b.age * (b.phase > 0f ? DragonPace : 1f);

    // How far the dragon is off its straight way, d metres along it: a zigzag, 4 m each side, a turn every 8 m.
    static float Zig(Bullet b, float d) => b.phase > 0f ? (Mathf.PingPong(d / 4f + 1f, 2f) - 1f) * 4f : 0f;

    // Half the width the cut sweeps: it widens as it flies; the dragon's jaws are wide all along.
    static float SlashHalf(Bullet b) => b.phase > 0f ? 3.5f : (1.6f + 0.25f * b.level) * (0.7f + 0.5f * b.age / b.span);

    void DrawSlash(Bullet b, float clock)
    {
        Vector3 side = Vector3.Cross(Vector3.up, b.dir);
        if (b.phase <= 0f)
        {
            // A crescent of light, bowed forward, thick in the middle and thin at the tips.
            float half = SlashHalf(b);
            const int Pieces = 11;
            for (int s = 0; s < Pieces; s++)
            {
                float u = s / (Pieces - 1f) * 2f - 1f;
                Vector3 at = b.position + Vector3.up * 0.8f + side * (u * half) - b.dir * (u * u * half * 0.55f);
                Vector3 tangent = side - b.dir * (1.1f * u);
                float thick = 0.45f * (1f - u * u * 0.75f);
                b.batch.Add(at, new Vector3(thick, 0.1f, half * 2.6f / (Pieces - 1)), Quaternion.LookRotation(tangent));
            }
            return;
        }
        // The dragon: a great serpent of blue fire. A horned head with open jaws and a flaming mane leads a long,
        // thick body that follows its zigzag, rising and falling, wrapped in flickering flames, thinning to the tail.
        float along = Along(b);
        Vector3 Point(float d) => b.from + b.dir * d + side * Zig(b, d) + Vector3.up * (2.6f + 1.1f * Mathf.Sin(d * 0.45f - clock * 3f));
        Vector3 head = Point(along);
        Quaternion face = Quaternion.LookRotation(Point(along + 0.6f) - head);
        float jaw = 20f + 12f * Mathf.Sin(clock * 5f); // the jaws snap open and shut
        dragonDark.Add(head, new Vector3(2.2f, 1.5f, 2.6f), face);                                                          // skull
        dragonScale.Add(head + face * new Vector3(0f, 0.15f, 1.9f), new Vector3(1.6f, 0.65f, 2f), face * Quaternion.Euler(-jaw * 0.3f, 0f, 0f)); // upper jaw
        dragonScale.Add(head + face * new Vector3(0f, -0.55f, 1.6f), new Vector3(1.3f, 0.4f, 1.8f), face * Quaternion.Euler(jaw, 0f, 0f));     // lower jaw
        dragonFire.Add(head + face * new Vector3(0f, -0.15f, 2.4f), new Vector3(0.9f, 0.35f, 0.9f), face);                  // the glow in its throat
        foreach (float x in new[] { -1f, 1f })
        {
            dragonFire.Add(head + face * new Vector3(0.6f * x, 0.45f, 0.9f), new Vector3(0.35f, 0.18f, 0.35f), face);       // eyes
            dragonScale.Add(head + face * new Vector3(0.75f * x, 1.2f, -1.1f), new Vector3(0.28f, 0.28f, 2.6f), face * Quaternion.Euler(-30f, 18f * x, 0f)); // horns
            dragonScale.Add(head + face * new Vector3(0.9f * x, -0.4f, 2.2f), new Vector3(0.12f, 0.12f, 1.6f), face * Quaternion.Euler(10f, 25f * x, 0f)); // whiskers
        }
        for (int m = 0; m < 6; m++) // the mane: flames streaming back from the head
        {
            float a = (m - 2.5f) * 28f, flick = 1f + 0.25f * Mathf.Sin(clock * 18f + m * 1.7f);
            dragonFire.Add(head + face * Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 1f, -1.2f), new Vector3(0.3f, 0.3f, 2.2f * flick), face * Quaternion.Euler(-25f, 0f, a));
        }
        for (int s = 1; s <= DragonPieces; s++)
        {
            float d = along - s * DragonStep;
            if (d < 0f) break;
            Vector3 at = Point(d);
            float k = s / (float)DragonPieces, thick = Mathf.Lerp(2f, 0.35f, k * k);
            Quaternion q = Quaternion.LookRotation(Point(d + DragonStep) - at) * Quaternion.Euler(0f, 0f, s * 25f + clock * 90f);
            dragonDark.Add(at, new Vector3(thick * 0.8f, thick * 0.8f, 1.2f), q);
            dragonScale.Add(at, new Vector3(thick, thick * 0.55f, 1f), q); // scales
            // Flames licking off it, two a piece, flickering.
            for (int f = 0; f < 2; f++)
            {
                float flick = 0.7f + 0.5f * Mathf.Abs(Mathf.Sin(clock * 14f + s * 1.3f + f * 2f));
                Quaternion out_ = q * Quaternion.Euler(0f, 0f, f * 180f + 45f);
                dragonFire.Add(at + out_ * new Vector3(0f, thick * 0.55f, -0.3f), new Vector3(0.22f * thick, thick * 0.9f * flick, 0.5f), out_ * Quaternion.Euler(-40f, 0f, 0f));
            }
        }
    }

    // A piercing shot (boomerang, orbit) hurts everyone it touches, each once per pass.
    void Pierce(Bullet b, float radius)
    {
        radius *= Area;
        for (int j = enemies.Count - 1; j >= 0; j--)
        {
            Enemy e = enemies[j];
            float reach = radius + e.type.size * 0.5f, dx = e.position.x - b.position.x, dz = e.position.z - b.position.z;
            if (dx * dx + dz * dz > reach * reach || b.hits.Contains(e.id)) continue;
            b.hits.Add(e.id);
            Burst("spark", e.position, 0.7f);
            heroHit = b.hero;
            source = b.weapon;
            Hurt(e, b.weapon.Damage(b.level) * Power * (b.style == Weapon.Slash && b.phase > 0f ? 2f : 1f)); // the dragon bites twice as hard
            heroHit = false;
        }
    }

    // A jagged line of glowing pieces from one point to another: lightning, a chain's jump, a fast shot's tracer.
    void Bolt(Vector3 from, Vector3 to, Batch batch, int pieces, float jag, float thick = 0.3f, float life = 0.12f)
    {
        Vector3 last = from;
        for (int k = 1; k <= pieces; k++)
        {
            Vector3 next = k == pieces ? to : Vector3.Lerp(from, to, k / (float)pieces) + Random.insideUnitSphere * jag;
            Vector3 d = next - last;
            if (d.sqrMagnitude > 0.0001f) Spawn((last + next) * 0.5f, new Vector3(thick, thick, d.magnitude), batch, life, Quaternion.LookRotation(d));
            last = next;
        }
    }

    // A pulse: a ring of glowing dashes racing out to the edge of its reach.
    void Ring(Vector3 at, float radius, Batch batch)
    {
        const int Dashes = 28;
        const float Seconds = 0.3f;
        for (int k = 0; k < Dashes; k++)
        {
            Vector3 out_ = Quaternion.Euler(0f, k * 360f / Dashes, 0f) * Vector3.forward;
            Bullet b = Spawn(at + out_ * 0.5f, new Vector3(0.25f, 0.25f, radius * 0.22f), batch, Seconds, Quaternion.LookRotation(Vector3.Cross(Vector3.up, out_)));
            b.velocity = out_ * (radius / Seconds);
        }
    }

    // The shotgun's spray: small pellets fanning out round the real shot.
    void Pellets(Vector3 from, Vector3 toward, Weapon weapon)
    {
        for (int k = 0; k < 5; k++)
        {
            Bullet b = Spawn(from, Vector3.one * 0.18f, weapon.Batch, weapon.range / weapon.speed * Random.Range(0.6f, 1f));
            b.velocity = Quaternion.Euler(0f, Random.Range(-22f, 22f), 0f) * toward * weapon.speed * Random.Range(0.8f, 1.1f);
        }
    }

    // Orbs circling the hero for one turn of the weapon; the old ones make way so they never pile up.
    void Orbit(Weapon weapon, int level)
    {
        if (!player) return;
        for (int i = bullets.Count - 1; i >= 0; i--)
            if (bullets[i].style == Weapon.Orbit && bullets[i].weapon == weapon) RemoveBullet(i);
        int count = weapon.Targets(level) + (level >= Weapon.Evolved ? 3 : 1) + Amount; // the Orb Shield: a full ring
        float span = weapon.Interval(level) * 1.05f;
        for (int k = 0; k < count; k++)
        {
            Bullet b = Spawn(player.Position, Vector3.one, weapon.Batch, span);
            b.weapon = weapon;
            b.level = level;
            b.hero = heroHit;
            b.style = Weapon.Orbit;
            b.span = span;
            b.phase = k * 360f / count;
        }
    }

    // A boomerang thrown at an enemy: out to the end of its reach and back, through everyone on the way.
    void Throw(Vector3 from, Enemy at, Weapon weapon, int level)
    {
        Bullet b = Spawn(from, Vector3.one, weapon.Batch, 9f);
        b.weapon = weapon;
        b.level = level;
        b.hero = heroHit;
        b.style = Weapon.Boomerang;
        b.span = 2f * weapon.range / weapon.speed * 1.4f * Duration;
        b.dir = FlatDir(at, from);
    }

    // One trigger pull of a weapon: a pulse, or one shot at `first` plus any extra shots at the next nearest enemies.
    // hero: the shots come from the hero, so they can break floating goods.
    // The hero's upgrades that reach every weapon (TrainSim.Passives): wider blasts and sweeps, more shots, longer effects.
    float Area => 1f + 0.12f * train.PassiveLevel("area");
    // The Amount passive, the hero's weapon only (Volley's extra): two more shots at most, as Vampire Survivors' Duplicator.
    int Amount => Mathf.Min(2, train.PassiveLevel("amount"));
    float Duration => 1f + 0.2f * train.PassiveLevel("duration");

    public void Volley(Vector3 from, Enemy first, Weapon weapon, int level, bool hero = false)
    {
        int extra = hero ? Amount : 0; // the Amount passive: the hero's own weapon only, not every gun on the train
        bool evolved = level >= Weapon.Evolved;
        if ((first != null || weapon.pulse) && weapon.style != Weapon.Orbit && weapon.style != Weapon.Slash) Muzzle(from, FlatDir(first, from), weapon);
        heroHit = hero;
        if (weapon.pulse)
        {
            float reach = weapon.range;
            if (evolved)
            {
                // A pulse evolved (Nova's Solar Heart, the wagon's Vital Pulse): wider, and every beat mends the
                // train, and the hero if it is hers.
                reach *= 1.4f;
                float mend = weapon.Damage(level) * 0.08f;
                train.Repair(mend);
                if (player && hero) player.Heal(mend);
                Ring(from, reach * 0.7f * Area, healRing);
            }
            Ring(from, reach * Area, weapon.Batch);
            Explode(from, reach, weapon.Damage(level) * Power, weapon);
        }
        else if (weapon.style == Weapon.Orbit) Orbit(weapon, level);
        else if (weapon.style == Weapon.Slash) { if (first != null) Cut(from, FlatDir(first, from), weapon, level); }
        else
        {
            if (weapon.beam)
            {
                if (first != null)
                {
                    Vector3 flat = first.position - from;
                    flat.y = 0f;
                    int ways = weapon.Directions(level);
                    if (flat == Vector3.zero) { }
                    else if (evolved) // the Prism: three thick beams fanned a little, red, green and blue
                        for (int k = 0; k < 3; k++) Beam(from, Quaternion.Euler(0f, (k - 1) * 9f, 0f) * flat.normalized, weapon, level, prism[k]);
                    else
                        for (int way = 0; way < ways; way++) Beam(from, Quaternion.Euler(0f, Weapon.Spread(way), 0f) * flat.normalized, weapon, level);
                }
            }
            else
            {
                int count = weapon.Targets(level) + extra;
                picked.Clear();
                for (Enemy e = first; e != null && picked.Count < count; e = Nearest(from, weapon.range, picked))
                {
                    picked.Add(e);
                    if (weapon.strike) Strike(e, weapon, level);
                    else if (weapon.style == Weapon.Boomerang) Throw(from, e, weapon, level);
                    else if (evolved && weapon.id == "rail") Beam(from, FlatDir(e, from), weapon, level); // the Ion Cannon
                    else if (evolved && weapon.id == "chain") Web(e, weapon, level);                      // the Power Grid
                    else
                    {
                        Fire(from, e, weapon, level);
                        if (evolved && weapon.style == Weapon.Swirl) bullets[bullets.Count - 1].split = true; // the Missile Swarm
                        if (evolved && weapon.id == "sniper") Bolt(from, e.position, weapon.Batch, 1, 0f, 0.35f, 0.18f); // the Hawk Eye's streak
                        if (evolved && hero && weapon.id == "smg") // the Drone Squad: each drone fires as well
                            for (int k = 0; k < 2; k++) Fire(Drone(k), e, weapon, level);
                        if (weapon.style == Weapon.Pellets) Pellets(from, FlatDir(e, from), weapon);
                    }
                }
                if (evolved && weapon.id == "minigun" && first != null)
                {
                    // The Lead Hurricane: two arms of a spiral of bullets turning out from the wagon, besides the aimed stream.
                    spiral += 23f;
                    for (int k = 0; k < 2; k++)
                    {
                        Bullet s = Spawn(from, Vector3.one * 0.25f, weapon.Batch, weapon.range / weapon.speed);
                        s.free = true;
                        s.weapon = weapon;
                        s.velocity = Quaternion.Euler(0f, spiral + k * 180f, 0f) * Vector3.forward * weapon.speed;
                        s.style = Weapon.Straight;
                        s.level = level;
                        s.hero = heroHit;
                    }
                }
                if (evolved && weapon.id == "blaster" && first != null && Time.time >= nextComet)
                {
                    // The Plasma Comet: every second or so, a comet along the aim, through the crowd.
                    nextComet = Time.time + 1.1f;
                    Vector3 aim = FlatDir(first, from);
                    for (int k = 0; k <= extra; k++)
                    {
                        Bullet c = Spawn(from, Vector3.one, weapon.Batch, 26f / 22f * Duration);
                        c.weapon = weapon;
                        c.level = level;
                        c.hero = heroHit;
                        c.style = Weapon.Comet;
                        c.velocity = Quaternion.Euler(0f, (k - extra * 0.5f) * 12f, 0f) * aim * 22f;
                    }
                    Burst("spark", from + aim, 2f);
                }
                if (evolved && weapon.id == "turret" && first != null && Random.value < 0.25f)
                {
                    // The Barrage: now and then a shell called down from the sky near its target.
                    for (int k = 0; k < 1 + extra; k++)
                    {
                        Vector3 at = first.position + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                        Bullet s = Spawn(new Vector3(at.x, 18f, at.z), Vector3.one, weapon.Batch, 9f);
                        s.weapon = weapon;
                        s.level = level;
                        s.hero = heroHit;
                        s.style = Weapon.Shell;
                        s.velocity = Vector3.down * 36f;
                    }
                }
                if (evolved && weapon.style == Weapon.Flame && first != null)
                {
                    // The Ring of Fire: a ring of flame thrown out all round as well.
                    for (int k = 0; k < 12; k++)
                    {
                        Bullet f = Spawn(from, Vector3.one * 0.25f, weapon.Batch, weapon.range / weapon.speed);
                        f.free = true;
                        f.weapon = weapon;
                        f.velocity = Quaternion.Euler(0f, k * 30f, 0f) * Vector3.forward * weapon.speed;
                        f.style = Weapon.Flame;
                        f.phase = Random.Range(-1f, 1f);
                        f.level = level;
                        f.hero = heroHit;
                    }
                }
                // The fan: the other ways leave evenly round the aimed one, at nobody in particular.
                int ways = weapon.Directions(level);
                Vector3 aimed = FlatDir(first, from);
                for (int way = 1; way < ways && first != null; way++)
                {
                    Bullet b = Spawn(from, Vector3.one * 0.25f, weapon.Batch, weapon.range / weapon.speed);
                    b.free = true;
                    b.weapon = weapon;
                    b.velocity = Quaternion.Euler(0f, Weapon.Spread(way), 0f) * aimed * weapon.speed;
                    b.style = weapon.style;
                    b.phase = Random.Range(-1f, 1f);
                    b.level = level;
                    b.hero = heroHit;
                    b.chain = weapon.Chain(level);
                }
            }
        }
        heroHit = false;
    }

    // The barrel flare that every shot leaves: a spark from the shared pool plus a quick flash in the
    // weapon's colour, drawn by the weapon's own batch (see FlashMuzzles).
    public void Muzzle(Vector3 at, Vector3 forward, Weapon weapon)
    {
        float size = FlashSize(weapon);
        Burst("spark", at + forward * (size * 0.5f), size);
        AddFlash(at, forward, weapon.Batch, size);
    }

    // A quick star of cubes in the given colour and size, drawn through `batch` (see FlashMuzzles).
    // The station's heavy guns call this too, with their own bolt colour and a bigger size.
    public void AddFlash(Vector3 at, Vector3 forward, Batch batch, float size) =>
        flashes.Add(new MuzzleFlash { at = at, forward = forward, batch = batch, size = size, life = FlashLife });

    static Vector3 FlatDir(Enemy first, Vector3 from)
    {
        Vector3 to = first != null ? first.position - from : Vector3.forward;
        to.y = 0f;
        return to.sqrMagnitude > 0.0001f ? to.normalized : Vector3.forward;
    }

    static float FlashSize(Weapon weapon) => Mathf.Clamp(0.3f + weapon.damage * 0.06f, 0.3f, 0.9f);

    // Older flashes shrink to nothing, then vanish.
    void FlashMuzzles()
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            MuzzleFlash f = flashes[i];
            if (!train.UltimatePresenting) f.life -= Time.unscaledDeltaTime;
            if (f.life <= 0f) { flashes.RemoveAt(i); continue; }
            flashes[i] = f;
            DrawFlash(f.batch, f.at, f.forward, f.size * (f.life / FlashLife));
        }
    }

    // Three cubes make a small star: one down the barrel, one flat across it, one upright. Drawn through the
    // batch's own colour.
    static void DrawFlash(Batch batch, Vector3 at, Vector3 forward, float size)
    {
        Quaternion face = Quaternion.LookRotation(forward);
        batch.Add(at + forward * (size * 0.5f), new Vector3(size, size, size * 2.2f), face);
        batch.Add(at, new Vector3(size * 1.6f, size * 1.6f, size * 0.6f), face);
        batch.Add(at, new Vector3(size * 0.5f, size * 2.4f, size * 0.5f), face);
    }

    public void Fire(Vector3 from, Enemy target, Weapon weapon, int level)
    {
        Bullet b = Spawn(from, Vector3.one * 0.25f, weapon.Batch, bulletLife);
        b.target = target;
        b.targetId = target.id;
        b.weapon = weapon;
        b.velocity = (target.position - from).normalized * weapon.speed;
        b.level = level;
        b.hero = heroHit;
        b.chain = weapon.Chain(level);
        b.style = weapon.style;
        b.phase = weapon.style == Weapon.Swirl ? (Random.value < 0.5f ? -1f : 1f) : Random.Range(-1f, 1f);
        if (weapon.style == Weapon.Swirl) b.life *= 1.5f; // the curl makes the way longer
        if (weapon.speed >= 100f) Bolt(from, target.position, weapon.Batch, 1, 0f, 0.12f, 0.1f); // a fast shot leaves a thin tracer
    }

    // Lightning: instant hit, drawn as a jagged bolt from the sky.
    public void Strike(Enemy target, Weapon weapon, int level)
    {
        source = weapon;
        Bolt(target.position + new Vector3(Random.Range(-2f, 2f), 14f, Random.Range(-2f, 2f)), target.position, weapon.Batch, 5, 1.2f, 0.4f, 0.15f);
        Burst(weapon.id, target.position, 1.5f);
        if (level >= Weapon.Evolved)
        {
            // The Tempest: the bolt forks on to two more around it.
            chainSkip.Clear();
            chainSkip.Add(target);
            for (int k = 0; k < 2; k++)
            {
                Enemy next = Nearest(target.position, 9f * Area, chainSkip);
                if (next == null) break;
                chainSkip.Add(next);
                Bolt(target.position + Vector3.up, next.position, weapon.Batch, 4, 0.8f, 0.3f, 0.15f);
                Hurt(next, weapon.Damage(level) * Power);
            }
        }
        Hurt(target, weapon.Damage(level) * Power);
    }

    // Weapon blast: damages every enemy within radius (measured on the ground). Only the VFX shows it.
    public void Explode(Vector3 center, float radius, float damage, Weapon weapon)
    {
        source = weapon;
        radius *= Area;
        Burst(weapon.id, center, radius * BurstSize);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Vector3 d = enemies[i].position - center;
            if (d.x * d.x + d.z * d.z <= radius * radius) Hurt(enemies[i], damage);
        }
    }

    // Laser: one instant straight ray. Weak per enemy, but it crosses every enemy standing on the line.
    public void Beam(Vector3 from, Vector3 direction, Weapon weapon, int level, Batch look = null)
    {
        source = weapon;
        bool evolved = level >= Weapon.Evolved;
        float range = weapon.range * (evolved ? 2f : 1f), damage = weapon.Damage(level) * Power, thick = evolved ? 0.6f : 0.28f;
        Spawn(from + direction * (range * 0.5f), new Vector3(thick, thick, range), look ?? weapon.Batch, evolved ? 0.14f : 0.08f, Quaternion.LookRotation(direction));
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            Vector3 to = e.position - from;
            to.y = 0f;
            float along = Vector3.Dot(to, direction);
            if (along < 0f || along > range) continue;
            Vector3 side = e.position - (from + direction * along);
            side.y = 0f;
            float radius = e.type.size * 0.5f;
            if (side.sqrMagnitude <= radius * radius) Hurt(e, damage);
        }
    }

    class Pool
    {
        public GameObject prefab;
        public GameObject[] copies = new GameObject[6];
        public int next;
    }

    readonly Dictionary<string, Pool> bursts = new();

    // A one-shot VFX Graph effect from Resources/Vfx/<effect>, sized to the blast. Each effect has a few
    // copies that are replayed in turn; an effect with no prefab (most weapons) simply does nothing.
    public GameObject Burst(string effect, Vector3 at, float size) // the effect's copy, for whoever has to play it by hand
    {
        if (!bursts.TryGetValue(effect, out Pool pool))
            bursts[effect] = pool = new Pool { prefab = Resources.Load<GameObject>("Vfx/" + effect) };
        if (!pool.prefab) return null;
        pool.next = (pool.next + 1) % pool.copies.Length;
        GameObject copy = pool.copies[pool.next];
        if (!copy) pool.copies[pool.next] = copy = Instantiate(pool.prefab);
        copy.transform.SetPositionAndRotation(at, Quaternion.identity);
        copy.transform.localScale = Vector3.one * size;
        foreach (var graph in copy.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>())
        {
            graph.Reinit();
            graph.Play();
        }
        // A one-shot effect has to end by itself: fire carries a looping ParticleSystem that would otherwise
        // burn for the rest of the run. Stop it, clear the loop and replay, so it dies out after a single pass.
        foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>())
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            system.Play();
        }
        return copy;
    }

    // Enemy death explosions hurt wagons and other enemies; those deaths can queue more blasts.
    void ResolveBlasts(float damageScale)
    {
        for (int n = 0; n < blasts.Count; n++)
        {
            Blast blast = blasts[n];
            float radius = blast.type.explode;
            Burst("enemy", blast.center, radius * BurstSize);
            train.DamageArea(blast.center, radius, blast.type.explodeDamage * damageScale); Tally("BLAST " + blast.type.name, blast.type.explodeDamage * damageScale);
            if (player.Alive && (player.Position - blast.center).sqrMagnitude <= radius * radius)
                player.Damage(blast.type.explodeDamage * damageScale);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Vector3 d = enemies[i].position - blast.center;
                if (d.x * d.x + d.z * d.z <= radius * radius) { source = null; Hurt(enemies[i], blast.type.explodeDamage * Balance.Scale, false); }
            }
        }
        blasts.Clear();
    }

    // What is about to hurt, shown before it does: a ring on the ground, the size of its blast, round everything that
    // blows up when it dies or reaches a wagon, once it is close to the train or the hero; it beats faster the closer.
    // The ones worth an arrow at the edge of the screen when out of it (Hud): mini bosses and anything big or tough.
    public void Threats(List<(Vector3 at, bool boss)> into)
    {
        into.Clear();
        foreach (Enemy e in enemies)
        {
            EnemyType t = e.type;
            if (t.projectile || t.loot >= 0) continue;
            bool boss = IsMiniBoss(t);
            if (boss || (!t.Fodder && t.size >= 1.6f)) into.Add((e.position, boss));
        }
    }

    // A kill breaks apart: chunks in its colour fly out, tumble, bounce and shrink away, and a white flash where it was.
    struct Chunk { public Vector3 at, velocity, axis; public float born, life, size, spin; public Batch look; }
    readonly List<Chunk> chunks = new();
    readonly Dictionary<Color, Batch> chunkLooks = new();
    Batch popLook;
    const int MaxChunks = 360;

    void Shatter(EnemyType t, Vector3 at)
    {
        float now = Time.time;
        if (!chunkLooks.TryGetValue(t.color, out Batch look)) chunkLooks[t.color] = look = new Batch(t.color, false);
        int count = t.Fodder ? 4 : t.size > 2.5f ? 12 : 7;
        for (int k = 0; k < count && chunks.Count < MaxChunks; k++)
        {
            Vector3 fly = Random.onUnitSphere;
            fly.y = Mathf.Abs(fly.y) + 0.4f;
            chunks.Add(new Chunk
            {
                at = at + Vector3.up * t.size * 0.5f, velocity = fly * Random.Range(5f, 10f) * Mathf.Sqrt(t.size), axis = Random.onUnitSphere,
                born = now, life = Random.Range(0.6f, 1f), size = t.size * Random.Range(0.12f, 0.24f), spin = Random.Range(360f, 900f), look = look,
            });
        }
        if (chunks.Count < MaxChunks && Comfort.Flashes) chunks.Add(new Chunk { at = at + Vector3.up * t.size * 0.5f, born = now, life = 0.16f, size = t.size * 1.6f, look = null }); // the flash
    }

    void Shards(float now)
    {
        float dt = Time.deltaTime;
        popLook ??= new Batch(new Color(1f, 0.92f, 0.7f), false, Resources.GetBuiltinResource<Mesh>("Sphere.fbx"), glow: 3f, plain: true);
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            Chunk c = chunks[i];
            float age = (now - c.born) / c.life;
            if (age >= 1f) { chunks[i] = chunks[^1]; chunks.RemoveAt(chunks.Count - 1); continue; }
            if (c.look == null) { popLook.Add(c.at, Vector3.one * c.size * (0.4f + age) * (1f - age)); continue; }
            c.velocity += Vector3.down * 22f * dt;
            c.at += c.velocity * dt;
            if (c.at.y < c.size * 0.5f) { c.at.y = c.size * 0.5f; c.velocity = new Vector3(c.velocity.x * 0.6f, -c.velocity.y * 0.35f, c.velocity.z * 0.6f); } // a bounce
            chunks[i] = c;
            float size = c.size * (age < 0.7f ? 1f : 1f - (age - 0.7f) / 0.3f);
            c.look.Add(c.at, Vector3.one * size, Quaternion.AngleAxis((now - c.born) * c.spin, c.axis));
        }
    }

    float Power => heroHit ? player.damageBonus : 1f + Balance.DamagePerRank * train.PassiveLevel("damage") + (Story.Active ? 0f : Workshop.DamageBonus);
    Weapon source; // RunStats credits this weapon; null for enemies' own blasts

    void Hurt(Enemy e, float damage, bool weaponHit = true, bool ultimateHit = false)
    {
        if ((e.type.loot >= 0 || e.type.heroOnly) && !heroHit) return;
        bool critical = false;
        if (weaponHit) damage = train.CriticalDamage(damage, out critical);
        if (Time.time < e.markedUntil) damage *= 2f;
        float applied = Mathf.Max(damage - e.type.armor * Balance.Scale, damage * 0.2f);
        bool redirected = ultimateHit && e.onUltimateHit != null;
        if (redirected) applied = e.onUltimateHit(applied);
        RunStats.Deal(source, Mathf.Min(applied, Mathf.Max(0f, e.hp)), ultimateHit); // what it really took, not the overkill
        if (!redirected) e.hp -= applied;
        if (applied > 0f) train.DamageNumber(e.position + Vector3.up * (e.type.size + 0.5f), applied, false, critical);
        if (applied > 0f && e.type.loot < 0)
        {
            // knocked back, away from the train, the more the smaller it is (a boss hardly moves), by how hard the
            // weapon hits (Weapon.push: a shotgun shoves, a minigun's many small hits barely do)
            Vector3 away = new Vector3(e.position.x, 0f, e.position.z);
            if (!IsMiniBoss(e.type) && !e.type.projectile && away.sqrMagnitude > 0.01f)
                e.position += away.normalized * Mathf.Clamp(0.5f / e.type.size, 0.04f, 0.35f) * (critical ? 2f : 1f) * (source != null ? source.push : 1f);
        }
        if (redirected || e.hp > 0f) return;
        if (ultimateHit && e.type.loot < 0) RunStats.UltimateKills++;
        if (e.type.loot < 0 && !e.type.Fodder) { train.HitStop(IsMiniBoss(e.type) ? 0.16f : 0.045f); train.Shake(IsMiniBoss(e.type) ? 0.5f : 0.08f); }
        if (e.type.loot < 0) train.Charge(); // a kill counts and fills the ultimate (the station's guns kill elsewhere: they do not)
        if (IsMiniBoss(e.type))
        {
            MiniBossesBeaten++;
            train.Shake(0.5f);
            Burst("grenade", e.position, 3f);
            train.OpenChest();
        }
        // Flat (the biome multiplier is on the cargo, where the risk is), and less a kill as the crowds thicken:
        // half at difficulty 20, so the purse does not run away late on.
        train.Earn(e.type.reward * killMoney * (e.type.Fodder ? FodderMoney : 1f) / (1f + 0.05f * level));
        Die(e, true);
    }

    // killed = shot down (as opposed to reaching the train); only then does it split.
    void Die(Enemy e, bool killed)
    {
        EnemyType t = e.type;
        Vector3 at = e.position;
        RemoveEnemy(e.index);
        // Everything that falls leaves a puff of smoke; the floating goods just drift off. (The black marks they
        // used to leave on the ground are off: by the hundred they made a mess of it. DrawStains still draws any.)
        if (t.loot < 0)
        {
            Burst("smoke", new Vector3(at.x, 0.15f, at.z), 0.8f + t.size * 0.5f);
            if (killed && !t.projectile) Shatter(t, at);
        }
        if (killed && t.loot >= 0) train.AddLoot(t.loot);
        if (t.explode > 0f) blasts.Add(new Blast { center = at, type = t });
        for (int i = 0; killed && i < t.split && OnField < maxOnField; i++)
            Add(Larva, at + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
    }

    Bullet Spawn(Vector3 position, Vector3 size, Batch batch, float life) => Spawn(position, size, batch, life, Quaternion.identity);

    Bullet Spawn(Vector3 position, Vector3 size, Batch batch, float life, Quaternion rotation)
    {
        Bullet b = bulletPool.Count > 0 ? bulletPool.Pop() : new Bullet();
        b.position = position;
        b.velocity = Vector3.zero;
        b.size = size;
        b.batch = batch;
        b.life = life;
        b.rotation = rotation;
        b.target = null;
        b.free = false;
        b.style = 0;
        b.age = 0f;
        b.back = false;
        b.split = false;
        b.power = 1f;
        b.from = position;
        b.hits.Clear();
        bullets.Add(b);
        return b;
    }

    void RemoveEnemy(int i)
    {
        int last = enemies.Count - 1;
        Enemy e = enemies[i];
        e.id++;
        enemyPool.Push(e);
        enemies[i] = enemies[last];
        enemies[i].index = i;
        enemies.RemoveAt(last);
    }

    void RemoveBullet(int i)
    {
        int last = bullets.Count - 1;
        bulletPool.Push(bullets[i]);
        bullets[i] = bullets[last];
        bullets.RemoveAt(last);
    }
}

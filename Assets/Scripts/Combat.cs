using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        Material material;
        int count;

        // mesh: a model painted with vertex colours (Tools/make_enemies.py); the batch's colour tints it.
        public Batch(Color color, bool shadows, Mesh mesh = null)
        {
            this.mesh = mesh;
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
            if (!material)
            {
                material = TrainSim.Mat(color);
                if (mesh) material.SetFloat("_MVCOL", 1f); // RealToon: multiply by the vertex colours
            }
            var parameters = new RenderParams(material)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 500f),
                shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = shadows
            };
            Graphics.RenderMeshInstanced(parameters, mesh ? mesh : cube, 0, matrices, count);
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
        public float stun;                      // seconds the wagon's weapon stops after contact
        // One timed ability per type, fired every `interval` seconds:
        public float interval;
        public float ranged;                    // stops this far from the train and shoots it
        public float blink;                     // teleports this far towards the train
        public float heal, healRadius;          // health per second given to enemies around it
        public bool spawner;                    // releases a larva

        Batch batch;
        // Enemies are robots, loot is a balloon carrying its cargo (Tools/make_enemies.py, Tools/make_cargo.py).
        public Batch Batch => batch ??= new Batch(color, true, Resources.Load<Mesh>(loot >= 0 ? "Models/Balloon_" + loot : "Models/Enemy_" + Shape));

        // Which robot it looks like, read off what it does: the look tells the player what to expect.
        public string Shape =>
            heal > 0f ? "drone" : spawner || split > 0 ? "hive" : blink > 0f ? "orb"
            : ranged > 0f && health < 20f ? "saucer" : armor > 0f || health >= 15f ? "walker" : explode > 0f ? "mine"
            : speed >= 7f ? "dart" : chargeRange > 0f || health >= 8f ? "diamond" : "saucer";
        public bool Flies => Shape is "saucer" or "drone" or "orb" or "diamond" or "mine";
        public bool Spins => Shape is "diamond" or "mine" or "orb";
    }

    static readonly EnemyType Larva = new EnemyType { name = "Larva", color = new Color(0.7f, 0.9f, 0.3f), health = 1.5f, speed = 7f, damage = 2f, size = 0.5f };

    public static readonly EnemyType[] Types =
    {
        // Everywhere
        new EnemyType { name = "Normal", color = new Color(0.2f, 0.6f, 0.2f), health = 3f, speed = 4f, damage = 5f, size = 1f, weight = 1f },
        new EnemyType { name = "Rápido", color = new Color(1f, 0.8f, 0.1f), health = 2f, speed = 8f, damage = 3f, size = 0.7f, firstLevel = 2, weight = 0.5f },
        new EnemyType { name = "Tanque", color = new Color(0.5f, 0.1f, 0.6f), health = 12f, speed = 2.5f, damage = 12f, size = 1.6f, reward = 3, firstLevel = 4, weight = 0.2f },
        // Desierto
        new EnemyType { name = "Bomba", biome = 0, color = new Color(1f, 0.3f, 0.1f), health = 4f, speed = 5f, damage = 4f, size = 0.9f, firstLevel = 1, weight = 0.3f, explode = 3.5f, explodeDamage = 8f },
        new EnemyType { name = "Embestidor", biome = 0, color = new Color(0.45f, 0.3f, 0.15f), health = 6f, speed = 3f, damage = 8f, size = 1.1f, firstLevel = 3, weight = 0.25f, chargeRange = 14f, chargeSpeed = 4f },
        // Nieve
        new EnemyType { name = "Yeti", biome = 1, color = new Color(0.8f, 0.9f, 1f), health = 18f, speed = 3f, damage = 10f, size = 1.5f, reward = 3, weight = 0.2f, regen = 3f },
        new EnemyType { name = "Lobo", biome = 1, color = new Color(0.45f, 0.5f, 0.55f), health = 1.5f, speed = 9f, damage = 2f, size = 0.6f, weight = 0.2f, pack = 4 },
        // Jungla
        new EnemyType { name = "Nido", biome = 2, color = new Color(0.1f, 0.3f, 0.1f), health = 10f, speed = 3f, damage = 6f, size = 1.4f, reward = 2, weight = 0.25f, split = 3 },
        new EnemyType { name = "Escupidor", biome = 2, color = new Color(0.6f, 1f, 0.2f), health = 4f, speed = 4f, damage = 3f, size = 0.9f, weight = 0.25f, ranged = 12f, interval = 1.5f },
        // Volcán
        new EnemyType { name = "Pez volador", biome = 3, color = new Color(0.2f, 0.85f, 0.95f), health = 2f, speed = 9f, damage = 2f, size = 0.6f, weight = 0.3f, pack = 3 },
        new EnemyType { name = "Boya mina", biome = 3, color = new Color(0.95f, 0.3f, 0.2f), health = 7f, speed = 3f, damage = 5f, size = 1.1f, weight = 0.22f, explode = 3f, explodeDamage = 7f },

        new EnemyType { name = "Gólem", biome = 4, color = new Color(0.35f, 0.05f, 0.02f), health = 25f, speed = 2f, damage = 15f, size = 1.8f, reward = 4, weight = 0.2f, armor = 1f },
        new EnemyType { name = "Diablillo", biome = 4, color = new Color(1f, 0.1f, 0.1f), health = 3f, speed = 9f, damage = 3f, size = 0.6f, weight = 0.35f, explode = 2.5f, explodeDamage = 5f, slowImmune = true },
        // Espacio
        new EnemyType { name = "Pandillero", biome = 5, color = new Color(1f, 0.85f, 0.1f), health = 3f, speed = 8f, damage = 3f, size = 0.7f, weight = 0.3f, pack = 3 },
        new EnemyType { name = "Blindado", biome = 5, color = new Color(0.2f, 0.25f, 0.35f), health = 22f, speed = 2.5f, damage = 8f, size = 1.5f, reward = 4, weight = 0.2f, ranged = 14f, interval = 2f, armor = 0.8f },

        new EnemyType { name = "Fantasma", biome = 6, color = new Color(0.7f, 1f, 1f), health = 6f, speed = 3f, damage = 8f, size = 1f, weight = 0.3f, blink = 7f, interval = 2f },
        new EnemyType { name = "Dron sanador", biome = 6, color = new Color(1f, 0.5f, 0.8f), health = 8f, speed = 3.5f, damage = 2f, size = 0.8f, reward = 2, weight = 0.2f, heal = 4f, healRadius = 7f, interval = 0.5f },
        // Planeta alienígena
        new EnemyType { name = "Colmena", biome = 7, color = new Color(0.4f, 0.5f, 0.1f), health = 30f, speed = 1.5f, damage = 10f, size = 2f, reward = 5, weight = 0.15f, spawner = true, interval = 2.5f },
        new EnemyType { name = "Saboteador", biome = 7, color = new Color(0f, 0.6f, 0.6f), health = 5f, speed = 7f, damage = 2f, size = 0.8f, weight = 0.3f, stun = 5f },
        // Fortaleza alienígena
        new EnemyType { name = "Centinela", biome = 8, color = new Color(0.5f, 0.55f, 0.6f), health = 15f, speed = 3f, damage = 5f, size = 1.2f, reward = 3, weight = 0.3f, ranged = 16f, interval = 1.2f, armor = 1f },
        new EnemyType { name = "Coloso", biome = 8, color = new Color(0.1f, 0.1f, 0.12f), health = 60f, speed = 2f, damage = 25f, size = 2.5f, reward = 8, weight = 0.12f, explode = 6f, explodeDamage = 15f, chargeRange = 10f, chargeSpeed = 2.5f },
    };

    // Floating goods, one per kind of cargo. Not enemies: they drift by, ignore the train, and only the hero can break them.
    public static readonly EnemyType[] Loot =
    {
        new EnemyType { name = "Madera", loot = 0, color = new Color(0.9f, 0.5f, 0.18f), health = 20f, size = 1.6f, reward = 0 },
        new EnemyType { name = "Roca", loot = 1, color = new Color(0.55f, 0.65f, 0.85f), health = 30f, size = 1.6f, reward = 0 },
        new EnemyType { name = "Armas", loot = 2, color = new Color(0.4f, 0.7f, 0.25f), health = 14f, size = 1.3f, reward = 0 },
    };

    public class Enemy
    {
        public Vector3 position, velocity; // velocity: only floating goods use it
        public EnemyType type;
        public float hp, maxHp, slowUntil, timer;
        public int index;
        public int id; // changes when the enemy dies; whoever targets it compares ids to notice
    }

    class Bullet
    {
        public Vector3 position, velocity, size;
        public Quaternion rotation = Quaternion.identity;
        public Enemy target; // null for a flash (explosion, lightning, beam)
        public int targetId;
        public bool hero;    // fired by the hero: the only shots that hurt floating goods
        public Weapon weapon;
        public Batch batch;
        public float life;
        public int level, chain;
    }

    struct Blast
    {
        public Vector3 center;
        public EnemyType type;
    }

    public static Combat Instance { get; private set; }
    public bool Paused => train.Swarming; // turrets hold their fire too
    public float calmDistance = 15f, calmRetreat = 12f; // how far enemies keep during a button game, and how fast they get there
    const float ModelScale = 1.4f;  // the robots are slimmer than the cubes they replace
    const float BurstSize = 0.6f; // effect scale per unit of blast radius

    // Difficulty = stations reached + biome jumps. Pressure (enemies x health) grows a little slower
    // than a train that takes a good upgrade every station, so skipped or unlucky upgrades get punished.
    [Header("Difficulty")]
    public float spawnRadius = 45f;
    public float spawnRate = 1.3f;         // spawns per second at difficulty 0
    public float spawnRatePerLevel = 0.12f;
    public float healthPerLevel = 0.045f;  // +4.5% enemy health per difficulty level
    public float damagePerLevel = 0.05f;
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
    readonly Stack<Enemy> enemyPool = new();
    readonly Stack<Bullet> bulletPool = new();

    const float PlayerRadius = 0.5f;

    TrainSim train;
    Player player;
    float spawnTimer, lootTimer = 8f;
    int level;
    bool heroHit; // true while a hit made by the hero is being applied

    public int EnemyCount => enemies.Count;
    public int BulletCount => bullets.Count;

    void Awake()
    {
        Instance = this;
        train = GetComponent<TrainSim>();
    }

    void Update()
    {
        if (train.Swarming) return; // seen from the gunner's seat: the rest of the fight is put away until it is over
        if (train.AtStation)
        {
            // Stations are safe: wipe the field and stop spawning until the train leaves.
            while (enemies.Count > 0) RemoveEnemy(enemies.Count - 1);
            while (bullets.Count > 0) RemoveBullet(bullets.Count - 1);
            return;
        }

        float dt = Time.deltaTime, now = Time.time;
        level = train.Difficulty;
        player = train.Player;

        // A button game or a curve under the train: nobody new arrives, the rest back off and nothing they do counts.
        bool calm = train.Calm;
        if (!calm) spawnTimer -= dt;
        while (spawnTimer <= 0f)
        {
            spawnTimer += 1f / (spawnRate + spawnRatePerLevel * level);
            SpawnWave();
        }

        lootTimer -= dt;
        if (lootTimer <= 0f)
        {
            lootTimer = Random.Range(lootEvery.x, lootEvery.y);
            if (train.CanTakeLoot) SpawnLoot();
        }

        // Enemies head for the closest wagon that is still standing.
        // Faster than cruise: everyone slides towards the rear, so the front arrives sooner and the rear lags.
        float damageScale = (1f + damagePerLevel * level) * Balance.Scale, drift = (train.Speed - train.maxSpeed) * speedDrift * dt;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            EnemyType t = e.type;
            if (t.loot >= 0)
            {
                // Drifts for a while, then speeds away and is gone once out of sight.
                e.timer -= dt;
                e.position += e.velocity * (dt * (e.timer > 0f ? 1f : 10f));
                if (e.timer <= 0f && e.position.x * e.position.x + e.position.z * e.position.z > spawnRadius * spawnRadius) RemoveEnemy(i);
                continue;
            }
            int wagon = train.ClosestWagon(e.position, out Vector3 toWagon);
            if (wagon < 0) break; // train destroyed
            e.position -= train.Forward(wagon) * drift; // along the track where its wagon is, so it holds through a curve
            float sqr = toWagon.sqrMagnitude, radius = t.size * 0.5f;
            if (calm)
            {
                float away = Mathf.Sqrt(sqr);
                if (away < calmDistance && away > 0.01f) e.position -= toWagon * (Mathf.Min(calmDistance - away, calmRetreat * dt) / away);
                continue;
            }

            // The hero on foot is a target too, whenever closer than the train.
            bool atPlayer = false;
            if (player.Alive)
            {
                Vector3 toPlayer = player.Position - e.position;
                toPlayer.y = 0f;
                float gap = Mathf.Max(0f, toPlayer.magnitude - PlayerRadius);
                if (gap * gap < sqr)
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
                    train.Damage(wagon, t.damage * damageScale);
                    if (t.stun > 0f) train.Stun(wagon, t.stun);
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
                    else train.Damage(wagon, t.damage * damageScale);
                    Spawn(e.position + toWagon + Vector3.up, Vector3.one * 0.6f, t.Batch, 0.1f);
                }
                else if (t.blink > 0f) e.position += toWagon.normalized * Mathf.Min(t.blink, Mathf.Sqrt(sqr) - radius);
                else if (t.spawner) Add(Larva, e.position + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
                else if (t.heal > 0f)
                {
                    foreach (Enemy other in enemies)
                        if (other != e && (other.position - e.position).sqrMagnitude <= t.healRadius * t.healRadius)
                            other.hp = Mathf.Min(other.maxHp, other.hp + t.heal * t.interval * Balance.Scale);
                }
            }
            if (inRange) continue; // shooters hold their ground

            float speed = t.speed;
            if (t.chargeRange > 0f && sqr <= t.chargeRange * t.chargeRange) speed *= t.chargeSpeed;
            if (now < e.slowUntil) speed *= 0.5f;
            e.position += toWagon * (speed * dt / Mathf.Sqrt(sqr));
        }
        ResolveBlasts(calm ? 0f : damageScale);

        // Each bullet tracks only its own target: one check per bullet, and it cannot miss or tunnel.
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet b = bullets[i];
            Enemy hit = b.target;
            if (hit != null && hit.id == b.targetId)
            {
                Vector3 toTarget = hit.position - b.position;
                float reach = b.weapon.speed * dt + hit.type.size * 0.5f;
                if (toTarget.sqrMagnitude <= reach * reach)
                {
                    Vector3 at = hit.position;
                    if (b.weapon.slow > 0f && !hit.type.slowImmune) hit.slowUntil = now + b.weapon.slow;
                    heroHit = b.hero;
                    float damage = b.weapon.Damage(b.level) * Power;
                    if (b.weapon.splash > 0f) Explode(at, b.weapon.Splash(b.level), damage, b.weapon);
                    else Hurt(hit, damage);
                    heroHit = false;

                    chainSkip.Clear();
                    chainSkip.Add(hit);
                    b.target = b.chain-- > 0 ? Nearest(at, chainRange, chainSkip) : null;
                    if (b.target == null) RemoveBullet(i);
                    else
                    {
                        b.targetId = b.target.id;
                        b.position = at;
                    }
                    continue;
                }
                b.velocity = toTarget.normalized * b.weapon.speed;
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
        ResolveBlasts((1f + damagePerLevel * level) * Balance.Scale); // kills made by turrets after our Update
        // Robots face the train, hover and bob if they fly, and turn on the spot if that is their thing.
        float clock = Time.time;
        foreach (Enemy e in enemies)
        {
            EnemyType t = e.type;
            Vector3 at = e.position, toTrain = new Vector3(-at.x, 0f, -at.z);
            if (t.loot >= 0)
            {
                // Floats well clear of the ground, swaying.
                at.y += 1.6f + Mathf.Sin(clock * 1.5f + e.id) * 0.25f;
                t.Batch.Add(at, Vector3.one * (t.size * 2.6f), Quaternion.Euler(0f, clock * 20f + e.id * 31f, Mathf.Sin(clock * 1.1f + e.id) * 5f));
                continue;
            }
            Quaternion facing = t.Spins ? Quaternion.Euler(0f, clock * 140f + e.id * 47f, 0f)
                : toTrain.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toTrain) : Quaternion.identity;
            if (t.Flies) at.y += 0.7f + Mathf.Sin(clock * 3f + e.id) * 0.18f;
            t.Batch.Add(at, Vector3.one * (t.size * ModelScale), facing);
        }
        foreach (Bullet b in bullets) b.batch.Add(b.position, b.size, b.rotation);
        foreach (EnemyType t in Types) t.Batch.Flush();
        foreach (EnemyType t in Loot) t.Batch.Flush();
        Larva.Batch.Flush();
        foreach (Weapon w in Weapon.Wagon) w.Batch.Flush();
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
        }
    }

    // Weighted pick among the types unlocked at this difficulty and living in this biome.
    void SpawnWave()
    {
        int biome = train.Biome;
        float total = 0f;
        foreach (EnemyType t in Types)
            if (t.firstLevel <= level && (t.biome < 0 || t.biome == biome)) total += t.weight;
        float roll = Random.value * total;
        EnemyType type = Types[0];
        foreach (EnemyType t in Types)
        {
            if (t.firstLevel > level || (t.biome >= 0 && t.biome != biome)) continue;
            type = t;
            if ((roll -= t.weight) <= 0f) break;
        }

        float angle = Random.value * Mathf.PI * 2f;
        float radius = Mathf.Max(spawnRadius, train.HalfExtents.x + 25f); // a long train pushes the spawn ring out
        var center = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        for (int i = 0; i < type.pack; i++)
            Add(type, center + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)) * (type.pack - 1));
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
        e.hp = e.maxHp = type.health * (1f + healthPerLevel * level) * Balance.Scale;
        e.velocity = Vector3.zero;
        e.slowUntil = 0f;
        e.timer = type.interval;
        e.index = enemies.Count;
        enemies.Add(e);
        return e;
    }

    // ponytail: linear scan; add a spatial grid if enemies reach several thousand.
    // loot: also consider floating goods (only the hero aims at those).
    public Enemy Nearest(Vector3 from, float range, List<Enemy> except = null, bool loot = false)
    {
        Enemy nearest = null;
        float best = range * range;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (!loot && enemies[i].type.loot >= 0) continue;
            float d = (enemies[i].position - from).sqrMagnitude;
            if (d < best && (except == null || !except.Contains(enemies[i]))) { best = d; nearest = enemies[i]; }
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

    // One trigger pull of a weapon: a pulse, or one shot at `first` plus any extra shots at the next nearest enemies.
    // hero: the shots come from the hero, so they can break floating goods.
    public void Volley(Vector3 from, Enemy first, Weapon weapon, int level, bool hero = false)
    {
        heroHit = hero;
        if (weapon.pulse) Explode(from, weapon.range, weapon.Damage(level) * Power, weapon);
        else
        {
            if (weapon.beam)
            {
                if (first != null)
                {
                    Vector3 flat = first.position - from;
                    flat.y = 0f;
                    if (flat != Vector3.zero) Beam(from, flat.normalized, weapon, level);
                }
            }
            else
            {
                int count = weapon.Targets(level);
                picked.Clear();
                for (Enemy e = first; e != null && picked.Count < count; e = Nearest(from, weapon.range, picked))
                {
                    picked.Add(e);
                    if (weapon.strike) Strike(e, weapon, level);
                    else Fire(from, e, weapon, level);
                }
            }
        }
        heroHit = false;
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
    }

    // Lightning: instant hit, drawn as a thin column from the sky.
    public void Strike(Enemy target, Weapon weapon, int level)
    {
        Spawn(new Vector3(target.position.x, 7f, target.position.z), new Vector3(0.3f, 14f, 0.3f), weapon.Batch, 0.12f);
        Burst(weapon.id, target.position, 1.5f);
        Hurt(target, weapon.Damage(level) * Power);
    }

    // Weapon blast: damages every enemy within radius (measured on the ground) and shows a short flash.
    public void Explode(Vector3 center, float radius, float damage, Weapon weapon)
    {
        Flash(center, radius, weapon.Batch);
        Burst(weapon.id, center, radius * BurstSize);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Vector3 d = enemies[i].position - center;
            if (d.x * d.x + d.z * d.z <= radius * radius) Hurt(enemies[i], damage);
        }
    }

    // Laser: one instant straight ray. Weak per enemy, but it crosses every enemy standing on the line.
    public void Beam(Vector3 from, Vector3 direction, Weapon weapon, int level)
    {
        float range = weapon.range, damage = weapon.Damage(level) * Power;
        Spawn(from + direction * (range * 0.5f), new Vector3(0.12f, 0.12f, range), weapon.Batch, 0.08f, Quaternion.LookRotation(direction));
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
    public void Burst(string effect, Vector3 at, float size)
    {
        if (!bursts.TryGetValue(effect, out Pool pool))
            bursts[effect] = pool = new Pool { prefab = Resources.Load<GameObject>("Vfx/" + effect) };
        if (!pool.prefab) return;
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
    }

    // Enemy death explosions hurt wagons and other enemies; those deaths can queue more blasts.
    void ResolveBlasts(float damageScale)
    {
        for (int n = 0; n < blasts.Count; n++)
        {
            Blast blast = blasts[n];
            float radius = blast.type.explode;
            Flash(blast.center, radius, blast.type.Batch);
            Burst("enemy", blast.center, radius * BurstSize);
            train.DamageArea(blast.center, radius, blast.type.explodeDamage * damageScale);
            if (player.Alive && (player.Position - blast.center).sqrMagnitude <= radius * radius)
                player.Damage(blast.type.explodeDamage * damageScale);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Vector3 d = enemies[i].position - blast.center;
                if (d.x * d.x + d.z * d.z <= radius * radius) Hurt(enemies[i], blast.type.explodeDamage * Balance.Scale);
            }
        }
        blasts.Clear();
    }

    void Flash(Vector3 center, float radius, Batch batch) =>
        Spawn(new Vector3(center.x, 0.05f, center.z), new Vector3(radius * 2f, 0.05f, radius * 2f), batch, 0.1f);

    float Power => heroHit ? player.damageBonus : 1f; // the hero's damage upgrades, on the hero's shots only

    void Hurt(Enemy e, float damage)
    {
        if (e.type.loot >= 0 && !heroHit) return;
        float applied = Mathf.Max(damage - e.type.armor * Balance.Scale, damage * 0.2f);
        e.hp -= applied;
        if (applied > 0f) train.DamageNumber(e.position + Vector3.up * (e.type.size + 0.5f), applied, false); // red, over the enemy
        if (e.hp > 0f) return;
        train.Earn(e.type.reward); // flat: the biome multiplier is on the cargo, where the risk is
        Die(e, true);
    }

    // killed = shot down (as opposed to reaching the train); only then does it split.
    void Die(Enemy e, bool killed)
    {
        EnemyType t = e.type;
        Vector3 at = e.position;
        RemoveEnemy(e.index);
        if (killed && t.loot >= 0) train.AddLoot(t.loot);
        if (t.explode > 0f) blasts.Add(new Blast { center = at, type = t });
        for (int i = 0; killed && i < t.split; i++)
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

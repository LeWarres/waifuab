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
        Material material;
        int count;

        public Batch(Color color, bool shadows)
        {
            this.color = color;
            this.shadows = shadows;
        }

        public void Add(Vector3 position, Vector3 size)
        {
            if (count == matrices.Length) Flush();
            var m = Matrix4x4.identity;
            m.m00 = size.x; m.m11 = size.y; m.m22 = size.z;
            m.m03 = position.x; m.m13 = position.y; m.m23 = position.z;
            matrices[count++] = m;
        }

        public void Flush()
        {
            if (count == 0) return;
            if (!cube) cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (!material) material = TrainSim.Mat(color);
            var parameters = new RenderParams(material)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 500f),
                shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = shadows
            };
            Graphics.RenderMeshInstanced(parameters, cube, 0, matrices, count);
            count = 0;
        }
    }

    // ponytail: enemy stats live in code, same as Weapon. Behaviours are opt-in fields, 0 = off.
    public class EnemyType
    {
        public string name;
        public Color color;
        public float health, speed, damage, size;
        public int reward = 1;   // money per kill, multiplied by the biome
        public int biome = -1;   // only spawns in this biome; -1 = everywhere
        public int firstLevel;   // difficulty at which this type starts to appear
        public float weight;     // share of the spawns once it appears; 0 = never spawned directly

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
        public Batch Batch => batch ??= new Batch(color, true);
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
        new EnemyType { name = "Gólem", biome = 3, color = new Color(0.35f, 0.05f, 0.02f), health = 25f, speed = 2f, damage = 15f, size = 1.8f, reward = 4, weight = 0.2f, armor = 1f },
        new EnemyType { name = "Diablillo", biome = 3, color = new Color(1f, 0.1f, 0.1f), health = 3f, speed = 9f, damage = 3f, size = 0.6f, weight = 0.35f, explode = 2.5f, explodeDamage = 5f, slowImmune = true },
        // Espacio
        new EnemyType { name = "Fantasma", biome = 4, color = new Color(0.7f, 1f, 1f), health = 6f, speed = 3f, damage = 8f, size = 1f, weight = 0.3f, blink = 7f, interval = 2f },
        new EnemyType { name = "Dron sanador", biome = 4, color = new Color(1f, 0.5f, 0.8f), health = 8f, speed = 3.5f, damage = 2f, size = 0.8f, reward = 2, weight = 0.2f, heal = 4f, healRadius = 7f, interval = 0.5f },
        // Planeta alienígena
        new EnemyType { name = "Colmena", biome = 5, color = new Color(0.4f, 0.5f, 0.1f), health = 30f, speed = 1.5f, damage = 10f, size = 2f, reward = 5, weight = 0.15f, spawner = true, interval = 2.5f },
        new EnemyType { name = "Saboteador", biome = 5, color = new Color(0f, 0.6f, 0.6f), health = 5f, speed = 7f, damage = 2f, size = 0.8f, weight = 0.3f, stun = 5f },
        // Fortaleza alienígena
        new EnemyType { name = "Centinela", biome = 6, color = new Color(0.5f, 0.55f, 0.6f), health = 15f, speed = 3f, damage = 5f, size = 1.2f, reward = 3, weight = 0.3f, ranged = 16f, interval = 1.2f, armor = 1f },
        new EnemyType { name = "Coloso", biome = 6, color = new Color(0.1f, 0.1f, 0.12f), health = 60f, speed = 2f, damage = 25f, size = 2.5f, reward = 8, weight = 0.12f, explode = 6f, explodeDamage = 15f, chargeRange = 10f, chargeSpeed = 2.5f },
    };

    public class Enemy
    {
        public Vector3 position;
        public EnemyType type;
        public float hp, maxHp, slowUntil, timer;
        public int index;
        public int id; // changes when the enemy dies; whoever targets it compares ids to notice
    }

    class Bullet
    {
        public Vector3 position, velocity, size;
        public Enemy target; // null for a flash (explosion, lightning)
        public int targetId;
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

    // Difficulty = stations reached + biome jumps. Pressure (enemies x health) grows a little slower
    // than a train that takes a good upgrade every station, so skipped or unlucky upgrades get punished.
    [Header("Difficulty")]
    public float spawnRadius = 45f;
    public float spawnRate = 1f;           // spawns per second at difficulty 0
    public float spawnRatePerLevel = 0.25f;
    public float healthPerLevel = 0.08f;   // +8% enemy health per difficulty level
    public float damagePerLevel = 0.05f;
    public float speedDrift = 0.15f;       // how much a faster or slower train drags enemies back or lets them catch up   // +5% enemy damage per difficulty level

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
    float spawnTimer;
    int level;

    public int EnemyCount => enemies.Count;
    public int BulletCount => bullets.Count;

    void Awake()
    {
        Instance = this;
        train = GetComponent<TrainSim>();
    }

    void Update()
    {
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

        spawnTimer -= dt;
        while (spawnTimer <= 0f)
        {
            spawnTimer += 1f / (spawnRate + spawnRatePerLevel * level);
            SpawnWave();
        }

        // Enemies head for the closest wagon that is still standing.
        // Faster than cruise: everyone slides towards the rear, so the front arrives sooner and the rear lags.
        float damageScale = 1f + damagePerLevel * level, drift = (train.Speed - train.maxSpeed) * speedDrift * dt;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            EnemyType t = e.type;
            int wagon = train.ClosestWagon(e.position, out Vector3 toWagon);
            if (wagon < 0) break; // train destroyed
            e.position -= train.Forward(wagon) * drift; // along the track where its wagon is, so it holds through a curve
            float sqr = toWagon.sqrMagnitude, radius = t.size * 0.5f;

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

            if (t.regen > 0f) e.hp = Mathf.Min(e.maxHp, e.hp + t.regen * dt);

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
                            other.hp = Mathf.Min(other.maxHp, other.hp + t.heal * t.interval);
                }
            }
            if (inRange) continue; // shooters hold their ground

            float speed = t.speed;
            if (t.chargeRange > 0f && sqr <= t.chargeRange * t.chargeRange) speed *= t.chargeSpeed;
            if (now < e.slowUntil) speed *= 0.5f;
            e.position += toWagon * (speed * dt / Mathf.Sqrt(sqr));
        }
        ResolveBlasts(damageScale);

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
                    float damage = b.weapon.Damage(b.level);
                    if (b.weapon.slow > 0f && !hit.type.slowImmune) hit.slowUntil = now + b.weapon.slow;
                    if (b.weapon.splash > 0f) Explode(at, b.weapon.Splash(b.level), damage, b.weapon);
                    else Hurt(hit, damage);

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
        ResolveBlasts(damageScale);
    }

    // Drawn after every script has moved, so nothing lags a frame behind.
    void LateUpdate()
    {
        ResolveBlasts(1f + damagePerLevel * level); // kills made by turrets after our Update
        foreach (Enemy e in enemies) e.type.Batch.Add(e.position, new Vector3(e.type.size, e.type.size, e.type.size));
        foreach (Bullet b in bullets) b.batch.Add(b.position, b.size);
        foreach (EnemyType t in Types) t.Batch.Flush();
        Larva.Batch.Flush();
        foreach (Weapon w in Weapon.Wagon) w.Batch.Flush();
        foreach (Weapon w in Weapon.Hero) w.Batch.Flush();
    }

    // After a curve the world is turned so the train lies along X again; everything on the field turns with it.
    public void Rotate(Quaternion turn)
    {
        foreach (Enemy e in enemies) e.position = turn * e.position;
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
        var center = new Vector3(Mathf.Cos(angle) * spawnRadius, 0f, Mathf.Sin(angle) * spawnRadius);
        for (int i = 0; i < type.pack; i++)
            Add(type, center + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)) * (type.pack - 1));
    }

    void Add(EnemyType type, Vector3 ground)
    {
        Enemy e = enemyPool.Count > 0 ? enemyPool.Pop() : new Enemy();
        e.position = new Vector3(ground.x, type.size * 0.5f, ground.z);
        e.type = type;
        e.hp = e.maxHp = type.health * (1f + healthPerLevel * level);
        e.slowUntil = 0f;
        e.timer = type.interval;
        e.index = enemies.Count;
        enemies.Add(e);
    }

    // ponytail: linear scan; add a spatial grid if enemies reach several thousand.
    public Enemy Nearest(Vector3 from, float range, List<Enemy> except = null)
    {
        Enemy nearest = null;
        float best = range * range;
        for (int i = 0; i < enemies.Count; i++)
        {
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
        if (current == null) return Nearest(from, range);
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
    public void Volley(Vector3 from, Enemy first, Weapon weapon, int level)
    {
        if (weapon.pulse)
        {
            Explode(from, weapon.range, weapon.Damage(level), weapon);
            return;
        }
        int count = weapon.Targets(level);
        picked.Clear();
        for (Enemy e = first; e != null && picked.Count < count; e = Nearest(from, weapon.range, picked))
        {
            picked.Add(e);
            if (weapon.strike) Strike(e, weapon, level);
            else Fire(from, e, weapon, level);
        }
    }

    public void Fire(Vector3 from, Enemy target, Weapon weapon, int level)
    {
        Bullet b = Spawn(from, Vector3.one * 0.25f, weapon.Batch, bulletLife);
        b.target = target;
        b.targetId = target.id;
        b.weapon = weapon;
        b.velocity = (target.position - from).normalized * weapon.speed;
        b.level = level;
        b.chain = weapon.Chain(level);
    }

    // Lightning: instant hit, drawn as a thin column from the sky.
    public void Strike(Enemy target, Weapon weapon, int level)
    {
        Spawn(new Vector3(target.position.x, 7f, target.position.z), new Vector3(0.3f, 14f, 0.3f), weapon.Batch, 0.12f);
        Hurt(target, weapon.Damage(level));
    }

    // Weapon blast: damages every enemy within radius (measured on the ground) and shows a short flash.
    public void Explode(Vector3 center, float radius, float damage, Weapon weapon)
    {
        Flash(center, radius, weapon.Batch);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Vector3 d = enemies[i].position - center;
            if (d.x * d.x + d.z * d.z <= radius * radius) Hurt(enemies[i], damage);
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
            train.DamageArea(blast.center, radius, blast.type.explodeDamage * damageScale);
            if (player.Alive && (player.Position - blast.center).sqrMagnitude <= radius * radius)
                player.Damage(blast.type.explodeDamage * damageScale);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Vector3 d = enemies[i].position - blast.center;
                if (d.x * d.x + d.z * d.z <= radius * radius) Hurt(enemies[i], blast.type.explodeDamage);
            }
        }
        blasts.Clear();
    }

    void Flash(Vector3 center, float radius, Batch batch) =>
        Spawn(new Vector3(center.x, 0.05f, center.z), new Vector3(radius * 2f, 0.05f, radius * 2f), batch, 0.1f);

    void Hurt(Enemy e, float damage)
    {
        e.hp -= Mathf.Max(damage - e.type.armor, damage * 0.2f);
        if (e.hp > 0f) return;
        train.money += e.type.reward * (train.Biome + 1);
        Die(e, true);
    }

    // killed = shot down (as opposed to reaching the train); only then does it split.
    void Die(Enemy e, bool killed)
    {
        EnemyType t = e.type;
        Vector3 at = e.position;
        RemoveEnemy(e.index);
        if (t.explode > 0f) blasts.Add(new Blast { center = at, type = t });
        for (int i = 0; killed && i < t.split; i++)
            Add(Larva, at + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
    }

    Bullet Spawn(Vector3 position, Vector3 size, Batch batch, float life)
    {
        Bullet b = bulletPool.Count > 0 ? bulletPool.Pop() : new Bullet();
        b.position = position;
        b.velocity = Vector3.zero;
        b.size = size;
        b.batch = batch;
        b.life = life;
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

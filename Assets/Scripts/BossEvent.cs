using System.Collections.Generic;
using UnityEngine;

// The biome boss: once in a while, instead of the leg's usual event, a war rig rolls up on the train's flank
// and starts lobbing slow homing rockets at it. The rockets can be shot down; the rig itself is one long
// health pool the hero and the wagons wear down. The picture pulls way back to take the rig in, then settles
// into the usual isometric view. Beat it and the way out of the biome opens for the rest of the run.
// TrainSim rolls for it (See TrainSim.bossChance) and gates the route on BossBeaten.
public class BossEvent : MonoBehaviour
{
    [Header("Intro")]
    public float introSeconds = 2.6f;    // the long shot before the fight proper
    public float zoomOut = 3.1f;         // how far the camera pulls back for it
    public float fightZoom = 1.2f;       // how far back it stays so the rig and the train both fit

    [Header("Driving")]
    public float approach = 21f;         // how far behind the rig hangs while it chases
    public float alongside = 6f;         // ...and how far out it runs when it pulls level with the train
    public float weave = 6.5f;           // how far it swings side to side
    public float speed = 30f;            // its speed on the ground (chasing and weaving)

    [Header("Rockets")]
    public float rocketEvery = 1.5f;     // seconds between two volleys, before the difficulty speeds it up
    public float rocketSpeed = 10f;      // slow on purpose: easy to line up a shot
    public float rocketTurn = 60f;       // degrees a second a rocket can steer towards the train
    public float rocketHealth = 2.4f * Balance.Scale; // a couple of hits to bring one down
    public float rocketDamage = 5f * Balance.Scale;   // to the wagon a rocket reaches

    [Header("The fight")]
    public float bossHealth = 75f * Balance.Scale;    // unscaled hits at difficulty 0, grows with it
    public float healthPerDifficulty = 0.12f;         // +12% of the rig's health per difficulty
    public float rampSpeed = 30f;                     // how fast it accelerates in beside the train

    // Same shape as Combat.EnemyType so the hero and the turrets can lock onto the rig and the rockets.
    static readonly Combat.EnemyType Rig = new Combat.EnemyType
    {
        name = "Boss", color = new Color(0.6f, 0.2f, 0.15f), health = 0f, speed = 0f, damage = 0f, size = 4f,
        reward = 250, loot = -1, projectile = true,
    };
    static readonly Combat.EnemyType Rocket = new Combat.EnemyType
    {
        name = "Rocket", color = new Color(1f, 0.5f, 0.15f), health = 0f, speed = 0f, damage = 0f, size = 0.9f,
        reward = 2, loot = -1, projectile = true,
    };

    // One entry per biome, in the order of TrainSim.Biomes. The desert one is the reference; the rest lean on
    // the same hull with their own name, colour and a little more health further along.
    struct Boss { public string id; public Color accent; public float healthMul; }
    static readonly Boss[] Bosses =
    {
        new Boss { id = "desert",   accent = new Color(0.90f, 0.55f, 0.15f), healthMul = 1.00f },
        new Boss { id = "snow",     accent = new Color(0.70f, 0.82f, 0.95f), healthMul = 1.15f },
        new Boss { id = "jungle",   accent = new Color(0.35f, 0.75f, 0.25f), healthMul = 1.30f },
        new Boss { id = "ocean",    accent = new Color(0.25f, 0.60f, 0.90f), healthMul = 1.45f },
        new Boss { id = "volcano",  accent = new Color(1.00f, 0.35f, 0.05f), healthMul = 1.65f },
        new Boss { id = "city",     accent = new Color(0.65f, 0.68f, 0.80f), healthMul = 1.85f },
        new Boss { id = "space",    accent = new Color(0.55f, 0.45f, 0.95f), healthMul = 2.10f },
        new Boss { id = "alien",    accent = new Color(0.30f, 0.95f, 0.75f), healthMul = 2.40f },
        new Boss { id = "fortress", accent = new Color(0.45f, 1.00f, 0.35f), healthMul = 2.80f },
    };

    class Shell { public Combat.Enemy e; public int id, car; public Vector3 at; }

    public bool Active { get; private set; }               // from the intro to the end
    public bool Incoming => Active && Time.unscaledTime < startsAt; // the rig is still rolling in, seen from above
    public string Name => L10n.T("boss.name." + type.id);

    TrainSim train;
    Combat.Batch rockets;
    Transform rig;
    Combat.Enemy boss;
    int bossId, midCar;
    float maxHp;
    readonly List<Shell> live = new();
    Boss type;

    int mode;               // 0 chasing behind, 1 running level with the train
    float modeTimer, phase, side = 1f, fireIn, startZoom, startsAt, nextDust;

    void Awake() => train = GetComponent<TrainSim>();

    // False only when the rig's model is missing; TrainSim then picks another event for that leg.
    public bool Begin()
    {
        GameObject model = Resources.Load<GameObject>("Models/Boss");
        if (!model) return false;
        type = Bosses[Mathf.Clamp(train.Biome, 0, Bosses.Length - 1)];

        if (rig) Destroy(rig.gameObject);
        rig = Instantiate(model).transform;
        rig.name = "BossRig";
        Skin();

        Combat combat = Combat.Instance;
        maxHp = bossHealth * type.healthMul * (1f + healthPerDifficulty * train.Difficulty);
        boss = combat.AddProjectile(Rig, new Vector3(0f, 2f, 0f), maxHp);
        bossId = boss.id;

        rockets = new Combat.Batch(type.accent, false, Resources.Load<Mesh>("Models/Enemy_dart"));
        live.Clear();

        midCar = Mathf.Clamp(train.CarCount / 2, 0, Mathf.Max(0, train.CarCount - 1));
        Vector3 dir = Ground(train.Forward(midCar));
        rig.position = train.CarPosition(midCar) - dir * (approach + 34f);
        rig.rotation = Quaternion.LookRotation(dir, Vector3.up);
        boss.position = rig.position + Vector3.up * 2f;

        Active = true;
        mode = 0;
        modeTimer = Random.Range(3.5f, 7f);
        phase = Random.value * 10f;
        side = Random.value < 0.5f ? -1f : 1f;
        fireIn = 1.2f;
        nextDust = 0f;
        startZoom = Mathf.Max(train.cameraZoom, 1f);
        train.cameraZoom = startZoom;
        startsAt = Time.unscaledTime + introSeconds;
        return true;
    }

    // The imported hull's materials, one toon twin each, leaned a little towards the biome's colour.
    void Skin()
    {
        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = renderer.sharedMaterials, made = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
                made[i] = TrainSim.Mat(mats[i] ? Color.Lerp(mats[i].color, type.accent, 0.22f) : type.accent);
            renderer.sharedMaterials = made;
        }
    }

    void Update()
    {
        if (!Active || train.Paused) return;
        if (train.Health <= 0f) { End(null); return; }
        if (boss == null || boss.id != bossId) { Win(); return; } // shot to bits
        if (train.AtStation) { End("boss.fled"); return; }

        float dt = Time.deltaTime, now = Time.time;
        Zoom(dt);
        Drive(dt, now);
        Dust(now);
        Aim(dt);
        Fire(dt);
        // The rig is one big target: keep its body where the guns can reach it.
        boss.position = rig.position + Vector3.up * 2f;
        train.BossBar(Name, maxHp > 0f ? boss.hp / maxHp : 0f);
    }

    // A long pull back that takes in the whole rig, then settles into the isometric view of the fight.
    void Zoom(float dt)
    {
        float t = Mathf.Clamp01((Time.unscaledTime - startsAt + introSeconds) / introSeconds);
        float want = t < 0.45f ? Mathf.Lerp(startZoom, zoomOut, t / 0.45f)
                                : Mathf.Lerp(zoomOut, fightZoom, (t - 0.45f) / 0.55f);
        train.cameraZoom = Mathf.Lerp(train.cameraZoom, want, 6f * dt);
    }

    void Drive(float dt, float now)
    {
        Vector3 dir = Ground(train.Forward(midCar));
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        Vector3 center = train.CarPosition(midCar);

        modeTimer -= dt;
        if (modeTimer <= 0f)
        {
            modeTimer = Random.Range(3.5f, 7f);
            mode = Random.value < 0.7f ? 0 : 1; // mostly chasing, sometimes pulling level
            side = Random.value < 0.5f ? -1f : 1f;
        }

        float ahead = mode == 0 ? -approach : Mathf.Sin(now * 0.35f) * 1.5f;
        float lateral = mode == 0 ? Mathf.Sin(now * 0.55f + phase) * weave : side * alongside;
        Vector3 want = center + dir * ahead + right * lateral;
        if (Incoming) want = center - dir * (approach + Mathf.Lerp(34f, 0f, 1f - (startsAt - Time.unscaledTime) / introSeconds));
        want.y = 0f;
        rig.position = Vector3.MoveTowards(rig.position, want, speed * dt);

        Vector3 face = mode == 0 ? dir : center - rig.position;
        face.y = 0f;
        if (face.sqrMagnitude > 0.04f)
            rig.rotation = Quaternion.Slerp(rig.rotation, Quaternion.LookRotation(face, Vector3.up), rampSpeed * dt);
    }

    void Dust(float now)
    {
        if (now < nextDust) return;
        nextDust = now + 0.05f;
        Vector3 at = rig.TransformPoint(new Vector3(Random.Range(-1.7f, 1.7f), 0.25f, -3.6f));
        Combat.Instance.Burst("smoke", at, 1.2f);
    }

    void Fire(float dt)
    {
        if (Incoming) return;
        fireIn -= dt;
        if (fireIn > 0f) return;
        fireIn = rocketEvery / (1f + 0.03f * train.Difficulty);
        Launch();
        if (Random.value < 0.35f) Launch(); // a second tube now and then
    }

    void Launch()
    {
        int car = RandomCar();
        if (car < 0) return;
        Vector3 local = new Vector3(Random.Range(-0.66f, 0.66f), Random.value < 0.5f ? 4.78f : 5.12f, 1.6f);
        Vector3 muzzle = rig.TransformPoint(local);
        Vector3 aim = train.CarTop(car);
        Combat.Enemy e = Combat.Instance.AddProjectile(Rocket, muzzle, rocketHealth * (1f + 0.05f * train.Difficulty));
        e.velocity = (aim - muzzle).normalized * rocketSpeed;
        live.Add(new Shell { e = e, id = e.id, car = car, at = muzzle });
        Combat.Instance.Burst("spark", muzzle, 0.6f);
    }

    // Rockets home slowly on the wagon they were aimed at; a shot-down one pops where it was.
    void Aim(float dt)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Shell r = live[i];
            if (r.e == null || r.e.id != r.id) // brought down by a gun (Combat already paid the reward)
            {
                Combat.Instance.Burst("grenade", r.at, 1.1f);
                live.RemoveAt(i);
                continue;
            }
            Vector3 aim = train.CarAlive(r.car) ? train.CarTop(r.car) : train.Focus + Vector3.up * 1.6f;
            Vector3 to = aim - r.e.position;
            Vector3 dir = to.sqrMagnitude > 0.01f ? Vector3.RotateTowards(r.e.velocity.normalized, to.normalized, rocketTurn * Mathf.Deg2Rad * dt, 0f) : r.e.velocity.normalized;
            r.e.velocity = dir * rocketSpeed;
            r.e.position += r.e.velocity * dt;
            r.at = r.e.position;
            Combat.Instance.Burst("smoke", r.e.position, 0.45f); // its trail

            if (r.e.position.y <= 0.15f) // buried itself in the ground
            {
                Combat.Instance.Burst("grenade", r.e.position, 1.1f);
                Combat.Instance.Despawn(r.e);
                live.RemoveAt(i);
                continue;
            }
            if (to.sqrMagnitude <= 2.2f * 2.2f) // reached its wagon
            {
                train.Damage(r.car, rocketDamage * (1f + 0.05f * train.Difficulty));
                train.Shake(0.18f);
                Combat.Instance.Burst("rocket", r.e.position, 1.3f);
                Combat.Instance.Despawn(r.e);
                live.RemoveAt(i);
            }
        }
    }

    void Win()
    {
        Vector3 at = rig ? rig.position + Vector3.up * 1.8f : Vector3.zero;
        Combat.Instance.Burst("fire", at, 3.6f);
        Combat.Instance.Burst("grenade", at, 2.6f);
        train.Shake(0.7f);
        train.BossDefeated(); // the way out of this biome is open, and this rig is done for the run
        End("boss.won");
    }

    void End(string key)
    {
        Active = false;
        train.cameraZoom = 1f;
        Combat combat = Combat.Instance;
        if (combat)
        {
            if (boss != null) combat.Despawn(boss); // a live rig just withdraws
            foreach (Shell r in live) if (r.e != null) combat.Despawn(r.e);
        }
        boss = null;
        live.Clear();
        if (rig) Destroy(rig.gameObject);
        if (key != null) train.Toast(L10n.T(key));
    }

    // The rocket volley is the boss's real threat: the hull is drawn by the model.
    void LateUpdate()
    {
        if (!Active) return;
        foreach (Shell r in live)
            if (r.e != null && r.e.id == r.id)
                rockets.Add(r.e.position, Vector3.one * 1.1f, r.e.velocity.sqrMagnitude > 0.01f ? Quaternion.LookRotation(r.e.velocity) : Quaternion.identity);
        rockets.Flush();
    }

    int RandomCar()
    {
        int alive = 0;
        for (int i = 0; i < train.CarCount; i++) if (train.CarAlive(i)) alive++;
        if (alive == 0) return -1;
        int pick = Random.Range(0, alive);
        for (int i = 0; i < train.CarCount; i++)
            if (train.CarAlive(i) && pick-- == 0) return i;
        return 0;
    }

    static Vector3 Ground(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 0.001f ? Vector3.right : v.normalized; }
}

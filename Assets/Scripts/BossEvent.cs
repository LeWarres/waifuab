using System.Collections.Generic;
using UnityEngine;

// The biome boss: once in a while, instead of the leg's usual event, a war rig rolls up on the train's flank
// and starts lobbing bombs at it, each on a slow curve onto a wagon under a flashing alert. Only the hero can shoot
// them down (the turrets cannot even aim at them): the rig keeps her busy while the guns work on it. The rig is one long
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

    [Header("Bombs")]
    public float bombEvery = 3.2f;       // seconds between two volleys, before the difficulty speeds it up
    public float bombFlight = 3.4f;      // seconds from the tube to the wagon: the hero's time to shoot it down
    public float bombArc = 7f;           // how high the curve goes
    public float bombCurl = 5f;          // and how far it swings to one side
    public float rocketHealth = 2.4f * Balance.Scale; // a couple of the hero's hits to bring one down
    public float rocketDamage = 4f * Balance.Scale;   // to the wagon a bomb reaches

    [Header("The fight")]
    public float bossHealth = 150f * Balance.Scale;   // unscaled hits at difficulty 0, grows with it
    public float healthPerDifficulty = 0.12f;         // +12% of the rig's health per difficulty
    public float rampSpeed = 30f;                     // how fast it accelerates in beside the train
    [System.NonSerialized] public float hard = 1f;    // the story's mid boss at the junction: this many times the health,
                                                      // and rockets 1.6 times as hard, 2.5 times as tough to shoot down and
                                                      // 1.8 times as often; set before Begin, back to 1 at the end

    // Same shape as Combat.EnemyType so the hero and the turrets can lock onto the rig and the rockets.
    static readonly Combat.EnemyType Rig = new Combat.EnemyType
    {
        name = "Boss", color = new Color(0.6f, 0.2f, 0.15f), health = 0f, speed = 0f, damage = 0f, size = 4f,
        reward = 250, loot = -1, projectile = true,
    };
    static Combat.EnemyType Rocket;

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

    // stage: 0 a bomb onto a wagon; a barrage missile 1 going out, 2 hanging out there, 3 diving onto a wagon.
    class Shell { public Combat.Enemy e; public int id, car, stage; public Vector3 at, from, curl, park; public float t, smokeIn, wait; }

    [Header("Barrage")]
    public float barrageEvery = 10f;     // seconds between two barrages (the junction's truck: sooner)
    public float barrageWait = 9f;       // how long the missiles hang out there before they dive
    public float pinShare = 0.06f;       // of the pinned wagon's health, a second, while the truck grinds on it
    float grindIn, barrageIn, grind, overheatUntil, lastHp;
    public float overheatSeconds = 6f;   // after a barrage is cleared: slowed, and every hit on it counts twice
    bool Overheated => Time.time < overheatUntil;
    int pinCar = -1;                     // the wagon the truck has rammed and holds while a barrage is up
    bool Barraging => live.Exists(s => s.stage > 0);

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
    // The junction's truck (hard > 1) also rams: it picks a wagon (marked "!"), lines up beside it, then charges, and
    // a button game starts. Most buttons right and the truck swerves off; most wrong and it slams into the wagon
    // for a share of that wagon's health.
    public float ramEvery = 9f, ramShare = 0.3f, ramWindUp = 1.6f;
    int ramCar = -1;
    int ramGame; // 0 not yet, 1 being played, 2 lost (it hits), 3 won (it swerves off)
    float ramIn, ramWind;
    float modeTimer, phase, side = 1f, fireIn, startZoom, startsAt, nextDust;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        if (Rocket == null)
        {
            Rocket = new Combat.EnemyType
            {
                name = "Rocket",
                color = new Color(1f, 0.5f, 0.15f),
                health = 0f,
                speed = 0f,
                damage = 0f,
                size = 0.9f,
                reward = 2,
                loot = -1,
                projectile = true,
                heroOnly = true,
            };
        }
        if (Rocket.customMesh == null)
            Rocket.customMesh = Resources.Load<Mesh>("Models/BossRocket");
    }

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
        maxHp = bossHealth * type.healthMul * (1f + healthPerDifficulty * train.Difficulty) * hard;
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
        ramCar = -1;
        ramIn = ramEvery * 0.6f;
        modeTimer = Random.Range(3.5f, 7f);
        phase = Random.value * 10f;
        side = Random.value < 0.5f ? -1f : 1f;
        fireIn = 1.2f;
        barrageIn = 6f;
        pinCar = -1;
        overheatUntil = 0f;
        lastHp = boss.hp;
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
        if (!Active || train.Paused || train.UltimatePresenting) return;
        if (train.Health <= 0f) { End(null); return; }
        if (boss == null || boss.id != bossId) { Win(); return; } // shot to bits
        if (train.AtStation) { End("boss.fled"); return; }

        float dt = Time.deltaTime, now = Time.time;
        Zoom(dt);
        Drive(dt, now);
        Dust(now);
        Aim(dt);
        Fire(dt);
        // Overheated: whatever it lost this frame, it loses again.
        if (Overheated && lastHp > boss.hp) boss.hp -= lastHp - boss.hp;
        lastHp = boss.hp;
        if (Overheated)
        {
            train.Panel.Mark(rig.position + Vector3.up * 6.5f, "x2", 4);
            if (Random.value < dt * 8f) Combat.Instance.Burst("smoke", rig.position + Vector3.up * 4.5f, 1.4f);
        }
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
        float pace = speed;
        if (Overheated) { pace = speed * 0.35f; want = center + right * (side * alongside); } // limping alongside
        else if (pinCar >= 0 && Pin(right, ref want, dt)) pace = speed * 1.8f;
        else if (hard > 1f && !Incoming && Ram(right, ref want, dt)) pace = speed * 1.8f;
        want.y = 0f;
        rig.position = Vector3.MoveTowards(rig.position, want, pace * dt);

        Vector3 face = dir; // always parallel to the train: pulling alongside must not turn the nose into its flank
        face.y = 0f;
        if (face.sqrMagnitude > 0.04f)
            rig.rotation = Quaternion.Slerp(rig.rotation, Quaternion.LookRotation(face, Vector3.up), rampSpeed * dt);
    }

    // Rammed into a wagon as the barrage went up, and grinding on it until every missile is down: true while it holds.
    bool Pin(Vector3 right, ref Vector3 want, float dt)
    {
        if (!Barraging || !train.CarAlive(pinCar))
        {
            if (Barraging && (pinCar = RandomCar()) >= 0) return true; // that wagon is gone: the next one
            pinCar = -1;
            if (!Barraging) // the sky is clear: it pulls off steaming, slowed, its armour open
            {
                overheatUntil = Time.time + overheatSeconds;
                train.Toast(L10n.T("boss.overheat"));
            }
            mode = 0;
            modeTimer = Random.Range(3.5f, 7f);
            ramIn = ramEvery;
            Combat.Instance.Burst("dust", rig.position, 2.4f);
            return false;
        }
        Vector3 car = train.CarPosition(pinCar);
        train.Panel.Mark(car + Vector3.up * 3.5f, "!", 4);
        want = car + right * (side * 2.6f);
        Vector3 gap = rig.position - want;
        gap.y = 0f;
        if (gap.magnitude > 1.5f) return true;
        // Grinding: a bite every half second (the number shows each), sparks and a jolt: get the missiles down.
        grind += train.CarMaxHealth(pinCar) * pinShare * dt;
        if ((grindIn -= dt) <= 0f)
        {
            grindIn = 0.5f;
            train.Damage(pinCar, grind);
            grind = 0f;
            Combat.Instance.Burst("spark", car + Vector3.up * 1.2f + right * side, 1.4f);
            train.Shake(0.12f);
        }
        return true;
    }

    // The ram, if one is on: where the truck heads for instead. True while it is charging.
    bool Ram(Vector3 right, ref Vector3 want, float dt)
    {
        if (ramCar < 0)
        {
            if (Barraging || (ramIn -= dt) > 0f) return false;
            int alive = 0;
            for (int i = 0; i < train.CarCount; i++) if (train.CarAlive(i) && Random.Range(0, ++alive) == 0) ramCar = i;
            if (ramCar < 0) return false;
            ramWind = ramWindUp;
            ramGame = 0;
            side = Random.value < 0.5f ? -1f : 1f;
            train.Toast(L10n.T("boss.ram"));
        }
        if (ramCar >= train.CarCount || !train.CarAlive(ramCar)) { ramCar = -1; ramIn = 3f; return false; }
        Vector3 car = train.CarPosition(ramCar);
        train.Panel.Mark(car + Vector3.up * 3.5f, "!", 4);
        if ((ramWind -= dt) > 0f) { want = car + right * (side * 9f); return false; } // lining up
        if (ramGame == 0)
        {
            ramGame = 1;
            GetComponent<TurnEvent>().Phrase(4 + Mathf.Min(2, train.Difficulty / 8), L10n.T("boss.ram.qte"), 0.2f, (hits, length) => ramGame = hits * 2 > length ? 3 : 2);
        }
        if (ramGame == 1) { want = car + right * (side * 5.5f); return true; } // closing in while the buttons are played
        if (ramGame == 3) // dodged: it swerves away and falls back
        {
            train.Toast(L10n.T("boss.ram.dodged"));
            Combat.Instance.Burst("dust", rig.position, 2.4f);
            ramCar = -1;
            ramIn = ramEvery;
            mode = 0;
            modeTimer = Random.Range(3.5f, 7f);
            return false;
        }
        want = car + right * (side * 2.6f);
        Vector3 gap = rig.position - want;
        gap.y = 0f;
        if (gap.magnitude > 1.5f) return true;
        train.Damage(ramCar, train.CarMaxHealth(ramCar) * ramShare);
        train.Toast(L10n.T("boss.ram.hit"));
        train.Shake(0.6f);
        Combat.Instance.Burst("grenade", car + Vector3.up, 2.4f);
        ramCar = -1;
        ramIn = ramEvery;
        return false;
    }

    void Dust(float now)
    {
        if (now < nextDust) return;
        nextDust = now + 0.05f;
        Vector3 at = rig.TransformPoint(new Vector3(Random.Range(-1.7f, 1.7f), 0.25f, -3.6f));
        Combat.Instance.Burst("dust", at, 1.8f);
        Combat.Instance.Burst("dust", at, 1.6f);
    }

    void Fire(float dt)
    {
        if (Incoming) return;
        if (Barraging || Overheated) return; // the sky is full, or it is steaming: no bombs
        if (ramCar < 0 && (barrageIn -= dt) <= 0f)
        {
            barrageIn = barrageEvery / (hard > 1f ? 1.25f : 1f);
            Barrage();
            return;
        }
        fireIn -= dt;
        if (fireIn > 0f) return;
        fireIn = bombEvery / (1f + 0.03f * train.Difficulty) / (hard > 1f ? 1.3f : 1f);
        Launch();
        if (Random.value < (hard > 1f ? 0.6f : 0.35f)) Launch();
    }

    // A bomb onto a standing wagon: up out of a tube and down on a curve, slow enough for the hero to shoot it.
    void Launch()
    {
        int car = RandomCar();
        if (car < 0) return;
        Vector3 muzzle = rig.TransformPoint(new Vector3(Random.Range(-0.66f, 0.66f), Random.value < 0.5f ? 4.78f : 5.12f, 1.6f));
        Combat.Enemy e = Combat.Instance.AddProjectile(Rocket, muzzle, rocketHealth * (1f + 0.05f * train.Difficulty) * (hard > 1f ? 1.5f : 1f));
        Vector3 across = Vector3.Cross(Vector3.up, Ground(train.CarTop(car) - muzzle));
        live.Add(new Shell { e = e, id = e.id, car = car, at = muzzle, from = muzzle, curl = across * (Random.Range(-1f, 1f) * bombCurl) });
        Combat.Instance.Burst("spark", muzzle, 0.6f);
    }

    // The barrage: a spread of missiles lobbed high and far out round the train, to hang under their alerts across
    // the picture until the hero runs each one down; any still up when the wait is over dives onto a wagon. As they go
    // up the truck rams a wagon and stays grinding on it until the sky is clear.
    void Barrage()
    {
        int count = (hard > 1f ? 7 : 4) + Mathf.Min(4, train.Difficulty / 8);
        Camera view = Camera.main;
        var height = new Plane(Vector3.up, Vector3.up * 3f);
        for (int k = 0; k < count; k++)
        {
            int car = RandomCar();
            if (car < 0) return;
            // Somewhere near the edge of the picture (clear of the HUD), all round: a run to each one.
            float a = (k + Random.value * 0.7f) / count * Mathf.PI * 2f;
            Ray ray = view.ViewportPointToRay(new Vector3(0.5f + Mathf.Cos(a) * Random.Range(0.3f, 0.42f), 0.47f + Mathf.Sin(a) * Random.Range(0.25f, 0.35f), 0f));
            Vector3 park = height.Raycast(ray, out float d) ? ray.GetPoint(d) : train.Focus + Random.insideUnitSphere * 20f;
            park.y = 3f;
            Vector3 muzzle = rig.TransformPoint(new Vector3(Random.Range(-0.66f, 0.66f), Random.value < 0.5f ? 4.78f : 5.12f, 1.6f));
            Combat.Enemy e = Combat.Instance.AddProjectile(Rocket, muzzle, rocketHealth * (1f + 0.05f * train.Difficulty) * (hard > 1f ? 1.5f : 1f));
            live.Add(new Shell { e = e, id = e.id, car = car, at = muzzle, from = muzzle, park = park, stage = 1, t = -k * 0.08f });
        }
        Combat.Instance.Burst("rocket", rig.position + Vector3.up * 5f, 2f);
        pinCar = RandomCar();
        side = Random.value < 0.5f ? -1f : 1f;
        train.Toast(L10n.T("boss.barrage"));
    }

    // Bombs ride their curve onto the wagon they were aimed at; a shot-down one pops where it was.
    void Aim(float dt)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Shell r = live[i];
            if (r.e == null || r.e.id != r.id) // brought down by the hero (Combat already paid the reward)
            {
                Combat.Instance.Burst("grenade", r.at, 1.1f);
                live.RemoveAt(i);
                continue;
            }
            if (!train.CarAlive(r.car)) { int other = RandomCar(); if (other >= 0) r.car = other; } // its wagon is gone: another one
            Vector3 was = r.e.position;
            if (r.stage == 1 || r.stage == 2)
            {
                if (r.stage == 1) // out on a high curve to its spot
                {
                    r.t += dt / 1.5f;
                    float s = Mathf.Clamp01(r.t), v = 1f - s;
                    Vector3 top = (r.from + r.park) * 0.5f + Vector3.up * 14f;
                    r.e.position = v * v * r.from + 2f * v * s * top + s * s * r.park;
                    if (r.t >= 1f) { r.stage = 2; r.wait = Time.time + barrageWait; }
                }
                else // hanging there, bobbing, until the wait is over: then down onto a wagon
                {
                    r.e.position = r.park + new Vector3(Mathf.Sin(Time.time * 2f + r.id), Mathf.Sin(Time.time * 3f + r.id) * 0.4f, Mathf.Cos(Time.time * 2f + r.id)) * 0.5f;
                    if (Time.time >= r.wait) { r.stage = 3; r.from = r.e.position; r.t = 0f; r.curl = Vector3.zero; }
                }
                r.e.velocity = (r.e.position - was) / Mathf.Max(dt, 0.0001f);
                r.at = r.e.position;
                train.Panel.Mark(r.e.position + Vector3.up * 1.2f, "!", 5);
                Combat.Instance.Burst("rocket", r.e.position - Vector3.up * 0.6f, 0.45f);
                continue;
            }
            // A curve from the tube to the wagon (which moves on under it): up, over to one side, down. A barrage
            // missile diving comes down faster, from where it hung.
            r.t = Mathf.Min(1f, r.t + dt / (r.stage == 3 ? 1.1f : bombFlight));
            Vector3 aim = train.CarTop(r.car), mid = (r.from + aim) * 0.5f + Vector3.up * (r.stage == 3 ? 4f : bombArc) + r.curl;
            float t = r.t, u = 1f - t;
            r.e.position = u * u * r.from + 2f * u * t * mid + t * t * aim;
            r.e.velocity = (r.e.position - was) / Mathf.Max(dt, 0.0001f);
            r.at = r.e.position;
            train.Panel.Mark(r.e.position + Vector3.up * 1.2f, "!", 5);
            // The trail is the exhaust flame itself, with a thin grey wake under it: fire the eye can follow.
            Vector3 back = -r.e.velocity.normalized;
            Vector3 trailPos = r.e.position + back * 0.8f; // trailing behind the rocket, not above
            Combat.Instance.Burst("rocket", trailPos, 0.55f);
            r.smokeIn -= dt;
            if (r.smokeIn <= 0f)
            {
                r.smokeIn = 0.09f;
                Combat.Instance.Burst("smoke", trailPos, 0.3f);
            }

            if (r.t >= 1f) // down on the wagon
            {
                if (train.CarAlive(r.car)) train.Damage(r.car, rocketDamage * (1f + 0.05f * train.Difficulty) * (hard > 1f ? 1.3f : 1f) * (r.stage == 3 ? 2f : 1f));
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
        Combat.Instance.Burst("grenade", at, 2.6f);
        // The rig's fire is left burning on the ground behind the train: it slides back with the landscape and goes
        // out on its own, so it never hangs on the screen once the train has run past the wreck (see Wreck).
        GameObject fire = Resources.Load<GameObject>("Vfx/fire");
        if (fire)
        {
            GameObject wreck = Instantiate(fire, at, Quaternion.identity);
            wreck.transform.localScale = Vector3.one * 3.6f;
            Wreck burning = wreck.AddComponent<Wreck>();
            burning.train = train;
            burning.wagon = midCar;
        }
        train.Shake(0.7f);
        train.BossDefeated(); // the way out of this biome is open, and this rig is done for the run
        End("boss.won");
    }

    void End(string key)
    {
        Active = false;
        hard = 1f;
        if (ramGame == 1 && ramCar >= 0) { GetComponent<TurnEvent>().Drop(); Time.timeScale = 1f; } // a ram's buttons cut short
        ramCar = -1;
        pinCar = -1;
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

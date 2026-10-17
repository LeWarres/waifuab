using System.Collections.Generic;
using UnityEngine;

// Story mode's duel: the rival driver's locomotive pulling two armed wagons, on a track of its own beside ours.
// It keeps pace with the train, drifting ahead and dropping back along its rails, and fights in turns: its guns
// fire at our wagons; it lobs bombs onto our track that can be shot down in the air; it puts a pack of robots
// over to board us; it hexes us; and after each of those its engine overheats for a moment and takes double
// damage. The hero and the turrets shoot its engine, one big health pool.
// Like a Guitar Hero battle, every so often a phrase of buttons comes up, named after a status: play it all
// right and the rival gets that status, miss most of it and the rival turns it on us. Statuses last a few
// seconds: jammed guns (theirs stop firing; our turrets stop), broken armour (double damage taken) and
// confusion (their shots miss; our hero's controls are reversed). Beaten, the chapter is won
// (TrainSim.RivalBeaten). The biome's truck boss is something else: it may or may not turn up along the way.
public class RivalEvent : MonoBehaviour
{
    public float apart = 10f;         // from the middle of our track to the middle of theirs
    public float slide = 8f;          // how far it drifts ahead of and behind the middle of our train
    public float trackLength = 320f;  // of the rails laid for it, centred on the train
    public float fireEvery = 1.3f;    // seconds between two shots of one of its guns, at difficulty 0
    public float shotDamage = 4f * Balance.Scale;
    public float health = 1300f * Balance.Scale;
    public float perDifficulty = 0.03f; // more health and harder shots for every level of difficulty
    public float bombDamage = 6f * Balance.Scale, bombHealth = 2.5f * Balance.Scale, bombFlight = 2.6f;
    public float introSeconds = 2.4f;
    public float zoom = 1.4f;          // the picture pulls back this far so both trains fit
    public float phraseEvery = 11f;    // seconds between two button phrases
    public float statusSeconds = 5f;   // how long a status lasts
    public float phraseSlow = 0.3f;    // the time scale while a phrase is played

    static readonly Combat.EnemyType Engine = new Combat.EnemyType
    {
        name = "Rival", color = new Color(0.6f, 0.2f, 0.15f), health = 0f, speed = 0f, damage = 0f, size = 5f,
        reward = 300, loot = -1, projectile = true,
    };
    static readonly Combat.EnemyType Bomb = new Combat.EnemyType
    {
        name = "RivalBomb", color = new Color(1f, 0.45f, 0.1f), health = 0f, speed = 0f, damage = 0f, size = 1.3f,
        reward = 1, loot = -1, projectile = true,
    };
    const float SleeperSpacing = 1.5f, WagonStep = 5.5f;

    // Its turns, round and round: guns, bombs, overheated, guns, a hex on us, boarders, overheated. (kind, seconds)
    static readonly (int kind, float seconds)[] Turns = { (0, 8f), (1, 5f), (3, 4f), (0, 6f), (4, 2f), (2, 4f), (3, 4f) };
    const int Guns = 0, Bombs = 1, Boarders = 2, Overheated = 3, Hex = 4;

    public const int Jam = 0, Weak = 1, Mirror = 2, Statuses = 3;
    readonly float[] ours = new float[Statuses], theirs = new float[Statuses]; // game time each one wears off
    float phraseIn;
    TurnEvent phrase;
    public bool OnUs(int status) => Active && Time.time < ours[status];
    bool OnThem(int status) => Time.time < theirs[status];
    static string StatusName(int status) => L10n.T("status." + status);
    int turn;
    float turnLeft, lastHp, bombIn;
    class Shell { public Combat.Enemy e; public int id, car; public Vector3 from; public float at; public bool miss; }
    readonly List<Shell> bombs = new();
    Combat.Batch bombLook;

    public bool Active { get; private set; }

    TrainSim train;
    Transform root, track;
    readonly List<Transform> sleepers = new();
    readonly List<Transform> guns = new();
    Combat.Enemy body;
    int bodyId;
    float maxHp, phase, sleeperOffset, startsAt;
    float[] fireIn;
    string label;
    Vector3 along, centre;
    Combat.Batch tracers;
    readonly List<(Vector3 from, Vector3 to, float until)> shots = new();

    void Awake() => train = GetComponent<TrainSim>();

    // rival: which of Locomotives.Models comes, driven by its hero.
    public void Begin(int rival)
    {
        End();
        along = train.Forward(train.CarCount / 2);
        along.y = 0f;
        along.Normalize();
        float side = Random.value < 0.5f ? 1f : -1f;
        centre = train.CarPosition(train.CarCount / 2) + Vector3.Cross(Vector3.up, along) * (apart * side);
        Quaternion facing = Quaternion.FromToRotation(Vector3.right, along);

        // Its track: rails and ballast stretched along the whole picture, sleepers that slide past like ours.
        track = new GameObject("RivalTrack").transform;
        track.SetPositionAndRotation(centre - along * (trackLength * 0.5f), facing);
        Transform rails = train.DressTrack(Instantiate(Resources.Load<GameObject>("Models/Track_rail"), track)).transform;
        rails.localScale = new Vector3(trackLength, 1f, 1f);
        GameObject sleeper = Resources.Load<GameObject>("Models/Sleeper_0");
        for (float x = 0f; x < trackLength; x += SleeperSpacing)
        {
            Transform piece = train.DressTrack(Instantiate(sleeper, track)).transform;
            piece.localPosition = Vector3.right * x;
            sleepers.Add(piece);
        }

        // The train: the rival's engine in front, two wagons with a gun each behind it.
        root = new GameObject("Rival").transform;
        root.SetPositionAndRotation(centre, facing);
        Transform engine = Locomotives.Create(root, rival);
        engine.localPosition = Vector3.right * WagonStep;
        engine.localRotation = Quaternion.Euler(0f, 180f, 0f); // its nose (-x in the model) towards the way it runs
        GameObject wagon = Resources.Load<GameObject>("Models/Wagon");
        for (int i = 0; i < 2; i++)
        {
            Transform car = Instantiate(wagon, root).transform;
            car.localPosition = Vector3.right * (-WagonStep * i);
            foreach (Renderer part in car.GetComponentsInChildren<Renderer>()) TrainSim.Toon(part);
            Turret gun = Instantiate(train.turretPrefab, root);
            gun.Init(Weapon.Wagon[i == 0 ? 0 : 1]);
            gun.enabled = false; // aimed and fired from here, at the train
            gun.transform.localPosition = new Vector3(-WagonStep * i, 0.45f + train.wagonSize.y, 0f);
            guns.Add(gun.transform);
        }
        fireIn = new float[guns.Count];
        for (int i = 0; i < fireIn.Length; i++) fireIn[i] = 1.5f + i * 0.6f;

        Combat.Instance.ClearField(); // nobody else on the field while the two trains fight it out
        maxHp = health * (1f + perDifficulty * train.Difficulty);
        body = Combat.Instance.AddProjectile(Engine, EnginePosition, maxHp);
        bodyId = body.id;
        label = L10n.T(Locomotives.HeroNameKey(rival)) + " · " + L10n.T(Locomotives.NameKeys[rival]);
        tracers = new Combat.Batch(new Color(1f, 0.35f, 0.25f), false);
        phase = 0f;
        turn = 0;
        turnLeft = Turns[0].seconds;
        lastHp = maxHp;
        bombLook = new Combat.Batch(Bomb.color, false, Resources.Load<Mesh>("Models/Enemy_mine"));
        startsAt = Time.unscaledTime + introSeconds;
        System.Array.Clear(ours, 0, Statuses);
        System.Array.Clear(theirs, 0, Statuses);
        phraseIn = phraseEvery * 0.5f;
        phrase = GetComponent<TurnEvent>();
        Active = true;
    }

    Vector3 EnginePosition => root.TransformPoint(Vector3.right * WagonStep) + Vector3.up * 2f;

    void Update()
    {
        if (!Active || train.Paused) return;
        if (train.Health <= 0f) { End(); return; }
        if (body == null || body.id != bodyId) { Win(); return; } // its engine is wrecked

        float dt = Time.deltaTime, now = Time.time;
        // The picture pulls back to take in both trains, slowly at first.
        float zoomTo = Time.unscaledTime < startsAt ? Mathf.Lerp(1f, zoom + 0.4f, 1f - (startsAt - Time.unscaledTime) / introSeconds) : zoom;
        train.cameraZoom = Mathf.Lerp(train.cameraZoom, zoomTo, 4f * dt);

        // Its turns. Overheated, it slows down and every hit on it counts twice.
        int kind = Turns[turn].kind;
        if (Time.unscaledTime >= startsAt && (turnLeft -= dt) <= 0f)
        {
            turn = (turn + 1) % Turns.Length;
            turnLeft = Turns[turn].seconds;
            kind = Turns[turn].kind;
            if (kind == Bombs) { train.Toast(L10n.T("rival.bombs")); bombIn = 0.4f; }
            else if (kind == Boarders)
            {
                train.Toast(L10n.T("rival.boarders"));
                Combat.Instance.Drop(EnginePosition - Vector3.up * 2f, 8 + train.Difficulty);
            }
            else if (kind == Overheated) train.Toast(L10n.T("rival.weak"));
            else if (kind == Hex)
            {
                int status = Random.Range(0, Statuses);
                Hit(true, status);
                train.Toast(L10n.T("rival.hex", StatusName(status)));
            }
        }
        // A phrase of buttons (not for the bot, which cannot play them).
        if (Time.unscaledTime >= startsAt && !phrase.Active && !TrainSim.NoEvents && (phraseIn -= dt) <= 0f)
        {
            phraseIn = phraseEvery;
            int status = Random.Range(0, Statuses);
            phrase.Phrase(Mathf.Min(4 + train.Difficulty / 4, 8), L10n.T("battle.title", StatusName(status)), phraseSlow, (hits, length) =>
            {
                if (!Active) return;
                bool all = hits == length, most = length - hits > hits;
                if (all) Hit(false, status);
                else if (most) Hit(true, status);
                train.Toast(L10n.T(all ? "battle.hit" : most ? "battle.back" : "battle.even", StatusName(status)));
            });
        }
        bool weak = kind == Overheated || OnThem(Weak);
        float lost = lastHp - body.hp;
        if (weak && lost > 0f) body.hp -= lost;
        if (body.hp <= 0f)
        {
            Combat.Instance.Despawn(body);
            Win();
            return;
        }
        lastHp = body.hp;
        if (weak) train.Panel.Mark(EnginePosition + Vector3.up * 2.5f, "x2", 4);
        if (kind == Bombs && !OnThem(Jam) && (bombIn -= dt) <= 0f)
        {
            bombIn = 0.9f / (1f + 0.03f * train.Difficulty);
            Lob();
        }
        Fly(now);

        // Drifts ahead and back along its rails (it slows when overheated); its sleepers slide past at the
        // speed of the ground.
        phase += dt * (kind == Overheated ? 0.08f : 0.35f);
        root.position = centre + along * (Mathf.Sin(phase) * slide);
        sleeperOffset = Mathf.Repeat(sleeperOffset - train.GroundScroll, SleeperSpacing);
        for (int i = 0; i < sleepers.Count; i++) sleepers[i].localPosition = Vector3.right * (i * SleeperSpacing + sleeperOffset);
        body.position = EnginePosition;

        // Its guns: each turns on one of our wagons and fires; a quick red tracer shows the shot.
        if (Time.unscaledTime >= startsAt && kind == Guns && !OnThem(Jam))
            for (int i = 0; i < guns.Count; i++)
            {
                if ((fireIn[i] -= dt) > 0f) continue;
                fireIn[i] = fireEvery / (1f + 0.03f * train.Difficulty) * Random.Range(0.8f, 1.2f);
                int target = RandomCar();
                if (target < 0) continue;
                bool miss = OnThem(Mirror); // confused: the shot goes wide
                Vector3 at = train.CarPosition(target) + Vector3.up * 1.5f + (miss ? Random.onUnitSphere * 4f : Vector3.zero), muzzle = guns[i].position + Vector3.up * 0.4f;
                Vector3 flat = at - guns[i].position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f) guns[i].rotation = Quaternion.LookRotation(flat);
                if (!miss) train.Damage(target, shotDamage * (1f + perDifficulty * train.Difficulty));
                Combat.Instance.Burst("spark", muzzle, 1.2f);
                Combat.Instance.Burst("smoke", at, 1f);
                shots.Add((muzzle, at, now + 0.08f));
            }
        train.BossBar(label, maxHp > 0f ? body.hp / maxHp : 0f);
    }

    // A status on us (us) or on the rival. Jammed turrets of ours are stunned turrets.
    void Hit(bool us, int status)
    {
        (us ? ours : theirs)[status] = Time.time + statusSeconds;
        if (us && status == Jam)
            for (int i = 0; i < train.CarCount; i++) train.Stun(i, statusSeconds);
        Combat.Instance.Burst("spark", us ? train.CarPosition(train.CarCount / 2) + Vector3.up * 2f : EnginePosition, 2f);
    }

    // For the HUD: the statuses on each side and the seconds they have left.
    public string StatusLine()
    {
        string you = "", them = "";
        for (int s = 0; s < Statuses; s++)
        {
            if (OnUs(s)) you += (you == "" ? "" : ", ") + StatusName(s) + $" {Mathf.CeilToInt(ours[s] - Time.time)}s";
            if (Active && OnThem(s)) them += (them == "" ? "" : ", ") + StatusName(s) + $" {Mathf.CeilToInt(theirs[s] - Time.time)}s";
        }
        return (you == "" ? "" : " · " + L10n.T("battle.you", you)) + (them == "" ? "" : " · " + L10n.T("battle.them", them));
    }

    // A bomb from its rear wagon, in an arc onto one of our wagons. Shot down in the air, it bursts harmlessly.
    void Lob()
    {
        int car = RandomCar();
        if (car < 0) return;
        Vector3 from = guns[guns.Count - 1].position + Vector3.up;
        Combat.Enemy e = Combat.Instance.AddProjectile(Bomb, from, bombHealth * (1f + perDifficulty * train.Difficulty));
        bombs.Add(new Shell { e = e, id = e.id, car = car, from = from, at = Time.time, miss = OnThem(Mirror) });
        Combat.Instance.Burst("spark", from, 1f);
    }

    void Fly(float now)
    {
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            Shell b = bombs[i];
            if (b.e.id != b.id) { bombs.RemoveAt(i); continue; } // shot down: Combat already burst it
            float k = (now - b.at) / bombFlight;
            Vector3 to = train.CarPosition(b.car) + Vector3.up * 1.2f + (b.miss ? along * 7f : Vector3.zero);
            b.e.position = Vector3.Lerp(b.from, to, k) + Vector3.up * (7f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(k)));
            if (k < 1f) continue;
            if (!b.miss && train.CarAlive(b.car)) train.Damage(b.car, bombDamage * (1f + perDifficulty * train.Difficulty));
            Combat.Instance.Burst("grenade", to, 1.6f);
            Combat.Instance.Despawn(b.e);
            bombs.RemoveAt(i);
        }
    }

    void LateUpdate()
    {
        if (!Active) return;
        float now = Time.time;
        foreach (Shell b in bombs)
            if (b.e.id == b.id) bombLook.Add(b.e.position, Vector3.one * 1.4f, Quaternion.Euler(now * 200f, now * 140f, 0f));
        bombLook.Flush();
        shots.RemoveAll(s => s.until < now);
        foreach (var s in shots)
        {
            Vector3 d = s.to - s.from;
            tracers.Add((s.from + s.to) * 0.5f, new Vector3(0.15f, 0.15f, d.magnitude), Quaternion.LookRotation(d));
        }
        tracers.Flush();
    }

    int RandomCar()
    {
        int alive = 0;
        for (int i = 0; i < train.CarCount; i++) if (train.CarAlive(i)) alive++;
        if (alive == 0) return -1;
        int pick = Random.Range(0, alive);
        for (int i = 0; i < train.CarCount; i++)
            if (train.CarAlive(i) && pick-- == 0) return i;
        return -1;
    }

    void Win()
    {
        Vector3 at = root.TransformPoint(Vector3.right * WagonStep) + Vector3.up * 1.5f;
        Combat.Instance.Burst("grenade", at, 3f);
        train.Shake(0.8f);
        body = null; // already gone back to the pool: nothing to take off the field
        End();
        train.RivalBeaten();
    }

    void End()
    {
        Active = false;
        if (phrase) phrase.Drop();
        if (train) train.cameraZoom = 1f;
        if (body != null && body.id == bodyId && Combat.Instance) Combat.Instance.Despawn(body);
        body = null;
        if (root) Destroy(root.gameObject);
        if (track) Destroy(track.gameObject);
        sleepers.Clear();
        guns.Clear();
        foreach (Shell b in bombs) if (b.e.id == b.id && Combat.Instance) Combat.Instance.Despawn(b.e);
        bombs.Clear();
        shots.Clear();
    }
}

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
    public float shotDamage = 0.6f * Balance.Scale;
    public float health = 800f * Balance.Scale;
    public float perDifficulty = 0.03f; // more health for every level of difficulty
    public float hitPerDifficulty = 0.012f; // harder and quicker shots and bombs: gentler, the fight now lasts
    // So that evolved weapons and the ultimate cannot just melt it: its health also grows with the arsenal, it
    // takes only part of each hit unless overheated or hexed weak, and it never loses more than a share of its
    // health a second (the ultimate's meteors included). Under half health it fights harder.
    public float armour = 0.5f;          // share of a hit it takes outside the weak windows
    public float maxLossPerSecond = 0.006f, weakLossPerSecond = 0.01f; // shares of its health: two to three minutes
    // Three acts, by its health: as it was; then under two thirds its driver comes aboard twice a round (in place of
    // the hex) and its guns lob bombs too; under a third it rages (faster, more boarders).
    public float secondAct = 0.66f, rageAt = 0.33f, rage = 1.6f;
    public float phraseShare = 0.05f;    // a phrase played without a miss also takes this share of its health
    int Act => hp < maxHp * rageAt ? 3 : hp < maxHp * secondAct ? 2 : 1;
    bool announcedSecond;
    public float bombDamage = 1.5f * Balance.Scale, bombHealth = 1.5f * Balance.Scale, bombFlight = 2.6f;
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

    // Its turns, round and round: guns, bombs, overheated, the driver comes aboard, guns, a hex on us, boarders,
    // overheated. (kind, seconds)
    static readonly (int kind, float seconds)[] Turns = { (0, 8f), (1, 5f), (3, 4f), (5, 1f), (0, 6f), (4, 2f), (2, 4f), (3, 4f) };
    const int Guns = 0, Bombs = 1, Boarders = 2, Overheated = 3, Hex = 4, Driver = 5;
    // The driver leaping aboard (BrawlEvent.BeginDriver): beaten in our wagon, its train loses this share of health.
    public float driverShare = 0.06f;
    // Waves from the other side of our train, away from the rival: the hero has to fly over and deal with them.
    public float waveEvery = 20f;
    float waveIn, rivalSide;
    int rivalIndex;

    public const int Jam = 0, Weak = 1, Mirror = 2, Statuses = 3;
    readonly float[] ours = new float[Statuses], theirs = new float[Statuses]; // game time each one wears off
    float phraseIn;
    TurnEvent phrase;
    public bool OnUs(int status) => Active && Time.time < ours[status];
    bool OnThem(int status) => Time.time < theirs[status];
    static string StatusName(int status) => L10n.T("status." + status);
    int turn;
    bool wasRaging;
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
    float maxHp, hp, phase, sleeperOffset, startsAt;
    bool Raging => hp < maxHp * rageAt;
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
        rivalIndex = rival;
        along = train.Forward(train.CarCount / 2);
        along.y = 0f;
        along.Normalize();
        float side = Random.value < 0.5f ? 1f : -1f;
        rivalSide = side;
        waveIn = waveEvery * 0.6f;
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
        // Its goods behind: three cargo wagons, the last one lost at three quarters of its health, the next at half,
        // the last at a quarter.
        cargo.Clear();
        string[] goods = { "Cargo_arms", "Cargo_rock", "Cargo_wood" };
        for (int i = 0; i < goods.Length; i++)
        {
            GameObject model = Resources.Load<GameObject>("Models/" + goods[i]);
            if (!model) continue;
            Transform car = Instantiate(model, root).transform;
            car.localPosition = Vector3.right * (-WagonStep * (2 + i));
            foreach (Renderer part in car.GetComponentsInChildren<Renderer>()) TrainSim.Toon(part);
            cargo.Add(car);
        }

        Combat.Instance.ClearField(); // nobody else on the field while the two trains fight it out
        maxHp = health * (1f + perDifficulty * train.Difficulty) * Mathf.Max(1f, train.Strength / 14f); // with how hard the train hits, as the truck
        hp = maxHp;
        // The engine itself is never shot down: its hits are read off it every frame and counted here, under the cap.
        body = Combat.Instance.AddProjectile(Engine, EnginePosition, maxHp * 100f);
        bodyId = body.id;
        label = L10n.T(Locomotives.HeroNameKey(rival)) + " · " + L10n.T(Locomotives.NameKeys[rival]);
        tracers = new Combat.Batch(new Color(1f, 0.35f, 0.25f), false);
        phase = 0f;
        wasRaging = announcedSecond = false;
        turn = 0;
        turnLeft = Turns[0].seconds;
        lastHp = body.hp;
        bombLook = new Combat.Batch(Bomb.color, false, Resources.Load<Mesh>("Models/Enemy_mine"));
        startsAt = Time.unscaledTime + introSeconds;
        System.Array.Clear(ours, 0, Statuses);
        System.Array.Clear(theirs, 0, Statuses);
        phraseIn = phraseEvery * 0.5f;
        phrase = GetComponent<TurnEvent>();
        Active = true;
    }

    readonly List<Transform> cargo = new();

    // A cargo wagon blown off for every quarter of health it has lost: from the back, burning.
    void ShedCargo()
    {
        int keep = Mathf.Clamp(Mathf.CeilToInt(hp / maxHp * 4f) - 1, 0, 3);
        for (int i = cargo.Count - 1; i >= keep; i--)
        {
            if (!cargo[i] || !cargo[i].gameObject.activeSelf) continue;
            Vector3 at = cargo[i].position + Vector3.up;
            Combat.Instance.Burst("grenade", at, 3f);
            Combat.Instance.Burst("rocket", at, 2.4f);
            train.Shake(0.4f);
            cargo[i].gameObject.SetActive(false);
            train.Toast(L10n.T("rival.cargo"));
        }
    }

    Vector3 EnginePosition => root.TransformPoint(Vector3.right * WagonStep) + Vector3.up * 2f;

    void Update()
    {
        if (!Active || train.Paused || train.UltimatePresenting) return;
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
                Combat.Instance.Drop(EnginePosition - Vector3.up * 2f, (3 + train.Difficulty / 6) * (Act == 3 ? 3 : 2) / 2);
            }
            else if (kind == Overheated) train.Toast(L10n.T("rival.weak"));
            else if ((kind == Driver || (kind == Hex && Act >= 2)) && Story.Active && Story.Current.cave)
                GetComponent<CaveEvent>().Begin(rivalIndex, won =>
                {
                    if (!Active) return;
                    if (won) hp = Mathf.Max(1f, hp - maxHp * driverShare * 1.5f); // found it twice: it and its train take it
                    else for (int n = 0; n < 2; n++) { int car = Random.Range(0, train.CarCount); if (train.CarAlive(car)) train.HurtCar(car, 0.5f); }
                });
            else if (kind == Driver || (kind == Hex && Act >= 2))
                GetComponent<BrawlEvent>().BeginDriver(rivalIndex, EnginePosition, won =>
                {
                    if (won && Active) hp = Mathf.Max(1f, hp - maxHp * driverShare); // the engine finishes off the rest
                });
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
                if (all) { Hit(false, status); hp = Mathf.Max(1f, hp - maxHp * phraseShare); }
                else if (most) Hit(true, status);
                train.Toast(L10n.T(all ? "battle.hit" : most ? "battle.back" : "battle.even", StatusName(status)));
            });
        }
        bool weak = kind == Overheated || OnThem(Weak);
        float lost = Mathf.Max(0f, lastHp - body.hp) * (weak ? 2f : armour);
        hp -= Mathf.Min(lost, maxHp * (weak ? weakLossPerSecond : maxLossPerSecond) * dt);
        body.hp = maxHp * 100f;
        lastHp = body.hp;
        if (hp <= 0f)
        {
            Combat.Instance.Despawn(body);
            Win();
            return;
        }
        ShedCargo();
        if (Raging && !wasRaging) { wasRaging = true; train.Toast(L10n.T("rival.rage")); }
        if (Time.unscaledTime >= startsAt && (waveIn -= dt) <= 0f)
        {
            waveIn = waveEvery;
            Vector3 far = Combat.Instance.OffScreen(-Vector3.Cross(Vector3.up, along) * rivalSide); // out of the picture, on our far side
            for (int k = 0; k < 3; k++) // in a few knots spread along that side
                Combat.Instance.DropRaiders(far + along * Random.Range(-14f, 14f), (1 + train.Difficulty / 10) * (Act == 3 ? 3 : 2) / 2);
            train.Toast(L10n.T("rival.wave"));
        }
        if (Act >= 2 && !announcedSecond) { announcedSecond = true; train.Toast(L10n.T("rival.act2")); }
        if (weak && !train.Swarming) train.Panel.Mark(EnginePosition + Vector3.up * 2.5f, "x2", 4); // not over the fight in a wagon
        if ((kind == Bombs || (kind == Guns && Act >= 2)) && !OnThem(Jam) && (bombIn -= dt) <= 0f)
        {
            bombIn = 0.9f / (1f + hitPerDifficulty * train.Difficulty) / (Raging ? rage : 1f) * (kind == Guns ? 2.5f : 1f); // fewer, mixed with the guns
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
                fireIn[i] = fireEvery / (1f + hitPerDifficulty * train.Difficulty) / (Raging ? rage : 1f) * Random.Range(0.8f, 1.2f);
                int target = RandomCar();
                if (target < 0) continue;
                bool miss = OnThem(Mirror); // confused: the shot goes wide
                Vector3 at = train.CarPosition(target) + Vector3.up * 1.5f + (miss ? Random.onUnitSphere * 4f : Vector3.zero), muzzle = guns[i].position + Vector3.up * 0.4f;
                Vector3 flat = at - guns[i].position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f) guns[i].rotation = Quaternion.LookRotation(flat);
                if (!miss) train.Damage(target, shotDamage * (1f + hitPerDifficulty * train.Difficulty));
                Combat.Instance.Burst("spark", muzzle, 1.2f);
                Combat.Instance.Burst("smoke", at, 1f);
                shots.Add((muzzle, at, now + 0.08f));
            }
        if (!train.Swarming) train.BossBar(label, maxHp > 0f ? hp / maxHp : 0f);
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
            if (!b.miss && train.CarAlive(b.car)) train.Damage(b.car, bombDamage * (1f + hitPerDifficulty * train.Difficulty));
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

    public void TestWin()
    {
        if (!Active) return;
        if (body != null && body.id == bodyId && Combat.Instance) Combat.Instance.Despawn(body);
        Win();
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

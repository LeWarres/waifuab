using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The swarm: a cloud of one-coloured robots flies in and covers the train. Then the game is seen from behind
// the gunner of one armed wagon: the cloud hangs in the sky straight ahead and its robots dive at the wagon one
// after another. Move the sight, hold fire, shoot them before they crash. It ends when the swarm is gone (every
// robot shot down or crashed) or when the wagon is wrecked; the damage stays. TrainSim starts it as the leg's
// event; time runs slow outside, the swarm itself runs on real time.
public class SwarmEvent : MonoBehaviour
{
    public Vector2Int swarmSize = new Vector2Int(13, 20); // robots to shoot down: a number between these, each time
    public float diveEvery = 0.5f;          // seconds between one robot leaving the cloud and the next
    public Vector2 flySpeed = new Vector2(5f, 8f); // slowest and fastest robot
    // The cloud waits close and spread right across the sky, so every robot has to be aimed at on its own.
    public float cloudDistance = 22f, cloudHeight = 11f, cloudWide = 20f, cloudTall = 7f;
    [Range(0f, 1f)] public float gunVolume = 0.6f;
    public float robotSize = 2.1f;
    public float crashHeight = 3.6f;        // where they hit: the turret, not the wagon's side
    // Both grow with the difficulty exactly like the ordinary enemies' (Combat.healthPerLevel, damagePerLevel).
    public float robotHealth = 0.8f * Balance.Scale;        // two shots of a level 1 turret
    public float crashDamage = 5f * Balance.Scale;          // to the wagon, per robot that gets through
    // A shot is worth what the wagon's own weapon deals in that time, so upgrading it keeps the fight fair;
    // never less than a level 1 turret, or the slow support weapons could not win at all.
    public float fireEvery = 0.11f, leastDamagePerSecond = 4f * Balance.Scale;
    public float aimAssist = 3.2f;          // degrees off the sight that still hit
    public float stickSpeed = 120f, mouseSpeed = 0.14f; // degrees a second, degrees a pixel
    public int moneyPerKill = 2;
    public float warnSeconds = 2.6f, slowMotion = 0.12f;
    public int coverCount = 45;             // robots in the cloud that covers the train before the fight (only for show)
    [System.NonSerialized] public bool testAtStart = true; // TESTING: the swarm opens every run. Set to false when done.
    public Color swarmColour = new Color(1f, 0.15f, 0.75f);
    public Color skyColour = new Color(0.95f, 0.95f, 0.95f); // how bright the sky painting comes out

    public bool Active { get; private set; }       // from the warning to the end
    public bool Incoming => Active && Time.unscaledTime < startsAt; // the cloud is still arriving, seen from above
    public bool Aiming => Active && !Incoming;
    public Camera View => view; // the gunner's camera, so the HUD can place its damage numbers from that seat

    class Robot
    {
        public Vector3 position, from; // from: where the cover cloud brings it in from (only before the fight)
        public float speed, wobble, divesAt, health, hitAt = -1f;
    }

    TrainSim train;
    InputAction look, move, fire, fireAlt;
    Camera view;
    GameObject sky;
    Material skyPaint;
    AudioSource gunSound; // loops for as long as the trigger is held
    Canvas sight;
    Text counter;
    Combat.Batch robots, tracer;
    readonly List<Robot> swarm = new();
    readonly List<Robot> cover = new();
    readonly List<(Vector3 from, Vector3 to, float born)> shots = new(); // bolts in flight, for show
    const float BoltTime = 0.09f, BoltLength = 7f;
    int wagon, kills, total;
    float shotDamage, crashHurts;
    Transform gunner;
    float startsAt, fireIn, yaw, pitch, baseYaw;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        look = InputSystem.actions.FindAction("Player/Look", true);
        move = InputSystem.actions.FindAction("Player/Move", true);
        fire = InputSystem.actions.FindAction("Player/Attack", true);
        fireAlt = InputSystem.actions.FindAction("Player/Jump", true);
        robots = new Combat.Batch(swarmColour, true, Resources.Load<Mesh>("Models/Enemy_saucer"));
        tracer = new Combat.Batch(new Color(4f, 0.9f, 0.1f), false); // hot orange: it has to show against the sky

        // The gunner's camera: over the main one while the swarm lasts. The main camera stays on underneath,
        // the rest of the game reads it.
        view = new GameObject("GunnerCamera").AddComponent<Camera>();
        view.fieldOfView = 62f;
        view.depth = 10f;
        view.enabled = false;
        // From down here the horizon shows, and the scene has no sky of its own: a big blue shell stands in for one.
        // It wears the biome's own sky painting (Resources/Sky, Tools/gen_sky.py).
        sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sky.GetComponent<Collider>());
        sky.transform.localScale = Vector3.one * 600f;
        // Every normal straight up: the whole shell is lit alike, with no day and night side.
        Mesh round = Instantiate(sky.GetComponent<MeshFilter>().sharedMesh);
        round.normals = System.Array.ConvertAll(round.normals, _ => Vector3.up);
        sky.GetComponent<MeshFilter>().sharedMesh = round;
        skyPaint = TrainSim.Mat(skyColour, false);
        skyPaint.SetInt("_Culling", 0); // seen from inside
        // The painting covers the upper half, horizon at its bottom edge, twice round and mirrored so its ends meet.
        skyPaint.mainTextureScale = new Vector2(2f, 2f);
        skyPaint.mainTextureOffset = new Vector2(0f, -1f);
        var shell = sky.GetComponent<Renderer>();
        shell.sharedMaterial = skyPaint;
        shell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shell.receiveShadows = false;
        sky.SetActive(false);

        gunSound = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), gunVolume);
        gunSound.clip = Resources.Load<AudioClip>("SFX/machinegunsound"); // Assets/MusicSound/Resources/SFX
        gunSound.loop = true;
        gunSound.playOnAwake = false;

        sight = UiKit.NewCanvas("Sight", 6);
        UiKit.Picture(sight.transform, "sight", Vector2.zero, 170f); // red scope reticle, Resources/Icons/sight.png
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.62f);
        Slanted bar = UiKit.Panel(sight.transform, new Vector2(0f, -70f), new Vector2(620f, 52f), glass, glass, 0.12f);
        bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        counter = UiKit.Label(bar.transform, UiKit.Heading, 26, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600f, 44f), true);
        sight.gameObject.SetActive(false);
    }

    void Start()
    {
        if (testAtStart) Invoke(nameof(TestBegin), 1f);
    }

    void TestBegin() => Begin();

    // False if there is no gunner to play it with (TrainSim then picks another event).
    public bool Begin()
    {
        gunner = train.Gunner(out wagon);
        if (!gunner) return false;
        Active = true;
        skyPaint.mainTexture = Resources.Load<Texture2D>("Sky/" + train.BiomeId);
        kills = 0;
        startsAt = Time.unscaledTime + warnSeconds;
        fireIn = 0f;
        baseYaw = yaw = Random.value < 0.5f ? 0f : 180f; // one side of the track or the other
        pitch = 14f;
        Quaternion side = Quaternion.Euler(0f, baseYaw, 0f);

        // The fight: every robot starts in the cloud, straight ahead of the gunner, and dives in its turn.
        swarm.Clear();
        total = Random.Range(swarmSize.x, swarmSize.y + 1);
        Combat combat = Combat.Instance;
        float health = robotHealth * (1f + combat.healthPerLevel * train.Difficulty);
        crashHurts = crashDamage * (1f + combat.damagePerLevel * train.Difficulty);
        Turret gun = gunner.GetComponent<Turret>();
        shotDamage = Mathf.Max(leastDamagePerSecond, gun.Weapon.Damage(gun.Level) / gun.Weapon.Interval(gun.Level)) * fireEvery;
        Vector3 wagonAt = train.CarPosition(wagon);
        for (int i = 0; i < total; i++)
            swarm.Add(new Robot
            {
                position = wagonAt + side * new Vector3(Random.Range(-cloudWide, cloudWide), cloudHeight + Random.Range(-cloudTall, cloudTall), cloudDistance + Random.Range(-5f, 5f)),
                speed = Random.Range(flySpeed.x, flySpeed.y), wobble = Random.value * 10f, divesAt = startsAt + 1.5f + i * diveEvery, health = health,
            });
        // The show before it: a bigger cloud comes in from that side and settles over the whole train.
        cover.Clear();
        float length = train.HalfExtents.x + 3f;
        for (int i = 0; i < coverCount; i++)
            cover.Add(new Robot
            {
                from = side * new Vector3(Random.Range(-18f, 18f), Random.Range(6f, 14f), 46f + Random.Range(0f, 14f)),
                position = new Vector3(Random.Range(-length, length), Random.Range(2.8f, 6.5f), Random.Range(-3.5f, 3.5f)),
                wobble = Random.value * 10f,
            });
        return true;
    }

    void Update()
    {
        if (Active && train.Paused) gunSound.Stop();
        if (!Active || train.Paused) return;
        if (train.Health <= 0f || train.AtStation) { End(null); return; }
        if (Incoming) return;
        if (!view.enabled)
        {
            view.enabled = true;
            sky.SetActive(true);
            sight.gameObject.SetActive(true);
            Time.timeScale = slowMotion; // the train all but stops while this is played
        }
        float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
        Vector3 target = train.CarPosition(wagon) + Vector3.up * 1.6f;
        Vector3 crash = train.CarPosition(wagon) + Vector3.up * crashHeight + Quaternion.Euler(0f, baseYaw, 0f) * Vector3.forward * 1.5f;

        // The sight: right stick or mouse, and the left stick, d-pad or keys as well. Free enough to follow a robot
        // right down to the wagon.
        Vector2 turn = move.ReadValue<Vector2>() * (stickSpeed * dt);
        Vector2 aim = look.ReadValue<Vector2>();
        turn += look.activeControl != null && look.activeControl.device is Pointer ? aim * mouseSpeed : aim * (stickSpeed * dt);
        yaw = Mathf.Clamp(yaw + turn.x, baseYaw - 75f, baseYaw + 75f);
        pitch = Mathf.Clamp(pitch + turn.y, -45f, 60f);
        Quaternion facing = Quaternion.Euler(-pitch, yaw, 0f);
        Vector3 forward = facing * Vector3.forward, eye = target + Vector3.up * 2.4f - Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 5.5f;
        view.transform.SetPositionAndRotation(eye, facing);
        gunner.rotation = Quaternion.Euler(0f, yaw, 0f);

        for (int i = swarm.Count - 1; i >= 0; i--)
        {
            Robot robot = swarm[i];
            if (now < robot.divesAt) // still in the cloud: hovering
            {
                robot.position += Vector3.up * (Mathf.Sin(now * 2f + robot.wobble) * 0.6f * dt);
                continue;
            }
            // They come in level with the turret and crash there, in plain view: never down among the wheels.
            Vector3 to = crash - robot.position;
            if (to.sqrMagnitude < 3f)
            {
                swarm.RemoveAt(i);
                train.Damage(wagon, crashHurts);
                train.Shake(0.25f);
                Combat.Instance.Burst("enemy", robot.position, 1.6f);
                continue;
            }
            Vector3 sway = (Vector3.Cross(to, Vector3.up).normalized * Mathf.Sin(now * 2.6f + robot.wobble) + Vector3.up * Mathf.Cos(now * 2.1f + robot.wobble) * 0.6f) * 4f; // weaving, not a straight line
            robot.position += (to.normalized * robot.speed + sway) * dt;
            robot.position.y = Mathf.Max(robot.position.y, crash.y);
        }

        // Fire while the button is held: whatever is nearest the sight, within the assist, goes down.
        fireIn -= dt;
        bool firing = fire.IsPressed() || fireAlt.IsPressed();
        if (firing != gunSound.isPlaying)
        {
            if (firing) gunSound.Play(); else gunSound.Stop();
        }
        if (firing && fireIn <= 0f)
        {
            fireIn = fireEvery;
            int hit = -1;
            float best = Mathf.Cos(aimAssist * Mathf.Deg2Rad);
            for (int i = 0; i < swarm.Count; i++)
            {
                float along = Vector3.Dot((swarm[i].position - eye).normalized, forward);
                if (along <= best) continue;
                best = along;
                hit = i;
            }
            Vector3 muzzle = target + Vector3.up * 0.6f, end = hit >= 0 ? swarm[hit].position : eye + forward * 60f;
            shots.Add((muzzle, end, now));
            Combat.Instance.Burst("spark", muzzle + (end - muzzle).normalized * 1.2f, 0.5f); // muzzle flash
            if (hit >= 0)
            {
                Robot robot = swarm[hit];
                bool down = (robot.health -= shotDamage) <= 0f;
                train.DamageNumber(robot.position, shotDamage, false); // red, over the robot that was hit
                if (down)
                {
                    Combat.Instance.Burst("grenade", robot.position, 1.3f);
                    swarm.RemoveAt(hit);
                    kills++;
                    train.Earn(moneyPerKill);
                }
                else
                {
                    Combat.Instance.Burst("spark", robot.position, 0.9f); // hit, still flying
                    robot.hitAt = now;
                }
            }
        }

        if (!train.CarAlive(wagon)) End("swarm.lost");
        else if (swarm.Count == 0) End("swarm.won");
    }

    void LateUpdate()
    {
        if (!Active) return;
        float now = Time.unscaledTime, spin = now * 160f;
        if (Incoming)
        {
            // Seen from above: the cloud sweeps in and settles over the train, swirling.
            float arrived = Mathf.SmoothStep(0f, 1f, 1f - (startsAt - now) / warnSeconds * 1.25f);
            foreach (Robot robot in cover)
            {
                Vector3 over = robot.position + new Vector3(Mathf.Sin(now * 2f + robot.wobble), Mathf.Sin(now * 3f + robot.wobble) * 0.4f, Mathf.Cos(now * 2f + robot.wobble)) * 1.2f;
                robots.Add(Vector3.Lerp(robot.from, over, arrived), Vector3.one * 1.3f, Quaternion.Euler(0f, spin + robot.wobble * 40f, 0f));
            }
            robots.Flush();
            return;
        }
        foreach (Robot robot in swarm) // one that has just been hit swells for a moment
            robots.Add(robot.position, Vector3.one * (robotSize * (now - robot.hitAt < 0.12f ? 1.35f : 1f)), Quaternion.Euler(0f, spin + robot.wobble * 40f, 0f));
        robots.Flush();
        // Each shot is a thick bolt that flies from the gun to where it lands.
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var shot = shots[i];
            float along = (now - shot.born) / BoltTime, reach = (shot.to - shot.from).magnitude;
            if (along >= 1f) { shots.RemoveAt(i); continue; }
            float length = Mathf.Min(BoltLength, reach);
            Vector3 way = (shot.to - shot.from) / Mathf.Max(reach, 0.01f);
            tracer.Add(shot.from + way * Mathf.Lerp(length * 0.5f, reach - length * 0.5f, along), new Vector3(0.28f, 0.28f, length), Quaternion.LookRotation(way));
        }
        tracer.Flush();
        counter.text = L10n.T("swarm.hud", swarm.Count, Mathf.RoundToInt(train.CarFill(wagon) * 100f), JumpEvent.ButtonName(train.Player.UsingGamepad));
    }

    void End(string key)
    {
        Active = false;
        view.enabled = false;
        sky.SetActive(false);
        sight.gameObject.SetActive(false);
        gunSound.Stop();
        swarm.Clear();
        shots.Clear();
        Time.timeScale = 1f;
        if (key != null) train.Toast(L10n.T(key, kills, kills * moneyPerKill));
    }
}

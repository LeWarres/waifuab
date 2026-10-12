using UnityEngine;
using UnityEngine.InputSystem;

// The hero: flies around the train (touches nothing), locks onto one enemy and shoots with the one
// weapon it has; upgrades evolve it, change its ammo or raise the hero's stats. Reads the project-wide input actions, so keyboard+mouse and
// gamepad both work and the active one is whichever device was really used last.
public class Player : MonoBehaviour
{
    public float speed = 12f;
    public float sprint = 1.7f;    // speed multiplier while the sprint button is held
    public float maxHealth = 30f * Balance.Scale;
    public float hover = 2.5f;     // flying height
    public float leash = 27f;      // how far from the train the hero may wander
    public float aimAssist = 20f;  // degrees around the aim line that still pick an enemy

    public float armor;            // share of incoming damage ignored, raised by upgrades
    public const float MaxArmor = 0.6f;
    public float damageBonus = 1f; // multiplies every shot, raised by upgrades
    public float moneyBonus = 1f;  // multiplies the money from kills, raised by upgrades
    public Weapon ammo = Weapon.Hero[0]; // what the weapon fires
    public int level = 1;                // how far the weapon has evolved

    public float Health { get; private set; }
    public bool Alive => Health > 0f;
    public bool UsingGamepad { get; private set; }
    public Vector3 Position => transform.position;

    const float Deadzone = 0.25f;
    static readonly Vector3 SpawnPoint = new Vector3(0f, 2.5f, -4f);
    // The rifle's muzzle, in the rifle's own frame (Tools/make_hero_gear.py: barrel tip at 0.65 * (0, -1.54, 0.13)).
    static readonly Vector3 MuzzleLocal = new Vector3(0f, 0.085f, 1f);

    readonly Combat.Batch marker = new Combat.Batch(Color.red, false); // frame around the locked enemy
    TrainSim train;
    GameObject visuals;
    Transform rifle;
    InputAction move, look, next, previous, targetNext, targetPrevious, run;
    InputAction[] buttons;
    Combat.Enemy target;
    int targetId;
    Vector3 aim = Vector3.forward;
    float cooldown;

    bool Locked => target != null && target.id == targetId;

    // What moves on her as she flies (Animate): both thighs, both shins, the spine; how each stood when she was posed.
    readonly Transform[] joints = new Transform[5];
    readonly Quaternion[] posed = new Quaternion[5];
    readonly Smoke[] flames = new Smoke[2];
    Vector3 before, lean; // where the hero was last frame; how fast it is going, seen from itself (1 = full speed)
    float kick;           // 1 on a shot, gone in a moment

    public void Init(TrainSim train)
    {
        this.train = train;
        Health = maxHealth;

        transform.position = SpawnPoint;
        visuals = new GameObject("Hero");
        visuals.transform.SetParent(transform, false);
        Dress();

        InputActionAsset actions = InputSystem.actions;
        actions.Enable();
        move = actions.FindAction("Player/Move", true);
        look = actions.FindAction("Player/Look", true);
        next = actions.FindAction("Player/Next", true);
        previous = actions.FindAction("Player/Previous", true);
        targetNext = actions.FindAction("Player/TargetNext", true);
        targetPrevious = actions.FindAction("Player/TargetPrevious", true);
        run = actions.FindAction("Player/Sprint", true);
        buttons = new[] { run, next, previous, targetNext, targetPrevious };
    }

    // The hero's looks: the character two units tall about the hero's own middle, facing +z, put once into a
    // flying pose (legs straight down, arms forward on the rifle); a rifle in her hand, a jetpack on her back
    // (Tools/make_hero_gear.py) with a flame under each nozzle.
    void Dress()
    {
        Transform body = Instantiate(Resources.Load<GameObject>("Characters/bba/bba"), visuals.transform).transform;
        Material skin = Resources.Load<Material>("Characters/bba/bba");
        foreach (Renderer part in body.GetComponentsInChildren<Renderer>()) part.sharedMaterial = skin;
        Animator rig = body.GetComponent<Animator>();
        rig.enabled = false; // nothing animates her: the bones stay where they are put here
        Transform Bone(HumanBodyBones bone) => rig.GetBoneTransform(bone);
        body.localScale *= 2f / ((Bone(HumanBodyBones.Head).position.y - Bone(HumanBodyBones.LeftFoot).position.y) * 1.15f);
        // The model comes standing in a fighter's stance, off to one side of its own origin. Squared up to face +z,
        // then every limb pointed where it should go, whatever way it came: legs straight down, arms out to the rifle.
        Transform hips = Bone(HumanBodyBones.Hips);
        void Point(HumanBodyBones from, HumanBodyBones to, Vector3 along)
        {
            Transform bone = Bone(from), end = Bone(to);
            if (bone && end) bone.rotation = Quaternion.FromToRotation(end.position - bone.position, along) * bone.rotation;
        }
        hips.rotation = Quaternion.FromToRotation(Flat(Bone(HumanBodyBones.RightUpperArm).position - Bone(HumanBodyBones.LeftUpperArm).position), Vector3.right) * hips.rotation;
        Point(HumanBodyBones.Hips, HumanBodyBones.Head, Vector3.up);
        foreach (bool left in new[] { true, false })
        {
            Point(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg, left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, Vector3.down);
            Point(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, Vector3.down);
            Point(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, left ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes, new Vector3(0f, -0.6f, 1f));
        }
        Point(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, new Vector3(0f, -0.5f, 1f));
        Point(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, Vector3.forward);
        Point(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, new Vector3(0.1f, -0.4f, 1f));
        Point(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, new Vector3(0.45f, 0.1f, 1f));
        // Her hips over the hero's own middle, so she turns on the spot and the shots leave from her.
        body.position += Flat(visuals.transform.position - hips.position) + Vector3.up * (visuals.transform.position.y + 0.75f - Bone(HumanBodyBones.Head).position.y);

        Transform Gear(string model, Vector3 at)
        {
            Transform gear = Instantiate(Resources.Load<GameObject>(model), visuals.transform).transform;
            gear.position = at;
            foreach (Renderer part in gear.GetComponentsInChildren<Renderer>()) TrainSim.Toon(part);
            return gear;
        }
        // Rifle and jetpack ride on her bones, so they go with her when she moves.
        rifle = Gear("Models/HeroRifle", Bone(HumanBodyBones.RightHand).position);
        rifle.SetParent(Bone(HumanBodyBones.RightHand), true);
        Transform back = Bone(HumanBodyBones.Chest) ?? Bone(HumanBodyBones.Spine);
        Transform pack = Gear("Models/HeroJetpack", back.position + Vector3.back * 0.22f);
        pack.SetParent(back, true);
        var moving = new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.Spine };
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i] = Bone(moving[i]);
            posed[i] = joints[i].rotation; // she faces +z here, so this is the joint as seen from her
        }
        before = transform.position;
        foreach (float side in new[] { -0.2f, 0.2f })
        {
            // Fire out of the nozzle: Smoke's puffs, bright, quick and sent downwards.
            var flame = new GameObject("Flame").AddComponent<Smoke>();
            flame.transform.SetParent(pack, false);
            flame.transform.localPosition = new Vector3(side, -0.52f, -0.22f);
            flame.colour = new Color(3f, 1.1f, 0.15f); // over 1: it blooms
            flame.every = 0.025f; flame.life = 0.28f; flame.rise = -6f; flame.size = 0.42f; flame.trail = 0f; flame.wander = 0.6f;
            flames[side < 0f ? 0 : 1] = flame;
        }
    }

    // Life in her, every frame, from how the hero is moving: she leans into her flight and banks into a slide,
    // her legs trail behind and paddle a little, her chest sways, a shot kicks her back, the jets burn harder at speed.
    void Animate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || !visuals.activeSelf) return;
        Quaternion facing = Quaternion.LookRotation(aim);
        Vector3 going = Quaternion.Inverse(facing) * Flat(transform.position - before) / (dt * speed * sprint);
        before = transform.position;
        lean = Vector3.Lerp(lean, Vector3.ClampMagnitude(going, 1f), 1f - Mathf.Exp(-7f * dt)); // also swallows the jump of a turn of the world
        kick = Mathf.Max(0f, kick - dt * 7f);
        float time = Time.time, paddle = Mathf.Sin(time * 2.3f) * 5f;
        visuals.transform.rotation = facing * Quaternion.Euler(lean.z * 20f, 0f, -lean.x * 16f);
        Quaternion turn = visuals.transform.rotation;
        for (int leg = 0; leg < 2; leg++)
        {
            // About her right: the leg swings back; about her front: it swings out to the side.
            Quaternion thigh = Quaternion.Euler(6f + lean.z * 24f + (leg == 0 ? paddle : -paddle), 0f, -lean.x * 16f + (leg == 0 ? -3f : 3f));
            joints[leg].rotation = turn * thigh * posed[leg];
            joints[leg + 2].rotation = turn * thigh * Quaternion.Euler(14f + Mathf.Max(0f, lean.z) * 26f + (leg == 0 ? -paddle : paddle), 0f, 0f) * posed[leg + 2];
        }
        joints[4].rotation = turn * Quaternion.Euler(Mathf.Sin(time * 1.7f) * 1.5f - kick * 8f, Mathf.Sin(time * 1.1f) * 3f, 0f) * posed[4];
        foreach (Smoke flame in flames) flame.size = 0.42f * (1f + lean.magnitude * 0.7f);
    }

    void Update()
    {
        // A screen left behind (the story's buttons) switches every action off as it goes, after Init switched
        // them on: back on, for the hero and for the events that read the same actions.
        if (!move.enabled) InputSystem.actions.Enable();
        // Out of the picture while the game is seen from a gunner's seat.
        if (visuals.activeSelf != (Alive && !train.Swarming)) visuals.SetActive(Alive && !train.Swarming);
        if (!Alive || Time.timeScale == 0f) return;

        // Whatever device was really used last is the one in use (a drifting stick does not count).
        Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        if (input.magnitude < Deadzone) input = Vector2.zero;
        Track(move, input.magnitude);
        Track(look, look.ReadValue<Vector2>().magnitude);
        foreach (InputAction button in buttons) Track(button, button.IsPressed() ? 1f : 0f);

        // The station is the safe spot: the hero waits while the menu has the controls.
        if (train.AtStation || train.InEvent)
        {
            target = null;
            return;
        }

        float range = ammo.range;
        Combat combat = Combat.Instance;

        // Movement relative to the camera, so "up" on the stick is "up" on the screen.
        Transform cam = Camera.main.transform;
        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized, right = cam.right;
        Vector3 walk = (right * input.x + forward * input.y) * (run.IsPressed() ? speed * sprint : speed);
        Vector3 p = train.Focus + Vector3.ClampMagnitude(Flat(transform.position) + walk * Time.deltaTime - train.Focus, Mathf.Max(leash, train.Reach));
        // Flying around the train, never through it: pushed out by the nearest side.
        Vector2 half = train.HalfExtents + Vector2.one * 0.8f;
        float insideX = half.x - Mathf.Abs(p.x), insideZ = half.y - Mathf.Abs(p.z);
        if (insideX > 0f && insideZ > 0f && !train.Bending) // mid-curve the train is not a straight box
        {
            if (insideZ <= insideX) p.z = p.z >= 0f ? half.y : -half.y;
            else p.x = p.x >= 0f ? half.x : -half.x;
        }
        p.y = hover + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = p;

        // The lock drops when the enemy dies or leaves the weapon's range.
        if (!Locked || Flat(target.position - p).sqrMagnitude > range * range) target = null;

        float cone = Mathf.Cos(aimAssist * Mathf.Deg2Rad);
        if (UsingGamepad)
        {
            // Right stick points at an enemy, R1/L1 step to the next one around, otherwise the nearest.
            Vector2 stick = look.ReadValue<Vector2>();
            if (stick.magnitude > Deadzone)
            {
                aim = (right * stick.x + forward * stick.y).normalized;
                Lock(combat.Aimed(p, aim, range, cone) ?? target);
            }
            else if (input != Vector2.zero) aim = walk.normalized;
            if (targetNext.WasPressedThisFrame()) Lock(combat.Cycle(p, target, range, 1) ?? target);
            if (targetPrevious.WasPressedThisFrame()) Lock(combat.Cycle(p, target, range, -1) ?? target);
        }
        else if (Mouse.current != null)
        {
            // Mouse: pointing at an enemy picks it; otherwise the lock stays where it is.
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
            {
                Vector3 cursor = ray.GetPoint(distance);
                if (Flat(cursor - p).sqrMagnitude > 0.01f) aim = Flat(cursor - p).normalized;
                Combat.Enemy pointed = combat.Nearest(cursor, 2.5f, null, true);
                if (pointed != null && Flat(pointed.position - p).sqrMagnitude <= range * range) Lock(pointed);
            }
        }
        if (target == null) Lock(combat.Nearest(Flat(p), range, null, true)); // re-aim: always the enemy closest to the hero
        if (target != null && Flat(target.position - p).sqrMagnitude > 0.01f) aim = Flat(target.position - p).normalized;

        // Fires on its own whenever there is someone to shoot at.
        cooldown -= Time.deltaTime;
        if (cooldown > 0f || target == null) return;
        cooldown = ammo.Interval(level);
        // Shots leave the barrel so they line up with the muzzle flash.
        combat.Volley(rifle ? rifle.TransformPoint(MuzzleLocal) : p, target, ammo, level, true);
        kick = 1f;
    }

    // Red frame on the ground around whoever is locked, plus a pointer above it.
    void LateUpdate()
    {
        Animate();
        if (!Alive || !Locked || train.Swarming) return;
        float half = target.type.size * 0.5f + 0.4f, side = half * 2f + 0.15f;
        Vector3 c = new Vector3(target.position.x, 0.08f, target.position.z);
        marker.Add(c + Vector3.forward * half, new Vector3(side, 0.1f, 0.15f));
        marker.Add(c + Vector3.back * half, new Vector3(side, 0.1f, 0.15f));
        marker.Add(c + Vector3.right * half, new Vector3(0.15f, 0.1f, side));
        marker.Add(c + Vector3.left * half, new Vector3(0.15f, 0.1f, side));
        marker.Add(target.position + Vector3.up * (target.type.size * 0.5f + 1.2f), new Vector3(0.3f, 0.6f, 0.3f));
        marker.Flush();
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    void Lock(Combat.Enemy enemy)
    {
        target = enemy;
        if (enemy != null) targetId = enemy.id;
    }

    void Track(InputAction action, float amount)
    {
        InputControl control = action.activeControl;
        if (amount < Deadzone || control == null) return;
        if (control.device is Pointer && amount < 4f) return; // a nudged mouse is not a change of device
        UsingGamepad = control.device is Gamepad;
    }

    public void Damage(float amount)
    {
        if (!Alive) return;
        Health = Mathf.Max(0f, Health - amount * (1f - armor));
        train.Flash("blood");
        if (!Alive) visuals.SetActive(false); // down: the train carries on alone until the next station
    }

    public void Rotate(Quaternion turn)
    {
        transform.position = turn * transform.position;
        aim = turn * aim;
    }

    public void Heal(float amount)
    {
        if (Health < maxHealth && amount > 0f) train.Flash("healing");
        Health = Mathf.Min(maxHealth, Health + amount);
    }

    // Station upgrades.
    public void SpeedUp() => speed *= 1.1f;
    public void DamageUp() => damageBonus += 0.2f;
    public void GreedUp() => moneyBonus += 0.25f;
    public void ArmorUp() => armor = Mathf.Min(MaxArmor, armor + 0.1f);
    public void VitalityUp(float amount)
    {
        maxHealth += amount;
        Heal(amount);
    }

    // Called on arrival: a fallen hero gets back up with 1 health and has to buy the rest.
    public void OnStation()
    {
        if (Alive) return;
        Health = Balance.Scale; // back on their feet with a scratch: the rest is bought
        transform.position = SpawnPoint;
        visuals.SetActive(true);
    }
}

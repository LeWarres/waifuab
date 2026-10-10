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

    readonly Combat.Batch marker = new Combat.Batch(Color.red, false); // frame around the locked enemy
    TrainSim train;
    GameObject visuals;
    InputAction move, look, next, previous, targetNext, targetPrevious, run;
    InputAction[] buttons;
    Combat.Enemy target;
    int targetId;
    Vector3 aim = Vector3.forward;
    float cooldown;

    bool Locked => target != null && target.id == targetId;

    public void Init(TrainSim train)
    {
        this.train = train;
        Health = maxHealth;

        transform.position = SpawnPoint;
        visuals = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(visuals.GetComponent<Collider>()); // flying: collides with nothing
        visuals.GetComponent<Renderer>().sharedMaterial = TrainSim.Mat(new Color(0.1f, 0.4f, 1f));
        visuals.transform.SetParent(transform, false);
        TrainSim.Box(visuals.transform, new Vector3(0f, 0.2f, 0.6f), new Vector3(0.2f, 0.2f, 0.9f), TrainSim.Mat(Color.black)); // gun, along +z

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

    void Update()
    {
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
        visuals.transform.rotation = Quaternion.LookRotation(aim);

        // Fires on its own whenever there is someone to shoot at.
        cooldown -= Time.deltaTime;
        if (cooldown > 0f || target == null) return;
        cooldown = ammo.Interval(level);
        combat.Volley(p, target, ammo, level, true);
    }

    // Red frame on the ground around whoever is locked, plus a pointer above it.
    void LateUpdate()
    {
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

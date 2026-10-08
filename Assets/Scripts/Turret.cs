using UnityEngine;

// A weapon mounted on a wagon. Locks onto one enemy and keeps firing at it until it dies,
// then picks the nearest one. What a shot does comes from the Weapon stats and the level.
public class Turret : MonoBehaviour
{
    public Weapon Weapon { get; private set; }
    public int Level { get; private set; } = 1;
    [System.NonSerialized] public float stunnedUntil; // set by saboteur enemies
    Combat.Enemy target;
    int targetId;
    float cooldown;

    public void Init(Weapon weapon)
    {
        Weapon = weapon;
        TrainSim.Box(transform, Vector3.zero, new Vector3(1f, 0.5f, 1f), weapon.Material);
        TrainSim.Box(transform, new Vector3(0f, 0.15f, 0.6f), new Vector3(0.2f, 0.2f, 1.2f), weapon.Material); // barrel along +z
    }

    public void LevelUp()
    {
        Level++;
        transform.localScale = Vector3.one * (1f + 0.15f * (Level - 1)); // bigger turret = higher level
    }

    void Update()
    {
        Combat combat = Combat.Instance;
        cooldown -= Time.deltaTime;
        if (Time.time < stunnedUntil) return;
        if (target == null || target.id != targetId)
        {
            target = combat.Nearest(transform.position, Weapon.range);
            if (target == null) return;
            targetId = target.id;
        }

        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat != Vector3.zero) transform.rotation = Quaternion.LookRotation(flat);

        if (cooldown > 0f) return;
        cooldown = Weapon.Interval(Level);

        combat.Volley(transform.position + Vector3.up * 0.15f, target, Weapon, Level);
    }
}

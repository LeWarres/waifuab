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
    const float ModelScale = 1.3f;
    // The twin barrels, in the turret's own frame (Tools/make_wagon.py: muzzle 1.36 up the barrel, 0.3 up, times ModelScale).
    static readonly Vector3 MuzzleLocal = new Vector3(0f, 0.465f, 1.77f);

    public void Init(Weapon weapon)
    {
        Weapon = weapon;
        // The gunner in her turret, barrels along +z; the gun takes the weapon's colour.
        Transform model = Instantiate(Resources.Load<GameObject>("Models/TurretGirl"), transform).transform;
        model.localScale = Vector3.one * ModelScale;
        model.localPosition = Vector3.up * 0.25f * (ModelScale - 1f); // keeps its base on the wagon roof
        Renderer skin = model.GetComponentInChildren<Renderer>();
        Material[] mats = skin.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i].name == "TurretAccent") mats[i] = weapon.Material;
        skin.sharedMaterials = mats;
        TrainSim.Toon(skin);
    }

    public void LevelUp()
    {
        Level++;
        transform.localScale = Vector3.one * (1f + 0.15f * (Level - 1)); // bigger turret = higher level
    }

    void Update()
    {
        Combat combat = Combat.Instance;
        if (combat.Paused) return;
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
        cooldown = Weapon.Interval(Level) / Mathf.Max(1f, combat.FireRate);

        combat.Volley(transform.TransformPoint(MuzzleLocal), target, Weapon, Level);
    }
}

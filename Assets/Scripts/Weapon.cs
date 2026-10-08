using UnityEngine;

// ponytail: weapon stats live in code; move to ScriptableObjects when they need Inspector tuning.
public class Weapon
{
    public const int MaxLevel = 5;

    public string id; // key of its texts in the string table: weapon.<id>.name / weapon.<id>.info
    public string Name => L10n.T(nameKey ??= $"weapon.{id}.name");
    public string Info => L10n.T(infoKey ??= $"weapon.{id}.info");
    string nameKey, infoKey;
    public Color color = Color.yellow;
    public float range = 25f, interval = 0.25f, speed = 40f, damage = 1f;
    public int firstBiome;   // offered from this biome onwards
    public float splash;     // > 0: damages every enemy within this radius of the impact
    public int targets = 1;  // shots per volley, each at a different enemy
    public int extraTargets; // more shots per volley for each level
    public int chain;        // extra enemies the bullet jumps to after a hit
    public float slow;       // seconds the hit enemy moves at half speed
    public bool pulse;       // no bullet: damages everything in range around the weapon
    public bool strike;      // no bullet: lightning falls straight on the target, instantly

    Material material;
    public Material Material => material ? material : material = TrainSim.Mat(color);

    Combat.Batch batch; // instanced draw of this weapon's bullets
    public Combat.Batch Batch => batch ??= new Combat.Batch(color, false);

    // Level scaling: level 5 deals 3.4x damage and fires 1.5x faster, about 5x the level 1 output.
    public float Damage(int level) => damage * (1f + 0.6f * (level - 1));
    public float Interval(int level) => interval * Mathf.Pow(0.9f, level - 1);
    public float Splash(int level) => splash * (1f + 0.1f * (level - 1));
    public int Chain(int level) => chain > 0 ? chain + level - 1 : 0;
    public int Targets(int level) => targets + extraTargets * (level - 1);

    // Level 1 output is close to 4-8 damage per second each; they differ in reach and in how they handle crowds.
    public static readonly Weapon[] Wagon =
    {
        new Weapon { id = "turret" },
        new Weapon { id = "rocket", color = new Color(1f, 0.4f, 0f), range = 30f, interval = 1.5f, speed = 18f, damage = 4f, splash = 4f },
        new Weapon { id = "minigun", color = Color.white, range = 16f, interval = 0.1f, damage = 0.6f },
        new Weapon { id = "shotgun", color = new Color(0.8f, 0.5f, 0.2f), range = 12f, interval = 0.9f, damage = 1.5f, targets = 5, extraTargets = 1 },
        new Weapon { id = "sniper", color = Color.red, range = 45f, interval = 2f, speed = 120f, damage = 12f },
        new Weapon { id = "pulse", color = Color.magenta, range = 9f, interval = 1.2f, damage = 2.5f, pulse = true },
        new Weapon { id = "storm", color = new Color(0.7f, 0.8f, 1f), range = 22f, interval = 1.2f, damage = 2.5f, extraTargets = 1, strike = true },
        new Weapon { id = "flame", color = new Color(1f, 0.2f, 0f), range = 8f, interval = 0.1f, speed = 20f, damage = 0.5f, splash = 1.5f, firstBiome = 1 }, // Nieve
        new Weapon { id = "freeze", color = new Color(0.5f, 0.7f, 1f), range = 20f, interval = 0.4f, slow = 2f, firstBiome = 3 },                                  // Volcán
        new Weapon { id = "chain", color = Color.cyan, range = 20f, interval = 0.8f, speed = 80f, damage = 1.5f, chain = 3, firstBiome = 5 },                 // Planeta alienígena
    };

    // The hero's own guns: cousins of the wagon weapons, but shorter-ranged and faster, so they reward flying close.
    public static readonly Weapon[] Hero =
    {
        new Weapon { id = "blaster", color = new Color(0.3f, 0.6f, 1f), range = 18f, interval = 0.2f },
        new Weapon { id = "grenade", color = new Color(0.4f, 0.8f, 0.2f), range = 16f, interval = 0.9f, speed = 25f, damage = 3f, splash = 2.5f },
        new Weapon { id = "smg", color = new Color(0.9f, 0.9f, 0.6f), range = 13f, interval = 0.08f, damage = 0.5f },
        new Weapon { id = "scatter", color = new Color(1f, 0.6f, 0.3f), range = 10f, interval = 0.7f, damage = 2f, targets = 3, extraTargets = 1 },
        new Weapon { id = "rail", color = new Color(0.6f, 0.3f, 1f), range = 30f, interval = 1.4f, speed = 120f, damage = 8f, chain = 1 },
        new Weapon { id = "nova", color = new Color(1f, 0.4f, 0.8f), range = 7f, interval = 1f, damage = 3f, pulse = true },
        new Weapon { id = "spark", color = new Color(0.8f, 1f, 1f), range = 16f, interval = 0.9f, damage = 2f, extraTargets = 1, strike = true },
    };
}

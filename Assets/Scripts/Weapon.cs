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
    public bool fan;         // evolves into shooting several ways at once: see Directions
    public float growSplash; // radius of that blast gained with every level: a one-enemy weapon that learns to hit a patch
    public int targets = 1;  // shots per volley, each at a different enemy
    public int extraTargets; // more shots per volley for each level
    public int chain;        // extra enemies the bullet jumps to after a hit
    public float slow;       // seconds the hit enemy moves at half speed
    public bool pulse;       // no bullet: damages everything in range around the weapon
    public bool strike;      // no bullet: lightning falls straight on the target, instantly
    public bool beam;        // no bullet: a weak instant ray that crosses every enemy on the line

    Material material;
    public Material Material => material ? material : material = TrainSim.Mat(color);

    Combat.Batch batch; // instanced draw of this weapon's bullets
    public Combat.Batch Batch => batch ??= new Combat.Batch(color, false);

    // Level scaling: level 5 deals 3x damage and fires 1.5x faster, about 4.5x the level 1 output.
    public float Damage(int level) => damage * (1f + 0.5f * (level - 1)) * Balance.Scale;
    public float Interval(int level) => interval * Mathf.Pow(0.9f, level - 1);
    public float Splash(int level) => splash * (1f + 0.2f * (level - 1)) + growSplash * (level - 1);
    public int Chain(int level) => chain > 0 ? chain + level - 1 : 0;
    public int Targets(int level) => targets + extraTargets * (level - 1);
    // A fanning weapon: one way up to level 2, two at level 3, three at 4, five at 5. The first shot is the aimed
    // one; the others leave evenly round it and fly free, hitting whatever they meet.
    public int Directions(int level) => !fan || level < 3 ? 1 : level == 3 ? 2 : level == 4 ? 3 : 5;

    // Level 1 output is close to 4-8 damage per second each; they differ in reach and in how they handle crowds.
    // The road is thick with enemies, so nothing stays a one-enemy weapon as it evolves: every level it shoots at
    // one more of them, or its shot bursts wider, or it goes through one more.
    public static readonly Weapon[] Wagon =
    {
        new Weapon { id = "turret", fan = true },
        new Weapon { id = "rocket", color = new Color(0.25f, 0.55f, 0.2f), range = 30f, interval = 1.5f, speed = 18f, damage = 4f, splash = 4f },
        new Weapon { id = "minigun", color = new Color(0.9f, 0.92f, 0.95f), range = 16f, interval = 0.1f, damage = 0.6f, growSplash = 0.5f },
        new Weapon { id = "shotgun", color = new Color(0.55f, 0.33f, 0.14f), range = 12f, interval = 0.9f, damage = 1.5f, targets = 5, extraTargets = 1 },
        new Weapon { id = "sniper", color = new Color(0.5f, 0.04f, 0.1f), range = 45f, interval = 2f, speed = 120f, damage = 12f, chain = 1 },
        new Weapon { id = "pulse", color = Color.magenta, range = 9f, interval = 1.2f, damage = 2.5f, pulse = true },
        new Weapon { id = "storm", color = new Color(0.5f, 0.3f, 0.95f), range = 22f, interval = 1.2f, damage = 2.5f, extraTargets = 1, strike = true },
        new Weapon { id = "flame", color = new Color(1f, 0.35f, 0f), range = 8f, interval = 0.1f, speed = 20f, damage = 0.5f, splash = 1.5f, firstBiome = 1 }, // Nieve
        new Weapon { id = "freeze", color = new Color(0.25f, 0.5f, 1f), range = 20f, interval = 0.4f, slow = 2f, extraTargets = 1, firstBiome = 4 },                                  // Volcán
        new Weapon { id = "chain", color = Color.cyan, range = 20f, interval = 0.8f, speed = 80f, damage = 1.5f, chain = 3, firstBiome = 7 },                 // Planeta alienígena
        new Weapon { id = "laser", color = new Color(1f, 0.1f, 0.4f), range = 30f, interval = 0.5f, damage = 0.8f, beam = true, fan = true },                              // débil, pero atraviesa la línea entera
    };

    // The hero's own guns: cousins of the wagon weapons, but shorter-ranged and faster, so they reward flying close.
    public static readonly Weapon[] Hero =
    {
        new Weapon { id = "blaster", color = new Color(0.3f, 0.6f, 1f), range = 18f, interval = 0.2f, fan = true },
        new Weapon { id = "grenade", color = new Color(0.4f, 0.8f, 0.2f), range = 16f, interval = 0.9f, speed = 25f, damage = 3f, splash = 2.5f },
        new Weapon { id = "smg", color = new Color(0.9f, 0.9f, 0.6f), range = 13f, interval = 0.08f, damage = 0.5f, growSplash = 0.4f },
        new Weapon { id = "scatter", color = new Color(1f, 0.6f, 0.3f), range = 10f, interval = 0.7f, damage = 2f, targets = 3, extraTargets = 1 },
        new Weapon { id = "rail", color = new Color(0.6f, 0.3f, 1f), range = 30f, interval = 1.4f, speed = 120f, damage = 8f, chain = 1 },
        new Weapon { id = "nova", color = new Color(1f, 0.4f, 0.8f), range = 7f, interval = 1f, damage = 3f, pulse = true },
        new Weapon { id = "spark", color = new Color(0.8f, 1f, 1f), range = 16f, interval = 0.9f, damage = 2f, extraTargets = 1, strike = true },
    };
}

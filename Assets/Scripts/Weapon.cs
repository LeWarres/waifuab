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
    public int style;        // how its shots move and look (Combat): one of the kinds below
    public int aim;          // whom a wagon's gun picks (Combat.Pick): one of the Aim kinds below
    public float push = 1f;  // how hard a hit knocks the enemy back from the train (Combat.Hurt), against the usual
    public string evolvesWith; // the hero's upgrade (TrainSim.Passives) that, owned with this at its top level, evolves it
    public string EvolvedName => L10n.T($"weapon.{id}.evo");
    public const int Evolved = MaxLevel + 1; // the level an evolved weapon stands at

    // Straight: a glowing streak. Lob: thrown up and over. Swirl: curls in on its target. Boomerang: a spinning
    // blade out and back, through everyone. Orbit: orbs circling the hero. Wobble: a stream that snakes.
    // Flame: puffs that swell as they go. Shard: a spinning ice crystal. Pellets: a spray of small shot.
    // Slash: the hero's sword cuts the air, a crescent through everyone; at the last level a blue dragon in a zigzag.
    public const int Straight = 0, Lob = 1, Swirl = 2, Boomerang = 3, Orbit = 4, Wobble = 5, Flame = 6, Shard = 7, Pellets = 8, Slash = 9;
    // Shots only evolutions make: the Plasma Comet's comet (the blaster) and the Barrage's falling shells (the turret).
    public const int Comet = 10, Shell = 11;
    // Vampire Survivors' aiming kinds, plus two a train needs: Nearest the gun (Magic Wand), Random in reach (Fire
    // Wand, Lightning Ring: spreads the fire instead of every wagon shooting the same one), Danger: the one closest to
    // the train (about to hit it), Strongest: the most health in reach (giants and elites).
    public const int AimNearest = 0, AimRandom = 1, AimDanger = 2, AimStrongest = 3;

    Material material;
    public Material Material => material ? material : material = TrainSim.Mat(color);

    Combat.Batch batch; // instanced draw of this weapon's bullets
    public Combat.Batch Batch => batch ??= new Combat.Batch(color, false, glow: 4f); // bright enough to bloom

    // Level scaling: level 5 deals 3x damage and fires 1.5x faster, about 4.5x the level 1 output.
    public float Damage(int level) => damage * (1f + 0.5f * (level - 1)) * Balance.Scale;
    public float Interval(int level) => interval * Mathf.Pow(0.9f, level - 1) * (style == Slash && level >= Evolved ? 3f : 1f); // the dragon takes its time to come back
    public float Splash(int level) => splash * (1f + 0.2f * (level - 1)) + growSplash * (level - 1);
    public int Chain(int level) => chain > 0 ? chain + level - 1 + (level >= Evolved ? 3 : 0) : 0; // evolved: three more jumps
    public int Targets(int level) => targets + extraTargets * (level - 1);
    // A fanning weapon: one way up to level 2, two at level 3, three at levels 4 and 5. The first shot is the aimed
    // one; the others leave evenly round it and fly free, hitting whatever they meet.
    // Evolved, it fans no more: it has its own special instead (the Plasma Comet, the Barrage, the Prism).
    public int Directions(int level) => !fan || level < 3 || level >= Evolved ? 1 : level == 3 ? 2 : 3;
    // The way the n-th of those leaves, in degrees off the aimed one: all forward, side by side.
    public static float Spread(int way) => way == 0 ? 0f : way == 1 ? 14f : -14f;

    // Level 1 output is close to 4-8 damage per second each; they differ in reach and in how they handle crowds.
    // The road is thick with enemies, so nothing stays a one-enemy weapon as it evolves: every level it shoots at
    // one more of them, or its shot bursts wider, or it goes through one more.
    // The mid boss's wagon, won by beating the truck (TrainSim.GiveTruck): its curling rockets. Never offered at a
    // station and never evolved; it only levels up.
    public static readonly Weapon Truck = new Weapon
    {
        id = "truck", color = new Color(0.9f, 0.55f, 0.15f), range = 28f, interval = 1.4f, speed = 18f, damage = 4f, splash = 3.5f,
        targets = 2, extraTargets = 1, style = Swirl,
    };

    // Each hero's weapon, for good (Locomotives.Heroes' ids): it levels up and evolves, it is never swapped.
    static readonly System.Collections.Generic.Dictionary<string, string> HeroWeapons = new()
    {
        { "bba", "blaster" }, { "nath", "nova" }, { "onini", "masamune" }, { "tzmil", "scatter" }, { "sentinel", "rail" },
        { "culu", "grenade" }, { "cerebron", "spark" }, { "xiuhtecuhtli", "smg" }, { "faceless", "rail" },
    };
    public static Weapon ForHero(string hero) =>
        HeroWeapons.TryGetValue(hero, out string id) ? System.Array.Find(Hero, w => w.id == id) ?? Hero[0] : Hero[0];

    public static readonly Weapon[] Wagon =
    {
        new Weapon { id = "turret", fan = true, evolvesWith = "amount" },
        new Weapon { id = "rocket", aim = AimRandom, push = 2f, color = new Color(0.25f, 0.55f, 0.2f), range = 30f, interval = 1.5f, speed = 18f, damage = 4f, splash = 4f, style = Swirl, evolvesWith = "luck" },
        new Weapon { id = "minigun", push = 0.25f, color = new Color(0.9f, 0.92f, 0.95f), range = 16f, interval = 0.1f, damage = 0.6f, growSplash = 0.5f, style = Wobble, evolvesWith = "fireRate" },
        new Weapon { id = "shotgun", aim = AimDanger, push = 3f, color = new Color(0.55f, 0.33f, 0.14f), range = 12f, interval = 0.9f, damage = 1.5f, targets = 5, extraTargets = 1, style = Pellets, evolvesWith = "greed" },
        new Weapon { id = "sniper", aim = AimStrongest, push = 2f, color = new Color(0.5f, 0.04f, 0.1f), range = 45f, interval = 2f, speed = 120f, damage = 12f, chain = 1, evolvesWith = "critDamage" },
        new Weapon { id = "pulse", push = 2.5f, color = Color.magenta, range = 9f, interval = 1.2f, damage = 2.5f, pulse = true, evolvesWith = "regen" },
        new Weapon { id = "storm", aim = AimRandom, color = new Color(0.5f, 0.3f, 0.95f), range = 22f, interval = 1.2f, damage = 2.5f, extraTargets = 1, strike = true, evolvesWith = "critChance" },
        new Weapon { id = "flame", aim = AimDanger, push = 0.25f, color = new Color(1f, 0.35f, 0f), range = 8f, interval = 0.1f, speed = 20f, damage = 0.5f, splash = 1.5f, firstBiome = 1, style = Flame, evolvesWith = "area" }, // Nieve
        new Weapon { id = "freeze", color = new Color(0.25f, 0.5f, 1f), range = 20f, interval = 0.4f, slow = 2f, extraTargets = 1, firstBiome = 4, style = Shard, evolvesWith = "armor" },                                  // Volcán
        new Weapon { id = "chain", color = Color.cyan, range = 20f, interval = 0.8f, speed = 80f, damage = 1.5f, chain = 3, firstBiome = 7, evolvesWith = "speed" },                 // Planeta alienígena
        new Weapon { id = "masamune", push = 3f, color = new Color(0.3f, 0.6f, 1f), range = 14f, interval = 2.6f, speed = 22f, damage = 6f, style = Slash, evolvesWith = "damage" }, // a swordsman on the roof (Samurai)
        new Weapon { id = "laser", push = 0.3f, color = new Color(1f, 0.1f, 0.4f), range = 30f, interval = 0.5f, damage = 0.8f, beam = true, fan = true, evolvesWith = "duration" },                              // débil, pero atraviesa la línea entera
    };

    // The hero's own guns: cousins of the wagon weapons, but shorter-ranged and faster, so they reward flying close.
    public static readonly Weapon[] Hero =
    {
        new Weapon { id = "blaster", color = new Color(0.3f, 0.6f, 1f), range = 18f, interval = 0.22f, damage = 0.8f, fan = true, evolvesWith = "critChance" }, // its fan made it far stronger than the others
        new Weapon { id = "grenade", push = 2f, color = new Color(0.4f, 0.8f, 0.2f), range = 16f, interval = 0.9f, speed = 25f, damage = 3f, splash = 2.5f, style = Lob, evolvesWith = "area" },
        new Weapon { id = "smg", push = 0.25f, color = new Color(0.9f, 0.9f, 0.6f), range = 13f, interval = 0.08f, damage = 0.5f, growSplash = 0.4f, style = Wobble, evolvesWith = "fireRate" },
        new Weapon { id = "scatter", color = new Color(1f, 0.6f, 0.3f), range = 10f, interval = 0.9f, speed = 20f, damage = 1.4f, targets = 2, extraTargets = 1, style = Boomerang, evolvesWith = "speed" },
        new Weapon { id = "rail", color = new Color(0.6f, 0.3f, 1f), range = 30f, interval = 1.4f, speed = 120f, damage = 8f, chain = 1, evolvesWith = "critDamage" },
        new Weapon { id = "nova", push = 2.5f, color = new Color(1f, 0.4f, 0.8f), range = 7f, interval = 1f, damage = 3f, pulse = true, evolvesWith = "vitality" },
        new Weapon { id = "spark", color = new Color(0.8f, 1f, 1f), range = 8f, interval = 0.9f, damage = 2f, extraTargets = 1, style = Orbit, evolvesWith = "armor" },
        new Weapon { id = "masamune", push = 3f, color = new Color(0.3f, 0.6f, 1f), range = 14f, interval = 2.2f, speed = 22f, damage = 6f, style = Slash, evolvesWith = "damage" }, // slow, but cuts through a crowd
    };
}

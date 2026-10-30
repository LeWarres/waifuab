using System.Collections.Generic;
using UnityEngine;

// What a run leaves behind, beyond the run itself (kept in SaveGame): the damage each weapon dealt in it (the
// results screen), the tickets it earns, the workshop they are spent in, and the achievements.

// This run's tally: damage dealt by each weapon (Combat.Hurt), and when it started.
public static class RunStats
{
    static readonly Dictionary<Weapon, float> dealt = new();
    public static float Started { get; private set; }
    public static int Ultimates;
    public static float UltimateDamage;
    public static int UltimateKills;

    public static void Begin()
    {
        dealt.Clear();
        Ultimates = 0;
        UltimateDamage = 0f;
        UltimateKills = 0;
        Started = Time.time;
    }

    public static void Deal(Weapon weapon, float amount, bool ultimate = false)
    {
        if (weapon == null || amount <= 0f) return;
        dealt.TryGetValue(weapon, out float sum);
        dealt[weapon] = sum + amount;
        if (ultimate) UltimateDamage += amount; // effective damage, excluding overkill
    }

    // The weapons that dealt the most, most first.
    public static List<(Weapon weapon, float dealt)> Top(int count)
    {
        var list = new List<(Weapon, float)>();
        foreach (var pair in dealt) list.Add((pair.Key, pair.Value));
        list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }

    public static float Total { get { float sum = 0f; foreach (float d in dealt.Values) sum += d; return sum; } }

    // Tickets for a run: the stations reached, the kills, the biomes crossed.
    public static int Tickets(int stations, int kills, int biome) => stations * 8 + kills / 40 + biome * 25;
}

// The workshop: upgrades bought with tickets that every endless run starts with (a story chapter starts from
// nothing, as its design says).
public static class Workshop
{
    public struct Upgrade { public string id, icon; public float per; }
    public static readonly Upgrade[] Upgrades =
    {
        new() { id = "vitality", icon = "vitality", per = 0.08f }, // the wagons' health
        new() { id = "damage", icon = "blaster", per = 0.06f },    // every shot
        new() { id = "armor", icon = "armor", per = 0.03f },       // the hero's armour
        new() { id = "speed", icon = "speed", per = 0.04f },       // the hero's speed
        new() { id = "greed", icon = "greed", per = 0.08f },       // money from kills
        new() { id = "purse", icon = "money", per = 40f },         // money to start with
    };
    public const int MaxLevel = 5;
    static readonly int[] Costs = { 50, 100, 175, 275, 400 };

    public static int Level(int i) => SaveGame.WorkshopLevel(i);
    public static int Cost(int i) => Level(i) < MaxLevel ? Costs[Level(i)] : 0;
    public static bool Buy(int i)
    {
        if (Level(i) >= MaxLevel || SaveGame.Tickets < Cost(i)) return false;
        SaveGame.AddTickets(-Cost(i));
        SaveGame.SetWorkshopLevel(i, Level(i) + 1);
        return true;
    }

    static float Bonus(string id) { int i = System.Array.FindIndex(Upgrades, u => u.id == id); return Level(i) * Upgrades[i].per; }
    public static float WagonHealth => 1f + Bonus("vitality");
    public static float DamageBonus => Bonus("damage");
    public static void Apply(Player hero, ref int money)
    {
        hero.damageBonus += Bonus("damage");
        hero.armor += Bonus("armor");
        hero.speed *= 1f + Bonus("speed");
        hero.moneyBonus += Bonus("greed");
        money += Mathf.RoundToInt(Bonus("purse"));
    }
}

// Goals across runs, each worth tickets the first time: checked now and then (Check) or told when they happen.
public static class Achievements
{
    public struct Def { public string id, icon; public int reward; }
    public static readonly Def[] All =
    {
        new() { id = "station1", icon = "wagon", reward = 25 },
        new() { id = "station10", icon = "rail", reward = 75 },
        new() { id = "volcano", icon = "flame", reward = 75 },
        new() { id = "endline", icon = "biome", reward = 150 },
        new() { id = "truck", icon = "truck", reward = 75 },
        new() { id = "kills500", icon = "blaster", reward = 50 },
        new() { id = "kills2000", icon = "minigun", reward = 100 },
        new() { id = "evolve", icon = "evolutions", reward = 75 },
        new() { id = "legendary", icon = "chest_open", reward = 100 },
        new() { id = "wagons8", icon = "wagon", reward = 50 },
        new() { id = "rich", icon = "money", reward = 50 },
        new() { id = "ultimate", icon = "nova", reward = 25 },
        new() { id = "chapter1", icon = "yes", reward = 50 },
        new() { id = "race", icon = "speed", reward = 75 },
        new() { id = "story", icon = "language", reward = 200 },
    };

    public static int Count { get { int n = 0; foreach (Def d in All) if (SaveGame.HasAchievement(d.id)) n++; return n; } }

    public static void Unlock(string id)
    {
        if (SaveGame.HasAchievement(id)) return;
        Def def = System.Array.Find(All, d => d.id == id);
        if (def.id == null) return;
        SaveGame.AddAchievement(id);
        SaveGame.AddTickets(def.reward);
        Object.FindFirstObjectByType<Hud>()?.Achievement(def);
    }

    // What can be read off the run as it goes.
    public static void Check(TrainSim t)
    {
        if (t.Stations >= 1) Unlock("station1");
        if (t.Stations >= 10) Unlock("station10");
        if (!Story.Active && t.Biome >= 4) Unlock("volcano");
        if (!Story.Active && t.Biome >= 8) Unlock("endline");
        if (t.Kills >= 500) Unlock("kills500");
        if (t.Kills >= 2000) Unlock("kills2000");
        if (t.CarCount >= 8) Unlock("wagons8");
        if (t.money >= 1000) Unlock("rich");
        foreach (var (_, level) in t.Arsenal()) if (level >= Weapon.Evolved) Unlock("evolve");
    }
}

// Comfort and accessibility settings, kept between runs (PlayerPrefs, like Display): how hard the screen shakes,
// the short freezes on big hits, full-screen flashes, the damage numbers, and the size of the interface.
public static class Comfort
{
    public static readonly float[] ShakeSteps = { 0f, 0.5f, 1f };
    public static readonly float[] ScaleSteps = { 0.9f, 1f, 1.1f, 1.25f };
    public static int ShakeIndex { get; private set; } = PlayerPrefs.GetInt("comfort.shake", 2);
    public static int ScaleIndex { get; private set; } = PlayerPrefs.GetInt("comfort.scale", 1);
    public static bool HitStop { get; private set; } = PlayerPrefs.GetInt("comfort.hitstop", 1) == 1;
    public static bool Flashes { get; private set; } = PlayerPrefs.GetInt("comfort.flashes", 1) == 1;
    public static bool Numbers { get; private set; } = PlayerPrefs.GetInt("comfort.numbers", 1) == 1;
    public static float Shake => ShakeSteps[Mathf.Clamp(ShakeIndex, 0, ShakeSteps.Length - 1)];
    public static float Scale => ScaleSteps[Mathf.Clamp(ScaleIndex, 0, ScaleSteps.Length - 1)];

    public static void StepShake(int step) { ShakeIndex = Wrap(ShakeIndex + step, ShakeSteps.Length); Save(); }
    public static void StepScale(int step) { ScaleIndex = Wrap(ScaleIndex + step, ScaleSteps.Length); Save(); UiKit.Rescale(); }
    public static void ToggleHitStop() { HitStop = !HitStop; Save(); }
    public static void ToggleFlashes() { Flashes = !Flashes; Save(); }
    public static void ToggleNumbers() { Numbers = !Numbers; Save(); }
    static int Wrap(int at, int count) => (at % count + count) % count;

    static void Save()
    {
        PlayerPrefs.SetInt("comfort.shake", ShakeIndex);
        PlayerPrefs.SetInt("comfort.scale", ScaleIndex);
        PlayerPrefs.SetInt("comfort.hitstop", HitStop ? 1 : 0);
        PlayerPrefs.SetInt("comfort.flashes", Flashes ? 1 : 0);
        PlayerPrefs.SetInt("comfort.numbers", Numbers ? 1 : 0);
        PlayerPrefs.Save();
    }

    // The rows a settings list shows for them: label key and value, what confirm does, what left / right do.
    public static IEnumerable<(string label, System.Action run, System.Action<int> adjust)> Rows()
    {
        string On(bool on) => L10n.T(on ? "menu.yes" : "menu.no");
        yield return (L10n.T("comfort.shake", Mathf.RoundToInt(Shake * 100f)), () => StepShake(1), StepShake);
        yield return (L10n.T("comfort.hitstop", On(HitStop)), ToggleHitStop, _ => ToggleHitStop());
        yield return (L10n.T("comfort.flashes", On(Flashes)), ToggleFlashes, _ => ToggleFlashes());
        yield return (L10n.T("comfort.numbers", On(Numbers)), ToggleNumbers, _ => ToggleNumbers());
        yield return (L10n.T("comfort.scale", Mathf.RoundToInt(Scale * 100f)), () => StepScale(1), StepScale);
    }
}

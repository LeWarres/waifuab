using System.Collections.Generic;
using UnityEngine;

// Story mode: a hero's journey told in chapters, one biome each. A chapter starts from nothing (a new train, no
// money), lasts Seconds of riding, then a rival driver's locomotive comes alongside for a duel; win it and one
// upgrade of that chapter, picked from three, is kept for the chapters after it. Lose and the chapter is played
// again. Between chapters the story scene tells what happened (its pages come from here). Endless mode is the
// old run through every biome and does not come through here.
public static class Story
{
    public const float Seconds = 20f * 60f;

    // rival: index of the locomotive (Locomotives.Models) the rival drives. intro / outro: pages of text before
    // and after the chapter (strings story.<hero>.<chapter>.intro.<n> / .outro.<n>); the first chapter's intro
    // is the prologue the story scene always had. before / after: who speaks each line of the talk on the track
    // as the duel starts and once it is won, h the hero and r the rival (strings ...before.<n> / ...after.<n>).
    public struct Part { public int biome, rival, intro, outro; public string before, after; }

    static readonly Part[] Bba =
    {
        new Part { biome = 0, rival = 2,  intro = 16, outro = 2, before = "hrh", after = "rh" }, // desert: Onini and the C-279
        new Part { biome = 5, rival = 7,  intro = 2,  outro = 2, before = "rhr", after = "rh" }, // city: Cerebron and the R-2D-2
        new Part { biome = 6, rival = 4,  intro = 2,  outro = 2, before = "rhr", after = "rh" }, // space: Sentinel and the Golem
        new Part { biome = 7, rival = 10, intro = 2,  outro = 2, before = "rhr", after = "rh" }, // alien planet: Faceless and T.exe
        new Part { biome = 8, rival = 3,  intro = 2,  outro = 3, before = "rhr", after = "rh" }, // the alien base: Tzmil and the Night Skull
    };

    // The heroes with a story so far; the others will get theirs.
    public static Part[] Route(string hero) => hero == "bba" ? Bba : null;
    public static string Hero => Locomotives.Hero;
    public static bool Active => Locomotives.StoryMode && Route(Hero) != null;
    public static int Chapter { get; set; }          // the one being played
    public static Part Current => Route(Hero)[Chapter];
    public static bool JustWon { get; private set; } // the story scene opens with that chapter's ending
    public static bool Over => JustWon && Chapter == Route(Hero).Length - 1; // the last one: the end of it all

    // Chapters that can be played: every one up to the first not beaten yet.
    public static int Reached => PlayerPrefs.GetInt("story." + Hero + ".reached", 0);

    // What a chapter carries over: the upgrade kept from each one before it, as Tokens.
    public static List<string> Kept()
    {
        var kept = new List<string>();
        for (int i = 0; i < Chapter; i++)
        {
            string token = PlayerPrefs.GetString("story." + Hero + ".keep." + i, "");
            if (token != "") kept.Add(token);
            // and the upgrade handed down at random with its level, "<id>*<level>": that many of it
            string[] passive = PlayerPrefs.GetString("story." + Hero + ".passive." + i, "").Split('*');
            if (passive.Length == 2 && int.TryParse(passive[1], out int times))
                for (int n = 0; n < times; n++) kept.Add(passive[0]);
        }
        return kept;
    }

    // The chapter is beaten; keep: the upgrade the player chose to take along (null: none); passive: one of the
    // hero's upgrades with its level, "<id>*<level>", handed down at random (null: none).
    public static void Win(string keep, string passive)
    {
        PlayerPrefs.SetString("story." + Hero + ".keep." + Chapter, keep ?? "");
        PlayerPrefs.SetString("story." + Hero + ".passive." + Chapter, passive ?? "");
        PlayerPrefs.SetInt("story." + Hero + ".reached", Mathf.Min(Mathf.Max(Reached, Chapter + 1), Route(Hero).Length - 1));
        PlayerPrefs.Save();
        JustWon = true;
    }

    // The truck's wagon won in a chapter rides on into the next one only (at the level it had), never further.
    public static void KeepTruck(int level) => PlayerPrefs.SetInt($"story.{Hero}.truck.{Chapter}", level);
    public static int CarriedTruck => Chapter > 0 ? PlayerPrefs.GetInt($"story.{Hero}.truck.{Chapter - 1}", 0) : 0;

    // Starting a chapter from the menu: its opening pages are told first.
    public static void Play(int chapter)
    {
        Chapter = chapter;
        JustWon = false;
    }

    // The pages the story scene shows now: the ending of the chapter just won and the opening of the next, or
    // only the opening of the one about to be played. Moves on to that next chapter.
    public static StoryIntro.Page[] Pages()
    {
        var pages = new List<StoryIntro.Page>();
        Part[] route = Route(Hero);
        if (JustWon)
        {
            for (int n = 1; n <= route[Chapter].outro; n++)
                pages.Add(new StoryIntro.Page { image = 3, textKey = $"story.{Hero}.{Chapter}.outro.{n}", titleKey = $"story.{Hero}.{Chapter}.title", number = Chapter + 1 });
            if (Chapter == route.Length - 1) return pages.ToArray(); // the end: no next chapter
            Chapter++;
            JustWon = false;
        }
        if (Chapter == 0)
            for (int n = 1; n <= route[0].intro; n++)
                pages.Add(new StoryIntro.Page { image = PrologueImages[n - 1], textKey = $"story.page.{n:00}" });
        else
            for (int n = 1; n <= route[Chapter].intro; n++)
                pages.Add(new StoryIntro.Page { image = 5, textKey = $"story.{Hero}.{Chapter}.intro.{n}", titleKey = $"story.{Hero}.{Chapter}.title", number = Chapter + 1 });
        return pages.ToArray();
    }

    // The talk on the track: as the rival comes alongside (before) or once it is beaten (after).
    public static void Duel(bool before, out string[] names, out string[] lines, out bool[] rival)
    {
        Part part = Current;
        string who = before ? part.before : part.after, side = before ? "before" : "after";
        names = new string[who.Length];
        lines = new string[who.Length];
        rival = new bool[who.Length];
        for (int i = 0; i < who.Length; i++)
        {
            rival[i] = who[i] == 'r';
            names[i] = L10n.T(Locomotives.HeroNameKey(rival[i] ? part.rival : Locomotives.Selected));
            lines[i] = L10n.T($"story.{Hero}.{Chapter}.{side}.{i + 1}");
        }
    }

    static readonly int[] PrologueImages = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5 }; // as the story scene had them

    // Upgrades as tokens, so a chapter can remember one and the next can hand it back: "ammo:<id>" (the hero's
    // weapon), "wagon:<id>" (a wagon weapon), "evolve" (a level on the hero's weapon) or a hero stat
    // ("speed", "armor", "vitality", "damage", "greed", "fireRate", "critChance", "critDamage").
    public static string Label(string token, out string icon)
    {
        string[] part = token.Split(':');
        Weapon weapon = part.Length > 1 ? System.Array.Find(part[0] == "ammo" ? Weapon.Hero : Weapon.Wagon, w => w.id == part[1]) : null;
        int passive = System.Array.IndexOf(TrainSim.Passives, part[0]);
        icon = weapon != null ? weapon.id : passive >= 0 ? TrainSim.PassiveIcons[passive] : part[0] == "evolve" ? "blaster" : "arms";
        if (weapon != null) return L10n.T(part[0] == "ammo" ? "keep.ammo" : "keep.wagon", weapon.Name);
        return L10n.T("keep." + part[0]);
    }
}

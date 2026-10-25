using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Story mode: a hero's journey told in chapters, one biome each. A chapter starts from nothing (a new train, no
// money), lasts Seconds of riding, then a rival driver's locomotive comes alongside for a duel; win it and one
// upgrade of that chapter, picked from three, is kept for the chapters after it. Lose and the chapter is played
// again. Between chapters the story scene tells what happened (its pages come from here). Endless mode is the
// old run through every biome and does not come through here.
public static class Story
{
    static int testAdvanceFrame = -1;
    // Testing aid: K skips the story's current stage. Consumed once across scene changes, so one press cannot skip two.
    public static bool TakeTestAdvance()
    {
        if (!Active || Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame || testAdvanceFrame == Time.frameCount) return false;
        testAdvanceFrame = Time.frameCount;
        Sound.Play("confirm");
        return true;
    }
    public const float Seconds = 20f * 60f;

    // rival: index of the locomotive (Locomotives.Models) the rival drives. intro / outro: pages of text before
    // and after the chapter (strings story.<hero>.<chapter>.intro.<n> / .outro.<n>); the first chapter's intro
    // is the prologue the story scene always had. before / after: who speaks each line of the talk on the track
    // as the duel starts and once it is won, h the hero and r the rival (strings ...before.<n> / ...after.<n>).
    public struct Part { public int biome, rival, intro, outro; public string before, after; public bool inspection; public QuestionnaireMode questionnaire; public bool race, eyeBoss, cave, gates, trench; } // trench: the finale is the run down the base's trench (RaceEvent, RaceTrench.cs) // gates: before the biome, the gates of the base to shoot open (GatesEvent) // cave: the duel's driver turn is the flashlight hunt (CaveEvent) // the finale instead of a duel: race, a race to the launcher (RaceEvent); eyeBoss, the ball of eyes on a ring of wagons (EyeBossEvent)

    static readonly Part[] Bba =
    {
        new Part { biome = 0, rival = 2,  intro = 16, outro = 2, before = "hrh", after = "rh" }, // desert: Onini and the C-279
        new Part { biome = 5, rival = 7,  intro = 2,  outro = 1, before = "rhr", after = "rh", inspection = true, questionnaire = QuestionnaireMode.Human, race = true }, // city: Cerebron and the R-2D-2
        new Part { biome = 6, rival = 4,  intro = 2,  outro = 2, before = "rhr", after = "rh", eyeBoss = true }, // space: Sentinel and the Golem
        new Part { biome = 7, rival = 10, intro = 2,  outro = 2, before = "rhr", after = "rh", questionnaire = QuestionnaireMode.Alien, cave = true }, // alien planet: Faceless and T.exe (its pages lead straight into the biome)
        new Part { biome = 8, rival = 3,  intro = 2,  outro = 5, before = "rhr", after = "rh", gates = true, trench = true }, // the alien base: Tzmil and the Night Skull
    };

    // Faceless currently has his prologue and first playable chapter; later chapters are still to be written.
    static readonly Part[] Faceless =
    {
        new Part { biome = 0, rival = 2, intro = 12, outro = 1, before = "hr", after = "h" },
    };

    public static Part[] Route(string hero) => hero switch { "bba" => Bba, "faceless" => Faceless, _ => null };
    public static string Hero => Locomotives.Hero;
    public static bool Active => Locomotives.StoryMode && Route(Hero) != null;
    public static int Chapter { get; set; }          // the one being played
    public static Part Current => Route(Hero)[Chapter];
    public static bool JustWon { get; private set; } // the story scene opens with that chapter's ending
    public static bool InspectionPending { get; private set; }
    public static bool AscentPending { get; private set; }
    public static bool GatesPassed { get; set; } // this try at the chapter is past its gates (a restart keeps it)
    public static void RetryAscent() => replayArrival = true;
    public static void PassAscent() { AscentPending = false; ascentPassed = true; }
    public static QuestionnaireMode InspectionMode => Current.questionnaire;
    static bool replayArrival, ascentPassed;
    public static void RetryInspection() => replayArrival = true;
    public static void PassInspection() => InspectionPending = false;
    public static bool Over => JustWon && Chapter == Route(Hero).Length - 1; // the last one: the end of it all

    // Chapters that can be played: every one up to the first not beaten yet.
    public static int Reached => SaveGame.HasStory ? Mathf.Min(SaveGame.Reached, (Route(Locomotives.HeroId(SaveGame.Locomotive))?.Length ?? 1) - 1) : 0;

    // What a chapter carries over: the upgrade kept from each one before it, as Tokens.
    public static List<string> Kept()
    {
        var kept = new List<string>();
        for (int i = 0; i < Chapter; i++)
        {
            string token = SaveGame.Keep(i);
            if (token != "") kept.Add(token);
            // and the upgrade handed down at random with its level, "<id>*<level>": that many of it
            string[] passive = SaveGame.Passive(i).Split('*');
            if (passive.Length == 2 && int.TryParse(passive[1], out int times))
                for (int n = 0; n < times; n++) kept.Add(passive[0]);
        }
        if (TestChapter > 0) // ponytail: testing a later chapter, what the ones before would hand down: a random upgrade at level 2 and a random weapon
        {
            string up = TrainSim.Passives[Random.Range(0, TrainSim.Passives.Length)];
            kept.Add(up);
            kept.Add(up);
            kept.Add("wagon:" + Weapon.Wagon[Random.Range(1, Weapon.Wagon.Length)].id);
            Debug.Log("Story test kept: " + string.Join(", ", kept));
        }
        return kept;
    }

    // The chapter is beaten; keep: the upgrade the player chose to take along (null: none); passive: one of the
    // hero's upgrades with its level, "<id>*<level>", handed down at random (null: none).
    // truck: the level of the truck's wagon won in it, which rides on into the next chapter only (0: none).
    public static void Win(string keep, string passive, int truck)
    {
        SaveGame.Won(Chapter, keep, passive, truck, Route(Hero).Length - 1);
        JustWon = true;
        if (Chapter == 0) Achievements.Unlock("chapter1");
        if (Current.race) Achievements.Unlock("race");
        if (Hero == "bba" && Chapter == Bba.Length - 1) Achievements.Unlock("story"); // Faceless's full ending is not written yet.
    }

    // The chapter is lost: it is the one to play again, from its start.
    public static void Lose() => SaveGame.Lost(Chapter);

    // The story saved can be continued: there is one, and its chapters are not all done.
    public static bool CanContinue => SaveGame.HasStory && SaveGame.Chapter < (Route(Locomotives.HeroId(SaveGame.Locomotive))?.Length ?? 0);

    // The truck's wagon won in a chapter rides on into the next one only (at the level it had), never further.
    // (Kept in the save with the chapter's other results: see Win.)
    public static int CarriedTruck => Chapter > 0 ? SaveGame.Truck(Chapter - 1) : 0;

    // Starting a chapter from the menu: its opening pages are told first.
    public static int TestChapter = -1; // -1 follows the chapter selected in the menu.
    public static void Play(int chapter)
    {
        Chapter = Mathf.Clamp(TestChapter >= 0 ? TestChapter : chapter, 0, (Route(Hero)?.Length ?? 1) - 1);
        JustWon = false;
        GatesPassed = false;
        InspectionPending = replayArrival = false;
        ascentPassed = false;
        AscentPending = false;
    }

    // The pages the story scene shows now: the ending of the chapter just won and the opening of the next, or
    // only the opening of the one about to be played. Moves on to that next chapter.
    public static StoryIntro.Page[] Pages()
    {
        var pages = new List<StoryIntro.Page>();
        Part[] route = Route(Hero);
        if (JustWon || replayArrival)
        {
            int ending = replayArrival ? Mathf.Max(0, Chapter - 1) : Chapter;
            for (int n = 1; n <= route[ending].outro; n++)
                pages.Add(new StoryIntro.Page { image = 3, textKey = $"story.{Hero}.{ending}.outro.{n}", titleKey = $"story.{Hero}.{ending}.title", number = ending + 1,
                    artwork = Hero == "bba" ? ending switch
                    {
                        0 => "Story/bba_desert_victory",
                        1 => "Story/bba_city_victory",
                        2 => n == 1 ? "Story/bba_space_victory" : "Story/bba_space_landing",
                        3 => n == 1 ? "Story/bba_alien_victory" : "Story/bba_base_journey",
                        4 => "Story/" + (n switch
                        {
                            1 => "bba_ending_return",
                            2 => "bba_ending_truth",
                            3 => "bba_ending_silicon",
                            4 => "bba_ending_missing_ship",
                            _ => "bba_ending_reflection"
                        }),
                        _ => null
                    } : Hero == "faceless" ? "Story/faceless_idea" : null });
            replayArrival = false;
            if (JustWon)
            {
                if (Chapter == route.Length - 1) return pages.ToArray(); // the end: no next chapter
                Chapter++;
                JustWon = false;
            }
        }
        // The city ending leads into the flight, before any narration already set in space.
        if (Hero == "bba" && Chapter == 2 && TestChapter != 2 && !ascentPassed) // (testing space: straight in)
        {
            InspectionPending = false;
            AscentPending = true;
            if (pages.Count == 0)
                for (int n = 1; n <= route[1].outro; n++)
                    pages.Add(new StoryIntro.Page { image = 3, textKey = $"story.bba.1.outro.{n}", titleKey = "story.bba.1.title", number = 2,
                        artwork = "Story/bba_city_victory" });
            return pages.ToArray();
        }
        AscentPending = false;
        if (Chapter == 0 && Hero == "bba")
            for (int n = 1; n <= route[0].intro; n++)
                pages.Add(new StoryIntro.Page { image = PrologueImages[n - 1], textKey = $"story.page.{n:00}" });
        else
            for (int n = 1; n <= route[Chapter].intro; n++)
                pages.Add(new StoryIntro.Page { image = 5, textKey = $"story.{Hero}.{Chapter}.intro.{n}", titleKey = $"story.{Hero}.{Chapter}.title", number = Chapter + 1,
                    artwork = Hero == "bba" ? Chapter switch
                    {
                        1 => "Story/bba_city_journey",
                        2 => "Story/bba_space_arrival",
                        3 => n == 1 ? "Story/bba_alien_arrival" : "Story/bba_space_landing",
                        4 => "Story/bba_fortress_arrival",
                        _ => null
                    } : Hero == "faceless" ? "Story/faceless_" + FacelessImages[n - 1] : null });
        InspectionPending = route[Chapter].inspection;
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
    static readonly string[] FacelessImages = { "champion", "orders", "legacy", "legacy", "artist", "artist", "friend", "friend", "artist", "idea", "idea", "idea" };

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

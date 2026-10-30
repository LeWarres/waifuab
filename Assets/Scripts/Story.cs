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
    public static bool GabrielRun => Active && Hero == "gabriel";
    public static float Duration => GabrielRun ? 30f * 60f : Seconds;

    // rival: index of the locomotive (Locomotives.Models) the rival drives. intro / outro: pages of text before
    // and after the chapter (strings story.<hero>.<chapter>.intro.<n> / .outro.<n>); the first chapter's intro
    // is the prologue the story scene always had. before / after: who speaks each line of the talk on the track
    // as the duel starts and once it is won, h the hero and r the rival (strings ...before.<n> / ...after.<n>).
    public struct Part { public int biome, rival, intro, outro; public string before, after; public bool inspection; public QuestionnaireMode questionnaire; public bool crates, race, eyeBoss, cave, gates, trench, drawings, holo, circuits, morality; } // trench: the finale is the run down the base's trench (RaceEvent, RaceTrench.cs) // gates: before the biome, the gates of the base to shoot open (GatesEvent) // cave: the duel's driver turn is the flashlight hunt (CaveEvent) // the finale instead of a duel: race, a race to the launcher (RaceEvent); eyeBoss, the ball of eyes on a ring of wagons (EyeBossEvent)

    static readonly Part[] Bba =
    {
        new Part { biome = 0, rival = 2,  intro = 16, outro = 2, before = "hrh", after = "rh" }, // desert: Onini and the C-279
        new Part { biome = 5, rival = 7,  intro = 2,  outro = 1, before = "rhr", after = "rh", inspection = true, questionnaire = QuestionnaireMode.Human, race = true }, // city: Cerebron and the R-2D-2
        new Part { biome = 6, rival = 4,  intro = 2,  outro = 2, before = "rhr", after = "rh", eyeBoss = true }, // space: Sentinel and the Golem
        new Part { biome = 7, rival = 10, intro = 2,  outro = 2, before = "rhr", after = "rh", questionnaire = QuestionnaireMode.Alien, cave = true }, // alien planet: Faceless and T.exe (its pages lead straight into the biome)
        new Part { biome = 8, rival = 3,  intro = 2,  outro = 5, before = "rhr", after = "rh", gates = true, trench = true }, // the alien base: Tzmil and the Night Skull
    };

    static readonly Part[] Faceless =
    {
        new Part { biome = 7, rival = 0, intro = 12, outro = 3, before = "rhh", after = "hhr", cave = true },
        new Part { biome = 8, rival = 7, intro = 5, outro = 3, before = "rhhr", after = "hr", inspection = true, questionnaire = QuestionnaireMode.Alien, holo = true }, // the finale: a Holo-Duel against Cerebron (HoloDuel)
        new Part { biome = 6, rival = 4, intro = 3, outro = 2, before = "rh", after = "h", eyeBoss = true },
        new Part { biome = 2, rival = 8, intro = 3, outro = 3, before = "rh", after = "hr" },
        new Part { biome = 2, rival = 8, intro = 3, outro = 6, before = "h", after = "h", drawings = true },
    };

    static readonly Part[] R20 =
    {
        new Part { biome = 2, rival = 6, intro = 6, outro = 2, before = "rhr", after = "hr" },
        new Part { biome = 5, rival = 7, intro = 2, outro = 3, before = "rh", after = "h", inspection = true, questionnaire = QuestionnaireMode.Human, race = true },
        new Part { biome = 3, rival = 0, intro = 2, outro = 2, before = "rhh", after = "rh" },
        new Part { biome = 4, rival = 3, intro = 2, outro = 3, before = "hrhr", after = "rh", holo = true },
        new Part { biome = 9, rival = 8, intro = 2, outro = 7, before = "rhrh", after = "rh" },
    };

    static readonly Part[] Onini =
    {
        new Part { biome=10, rival=0, intro=6, outro=2, before="rhr", after="hr" },
        new Part { biome=0, rival=0, intro=2, outro=3, before="hrh", after="hrh" },
        new Part { biome=9, rival=4, intro=2, outro=4, before="rhr", after="hr" },
        new Part { biome=11, rival=4, intro=2, outro=5, before="rhr", after="hr", eyeBoss=true },
        new Part { biome=12, rival=2, intro=2, outro=4, before="rhrh", after="h" },
    };

    static readonly Part[] Culu =
    {
        new Part { biome=3, rival=2, intro=6, outro=2, before="rhr", after="hr" },
        new Part { biome=2, rival=8, intro=2, outro=2, before="rhr", after="rh" },
        new Part { biome=0, rival=0, intro=2, outro=4, before="rhr", after="hr" },
        new Part { biome=12, rival=9, intro=2, outro=2, before="hrh", after="hr" },
        new Part { biome=13, rival=11, intro=2, outro=5, before="hrh", after="rh" },
    };

    static readonly Part[] Cerebron =
    {
        new Part { biome=6, rival=4, intro=7, outro=2, before="rhr", after="hh", eyeBoss=true },
        new Part { biome=7, rival=0, intro=2, outro=3, before="rhr", after="hr" },
        new Part { biome=8, rival=10, intro=2, outro=4, before="hrrh", after="rh", holo=true },
        new Part { biome=13, rival=11, intro=2, outro=5, before="hrh", after="rh" },
    };

    static readonly Part[] Tzmil =
    {
        new Part { biome=4, rival=8, intro=6, outro=3, before="rhrh", after="rh", holo=true },
        new Part { biome=9, rival=4, intro=2, outro=4, before="rhr", after="hr" },
        new Part { biome=10, rival=2, intro=2, outro=4, before="rhr", after="rh" },
        new Part { biome=11, rival=4, intro=2, outro=7, before="rhr", after="rh", eyeBoss=true },
    };

    static readonly Part[] Golem =
    {
        new Part { biome=9, rival=3, intro=6, outro=5, before="rhr", after="hr" },
        new Part { biome=6, rival=10, intro=2, outro=4, before="rhr", after="rh" },
        new Part { biome=13, rival=11, intro=2, outro=4, before="hh", after="h", circuits=true },
        new Part { biome=14, rival=4, intro=3, outro=6, before="hh", after="h", morality=true },
    };

    static readonly Part[] Giger =
    {
        new Part { biome=12, rival=6, intro=4, outro=3, before="hrh", after="rh" },
        new Part { biome=13, rival=11, intro=2, outro=2, before="hrh", after="rh" },
        new Part { biome=10, rival=2, intro=2, outro=2, before="hh", after="h", crates=true },
        new Part { biome=11, rival=5, intro=2, outro=5, before="hrh", after="rh" },
    };
    static readonly Part[] Gabriel =
    {
        new Part { biome=11, rival=2, intro=6, outro=2, before="rhr", after="hr" },
        new Part { biome=11, rival=3, intro=1, outro=3, before="rhr", after="hr" },
        new Part { biome=11, rival=9, intro=1, outro=3, before="hrh", after="h" },
        new Part { biome=11, rival=1, intro=1, outro=6, before="rhr", after="hr" },
    };
    static readonly Part[] Nath =
    {
        new Part { biome=4, rival=3, intro=5, outro=2, before="rhr", after="hr" },
        new Part { biome=5, rival=7, intro=1, outro=3, before="rh", after="h", inspection=true, questionnaire=QuestionnaireMode.Human, race=true },
        new Part { biome=11, rival=5, intro=1, outro=2, before="rhr", after="hr" },
        new Part { biome=10, rival=2, intro=1, outro=6, before="rhr", after="hr" },
        new Part { biome=2, rival=10, intro=2, outro=5, before="rhr", after="hr" },
    };
    public static Part[] Route(string hero) => hero switch { "nath" => Nath, "gabriel" => Gabriel, "giger" => Giger, "bba" => Bba, "faceless" => Faceless, "xiuhtecuhtli" => R20, "onini" => Onini, "culu" => Culu, "cerebron" => Cerebron, "tzmil" => Tzmil, "sentinel" => Golem, _ => null };
    public static string Hero => Locomotives.CampaignHeroId(Locomotives.Selected);
    public static bool Active => Locomotives.StoryMode && Route(Hero) != null;
    public static bool ShadowRival => Active && Hero=="onini" && Chapter==4;
    public static bool MachineRival => Active && (Current.rival==9 || Current.rival==11 || ((Hero=="giger" || Hero=="nath") && Current.rival==5));
    public static int Chapter { get; set; }          // the one being played
    public static Part Current => Route(Hero)[Chapter];
    public static bool JustWon { get; private set; } // the story scene opens with that chapter's ending
    public static bool InspectionPending { get; private set; }
    public static bool AscentPending { get; private set; }
    public static bool GatesPassed { get; set; } // this try at the chapter is past its gates (a restart keeps it)
    public static void RetryAscent() => replayArrival = true;
    public static void PassAscent() { AscentPending = false; ascentPassed = true; }
    public static QuestionnaireMode InspectionMode => Current.questionnaire;
    static bool replayArrival, ascentPassed, inspectionPassed;
    public static bool Descending => Active && Hero == "faceless" && Chapter == 3;
    public static void RetryInspection() => replayArrival = true;
    public static void PassInspection() { InspectionPending = false; inspectionPassed = true; }
    public static bool Over => JustWon && Chapter == Route(Hero).Length - 1; // the last one: the end of it all

    // Chapters that can be played: every one up to the first not beaten yet.
    public static int Reached => SaveGame.HasStory ? Mathf.Min(SaveGame.Reached, (Route(Locomotives.CampaignHeroId(SaveGame.Locomotive))?.Length ?? 1) - 1) : 0;

    // What a chapter carries over: the upgrade kept from each one before it, as Tokens.
    public static List<string> Kept()
    {
        var kept = new List<string>();
        for (int i = 0; i < Chapter; i++)
        {
            string token = SaveGame.Keep(i);
            if (token != "") kept.Add(token);
            // and the upgrade handed down at random, "<id>*<level>": at half its level (rounded up), a head start and
            // not a finished upgrade, so later chapters stay harder than the ones before
            string[] passive = SaveGame.Passive(i).Split('*');
            if (passive.Length == 2 && int.TryParse(passive[1], out int times))
                for (int n = 0; n < (times + 1) / 2; n++) kept.Add(passive[0]);
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
        if (Chapter == Route(Hero).Length - 1) Achievements.Unlock("story");
    }

    // The chapter is lost: it is the one to play again, from its start.
    public static void Lose() => SaveGame.Lost(Chapter);

    // The story saved can be continued: there is one, and its chapters are not all done.
    public static bool CanContinue => SaveGame.HasStory && SaveGame.Chapter < (Route(Locomotives.CampaignHeroId(SaveGame.Locomotive))?.Length ?? 0);

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
        InspectionPending = replayArrival = inspectionPassed = false;
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
                pages.Add(new StoryIntro.Page { image = 3, textKey = Hero=="sentinel" && ending==3 ? $"story.sentinel.3.{(SaveGame.GolemHighMorality ? "good" : "bad")}.{n}" : $"story.{Hero}.{ending}.outro.{n}", titleKey = $"story.{Hero}.{ending}.title", number = ending + 1,
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
                    } : Hero == "faceless" ? FacelessArt(ending, false, n) : Hero == "xiuhtecuhtli" ? R20Art(ending, false, n) : Hero == "onini" ? OniniArt(ending, false, n) : Hero == "culu" ? CuluArt(ending, false, n) : Hero == "cerebron" ? CerebronArt(ending, false, n) : Hero == "tzmil" ? TzmilArt(ending, false, n) : Hero == "sentinel" ? GolemArt(ending, false, n) : Hero == "giger" ? GigerArt(ending, false, n) : Hero == "gabriel" ? GabrielArt(ending,false,n) : Hero == "nath" ? NathArt(ending,false,n) : null });
            replayArrival = false;
            if (JustWon)
            {
                if (Chapter == route.Length - 1) return pages.ToArray(); // the end: no next chapter
                Chapter++;
                ascentPassed = inspectionPassed = false;
                JustWon = false;
            }
        }
        // Golem uses the existing ascent before his first space chapter, including retry narration.
        if (Hero=="sentinel" && Chapter==1 && !ascentPassed)
        {
            InspectionPending=false;AscentPending=true;
            if(pages.Count==0)for(int n=1;n<=route[0].outro;n++)
                pages.Add(new StoryIntro.Page {textKey=$"story.sentinel.0.outro.{n}",titleKey="story.sentinel.0.title",number=1,artwork=GolemArt(0,false,n)});
            return pages.ToArray();
        }
        // Faceless enters Earth through the same flight challenge, in reverse, before the jungle narration.
        if (Hero == "faceless" && Chapter == 3 && !ascentPassed)
        {
            InspectionPending = false;
            AscentPending = true;
            if (pages.Count == 0)
                for (int n = 1; n <= route[2].outro; n++)
                    pages.Add(new StoryIntro.Page { textKey = $"story.faceless.2.outro.{n}", titleKey = "story.faceless.2.title", number = 3, artwork = FacelessArt(2, false, n) });
            return pages.ToArray();
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
            for (int n = Hero == "faceless" && Chapter == 1 && inspectionPassed ? 3 : 1;
                n <= (Hero == "faceless" && Chapter == 1 && !inspectionPassed ? 2 : route[Chapter].intro); n++)
                pages.Add(new StoryIntro.Page { image = 5, textKey = $"story.{Hero}.{Chapter}.intro.{n}", titleKey = $"story.{Hero}.{Chapter}.title", number = Chapter + 1,
                    artwork = Hero == "bba" ? Chapter switch
                    {
                        1 => "Story/bba_city_journey",
                        2 => "Story/bba_space_arrival",
                        3 => n == 1 ? "Story/bba_alien_arrival" : "Story/bba_space_landing",
                        4 => "Story/bba_fortress_arrival",
                        _ => null
                    } : Hero == "faceless" ? FacelessArt(Chapter, true, n) : Hero == "xiuhtecuhtli" ? R20Art(Chapter, true, n) : Hero == "onini" ? OniniArt(Chapter, true, n) : Hero == "culu" ? CuluArt(Chapter, true, n) : Hero == "cerebron" ? CerebronArt(Chapter, true, n) : Hero == "tzmil" ? TzmilArt(Chapter, true, n) : Hero == "sentinel" ? GolemArt(Chapter, true, n) : Hero == "giger" ? GigerArt(Chapter, true, n) : Hero == "gabriel" ? GabrielArt(Chapter,true,n) : Hero == "nath" ? NathArt(Chapter,true,n) : null });
        InspectionPending = route[Chapter].inspection && !inspectionPassed;
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
            names[i] = rival[i] && Hero == "xiuhtecuhtli" && Chapter == 4 ? L10n.T("story.xiuhtecuhtli.clone") : L10n.T(Locomotives.HeroNameKey(rival[i] ? part.rival : Locomotives.Selected));
            lines[i] = L10n.T($"story.{Hero}.{Chapter}.{side}.{i + 1}");
            if(rival[i] && ShadowRival) names[i]=L10n.T("story.onini.shadow");
            else if(rival[i] && (Hero=="onini" || Hero=="tzmil") && Current.eyeBoss) names[i]=L10n.T("eyeboss.heaven.name");
            else if(rival[i] && MachineRival) names[i]=L10n.T(Locomotives.NameKeys[part.rival]);
            else if(rival[i] && Hero=="cerebron" && Current.eyeBoss) names[i]=L10n.T("story.cerebron.roger");
        }
    }

    static readonly int[] PrologueImages = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5 }; // as the story scene had them
    static readonly string[] FacelessImages = { "champion", "orders", "legacy", "legacy", "artist", "artist", "friend", "friend", "artist", "idea", "idea", "idea" };

    public static string FacelessArt(int chapter, bool opening, int n)
    {
        string name = opening ? chapter switch
        {
            0 => FacelessImages[n - 1],
            1 => n <= 2 ? "bba_bluff" : n <= 4 ? "resignation" : "fortress",
            2 => "space_escape",
            3 => "earth_arrival",
            _ => n == 1 ? "tie_choice" : "stolen_drawings"
        } : chapter switch
        {
            0 => "bba_bluff",
            1 => "cerebron_bluff",
            2 => "eye_escape",
            3 => n == 1 ? "r20_peace" : "tie_choice",
            _ => n <= 3 ? "cabin_artist" : "texe_job"
        };
        return "Story/faceless_" + name;
    }

    public static string R20Art(int chapter, bool opening, int n)
    {
        string name = opening ? chapter switch
        {
            0 => n <= 3 ? "premium" : n <= 5 ? "recruitment" : "wrong_turn",
            1 => "wrong_turn",
            2 => "ocean",
            3 => n == 1 ? "volcano" : "sister",
            _ => "return_rock"
        } : chapter switch
        {
            0 => n == 1 ? "culu" : "wrong_turn",
            1 => "launcher",
            2 => "volcano",
            3 => n == 1 ? "sister" : "return_rock",
            _ => n == 1 ? "clone" : n == 2 ? "clones" : n == 3 ? "silicon" : n == 4 ? "shaft" : "feast"
        };
        return "Story/r20_" + name;
    }

    public static string OniniArt(int chapter,bool opening,int n)
    {
        string name=opening ? chapter switch
        {
            0 => n<=2 ? "rebel" : n<=4 ? "champion" : "deal",
            1 => "hell_escape",
            2 => n==1 ? "repair" : "security",
            3 => n==1 ? "heaven_entry" : "guardian",
            _ => n==1 ? "psycho" : "shadow"
        } : chapter switch
        {
            0 => "hell_escape",
            1 => n==1 ? "desert_chase" : "repair",
            2 => n==1 ? "security" : "bargain",
            3 => n<=2 ? "trophy" : n<=4 ? "cactus" : "psycho",
            _ => n==1 ? "shadow" : "home_feast"
        };
        return "Story/onini_"+name;
    }

    public static string CuluArt(int chapter, bool opening, int n)
    {
        string name = opening ? chapter switch
        {
            0 => n switch { 1=>"origin", 2=>"conquest", 3=>"rejection", 4=>"sea_home", _=>"deal" },
            1 => "jungle_r20",
            2 => "bba",
            3 => "psycho",
            _ => n==1 ? "father_arrival" : "crochet"
        } : chapter switch
        {
            0 => n==1 ? "onini_ocean" : "jungle_r20",
            1 => "desert_lost",
            2 => n==1 ? "bba" : n==2 ? "desert_lost" : "cactus",
            3 => "father_arrival",
            _ => n<=2 ? "father_talk" : "baratito"
        };
        return "Story/culu_"+name;
    }

    public static string CerebronArt(int chapter,bool opening,int n)
    {
        string name=opening ? chapter switch
        {
            0 => n<=2 ? "origin" : n<=4 ? "doris" : n==5 ? "carrot" : "deal",
            1 => "bba",
            2 => n==1 ? "salvage" : "holo",
            _ => n==1 ? "portal" : "crochet"
        } : chapter switch
        {
            0 => "roger",
            1 => n==1 ? "bba" : "salvage",
            2 => n==1 ? "holo" : n==2 ? "faceless_escape" : "portal",
            _ => n==1 ? "crochet" : n<=3 ? "promise" : "baratito"
        };
        return "Story/cerebron_"+name;
    }

    public static string TzmilArt(int chapter, bool opening, int n)
    {
        string name = opening ? chapter switch
        {
            0 => n<=3 ? "family" : n<=5 ? "guardian" : "volcano",
            1 => "government",
            2 => "hell_search",
            _ => n==1 ? "heaven_journey" : "angel"
        } : chapter switch
        {
            0 => n==1 ? "volcano" : "government",
            1 => n==1 ? "golem" : n<=3 ? "explosion" : "hell_search",
            2 => n==1 ? "hell_search" : n<=3 ? "onini_meal" : "heaven_journey",
            _ => n<=3 ? "father" : n<=5 ? "mission" : "carito"
        };
        return "Story/tzmil_" + name;
    }

    public static string NathArt(int chapter,bool opening,int n)
    {
        string name=opening ? chapter switch
        {
            0 => n==1 ? "origin" : n<=3 ? "sports" : "deal",
            1 => "inspection",
            2 => "heaven",
            3 => "hell",
            _ => "forest"
        } : chapter switch
        {
            0 => "volcano",
            1 => n==1 ? "launcher" : "heaven",
            2 => "heaven",
            3 => n==1 ? "hell" : n==2 ? "escape" : n==3 ? "base" : n==4 ? "bba" : n==5 ? "cafe" : "forest",
            _ => "ending"
        };
        return "Story/nath_"+name;
    }

    public static string GabrielArt(int chapter,bool opening,int n)
    {
        string id=opening ? chapter switch {0=>n<=2 ? "wingless" : n<=5 ? "origin" : "onini",1=>"tzmil",2=>"giger",_=>"reymar"}
            : chapter switch {0=>"credit",1=>n==1 ? "tzmil" : "crush",2=>n==1 ? "giger" : "dayoff",_=>n==1 ? "reymar" : n<=4 ? "reunion" : "date"};
        return "Story/gabriel_"+id;
    }

    public static string GigerArt(int chapter,bool opening,int n)
    {
        string id=opening ? chapter switch {0=>n<=3 ? "origin" : "culu",1=>"thulu",2=>"hell",_=>"heaven"}
            : chapter switch {0=>n==1 ? "culu" : "rock",1=>n==1 ? "crochet" : "hell",2=>"trick",_=>n==1 ? "heaven" : n<=3 ? "trick" : "home"};
        return "Story/giger_"+id;
    }

    public static string GolemArt(int chapter, bool opening, int n)
    {
        string name=opening ? chapter switch
        {
            0=>n<=3 ? "origin" : "orders",
            1=>"faceless",
            2=>n==1 ? "portal" : "circuits",
            _=>n<=2 ? "heaven" : "judgment"
        } : chapter switch
        {
            0=>n==1 ? "tzmil" : n==2 ? "vent" : n==3 ? "clones" : "launch",
            1=>n==1 ? "faceless" : "portal",
            2=>n<=2 ? "circuits" : "sacrifice",
            _=>SaveGame.GolemHighMorality ? (n<=3 ? "peace" : "rebirth") : (n<=3 ? "prison" : "baratito")
        };
        return "Story/golem_"+name;
    }

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

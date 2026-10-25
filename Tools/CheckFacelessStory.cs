using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Localization;
using UnityEngine;

public static class CheckFacelessStory
{
    const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static string Run()
    {
        int selected = Locomotives.Selected, chapter = Story.Chapter, test = Story.TestChapter;
        bool mode = Locomotives.StoryMode;
        var state = new Dictionary<FieldInfo, object>();
        foreach (var f in typeof(Story).GetFields(Flags)) if (!f.IsInitOnly && !f.IsLiteral) state[f] = f.GetValue(null);
        var keys = new HashSet<string>(); var pictures = new HashSet<string>();
        try
        {
            PlayerPrefs.SetInt("locomotive.selected", 10); Locomotives.StoryMode = true; Story.TestChapter = -1;
            var route = Story.Route("faceless");
            Check(route.Length == 5 && Story.Route("bba").Length == 5, "Both routes need five chapters");
            int[] biomes = { 7, 8, 6, 2, 2 }, rivals = { 0, 7, 4, 8, 8 };
            for (int c = 0; c < route.Length; c++)
            {
                var part = route[c];
                Check(part.biome == biomes[c] && part.rival == rivals[c], "Wrong biome/rival " + c);
                for (int n = 1; n <= part.intro; n++) { keys.Add($"story.faceless.{c}.intro.{n}"); pictures.Add(Story.FacelessArt(c, true, n)); }
                for (int n = 1; n <= part.outro; n++) { keys.Add($"story.faceless.{c}.outro.{n}"); pictures.Add(Story.FacelessArt(c, false, n)); }
                foreach (var side in new[] { "before", "after" })
                    for (int n = 1; n <= (side == "before" ? part.before : part.after).Length; n++) keys.Add($"story.faceless.{c}.{side}.{n}");
                keys.Add($"story.faceless.{c}.title");
            }
            Check(route[0].cave && route[1].cave && route[2].eyeBoss && route[4].drawings, "Wrong finales");
            Story.Play(0); Check(Story.Pages().Length == 12 && !Story.InspectionPending, "Wrong prologue");
            typeof(Story).GetProperty("JustWon").SetValue(null, true);
            var pages = Story.Pages();
            Check(Story.Chapter == 1 && Story.InspectionPending && Story.InspectionMode == QuestionnaireMode.Alien && pages.Length == 5, "BBA victory must lead to alien quiz");
            Story.RetryInspection(); pages = Story.Pages(); Check(pages.Length == 5 && Story.InspectionPending, "Arrest retry");
            Story.PassInspection(); pages = Story.Pages();
            Check(pages.Length == 3 && pages[0].textKey.EndsWith("intro.3") && !Story.InspectionPending, "Rejected resignation after pass");
            Story.Play(2); typeof(Story).GetProperty("JustWon").SetValue(null, true); pages = Story.Pages();
            Check(Story.Chapter == 3 && Story.AscentPending && Story.Descending && pages.Length == 2, "Eye boss must lead to descent");
            Story.RetryAscent(); pages = Story.Pages(); Check(pages.Length == 2 && Story.AscentPending, "Retry descent");
            Story.PassAscent(); pages = Story.Pages(); Check(pages.Length == 3 && !Story.AscentPending, "Earth narration after descent");
            Story.Play(4); typeof(Story).GetProperty("JustWon").SetValue(null, true); pages = Story.Pages();
            Check(Story.Over && pages.Length == 6 && !Story.AscentPending && !Story.InspectionPending, "Epilogue must end at Thanks");
            PlayerPrefs.SetInt("locomotive.selected", 0); Story.Play(2); Story.Pages(); Check(Story.AscentPending && !Story.Descending, "BBA ascent regression");
            int translated = 0;
            foreach (var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)
                foreach (var key in keys) { Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue), "Missing " + table.LocaleIdentifier.Code + " " + key); translated++; }
            foreach (var picture in pictures) Check(Resources.Load<Texture2D>(picture), "Missing " + picture);
            return $"PASS: five chapters, biomes/rivals, alien quiz pass/arrest, descent pass/retry, six-page ending, BBA ascent unchanged; {pictures.Count} illustrations, {translated} translations.";
        }
        finally
        {
            PlayerPrefs.SetInt("locomotive.selected", selected); Locomotives.StoryMode = mode; Story.Chapter = chapter; Story.TestChapter = test;
            foreach (var item in state) item.Key.SetValue(null, item.Value);
        }
    }
}

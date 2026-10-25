using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Localization;
using UnityEngine;

public static class CheckFacelessStory
{
    public static string Run()
    {
        if (!Application.isPlaying || !Story.Active || Story.Hero != "faceless") throw new Exception("Run in Faceless story mode");
        var pages = Story.Pages();
        if (pages.Length != 12 || Story.Route("bba").Length != 5) throw new Exception("Unexpected route or prologue length");
        var illustrations = new HashSet<string>();
        foreach (var page in pages)
        {
            if (!Resources.Load<Texture2D>(page.artwork)) throw new Exception("Missing artwork: " + page.artwork);
            illustrations.Add(page.artwork);
        }
        if (illustrations.Count != 6) throw new Exception("Expected six illustrations");
        int translated = 0;
        foreach (var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)
        {
            foreach (var page in pages) CheckText(table, page.textKey, ref translated);
            foreach (string key in new[] { "title", "before.1", "before.2", "after.1", "outro.1" }) CheckText(table, "story.faceless.0." + key, ref translated);
        }

        // Exercise save routing with an in-memory copy; no save operation or file write is called.
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var field = typeof(SaveGame).GetField("data", flags);
        var original = typeof(SaveGame).GetProperty("D", flags).GetValue(null);
        var fixture = JsonUtility.FromJson(JsonUtility.ToJson(original), original.GetType());
        try
        {
            field.SetValue(null, fixture);
            fixture.GetType().GetField("story").SetValue(fixture, true);
            fixture.GetType().GetField("locomotive").SetValue(fixture, 10);
            fixture.GetType().GetField("chapter").SetValue(fixture, 0);
            if (!Story.CanContinue) throw new Exception("Faceless chapter cannot be resumed");
            fixture.GetType().GetField("chapter").SetValue(fixture, 1);
            if (Story.CanContinue) throw new Exception("Faceless resumes beyond the written route");
            fixture.GetType().GetField("locomotive").SetValue(fixture, 0);
            fixture.GetType().GetField("chapter").SetValue(fixture, 4);
            if (!Story.CanContinue) throw new Exception("BBA final chapter cannot be resumed");
        }
        finally { field.SetValue(null, original); }
        return "PASS: 12 Faceless pages, 6 images, " + translated + " localized entries; Faceless and BBA save routing valid without writing saves.";
    }

    static void CheckText(UnityEngine.Localization.Tables.StringTable table, string key, ref int total)
    {
        if (string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue)) throw new Exception("Missing " + table.LocaleIdentifier.Code + " " + key);
        total++;
    }
}

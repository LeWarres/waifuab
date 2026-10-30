using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckGabrielStory
{
    public static string Run()
    {
        int selected=Locomotives.Selected;bool had=PlayerPrefs.HasKey("locomotive.selected"),mode=Locomotives.StoryMode;
        var snapshot=new Dictionary<FieldInfo,object>();foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!f.IsLiteral&&!f.IsInitOnly)snapshot[f]=f.GetValue(null);
        try
        {
            Locomotives.Select(5);Locomotives.StoryMode=true;Story.TestChapter=-1;
            if(Story.Hero!="gabriel"||Story.Duration!=1800f)throw new Exception("Wrong hero/duration");
            var route=Story.Route("gabriel");int[] rivals={2,3,9,1};var art=new HashSet<string>();var keys=new List<string>{"hero.character.gabriel","gabriel.upgrade.weapon","gabriel.upgrade.passive","gabriel.upgrade.hint","gabriel.upgrade.repair","gabriel.upgrade.take"};
            for(int c=0;c<4;c++)
            {
                Story.Play(c);var p=route[c];if(p.biome!=11||p.rival!=rivals[c])throw new Exception("Wrong stage "+c);
                keys.Add($"story.gabriel.{c}.title");
                foreach(var side in new[]{"intro","outro","before","after"})
                {
                    int count=side=="intro"?p.intro:side=="outro"?p.outro:side=="before"?p.before.Length:p.after.Length;
                    for(int n=1;n<=count;n++){keys.Add($"story.gabriel.{c}.{side}.{n}");if(side=="intro"||side=="outro")art.Add(Story.GabrielArt(c,side=="intro",n));}
                }
            }
            foreach(string p in art)if(!Resources.Load<Texture2D>(p))throw new Exception("Missing "+p);
            int translations=0;foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)foreach(string key in keys){if(string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue))throw new Exception("Missing "+key);translations++;}
            if(!Resources.Load<Texture2D>("Menu/hero_gabriel"))throw new Exception("No portrait");
            Locomotives.Select(0);if(Story.Duration!=Story.Seconds)throw new Exception("Other stories changed");
            return $"PASS 4 encounters in heaven, 1800 seconds, {art.Count} illustrations, portrait and {translations} translations; other campaigns still 1200 seconds.";
        }
        finally
        {
            if(had)PlayerPrefs.SetInt("locomotive.selected",selected);else PlayerPrefs.DeleteKey("locomotive.selected");PlayerPrefs.Save();Locomotives.StoryMode=mode;foreach(var pair in snapshot)pair.Key.SetValue(null,pair.Value);
        }
    }
}

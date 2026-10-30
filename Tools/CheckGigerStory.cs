using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckGigerStory
{
    static void Check(bool yes,string message){if(!yes)throw new Exception(message);}
    public static string Run()
    {
        int selected=Locomotives.Selected;bool had=PlayerPrefs.HasKey("locomotive.selected"),mode=Locomotives.StoryMode;
        var snapshot=new Dictionary<FieldInfo,object>();foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!f.IsLiteral&&!f.IsInitOnly)snapshot[f]=f.GetValue(null);
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",9);Locomotives.StoryMode=true;Story.TestChapter=-1;
            Check(Story.Hero=="giger"&&Story.Active,"Selector route missing");var route=Story.Route("giger");Check(route.Length==4,"Wrong length");
            int[] biomes={12,13,10,11},rivals={6,11,2,5};var art=new HashSet<string>();var keys=new HashSet<string>{"hero.character.giger","giger.crates.controls","giger.crates.count","giger.crates.target","giger.crates.won","giger.crates.lost"};
            for(int c=0;c<4;c++)
            {
                var p=route[c];Check(p.biome==biomes[c]&&p.rival==rivals[c],"Wrong route "+c);Check(p.crates==(c==2),"Wrong crate stage");
                Story.Play(c);Check(Story.MachineRival==(c==1||c==3),"Wrong rival driver");var pages=Story.Pages();Check(pages.Length==p.intro&&!Story.AscentPending&&!Story.InspectionPending,"Wrong pages "+c);
                keys.Add($"story.giger.{c}.title");
                foreach(var side in new[]{"intro","outro","before","after"})
                {
                    int count=side=="intro"?p.intro:side=="outro"?p.outro:side=="before"?p.before.Length:p.after.Length;
                    for(int n=1;n<=count;n++){keys.Add($"story.giger.{c}.{side}.{n}");if(side=="intro"||side=="outro")art.Add(Story.GigerArt(c,side=="intro",n));}
                }
            }
            Check(art.Count==9,"Expected nine images");foreach(string p in art)Check(Resources.Load<Texture2D>(p),"Missing "+p);
            int translations=0;var tables=LocalizationEditorSettings.GetStringTableCollection("Game");
            foreach(var table in tables.StringTables)foreach(string key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+key);translations++;}
            Check(!RaceEvent.CratesPassed(34)&&RaceEvent.CratesPassed(35)&&RaceEvent.CratesPassed(40),"Wrong 35/40 boundary");
            return $"PASS four chapters, nine illustrations, {translations} translations, 34/35 boundary and machine rivals.";
        }
        finally
        {
            if(had)PlayerPrefs.SetInt("locomotive.selected",selected);else PlayerPrefs.DeleteKey("locomotive.selected");Locomotives.StoryMode=mode;foreach(var pair in snapshot)pair.Key.SetValue(null,pair.Value);
        }
    }
}

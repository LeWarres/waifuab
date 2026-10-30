using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckTzmilStory
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        bool had=PlayerPrefs.HasKey("locomotive.selected"),mode=Locomotives.StoryMode;int selected=Locomotives.Selected;
        var snapshot=new Dictionary<FieldInfo,object>();foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!f.IsLiteral&&!f.IsInitOnly)snapshot[f]=f.GetValue(null);
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",3);Locomotives.StoryMode=true;Story.TestChapter=-1;
            Check(Locomotives.Hero=="tzmil"&&Story.Active,"Selector does not route Tzmil");
            var route=Story.Route("tzmil");Check(route.Length==4,"Wrong chapter count");
            int[] biomes={4,9,10,11},rivals={8,4,2,4};var keys=new HashSet<string>();var art=new HashSet<string>();
            for(int c=0;c<4;c++)
            {
                var p=route[c];Check(p.biome==biomes[c]&&p.rival==rivals[c],"Wrong biome/rival "+c);
                Check(p.holo==(c==0)&&p.eyeBoss==(c==3),"Wrong finale "+c);
                Check(!p.inspection&&!p.race&&!p.cave&&!p.drawings&&!p.gates&&!p.trench,"Unexpected minigame");
                keys.Add($"story.tzmil.{c}.title");
                for(int n=1;n<=p.intro;n++){keys.Add($"story.tzmil.{c}.intro.{n}");art.Add(Story.TzmilArt(c,true,n));}
                for(int n=1;n<=p.outro;n++){keys.Add($"story.tzmil.{c}.outro.{n}");art.Add(Story.TzmilArt(c,false,n));}
                foreach(var side in new[]{"before","after"})for(int n=1;n<=(side=="before"?p.before:p.after).Length;n++)keys.Add($"story.tzmil.{c}.{side}.{n}");
                Story.Play(c);Check(Story.Pages().Length==p.intro&&!Story.InspectionPending&&!Story.AscentPending,"Wrong opening");
                Story.Duel(true,out var names,out var lines,out var speakers);
                if(c==3)for(int n=0;n<names.Length;n++)if(speakers[n])Check(names[n]==L10n.T("eyeboss.heaven.name"),"Angel mislabeled");
                typeof(Story).GetProperty("JustWon").SetValue(null,true);var pages=Story.Pages();
                Check(pages.Length==p.outro+(c<3?route[c+1].intro:0),"Wrong transition "+c);
                Check(c<3?Story.Chapter==c+1:Story.Over,"Missing ending");
            }
            Check(keys.Count==55&&art.Count==13,"Wrong content totals: "+keys.Count+" / "+art.Count);
            foreach(var path in art){var texture=Resources.Load<Texture2D>(path);Check(texture&&texture.width>texture.height,"Missing/portrait art "+path);}
            int translations=0;foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)foreach(var key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+table.LocaleIdentifier.Code+" "+key);translations++;}
            Check(translations==220,"Expected four locales");
            return "PASS: Tzmil selector, 4 stages, holo R-20/Golem/Onini/heaven angel, 13 images, 220 translations, transitions and Thanks ending.";
        }
        finally{if(had)PlayerPrefs.SetInt("locomotive.selected",selected);else PlayerPrefs.DeleteKey("locomotive.selected");Locomotives.StoryMode=mode;foreach(var pair in snapshot)pair.Key.SetValue(null,pair.Value);}
    }
}

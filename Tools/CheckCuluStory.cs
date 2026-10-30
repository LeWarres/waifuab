using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckCuluStory
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        int selected=Locomotives.Selected;bool mode=Locomotives.StoryMode;
        var snapshot=new Dictionary<FieldInfo,object>();
        foreach(var field in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!field.IsLiteral&&!field.IsInitOnly)snapshot[field]=field.GetValue(null);
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",6);Locomotives.StoryMode=true;Story.TestChapter=-1;
            var route=Story.Route("culu");Check(route!=null&&route.Length==5,"Culu route missing");
            int[] biomes={3,2,0,12,13},rivals={2,8,0,9,11};var keys=new HashSet<string>();var pictures=new HashSet<string>();
            for(int c=0;c<5;c++)
            {
                var part=route[c];Check(part.biome==biomes[c]&&part.rival==rivals[c],"Wrong route "+c);
                keys.Add($"story.culu.{c}.title");
                for(int n=1;n<=part.intro;n++){keys.Add($"story.culu.{c}.intro.{n}");pictures.Add(Story.CuluArt(c,true,n));}
                for(int n=1;n<=part.outro;n++){keys.Add($"story.culu.{c}.outro.{n}");pictures.Add(Story.CuluArt(c,false,n));}
                foreach(string side in new[]{"before","after"})for(int n=1;n<=(side=="before"?part.before:part.after).Length;n++)keys.Add($"story.culu.{c}.{side}.{n}");
                Story.Play(c);Check(Story.Pages().Length==part.intro&&!Story.InspectionPending&&!Story.AscentPending,"Unexpected stage "+c);
                if(c>=3){Story.Duel(true,out var names,out var lines,out var speakers);for(int n=0;n<names.Length;n++)if(speakers[n])Check(names[n]==L10n.T(Locomotives.NameKeys[rivals[c]]),"Wrong machine name");}
                typeof(Story).GetProperty("JustWon").SetValue(null,true);var pages=Story.Pages();
                Check(pages.Length==part.outro+(c<4?route[c+1].intro:0),"Wrong transition "+c);
                Check(c<4?Story.Chapter==c+1:Story.Over,"Ending missing "+c);
            }
            Check(pictures.Count==15,"Expected 15 pictures");foreach(string path in pictures)Check(Resources.Load<Texture2D>(path),"Missing art "+path);
            keys.Add("biome.thulu");keys.Add("boss.name.thulu");int translations=0;
            foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)foreach(string key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+table.LocaleIdentifier.Code+" "+key);translations++;}
            Check(Resources.Load<GameObject>("Models/Station_thulu"),"Missing Thulu station");
            Check(Resources.LoadAll<GameObject>("Scenery/thulu").Length>=4,"Missing Thulu scenery");
            Check(Resources.Load<Texture2D>("Ground/thulu")&&Resources.Load<Texture2D>("Sky/thulu"),"Missing Thulu textures");
            var flags=BindingFlags.Static|BindingFlags.NonPublic;int count=((Array)typeof(TrainSim).GetField("Biomes",flags).GetValue(null)).Length;
            Check(count>=14&&((Array)typeof(TrainSim).GetField("Tracks",flags).GetValue(null)).Length==count,"Biome/track mismatch");
            return $"PASS: Culu five chapters/rivals, 15 illustrations, {translations} translations, no unintended minigames, Thulu assets and ending.";
        }
        finally{PlayerPrefs.SetInt("locomotive.selected",selected);Locomotives.StoryMode=mode;foreach(var entry in snapshot)entry.Key.SetValue(null,entry.Value);}
    }
}

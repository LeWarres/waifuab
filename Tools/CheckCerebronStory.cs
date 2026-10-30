using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckCerebronStory
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        int selected=Locomotives.Selected;bool mode=Locomotives.StoryMode;
        var snapshot=new Dictionary<FieldInfo,object>();
        foreach(var field in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!field.IsLiteral&&!field.IsInitOnly)snapshot[field]=field.GetValue(null);
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",7);Locomotives.StoryMode=true;Story.TestChapter=-1;
            var route=Story.Route("cerebron");Check(route!=null&&route.Length==4,"Missing Cerebron campaign");
            int[] biomes={6,7,8,13},rivals={4,0,10,11};var keys=new HashSet<string>{"story.cerebron.roger","story.cerebron.roger.intro"};var pictures=new HashSet<string>();
            for(int c=0;c<4;c++)
            {
                var part=route[c];Check(part.biome==biomes[c]&&part.rival==rivals[c],"Wrong route "+c);
                Check(part.eyeBoss==(c==0)&&part.holo==(c==2),"Wrong finale "+c);
                Check(!part.race&&!part.inspection&&!part.cave&&!part.drawings&&!part.gates&&!part.trench,"Unexpected minigame");
                keys.Add($"story.cerebron.{c}.title");
                for(int n=1;n<=part.intro;n++){keys.Add($"story.cerebron.{c}.intro.{n}");pictures.Add(Story.CerebronArt(c,true,n));}
                for(int n=1;n<=part.outro;n++){keys.Add($"story.cerebron.{c}.outro.{n}");pictures.Add(Story.CerebronArt(c,false,n));}
                foreach(string side in new[]{"before","after"})for(int n=1;n<=(side=="before"?part.before:part.after).Length;n++)keys.Add($"story.cerebron.{c}.{side}.{n}");
                Story.Play(c);Check(Story.Pages().Length==part.intro&&!Story.InspectionPending&&!Story.AscentPending,"Bad opening "+c);
                Story.Duel(true,out var names,out var lines,out var speakers);
                if(c==0)for(int n=0;n<names.Length;n++)if(speakers[n])Check(names[n]==L10n.T("story.cerebron.roger"),"Roger mislabeled");
                if(c==3){Check(Story.MachineRival,"Crochtrain must have no borrowed BBA driver");for(int n=0;n<names.Length;n++)if(speakers[n])Check(names[n]==L10n.T("loco.crochet"),"Crochtrain mislabeled");}
                typeof(Story).GetProperty("JustWon").SetValue(null,true);var pages=Story.Pages();
                Check(pages.Length==part.outro+(c<3?route[c+1].intro:0),"Bad transition "+c);
                Check(c<3?Story.Chapter==c+1:Story.Over,"Missing ending");
            }
            Check(keys.Count==54&&pictures.Count==13,"Wrong content count");foreach(string path in pictures)Check(Resources.Load<Texture2D>(path),"Missing art "+path);
            int translations=0;
            foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)
            {
                foreach(string key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+table.LocaleIdentifier.Code+" "+key);translations++;}
                foreach(string side in new[]{"before","after"})for(int n=1;n<=(side=="before"?4:2);n++)Check(table.GetEntry($"story.cerebron.2.{side}.{n}").LocalizedValue==table.GetEntry($"story.faceless.1.{side}.{n}").LocalizedValue,"Faceless dialogue diverged");
            }
            Check(route[2].before=="hrrh"&&route[2].after=="rh","Mirrored speakers incorrect");
            return $"PASS: four stages, Roger/BBA/Faceless holo/Crochtrain, 13 illustrations, {translations} translations, mirrored Faceless lines and Thanks ending.";
        }
        finally{PlayerPrefs.SetInt("locomotive.selected",selected);Locomotives.StoryMode=mode;foreach(var entry in snapshot)entry.Key.SetValue(null,entry.Value);}
    }
}

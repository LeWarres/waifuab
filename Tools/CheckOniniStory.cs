using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Localization;
using UnityEngine;
public static class CheckOniniStory
{
    static void Check(bool yes,string why){if(!yes)throw new Exception(why);}
    public static string Run()
    {
        int selected=Locomotives.Selected;bool mode=Locomotives.StoryMode;
        var state=new Dictionary<FieldInfo,object>();
        foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!f.IsLiteral&&!f.IsInitOnly)state[f]=f.GetValue(null);
        var keys=new HashSet<string>();var pictures=new HashSet<string>();
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",2);Locomotives.StoryMode=true;Story.TestChapter=-1;
            var route=Story.Route("onini");int[] biomes={10,0,9,11,12},rivals={0,0,4,4,2};
            Check(route.Length==5,"Five chapters");
            for(int c=0;c<5;c++)
            {
                var p=route[c];Check(p.biome==biomes[c]&&p.rival==rivals[c],"Route "+c);
                keys.Add($"story.onini.{c}.title");
                for(int n=1;n<=p.intro;n++){keys.Add($"story.onini.{c}.intro.{n}");pictures.Add(Story.OniniArt(c,true,n));}
                for(int n=1;n<=p.outro;n++){keys.Add($"story.onini.{c}.outro.{n}");pictures.Add(Story.OniniArt(c,false,n));}
                foreach(var side in new[]{"before","after"})for(int n=1;n<=(side=="before"?p.before:p.after).Length;n++)keys.Add($"story.onini.{c}.{side}.{n}");
                Story.Play(c);Check(Story.Pages().Length==p.intro&&!Story.InspectionPending&&!Story.AscentPending,"Unexpected minigame "+c);
                typeof(Story).GetProperty("JustWon").SetValue(null,true);var pages=Story.Pages();
                Check(pages.Length==p.outro+(c<4?route[c+1].intro:0),"Chapter transition "+c);
                Check(c<4?Story.Chapter==c+1:Story.Over,"Progression "+c);
            }
            Check(route[3].eyeBoss&&!route[3].race,"Heaven finale");
            Story.Play(4);Check(Story.ShadowRival,"Shadow missing");Story.Play(0);Check(!Story.ShadowRival,"Shadow leaked");
            foreach(var id in new[]{"hell","heaven","psychoworlds"})
            {
                keys.Add("biome."+id);keys.Add("boss.name."+id);
                Check(Resources.Load<GameObject>("Models/Station_"+id),"Missing station "+id);
                Check(Resources.Load<Texture2D>("Ground/"+id)&&Resources.Load<Texture2D>("Sky/"+id),"Missing environment "+id);
                Check(Resources.LoadAll<GameObject>("Scenery/"+id).Length>=4,"Missing props "+id);
            }
            foreach(var k in new[]{"story.onini.shadow","eyeboss.heaven.name","eyeboss.heaven.intro"})keys.Add(k);
            int translated=0;foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)foreach(string key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+table.LocaleIdentifier.Code+" "+key);translated++;}
            foreach(string art in pictures)Check(Resources.Load<Texture2D>(art),"Missing "+art);
            Check(pictures.Count==15,"Fifteen pictures");
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            var biomeCount=((Array)typeof(TrainSim).GetField("Biomes",flags).GetValue(null)).Length;
            Check(biomeCount>=13,"Onini biome entries missing");
            Check(((Array)typeof(TrainSim).GetField("Tracks",flags).GetValue(null)).Length==biomeCount,"Track palette mismatch");
            Check(Story.Route("bba")[0].biome==0&&Story.Route("faceless")[0].biome==7&&Story.Route("xiuhtecuhtli")[0].biome==2,"Other stories changed");
            return $"PASS: Onini five chapters, correct rivals, 15 pictures, {translated} translations, heaven boss, shadow scope, no unintended quiz/ascent, ending and 13 biome/track entries.";
        }
        finally{PlayerPrefs.SetInt("locomotive.selected",selected);Locomotives.StoryMode=mode;foreach(var p in state)p.Key.SetValue(null,p.Value);}
    }
}

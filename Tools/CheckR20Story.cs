using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Localization;
using UnityEngine;
public static class CheckR20Story
{
    static void Check(bool yes,string why) { if(!yes) throw new Exception(why); }
    public static string Run()
    {
        int selected=Locomotives.Selected; bool mode=Locomotives.StoryMode;
        var snapshot=new Dictionary<FieldInfo,object>();
        foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public))
            if(!f.IsInitOnly&&!f.IsLiteral) snapshot[f]=f.GetValue(null);
        var keys=new HashSet<string>();var art=new HashSet<string>();
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",8);Locomotives.StoryMode=true;Story.TestChapter=-1;
            var route=Story.Route("xiuhtecuhtli"); int[] biomes={2,5,3,4,9},rivals={6,7,0,3,8};
            Check(route.Length==5,"Five chapters");
            for(int c=0;c<5;c++)
            {
                var p=route[c];Check(p.biome==biomes[c]&&p.rival==rivals[c],"Biome/rival "+c);
                keys.Add($"story.xiuhtecuhtli.{c}.title");
                for(int n=1;n<=p.intro;n++){keys.Add($"story.xiuhtecuhtli.{c}.intro.{n}");art.Add(Story.R20Art(c,true,n));}
                for(int n=1;n<=p.outro;n++){keys.Add($"story.xiuhtecuhtli.{c}.outro.{n}");art.Add(Story.R20Art(c,false,n));}
                foreach(string side in new[]{"before","after"})for(int n=1;n<=(side=="before"?p.before:p.after).Length;n++)keys.Add($"story.xiuhtecuhtli.{c}.{side}.{n}");
            }
            Check(route[1].race&&route[1].inspection&&route[1].questionnaire==QuestionnaireMode.Human&&route[3].holo,"Finales");
            Story.Play(0);Check(Story.Pages().Length==6,"Prologue");
            typeof(Story).GetProperty("JustWon").SetValue(null,true);
            Check(Story.Pages().Length==4&&Story.Chapter==1&&Story.InspectionPending,"City checkpoint");
            Story.RetryInspection();Check(Story.Pages().Length==4,"Arrest retry");
            Story.PassInspection();Check(!Story.InspectionPending,"Quiz pass");
            typeof(Story).GetProperty("JustWon").SetValue(null,true);
            Check(Story.Pages().Length==5&&Story.Chapter==2&&!Story.AscentPending,"City win must go to ocean, no ascent");
            Story.Play(4);typeof(Story).GetProperty("JustWon").SetValue(null,true);
            Check(Story.Pages().Length==7&&Story.Over,"Seven-page epilogue");
            keys.Add("story.xiuhtecuhtli.clone");keys.Add("story.xiuhtecuhtli.race.denied");keys.Add("biome.government");keys.Add("boss.name.government");
            int translated=0;
            foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)
            {
                foreach(var key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+table.LocaleIdentifier.Code+" "+key);translated++;}
                Check(table.GetEntry("story.xiuhtecuhtli.clone").LocalizedValue!=table.GetEntry("hero.character.xiuhtecuhtli").LocalizedValue,"Clone name "+table.LocaleIdentifier.Code);
            }
            foreach(string path in art)Check(Resources.Load<Texture2D>(path),"Missing "+path);
            Check(Resources.Load<GameObject>("Models/Station_government"),"Government station");
            Check(Resources.LoadAll<GameObject>("Scenery/government").Length>=4,"Government scenery");
            Check(Resources.Load<Texture2D>("Ground/government")&&Resources.Load<Texture2D>("Sky/government"),"Government background");
            Check(((Array)typeof(TrainSim).GetField("Biomes",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Length>=10,"Existing biomes preserved");
            Check(Story.Route("bba").Length==5&&Story.Route("faceless")[1].holo,"Existing routes preserved");
            return $"PASS: R20 five chapters, human quiz retry/pass, denied launch to ocean, Tzmil holo duel, clone and ending; {art.Count} pictures, {translated} translated strings.";
        }
        finally {PlayerPrefs.SetInt("locomotive.selected",selected);Locomotives.StoryMode=mode;foreach(var p in snapshot)p.Key.SetValue(null,p.Value);}
    }
}

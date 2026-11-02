using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckCrochtrainStory
{
    static void Check(bool yes,string why){if(!yes)throw new Exception(why);}
    public static string Run()
    {
        int selected=Locomotives.Selected;bool had=PlayerPrefs.HasKey("locomotive.selected"),mode=Locomotives.StoryMode;
        var snapshot=new Dictionary<FieldInfo,object>();foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!f.IsLiteral&&!f.IsInitOnly)snapshot[f]=f.GetValue(null);
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",11);Locomotives.StoryMode=true;Story.TestChapter=-1;
            Check(Story.Active&&Story.Hero=="crochtrain"&&Player.Orbs,"Wrong Crochtrain campaign/player mapping");
            var route=Story.Route("crochtrain");Check(route.Length==4,"Wrong campaign length");
            int[] biomes={13,10,11,3},rivals={9,2,5,7},intro={7,2,2,2},outro={2,2,2,6};
            var art=new HashSet<string>();var keys=new HashSet<string>{"hero.character.crochtrain","story.crochtrain.thoughts"};
            for(int c=0;c<4;c++)
            {
                var p=route[c];Check(p.biome==biomes[c]&&p.rival==rivals[c]&&p.intro==intro[c]&&p.outro==outro[c],"Wrong route: "+c);
                Story.Play(c);Check(Story.MachineRival==(c==0||c==2),"Wrong rival driver: "+c);
                var pages=Story.Pages();Check(pages.Length==intro[c]&&!Story.AscentPending&&!Story.InspectionPending,"Wrong opening: "+c);
                keys.Add($"story.crochtrain.{c}.title");
                foreach(string side in new[]{"intro","outro","before","after"})
                {
                    int count=side=="intro"?p.intro:side=="outro"?p.outro:side=="before"?p.before.Length:p.after.Length;
                    for(int n=1;n<=count;n++){keys.Add($"story.crochtrain.{c}.{side}.{n}");if(side=="intro"||side=="outro")art.Add(Story.CrochtrainArt(c,side=="intro",n));}
                }
                foreach(bool before in new[]{true,false})
                {
                    Story.Duel(before,out var names,out var lines,out var sides);
                    for(int n=0;n<lines.Length;n++)if(!sides[n])Check(names[n]==L10n.T("story.crochtrain.thoughts"),"Dorian must think, not speak");
                }
            }
            Check(art.Count==9,"Wrong art count");foreach(string path in art){var image=Resources.Load<Texture2D>(path);Check(image&&image.width>image.height,"Missing landscape: "+path);}
            Check(keys.Count==51,"Wrong key count");int values=0;var tables=LocalizationEditorSettings.GetStringTableCollection("Game");
            foreach(var table in tables.StringTables)foreach(string key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+key);values++;}
            Check(values==204,"Wrong translation count");Check(UiKit.Speaker(L10n.T("story.crochtrain.thoughts"),false)==UiKit.Hero("crochtrain").main,"Wrong thought color");
            Check(Story.CrochtrainArt(3,false,6)=="Story/crochtrain_taken","Ending must finish with Culu's promise");
            return "PASS four chapters, nine illustrations, 204 values, thought labels/colors and orb player; existing routes preserved.";
        }
        finally
        {
            if(had)PlayerPrefs.SetInt("locomotive.selected",selected);else PlayerPrefs.DeleteKey("locomotive.selected");Locomotives.StoryMode=mode;foreach(var p in snapshot)p.Key.SetValue(null,p.Value);
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Localization;
public static class CheckGolemStory
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        int selected=Locomotives.Selected;bool had=PlayerPrefs.HasKey("locomotive.selected"),mode=Locomotives.StoryMode;
        string file=Path.Combine(Application.persistentDataPath,"save.json");bool existed=File.Exists(file);string save=existed?File.ReadAllText(file):null;
        var snapshot=new Dictionary<FieldInfo,object>();foreach(var f in typeof(Story).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))if(!f.IsLiteral&&!f.IsInitOnly)snapshot[f]=f.GetValue(null);
        try
        {
            PlayerPrefs.SetInt("locomotive.selected",4);Locomotives.StoryMode=true;Story.TestChapter=-1;
            Check(Locomotives.Hero=="sentinel"&&Story.Active,"Selector route missing");var route=Story.Route("sentinel");Check(route.Length==4,"Wrong chapter count");
            int[] biomes={9,6,13,14},rivals={3,10,11,4};var art=new HashSet<string>();
            for(int c=0;c<4;c++)
            {
                var p=route[c];Check(p.biome==biomes[c]&&p.rival==rivals[c],"Wrong route "+c);Check(p.circuits==(c==2)&&p.morality==(c==3),"Wrong final minigame");
                Check(!p.holo&&!p.race&&!p.eyeBoss&&!p.cave&&!p.inspection&&!p.trench,"Unexpected challenge");
                for(int n=1;n<=p.intro;n++)art.Add(Story.GolemArt(c,true,n));
                foreach(int score in new[]{6,7}){SaveGame.GolemJudged(score);for(int n=1;n<=p.outro;n++)art.Add(Story.GolemArt(c,false,n));}
                Story.Play(c);var pages=Story.Pages();
                Check(!Story.InspectionPending,"Unwanted officer quiz");
                if(c==1)
                {
                    Check(Story.AscentPending&&pages.Length==route[0].outro,"Ascent entry missing");Story.RetryAscent();Check(Story.Pages().Length==5,"Ascent retry missing");Story.PassAscent();pages=Story.Pages();
                }
                Check(pages.Length==p.intro&&!Story.AscentPending,"Wrong opening "+c);
                if(c==0){typeof(Story).GetProperty("JustWon").SetValue(null,true);Check(Story.Pages().Length==5&&Story.Chapter==1&&Story.AscentPending,"Government ending did not enter flight");}
            }
            foreach(string path in art)Check(Resources.Load<Texture2D>(path),"Missing art "+path);Check(art.Count==16,"Wrong picture count");
            SaveGame.GolemJudged(6);Check(!SaveGame.GolemHighMorality,"6/13 must be low");SaveGame.GolemJudged(7);Check(SaveGame.GolemHighMorality,"7/13 must be high");
            foreach(int score in new[]{0,6,7,13})
            {
                SaveGame.GolemJudged(score);typeof(SaveGame).GetField("data",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);Check(SaveGame.GolemHighMorality==(score>=7),"Moral result not saved");
                Story.Play(3);typeof(Story).GetProperty("JustWon").SetValue(null,true);var pages=Story.Pages();Check(pages.Length==6&&Story.Over,"Ending missing");
                foreach(var page in pages)Check(page.textKey.StartsWith("story.sentinel.3."+(score>=7?"good.":"bad.")),"Wrong ending branch");
            }
            var solved=GolemChallenge.SolvedBoard();Check(GolemChallenge.Connected(solved),"Puzzle solution disconnected");var broken=(int[])solved.Clone();broken[10]&=~8;Check(!GolemChallenge.Connected(broken),"Puzzle accepts no power input");broken=(int[])solved.Clone();broken[14]&=~2;Check(!GolemChallenge.Connected(broken),"Puzzle accepts no core output");
            for(int m=0;m<16;m++)Check(GolemChallenge.Rotate(GolemChallenge.Rotate(GolemChallenge.Rotate(GolemChallenge.Rotate(m))))==m,"Rotation invalid");
            int translations=0;var keys=new HashSet<string>();foreach(var line in File.ReadAllLines("Assets/Localization/strings.tsv"))if(line.StartsWith("story.sentinel.")||line.StartsWith("golem.")||line.StartsWith("biome.robotheaven\t")||line.StartsWith("boss.name.robotheaven\t"))Check(keys.Add(line.Split('\t')[0]),"Duplicate key");
            Check(keys.Count==124,"Wrong localization count");foreach(var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)foreach(var key in keys){Check(!string.IsNullOrWhiteSpace(table.GetEntry(key)?.LocalizedValue),"Missing "+key);translations++;}
            var biomesArray=(Array)typeof(TrainSim).GetField("Biomes",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var tracks=(Array)typeof(TrainSim).GetField("Tracks",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);Check(biomesArray.Length==15&&tracks.Length==15,"Biome/track indices mismatch");
            Check(Resources.Load<GameObject>("Models/Station_robotheaven")&&Resources.LoadAll<GameObject>("Scenery/robotheaven").Length==4,"New biome assets missing");
            return $"PASS: 4 chapters, ascent/retry, 16 images, {translations} translations, circuit connectivity/rotation, 6/7 moral boundary, saved outcomes, both Thanks branches and 15 aligned biome/track entries.";
        }
        finally
        {
            if(existed)File.WriteAllText(file,save);else if(File.Exists(file))File.Delete(file);typeof(SaveGame).GetField("data",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            if(had)PlayerPrefs.SetInt("locomotive.selected",selected);else PlayerPrefs.DeleteKey("locomotive.selected");Locomotives.StoryMode=mode;foreach(var pair in snapshot)pair.Key.SetValue(null,pair.Value);
        }
    }
}

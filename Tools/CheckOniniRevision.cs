using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
public static class CheckOniniRevision
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("OniniRevisionProbe").AddComponent<OniniRevisionProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);return "Started Tools/onini_revision_test.txt";
    }
}
public class OniniRevisionProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/onini_revision_test.txt";
    bool noEvents;int errors;
    object Get(object obj,string field)=>obj.GetType().GetField(field,F).GetValue(obj);
    void Set(object obj,string field,object value)=>obj.GetType().GetField(field,F).SetValue(obj,value);
    void Call(object obj,string method)=>obj.GetType().GetMethod(method,F).Invoke(obj,null);
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Log(string line)=>File.AppendAllText(Output,line+"\n");
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;Log(m);}}
    IEnumerator Start()
    {
        File.WriteAllText(Output,"RUNNING\n");Application.logMessageReceived+=Error;
        noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object value=null;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}
            if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Log("PASS: five Onini chapters, real before/after dialogue, 4-language text fit, manual advancement and Thanks; consoleErrors="+errors+". Travel clocks and wins accelerated; not a balance test.");Destroy(gameObject);
    }
    void ValidateCopy(Text story,Text talk)
    {
        var rows=File.ReadAllLines("Assets/Localization/strings.tsv");var header=rows[0].Split('\t');int count=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(var row in rows)
            {
                if(!row.StartsWith("story.onini."))continue;
                var cells=row.Split('\t');string value=table.GetEntry(cells[0])?.LocalizedValue;
                Check(value==cells[c].Replace("\\n","\n"),"Wrong copy: "+header[c]+" "+cells[0]);
                bool narrative=cells[0].Contains(".intro.")||cells[0].Contains(".outro.");
                if(narrative||cells[0].Contains(".before.")||cells[0].Contains(".after."))
                {
                    Text target=narrative?story:talk;var settings=target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width,0));
                    settings.resizeTextForBestFit=false;settings.fontSize=narrative?25:31;
                    float height=new TextGenerator().GetPreferredHeight(value,settings)/target.pixelsPerUnit;
                    Check(height<=target.rectTransform.rect.height+1f,"Text clips: "+header[c]+" "+cells[0]);
                }
                count++;
            }
        }
        Log("PASS "+count+" imported Onini values; narration and gameplay fit actual UI in four languages");
    }
    void Match(Talk talk,bool before)
    {
        Story.Duel(before,out var names,out var lines,out var sides);
        var shown=(string[])Get(talk,"lines");var speakers=(string[])Get(talk,"names");
        Check(shown.Length==lines.Length,"Wrong talk length");
        for(int i=0;i<lines.Length;i++)Check(shown[i]==lines[i]&&speakers[i]==names[i],"Wrong line/speaker "+Story.Chapter+"/"+i);
    }
    IEnumerator Close(Talk talk)
    {
        for(int i=0;i<20&&(talk.Open||talk.Finishing);i++)
        {
            if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.3f);
        }
        Check(!talk.Open&&!talk.Finishing,"Talk callback not finished");
    }
    IEnumerator Run()
    {
        Locomotives.Select(2);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(2);
        string[] biomes={"hell","desert","government","heaven","psychoworlds"};
        for(int c=0;c<5;c++)
        {
            Story.Play(c);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
            var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();var part=Story.Current;
            Check(intro&&intro.pages.Length==part.intro,"Opening pages "+c);
            for(int i=0;i<intro.pages.Length;i++)intro.ShowPage(i,true);
            if(c==0)
            {
                var layout=intro.gameObject.AddComponent<Talk>();layout.Show(new[]{L10n.T("hero.character.onini")},new[]{"Test"},new[]{false},null);
                Canvas.ForceUpdateCanvases();ValidateCopy((Text)Get(intro,"narration"),(Text)Get(layout,"what"));yield return Close(layout);Destroy(layout);
                intro.ShowPage(0,true);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/onini_revision_prologue.png");
            }
            Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.8f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c],"Wrong biome "+c);
            Set(train,"junctionAsked",true);Set(train,"storyClock",Story.Duration);Set(train,"duelCalled",false);
            var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+12f;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;
            Check(talk.Open,"Pre-battle talk absent "+c);Match(talk,true);
            yield return new WaitForSecondsRealtime(.35f);Check((int)Get(talk,"at")==0,"Automatic dialogue advancement");
            yield return Close(talk);yield return new WaitForSecondsRealtime(.6f);
            if(c==3){var boss=train.GetComponent<EyeBossEvent>();Check(boss.Active,"Heaven boss absent");boss.TestWin();}
            else{var rival=train.GetComponent<RivalEvent>();Check(rival.Active,"Rival absent "+c);rival.TestWin();}
            until=Time.unscaledTime+6f;while(!talk.Open&&Time.unscaledTime<until)yield return null;
            Check(talk.Open,"After-battle talk absent "+c);Match(talk,false);yield return Close(talk);
            if(c!=3)train.RaceFinished(true);
            yield return new WaitForSecondsRealtime(1.5f);intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"Ending missing "+c);
            for(int i=0;i<part.outro;i++)
            {
                Check(intro.pages[i].textKey==$"story.onini.{c}.outro.{i+1}","Wrong ending key");intro.ShowPage(i,true);
            }
            Log("PASS chapter "+c+" "+biomes[c]+": opening, real pre/post battle dialogue and ending");
            if(c==4){Check(Story.Over,"Story not over");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks absent");}
        }
        Check(errors==0,"Runtime errors");
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;}
}

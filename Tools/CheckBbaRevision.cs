using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
public static class CheckBbaRevision
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("BbaRevisionProbe").AddComponent<BbaRevisionProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);return "Started Tools/bba_revision_test.txt";
    }
}
public class BbaRevisionProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/bba_revision_test.txt";
    int errors;bool noEvents;
    object Get(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name)=>target.GetType().GetMethod(name,F).Invoke(target,null);
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
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
        Log("PASS: five BBA chapter openings/endings, real pre-finale dispatch, localized layout, manual dialogue and Thanks; consoleErrors="+errors+". Combat wins accelerated; not a balance test.");Destroy(gameObject);
    }
    void Fit(Text text,string value,string key,int size)
    {
        var settings=text.GetGenerationSettings(new Vector2(text.rectTransform.rect.width,0));
        settings.resizeTextForBestFit=false;settings.fontSize=size;
        float height=new TextGenerator().GetPreferredHeight(value,settings)/text.pixelsPerUnit;
        Check(height<=text.rectTransform.rect.height+1f,"Text clips: "+key+" height="+height+" box="+text.rectTransform.rect.height);
    }
    void ValidateCopy(Text storyText,Text talkText)
    {
        var rows=File.ReadAllLines("Tools/bba_story_revision.tsv");var header=rows[0].Split('\t');
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");int count=0;
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            for(int r=1;r<rows.Length;r++)
            {
                var cells=rows[r].Split('\t');var entry=table.GetEntry(cells[0]);
                Check(entry!=null&&entry.LocalizedValue==cells[c].Replace("\\n","\n"),"Wrong imported copy: "+header[c]+" "+cells[0]);
                bool narrative=cells[0].StartsWith("story.page.")||cells[0].Contains(".intro.")||cells[0].Contains(".outro.");
                Fit(narrative?storyText:talkText,entry.LocalizedValue,header[c]+" "+cells[0],narrative?25:31);count++;
            }
        }
        Log("PASS "+count+" imported translations and text heights in actual UI rectangles");
    }
    IEnumerator Close(Talk talk)
    {
        for(int i=0;i<20&&(talk.Open||talk.Finishing);i++)
        {
            if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.4f);
        }
        Check(!talk.Open&&!talk.Finishing,"Dialogue failed to finish");
    }
    IEnumerator Run()
    {
        Locomotives.Select(0);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(0);
        int[] openings={16,2,2,2,2};
        for(int c=0;c<5;c++)
        {
            Story.Play(c);Story.PassInspection();Story.PassAscent();Story.GatesPassed=c!=4;
            SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
            var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==openings[c],"Opening pages "+c);
            foreach(var page in intro.pages)Check(!L10n.T(page.textKey).Contains("No translation found"),"Missing page "+page.textKey);
            for(int i=0;i<intro.pages.Length;i++)intro.ShowPage(i,true);
            if(c==0)
            {
                var testTalk=intro.gameObject.AddComponent<Talk>();testTalk.Show(new[]{L10n.T("hero.character.bba")},new[]{"Test"},new[]{false},null);
                Canvas.ForceUpdateCanvases();ValidateCopy((Text)Get(intro,"narration"),(Text)Get(testTalk,"what"));
                yield return Close(testTalk);Destroy(testTalk);
            }
            Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.8f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train,"Gameplay missing "+c);
            Log("Gameplay chapter "+c+" biome="+train.BiomeId);
            var talk=train.GetComponent<Talk>();
            if(c==4)
            {
                Check(talk.Open&&((string[])Get(talk,"lines"))[0]==L10n.T("story.bba.gates.before"),"Gate intro missing");
                yield return Close(talk);yield return new WaitForSecondsRealtime(.3f);
                var gates=train.GetComponent<GatesEvent>();Check(gates.Active,"Gates did not start");gates.TestWin();
                yield return new WaitForSecondsRealtime(.2f);
                Check(talk.Open&&((string[])Get(talk,"lines"))[0]==L10n.T("story.bba.gates.after"),"Gate aftermath missing");
                yield return Close(talk);Check(Story.GatesPassed,"Gates progression missing");Log("PASS BBA gate dialogue before/after");
            }
            Set(train,"storyClock",Story.Duration);Set(train,"duelCalled",false);
            float until=Time.unscaledTime+12f;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;
            Check(talk.Open,"Real pre-finale dispatcher did not open "+c);
            Story.Duel(true,out var names,out var lines,out var sides);
            var actual=(string[])Get(talk,"lines");var who=(string[])Get(talk,"names");
            Check(actual.Length==lines.Length,"Wrong pre-finale length "+c);
            for(int i=0;i<lines.Length;i++)Check(actual[i]==lines[i]&&who[i]==names[i],"Wrong line/speaker "+c+"/"+i);
            if(c==1)Check(who[0]==L10n.T("race.radio.name")&&who[2]==L10n.T("hero.character.cerebron"),"City speakers");
            if(c==2)Check(who[0]==L10n.T("eyeboss.name")&&who[2]==who[0],"Moho mislabeled");
            if(c==4)Check(who[0]==L10n.T("race.radio.name")&&who[2]==who[0],"Phantom Tzmil in trench");
            yield return new WaitForSecondsRealtime(.4f);Check((int)Get(talk,"at")==0,"Dialogue advanced automatically");
            yield return Close(talk);yield return new WaitForSecondsRealtime(.5f);
            if(c==0||c==3)
            {
                var rival=train.GetComponent<RivalEvent>();Check(rival.Active,"Rival not started "+c);rival.TestWin();
                yield return new WaitForSecondsRealtime(.2f);Story.Duel(false,out names,out lines,out sides);
                actual=(string[])Get(talk,"lines");Check(talk.Open&&actual[0]==lines[0],"Rival aftermath missing "+c);
                yield return Close(talk);train.RaceFinished(true);
            }
            else if(c==2)
            {
                var eye=train.GetComponent<EyeBossEvent>();Check(eye.Active,"Eye boss not started");eye.TestWin();
                yield return new WaitForSecondsRealtime(.2f);Check(talk.Open&&((string[])Get(talk,"lines"))[0]==L10n.T("story.bba.2.after.1"),"BBA eye aftermath");
                yield return Close(talk);
            }
            else {Check(train.Racing,"Race/trench not started "+c);train.RaceFinished(true);}
            yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"Ending scene missing "+c);
            int endings=Story.Route("bba")[c].outro;
            for(int i=0;i<endings;i++)
            {
                Check(intro.pages[i].textKey==$"story.bba.{c}.outro.{i+1}","Wrong ending key");intro.ShowPage(i,true);
            }
            Log("PASS chapter "+c+": opening, real pre-finale dialogue, finale start and ending");
            if(c==4){Check(Story.Over,"Not final ending");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks missing");}
        }
        Check(errors==0,"Runtime console errors");
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;}
}

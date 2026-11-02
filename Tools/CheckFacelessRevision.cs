using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
public static class CheckFacelessRevision
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("FacelessRevisionProbe").AddComponent<FacelessRevisionProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/faceless_revision_test.txt";
    }
}
public class FacelessRevisionProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/faceless_revision_test.txt";
    Gamepad pad;readonly List<InputDevice> disabled=new();bool noEvents;int errors,firstFrame;
    object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Log(string line)=>File.AppendAllText(Output,line+"\n");
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;Log("CONSOLE: "+m);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    public IEnumerator Guarded()
    {
        File.WriteAllText(Output,"RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object next=null;bool more=false;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Travel, combat and drawing retrieval accelerated; not a balance test.");Destroy(gameObject);
    }
    IEnumerator Press()
    {
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
    }
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<18&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.3f);}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback stuck");
    }
    void Fits(Text target,string text,int size,string key)
    {
        var settings=target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width,0));settings.resizeTextForBestFit=false;settings.fontSize=size;
        Check(new TextGenerator().GetPreferredHeight(text,settings)/target.pixelsPerUnit<=target.rectTransform.rect.height+1,"Text clips: "+key);
    }
    void ValidateCopy(Text story,Text talk)
    {
        var rows=File.ReadAllLines("Assets/Localization/strings.tsv");var header=rows[0].Split('\t');int count=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string row in rows)
            {
                var cells=row.Split('\t');string key=cells[0];
                if(!key.StartsWith("story.faceless.")&&key!="descent.intro"&&key!="descent.arrived"&&key!="descent.lost"&&key!="forest.won.text"&&key!="forest.lost.text"&&key!="forest.lost.panic")continue;
                string text=table.GetEntry(key)?.LocalizedValue;Check(text==cells[c].Replace("\\n","\n"),"Wrong imported text: "+header[c]+" "+key);count++;
                if(!key.StartsWith("story.faceless.")||key.EndsWith(".title"))continue;
                bool dialogue=key.Contains(".before.")||key.Contains(".after.");Fits(dialogue?talk:story,text,dialogue?31:25,header[c]+" "+key);
            }
        }
        Check(count==300,"Unexpected copy count: "+count);Log("PASS 300 localized values and actual narrative/gameplay text heights in four languages");
    }
    void CheckTalk(Talk talk,bool before)
    {
        Story.Duel(before,out var names,out var lines,out var sides);var actual=(string[])Get(talk,"lines");var actualNames=(string[])Get(talk,"names");
        Check(actual.Length==lines.Length,"Dialogue count");for(int n=0;n<lines.Length;n++)Check(actual[n]==lines[n]&&actualNames[n]==names[n],"Wrong text/speaker");
        if(before&&Story.Chapter==2)Check(actualNames[0]==L10n.T("eyeboss.name"),"Placeholder robot name on eye creature");
    }
    IEnumerator Quiz()
    {
        var q=UnityEngine.Object.FindAnyObjectByType<Cuestionario>();Check(q&&q.mode==QuestionnaireMode.Alien,"Wrong officer mode");
        Call(q,"Activate",0);yield return null;
        for(int n=0;n<3;n++){int id=((int[])Get(q,"deck"))[n];Call(q,"Activate",(id%4+1)%4);yield return new WaitForSecondsRealtime(.85f);}
        Check(Get(q,"stage").ToString()=="Arrested","No arrest at three errors");Call(q,"Activate",0);yield return new WaitForSecondsRealtime(1.3f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&Story.InspectionPending&&intro.pages.Length==5,"Quiz retry narration");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.3f);
        q=UnityEngine.Object.FindAnyObjectByType<Cuestionario>();Call(q,"Activate",0);yield return null;
        for(int n=0;n<5;n++){int id=((int[])Get(q,"deck"))[n];Call(q,"Activate",id%4);yield return new WaitForSecondsRealtime(.85f);}
        Check(Get(q,"stage").ToString()=="Passed","Alien quiz did not pass");Call(q,"Activate",0);yield return new WaitForSecondsRealtime(1.3f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==3&&intro.pages[0].textKey=="story.faceless.1.intro.3","Missing resignation rejection");
        Log("PASS alien questionnaire arrest/retry/pass and rejected resignation");
    }
    IEnumerator Descent()
    {
        var f=UnityEngine.Object.FindAnyObjectByType<Ascenso>();Check(f&&Story.Descending,"Descent missing");
        Check(((Text)Get(f,"dialogue")).text==L10n.T("descent.intro"),"Wrong descent text");Set(f,"dialogueAge",1f);yield return Press();
        Check(Get(f,"phase").ToString()=="Flying","X did not start descent");
        for(int n=0;n<3;n++){Set(f,"immunity",0f);Call(f,"Hit");}
        Check(Get(f,"phase").ToString()=="Lost","Descent loss");Set(f,"dialogueAge",1f);yield return Press();yield return new WaitForSecondsRealtime(1.3f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==2&&Story.AscentPending,"Descent retry pages");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.3f);
        f=UnityEngine.Object.FindAnyObjectByType<Ascenso>();f.duration=1.2f;Set(f,"dialogueAge",1f);Set(f,"immunity",100f);yield return Press();yield return new WaitForSecondsRealtime(1.5f);
        Check(Get(f,"phase").ToString()=="Arrived","Descent arrival");Check(((Text)Get(f,"dialogue")).text==L10n.T("descent.arrived"),"Wrong arrival text");Set(f,"dialogueAge",1f);yield return Press();yield return new WaitForSecondsRealtime(1.3f);
        Log("PASS X, descent three-hit failure, retry and own arrival dialogue");
    }
    IEnumerator Run()
    {
        noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        foreach(var d in InputSystem.devices)if(d is Gamepad&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("FacelessRevisionPad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",10);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(10);Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==12,"Prologue missing");Check(UiKit.Now.main==UiKit.Hero("faceless").main,"Wrong palette");
        yield return Press();Check((int)Get(intro,"visible")>0,"X did not reveal text");
        var layout=intro.gameObject.AddComponent<Talk>();layout.Show(new[]{L10n.T("hero.character.faceless")},new[]{"Test"},new[]{false},null);Canvas.ForceUpdateCanvases();ValidateCopy((Text)Get(intro,"narration"),(Text)Get(layout,"what"));yield return CloseTalk(layout);Destroy(layout);
        string[] biomes={"alien","fortress","space","jungle"};int[] rivals={0,7,4,8};
        for(int c=0;c<4;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.7f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c],"Wrong biome "+c);
            Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+8;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Before dialogue missing");CheckTalk(talk,true);yield return new WaitForSecondsRealtime(.35f);Check((int)Get(talk,"at")==0,"Auto advance");
            yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.8f);
            if(c==1){var h=train.GetComponent<HoloDuel>();Check(h.Active,"Cerebron holo missing");h.TestWin();}
            else if(c==2){var eye=train.GetComponent<EyeBossEvent>();Check(eye.Active,"Eye boss missing");eye.TestWin();}
            else{var r=train.GetComponent<RivalEvent>();Check(r.Active&&(int)Get(r,"rivalIndex")==rivals[c],"Wrong rival");r.TestWin();}
            until=Time.unscaledTime+5;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"After dialogue missing");CheckTalk(talk,false);yield return CloseTalk(talk);
            if(c==0||c==3)train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.4f);intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"No narrative transition");
            if(c==0){Check(Story.InspectionPending,"No alien quiz");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.3f);yield return Quiz();intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();}
            if(c==2){Check(Story.AscentPending,"No descent");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.3f);yield return Descent();intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();}
            Log("PASS chapter "+(c+1)+": "+biomes[c]+", actual before/after dialogue and transition");
        }
        Check(Story.Chapter==4&&intro.pages.Length==6,"Forest opening pages");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.5f);
        var forest=UnityEngine.Object.FindAnyObjectByType<ForestChase>();Check(forest,"Forest missing");Set(forest,"age",1f);yield return Press();Check(forest.Playing,"Forest X failed");forest.TestWin();yield return new WaitForSecondsRealtime(.5f);
        Check(forest.Recovered==8,"Missing recovered drawings");Check(((Text)Get(forest,"instructions")).text==L10n.T("forest.won.text"),"Wrong forest result");Set(forest,"age",1f);yield return Press();yield return new WaitForSecondsRealtime(1.4f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&Story.Over&&intro.pages.Length==6,"Ending missing");intro.ShowPage(5,true);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("Tools/faceless_revision_ending.png");yield return new WaitForEndOfFrame();
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1);Check(GameObject.Find("Thanks"),"Thanks missing");Check(errors==0,"Console errors");Log("PASS forest, eight drawings, six-page epilogue, Thanks and manual dialogue");
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);}
}

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

public static class PlayR20Story
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("R20CampaignProbe").AddComponent<R20CampaignProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());
        return "R20 campaign test running: Temp/r20-play-check.txt";
    }
}
public class R20CampaignProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Gamepad pad;int errors;int firstFrame;
    bool noEvents;
    readonly System.Collections.Generic.List<InputDevice> disabled=new();
    object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    void Check(bool yes,string why){if(!yes)throw new Exception(why);}
    void KeepCurrent(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    void Log(string message)=>System.IO.File.AppendAllText("Temp/r20-play-check.txt",message+"\n");
    void Error(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){errors++;Log("CONSOLE "+condition);}}
    public IEnumerator Guarded()
    {
        System.IO.File.WriteAllText("Temp/r20-play-check.txt","RUNNING\n");
        firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object current=null;bool next=false;Exception fault=null;
            try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}
            if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Battle wins accelerated with test hooks; not a combat balance run.");
        Destroy(gameObject);
    }
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<18&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.3f);}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback stuck");
    }
    void CheckTalk(Talk talk,bool before)
    {
        Story.Duel(before,out var names,out var lines,out var sides);if(before&&Story.Current.race)names[0]=L10n.T("race.radio.name");
        var actual=(string[])Get(talk,"lines");var actualNames=(string[])Get(talk,"names");
        Check(actual.Length==lines.Length,"Dialogue count");for(int n=0;n<lines.Length;n++)Check(actual[n]==lines[n]&&actualNames[n]==names[n],"Wrong text/speaker: "+Story.Chapter+"/"+before+"/"+n);
    }
    IEnumerator Before(TrainSim train)
    {
        Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+8;
        while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Before dialogue missing");CheckTalk(talk,true);
        yield return new WaitForSecondsRealtime(.35f);Check((int)Get(talk,"at")==0,"Auto advance");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.7f);
    }
    IEnumerator After(TrainSim train)
    {
        var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+5;
        while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"After dialogue missing");CheckTalk(talk,false);yield return CloseTalk(talk);
    }
    void ValidateCopy(Text story,Text talk)
    {
        var rows=System.IO.File.ReadAllLines("Assets/Localization/strings.tsv");var header=rows[0].Split('\t');int count=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in rows)
            {
                if(!line.StartsWith("story.xiuhtecuhtli."))continue;var row=line.Split('\t');string text=table.GetEntry(row[0])?.LocalizedValue;
                Check(text==row[c].Replace("\\n","\n"),"Wrong imported text: "+header[c]+" "+row[0]);count++;
                bool dialogue=row[0].Contains(".before.")||row[0].Contains(".after."),narration=row[0].Contains(".intro.")||row[0].Contains(".outro.");
                if(!dialogue&&!narration)continue;Text target=dialogue?talk:story;var settings=target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width,0));settings.resizeTextForBestFit=false;settings.fontSize=dialogue?31:25;
                Check(new TextGenerator().GetPreferredHeight(text,settings)/target.pixelsPerUnit<=target.rectTransform.rect.height+1,"Text clips: "+header[c]+" "+row[0]);
            }
        }
        Check(count==252,"Unexpected localized count");Log("PASS 252 localized values and actual narrative/gameplay text heights in four languages");
    }
    public IEnumerator Run()
    {
        noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        foreach(var d in InputSystem.devices)if(d is Gamepad&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("R20ProbePad");InputSystem.onAfterUpdate+=KeepCurrent;
        PlayerPrefs.SetInt("locomotive.selected",8);Locomotives.StoryMode=true;Story.TestChapter=-1;
        SaveGame.NewStory(8);Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1.5f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6,"R20 prologue missing");
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        Check((int)Get(intro,"visible")>0,"X does not reveal dialogue");
        Check(UiKit.Now.main==UiKit.Hero("xiuhtecuhtli").main,"Wrong hero palette");
        var layout=intro.gameObject.AddComponent<Talk>();layout.Show(new[]{L10n.T("hero.character.xiuhtecuhtli")},new[]{"Test"},new[]{false},null);
        Canvas.ForceUpdateCanvases();ValidateCopy((Text)Get(intro,"narration"),(Text)Get(layout,"what"));yield return CloseTalk(layout);Destroy(layout);
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(2f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId=="jungle","Jungle start");
        yield return Before(train);
        var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==6,"Culu duel");
        rival.TestWin();yield return After(train);train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==4&&Story.InspectionPending,"Culu win -> city arrival");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.5f);
        var quiz=UnityEngine.Object.FindAnyObjectByType<Cuestionario>();Check(quiz&&quiz.mode==QuestionnaireMode.Human,"Human checkpoint");
        int[] correct=(int[])typeof(Cuestionario).GetField("Correct",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Call(quiz,"Activate",0);yield return null;
        for(int i=0;i<3;i++)
        {
            int mask=correct[((int[])Get(quiz,"deck"))[i]],a=0;while((mask&(1<<a))!=0)a++;
            Call(quiz,"Activate",a);yield return new WaitForSecondsRealtime(.85f);
        }
        Check(Get(quiz,"stage").ToString()=="Arrested","Three wrong answers");
        Call(quiz,"Activate",0);yield return new WaitForSecondsRealtime(1.5f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==4,"Arrest retry narration");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.5f);
        quiz=UnityEngine.Object.FindAnyObjectByType<Cuestionario>();Call(quiz,"Activate",0);yield return null;
        for(int i=0;i<5;i++)
        {
            int mask=correct[((int[])Get(quiz,"deck"))[i]],a=0;while((mask&(1<<a))==0)a++;
            Call(quiz,"Activate",a);yield return new WaitForSecondsRealtime(.85f);
        }
        Check(Get(quiz,"stage").ToString()=="Passed","Five correct answers");
        Call(quiz,"Activate",0);yield return new WaitForSecondsRealtime(2f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId=="city","Quiz -> city");
        yield return Before(train);var race=train.GetComponent<RaceEvent>();Check(race.Active,"Timed city race missing");
        yield return new WaitForSecondsRealtime(1f);
        var me=Get(race,"me");Set(me,"finishedAt",Time.unscaledTime);yield return new WaitForSecondsRealtime(1f);
        var launch=race.GetType().GetProperty("LaunchActor",F).GetValue(race);
        Check(ReferenceEquals(Get(race,"winner"),me)&&launch!=null&&(int)Get(launch,"loco")==0,"R20 wins / BBA selected to launch");
        Check((float)Get(me,"rampS")==0&&(float)Get(launch,"rampS")>0,"R20 blocked / BBA moving up ramp");
        Call(race,"End");yield return new WaitForSecondsRealtime(1.5f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==5&&Story.Chapter==2&&!Story.AscentPending,"Race -> ocean without ascent");
        Log("PASS prologue X, Culu/jungle, human quiz arrest/retry/pass, city race R20 wins but BBA launches.");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(2f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train.BiomeId=="ocean","Ocean chapter");
        yield return Before(train);
        rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==0,"Ocean BBA duel");
        rival.TestWin();yield return After(train);train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&Story.Chapter==3,"Ocean win -> volcano narration");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(2f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train.BiomeId=="volcano","Volcano chapter");
        yield return Before(train);
        var holo=train.GetComponent<HoloDuel>();Check(holo.Active&&Story.Current.rival==3,"Tzmil holo duel");
        Check(((UnityEngine.UI.Text)Get(holo,"rivalName")).text==L10n.T(Locomotives.HeroNameKey(3)).ToUpperInvariant(),"Tzmil displayed");
        holo.TestWin();yield return After(train);yield return new WaitForSecondsRealtime(1.5f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&Story.Chapter==4,"Volcano win -> government narration");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(2f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train.BiomeId=="government","Government biome");
        var kinds=(GameObject[][][])Get(train,"sceneryKinds");
        for(int i=0;i<3;i++)Check(kinds[9][i].Length>0,"Missing government scenery size "+i);
        yield return Before(train);
        rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==8,"Clone locomotive");
        rival.TestWin();yield return After(train);train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);
        intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==7&&Story.Over,"Ending");
        intro.ShowPage(4,true);for(int frame=0;frame<18;frame++)yield return null;
        System.IO.Directory.CreateDirectory("Tools/r20_test_session");ScreenCapture.CaptureScreenshot("Tools/r20_test_session/personality-ending.png");yield return new WaitForEndOfFrame();yield return null;
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.4f);
        Check(GameObject.Find("Thanks"),"Thanks missing");
        Check(Time.frameCount>firstFrame+100,"Game did not advance");
        Check(errors==0,"Game console errors");
        Log("PASS ocean/BBA, volcano/Tzmil holo, government security scenery, own clone, seven ending pages and Thanks.");
    }
    void OnDestroy()
    {
        InputSystem.onAfterUpdate-=KeepCurrent;Application.logMessageReceived-=Error;
        TrainSim.NoEvents=noEvents;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
        foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

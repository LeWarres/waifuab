using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayCerebronStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("CerebronStoryProbe").AddComponent<CerebronStoryProbe>();UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);
        probe.StartCoroutine(probe.Guarded());return "Campaign test started: Temp/cerebron-play-check.txt";
    }
}
public class CerebronStoryProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Gamepad pad;readonly System.Collections.Generic.List<InputDevice> disabled=new();int errors,firstFrame;
    object Get(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
    void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Log(string message)=>System.IO.File.AppendAllText("Temp/cerebron-play-check.txt",message+"\n");
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("CONSOLE: "+message);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<12&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return null;yield return null;}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback did not finish");
    }
    public IEnumerator Guarded()
    {
        System.IO.File.WriteAllText("Temp/cerebron-play-check.txt","RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var steps=Run();while(true)
        {
            bool more=false;object value=null;Exception fault=null;
            try{more=steps.MoveNext();if(more)value=steps.Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more)break;yield return value;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Victories accelerated; combat balance not fully tested.");Destroy(gameObject);
    }
    IEnumerator Run()
    {
        foreach(var device in InputSystem.devices)if(device is Gamepad&&device.enabled){disabled.Add(device);InputSystem.DisableDevice(device);}
        pad=InputSystem.AddDevice<Gamepad>("CerebronProbePad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",7);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(7);Story.Play(0);
        SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==7,"Missing prologue");
        Check(UiKit.Now.main==UiKit.Hero("cerebron").main,"Wrong hero palette");
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;Check((int)Get(intro,"visible")>0,"X did not reveal text");
        ScreenCapture.CaptureScreenshot("Temp/cerebron-prologue.png");yield return new WaitForEndOfFrame();
        string[] biomes={"space","alien","fortress","thulu"};int[] rivals={4,0,10,11},pages={4,5,6,5};
        for(int chapter=0;chapter<4;chapter++)
        {
            Check(Story.Chapter==chapter,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.8f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[chapter],"Wrong biome "+chapter);
            Check(train.Player&&Locomotives.Hero=="cerebron","Wrong player");
            // Exercise the real timed-finale trigger and its pre-battle conversation.
            Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);
            var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+8f;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;
            Check(talk.Open,"Pre-battle dialogue did not start "+chapter);
            Story.Duel(true,out var names,out var lines,out var speakers);
            Check(((string[])Get(talk,"lines")).Length==lines.Length,"Wrong pre-battle text count");
            if(chapter==0)Check(((string[])Get(talk,"names"))[0]==L10n.T("story.cerebron.roger"),"Wrong Roger speaker");
            yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.9f);
            if(chapter==0)
            {
                var eye=train.GetComponent<EyeBossEvent>();Check(eye.Active,"Roger finale not active");
                ScreenCapture.CaptureScreenshot("Temp/cerebron-roger-finale.png");yield return new WaitForEndOfFrame();eye.TestWin();
            }
            else if(chapter==2)
            {
                var holo=train.GetComponent<HoloDuel>();Check(holo.Active,"Holo-Duel not active");
                Check(((UnityEngine.UI.Text)Get(holo,"rivalName")).text==L10n.T("hero.character.faceless").ToUpperInvariant(),"Wrong holo opponent");
                ScreenCapture.CaptureScreenshot("Temp/cerebron-faceless-holo.png");yield return new WaitForEndOfFrame();holo.TestWin();
            }
            else
            {
                var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[chapter],"Wrong rival "+chapter);
                if(chapter==3)
                {
                    Check((string)Get(rival,"label")==L10n.T("loco.crochet"),"Crochtrain mislabeled");
                    var turns=(Array)typeof(RivalEvent).GetField("Turns",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                    int driver=(int)typeof(RivalEvent).GetField("Driver",BindingFlags.Static|BindingFlags.NonPublic).GetRawConstantValue(),index=-1;
                    for(int n=0;n<turns.Length;n++)if((int)turns.GetValue(n).GetType().GetField("Item1").GetValue(turns.GetValue(n))==driver){index=n;break;}
                    Check(index>0,"Driver turn missing");Set(rival,"turn",index-1);Set(rival,"turnLeft",0f);Set(rival,"startsAt",0f);
                    yield return new WaitForSecondsRealtime(.3f);Check(!train.GetComponent<BrawlEvent>().Active,"Borrowed BBA driver");
                    ScreenCapture.CaptureScreenshot("Temp/cerebron-crochet-finale.png");yield return new WaitForEndOfFrame();
                }
                rival.TestWin();
            }
            float postUntil=Time.unscaledTime+5f;
            while(!talk.Open&&Time.unscaledTime<postUntil)yield return null; // Holo-Duel shows its result for 1.8 seconds first
            Check(talk.Open,"Post-battle dialogue missing "+chapter);yield return CloseTalk(talk);
            if(chapter==1||chapter==3)train.RaceFinished(true); // skip the upgrade choice, preserving the normal story victory path
            yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==pages[chapter],"Wrong narrative transition "+chapter);
            Check(!Story.InspectionPending&&!Story.AscentPending,"Unexpected minigame");Log("PASS chapter "+(chapter+1)+": "+biomes[chapter]+", correct finale, before/after conversations and next pages");
        }
        Check(Story.Over,"Ending missing");intro.ShowPage(4,true);ScreenCapture.CaptureScreenshot("Temp/cerebron-baratito-ending.png");yield return new WaitForEndOfFrame();
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks screen missing");
        Check(errors==0,"Console errors during campaign");Log("PASS: X, purple palette, four biome/finale chains, driverless Crochtrain and Thanks.");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayGigerStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("GigerCampaignProbe").AddComponent<GigerCampaignProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/giger_test_session/play-check.txt";
    }
}
public class GigerCampaignProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/giger_test_session/";
    Gamepad pad;readonly List<InputDevice> disabled=new();int errors,firstFrame;
    object Get(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
    void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Log(string line)=>File.AppendAllText(Output+"play-check.txt",line+"\n");
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("CONSOLE: "+message);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    public IEnumerator Guarded()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"play-check.txt","RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object next=null;bool more=false;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}
            if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Rides and combat wins accelerated; new puzzles solved through their real actions.");Destroy(gameObject);
    }
    IEnumerator Press(GamepadButton button=GamepadButton.South)
    {
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
    }
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<18&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return null;yield return null;}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback stuck");
    }
    IEnumerator Crates(RaceEvent game,int hits)
    {
        Check(game.CrateRun&&game.Active,"No crate run");Check(game.CratesBroken==0&&game.CratesSeen==0,"Counters not reset");
        Check(((UnityEngine.UI.Text)Get(game,"hint")).text==L10n.T("giger.crates.controls"),"Wrong briefing");
        yield return Press();Check(!((Slanted)Get(game,"briefing")).gameObject.activeSelf,"X did not dismiss briefing");
        Set(game,"startsAt",Time.unscaledTime-.1f);object me=Get(game,"me");Vector3 before=(Vector3)Get(me,"pos");
        for(int f=0;f<75;f++){InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1f});yield return null;}
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        Check(Vector3.Distance(before,(Vector3)Get(me,"pos"))>1f,"R2 did not drive");
        var boxes=(System.Collections.IList)Get(game,"crates");Check(boxes.Count==40,"Wrong box count");
        for(int i=0;i<40;i++)
        {
            object box=boxes[i];var type=box.GetType();Vector3 at=(Vector3)type.GetField("Item2").GetValue(box);float s=(float)type.GetField("Item3").GetValue(box);
            var dirs=(List<Vector3>)Get(game,"dirs");int seg=(int)(s/20f);Vector3 d=dirs[seg],right=Vector3.Cross(Vector3.up,d);
            // Accelerate the long drive between boxes; keep real swept collision and miss detection.
            Vector3 pos=i<hits ? at : at+right*14f+d*14f;
            Set(me,"pos",pos);Set(me,"seg",seg);Set(me,"speed",0f);Set(me,"recoverUntil",0f);Set(game,"cratePrevious",pos-d*2f);
            yield return null;yield return null;
            Check(game.CratesSeen==i+1,"Unresolved/double-counted box "+i+" seen="+game.CratesSeen);
            Check(game.CratesBroken==Mathf.Min(i+1,hits),"Wrong collision score "+i);
        }
        ScreenCapture.CaptureScreenshot(Output+"crates-"+hits+".png");yield return new WaitForEndOfFrame();
        Check(game.CratesSeen==40&&game.CratesBroken==hits,"Wrong total");
        Set(me,"finishedAt",Time.unscaledTime);yield return null;Check((float)Get(game,"endsAt")>0,"No result banner");
        Set(game,"endsAt",Time.unscaledTime-.01f);yield return null;yield return null;
        Check(!game.Active,"Run did not end");Log("PASS real controller R2 and 40 collision/miss resolutions; score="+hits);
    }
    IEnumerator Run()
    {
        foreach(var d in InputSystem.devices)if(d is Gamepad&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("GigerProbePad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",9);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(9);Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==4,"Missing prologue");
        Check(UiKit.Now.main==UiKit.Hero("giger").main,"Wrong Giger palette");
        yield return Press();Check((int)Get(intro,"visible")>0,"X did not reveal story");intro.ShowPage(0,true);ScreenCapture.CaptureScreenshot(Output+"prologue.png");yield return new WaitForEndOfFrame();
        string[] biomes={"psychoworlds","thulu","hell","heaven"};int[] rivals={6,11,2,5};
        for(int c=0;c<4;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.6f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c]&&Story.Hero=="giger","Wrong biome/player "+c);
            Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+10;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Timed finale missing "+c);yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.8f);
            if(c==2)
            {
                var race=train.GetComponent<RaceEvent>();yield return Crates(race,34);Check((bool)Get(train,"finaleRetry"),"34/40 did not fail");
                Call(train,"RetryFinale");yield return new WaitForSecondsRealtime(1.3f);Check(race.Active&&race.CrateRun,"Retry did not rebuild crates");yield return Crates(race,35);
            }
            else
            {
                var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[c],"Wrong rival "+c);
                if(c==1||c==3)Check(Story.MachineRival,"Phantom humanoid rival");
                ScreenCapture.CaptureScreenshot(Output+"duel-"+c+".png");yield return new WaitForEndOfFrame();rival.TestWin();
            }
            until=Time.unscaledTime+5;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"After-duel text missing "+c);yield return CloseTalk(talk);
            if(c!=2)train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"Missing story transition "+c);
            Log("PASS chapter "+(c+1)+": "+biomes[c]);
        }
        Check(Story.Over&&intro.pages.Length==5,"Ending missing");intro.ShowPage(4,true);ScreenCapture.CaptureScreenshot(Output+"ending.png");yield return new WaitForEndOfFrame();
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1);Check(GameObject.Find("Thanks"),"Missing Thanks");Check(errors==0,"Console errors");Log("PASS ending and Thanks");
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayGabrielStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("GabrielCampaignProbe").AddComponent<GabrielCampaignProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/gabriel_test_session/play-check.txt";
    }
}
public class GabrielCampaignProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/gabriel_test_session/";
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
    IEnumerator Run()
    {
        foreach(var d in InputSystem.devices)if(d is Gamepad&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("GabrielProbePad");InputSystem.onAfterUpdate+=Current;
        Locomotives.Select(5);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(5);Story.Play(0);SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(1);
        var menu=UnityEngine.Object.FindAnyObjectByType<SelectLocomotive>();Set(menu,"selected",5);Call(menu,"Refresh");Set(menu,"asking",true);Call(menu,"Go");yield return new WaitForSecondsRealtime(1.4f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6&&Story.Hero=="gabriel","Selector did not open Gabriel prologue");
        Check(UiKit.Now.main==UiKit.Hero("gabriel").main,"Wrong story palette");yield return Press();Check((int)Get(intro,"visible")>0,"X did not reveal text");intro.ShowPage(2,true);ScreenCapture.CaptureScreenshot(Output+"prologue.png");yield return new WaitForEndOfFrame();
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.6f);var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var originalTrain=train;var talk=train.GetComponent<Talk>();
        Check(train.BiomeId=="heaven"&&train.PassiveLevel("luck")==3,"Wrong starting biome/luck");
        Set(train,"junctionAsked",true);Set(train,"storyClock",1798f);yield return null;yield return null;Check(!(bool)Get(train,"duelCalled"),"Duel triggered before 30 minutes");
        Set(train,"storyClock",1800f);float until=Time.unscaledTime+10;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"30-minute finale did not start");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.6f);
        int[] rivals={2,3,9,1};
        for(int c=0;c<4;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter "+c);var enemy=train.GetComponent<RivalEvent>();Check(enemy.Active&&(int)Get(enemy,"rivalIndex")==rivals[c],"Wrong rival "+c);
            int cars=train.CarCount,level=train.Player.level,luck=train.PassiveLevel("luck");
            ScreenCapture.CaptureScreenshot(Output+"duel-"+c+".png");yield return new WaitForEndOfFrame();enemy.TestWin();yield return null;yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.8f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"Missing post-duel pages "+c);
            intro.ShowPage(0,true);ScreenCapture.CaptureScreenshot(Output+"story-"+c+".png");yield return new WaitForEndOfFrame();
            if(c==3)break;
            Check(train.GabrielInterludeOpen&&train==originalTrain&&SceneManager.GetActiveScene().name=="sandScenario","Biome restarted during interlude");
            Check(train.CarCount==cars&&train.Player.level==level&&train.PassiveLevel("luck")==luck,"Build lost during dialogue");
            Call(intro,"Finish");yield return null;yield return null;Check(train.GabrielDraft==1&&Time.timeScale==0f,"Weapon choice missing or not frozen");
            int tokens=SaveGame.GabrielBuild.Count;yield return Press();yield return null;Check(train.GabrielDraft==2&&SaveGame.GabrielDraft==2,"X did not choose weapon");
            ScreenCapture.CaptureScreenshot(Output+"passive-"+c+".png");yield return new WaitForEndOfFrame();yield return Press();yield return null;
            Check(train.GabrielDraft==0&&SaveGame.GabrielDraft==0&&SaveGame.GabrielBuild.Count==tokens+2,"Expected exactly weapon and passive choices");
            Check(train==originalTrain&&train.BiomeId=="heaven","World replaced between duels");
            Check(talk.Open,"Direct duel briefing missing");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.6f);
            Log("PASS same TrainSim instance, retained train/equipment, frozen interlude, X weapon + passive, next direct duel "+(c+1));
        }
        Check(Story.Over&&intro.pages.Length==6,"Final ending missing");intro.ShowPage(5,true);ScreenCapture.CaptureScreenshot(Output+"ending.png");yield return new WaitForEndOfFrame();Call(intro,"Finish");yield return new WaitForSecondsRealtime(.8f);Check(GameObject.Find("Thanks"),"Thanks missing");
        Log("PASS campaign selector, 30-minute threshold, +3 luck, Onini/Tzmil/Giger/ReyMar and Thanks");
        // Resume a partially completed upgrade pair: only the remaining passive may be granted.
        SaveGame.NewStory(5);SaveGame.SaveGabrielBuild(new List<string>{"wagon:turret"},2);Story.Play(1);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.5f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train.GabrielDraft==2,"Resume repeated weapon choice");yield return Press();yield return null;Check(SaveGame.GabrielDraft==0&&SaveGame.GabrielBuild.Count==2,"Resume duplicated/lost upgrade");
        Story.Play(1);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.5f);train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();talk=train.GetComponent<Talk>();
        Check(train.GabrielDraft==0&&talk.Open,"Completed pair not resumed directly to duel");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.3f);Check(train.GetComponent<RivalEvent>().Active,"Resume finale missing");
        Check(errors==0,"Console errors");Log("PASS saved partial and completed upgrades resume without duplicates");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayCuluStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("CuluStoryProbe").AddComponent<CuluStoryProbe>();UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);
        probe.StartCoroutine(probe.Guarded());return "Culu campaign test started: Temp/culu-play-check.txt";
    }
}
public class CuluStoryProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Gamepad pad;readonly System.Collections.Generic.List<InputDevice> disabled=new();int errors,firstFrame;
    object Get(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
    void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Log(string message)=>System.IO.File.AppendAllText("Temp/culu-play-check.txt",message+"\n");
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("CONSOLE: "+message);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    public IEnumerator Guarded()
    {
        System.IO.File.WriteAllText("Temp/culu-play-check.txt","RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var steps=Run();while(true)
        {
            bool more=false;object value=null;Exception fault=null;
            try{more=steps.MoveNext();if(more)value=steps.Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more)break;yield return value;
        }
        Log("PASS: frames="+(Time.frameCount-firstFrame)+", consoleErrors="+errors+". Victories accelerated; combat balance not fully tested.");Destroy(gameObject);
    }
    IEnumerator Run()
    {
        foreach(var device in InputSystem.devices)if(device is Gamepad&&device.enabled){disabled.Add(device);InputSystem.DisableDevice(device);}
        pad=InputSystem.AddDevice<Gamepad>("CuluProbePad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",6);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(6);Story.Play(0);
        SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6,"Missing Culu prologue");
        Check(UiKit.Now.main==UiKit.Hero("culu").main,"Wrong hero palette");
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;Check((int)Get(intro,"visible")>0,"X does not reveal dialogue");
        string[] biomes={"ocean","jungle","desert","psychoworlds","thulu"};int[] rivals={2,8,0,9,11},pages={4,4,6,4,5};
        for(int chapter=0;chapter<5;chapter++)
        {
            Check(Story.Chapter==chapter,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.8f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[chapter],"Wrong biome "+chapter);
            Check(train.Player&&Locomotives.Hero=="culu","Wrong hero in gameplay");
            if(chapter==4)
            {
                var pools=(GameObject[][][])Get(train,"sceneryKinds");for(int size=0;size<3;size++)Check(pools[13][size].Length>0,"Missing Thulu scenery size "+size);
                Check(Resources.Load<GameObject>("Models/Station_thulu"),"Missing station");
                ScreenCapture.CaptureScreenshot("Temp/culu-thulu-gameplay.png");yield return new WaitForEndOfFrame();yield return null;
            }
            Call(train,"StartFinale");yield return new WaitForSecondsRealtime(.9f);
            var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[chapter],"Wrong final rival "+chapter);
            if(chapter>=3)
            {
                Check((string)Get(rival,"label")==L10n.T(Locomotives.NameKeys[rivals[chapter]]),"Machine mislabeled as BBA");
                // Force the transition into its driver turn: driverless trains must send boarders instead of BBA.
                var turns=(Array)typeof(RivalEvent).GetField("Turns",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                int driver=(int)typeof(RivalEvent).GetField("Driver",BindingFlags.Static|BindingFlags.NonPublic).GetRawConstantValue(),index=-1;
                for(int n=0;n<turns.Length;n++)if((int)turns.GetValue(n).GetType().GetField("Item1").GetValue(turns.GetValue(n))==driver){index=n;break;}
                Check(index>0,"Driver turn missing");Set(rival,"turn",index-1);Set(rival,"turnLeft",0f);Set(rival,"startsAt",0f);
                yield return new WaitForSecondsRealtime(.4f);Check(!train.GetComponent<BrawlEvent>().Active,"Borrowed BBA driver in machine fight");
            }
            if(chapter==4){yield return new WaitForSecondsRealtime(2.5f);ScreenCapture.CaptureScreenshot("Temp/culu-crochetrain-finale.png");yield return new WaitForEndOfFrame();yield return null;}
            rival.TestWin();yield return null;train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==pages[chapter],"Wrong narrative transition "+chapter);
            Check(!Story.InspectionPending&&!Story.AscentPending,"Unexpected minigame");Log("PASS chapter "+(chapter+1)+": "+biomes[chapter]+", rival="+rivals[chapter]);
        }
        Check(Story.Over,"Ending missing");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks screen missing");
        Check(Time.frameCount>firstFrame+100&&errors==0,"Console/frame progression");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
        Log("PASS: X, five biomes/finales, machine boarders, three Thulu scenery sizes, narrative transitions and Thanks.");
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);
    }
}

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayOniniStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("OniniStoryProbe").AddComponent<OniniStoryProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());
        return "Onini campaign running; Temp/onini-play-check.txt";
    }
}
public class OniniStoryProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Gamepad pad;readonly System.Collections.Generic.List<InputDevice> disabled=new();int errors,firstFrame;
    object Get(object o,string key)=>o.GetType().GetField(key,F).GetValue(o);
    void Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key,F).Invoke(o,args);
    void Check(bool value,string why){if(!value)throw new Exception(why);}
    void Log(string message)=>System.IO.File.AppendAllText("Temp/onini-play-check.txt",message+"\n");
    void Error(string text,string trace,LogType kind){if(kind==LogType.Error||kind==LogType.Exception){errors++;Log("CONSOLE "+text);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    void Input(GamepadState state){pad.MakeCurrent();InputSystem.QueueStateEvent(pad,state);}
    public IEnumerator Guarded()
    {
        System.IO.File.WriteAllText("Temp/onini-play-check.txt","RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var it=Run();while(true)
        {
            bool more=false;object current=null;Exception fault=null;
            try{more=it.MoveNext();if(more)current=it.Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}
            if(!more)break;yield return current;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Victories accelerated; not a complete combat balance run.");Destroy(gameObject);
    }
    bool AllBlack(Transform root)
    {
        var block=new MaterialPropertyBlock();int n=0;
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){renderer.GetPropertyBlock(block);if(block.GetColor("_MainColor")!=Color.black)return false;n++;}
        return n>0;
    }
    IEnumerator Run()
    {
        foreach(var d in InputSystem.devices)if(d is Gamepad&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("OniniProbePad");InputSystem.onAfterUpdate+=Current;
        EyeBossEvent.Botting=false;PlayerPrefs.SetInt("locomotive.selected",2);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(2);
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6,"Onini prologue");
        Check(UiKit.Now.main==UiKit.Hero("onini").main,"Onini story palette");
        Input(new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;Input(new GamepadState());yield return null;
        Check((int)Get(intro,"visible")>0,"X dialogue");
        int[] rivals={0,0,4,4,2};string[] biomes={"hell","desert","government","heaven","psychoworlds"};int[] pageCounts={4,5,6,7,4};
        for(int c=0;c<5;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter before "+c);Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.8f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c],"Wrong biome "+c);
            if(c==0||c==3||c==4)
            {
                var pools=(GameObject[][][])Get(train,"sceneryKinds");
                for(int size=0;size<3;size++)Check(pools[train.Biome][size].Length>0,"Missing scenery class "+c+"/"+size);
            }
            Call(train,"StartFinale");yield return new WaitForSecondsRealtime(.9f);
            if(c==3)
            {
                var eye=train.GetComponent<EyeBossEvent>();Check(eye.Active,"Heaven fight not active");
                var boss=(Transform)Get(eye,"boss");Check(boss.name=="HeavenGuardian","Wrong heavenly enemy");
                int rings=0,feathers=0;foreach(Transform part in boss){if(part.name=="OphanimRing")rings++;if(part.name=="IvoryFeather")feathers++;}
                Check(rings==3&&feathers==36&&((IList)Get(eye,"eyes")).Count==eye.eyeCount,"Heaven model/eyes");
                yield return new WaitForSecondsRealtime(2f);
                float angle=(float)Get(eye,"angle");Input(new GamepadState{leftStick=Vector2.right});yield return new WaitForSecondsRealtime(.18f);Input(new GamepadState());yield return null;
                Check(Mathf.Abs((float)Get(eye,"angle")-angle)>.01f,"Heaven movement");
                Input(new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;Input(new GamepadState());yield return new WaitForSecondsRealtime(.06f);
                Check((float)Get(eye,"height")>0f,"Heaven jump");
                ScreenCapture.CaptureScreenshot("Temp/onini-heaven-guardian.png");
                eye.TestWin();eye.Close();train.RaceFinished(true);
            }
            else
            {
                var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[c],"Wrong rival "+c);
                if(c==4)
                {
                    var root=(Transform)Get(rival,"root");Check(AllBlack(root)&&root.GetComponentInChildren<Animator>(),"Shadow train/avatar not black");
                    var block=new MaterialPropertyBlock();var own=train.Player.GetComponentInChildren<Renderer>();own.GetPropertyBlock(block);Check(block.GetColor("_MainColor")!=Color.black,"Player was blackened");
                    ScreenCapture.CaptureScreenshot("Temp/onini-shadow-train.png");
                    var brawl=train.GetComponent<BrawlEvent>();Check(brawl.BeginDriver(2,train.CarPosition(0),won=>{}),"Shadow boarding start");
                    yield return new WaitForSecondsRealtime(1f);
                    var drivers=(IDictionary)Get(brawl,"drivers");var driver=drivers["onini"];var rig=Get(driver,"rig");
                    Check(AllBlack((Transform)Get(rig,"root"))&&AllBlack((Transform)Get(brawl,"leaper")),"Shadow driver/leaper not black");
                    brawl.TestWin();yield return new WaitForSecondsRealtime(.3f);
                }
                rival.TestWin();yield return null;train.RaceFinished(true);
            }
            yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
            Check(intro&&intro.pages.Length==pageCounts[c]&&!Story.InspectionPending&&!Story.AscentPending,"Wrong narration transition "+c);
            Log("PASS chapter "+(c+1)+" "+biomes[c]+": finale, art and next dialogue.");
        }
        Check(Story.Over,"Final ending");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks absent");
        // Existing space fight must keep its original model.
        PlayerPrefs.SetInt("locomotive.selected",0);Story.Play(2);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.5f);
        var original=UnityEngine.Object.FindAnyObjectByType<TrainSim>().GetComponent<EyeBossEvent>();original.Begin(won=>{});
        yield return new WaitForSecondsRealtime(.5f);Check(((Transform)Get(original,"boss")).name=="EyeBall","Space boss changed");original.TestWin();original.Close();
        Check(Time.frameCount>firstFrame+100&&errors==0,"Frames/console");
        PlayerPrefs.SetInt("locomotive.selected",2);Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
        Log("PASS X, scenery sizes, heaven movement/jump, shadow train/driver/leaper, normal player, Thanks and original space boss.");
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayGigerDrive
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("GigerDriveProbe").AddComponent<GigerDriveProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/giger_test_session/drive-check.txt";
    }
}
public class GigerDriveProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/giger_test_session/";
    Gamepad pad;readonly List<InputDevice> disabled=new();int errors,firstFrame;
    object Get(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
    void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Log(string line)=>File.AppendAllText(Output+"drive-check.txt",line+"\n");
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("CONSOLE: "+message);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    public IEnumerator Guarded()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"drive-check.txt","RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object next=null;bool more=false;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}
            if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Approach to finale accelerated; the complete crate course was driven using virtual controller input.");Destroy(gameObject);
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
        pad=InputSystem.AddDevice<Gamepad>("GigerDrivePad");InputSystem.onAfterUpdate+=Current;
        Locomotives.Select(9);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(9);Story.Play(2);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.8f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Call(train,"StartFinale");yield return new WaitForSecondsRealtime(.4f);
        var game=train.GetComponent<RaceEvent>();yield return Press();Check(!((Slanted)Get(game,"briefing")).gameObject.activeSelf,"Start failed");
        object me=Get(game,"me");float began=Time.unscaledTime;bool captured=false;int lastSeen=0;
        // Only virtual controller input from here: no speed, clock or position edits.
        while(game.Active && Time.unscaledTime-began<190f)
        {
            float progress=(float)Get(me,"progress"),speed=(float)Get(me,"speed");
            Vector3 pos=(Vector3)Get(me,"pos");var points=(List<Vector3>)Get(game,"points");var dirs=(List<Vector3>)Get(game,"dirs");
            float ahead=progress+Mathf.Max(22f,speed*.38f);int seg=Mathf.Clamp((int)(ahead/20f),0,points.Count-2);
            Vector3 target=points[seg]+dirs[seg]*(ahead-seg*20f);float lane=0;
            var boxes=(System.Collections.IList)Get(game,"crates");var resolved=(HashSet<int>)Get(game,"resolvedCrates");
            for(int i=0;i<boxes.Count;i++)if(!resolved.Contains(i))
            {
                var b=boxes[i];var type=b.GetType();Vector3 at=(Vector3)type.GetField("Item2").GetValue(b);float s=(float)type.GetField("Item3").GetValue(b);
                int k=(int)(s/20f);Vector3 centre=points[k]+dirs[k]*(s-k*20f);lane=Vector3.Dot(at-centre,Vector3.Cross(Vector3.up,dirs[k]));break;
            }
            target+=Vector3.Cross(Vector3.up,dirs[seg])*lane;
            Vector3 way=target-pos;float want=Mathf.Atan2(way.x,way.z)*Mathf.Rad2Deg;
            float steer=Mathf.Clamp(Mathf.DeltaAngle((float)Get(me,"yaw"),want)/20f,-1f,1f);
            bool turbo=false;
            var state=new GamepadState{rightTrigger=1f,leftStick=new Vector2(steer,0)};
            if(turbo)state=state.WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(pad,state);
            if(!captured&&game.CratesBroken>=10){ScreenCapture.CaptureScreenshot(Output+"continuous-drive.png");captured=true;}
            if(game.CratesSeen>lastSeen){lastSeen=game.CratesSeen;Log("box="+lastSeen+" broken="+game.CratesBroken+" lane="+lane.ToString("F1")+" speed="+speed.ToString("F1"));}
            yield return null;
        }
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        Log("CONTINUOUS DRIVE: "+game.CratesBroken+"/40; elapsed="+(Time.unscaledTime-began).ToString("F1")+"s");
        Check(!game.Active&&RaceEvent.CratesPassed(game.CratesBroken),"Real controller could not complete challenge");
        var talk=train.GetComponent<Talk>();Check(talk.Open,"No success dialogue");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(1.4f);
        Check(Story.Chapter==3&&UnityEngine.Object.FindAnyObjectByType<StoryIntro>(),"Continuous win did not continue to heaven");
        Check(errors==0,"Console errors");Log("PASS uninterrupted R2 and analog steering, without turbo, on the real 40-crate course with no teleports or clock/speed edits");
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

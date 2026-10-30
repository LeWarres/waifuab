using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayNathStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("NathCampaignProbe").AddComponent<NathCampaignProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/nath_test_session/play-check.txt";
    }
}
public class NathCampaignProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/nath_test_session/";
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
    IEnumerator Quiz(bool pass)
    {
        var quiz=UnityEngine.Object.FindAnyObjectByType<Cuestionario>();Check(quiz&&quiz.mode==QuestionnaireMode.Human,"Missing human inspection");
        Call(quiz,"Activate",0);yield return null;yield return null;
        int[] correct=(int[])typeof(Cuestionario).GetField("Correct",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        for(int n=0;n<(pass?5:3);n++)
        {
            int[] deck=(int[])Get(quiz,"deck");int mask=correct[deck[(int)Get(quiz,"question")]],pick=0;
            while((((mask&(1<<pick))!=0)!=pass))pick++;
            Call(quiz,"Activate",pick);yield return null;yield return null;
            if(Get(quiz,"stage").ToString()=="Feedback"){Set(quiz,"feedbackUntil",Time.unscaledTime-1f);yield return null;yield return null;}
        }
        Check(Get(quiz,"stage").ToString()==(pass?"Passed":"Arrested"),"Wrong inspection result");
        ScreenCapture.CaptureScreenshot(Output+(pass?"quiz-pass":"quiz-arrest")+".png");yield return null;
        Call(quiz,"Activate",0);yield return new WaitForSecondsRealtime(1.5f);
    }
    IEnumerator Run()
    {
        foreach(var d in InputSystem.devices)if((d is Gamepad||d is Keyboard||d is Mouse)&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("NathProbePad");InputSystem.onAfterUpdate+=Current;
        Locomotives.Select(1);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(1);Story.Play(0);SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(1);
        var menu=UnityEngine.Object.FindAnyObjectByType<SelectLocomotive>();Set(menu,"selected",1);Call(menu,"Refresh");Set(menu,"asking",true);Call(menu,"Go");yield return new WaitForSecondsRealtime(1.4f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==5&&Story.Hero=="nath","Selector did not open Nath prologue");
        Check(UiKit.Now.main==UiKit.Hero("nath").main,"Wrong Nath palette");yield return Press();Check((int)Get(intro,"visible")>0,"X did not reveal story");
        intro.ShowPage(0,true);ScreenCapture.CaptureScreenshot(Output+"prologue.png");yield return null;
        string[] biomes={"volcano","city","heaven","hell","jungle"};int[] rivals={3,7,5,2,10};
        for(int c=0;c<5;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter "+c);Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.6f);
            if(c==1)
            {
                yield return Quiz(false);intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&Story.Chapter==1,"Arrest lost chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.4f);yield return Quiz(true);Log("PASS human quiz arrest/retry + five correct answers to city");
            }
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c]&&Story.Hero=="nath","Wrong biome/player "+c);
            Set(train,"junctionAsked",true);Set(train,"storyClock",Story.Duration);Set(train,"duelCalled",false);
            var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+10;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Finale talk missing "+c);yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.6f);
            if(c==1)
            {
                var race=train.GetComponent<RaceEvent>();Check(race.Active&&!race.CrateRun,"City did not start normal race");yield return Press();yield return new WaitForSecondsRealtime(.3f);race.TestWin();
                until=Time.unscaledTime+25;while(!UnityEngine.Object.FindAnyObjectByType<StoryIntro>()&&Time.unscaledTime<until)yield return null;
            }
            else
            {
                var enemy=train.GetComponent<RivalEvent>();Check(enemy.Active&&(int)Get(enemy,"rivalIndex")==rivals[c],"Wrong rival "+c);
                ScreenCapture.CaptureScreenshot(Output+"duel-"+c+".png");yield return null;enemy.TestWin();
                until=Time.unscaledTime+6;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"After talk missing");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.3f);
                Check((bool)Get(train,"chapterWon"),"Win menu missing");yield return Press();yield return new WaitForSecondsRealtime(1.5f);
            }
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"No chapter transition "+c);Log("PASS chapter "+c+": "+biomes[c]);
        }
        Check(Story.Over&&intro.pages.Length==5,"Ending missing");intro.ShowPage(4,true);ScreenCapture.CaptureScreenshot(Output+"ending.png");yield return null;
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1);Check(GameObject.Find("Thanks"),"No Thanks");Check(errors==0,"Console errors");Log("PASS ending and Thanks");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

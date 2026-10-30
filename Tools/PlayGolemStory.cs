using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class PlayGolemStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("GolemCampaignProbe").AddComponent<GolemCampaignProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/golem_test_session/play-check.txt";
    }
}
public class GolemCampaignProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/golem_test_session/";
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
    IEnumerator Pick(GolemChallenge game,int index){game.Activate(index);yield return null;yield return null;}
    IEnumerator SolveCircuit(GolemChallenge game)
    {
        yield return new WaitForSecondsRealtime(.35f);yield return Press();Check(Get(game,"stage").ToString()=="Parts","X did not start assembly");
        yield return Pick(game,0);yield return Pick(game,3);
        Check(!((bool[])Get(game,"installed"))[0],"Wrong socket accepted");
        game.Back();Check((int)Get(game,"picked")==-1,"Circle did not cancel part selection");
        int[] sockets={4,5,3};for(int part=0;part<3;part++){yield return Pick(game,part);yield return Pick(game,sockets[part]);}
        Check(Get(game,"stage").ToString()=="Wires","Assembly did not lead to wiring");
        ScreenCapture.CaptureScreenshot(Output+"circuits.png");yield return new WaitForEndOfFrame();
        var masks=(int[])Get(game,"masks");int original=masks[0];yield return Pick(game,0);game.Back();Check(masks[0]==original,"Wire undo failed");
        var solved=GolemChallenge.SolvedBoard();
        foreach(var point in GolemChallenge.Route)
        {
            int tile=point.y*5+point.x;
            for(int n=0;n<4&&masks[tile]!=solved[tile]&&Get(game,"stage").ToString()=="Wires";n++)yield return Pick(game,tile);
        }
        Check(Get(game,"stage").ToString()=="Armed"&&GolemChallenge.Connected(masks),"Real rotated circuit did not connect");
        ScreenCapture.CaptureScreenshot(Output+"core-ready.png");yield return new WaitForEndOfFrame();yield return Pick(game,0);
        Check(!game.Active,"Device activation did not finish");Log("PASS: wrong component rejected, selection cancel, matching parts, wire rotation, undo, connected core activation");
    }
    IEnumerator Moral(GolemChallenge game,int highCount)
    {
        yield return new WaitForSecondsRealtime(.35f);yield return Press();Check(Get(game,"stage").ToString()=="Questions","X did not start moral test");
        ScreenCapture.CaptureScreenshot(Output+"moral-"+highCount+".png");yield return new WaitForEndOfFrame();
        for(int q=0;q<13;q++)
        {
            Check((int)Get(game,"question")==q,"Question skipped/repeated");int option=q<highCount?game.HighOption(q):1-game.HighOption(q);
            yield return Pick(game,option);
            if(q==4)
            {
                int before=(int)Get(game,"high");yield return Press(GamepadButton.East);Check((int)Get(game,"question")==4,"Circle did not review previous answer");
                yield return Pick(game,option);Check((int)Get(game,"high")==before,"Review double-counted morality");
            }
        }
        Check(Get(game,"stage").ToString()=="Result"&&(int)Get(game,"high")==highCount,"Moral tally incorrect");
        yield return Pick(game,0);Check(!game.Active&&SaveGame.GolemHighMorality==(highCount>=7),"Moral branch incorrect");
        Log("PASS: all 13 questions, Circle review, exact tally "+highCount+" and "+(highCount>=7?"high":"low")+" outcome saved");
    }
    IEnumerator Flight()
    {
        var flight=UnityEngine.Object.FindAnyObjectByType<Ascenso>();Check(flight&&Story.AscentPending&&!Story.Descending,"Golem ascent missing");
        Check(((UnityEngine.UI.Text)Get(flight,"dialogue")).text==L10n.T("story.sentinel.ascent.intro"),"BBA text leaked into Golem flight");
        Set(flight,"dialogueAge",1f);yield return Press();Check(Get(flight,"phase").ToString()=="Flying","Flight X failed");
        for(int n=0;n<3;n++){Set(flight,"immunity",0f);Call(flight,"Hit");}
        Check(Get(flight,"phase").ToString()=="Lost","Three-hit flight defeat missing");Set(flight,"dialogueAge",1f);yield return Press();yield return new WaitForSecondsRealtime(1.3f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==5&&Story.AscentPending,"Flight failure did not return to Golem's previous dialogue");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.3f);flight=UnityEngine.Object.FindAnyObjectByType<Ascenso>();Check(flight,"Retry flight missing");
        flight.duration=1.2f;Set(flight,"dialogueAge",1f);Set(flight,"immunity",100f);yield return Press();yield return new WaitForSecondsRealtime(1.5f);
        Check(Get(flight,"phase").ToString()=="Arrived","Ascent did not reach orbit");Set(flight,"dialogueAge",1f);yield return Press();yield return new WaitForSecondsRealtime(1.3f);
        Check(UnityEngine.Object.FindAnyObjectByType<StoryIntro>()&&!Story.AscentPending&&Story.Chapter==1,"Orbit did not continue Golem story");Log("PASS: Golem flight X, three-hit failure/retry narration, orbital arrival and space continuation");
    }
    IEnumerator Run()
    {
        foreach(var d in InputSystem.devices)if(d is Gamepad&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("GolemProbePad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",4);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(4);Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6,"Golem prologue missing");Check(UiKit.Now.main==UiKit.Hero("sentinel").main,"Wrong Golem palette");
        yield return Press();Check((int)Get(intro,"visible")>0,"X did not reveal story");intro.ShowPage(0,true);ScreenCapture.CaptureScreenshot(Output+"prologue.png");yield return new WaitForEndOfFrame();
        string[] biomes={"government","space","thulu","robotheaven"};int[] rivals={3,10};
        for(int c=0;c<4;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.6f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c]&&Locomotives.Hero=="sentinel","Wrong world/player "+c);
            if(c==3){ScreenCapture.CaptureScreenshot(Output+"robot-heaven.png");yield return new WaitForEndOfFrame();}
            Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+8;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Timed finale did not start "+c);yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.8f);
            if(c<2)
            {
                var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[c],"Wrong train duel");rival.TestWin();
            }
            else
            {
                var game=train.GetComponent<GolemChallenge>();Check(game&&game.Active&&train.Racing,"Missing/passthrough challenge");
                if(c==2)yield return SolveCircuit(game);else yield return Moral(game,7);
            }
            until=Time.unscaledTime+5;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Post-finale dialogue missing");yield return CloseTalk(talk);
            if(c<2)train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.4f);intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"No post-chapter scene");
            if(c==0){Check(Story.AscentPending&&intro.pages.Length==5,"Missing pre-flight pages");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.4f);yield return Flight();intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();}
            Log("PASS chapter "+(c+1)+": "+biomes[c]);
        }
        Check(Story.Over&&SaveGame.GolemHighMorality&&intro.pages[0].textKey.Contains(".good."),"High ending missing");intro.ShowPage(5,true);ScreenCapture.CaptureScreenshot(Output+"good-ending.png");yield return new WaitForEndOfFrame();Call(intro,"Finish");yield return new WaitForSecondsRealtime(1);Check(GameObject.Find("Thanks"),"High Thanks missing");
        Story.Play(3);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.7f);var last=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Call(last,"StartFinale");yield return new WaitForSecondsRealtime(.4f);var judge=last.GetComponent<GolemChallenge>();yield return Moral(judge,6);
        var lastTalk=last.GetComponent<Talk>();yield return CloseTalk(lastTalk);yield return new WaitForSecondsRealtime(1.4f);intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&Story.Over&&!SaveGame.GolemHighMorality&&intro.pages[0].textKey.Contains(".bad."),"Low ending missing");intro.ShowPage(5,true);ScreenCapture.CaptureScreenshot(Output+"bad-ending.png");yield return new WaitForEndOfFrame();Call(intro,"Finish");yield return new WaitForSecondsRealtime(1);Check(GameObject.Find("Thanks"),"Low Thanks missing");
        Check(errors==0,"Console errors during campaign");Log("PASS: both endings and Thanks, 7/13 vs 6/13 boundary, saved result");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

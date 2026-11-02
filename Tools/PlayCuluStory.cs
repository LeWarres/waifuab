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
    bool noEvents;
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
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0)
        {
            bool more=false;object value=null;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Log("PASS: frames="+(Time.frameCount-firstFrame)+", consoleErrors="+errors+". Victories accelerated; combat balance not fully tested.");Destroy(gameObject);
    }
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<18&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.3f);}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback stuck");
    }
    void CheckTalk(Talk talk,bool before)
    {
        Story.Duel(before,out var names,out var lines,out var sides);var actual=(string[])Get(talk,"lines");var actualNames=(string[])Get(talk,"names");
        Check(actual.Length==lines.Length,"Wrong dialogue count");for(int n=0;n<lines.Length;n++)Check(actual[n]==lines[n]&&actualNames[n]==names[n],"Wrong text/speaker: "+Story.Chapter+"/"+before+"/"+n);
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
                if(!line.StartsWith("story.culu."))continue;var row=line.Split('\t');string text=table.GetEntry(row[0])?.LocalizedValue;
                Check(text==row[c].Replace("\\n","\n"),"Wrong imported copy: "+header[c]+" "+row[0]);count++;
                if(row[0].EndsWith(".title"))continue;bool dialogue=row[0].Contains(".before.")||row[0].Contains(".after.");Text target=dialogue?talk:story;
                var settings=target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width,0));settings.resizeTextForBestFit=false;settings.fontSize=dialogue?31:25;
                Check(new TextGenerator().GetPreferredHeight(text,settings)/target.pixelsPerUnit<=target.rectTransform.rect.height+1,"Text clips: "+header[c]+" "+row[0]);
            }
        }
        Check(count==236,"Unexpected localized count");Log("PASS 236 localized values and actual narrative/gameplay text heights in four languages");
    }
    IEnumerator Run()
    {
        noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        foreach(var device in InputSystem.devices)if(device is Gamepad&&device.enabled){disabled.Add(device);InputSystem.DisableDevice(device);}
        pad=InputSystem.AddDevice<Gamepad>("CuluProbePad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",6);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(6);Story.Play(0);
        SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6,"Missing Culu prologue");
        Check(UiKit.Now.main==UiKit.Hero("culu").main,"Wrong hero palette");
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;Check((int)Get(intro,"visible")>0,"X does not reveal dialogue");
        var layout=intro.gameObject.AddComponent<Talk>();layout.Show(new[]{L10n.T("hero.character.culu")},new[]{"Test"},new[]{false},null);Canvas.ForceUpdateCanvases();
        ValidateCopy((Text)Get(intro,"narration"),(Text)Get(layout,"what"));yield return CloseTalk(layout);Destroy(layout);
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
            Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+8;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Before dialogue missing");CheckTalk(talk,true);
            yield return new WaitForSecondsRealtime(.35f);Check((int)Get(talk,"at")==0,"Auto dialogue advance");yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.9f);
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
            rival.TestWin();until=Time.unscaledTime+5;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"After dialogue missing");CheckTalk(talk,false);
            yield return CloseTalk(talk);train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==pages[chapter],"Wrong narrative transition "+chapter);
            Check(!Story.InspectionPending&&!Story.AscentPending,"Unexpected minigame");Log("PASS chapter "+(chapter+1)+": "+biomes[chapter]+", rival="+rivals[chapter]);
        }
        Check(Story.Over,"Ending missing");intro.ShowPage(4,true);for(int frame=0;frame<18;frame++)yield return null;
        ScreenCapture.CaptureScreenshot("Tools/culu_revision_ending.png");yield return new WaitForEndOfFrame();yield return null;
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks screen missing");
        Check(Time.frameCount>firstFrame+100&&errors==0,"Console/frame progression");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
        Log("PASS: X, five biomes/finales, machine boarders, three Thulu scenery sizes, narrative transitions and Thanks.");
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;InputSystem.onAfterUpdate-=Current;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);
    }
}

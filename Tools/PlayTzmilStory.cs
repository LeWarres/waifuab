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
public static class PlayTzmilStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("TzmilStoryProbe").AddComponent<TzmilStoryProbe>();UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);
        probe.StartCoroutine(probe.Guarded());return "Campaign test started: Tools/tzmil_test_session/play-check.txt";
    }
}
public class TzmilStoryProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/tzmil_test_session/";
    Gamepad pad;readonly System.Collections.Generic.List<InputDevice> disabled=new();int errors,firstFrame;
    bool noEvents;
    object Get(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
    void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Log(string message)=>System.IO.File.AppendAllText(Output+"play-check.txt",message+"\n");
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("CONSOLE: "+message);}}
    void Current(){if(pad!=null&&pad.added)pad.MakeCurrent();}
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<16&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.3f);}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback did not finish");
    }
    void ValidateCopy(Text story,Text talk)
    {
        var rows=System.IO.File.ReadAllLines("Assets/Localization/strings.tsv");var header=rows[0].Split('\t');int values=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(var row in rows)
            {
                if(!row.StartsWith("story.tzmil."))continue;
                var cells=row.Split('\t');string text=table.GetEntry(cells[0])?.LocalizedValue;
                Check(text==cells[c].Replace("\\n","\n"),"Wrong imported text: "+header[c]+" "+cells[0]);
                bool narrative=cells[0].Contains(".intro.")||cells[0].Contains(".outro.");
                if(narrative||cells[0].Contains(".before.")||cells[0].Contains(".after."))
                {
                    Text target=narrative?story:talk;var settings=target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width,0));
                    settings.resizeTextForBestFit=false;settings.fontSize=narrative?25:31;
                    float height=new TextGenerator().GetPreferredHeight(text,settings)/target.pixelsPerUnit;
                    Check(height<=target.rectTransform.rect.height+1f,"Text clips: "+header[c]+" "+cells[0]);
                }
                values++;
            }
        }
        Log("PASS "+values+" imported Tzmil values and actual UI text heights in four languages");
    }
    public IEnumerator Guarded()
    {
        System.IO.File.WriteAllText(Output+"play-check.txt","RUNNING\n");firstFrame=Time.frameCount;Application.logMessageReceived+=Error;
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0)
        {
            bool more=false;object value=null;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL: "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}
            if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Victories accelerated; combat balance not fully tested.");Destroy(gameObject);
    }
    IEnumerator Run()
    {
        noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        foreach(var device in InputSystem.devices)if(device is Gamepad&&device.enabled){disabled.Add(device);InputSystem.DisableDevice(device);}
        pad=InputSystem.AddDevice<Gamepad>("TzmilProbePad");InputSystem.onAfterUpdate+=Current;
        PlayerPrefs.SetInt("locomotive.selected",3);Locomotives.StoryMode=true;Story.TestChapter=-1;SaveGame.NewStory(3);Story.Play(0);
        SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(1f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==6,"Missing prologue");
        Check(UiKit.Now.main==UiKit.Hero("tzmil").main,"Wrong hero palette");
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;Check((int)Get(intro,"visible")>0,"X did not reveal text");
        var layout=intro.gameObject.AddComponent<Talk>();layout.Show(new[]{L10n.T("hero.character.tzmil")},new[]{"Test"},new[]{false},null);
        Canvas.ForceUpdateCanvases();ValidateCopy((Text)Get(intro,"narration"),(Text)Get(layout,"what"));yield return CloseTalk(layout);UnityEngine.Object.Destroy(layout);
        intro.ShowPage(3,true);yield return new WaitForSecondsRealtime(.4f);
        ScreenCapture.CaptureScreenshot(Output+"guardian.png");yield return new WaitForEndOfFrame();
        var currentArt=((UnityEngine.UI.RawImage)Get(intro,"art")).texture;
        intro.ShowPage(4,true);Check(((UnityEngine.UI.RawImage)Get(intro,"art")).texture==currentArt,"Guardian art unexpectedly changed");
        Check((float)Get(intro,"artElapsed")>=.28f,"Same picture flashed again");
        string[] biomes={"volcano","government","hell","heaven"};int[] rivals={8,4,2,4},pageCounts={5,6,6,7};
        for(int chapter=0;chapter<4;chapter++)
        {
            Check(Story.Chapter==chapter,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.8f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[chapter],"Wrong biome "+chapter);
            Check(train.Player&&Locomotives.Hero=="tzmil","Wrong player");
            Set(train,"storyClock",Story.Seconds);Set(train,"duelCalled",false);
            var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+8f;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;
            Check(talk.Open,"Pre-battle dialogue did not start "+chapter);
            Story.Duel(true,out var names,out var lines,out var speakers);
            Check(((string[])Get(talk,"lines")).Length==lines.Length,"Wrong pre-battle text count");
            var actual=(string[])Get(talk,"lines");var actualNames=(string[])Get(talk,"names");
            for(int n=0;n<lines.Length;n++)Check(actual[n]==lines[n]&&actualNames[n]==names[n],"Wrong pre-battle line/speaker "+chapter+"/"+n);
            if(chapter==3)Check(((string[])Get(talk,"names"))[0]==L10n.T("eyeboss.heaven.name"),"Wrong angel speaker");
            yield return new WaitForSecondsRealtime(.35f);Check((int)Get(talk,"at")==0,"Automatic dialogue advance");
            yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.9f);
            if(chapter==0)
            {
                var holo=train.GetComponent<HoloDuel>();Check(holo.Active,"Holo-Duel not active");
                Check(((UnityEngine.UI.Text)Get(holo,"rivalName")).text==L10n.T("hero.character.xiuhtecuhtli").ToUpperInvariant(),"Wrong holo opponent");
                ScreenCapture.CaptureScreenshot(Output+"r20-holo.png");yield return new WaitForEndOfFrame();holo.TestWin();
            }
            else if(chapter==3)
            {
                var eye=train.GetComponent<EyeBossEvent>();Check(eye.Active,"Heaven finale not active");
                Check(GameObject.Find("HeavenGuardian"),"Wrong boss model: expected celestial angel");
                ScreenCapture.CaptureScreenshot(Output+"angel.png");yield return new WaitForEndOfFrame();eye.TestWin();
            }
            else
            {
                var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[chapter],"Wrong rival "+chapter);
                ScreenCapture.CaptureScreenshot(Output+biomes[chapter]+"-duel.png");yield return new WaitForEndOfFrame();rival.TestWin();
            }
            float postUntil=Time.unscaledTime+5f;while(!talk.Open&&Time.unscaledTime<postUntil)yield return null;
            Check(talk.Open,"Post-battle dialogue missing "+chapter);
            Story.Duel(false,out names,out lines,out speakers);actual=(string[])Get(talk,"lines");actualNames=(string[])Get(talk,"names");
            Check(actual.Length==lines.Length,"Wrong after-battle count");for(int n=0;n<lines.Length;n++)Check(actual[n]==lines[n]&&actualNames[n]==names[n],"Wrong after-battle line/speaker");
            yield return CloseTalk(talk);
            if(chapter==1||chapter==2)train.RaceFinished(true); // bypass only the upgrade-choice screen
            yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==pageCounts[chapter],"Wrong narrative transition "+chapter);
            Check(!Story.InspectionPending&&!Story.AscentPending,"Unexpected minigame");
            Log("PASS chapter "+(chapter+1)+": "+biomes[chapter]+", correct finale, before/after dialogue and next pages");
        }
        Check(Story.Over,"Ending missing");intro.ShowPage(6,true);yield return new WaitForSecondsRealtime(.4f);
        ScreenCapture.CaptureScreenshot(Output+"carito-ending.png");yield return new WaitForEndOfFrame();
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.2f);Check(GameObject.Find("Thanks"),"Thanks screen missing");
        Check(errors==0,"Console errors during campaign");Log("PASS: X, Tzmil palette, same-image continuity, four biome/finale chains and Thanks.");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;InputSystem.onAfterUpdate-=Current;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);
    }
}

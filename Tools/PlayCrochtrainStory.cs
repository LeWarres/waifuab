using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
public static class PlayCrochtrainStory
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("CrochtrainCampaignProbe").AddComponent<CrochtrainCampaignProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started Tools/crochtrain_test_session/play-check.txt";
    }
}
public class CrochtrainCampaignProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Output="Tools/crochtrain_test_session/";
    Gamepad pad;readonly List<InputDevice> disabled=new();int errors,firstFrame;bool noEvents;
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
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
        }
        Log("PASS frames="+(Time.frameCount-firstFrame)+" consoleErrors="+errors+". Biome time and combat victories accelerated; balance not evaluated.");Destroy(gameObject);
    }
    IEnumerator Press()
    {
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
    }
    IEnumerator CloseTalk(Talk talk)
    {
        for(int n=0;n<20&&(talk.Open||talk.Finishing);n++){if(talk.Open)talk.Advance();yield return new WaitForSecondsRealtime(.3f);}
        Check(!talk.Open&&!talk.Finishing,"Dialogue callback stuck");
    }
    void ValidateCopy(Text narration,Text gameplay)
    {
        var source=File.ReadAllLines("Assets/Localization/strings.tsv");var header=source[0].Split('\t');int values=0;var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in source)
            {
                var row=line.Split('\t');if(!row[0].StartsWith("story.crochtrain.")&&row[0]!="hero.character.crochtrain")continue;
                string text=table.GetEntry(row[0])?.LocalizedValue;Check(text==row[c],"Wrong imported text: "+header[c]+" "+row[0]);values++;
                bool story=row[0].Contains(".intro.")||row[0].Contains(".outro."),talk=row[0].Contains(".before.")||row[0].Contains(".after.");if(!story&&!talk)continue;
                Text target=story?narration:gameplay;var settings=target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width,0));settings.resizeTextForBestFit=false;settings.fontSize=story?25:31;
                Check(new TextGenerator().GetPreferredHeight(text,settings)/target.pixelsPerUnit<=target.rectTransform.rect.height+1f,"Clipped text: "+header[c]+" "+row[0]);
            }
        }
        Check(values==204,"Wrong localized count");Log("PASS 204 localized values and actual narrative/gameplay heights in four languages");
    }
    void CheckTalk(Talk talk,bool before)
    {
        Story.Duel(before,out var names,out var lines,out var sides);var actual=(string[])Get(talk,"lines");var actualNames=(string[])Get(talk,"names");
        Check(actual.Length==lines.Length,"Wrong dialogue count");for(int n=0;n<lines.Length;n++)
        {
            Check(actual[n]==lines[n]&&actualNames[n]==names[n],"Wrong text or speaker: "+Story.Chapter+"/"+n);
            if(!sides[n])Check(actualNames[n]==L10n.T("story.crochtrain.thoughts"),"Dorian is speaking aloud");
        }
    }
    IEnumerator Capture(string file)
    {
        for(int frame=0;frame<18;frame++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+file+".png");yield return new WaitForEndOfFrame();yield return null;
    }
    IEnumerator Run()
    {
        noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        foreach(var d in InputSystem.devices)if((d is Gamepad||d is Keyboard||d is Mouse)&&d.enabled){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>("CrochtrainProbePad");InputSystem.onAfterUpdate+=Current;
        Locomotives.Select(11);Locomotives.StoryMode=true;Story.TestChapter=-1;SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(1.2f);
        var menu=UnityEngine.Object.FindAnyObjectByType<SelectLocomotive>();Check(menu,"Selector missing");Set(menu,"selected",11);Call(menu,"Refresh");
        Check((bool)menu.GetType().GetProperty("Available",F).GetValue(menu),"Crochtrain story unavailable");Set(menu,"asking",true);Call(menu,"Go");yield return new WaitForSecondsRealtime(1.5f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==7&&Story.Hero=="crochtrain","Selector did not open Crochtrain prologue");
        Check(SaveGame.HasStory&&SaveGame.Locomotive==11&&SaveGame.Chapter==0,"New campaign not saved");Check(UiKit.Now.main==UiKit.Hero("crochtrain").main,"Wrong story palette");
        yield return Press();Check((int)Get(intro,"visible")>0,"X did not reveal narration");
        var layout=intro.gameObject.AddComponent<Talk>();layout.Show(new[]{L10n.T("story.crochtrain.thoughts")},new[]{"Test"},new[]{false},null);Canvas.ForceUpdateCanvases();
        ValidateCopy((Text)Get(intro,"narration"),(Text)Get(layout,"what"));yield return new WaitForSecondsRealtime(.3f);
        bool portrait=false;foreach(var image in ((Canvas)Get(layout,"canvas")).GetComponentsInChildren<RawImage>(true))if(image.texture==Resources.Load<Texture2D>("Menu/hero_crochtrain_cut"))portrait=true;
        Check(portrait,"Thought label lost Crochtrain portrait");yield return CloseTalk(layout);Destroy(layout);intro.ShowPage(0,true);yield return Capture("prologue");
        yield return Press();Check((int)Get(intro,"page")==1,"X did not advance manually");intro.ShowPage(0,true);
        string[] biomes={"thulu","hell","heaven","ocean"};int[] rivals={9,2,5,7},transitionPages={4,4,4,6};
        for(int c=0;c<4;c++)
        {
            Check(Story.Chapter==c,"Wrong chapter");Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.6f);
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train&&train.BiomeId==biomes[c]&&Player.Orbs,"Wrong biome or orb player: "+c);
            Set(train,"junctionAsked",true);Set(train,"storyClock",Story.Duration);Set(train,"duelCalled",false);var talk=train.GetComponent<Talk>();float until=Time.unscaledTime+10;
            while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"Pre-duel talk missing: "+c);CheckTalk(talk,true);
            yield return new WaitForSecondsRealtime(.35f);Check((int)Get(talk,"at")==0,"Automatic dialogue advance");yield return Capture("talk-"+c);yield return CloseTalk(talk);yield return new WaitForSecondsRealtime(.8f);
            var rival=train.GetComponent<RivalEvent>();Check(rival.Active&&(int)Get(rival,"rivalIndex")==rivals[c],"Wrong final rival: "+c);
            Check(Story.MachineRival==(c==0||c==2),"Wrong machine driver setting");yield return Capture("duel-"+c);rival.TestWin();
            until=Time.unscaledTime+6;while(!talk.Open&&Time.unscaledTime<until)yield return null;Check(talk.Open,"After-duel talk missing: "+c);CheckTalk(talk,false);yield return CloseTalk(talk);
            Check((bool)Get(train,"chapterWon"),"Chapter victory selection missing");train.RaceFinished(true);yield return new WaitForSecondsRealtime(1.5f);
            intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro&&intro.pages.Length==transitionPages[c],"Wrong next narration: "+c);
            Check(SaveGame.Locomotive==11&&SaveGame.Chapter==c+1&&SaveGame.Reached==Mathf.Min(c+1,3),"Campaign progress not saved");Log("PASS chapter "+(c+1)+": "+biomes[c]+", exact thoughts/rival lines and saved progress");
        }
        Check(Story.Over,"Campaign ending missing");intro.ShowPage(0,true);yield return Capture("doris");intro.ShowPage(5,true);yield return Capture("culu-promise");
        Check(((Text)Get(intro,"narration")).text==L10n.T("story.crochtrain.3.outro.6"),"Culu's final promise missing");
        Call(intro,"Finish");yield return new WaitForSecondsRealtime(1.4f);Check(GameObject.Find("Thanks"),"Thanks missing");Check(errors==0,"Console errors");Log("PASS final Doris/Cerebron café, Culu promise and Thanks");
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;InputSystem.onAfterUpdate-=Current;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class PlayGameplayPolish
{
    public static string Run(){if(!Application.isPlaying)throw new Exception("Play Mode required");var p=new GameObject("GameplayPolishProbe").AddComponent<GameplayPolishProbe>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guard());return "Started gameplay polish tests";}
}
public class GameplayPolishProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Dir="Tools/gameplay_polish_review/";int errors;bool oldEvents;
    object Get(object o,string n){var f=o.GetType().GetField(n,F);return f!=null?f.GetValue(o):o.GetType().GetProperty(n,F).GetValue(o);}
    void Set(object o,string n,object v){var f=o.GetType().GetField(n,F);if(f!=null)f.SetValue(o,v);else o.GetType().GetProperty(n,F).SetValue(o,v);}
    object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    void Check(bool yes,string m){if(!yes)throw new Exception(m);}
    void Log(string m)=>File.AppendAllText(Dir+"play.txt",m+"\n");
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;Log("ERROR "+m);}}
    public IEnumerator Guard()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"play.txt","RUNNING\n");oldEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;Application.logMessageReceived+=Error;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object next=null;bool more=false;Exception fault=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}
        Check(errors==0,"Console errors");Log("PASS all gameplay checks; errors="+errors);Destroy(gameObject);
    }
    IEnumerator Load(string scene){SceneManager.LoadScene(scene);yield return new WaitForSecondsRealtime(1.2f);}
    List<Chest.Prize> Prizes(TrainSim t)=>(List<Chest.Prize>)Get(t.GetComponent<Chest>(),"showing");
    void Close(TrainSim t){while(t.ChestOpen)t.CloseChest();}
    IEnumerator Shot(string id){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Dir+id+".png");yield return new WaitForEndOfFrame();yield return null;}
    IEnumerator Run()
    {
        Locomotives.Select(0);Locomotives.StoryMode=true;SaveGame.NewStory(0);Story.Play(0);yield return Load("historiaDialogos");
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();Check(intro,"Missing story");intro.ShowPage(0,false);var narration=(Text)Get(intro,"narration");int font=narration.fontSize;
        Check(!narration.resizeTextForBestFit,"Story still auto-fits partial text");
        for(int i=0;i<80;i++){yield return null;Check(narration.fontSize==font,"Story size changed while typing");}
        Call(intro,"Advance");Check(narration.fontSize==font,"Story size changed on reveal");yield return Shot("story-fixed");Log("PASS story typewriter fixed font="+font);
        yield return Load("sandScenario");var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var combat=train.GetComponent<Combat>();
        var hud=train.GetComponent<Hud>();var timer=(Text)Get(hud,"rideTime");Check(timer.fontSize==42&&!timer.resizeTextForBestFit,"Clock not readable/fixed");
        float clock=train.RideClock;yield return new WaitForSecondsRealtime(.7f);Check(train.RideClock>clock,"Clock not advancing");yield return Shot("hud-clock");Log("PASS central 42px elapsed clock advances");
        var talk=train.GetComponent<Talk>();talk.Show(new[]{L10n.T("hero.character.bba")},new[]{new string('a',200)},new[]{false},null);var what=(Text)Get(talk,"what");font=what.fontSize;
        for(int i=0;i<35;i++){yield return null;Check(what.fontSize==font&&!what.resizeTextForBestFit,"Talk resized while typing");}talk.Advance();yield return new WaitForSecondsRealtime(.4f);Log("PASS gameplay dialogue fixed font");
        // Drive the real mini-boss spawn and on-screen announcement before showing its taunt instruction.
        combat.enabled=false;Set(combat,"miniBossTimer",0f);Call(combat,"Update");var mini=(Combat.Enemy)Get(combat,"miniBoss");Check(mini!=null,"Mini boss failed to spawn");
        Check((float)Get(combat,"miniBossTimer")==55f,"Wrong interval");Check(!(bool)Get(combat,"miniBossHint"),"Taunt appeared on spawn");
        mini.position=train.Focus+Vector3.forward*3f;Call(combat,"Update");Check((bool)Get(combat,"miniBossAnnounced"),"No entry warning on screen");Check(!(bool)Get(combat,"miniBossHint"),"Taunt precedes warning");
        yield return Shot("mini-warning");Set(combat,"miniBossHintAt",Time.unscaledTime-.1f);Call(combat,"Update");Check((bool)Get(combat,"miniBossHint"),"No taunt after warning");
        Log("PASS mini boss warning before taunt; interval 70 to 55 seconds");combat.Despawn(mini);
        // The desert rig uses the same strength/difficulty formula, with exactly one third removed.
        var boss=train.GetComponent<BossEvent>();Check(boss.Begin(),"No desert truck");float expected=boss.bossHealth*(1f+boss.healthPerDifficulty*train.Difficulty)*boss.hard*2f/3f;
        Check(Mathf.Abs((float)Get(boss,"maxHp")-expected)<.1f,"Desert truck health not two thirds");Call(boss,"End",new object[]{null});Log("PASS desert truck health="+expected+" (two thirds)");
        // Hide wagon upgrade candidates for a controlled fair draw: one eligible evolution and one normal passive.
        foreach(var car in (IList)Get(train,"cars")){var gun=(Turret)Get(car,"mount");if(gun)gun.enabled=false;Set(car,"mount",null);}
        var levels=(Dictionary<string,int>)Get(train,"passiveLevels");string partner=train.Player.ammo.evolvesWith;Check(!string.IsNullOrEmpty(partner),"No evolution partner");
        int evolved=0;
        for(int i=0;i<128;i++)
        {
            levels.Clear();train.passiveOrder.Clear();levels[partner]=1;train.passiveOrder.Add(partner);train.Player.level=Weapon.MaxLevel;
            UnityEngine.Random.InitState(i*19+3);Call(train,"AwardChest",false,true);var prizes=Prizes(train);Check(prizes.Count==1||prizes.Count==3||prizes.Count==5,"Shop prize count");if(prizes[0].evolution)evolved++;Close(train);
        }
        Check(evolved>40&&evolved<88,"Shop evolution draw biased: "+evolved);Log("PASS shop evolution equal candidate weight: "+evolved+"/128 first prizes with 2 candidates");
        train.Player.level=Weapon.MaxLevel;levels[partner]=1;train.OpenChest();Check(Prizes(train)[0].evolution,"Boss evolution priority lost");Close(train);
        for(int i=0;i<30;i++)
        {
            train.Player.level=Weapon.MaxLevel;levels[partner]=1;Call(train,"AwardChest",true,false);Check(Prizes(train).Count==1&&!Prizes(train)[0].evolution,"Simple chest evolved/multiple prizes");Close(train);
        }
        train.Player.level=Weapon.Evolved;levels[partner]=TrainSim.PassiveCap(partner);int money=train.money;Call(train,"AwardChest",true,false);Check(Prizes(train).Count==1&&Prizes(train)[0].icon=="money"&&train.money>money,"No capped fallback money");Close(train);
        Set(train,"<Kills>k__BackingField",999);train.Charge();Check((int)Get(train,"pendingKillChests")==1,"1000 kill reward not queued");int count=train.Chests;
        yield return null;yield return null;Check(train.ChestOpen&&train.Chests==count+1&&Prizes(train).Count==1,"1000 kill chest not delivered");Close(train);
        train.Charge();yield return null;Check(!train.ChestOpen,"Duplicate milestone reward");Set(train,"<Kills>k__BackingField",1999);train.Charge();yield return null;yield return null;Check(train.ChestOpen,"2000 kill reward missing");Close(train);
        Log("PASS 1000/2000 milestones once each; single normal prize; capped fallback money; boss evolution priority retained");
        // Shop action: unaffordable disabled, correct debit, modal input block, return to the same shop.
        combat.ClearField();Set(train,"<AtStation>k__BackingField",true);Set(train,"deliveryPending",false);Set(train,"stage",3);train.money=799;Call(train,"BuildMenu");
        int option=-1;for(int i=0;i<train.OptionCount;i++)if(train.OptionIcon(i)=="chest")option=i;Check(option>=0&&!train.OptionEnabled(option),"Chest not disabled at 799");
        train.Player.level=1;levels[partner]=1;train.money=1600;yield return null;Call(train,"BuildMenu");train.Choose(option);Check(train.money==800&&train.ChestOpen,"Wrong shop debit/modal: "+train.money+"/"+train.ChestOpen);train.Choose(option);Check(train.money==800,"Double purchase while chest open");
        Close(train);train.Choose(option);Check(train.money==800,"Closing press bought another chest");yield return null;yield return null;Check(train.MenuKind>=0&&!train.ChestOpen,"Shop did not return");yield return new WaitForSecondsRealtime(.6f);yield return Shot("shop-chest");Log("PASS 800 coin shop price, disabled under price, modal purchase and return without double input");
        // A new scene restores the real wagons for boarding.
        yield return Load("sandScenario");train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var brawl=train.GetComponent<BrawlEvent>();Check(brawl.Begin(),"No boarding");Set(brawl,"startsAt",Time.unscaledTime-.1f);yield return null;yield return null;
        var ground=(Material)Get(brawl,"groundPaint");Vector2 offset=ground.mainTextureOffset;int frame=Time.frameCount;yield return new WaitForSecondsRealtime(.5f);
        Check(brawl.Fighting&&Time.timeScale==0f&&ground.mainTextureOffset!=offset&&Time.frameCount>frame+5,"Boarding background stationary while external time paused");yield return Shot("boarding-motion");brawl.TestWin();Log("PASS boarding background moves with unscaled time while the outside simulation is paused");
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=oldEvents;}
}

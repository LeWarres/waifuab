using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class PlayCombatReview
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("CombatReviewProbe").AddComponent<CombatReviewProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);probe.StartCoroutine(probe.Guard());return "Started combat review";
    }
}
public class CombatReviewProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Dir="Tools/combat_review/";
    int errors;TrainSim train;Combat combat;Hud hud;RivalEvent rival;BrawlEvent brawl;TurnEvent phrase;
    object Get(object o,string n)=>o.GetType().GetField(n,F)?.GetValue(o)??o.GetType().GetProperty(n,F)?.GetValue(o);
    void Set(object o,string n,object v){var f=o.GetType().GetField(n,F);if(f!=null)f.SetValue(o,v);else o.GetType().GetProperty(n,F).SetValue(o,v);}
    object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    void Check(bool yes,string why){if(!yes)throw new Exception(why);}
    void Log(string text)=>File.AppendAllText(Dir+"play.txt",text+"\n");
    void Error(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("ERROR "+text);}}
    public IEnumerator Guard()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"play.txt","RUNNING\n");Application.logMessageReceived+=Error;
        var work=new Stack<IEnumerator>();work.Push(Run());
        while(work.Count>0)
        {
            bool more=false;object next=null;Exception fault=null;
            try{more=work.Peek().MoveNext();if(more)next=work.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}
            if(!more){work.Pop();continue;}if(next is IEnumerator nested){work.Push(nested);continue;}yield return next;
        }
        Log((errors==0?"PASS":"FAIL")+" complete; console errors="+errors);Destroy(gameObject);
    }
    IEnumerator Load()
    {
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.2f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();combat=train.GetComponent<Combat>();combat.enabled=false;
        hud=train.GetComponent<Hud>();rival=train.GetComponent<RivalEvent>();brawl=train.GetComponent<BrawlEvent>();phrase=train.GetComponent<TurnEvent>();
        foreach(var gun in train.GetComponentsInChildren<Turret>())gun.enabled=false;
    }
    IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Dir+name+".png");yield return null;}
    void Close(){while(train.ChestOpen)train.CloseChest();}
    IList Cars=>(IList)Get(train,"cars");
    Dictionary<string,int> Levels=>(Dictionary<string,int>)Get(train,"passiveLevels");
    List<Chest.Prize> Prizes=>(List<Chest.Prize>)Get(train.GetComponent<Chest>(),"showing");
    void Mount(int at,Weapon weapon,int level)
    {
        Call(train,"Mount",at,weapon);var gun=(Turret)Get(Cars[at],"mount");Set(gun,"<Level>k__BackingField",level);gun.enabled=false;
    }
    IEnumerator Run()
    {
        TrainSim.NoEvents=true;Application.runInBackground=true;Locomotives.Select(0);Locomotives.StoryMode=true;SaveGame.NewStory(0);Story.Play(0);
        yield return Load();
        foreach(var weapon in Weapon.Hero)Check(UiKit.Icon(weapon.Icon(Weapon.Evolved)),"Missing evolved hero icon "+weapon.id);
        foreach(var weapon in Weapon.Wagon)Check(UiKit.Icon(weapon.Icon(Weapon.Evolved)),"Missing evolved wagon icon "+weapon.id);
        Check(Weapon.Truck.Icon(Weapon.MaxLevel)=="truck","Truck changed icon");Log("PASS all 19 evolved icon assets load; base/truck icons preserved");
        // Actual regeneration must stop in station menus even though their simulation uses game time.
        Levels["regen"]=5;train.passiveOrder.Add("regen");train.HurtCar(0,.4f);train.Player.Damage(50f);
        Set(train,"<AtStation>k__BackingField",true);Set(train,"deliveryPending",false);Set(train,"stage",0);Time.timeScale=1f;
        float hp=train.Health,hero=train.Player.Health;yield return new WaitForSecondsRealtime(.6f);
        Check(Mathf.Approximately(hp,train.Health)&&Mathf.Approximately(hero,train.Player.Health),"Regeneration during weapon menu");
        Set(train,"stage",1);yield return new WaitForSecondsRealtime(.4f);Check(Mathf.Approximately(hp,train.Health),"Regeneration during passive menu");
        Set(train,"<AtStation>k__BackingField",false);Set(train,"stage",0);yield return new WaitForSecondsRealtime(.6f);
        Check(train.Health>hp&&train.Player.Health>hero,"Regeneration did not resume on road");Log("PASS train/hero regeneration paused in upgrade menus and resumes on road");
        // A shared two-second entrance banner, not a small toast, precedes milestone rewards.
        Set(hud,"toastUntil",0f);Set(hud,"entranceAt",-10f);Set(hud,"ultimateNoticeUntil",0f);Set(train,"<Kills>k__BackingField",2499);train.Charge();
        yield return null;yield return null;Check(!train.ChestOpen,"Milestone opened early");
        Check(((Text)Get(hud,"entranceName")).text==L10n.T("kills.reached",2500).ToUpperInvariant(),"Milestone is not in entrance banner");
        yield return new WaitForSecondsRealtime(.4f);yield return Shot("milestone-banner");
        float until=Time.unscaledTime+4f;while(!train.ChestOpen&&Time.unscaledTime<until)yield return null;Check(train.ChestOpen,"Milestone chest missing");Close();
        train.Toast(L10n.T("miniboss.coming","[□]"));yield return null;
        var toast=(Prompt)Get(hud,"toastLine");Check(toast.GetComponentsInChildren<RawImage>().Length>0,"Square button not drawn");
        yield return Shot("square-button");Log("PASS large animated kill banner precedes chest; taunt toast renders shared QTE square glyph");
        // Three/five reward chests may never select the same weapon or passive twice, nor level then evolve it.
        while(Cars.Count<4)Call(train,"AddCar",Cars.Count,-1,false);
        int multi=0,five=0;
        for(int seed=0;seed<160;seed++)
        {
            Levels.Clear();train.passiveOrder.Clear();foreach(string id in TrainSim.Passives){Levels[id]=1;train.passiveOrder.Add(id);}
            train.Player.level=Weapon.MaxLevel-1;
            Mount(0,Weapon.Wagon[0],Weapon.MaxLevel-1);Mount(1,Weapon.Wagon[0],Weapon.MaxLevel-1);Mount(2,Weapon.Wagon[1],Weapon.MaxLevel);
            var before=new Dictionary<string,int>(Levels);UnityEngine.Random.InitState(seed*13+7);
            Call(train,"AwardChest",false,seed%2==0);if(Prizes.Count>1)multi++;if(Prizes.Count==5)five++;
            Check(train.Player.level<=Weapon.MaxLevel,"Hero leveled and evolved in one chest");
            int same=0;for(int i=0;i<2;i++)same+=((Turret)Get(Cars[i],"mount")).Level-(Weapon.MaxLevel-1);
            Check(same<=1,"Two upgrades of same weapon in one chest");
            foreach(string id in TrainSim.Passives)Check(train.PassiveLevel(id)-before[id]<=1,"Repeated passive "+id);
            var icons=new HashSet<string>();foreach(var p in Prizes)if(p.icon!="money")Check(icons.Add(p.icon.EndsWith("_evo")?p.icon.Substring(0,p.icon.Length-4):p.icon),"Duplicate reward icon "+p.icon);
            Close();
        }
        Check(multi>0&&five>0,"No multi chests exercised");Log("PASS 160 boss/shop chests; "+multi+" multi including "+five+" five-prize; no repeat upgrade or upgrade-plus-evolution");
        train.Player.level=Weapon.Evolved;Mount(0,Weapon.Wagon[0],Weapon.Evolved);Mount(1,Weapon.Wagon[3],Weapon.Evolved);Mount(2,Weapon.Wagon[1],Weapon.Evolved);
        yield return null;yield return null;Check(((RawImage)Get(hud,"ammoIcon")).texture==UiKit.Icon("blaster_evo"),"Hero HUD did not switch evolved icon");yield return Shot("evolved-icons-hud");
        // Long train: preserve space above the rear when the nose projects down-screen.
        yield return Load();while(Cars.Count<11)Call(train,"AddCar",Cars.Count,-1,false);Call(train,"Layout");
        Set(train,"camYaw",-90f);Call(train,"AimCamera");yield return new WaitForSecondsRealtime(4f);
        var camera=Camera.main;float focusY=camera.WorldToViewportPoint(train.Focus).y;
        Check(focusY<.47f,"Downward train did not move forward on screen: "+focusY);
        float tailY=camera.WorldToViewportPoint(train.CarPosition(train.CarCount-1)+Vector3.up*3.5f).y;
        Check(tailY<.82f,"Tail too close to clock: "+tailY);yield return Shot("downward-long-train");
        Log("PASS downward long train focus at viewport y="+focusY.ToString("0.000"));
        // Actual rival encounter, forcing simultaneous deadlines to reproduce the boarding/QTE race.
        Set(train,"camYaw",0f);Call(train,"AimCamera");rival.Begin(2);
        float max=(float)Get(rival,"maxHp"),expected=400f*Balance.Scale*(1f+rival.perDifficulty*train.Difficulty)*Mathf.Max(1f,train.Strength/14f);
        Check(Mathf.Approximately(max,expected),"Rival not at half base health");Check(rival.apart>=20f,"Tracks still too close");
        yield return new WaitForSecondsRealtime(2f);yield return Shot("duel-separated-trains");
        Check(((Transform)Get(rival,"root")).position!=Vector3.zero,"No rival train");
        Set(rival,"startsAt",Time.unscaledTime-1f);Set(rival,"turn",2);Set(rival,"turnLeft",-1f);Set(rival,"phraseIn",0f);TrainSim.NoEvents=false;
        yield return null;yield return null;Check(brawl.Active&&!phrase.Active,"Boarding and QTE overlapped at shared deadline");
        Check(!brawl.Begin()&&brawl.DriverFight,"Rejected second boarding changed the active driver's fight");
        Set(brawl,"approachUntil",Time.unscaledTime-1f);Set(brawl,"startsAt",Time.unscaledTime-1f);
        yield return new WaitForSecondsRealtime(.6f);Check(brawl.Fighting&&!phrase.Active,"QTE appeared inside boarding");
        float entrance=(float)Get(hud,"entranceAt"),beforeHp=(float)Get(rival,"hp");
        Set(hud,"bossUntil",Time.unscaledTime-20f);brawl.TestWin();yield return null;yield return null;
        Check(Mathf.Abs((float)Get(rival,"hp")-(beforeHp-max*rival.driverShare))<.01f,"Boarding win did not remove rival HP");
        Check(!phrase.Active&&(float)Get(rival,"phraseIn")>rival.phraseEvery-1f,"No QTE recovery time");
        Check((float)Get(hud,"entranceAt")==entrance,"Boss introduction replayed after boarding");
        Log("PASS half rival HP, 20m track separation, boarding win removes 12%, no repeated boss intro or immediate QTE");
        phrase.Phrase(4,"TEST",.3f,(_,__)=>{});Set(phrase,"timer",100f);Set(rival,"turn",2);Set(rival,"turnLeft",-1f);
        yield return null;yield return null;Check(!brawl.Active&&phrase.Active,"Boarding interrupted existing QTE");
        Check(!brawl.BeginDriver(2,Vector3.zero,_=>{}),"Manual boarding accepted during QTE");phrase.Drop();Time.timeScale=1f;
        Log("PASS already-active QTE blocks boarding in both scheduler and entry point");
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=false;}
}

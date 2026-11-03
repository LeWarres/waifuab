using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class PlayKillMilestones
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("KillMilestoneProbe").AddComponent<KillMilestoneProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);probe.StartCoroutine(probe.Guard());return "Testing kill milestone notices";
    }
}
public class KillMilestoneProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Dir="Tools/kill_milestone_review/";
    int errors;TrainSim train;Hud hud;
    object Get(object o,string n)=>o.GetType().GetField(n,F)?.GetValue(o)??o.GetType().GetProperty(n,F)?.GetValue(o);
    void Set(object o,string n,object value){var f=o.GetType().GetField(n,F);if(f!=null)f.SetValue(o,value);else o.GetType().GetProperty(n,F).SetValue(o,value);}
    void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    void Log(string text)=>File.AppendAllText(Dir+"play.txt",text+"\n");
    void OnLog(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;Log("ERROR "+m);}}
    public IEnumerator Guard()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"play.txt","RUNNING\n");Application.logMessageReceived+=OnLog;
        var stack=new Stack<IEnumerator>();stack.Push(Test());
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception fault=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
        }
        Log((errors==0?"PASS":"FAIL")+" complete; console errors="+errors);Destroy(gameObject);
    }
    IEnumerator Test()
    {
        TrainSim.NoEvents=true;Application.runInBackground=true;Locomotives.Select(0);Locomotives.StoryMode=true;SaveGame.NewStory(0);Story.Play(0);
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.2f);
        train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();hud=train.GetComponent<Hud>();train.GetComponent<Combat>().enabled=false;
        Set(hud,"toastUntil",0f);Set(hud,"ultimateNoticeUntil",0f);
        // Queue two real milestones while station UI prevents reward presentation.
        Set(train,"<AtStation>k__BackingField",true);Set(train,"deliveryPending",false);Set(train,"stage",3);
        foreach(int before in new[]{999,1999,2498})
        {
            Set(train,"<Kills>k__BackingField",before);train.Charge();
            Check((int)Get(train,"pendingKillChests")==0,"Early chest at "+train.Kills);
        }
        Log("PASS no reward at 1000, 2000 or 2499 kills");
        Set(train,"<Kills>k__BackingField",2499);train.Charge();Set(train,"<Kills>k__BackingField",4999);train.Charge();
        yield return null;Check(!train.ChestOpen&&(int)Get(train,"pendingKillChests")==2,"Rewards opened in station/lost a milestone");
        Set(train,"<AtStation>k__BackingField",false);Set(train,"stage",0);
        yield return Notice(2500,true);while(train.ChestOpen)train.CloseChest();
        yield return Notice(5000,false);while(train.ChestOpen)train.CloseChest();
        train.Charge();yield return new WaitForSecondsRealtime(.3f);Check(!train.ChestOpen&&(int)Get(train,"pendingKillChests")==0,"Duplicate reward at 5001");
        Log("PASS queued milestones announced as 2500 then 5000; no duplicate at 5001");
        Set(train,"<Kills>k__BackingField",7499);train.Charge();yield return WaitNotice();
        typeof(TrainSim).GetMethod("SetPaused",F).Invoke(train,new object[]{true});yield return new WaitForSecondsRealtime(2.2f);Check(!train.ChestOpen,"Reward opened while paused");typeof(TrainSim).GetMethod("SetPaused",F).Invoke(train,new object[]{false});
        yield return Notice(7500,false);while(train.ChestOpen)train.CloseChest();Log("PASS interrupted notice repeats for full duration after pause");
    }
    IEnumerator WaitNotice()
    {
        float timeout=Time.unscaledTime+8f;
        while((float)Get(train,"killChestNoticeUntil")==0f&&Time.unscaledTime<timeout)yield return null;
        Check((float)Get(train,"killChestNoticeUntil")>Time.unscaledTime,"Notice not started");
    }
    IEnumerator Notice(int milestone,bool screenshot)
    {
        yield return WaitNotice();
        string expected=L10n.T("kills.reached",milestone);Check(((Text)Get(hud,"entranceName")).text==expected.ToUpperInvariant(),"Wrong milestone text: "+milestone);
        Check(!train.ChestOpen,"Chest opened before notice");
        float until=(float)Get(train,"killChestNoticeUntil");Check(until-Time.unscaledTime>1.8f,"Notice too short");
        if(screenshot){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Dir+milestone+"-notice.png");}
        while(Time.unscaledTime<until-.1f){Check(!train.ChestOpen,"Chest opened during notice");yield return null;}
        float timeout=until+2f;while(!train.ChestOpen&&Time.unscaledTime<timeout)yield return null;
        Check(train.ChestOpen,"Missing chest after notice");var prizes=(List<Chest.Prize>)Get(train.GetComponent<Chest>(),"showing");
        Check(prizes.Count==1&&!prizes[0].evolution,"Milestone reward changed");Log("PASS "+milestone+" notice precedes single normal reward by 2 seconds");
    }
    void OnDestroy(){Application.logMessageReceived-=OnLog;TrainSim.NoEvents=false;}
}

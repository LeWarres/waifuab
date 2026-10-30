using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class ProbeUltimateBalance
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("UltimateBalanceProbe").AddComponent<UltimateBalanceProbe>();UnityEngine.Object.DontDestroyOnLoad(p);return "Started paired 120-second combat samples; Tools/balance_ultimates.csv";
    }
}
public class UltimateBalanceProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    Keyboard keyboard;int errors;
    void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/balance_test_session/ultimate-errors.txt",message+"\n"+stack+"\n");}}
    IEnumerator Start()
    {
        Directory.CreateDirectory("Tools/balance_test_session");File.WriteAllText("Tools/balance_test_session/ultimate-errors.txt","");
        File.WriteAllText("Tools/balance_ultimates.csv","difficulty,use_ultimate,seconds,train_hp,train_max,hero_hp,peak_enemies,mean_enemies,kills,casts,ultimate_kills,ultimate_damage,total_damage,frames,first_cast_clock\n");
        keyboard=InputSystem.AddDevice<Keyboard>("BalanceKeyboard");Application.logMessageReceived+=Error;Application.runInBackground=true;
        for(int scenario=0;scenario<3;scenario++)for(int special=0;special<2;special++)
        {
            Locomotives.Select(0);Locomotives.StoryMode=false;SceneManager.LoadScene("sandScenario");yield return null;yield return null;
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var combat=Combat.Instance;
            TrainSim.NoEvents=true;Set(train,"remaining",1000000f);Set(train,"stationX",1000000f);
            int difficulty=new[]{0,12,29}[scenario];Set(train,"<Stations>k__BackingField",difficulty);Set(combat,"miniBossTimer",1000000f);Set(combat,"lootTimer",1000000f);
            while(train.CarCount<new[]{2,4,6}[scenario])Call(train,"AddCar",train.CarCount,-1,false);
            int level=new[]{1,3,5}[scenario];string[] guns={"turret","rocket","storm","minigun","sniper","laser"};
            for(int i=0;i<train.CarCount;i++)for(int k=0;k<level;k++)Call(train,"Mount",i,Array.Find(Weapon.Wagon,w=>w.id==guns[i]));train.Player.level=level;
            if(scenario>0)for(int k=0;k<(scenario==1?2:4);k++)foreach(string id in new[]{"damage","armor","regen","fireRate","critChance"})Call(train,"Apply",id);
            var bot=train.gameObject.AddComponent<AutoPlay>();bot.speed=4f;bot.every=100000f;UnityEngine.Random.InitState(741);
            float start=train.RideClock,last=start,exposure=0f,first=-1f,deadline=Time.realtimeSinceStartup+90f;int peak=0,frame=Time.frameCount;
            while(train.RideClock-start<120f&&train.Health>0f&&Time.realtimeSinceStartup<deadline)
            {
                float clock=train.RideClock;exposure+=combat.EnemyCount*(clock-last);last=clock;peak=Math.Max(peak,combat.EnemyCount);
                // Cast straight through the game's own path (minus the cut-in): injected keys are lost while the Editor is unfocused.
                if(special==1&&train.UltimateReady&&!train.Alerting&&!train.AtStation&&!train.Paused&&train.Player.Alive){Set(train,"ultimateCharge",0f);RunStats.Ultimates++;combat.Ultimate(train.Player.Position,train.Player.ammo);}
                if(RunStats.Ultimates>0&&first<0f)first=clock-start;
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());float seconds=train.RideClock-start;
            File.AppendAllText("Tools/balance_ultimates.csv",FormattableString.Invariant($"{difficulty},{special},{seconds:F2},{train.Health:F2},{train.MaxHealth:F2},{train.Player.Health:F2},{peak},{exposure/Mathf.Max(.001f,seconds):F2},{train.Kills},{RunStats.Ultimates},{RunStats.UltimateKills},{RunStats.UltimateDamage:F2},{RunStats.Total:F2},{Time.frameCount-frame},{first:F2}\n"));
            Destroy(bot);yield return null;
        }
        File.WriteAllText("Tools/balance_test_session/ultimate-result.txt",$"DONE consoleErrors={errors}\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);TrainSim.NoEvents=false;Player.Bot=null;Time.timeScale=1f;}
}

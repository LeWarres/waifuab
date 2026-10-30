using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ProbeSurvivorBalance
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("BalanceProbe").AddComponent<SurvivorBalanceProbe>();UnityEngine.Object.DontDestroyOnLoad(probe);return "Started measured combat scenarios; output Tools/balance_probe.txt";
    }
}
public class SurvivorBalanceProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
    int errors;
    void Error(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/balance_probe.txt","ERROR "+text+"\n");}}
    IEnumerator Start()
    {
        File.WriteAllText("Tools/balance_probe.txt","RUNNING real combat, fixed builds, no events/stations/miniboss, seed 741\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        int[] difficulties={0,12,29,29};int[] cars={2,4,6,2};int[] levels={1,3,5,1};
        for(int scenario=0;scenario<4;scenario++)
        {
            Locomotives.Select(0);Locomotives.StoryMode=false;SceneManager.LoadScene("sandScenario");yield return null;yield return null;
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var combat=Combat.Instance;
            TrainSim.NoEvents=true;Set(train,"remaining",1000000f);Set(train,"stationX",1000000f);Set(train,"<Stations>k__BackingField",difficulties[scenario]);
            Set(combat,"miniBossTimer",1000000f);Set(combat,"lootTimer",1000000f);
            while(train.CarCount<cars[scenario])Call(train,"AddCar",train.CarCount,-1,false);
            string[] guns={"turret","rocket","storm","minigun","sniper","laser"};
            for(int i=0;i<train.CarCount;i++)for(int k=0;k<levels[scenario];k++)Call(train,"Mount",i,Array.Find(Weapon.Wagon,w=>w.id==guns[i%guns.Length]));
            train.Player.level=levels[scenario];
            if(scenario>0&&scenario<3)for(int k=0;k<(scenario==1?2:4);k++){Call(train,"Apply","damage");Call(train,"Apply","armor");Call(train,"Apply","regen");Call(train,"Apply","fireRate");Call(train,"Apply","critChance");}
            var bot=train.gameObject.AddComponent<AutoPlay>();bot.speed=4f;bot.every=100000f;
            UnityEngine.Random.InitState(741);int frame=Time.frameCount,peak=0;float start=train.RideClock;float deadline=Time.realtimeSinceStartup+55f;
            while(train.RideClock-start<60f&&train.Health>0f&&Time.realtimeSinceStartup<deadline){peak=Math.Max(peak,combat.EnemyCount);yield return null;}
            File.AppendAllText("Tools/balance_probe.txt",$"difficulty={difficulties[scenario]} seconds={train.RideClock-start:F1} frames={Time.frameCount-frame} train={train.Health:F1}/{train.MaxHealth:F1} hero={train.Player.Health:F1}/{train.Player.maxHealth:F1} peakEnemies={peak} kills={train.Kills}\n");
            Destroy(bot);yield return null;
        }
        Time.timeScale=1f;File.AppendAllText("Tools/balance_probe.txt",$"DONE consoleErrors={errors}\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=false;Player.Bot=null;Time.timeScale=1f;}
}

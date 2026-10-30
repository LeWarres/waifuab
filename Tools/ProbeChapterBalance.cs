using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
// A whole story chapter ridden by AutoPlay (stations, upgrades, wagons bought, repairs), BBA chapter 1, once with the
// ultimate cast whenever ready and once without. One row a minute of chapter clock; stops before the finale.
public static class ProbeChapterBalance
{
    public static int[] Modes={0,1};
    public static int[] Seeds={741};  // one run per seed and mode  // 0: no ultimate, 1: cast whenever ready
    public static bool Legacy;        // first-pass enemy growth: +10% health a level, no speed growth
    public static bool Smart, Events; // AutoPlay's: picks like a player who knows the game; the road's events left on
    public static int Chapter;        // which chapter (0 the first); a later one starts with what the earlier ones hand down, as below
    // Typical hand-downs of a won chapter: the upgrade picked, and the random one with its level (Story.Kept halves it).
    // Overwrites the story save.
    static readonly string[] Keeps = { "wagon:rocket", "critChance", "amount", "wagon:storm" }, Passives = { "luck*4", "damage*3", "critChance*3", "fireRate*3" }; // one build: blaster and storm evolve with critChance, turret with amount, rocket with luck
    public static void Setup()
    {
        SaveGame.NewStory(0);
        for (int c = 0; c < Chapter; c++) SaveGame.Won(c, Keeps[c], Passives[c], 0, 10);
        Story.TestChapter = -1; Story.Play(Chapter);
    }
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        if(UnityEngine.Object.FindAnyObjectByType<ChapterBalanceProbe>())return "Already running"; // two at once put two bots on one train, both picking cards
        var p=new GameObject("ChapterBalanceProbe").AddComponent<ChapterBalanceProbe>();UnityEngine.Object.DontDestroyOnLoad(p);return "Started; output Tools/balance_chapter.csv";
    }
}
public class ChapterBalanceProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const string Out="Tools/balance_chapter.csv";
    int errors;
    void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/balance_test_session/chapter-errors.txt",message+"\n"+stack+"\n");}}
    IEnumerator Start()
    {
        Directory.CreateDirectory("Tools/balance_test_session");File.WriteAllText("Tools/balance_test_session/chapter-errors.txt","");
        File.WriteAllText(Out,"use_ultimate,minute,difficulty,stations,train_hp,train_max,lowest_fill,hero_hp,hero_max,cars,enemies,kills,casts,ultimate_kills,money,arms,upgrades\n");
        Application.logMessageReceived+=Error;Application.runInBackground=true;
        foreach(int seed in ProbeChapterBalance.Seeds)foreach(int special in ProbeChapterBalance.Modes)
        {
            UnityEngine.Random.InitState(seed);File.AppendAllText(Out,"== seed "+seed+"\n");
            Locomotives.Select(0);Locomotives.StoryMode=true;ProbeChapterBalance.Setup();SceneManager.LoadScene("sandScenario");yield return null;yield return null;
            var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var combat=Combat.Instance;Set(train,"junctionAsked",true);
            if(ProbeChapterBalance.Legacy){combat.healthPerLevel=.10f;combat.speedPerLevel=0f;}
            var bot=train.gameObject.AddComponent<AutoPlay>();bot.speed=5f;bot.every=100000f;bot.smart=ProbeChapterBalance.Smart;bot.events=ProbeChapterBalance.Events;
            int minute=0;float lowest=1f,deadline=Time.realtimeSinceStartup+900f;
            while(train.StoryClock<Story.Duration-2f&&train.Health>0f&&!bot.Over&&Time.realtimeSinceStartup<deadline)
            {
                if(train.MaxHealth>0f&&!train.AtStation)lowest=Mathf.Min(lowest,train.Health/train.MaxHealth);
                if(special==1&&train.UltimateReady&&!train.Alerting&&!train.AtStation&&!train.Paused&&train.Player.Alive){Set(train,"ultimateCharge",0f);RunStats.Ultimates++;combat.Ultimate(train.Player.Position,train.Player.ammo);}
                if(train.StoryClock>=minute*60f){Row(special,minute,train,lowest);minute++;}
                yield return null;
            }
            Row(special,-1,train,lowest);
            Destroy(bot);yield return null;
        }
        Time.timeScale=1f;File.AppendAllText(Out,$"DONE consoleErrors={errors}\n");Destroy(gameObject);
    }
    void Row(int special,int minute,TrainSim t,float lowest)
    {
        string arms="",ups="";
        foreach(var a in t.Arsenal())arms+=a.weapon.id+a.level+" ";
        foreach(string id in t.passiveOrder)ups+=id+t.PassiveLevel(id)+" ";
        File.AppendAllText(Out,FormattableString.Invariant($"{special},{(minute<0?"end":minute.ToString())},{t.Difficulty},{t.Stations},{t.Health:F0},{t.MaxHealth:F0},{lowest:F2},{t.Player.Health:F0},{t.Player.maxHealth:F0},{t.CarCount},{Combat.Instance.EnemyCount},{t.Kills},{RunStats.Ultimates},{RunStats.UltimateKills},{t.money},{arms.Trim()},{ups.Trim()}\n"));Hurt();
    }
    static void Hurt(){var hurt=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,float>>(Combat.TrainHurtBy);hurt.Sort((a,b)=>b.Value.CompareTo(a.Value));string h="  hurt:";foreach(var kv in hurt)h+=FormattableString.Invariant($" {kv.Key}={kv.Value:F0}");File.AppendAllText(Out,h+"\n");Combat.TrainHurtBy.Clear();}
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=false;Player.Bot=null;Time.timeScale=1f;}
}

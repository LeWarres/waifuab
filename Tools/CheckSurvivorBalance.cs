using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class CheckSurvivorBalance
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Near(float a,float b,string why)=>Check(Mathf.Abs(a-b)<.02f,$"{why}: {a} != {b}");
    public static string Run()
    {
        Check(Application.isPlaying,"Play Mode required");
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var combat=Combat.Instance;Time.timeScale=0f;combat.ClearField();combat.enabled=false;
        Locomotives.StoryMode=true;Story.Play(0);train.Player.damageBonus=1f;train.Player.armor=0f;train.fireRate=1f;
        ((Dictionary<string,int>)typeof(TrainSim).GetField("passiveLevels",F).GetValue(train)).Clear();train.passiveOrder.Clear();
        for(int n=0;n<5;n++){Call(train,"Apply","damage");Call(train,"Apply","fireRate");Call(train,"Apply","armor");}
        Near(train.Player.damageBonus,1.5f,"Hero might");Set(combat,"heroHit",false);Near((float)typeof(Combat).GetProperty("Power",F).GetValue(combat),1.5f,"Wagon might");
        Near(train.fireRate,1f/.6f,"Five cooldown ranks");Near(train.Player.armor,.5f,"Hero armor");
        Call(train,"Apply","fireRate");Near(train.fireRate,1f/.6f,"Saved duplicate rank cap");
        for(int n=0;n<5;n++)Call(train,"Apply","amount");
        Check(train.PassiveLevel("amount")==2,"Amount rank cap");Near((int)typeof(Combat).GetProperty("Amount",F).GetValue(combat),2f,"Every amount rank adds a shot");
        for(int i=0;i<train.CarCount;i++)train.Repair(100000f,true);
        float hp=train.Health;train.Damage(0,100f);Near(hp-train.Health,75f,"Wagon armor");
        train.Repair(100000f,true);float baseHp=train.CarMaxHealth(0),heroHp=train.Player.maxHealth;
        for(int n=0;n<5;n++)Call(train,"Apply","vitality");
        Near(train.CarMaxHealth(0),baseHp+train.wagonMaxHealth*.5f,"Existing wagon vitality");Near(train.Player.maxHealth,heroHp+250f,"Hero vitality");
        int at=train.CarCount;Call(train,"AddCar",at,-1,false);Near(train.CarMaxHealth(at),train.wagonMaxHealth*1.5f,"New wagon vitality");
        train.Damage(at,100000f);Call(train,"Apply","vitality");Near(train.CarFill(at),0f,"Wreck stays wrecked");
        Set(combat,"level",29);combat.maxOnField=10;Call(combat,"NewRoster");
        for(int n=0;n<30;n++)Call(combat,"SpawnWave",110);
        Check(combat.EnemyCount==10,"Horde capacity");
        var enemies=(List<Combat.Enemy>)typeof(Combat).GetField("enemies",F).GetValue(combat);
        foreach(var e in enemies)Check(e.type.tier==0&&e.type.Fodder,"Horde rolled elite");
        combat.ClearField();Call(combat,"Stampede",90);Check(combat.EnemyCount==10,"Stampede capacity");
        foreach(var e in enemies)Check(e.type.tier==0,"Stampede rolled elite");
        combat.ClearField();for(int n=0;n<40;n++)Call(combat,"SpawnWave",0);Check(combat.EnemyCount<=10,"Normal pack capacity");combat.ClearField();
        int biomeCount=((Array)typeof(TrainSim).GetField("Biomes",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Length;
        for(int b=0;b<biomeCount;b++)foreach(int difficulty in new[]{0,29})
        {
            Set(train,"<Biome>k__BackingField",b);Set(combat,"level",difficulty);Call(combat,"NewRoster");Call(combat,"SpawnWave",110);
            Check(combat.EnemyCount>0&&combat.EnemyCount<=10,"Biome roster/cap "+b+" difficulty "+difficulty);combat.ClearField();
        }
        Set(combat,"level",200);Near(combat.DamageScale,2f*combat.enemyDamage*Balance.Scale,"Contact growth cap");
        Check(Balance.StoryDifficulty(1200,1200,0)==29,"20 minute endpoint");Check(Balance.StoryDifficulty(1800,1800,0)==29,"30 minute endpoint");
        Check(Balance.StoryDifficulty(600,1200,2)==Balance.StoryDifficulty(900,1800,2),"Equal midpoint pressure");
        Directory.CreateDirectory("Tools/balance_test_session");var rows=new List<string>{"weapon,role,damage_1,damage_5,interval_1,interval_5,dps_single_1,dps_single_5,targets_5,directions_5,chain_5"};
        void WeaponRows(Weapon[] weapons,string role){foreach(var w in weapons){for(int n=1;n<5;n++)Check(w.Damage(n+1)>w.Damage(n)&&w.Interval(n+1)<w.Interval(n),"Weapon progression "+w.id);rows.Add(FormattableString.Invariant($"{w.id},{role},{w.Damage(1)},{w.Damage(5)},{w.Interval(1)},{w.Interval(5)},{w.Damage(1)/w.Interval(1)},{w.Damage(5)/w.Interval(5)},{w.Targets(5)},{w.Directions(5)},{w.Chain(5)}"));}}
        WeaponRows(Weapon.Hero,"hero");WeaponRows(Weapon.Wagon,"wagon");File.WriteAllLines("Tools/balance_weapons.csv",rows);
        // Measure the real Update healing budget with combat disabled and several damaged wagons.
        for(int n=0;n<5;n++)Call(train,"Apply","regen");
        for(int i=0;i<train.CarCount-1;i++)train.Damage(i,150f);
        Set(train,"remaining",1000000f);Set(train,"stationX",1000000f);TrainSim.NoEvents=true;
        var probe=train.gameObject.AddComponent<SurvivorRegenCheck>();probe.Begin(train,at);
        return "PASS: shared damage, cooldown and rank cap, armor, existing/new wagon vitality, wrecks, three spawn caps, fodder-only hordes, damage cap, chapter pacing and 20 weapon curves. Started actual Update regeneration check.";
    }
}
public class SurvivorRegenCheck:MonoBehaviour
{
    TrainSim train;int wreck;float hp,start;int frame;
    public void Begin(TrainSim value,int dead){train=value;wreck=dead;hp=train.Health;start=Time.time;frame=Time.frameCount;Time.timeScale=1f;}
    void Update()
    {
        if(Time.frameCount-frame<90)return;
        float dt=Time.time-start,healed=train.Health-hp;bool pass=healed>0f&&Mathf.Abs(healed-dt*5f)<.25f&&train.CarFill(wreck)==0f;
        File.WriteAllText("Tools/balance_test_session/regeneration.txt",$"{(pass?"PASS":"FAIL")} actual Update healing={healed:F3} expected={dt*5f:F3} seconds={dt:F3} frames={Time.frameCount-frame}; wreck fill={train.CarFill(wreck)}\n");
        Time.timeScale=0f;Destroy(this);if(!pass)Debug.LogError("Regeneration budget failed");
    }
}

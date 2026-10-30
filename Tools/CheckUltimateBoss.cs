using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class CheckUltimateBoss
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var combat=Combat.Instance;
        Time.timeScale=1f;combat.ClearField();train.Player.enabled=false;
        foreach(var gun in train.GetComponentsInChildren<Turret>())gun.enabled=false;
        var rival=train.GetComponent<RivalEvent>();rival.Begin(2);rival.enabled=false;
        var body=(Combat.Enemy)Get(rival,"body");float before=(float)Get(rival,"hp"),max=(float)Get(rival,"maxHp");RunStats.Begin();
        combat.Ultimate(train.Player.Position,train.Player.ammo);
        var meteors=(IList)Get(combat,"meteors");
        for(int i=0;i<meteors.Count;i++){object m=meteors[i];m.GetType().GetField("at").SetValue(m,body.position);m.GetType().GetField("landAt").SetValue(m,Time.time-1f);meteors[i]=m;}
        Call(combat,"Meteors");Call(rival,"Update");float accepted=before-(float)Get(rival,"hp");
        Check(Mathf.Abs(accepted-max*.05f)<.1f,"Five hit burst cap");Check(Mathf.Abs(RunStats.UltimateDamage-accepted)<.1f,"Accepted damage accounting");
        string result=FormattableString.Invariant($"Five forced direct meteor hits: combat damage={RunStats.UltimateDamage:F3}; accepted rival HP loss={accepted:F3}/{max:F3} ({100f*accepted/max:F5}%); frame dt={Time.deltaTime:F5}; cap fraction/s={rival.maxLossPerSecond}; this tests the existing rival cap, not targeting accuracy.");
        Set(rival,"hp",max);float total=RunStats.UltimateDamage;Call(combat,"Hurt",body,10f,false,true);
        Check(Mathf.Abs(max-(float)Get(rival,"hp")-5f)<.05f,"Small hit must not become percentage damage");
        Check(Mathf.Abs(RunStats.UltimateDamage-total-5f)<.05f,"Small hit accounting");
        Set(rival,"hp",3f);total=RunStats.UltimateDamage;Call(combat,"Hurt",body,100000f,false,true);Call(combat,"Hurt",body,100000f,false,true);
        Check((float)Get(rival,"hp")==0f&&Mathf.Abs(RunStats.UltimateDamage-total-3f)<.05f,"No boss overkill or negative health");
        Set(rival,"hp",max);body.hp-=100000f;Call(rival,"Update");Check(max-(float)Get(rival,"hp")<=max*rival.weakLossPerSecond*Time.deltaTime+.05f,"Normal gunfire cap changed");
        combat.Despawn(body);var reused=combat.AddProjectile(new Combat.EnemyType{projectile=true},Vector3.zero,100f);Check(reused.onUltimateHit==null,"Pooled proxy retained boss callback");combat.Despawn(reused);
        Set(train,"ultimateCharge",0f);for(int i=0;i<100;i++)train.Charge();Check(train.UltimateFill==0f,"Kills accelerated ultimate cooldown");
        Call(train,"ChargeUltimate",54f);Check(!train.UltimateReady,"Early ultimate ready");Call(train,"ChargeUltimate",1f);Check(train.UltimateReady,"55 second cooldown");
        Set(train,"ultimateCharge",0f);Set(train,"<Paused>k__BackingField",true);Call(train,"ChargeUltimate",55f);Check(!train.UltimateReady,"Ultimate charged while paused");Set(train,"<Paused>k__BackingField",false);
        result+=" PASS burst cap, actual HP accounting, low damage, zero HP, normal fire cap, recycled proxy reset, 55-second time-only cooldown and pause.";
        File.WriteAllText("Tools/balance_test_session/ultimate-boss.txt",result+"\n");return result;
    }
}

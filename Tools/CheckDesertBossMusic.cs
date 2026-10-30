using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CheckDesertBossMusic
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("DesertBossMusicProbe").AddComponent<DesertBossMusicProbe>();UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/desert_boss_music_test.txt";
    }
}
public class DesertBossMusicProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Call(object o,string name,params object[] a)=>o.GetType().GetMethod(name,F).Invoke(o,a);
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/desert_boss_music_test.txt","ERROR "+message+"\n");}}
    AudioSource Track(string name)
    {
        var music=UnityEngine.Object.FindObjectsByType<SceneMusic>();Check(music.Length==1,"Duplicate music owner");var source=music[0].GetComponent<AudioSource>();
        Check(source.clip&&source.clip.name==name&&source.isPlaying&&source.loop,"Wrong or stopped music: "+name);return source;
    }
    IEnumerator Start()
    {
        File.WriteAllText("Tools/desert_boss_music_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Locomotives.Select(0);Locomotives.StoryMode=false;SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(.8f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();TrainSim.NoEvents=true;typeof(TrainSim).GetField("remaining",F).SetValue(train,100000f);
        Track("desierto");var boss=train.GetComponent<BossEvent>();Check(boss.Begin(),"Truck could not start");yield return new WaitForSecondsRealtime(.35f);
        var source=Track("desiertofinalboss");float at=source.time;int frame=Time.frameCount;yield return new WaitForSecondsRealtime(.3f);Check(source.time>at&&Time.frameCount>frame,"Boss track not advancing");
        source.time=source.clip.length-.2f;yield return new WaitForSecondsRealtime(.6f);Check(source.time<2f&&source.isPlaying,"Boss track did not loop");
        Call(boss,"End",(object)null);yield return null;yield return null;Track("desierto");File.AppendAllText("Tools/desert_boss_music_test.txt","PASS real truck Begin/End, boss loop and return to desert\n");
        var rival=train.GetComponent<RivalEvent>();rival.Begin(2);yield return new WaitForSecondsRealtime(.3f);Track("desiertofinalboss");
        var biome=typeof(TrainSim).GetField("<Biome>k__BackingField",F);biome.SetValue(train,1);yield return null;yield return null;Check(!source.clip&&!source.isPlaying,"Desert boss theme leaked into snow");
        biome.SetValue(train,0);yield return null;yield return null;Track("desiertofinalboss");Call(rival,"End");yield return null;yield return null;Track("desierto");
        SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(.8f);Track("selectMenu");
        File.AppendAllText("Tools/desert_boss_music_test.txt",$"PASS real final rival Begin/End, biome guard and selector transition; consoleErrors={errors}\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=false;}
}

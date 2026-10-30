using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CheckCityMusic
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("CityMusicProbe").AddComponent<CityMusicProbe>();UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/city_music_test.txt";
    }
}
public class CityMusicProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Call(object o,string name,params object[] a)=>o.GetType().GetMethod(name,F).Invoke(o,a);
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/city_music_test.txt","ERROR "+message+"\n");}}
    AudioSource Track(string name)
    {
        var music=UnityEngine.Object.FindObjectsByType<SceneMusic>();Check(music.Length==1,"Duplicate music owner");var source=music[0].GetComponent<AudioSource>();
        Check(source.clip&&source.clip.name==name&&source.isPlaying&&source.loop,"Wrong or stopped music: "+name);return source;
    }
    IEnumerator Start()
    {
        File.WriteAllText("Tools/city_music_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Locomotives.Select(0);Locomotives.StoryMode=false;SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(.8f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();TrainSim.NoEvents=true;typeof(TrainSim).GetField("remaining",F).SetValue(train,100000f);
        var biome=typeof(TrainSim).GetField("<Biome>k__BackingField",F);biome.SetValue(train,5);yield return null;yield return null;
        Track("city");var race=train.GetComponent<RaceEvent>();race.Begin(_=>{});yield return new WaitForSecondsRealtime(.5f);
        var source=Track("cityfinalboss");float at=source.time;int frame=Time.frameCount;yield return new WaitForSecondsRealtime(.3f);Check(source.time>at&&Time.frameCount>frame,"Race music not advancing with paused world");
        source.time=source.clip.length-.2f;yield return new WaitForSecondsRealtime(.6f);Check(source.time<2f&&source.isPlaying,"City boss track did not loop");
        race.TestWin();yield return null;yield return null;Track("city");
        source.time=source.clip.length-.2f;yield return new WaitForSecondsRealtime(.6f);Check(source.time<2f&&source.isPlaying,"City track did not loop");
        var rival=train.GetComponent<RivalEvent>();rival.Begin(2);yield return new WaitForSecondsRealtime(.3f);Track("cityfinalboss");
        Call(rival,"End");yield return null;yield return null;Track("city");
        biome.SetValue(train,0);yield return null;yield return null;Track("desierto");
        File.AppendAllText("Tools/city_music_test.txt","PASS real race Begin/TestWin, playback with paused world, both loop wraps, rival Begin/End and desert transition\n");
        SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(.8f);Track("selectMenu");
        File.AppendAllText("Tools/city_music_test.txt",$"PASS real final rival Begin/End, biome guard and selector transition; consoleErrors={errors}\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=false;}
}

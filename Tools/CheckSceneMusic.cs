using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CheckSceneMusic
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("MusicProbe").AddComponent<SceneMusicProbe>();UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/music_test.txt";
    }
}
public class SceneMusicProbe:MonoBehaviour
{
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/music_test.txt","ERROR "+message+"\n"+stack+"\n");}}
    AudioSource Source(string clip)
    {
        var music=UnityEngine.Object.FindObjectsByType<SceneMusic>();Check(music.Length==1,"Duplicate music owner");
        var source=music[0].GetComponent<AudioSource>();Check(source.clip&&source.clip.name==clip&&source.loop&&source.isPlaying,"Wrong/inaudible track "+clip);Check(source.spatialBlend==0f,"Music is spatialized");return source;
    }
    IEnumerator Start()
    {
        File.WriteAllText("Tools/music_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Locomotives.Select(0);Locomotives.StoryMode=false;
        SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(1f);
        var source=Source("selectMenu");Time.timeScale=0f;float at=source.time;yield return new WaitForSecondsRealtime(.35f);Check(source.time>at,"Selection music stopped with paused game time");
        source.time=source.clip.length-.2f;yield return new WaitForSecondsRealtime(.6f);Check(source.isPlaying&&source.time<2f,"Selection loop failed");
        File.AppendAllText("Tools/music_test.txt",$"PASS selector: {source.clip.length:F2}s, loop wrap and playback with Time.timeScale=0\n");
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1f);
        source=Source("desierto");int frame=Time.frameCount;at=source.time;yield return new WaitForSecondsRealtime(.35f);Check(Time.frameCount>frame&&source.time>at,"Desert music/game not advancing");
        var prop=typeof(Sound).GetProperty("Music",BindingFlags.Static|BindingFlags.Public);float old=Sound.Music;
        try{prop.SetValue(null,.2f);typeof(Sound).GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Check(Mathf.Abs(source.volume-.09f)<.001f,"Music setting not routed");}
        finally{prop.SetValue(null,old);typeof(Sound).GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);}
        source.time=source.clip.length-.2f;yield return new WaitForSecondsRealtime(.6f);Check(source.isPlaying&&source.time<2f,"Desert loop failed");
        File.AppendAllText("Tools/music_test.txt",$"PASS desert: {source.clip.length:F2}s, loop wrap, advancing frames and music volume\n");
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(.8f);Source("desierto");
        File.AppendAllText("Tools/music_test.txt","PASS restarting the same gameplay scene\n");
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var field=typeof(TrainSim).GetField("<Biome>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        field.SetValue(train,1);yield return null;yield return null;Check(!source.isPlaying&&!source.clip,"Desert music leaked into snow");
        field.SetValue(train,0);yield return null;yield return null;Source("desierto");
        SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(.8f);Source("selectMenu");
        SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(.8f);Check(!source.isPlaying&&!source.clip,"Selection music leaked into main menu");
        File.AppendAllText("Tools/music_test.txt",$"PASS biome and scene transitions, one persistent source; consoleErrors={errors}\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;Time.timeScale=1f;}
}

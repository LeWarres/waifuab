using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class CheckOniniPresentation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("OniniPresentationProbe").AddComponent<OniniPresentationProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p);return "Started Onini presentation verification";
    }
}
public class OniniPresentationProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText("Tools/onini_presentation_test.txt",m+"\n");}}
    IEnumerator Start()
    {
        File.WriteAllText("Tools/onini_presentation_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Locomotives.Select(2);Locomotives.StoryMode=false;
        SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(.8f);
        var menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();Check(menu,"Main menu missing");
        var portrait=Resources.Load<Texture2D>("Menu/hero_onini_cut");Check(portrait,"Menu portrait missing");
        ((RawImage)typeof(MainMenu).GetField("heroArt",F).GetValue(menu)).texture=portrait;
        ((RawImage)typeof(MainMenu).GetField("heroShade",F).GetValue(menu)).texture=portrait;
        ((Text)typeof(MainMenu).GetField("heroName",F).GetValue(menu)).text="ONINI";UiKit.SetHeroTones("onini");
        yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot("Tools/onini_art_trials/face_fix/menu_after.png");yield return new WaitForSecondsRealtime(.2f);
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(.8f);
        var cut=UnityEngine.Object.FindAnyObjectByType<UltimateCutIn>();Check(cut,"Ultimate presentation missing");
        float original=Time.timeScale;bool done=false;cut.Play(()=>done=true);Check(cut.Active&&Time.timeScale==0f,"Ultimate did not freeze world");
        yield return new WaitForSecondsRealtime(.3f);
        var picture=(RawImage)typeof(UltimateCutIn).GetField("picture",F).GetValue(cut);
        Check(picture.texture&&picture.texture.name=="onini_cut_in","Wrong Ultimate image");
        var streaks=(UltimateStreaks)typeof(UltimateCutIn).GetField("streaks",F).GetValue(cut);
        Check(streaks.powerColor.r>.9f&&streaks.powerColor.g<.3f,"Wrong Onini effect color");
        ScreenCapture.CaptureScreenshot("Tools/onini_art_trials/face_fix/ultimate_ingame.png");
        yield return new WaitForSecondsRealtime(1f);Check(done&&!cut.Active&&Time.timeScale==original,"Ultimate did not finish and restore time");
        Check(errors==0,"Runtime errors during test");File.AppendAllText("Tools/onini_presentation_test.txt","PASS menu portrait, Onini Ultimate texture and red-orange effects; freeze, callback and time restoration; errors=0\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;}
}

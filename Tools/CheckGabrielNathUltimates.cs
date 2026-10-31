using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class CheckGabrielNathUltimates
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("HeroUltimateProbe").AddComponent<HeroUltimateProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/gabriel_nath_ultimates_test.txt";
    }
}
public class HeroUltimateProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText("Tools/gabriel_nath_ultimates_test.txt",m+"\n");}}
    IEnumerator Start()
    {
        File.WriteAllText("Tools/gabriel_nath_ultimates_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Directory.CreateDirectory("Tools/ultimate_art_review");
        Locomotives.StoryMode=false;
        foreach(int index in new[]{5,1})
        {
            Locomotives.Select(index);SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(.8f);
            string hero=Locomotives.Hero;
            var menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();
            var heroArt=(RawImage)typeof(MainMenu).GetField("heroArt",F).GetValue(menu);
            var shuffle=typeof(MainMenu).GetMethod("Shuffle",F);
            var portrait=Resources.Load<Texture2D>("Menu/hero_"+hero+"_cut");Check(portrait,"Menu portrait missing");
            for(int tries=0;heroArt.texture!=portrait&&tries<200;tries++)shuffle.Invoke(menu,null);
            Check(heroArt.texture==portrait,"Menu shuffle never selected "+hero);
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/ultimate_art_review/"+hero+"_menu.png");
            yield return new WaitForSecondsRealtime(.2f);
            var cut=new GameObject("UltimatePresentationTest").AddComponent<UltimateCutIn>();
            float previous=Time.timeScale;bool done=false;int frame=Time.frameCount;
            cut.Play(()=>done=true);Check(cut.Active&&Time.timeScale==0f,"Freeze missing");
            yield return new WaitForSecondsRealtime(.35f);
            var picture=(RawImage)typeof(UltimateCutIn).GetField("picture",F).GetValue(cut);
            Check(Time.frameCount>frame&&picture.texture&&picture.texture.name==hero+"_cut_in","Wrong image or stalled game");
            var streaks=(UltimateStreaks)typeof(UltimateCutIn).GetField("streaks",F).GetValue(cut);
            Color expected=hero=="gabriel"?new Color(1f,.62f,.27f):new Color(.68f,.4f,.92f);
            Check(streaks.powerColor==expected,"Wrong effect color");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/ultimate_art_review/"+hero+"_ingame.png");
            yield return new WaitForSecondsRealtime(1f);
            Check(done&&!cut.Active&&Time.timeScale==previous,"Callback or time restoration missing");
            File.AppendAllText("Tools/gabriel_nath_ultimates_test.txt","PASS "+hero+": menu shuffle portrait, Ultimate image, color, advancing frames, freeze and resume\n");
        }
        Check(errors==0,"Runtime errors");File.AppendAllText("Tools/gabriel_nath_ultimates_test.txt","PASS consoleErrors=0\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;}
}

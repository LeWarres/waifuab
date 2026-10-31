using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class CheckRemainingUltimates
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("RemainingUltimateProbe").AddComponent<RemainingUltimateProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/remaining_ultimates_test.txt";
    }
}
public class RemainingUltimateProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText("Tools/remaining_ultimates_test.txt",m+"\n");}}
    IEnumerator Start()
    {
        File.WriteAllText("Tools/remaining_ultimates_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Directory.CreateDirectory("Tools/ultimate_art_review");Locomotives.StoryMode=false;
        SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(.8f);
        var cut=new GameObject("UltimatePresentationTest").AddComponent<UltimateCutIn>();
        int[] indices={8,9,11,0};string[] heroes={"xiuhtecuhtli","giger","crochtrain","bba"};
        Color[] colors={new(.1f,.92f,.78f),new(.65f,1f,.08f),new(.35f,.95f,.72f),new(.35f,1f,.5f)};
        for(int i=0;i<indices.Length;i++)
        {
            Locomotives.Select(indices[i]);string hero=heroes[i];Time.timeScale=.5f;
            bool done=false;int frame=Time.frameCount;cut.Play(()=>done=true);
            Check(cut.Active&&Time.timeScale==0f,"Freeze missing: "+hero);
            yield return new WaitForSecondsRealtime(.3f);
            var picture=(RawImage)typeof(UltimateCutIn).GetField("picture",F).GetValue(cut);
            Check(Time.frameCount>frame&&picture.texture&&picture.texture.name==hero+"_cut_in","Wrong image or stalled game: "+hero);
            var streaks=(UltimateStreaks)typeof(UltimateCutIn).GetField("streaks",F).GetValue(cut);
            Check(streaks.powerColor==colors[i],"Wrong effect color: "+hero);
            if(i<3){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/ultimate_art_review/"+hero+"_ultimate.png");}
            yield return new WaitForSecondsRealtime(1f);
            Check(done&&!cut.Active&&Time.timeScale==.5f,"Callback/time restoration failed: "+hero);
            File.AppendAllText("Tools/remaining_ultimates_test.txt","PASS "+hero+": correct card and color, moving frames, freeze and resume\n");
        }
        Time.timeScale=1f;Check(errors==0,"Runtime errors");
        File.AppendAllText("Tools/remaining_ultimates_test.txt","PASS shared presenter switches cards; BBA regression; consoleErrors=0\n");Destroy(cut.gameObject);Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;}
}

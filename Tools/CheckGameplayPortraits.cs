using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class CheckGameplayPortraits
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("GameplayPortraitProbe").AddComponent<GameplayPortraitProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/gameplay_portraits_test.txt";
    }
}
public class GameplayPortraitProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    bool noEvents;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText("Tools/gameplay_portraits_test.txt",m+"\n");}}
    RawImage Image(TalkPortraits portraits,string hero) => portraits.GetComponentsInChildren<RawImage>().Single(p=>p.texture&&p.texture.name=="hero_"+hero+"_cut");
    IEnumerator Start()
    {
        File.WriteAllText("Tools/gameplay_portraits_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Directory.CreateDirectory("Tools/gameplay_portraits_review");noEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;
        Locomotives.Select(0);Locomotives.StoryMode=false;SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.2f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train,"Gameplay train missing");
        var talk=train.GetComponent<Talk>();Time.timeScale=0f;bool done=false;
        string[] names={L10n.T("hero.character.bba"),L10n.T("hero.character.bba"),L10n.T("hero.character.onini"),L10n.T("loco.crochet"),L10n.T("race.radio.name"),L10n.T("hero.character.giger")};
        string[] lines={"Mi locomotora esta lista.","No pienso rendirme.","Aun tendremos que pelear.","Yo soy mi propia locomotora.","Atencion a todos los maquinistas.","Mi mundo, mis reglas."};
        train.Say(names,lines,new[]{false,false,true,true,true,false},()=>{done=true;Time.timeScale=1f;});
        var portraits=talk.GetComponentInChildren<TalkPortraits>();
        // Portraits live on Talk's canvas, not under the train itself.
        if(!portraits)portraits=UnityEngine.Object.FindAnyObjectByType<TalkPortraits>();Check(portraits,"Portrait layer missing");
        float start=Image(portraits,"bba").rectTransform.anchoredPosition.x;yield return new WaitForSecondsRealtime(.35f);
        var bba=Image(portraits,"bba");Check(bba.rectTransform.anchoredPosition.x>start&&bba.rectTransform.anchoredPosition.x<0f,"Player did not enter from left");
        float settled=bba.rectTransform.anchoredPosition.x;talk.Advance();yield return null;
        Check(Image(portraits,"bba")==bba&&Mathf.Abs(bba.rectTransform.anchoredPosition.x-settled)<1f,"Same speaker re-entered");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/gameplay_portraits_review/bba_left.png");
        talk.Advance();float entry=Image(portraits,"onini").rectTransform.anchoredPosition.x;yield return new WaitForSecondsRealtime(.35f);
        var oni=Image(portraits,"onini");Check(oni.rectTransform.anchoredPosition.x<entry&&oni.rectTransform.anchoredPosition.x>0f,"Rival did not enter from right");Check(!bba.gameObject.activeSelf,"Previous portrait did not leave");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/gameplay_portraits_review/onini_right.png");
        talk.Advance();yield return new WaitForSecondsRealtime(.35f);var crochet=Image(portraits,"crochtrain");
        Vector2 size=crochet.rectTransform.sizeDelta;Check(Mathf.Abs(size.x/size.y-crochet.texture.width/(float)crochet.texture.height)<.001f,"Machine portrait stretched");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/gameplay_portraits_review/crochtrain_right.png");
        talk.Advance();yield return new WaitForSecondsRealtime(.35f);Check(!portraits.Visible,"Radio borrowed a hero portrait");
        talk.Advance();yield return new WaitForSecondsRealtime(.35f);Image(portraits,"giger");
        Check(talk.Open&&!done&&Time.timeScale==0f,"Dialogue auto-advanced or unpaused");
        talk.Advance();Check(talk.Open&&talk.Finishing&&!done,"Callback ran before portrait exit");
        yield return new WaitForSecondsRealtime(.4f);Check(!talk.Open&&!talk.Finishing&&done&&Time.timeScale==1f,"Exit did not finish and resume");
        Check(errors==0,"Runtime errors");File.AppendAllText("Tools/gameplay_portraits_test.txt","PASS actual TrainSim.Say: left/right entry and exit at timeScale=0; same-speaker continuity; machine aspect; radio hides art; manual advance; completion after exit; consoleErrors=0\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=noEvents;}
}

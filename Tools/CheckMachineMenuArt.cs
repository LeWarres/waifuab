using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class CheckMachineMenuArt
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("MachineMenuProbe").AddComponent<MachineMenuProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p);return "Started Tools/machine_menu_test.txt";
    }
}
public class MachineMenuProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    int errors;
    void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText("Tools/machine_menu_test.txt",m+"\n");}}
    IEnumerator Start()
    {
        File.WriteAllText("Tools/machine_menu_test.txt","RUNNING\n");Application.logMessageReceived+=Error;Application.runInBackground=true;
        Directory.CreateDirectory("Tools/machine_menu_art");SceneManager.LoadScene("MainMenu");yield return new WaitForSecondsRealtime(1f);
        var menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();
        var art=(RawImage)typeof(MainMenu).GetField("heroArt",F).GetValue(menu);
        var shade=(RawImage)typeof(MainMenu).GetField("heroShade",F).GetValue(menu);
        var name=(Text)typeof(MainMenu).GetField("heroName",F).GetValue(menu);
        var shuffle=typeof(MainMenu).GetMethod("Shuffle",F);
        foreach(string hero in new[]{"giger","crochtrain","xiuhtecuhtli","bba"})
        {
            var portrait=Resources.Load<Texture2D>("Menu/hero_"+hero+"_cut");Check(portrait,"Missing "+hero);
            for(int tries=0;art.texture!=portrait&&tries<300;tries++)shuffle.Invoke(menu,null);
            Check(art.texture==portrait&&shade.texture==portrait,"Not included in menu rotation: "+hero);
            string label=L10n.T(hero=="crochtrain"?"loco.crochet":"hero.character."+hero).ToUpperInvariant();
            Check(name.text==label,"Wrong label: "+hero);
            Check(UiKit.Now.main==UiKit.Hero(hero).main,"Wrong colors: "+hero);
            Vector2 size=art.rectTransform.sizeDelta;
            Check(Mathf.Abs(size.x/size.y-portrait.width/(float)portrait.height)<.001f,"Stretched portrait: "+hero);
            int frame=Time.frameCount;yield return new WaitForSecondsRealtime(.4f);Check(Time.frameCount>frame,"Stalled menu");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Tools/machine_menu_art/"+hero+"_menu.png");yield return new WaitForSecondsRealtime(.2f);
            File.AppendAllText("Tools/machine_menu_test.txt","PASS "+hero+": actual menu rotation, name, theme, shadow, preserved aspect and moving frames\n");
        }
        Check(errors==0,"Runtime errors");File.AppendAllText("Tools/machine_menu_test.txt","PASS consoleErrors=0\n");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;}
}

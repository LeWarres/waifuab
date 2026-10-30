using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PreviewGabrielUpgrade
{
    public static string Run()
    {
        var p=new GameObject("GabrielUpgradePreview").AddComponent<GabrielUpgradePreview>();UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Run());return "Checking final upgrade labels";
    }
}
public class GabrielUpgradePreview:MonoBehaviour
{
    public IEnumerator Run()
    {
        Locomotives.Select(5);Locomotives.StoryMode=true;SaveGame.NewStory(5);Story.Play(1);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.6f);
        try
        {
            var t=UnityEngine.Object.FindAnyObjectByType<TrainSim>();if(t.GabrielDraft!=1||Time.timeScale!=0f)throw new Exception("Missing frozen weapon choice");
            var options=(System.Collections.IList)typeof(TrainSim).GetField("options",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(t);
            if(options.Count!=3)throw new Exception("Wrong choice count");
            foreach(var option in options)
            {
                string label=(string)option.GetType().GetField("label").GetValue(option);
                if(label.Contains("Conservar")||label.Contains("gabriel.upgrade.")||label.Contains(@"
"))throw new Exception("Wrong label: "+label);
            }
            ScreenCapture.CaptureScreenshot("Tools/gabriel_test_session/final-upgrade-labels.png");
            File.WriteAllText("Tools/gabriel_test_session/ui-check.txt","PASS: three localized upgrade/equip cards; no Keep wording; frozen game.");
        }
        catch(Exception e){File.WriteAllText("Tools/gabriel_test_session/ui-check.txt","FAIL: "+e);}
        Destroy(gameObject);
    }
}

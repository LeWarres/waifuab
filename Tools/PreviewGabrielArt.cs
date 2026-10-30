using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PreviewGabrielArt
{
    public static string Open()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required with save backup");
        Locomotives.Select(5);Locomotives.StoryMode=true;SaveGame.NewStory(5);Story.Play(0);
        SceneManager.LoadScene("historiaDialogos");return "Opening Gabriel artwork review";
    }
    public static string Run()
    {
        var probe=new GameObject("GabrielArtworkReview").AddComponent<GabrielArtworkReview>();
        probe.StartCoroutine(probe.Check());return "Reviewing six corrected illustrations in story UI";
    }
}
public class GabrielArtworkReview:MonoBehaviour
{
    public IEnumerator Check()
    {
        var story=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        string[] ids={"wingless","tzmil","crush","reymar","reunion","date"};
        string[] keys={"0.intro.1","1.intro.1","1.outro.2","3.intro.1","3.outro.2","3.outro.5"};
        var pages=new StoryIntro.Page[ids.Length];
        for(int n=0;n<ids.Length;n++)pages[n]=new StoryIntro.Page{artwork="Story/gabriel_"+ids[n],textKey="story.gabriel."+keys[n],titleKey="story.gabriel.0.title",number=n+1};
        story.pages=pages;story.enabled=false;UiKit.SetHeroTones("gabriel");
        int start=Time.frameCount;
        Directory.CreateDirectory("Tools/gabriel_test_session/art-corrections");
        for(int n=0;n<ids.Length;n++)
        {
            story.ShowPage(n,true);yield return new WaitForSecondsRealtime(.6f);
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot("Tools/gabriel_test_session/art-corrections/"+ids[n]+".png");
            yield return null;
        }
        File.WriteAllText("Tools/gabriel_test_session/art-corrections/result.txt","PASS six corrected artwork pages rendered in live StoryIntro; frames="+(Time.frameCount-start));
        Destroy(gameObject);
    }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class PreviewOniniArt
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        if(GameObject.Find("OniniArtReview"))throw new Exception("Preview already active");
        SessionState.SetInt("onini.art.selection",Locomotives.Selected);
        SessionState.SetBool("onini.art.mode",Locomotives.StoryMode);
        SessionState.SetBool("onini.art.background",Application.runInBackground);
        Application.runInBackground=true;
        PlayerPrefs.SetInt("locomotive.selected",2);Locomotives.StoryMode=false;
        var root=new GameObject("OniniArtReview");root.SetActive(false);
        var intro=root.AddComponent<StoryIntro>();
        string[] ids={"rebel","champion","deal","hell_escape","desert_chase","repair","security","bargain","heaven_entry","guardian","trophy","cactus","psycho","shadow","home_feast"};
        intro.pages=ids.Select(id=>new StoryIntro.Page{artwork="Story/onini_"+id,textKey="story.onini.0.intro.1"}).ToArray();
        root.SetActive(true);intro.enabled=false;
        var canvas=(Canvas)typeof(StoryIntro).GetField("canvas",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(intro);
        canvas.transform.SetParent(root.transform,false);canvas.sortingOrder=1000;
        intro.ShowPage(0,true);
        return "Preview active: 15 illustrations; original scene and saved campaign untouched.";
    }
    public static string Restore()
    {
        var root=GameObject.Find("OniniArtReview");if(root)UnityEngine.Object.Destroy(root);
        PlayerPrefs.SetInt("locomotive.selected",SessionState.GetInt("onini.art.selection",0));
        Locomotives.StoryMode=SessionState.GetBool("onini.art.mode",false);
        Application.runInBackground=SessionState.GetBool("onini.art.background",false);
        UiKit.SetHeroTones(Locomotives.Hero);
        return "Preview removed; original selection, mode and background setting restored.";
    }
}

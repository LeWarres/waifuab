using System;
using System.Linq;
using UnityEngine;
public static class PreviewCuluArt
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        if(GameObject.Find("CuluArtReview"))throw new Exception("Preview already active");
        PlayerPrefs.SetInt("locomotive.selected",6);Locomotives.StoryMode=false;
        foreach(var train in UnityEngine.Object.FindObjectsByType<TrainSim>())train.enabled=false;
        UiKit.SetHeroTones("culu");
        var root=new GameObject("CuluArtReview");root.SetActive(false);
        var intro=root.AddComponent<StoryIntro>();
        string[] ids={"desert_lost","jungle_r20","bba","onini_ocean","psycho","crochet","sea_home","deal","cactus","father_arrival","baratito"};
        intro.pages=ids.Select(id=>new StoryIntro.Page{artwork="Story/culu_"+id,textKey="story.culu.1.outro.1"}).ToArray();
        root.SetActive(true);intro.enabled=false;
        var canvas=(Canvas)typeof(StoryIntro).GetField("canvas",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(intro);
        canvas.transform.SetParent(root.transform,false);canvas.sortingOrder=1000;
        intro.ShowPage(0,true);
        Time.timeScale=0;
        return "Culu review: 11 corrected illustrations using actual StoryIntro UI.";
    }
}

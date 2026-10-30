using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PreviewCerebronArt
{
    public static string Open()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required, with save backup");
        PlayerPrefs.SetInt("locomotive.selected",7);Locomotives.StoryMode=true;Story.TestChapter=-1;
        SaveGame.NewStory(7);Story.Play(0);SceneManager.LoadScene("historiaDialogos");
        return "Opening actual Cerebron story scene.";
    }
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        if(!intro)throw new Exception("Story scene not ready");
        string[] ids={"origin","doris","carrot","deal","roger","bba","salvage","holo","faceless_escape","portal","crochet","promise","baratito"};
        string[] keys={"0.intro.1","0.intro.3","0.intro.5","0.intro.6","0.outro.1","1.intro.2","1.outro.2","2.intro.2","2.outro.2","2.outro.3","3.intro.2","3.outro.2","3.outro.4"};
        intro.pages=ids.Select((id,n)=>new StoryIntro.Page{artwork="Story/cerebron_"+id,textKey="story.cerebron."+keys[n],titleKey="story.cerebron.0.title",number=1}).ToArray();
        intro.enabled=false;UiKit.SetHeroTones("cerebron");intro.ShowPage(0,true);
        return "Reviewing thirteen Cerebron illustrations in the actual StoryIntro layout, with relevant localized text.";
    }
}

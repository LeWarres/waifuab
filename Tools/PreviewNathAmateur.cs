using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class PreviewNathAmateur
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        Locomotives.Select(1);Locomotives.StoryMode=true;Story.TestChapter=-1;Story.Play(0);
        var story=StoryIntro.Inline(()=>{});story.enabled=false;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var art=(UnityEngine.UI.RawImage)typeof(StoryIntro).GetField("art",flags).GetValue(story);
        string[] ids={"origin","sports","deal","volcano","inspection","launcher","heaven","hell","escape","base","bba","cafe","forest","ending"};
        foreach(var id in ids)
        {
            story.pages=new[]{new StoryIntro.Page{artwork="Story/nath_"+id,textKey="story.nath.0.intro.1",titleKey="story.nath.0.title",number=1}};
            story.ShowPage(0,true);Canvas.ForceUpdateCanvases();
            if(!art.texture||art.texture.name!="nath_"+id)throw new Exception("Wrong texture "+id);
            float expected=(float)art.texture.width/art.texture.height;
            if(Mathf.Abs(art.rectTransform.rect.width/art.rectTransform.rect.height-expected)>.01f)throw new Exception("Aspect ratio "+id);
        }
        story.pages=new[]{new StoryIntro.Page{artwork="Story/nath_origin",textKey="story.nath.0.intro.1",titleKey="story.nath.0.title",number=1}};story.ShowPage(0,true);
        Directory.CreateDirectory("Tools/nath_test_session");ScreenCapture.CaptureScreenshot("Tools/nath_test_session/amateur-preview.png");
        return "PASS: 14 new textures displayed through real StoryIntro in Play Mode; original aspect ratios preserved. Frame="+Time.frameCount;
    }
}

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CheckStoryMotion
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("StoryMotionProbe").AddComponent<StoryMotionProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());
        return "Checking hero palettes, repeated pictures, text completion, picture changes and atlas UVs.";
    }
}
public class StoryMotionProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    void Check(bool yes,string why){if(!yes)throw new Exception(why);}
    void Log(string text)=>System.IO.File.AppendAllText("Temp/story-motion-check.txt",text+"\n");
    int errors;
    void Error(string text,string trace,LogType kind){if(kind==LogType.Error||kind==LogType.Exception){errors++;Log(text);}}
    public IEnumerator Guarded()
    {
        System.IO.File.WriteAllText("Temp/story-motion-check.txt","RUNNING\n");
        Application.logMessageReceived+=Error;var it=Run();
        while(true)
        {
            bool next=false;object current=null;Exception fault=null;
            try{next=it.MoveNext();if(next)current=it.Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}
            if(!next)break;yield return current;
        }
        Log("PASS consoleErrors="+errors);Destroy(gameObject);
    }
    IEnumerator Run()
    {
        int frame=Time.frameCount;
        Locomotives.StoryMode=true;Story.TestChapter=-1;
        foreach(int selected in new[]{0,10,8})
        {
            PlayerPrefs.SetInt("locomotive.selected",selected);Story.Play(0);
            SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.7f);
            var story=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
            Check(story,"No story");var tone=UiKit.Hero(Locomotives.Hero);
            Check(UiKit.Now.main==tone.main&&UiKit.Now.deep==tone.deep,"Wrong hero palette "+Locomotives.Hero);
            foreach(string name in new[]{"skip","next"})
            {
                var button=(Button)Get(story,name);
                Check(button.colors.highlightedColor==tone.main&&button.colors.pressedColor==tone.hot&&button.colors.normalColor==tone.deep,"Wrong button palette");
            }
            var label=(Text)Get(story,"storyName");
            var tag=label.transform.parent.GetComponent<Image>();
            Check(Vector3.Distance(new Vector3(tag.color.r,tag.color.g,tag.color.b),new Vector3(tone.main.r,tone.main.g,tone.main.b))<.01f,"Wrong name tag");
            var art=(RawImage)Get(story,"art");var area=(RectTransform)Get(story,"illustrationArea");
            string key=story.pages[0].textKey;
            story.pages=new[]{
                new StoryIntro.Page{artwork="Story/r20_premium",textKey=key},
                new StoryIntro.Page{artwork="Story/r20_premium",textKey=key},
                new StoryIntro.Page{artwork="Story/r20_sister",textKey=key}
            };
            story.ShowPage(0,true);yield return new WaitForSecondsRealtime(.4f);
            story.ShowPage(1,false);
            Check(Mathf.Approximately((float)Get(story,"artElapsed"),.28f)&&area.localScale==Vector3.one&&art.color==Color.white,"Repeated picture restarted animation");
            yield return new WaitForSecondsRealtime(.1f);
            Check(art.color.a==1f&&area.localScale==Vector3.one&&(int)Get(story,"visible")>0,"Repeated picture flashes while text types");
            story.ShowPage(2,false);
            Check((float)Get(story,"artElapsed")==0&&Mathf.Approximately(area.localScale.x,.97f)&&art.color.a==1f,"New picture transition");
            yield return new WaitForSecondsRealtime(.1f);
            float clock=(float)Get(story,"artElapsed");float size=area.localScale.x;
            Check(clock>0&&clock<.28f&&size>.97f&&size<1f,"Transition does not advance");
            typeof(StoryIntro).GetMethod("Advance",F).Invoke(story,null);
            Check((float)Get(story,"artElapsed")==clock&&area.localScale.x==size,"Completing text jumps picture animation");
            story.ShowPage(1,false);yield return new WaitForSecondsRealtime(.1f);
            clock=(float)Get(story,"artElapsed");size=area.localScale.x;story.ShowPage(0,false);
            Check((float)Get(story,"artElapsed")==clock&&area.localScale.x==size,"Same picture restarts active transition");
            yield return new WaitForSecondsRealtime(.4f);
            story.pages=new[]{new StoryIntro.Page{image=0,textKey=key},new StoryIntro.Page{image=1,textKey=key}};
            story.ShowPage(0,true);yield return new WaitForSecondsRealtime(.4f);
            var texture=art.texture;var uv=art.uvRect;story.ShowPage(1,true);
            Check(art.texture==texture&&art.uvRect!=uv&&(float)Get(story,"artElapsed")==0,"Atlas picture change missed");
            yield return new WaitForSecondsRealtime(.4f);
            Check(art.color.a==1f&&area.localScale==Vector3.one,"Atlas transition does not settle");
            Log("PASS "+Locomotives.Hero+": palette/name tag/buttons; repeated image stable; typing independent; new-image motion; rapid advance; atlas UV change.");
        }
        Check(Time.frameCount>frame+30,"Game not advancing");Check(errors==0,"Console errors");
        Story.Play(0);SceneManager.LoadScene("historiaDialogos");yield return new WaitForSecondsRealtime(.8f);
    }
    void OnDestroy(){Application.logMessageReceived-=Error;}
}

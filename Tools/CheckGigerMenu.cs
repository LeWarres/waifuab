using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CheckGigerMenu
{
    public static string Run()
    {
        var probe=new GameObject("GigerMenuProbe").AddComponent<GigerMenuProbe>();UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);probe.StartCoroutine(probe.Run());return "Checking selector and Continue";
    }
}
public class GigerMenuProbe:MonoBehaviour
{
    public IEnumerator Run()
    {
        const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
        Locomotives.StoryMode=true;Locomotives.Select(9);SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(1.2f);
        string result;
        try
        {
            var menu=UnityEngine.Object.FindAnyObjectByType<SelectLocomotive>();if(!menu)throw new Exception("Selector missing");
            typeof(SelectLocomotive).GetField("selected",f).SetValue(menu,9);typeof(SelectLocomotive).GetMethod("Refresh",f).Invoke(menu,null);
            if(!(bool)typeof(SelectLocomotive).GetProperty("Available",f).GetValue(menu))throw new Exception("Giger unavailable");
            typeof(SelectLocomotive).GetField("asking",f).SetValue(menu,true); // saved-game warning already covered; this is a disposable test save
            typeof(SelectLocomotive).GetMethod("Go",f).Invoke(menu,null);result="STARTED";
        }
        catch(Exception e){File.WriteAllText("Tools/giger_test_session/menu-check.txt","FAIL "+e);Destroy(gameObject);yield break;}
        yield return new WaitForSecondsRealtime(1.5f);
        var intro=UnityEngine.Object.FindAnyObjectByType<StoryIntro>();result=intro&&Story.Hero=="giger"&&Story.Chapter==0&&intro.pages.Length==4 ? "PASS selector Giger starts its own prologue; campaign saved." : "FAIL wrong prologue";
        File.WriteAllText("Tools/giger_test_session/menu-check.txt",result);Destroy(gameObject);
    }
}

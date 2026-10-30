using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class R20PlaySession
{
    public static string Start()
    {
        if(EditorApplication.isPlaying) throw new System.Exception("Expected Edit Mode");
        string save=Path.Combine(Application.persistentDataPath,"save.json");
        Directory.CreateDirectory("Temp");
        SessionState.SetBool("r20.saveExists",File.Exists(save));
        if(File.Exists(save))File.Copy(save,"Temp/r20-original-save.json",true);
        SessionState.SetString("r20.scene",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        SessionState.SetBool("r20.hasSelection",PlayerPrefs.HasKey("locomotive.selected"));
        SessionState.SetInt("r20.selection",Locomotives.Selected);
        SessionState.SetBool("r20.background",Application.runInBackground);
        Application.runInBackground=true;
        EditorApplication.isPlaying=true;
        return "Save and selection backed up; starting Play Mode.";
    }
    public static string Restore()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first");
        string save=Path.Combine(Application.persistentDataPath,"save.json");
        if(SessionState.GetBool("r20.saveExists",false))File.Copy("Temp/r20-original-save.json",save,true);
        else if(File.Exists(save))File.Delete(save);
        typeof(SaveGame).GetField("data",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
        if(SessionState.GetBool("r20.hasSelection",false))PlayerPrefs.SetInt("locomotive.selected",SessionState.GetInt("r20.selection",0));else PlayerPrefs.DeleteKey("locomotive.selected");
        PlayerPrefs.Save();Application.runInBackground=SessionState.GetBool("r20.background",false);
        var path=SessionState.GetString("r20.scene","");
        if(path!="")EditorSceneManager.OpenScene(path);
        return "Original save, selected locomotive, background setting and scene restored.";
    }
}

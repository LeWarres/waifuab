using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class TzmilPlaySession
{
    const string Backup="Tools/tzmil_test_session/original.json";
    [Serializable] class Snapshot { public bool saveExists,hasSelection,background,storyMode;public int selection;public string save,scene; }
    public static string CaptureAndStop()
    {
        if(File.Exists(Backup))throw new Exception("Existing backup must be restored before another capture");
        var file=Path.Combine(Application.persistentDataPath,"save.json");
        var s=new Snapshot{saveExists=File.Exists(file),save=File.Exists(file)?File.ReadAllText(file):null,hasSelection=PlayerPrefs.HasKey("locomotive.selected"),selection=Locomotives.Selected,background=Application.runInBackground,storyMode=Locomotives.StoryMode,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
        Directory.CreateDirectory(Path.GetDirectoryName(Backup));File.WriteAllText(Backup,JsonUtility.ToJson(s,true));
        EditorApplication.isPlaying=false;return "Durable save/selection backup created. Stopping authorized Play session.";
    }
    public static string Verify()
    {
        var files=Directory.GetFiles("Tools/tzmil_test_session","original.json.restored-*");Array.Sort(files);
        if(files.Length==0)throw new Exception("Restored backup missing");
        var s=JsonUtility.FromJson<Snapshot>(File.ReadAllText(files[files.Length-1]));
        var file=Path.Combine(Application.persistentDataPath,"save.json");
        bool same=s.saveExists?File.Exists(file)&&File.ReadAllText(file)==s.save:!File.Exists(file);
        if(!same||Locomotives.Selected!=s.selection)throw new Exception("Original state mismatch");
        return "PASS: original save identical to backup; selected locomotive restored; scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"; playing="+Application.isPlaying;
    }
    public static string Restore()
    {
        if(Application.isPlaying)throw new Exception("Stop test first");
        if(!File.Exists(Backup))throw new Exception("Backup absent; refusing to touch save");
        var s=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Backup));
        var file=Path.Combine(Application.persistentDataPath,"save.json");
        if(s.saveExists)File.WriteAllText(file,s.save);else if(File.Exists(file))File.Delete(file);
        typeof(SaveGame).GetField("data",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
        if(s.hasSelection)PlayerPrefs.SetInt("locomotive.selected",s.selection);else PlayerPrefs.DeleteKey("locomotive.selected");
        PlayerPrefs.Save();Application.runInBackground=s.background;Locomotives.StoryMode=s.storyMode;
        if(!string.IsNullOrEmpty(s.scene))EditorSceneManager.OpenScene(s.scene);
        File.Move(Backup,Backup+".restored-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        return "Original save, locomotive selection, background flag and scene restored.";
    }
}

using System;
using System.IO;
using UnityEngine;
public static class InspectUltimateSession
{
    [Serializable] class Snapshot { public bool saveExists,hasSelection,background,storyMode; public int selection; public string save,scene; }
    public static string Run()
    {
        var files=Directory.GetFiles("Tools/tzmil_test_session","original.json.restored-*");Array.Sort(files);
        string latest=files[files.Length-1];var s=JsonUtility.FromJson<Snapshot>(File.ReadAllText(latest));
        string path=Path.Combine(Application.persistentDataPath,"save.json");
        bool same=s.saveExists?File.Exists(path)&&File.ReadAllText(path)==s.save:!File.Exists(path);
        return $"snapshot={latest}; saveIdentical={same}; originalSelection={s.selection}; currentSelection={Locomotives.Selected}; scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}; playing={Application.isPlaying}";
    }
}

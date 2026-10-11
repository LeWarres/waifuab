using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ApplySelectionBackdrop
{
    public static string Run()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.path!="Assets/Scenes/selectHero.unity") {
            if(active.isDirty) throw new System.InvalidOperationException("Save the current scene before switching.");
            EditorSceneManager.OpenScene("Assets/Scenes/selectHero.unity");
        }
        var menu=Object.FindAnyObjectByType<SelectLocomotive>();
        if(!menu) throw new System.InvalidOperationException("Open selectHero first.");
        menu.BuildBackdrop();
        Canvas.ForceUpdateCanvases();
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        return "Selection backdrop saved in "+menu.gameObject.scene.path;
    }
}

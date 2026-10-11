using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ApplyMenuHighlight
{
    public static string Run()
    {
        var menu=Object.FindAnyObjectByType<MainMenu>();
        if(!menu) return "The highlight correction is compiled; it applies when Main Menu opens.";
        menu.RefreshUI();
        if(!EditorApplication.isPlaying) {
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            EditorSceneManager.SaveScene(menu.gameObject.scene);
        }
        return "Blue selection highlight applied.";
    }
}

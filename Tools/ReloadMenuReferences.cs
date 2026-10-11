using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ReloadMenuReferences
{
    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if(EditorApplication.isPlaying) return "References repaired on disk; reopen the menu to use them.";
        var scene=SceneManager.GetActiveScene();
        if(scene.isDirty) return "References repaired on disk; current unsaved scene preserved.";
        if(scene.path=="Assets/Scenes/MainMenu.unity" || scene.path=="Assets/Scenes/selectHero.unity")
            EditorSceneManager.OpenScene(scene.path);
        return "Persistent menu references loaded.";
    }
}

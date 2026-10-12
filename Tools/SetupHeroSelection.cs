using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupHeroSelection
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Stop Play mode before editing the selector.");
        LocalizationImport.Run();
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/selectHero.unity");
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/selectHero.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SelectLocomotive>(true)).First();
        var otherCameras = Object.FindObjectsByType<Camera>().Where(c => c.enabled && c.gameObject.scene != scene).ToArray();
        foreach (var camera in otherCameras) camera.enabled = false;
        try
        {
            menu.BuildUI();
            menu.BuildHeroPreview();
            menu.ComposePreview();
            menu.SendMessage("Refresh");
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Hero preview saved in selectHero; BBA linked to the blue train.";
        }
        finally
        {
            foreach (var camera in otherCameras) camera.enabled = true;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}

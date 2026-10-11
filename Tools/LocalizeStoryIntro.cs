using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class LocalizeStoryIntro
{
    public static string Run()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before saving localization.");
        LocalizationImport.Run();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Additive);
        var menu=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MainMenu>(true)).First();
        var serialized=new SerializedObject(menu);
        if(!serialized.FindProperty("operationsLabel").objectReferenceValue) {
            var canvas=(Canvas)serialized.FindProperty("canvas").objectReferenceValue;
            serialized.FindProperty("operationsLabel").objectReferenceValue=canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t=>t.text.Contains("RAILWAY OPERATIONS"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        menu.RefreshUI();EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets();
        return "Intro and menu texts localized in es, en, pt, ja; gamepad Start skips to sandScenario.";
    }
}

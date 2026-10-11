using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class StyleMainMenu
{
    public static string Run()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.path!="Assets/Scenes/MainMenu.unity") {
            if(active.isDirty) throw new System.InvalidOperationException("Save the current scene first.");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        var menu=Object.FindAnyObjectByType<MainMenu>();
        var existing=GameObject.Find("MainMenuCanvas");
        if(!existing) menu.BuildUI();
        else {
            existing.GetComponentInChildren<SelectionBackdrop>().color=new Color(1,1,1,.12f);
            if(!existing.transform.Find("Footer")) {
                var footer=UiKit.Group(existing.transform,new Vector2(.5f,0),Vector2.zero);footer.name="Footer";
                UiKit.Panel(footer,new Vector2(0,33),new Vector2(2400,66),UiKit.Navy,UiKit.Navy,0);
            }
            var hint=(UnityEngine.UI.Text)new SerializedObject(menu).FindProperty("hint").objectReferenceValue;
            existing.transform.Find("Footer").SetSiblingIndex(hint.transform.GetSiblingIndex());
            menu.RefreshUI();
        }
        Canvas.ForceUpdateCanvases();
        EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        return "Main Menu styled and saved.";
    }
}

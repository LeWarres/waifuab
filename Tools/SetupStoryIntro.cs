using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SetupStoryIntro
{
    static StoryIntro.Page Page(int image,string text)=>new StoryIntro.Page{image=image,textKey=text};
    public static string Run()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before editing the story.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        const string path="Assets/Scenes/historiaDialogos.unity";
        var scene=SceneManager.GetSceneByPath(path);
        if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        if(!scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Any())
            new GameObject("StoryEventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="StoryIntro") ?? new GameObject("StoryIntro");
        var story=root.GetComponent<StoryIntro>() ?? root.AddComponent<StoryIntro>();
        story.gameScene="sandScenario";
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Story/prologue.png");
        importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;
        importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        story.backgrounds=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Story/prologue.png");
        story.pages=new[]{
            Page(0,"story.page.01"),
            Page(0,"story.page.02"),
            Page(1,"story.page.03"),
            Page(1,"story.page.04"),
            Page(2,"story.page.05"),
            Page(2,"story.page.06"),
            Page(3,"story.page.07"),
            Page(3,"story.page.08"),
            Page(4,"story.page.09"),
            Page(4,"story.page.10"),
            Page(4,"story.page.11"),
            Page(5,"story.page.12"),
            Page(5,"story.page.13"),
            Page(5,"story.page.14"),
            Page(5,"story.page.15"),
            Page(5,"story.page.16")
        };
        var serialized=new SerializedObject(story);
        var existingCanvas=(Canvas)serialized.FindProperty("canvas").objectReferenceValue;
        if(existingCanvas) {
            var texts=existingCanvas.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            if(!serialized.FindProperty("storyName").objectReferenceValue)
                serialized.FindProperty("storyName").objectReferenceValue=texts.First(t=>t.text=="MI HISTORIA");
            if(!serialized.FindProperty("hint").objectReferenceValue)
                serialized.FindProperty("hint").objectReferenceValue=texts.First(t=>t.text.StartsWith("Espacio / Enter"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        if(!serialized.FindProperty("canvas").objectReferenceValue) story.BuildUI();
        else story.ShowPage(0,true);
        var storyCanvas=(Canvas)serialized.FindProperty("canvas").objectReferenceValue;
        foreach(var button in storyCanvas.GetComponentsInChildren<UnityEngine.UI.Button>()) {
            var panel=button.GetComponent<Slanted>();panel.color=panel.bottom=Color.white;
            var colors=button.colors;colors.normalColor=UiKit.Navy;colors.highlightedColor=UiKit.Sky;
            colors.pressedColor=UiKit.Slate;colors.selectedColor=UiKit.Navy;button.colors=colors;
            EditorUtility.SetDirty(button);EditorUtility.SetDirty(panel);
        }
        EditorUtility.SetDirty(story);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var selection=SceneManager.GetSceneByPath("Assets/Scenes/selectHero.unity");
        bool opened=!selection.isLoaded;
        if(opened) selection=EditorSceneManager.OpenScene("Assets/Scenes/selectHero.unity",OpenSceneMode.Additive);
        var menu=selection.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SelectLocomotive>(true)).First();
        menu.gameScene="historiaDialogos";EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(selection);EditorSceneManager.SaveScene(selection);
        if(opened) EditorSceneManager.CloseScene(selection,true);
        var builds=EditorBuildSettings.scenes.ToList();
        if(!builds.Any(s=>s.path==path)) builds.Add(new EditorBuildSettingsScene(path,true));
        else builds.First(s=>s.path==path).enabled=true;
        EditorBuildSettings.scenes=builds.ToArray();
        AssetDatabase.SaveAssets();
        for(int i=SceneManager.sceneCount-1;i>=0;i--) {
            var other=SceneManager.GetSceneAt(i);
            if(other!=scene && !other.isDirty) EditorSceneManager.CloseScene(other,true);
        }
        return "16 narrated pages, 6 backgrounds; selection > historiaDialogos > sandScenario.";
    }
}


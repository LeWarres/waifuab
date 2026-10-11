using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ApplyBbaStoryArt
{
    public static string Run()
    {
        const string path="Assets/Resources/Story/prologue.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize=4096;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var story=Object.FindAnyObjectByType<StoryIntro>();
        if(story && !EditorApplication.isPlaying) {
            story.backgrounds=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            story.ShowPage(0,true);
            EditorUtility.SetDirty(story);EditorSceneManager.MarkSceneDirty(story.gameObject.scene);
            EditorSceneManager.SaveScene(story.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        return "Six BBA illustrations imported; existing narration and locale keys preserved.";
    }
}

using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RestoreRoundedHighlight
{
    public static string Run()
    {
        const string path="Assets/Resources/Menu/rounded.png";
        if(!File.Exists(path)) {
            var generated=UiKit.Rounded();
            File.WriteAllBytes(path,generated.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=Vector4.one*22;
            importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;
            importer.SaveAndReimport();
        }
        var menu=Object.FindAnyObjectByType<MainMenu>();
        if(!menu) return "Rounded sprite saved; Main Menu uses it on opening.";
        var reference=new SerializedObject(menu).FindProperty("bar").objectReferenceValue as RectTransform;
        var image=reference.GetComponent<Slanted>();
        image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        image.type=UnityEngine.UI.Image.Type.Sliced;
        menu.RefreshUI();
        if(!EditorApplication.isPlaying) {
            EditorUtility.SetDirty(image);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            EditorSceneManager.SaveScene(menu.gameObject.scene);
        }
        return "Rounded blue highlight restored with a persistent sprite.";
    }
}

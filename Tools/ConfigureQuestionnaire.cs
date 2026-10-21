using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureQuestionnaire
{
    public static string Run()
    {
        LocalizationImport.Run();
        foreach (string name in new[] { "window", "half", "closed", "alien_matte", "alien_half", "alien_closed" })
        {
            string asset = "Assets/Resources/Questionnaire/officer_" + name + ".png";
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
            var texture = (TextureImporter)AssetImporter.GetAtPath(asset);
            texture.mipmapEnabled = false;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.maxTextureSize = 4096;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.SaveAndReimport();
        }
        const string path = "Assets/Scenes/cuestionario.unity";
        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            Cuestionario quiz = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                quiz = root.GetComponentInChildren<Cuestionario>(true);
                if (quiz) break;
            }
            if (!quiz) quiz = new GameObject("Cuestionario").AddComponent<Cuestionario>();
            quiz.BuildUI();
            var blink = quiz.GetComponent<OfficerBlink>() ?? quiz.gameObject.AddComponent<OfficerBlink>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
                    if (image.gameObject.name == "OficialEnVentanilla") blink.portrait = image;
            EditorUtility.SetDirty(blink);
            EditorUtility.SetDirty(quiz);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Questionnaire installed: human/alien modes, 50 questions each, 5 per attempt, arrest after 3 mistakes, 4 locales.";
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}

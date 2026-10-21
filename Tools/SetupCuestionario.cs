using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public static class SetupCuestionario
{
    public static string Run()
    {
        const string scenePath = "Assets/Scenes/cuestionario.unity";
        const string artPath = "Assets/Resources/Questionnaire/officer_window.png";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath))
            throw new System.InvalidOperationException("cuestionario already exists; edit it instead of recreating it.");
        AssetDatabase.ImportAsset(artPath);
        var importer = AssetImporter.GetAtPath(artPath) as TextureImporter;
        if (!importer) throw new System.IO.FileNotFoundException(artPath);
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.10f, 0.19f);
            var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hd.backgroundColorHDR = camera.backgroundColor;
            var canvas = new GameObject("CuestionarioCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var art = new GameObject("OficialEnVentanilla", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter)).GetComponent<UnityEngine.UI.RawImage>();
            art.transform.SetParent(canvas.transform, false);
            art.texture = texture;
            art.raycastTarget = false;
            art.rectTransform.anchorMin = art.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            art.rectTransform.anchoredPosition = Vector2.zero;
            var fit = art.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = (float)texture.width / texture.height;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new System.IO.IOException("Could not save cuestionario.");
            if (!EditorBuildSettings.scenes.Any(entry => entry.path == scenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(scenePath, true) }).ToArray();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        return scenePath;
    }
}

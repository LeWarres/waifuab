using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public static class SetupAscenso
{
    public static string Run()
    {
        const string path = "Assets/Scenes/ascenso.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) return "ascenso already exists.";
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -30f);
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .46f, .79f);
            var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hd.backgroundColorHDR = camera.backgroundColor;
            var light = new GameObject("Sun", typeof(Light), typeof(HDAdditionalLightData)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .96f, .89f);
            light.intensity = 100000f;
            light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            new GameObject("Ascenso").AddComponent<Ascenso>();
            EditorSceneManager.SaveScene(scene, path);
            if (!EditorBuildSettings.scenes.Any(s => s.path == path))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
            return LocalizationImport.Run() + " Created " + path;
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}

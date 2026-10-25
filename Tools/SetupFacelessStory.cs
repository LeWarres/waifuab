using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public static class SetupFacelessStory
{
    public static string Run()
    {
        foreach (string file in System.IO.Directory.GetFiles("Assets/Resources/Story", "faceless_*.png"))
        {
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
            var t = (TextureImporter)AssetImporter.GetAtPath(file);
            t.textureType = TextureImporterType.Default; t.sRGBTexture = true; t.mipmapEnabled = false;
            t.npotScale = TextureImporterNPOTScale.None; t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear; t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.CompressedHQ; t.SaveAndReimport();
        }
        const string path = "Assets/Scenes/bosque.unity";
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(HDAdditionalCameraData)).GetComponent<Camera>();
                camera.tag = "MainCamera"; camera.nearClipPlane = .1f;
                var hd = camera.GetComponent<HDAdditionalCameraData>();
                hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                hd.backgroundColorHDR = new Color(.05f, .15f, .12f);
                var light = new GameObject("ForestSun", typeof(Light), typeof(HDAdditionalLightData)).GetComponent<Light>();
                light.type = LightType.Directional; light.color = new Color(1f, .93f, .8f); light.intensity = 100000f;
                light.transform.rotation = Quaternion.Euler(48f, -35f, 0f); light.shadows = LightShadows.Soft;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var exposure = profile.Add<Exposure>(true); exposure.mode.value = ExposureMode.Fixed; exposure.fixedExposure.value = 13.7f;
                profile.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
                var grade = profile.Add<ColorAdjustments>(true); grade.saturation.value = -10f; grade.contrast.value = 6f;
                const string profilePath = "Assets/Settings/ForestChaseVolume.asset";
                AssetDatabase.CreateAsset(profile, profilePath);
                var volume = new GameObject("ForestLook").AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
                new GameObject("ForestChase").AddComponent<ForestChase>();
                EditorSceneManager.SaveScene(scene, path);
                if (!EditorBuildSettings.scenes.Any(s => s.path == path))
                    EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        return LocalizationImport.Run() + " Imported 18 Faceless illustrations; bosque registered.";
    }
}

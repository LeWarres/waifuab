using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BakeAscentArt
{
    public static string Run()
    {
        const string materialFolder = "Assets/Art/Ascent/Materials";
        const string prefabFolder = "Assets/Resources/Ascent/Prefabs";
        Directory.CreateDirectory(materialFolder);
        Directory.CreateDirectory(prefabFolder);
        AssetDatabase.Refresh();
        var palette = new Dictionary<string, Color> {
            { "HullIvory", new Color(.79f, .83f, .85f) },
            { "HullShadow", new Color(.38f, .47f, .58f) },
            { "OrbitalNavy", new Color(.085f, .14f, .25f) },
            { "SolarBlue", new Color(.13f, .32f, .57f) },
            { "SolarTeal", new Color(.23f, .56f, .66f) },
            { "FoilGold", new Color(.78f, .59f, .27f) },
            { "SafetyCoral", new Color(.78f, .29f, .25f) },
            { "CableRubber", new Color(.10f, .13f, .18f) },
            { "GlowTelemetry", new Color(.23f, .72f, .82f) },
            { "ExposedSteel", new Color(.55f, .62f, .69f) }
        };
        var materials = new Dictionary<string, Material>();
        foreach (var pair in palette)
        {
            string path = materialFolder + "/" + pair.Key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var authored = TrainSim.Mat(pair.Value);
            authored.name = pair.Key;
            if (material) { EditorUtility.CopySerialized(authored, material); Object.DestroyImmediate(authored); }
            else { material = authored; AssetDatabase.CreateAsset(material, path); }
            EditorUtility.SetDirty(material);
            materials[pair.Key] = material;
        }
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (string name in new[] { "HullWreckage", "PressureCanister", "OrbitalSatellite", "DamagedBooster", "FlightDrive" })
            {
                string path = "Assets/Resources/Ascent/Models/" + name + ".fbx";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var root = new GameObject(name);
                SceneManager.MoveGameObjectToScene(root, scene);
                var body = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                body.transform.SetParent(root.transform, false);
                // FBX's front is +Z; present the authored panels and service details to the flight camera.
                body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (var renderer in body.GetComponentsInChildren<Renderer>())
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++) slots[i] = materials[slots[i].name];
                    renderer.sharedMaterials = slots;
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabFolder + "/" + name + ".prefab");
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            return "Baked 5 flight prefabs with 10 shared RealToon materials.";
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}

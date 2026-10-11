using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LocomotiveRedWhiteImport
{
    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        const string modelPath = "Assets/Resources/Models/Locomotive_RedWhite.fbx";
        const string prefabPath = "Assets/Resources/Prefabs/Locomotive_RedWhite.prefab";
        const string materialFolder = "Assets/Materials/Locomotive_RedWhite";
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (!importer) throw new Exception("Locomotive FBX not imported.");
        importer.importAnimation = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.SaveAndReimport();
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(materialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Locomotive_RedWhite");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs")) AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        var palette = new Dictionary<string, Color> {
            {"RW_Red", new Color(.83f,.055f,.075f)},
            {"RW_DeepRed", new Color(.51f,.025f,.045f)},
            {"RW_White", new Color(.94f,.95f,.97f)},
            {"RW_Black", new Color(.055f,.065f,.080f)},
            {"RW_Steel", new Color(.32f,.36f,.41f)},
            {"RW_Glass", new Color(.24f,.57f,.70f)},
            {"RW_Lamp", new Color(1f,.96f,.82f)}
        };
        var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Toon.mat");
        if (!template) throw new Exception("Game Toon material missing.");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var instance = UnityEngine.Object.Instantiate(model);
        try {
            instance.name = "Locomotive_RedWhite";
            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            var slots = renderer.sharedMaterials;
            for (int i=0; i<slots.Length; i++) {
                string name = slots[i].name;
                if (!palette.TryGetValue(name, out Color color)) throw new Exception("Unknown material: " + name);
                string path = materialFolder + "/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(template); AssetDatabase.CreateAsset(mat,path); }
                else mat.CopyPropertiesFromMaterial(template);
                mat.name = name;
                mat.SetColor("_MainColor",color.linear);
                mat.SetTexture("_MainTex",null);
                EditorUtility.SetDirty(mat);
                slots[i] = mat;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
            }
            renderer.sharedMaterials = slots;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
            if (!prefab) throw new Exception("Prefab save failed.");
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            var mesh = instance.GetComponentInChildren<MeshFilter>().sharedMesh;
            string result = prefabPath + " | " + mesh.triangles.Length/3 + " triangles | " + slots.Length + " RealToon materials | size " + renderer.bounds.size;
            importer.SaveAndReimport();
            return result;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}

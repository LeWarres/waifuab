using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SetupR20Story
{
    static void Folder(string path) { if (!AssetDatabase.IsValidFolder(path)) { Folder(Path.GetDirectoryName(path).Replace('\\','/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'), Path.GetFileName(path)); } }
    static Material Material(string name, Color color)
    {
        string path = "Assets/Art/Government/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = TrainSim.Mat(color); AssetDatabase.CreateAsset(m, path); }
        return m;
    }
    static GameObject Model(string path, Transform parent, Vector3 position, float scale = 1)
    {
        var source = Resources.Load<GameObject>(path);
        if (!source) throw new Exception("Missing " + path);
        var o = UnityEngine.Object.Instantiate(source, parent); o.transform.localPosition = position; o.transform.localRotation = Quaternion.identity; o.transform.localScale = Vector3.one * scale; return o;
    }
    static void Block(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name; o.transform.SetParent(parent, false);
        o.transform.localPosition = position; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
    }
    static void Save(GameObject root, string name)
    {
        try { PrefabUtility.SaveAsPrefabAsset(root, name); } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Import in Edit Mode");
        Folder("Assets/Art/Government"); Folder("Assets/Resources/Scenery/government");
        var blue = Material("SecurityBlue", new Color(.12f,.38f,.65f));
        var white = Material("Concrete", new Color(.72f,.76f,.79f));
        var yellow = Material("HazardGold", new Color(.95f,.65f,.12f));
        var dark = Material("Steel", new Color(.12f,.16f,.2f));
        var root = new GameObject("Security supplies");
        Model("Interior/crate", root.transform, Vector3.zero, .65f);
        Block(root.transform, "Seal", new Vector3(0,.6f,0), new Vector3(.55f,.08f,.55f), blue);
        Save(root, "Assets/Resources/Scenery/government/01_supplies.prefab");
        root = new GameObject("Security checkpoint");
        Model("Scenery/city/29_city_barrier", root.transform, Vector3.zero);
        Model("Scenery/city/07_FloorProps_Light_01", root.transform, new Vector3(1.5f,0,0), .55f);
        Save(root, "Assets/Resources/Scenery/government/02_checkpoint.prefab");
        root = new GameObject("Patrol");
        Model("Scenery/city/14_Vehicles_Police_01", root.transform, Vector3.zero);
        Save(root, "Assets/Resources/Scenery/government/03_patrol.prefab");
        root = new GameObject("Cloning research hangar");
        Model("Scenery/city/04_Buildings_Factory_02", root.transform, Vector3.zero);
        Model("Scenery/city/11_FloorProps_Fence_01", root.transform, new Vector3(0,0,-4));
        Block(root.transform,"Security facade",new Vector3(0,1.5f,-3),new Vector3(4,3,.2f),white);
        Block(root.transform,"Armored door",new Vector3(0,1.2f,-3.15f),new Vector3(2,2.4f,.15f),dark);
        Block(root.transform,"Blue identification",new Vector3(0,2.8f,-3.25f),new Vector3(3,.25f,.12f),blue);
        for(int i=0;i<5;i++) Block(root.transform,"Warning strip",new Vector3(-1.5f+i*.75f,.12f,-3.5f),new Vector3(.4f,.2f,.6f),yellow);
        Save(root, "Assets/Resources/Scenery/government/04_hangar.prefab");
        root = new GameObject("Government terminal");
        Model("Models/Station_city", root.transform, Vector3.zero);
        Model("Scenery/city/14_Vehicles_Police_01", root.transform, new Vector3(5,0,3));
        Model("Scenery/city/11_FloorProps_Fence_01", root.transform, new Vector3(0,0,5));
        Block(root.transform,"Security identification",new Vector3(0,3,2),new Vector3(5,.4f,.3f),blue);
        Save(root, "Assets/Resources/Models/Station_government.prefab");
        foreach(string kind in new[]{"Ground","Sky"})
        {
            string path="Assets/Resources/"+kind+"/government.png";
            if (!File.Exists(path)) AssetDatabase.CopyAsset("Assets/Resources/"+kind+"/city.png",path);
        }
        foreach(string path in Directory.GetFiles("Assets/Resources/Story","r20_*.png"))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(path); t.textureType=TextureImporterType.Default;
            t.sRGBTexture=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;
            t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return LocalizationImport.Run()+" Government terminal, patrols, checkpoint, research hangar and supplies imported.";
    }
}

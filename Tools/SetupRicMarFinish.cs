using System;
using UnityEditor;
using UnityEngine;
public static class SetupRicMarFinish
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string id in new[]{"enamel","glass"})
        {
            var t=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/RicMarFinish/"+id+".png");t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;
            t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        var source=Resources.Load<GameObject>("Models/Locomotive");int count=0;
        foreach(var r in source.GetComponentsInChildren<Renderer>())foreach(var original in r.sharedMaterials)
        {
            string path="Assets/Resources/RicMarFinish/"+original.name+".mat";var paint=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!paint){paint=new Material(Resources.Load<Material>("Toon"));AssetDatabase.CreateAsset(paint,path);}
            paint.CopyPropertiesFromMaterial(Resources.Load<Material>("Toon"));paint.name=original.name;paint.enableInstancing=true;
            TrainSim.Tint(paint,original.color);
            string id=original.name;
            if(id=="LocoBlue"||id=="LocoNavy"||id=="LocoYellow"||id=="LocoOrange")paint.mainTexture=Resources.Load<Texture2D>("RicMarFinish/enamel");
            else if(id=="LocoGlass"){paint.mainTexture=Resources.Load<Texture2D>("RicMarFinish/glass");TrainSim.Tint(paint,new Color(.62f,.8f,.92f));}
            else if(id=="LocoSteel"||id=="LocoBronze"||id=="LocoDark")paint.mainTexture=Resources.Load<Texture2D>("RaceSurfaces/steel");
            paint.SetFloat("_GlossIntensity",0f);paint.SetFloat("_ReflectionIntensity",0f);paint.SetFloat("_RimLightColorPower",0f);
            EditorUtility.SetDirty(paint);count++;
        }
        AssetDatabase.SaveAssets();AssetDatabase.ImportAsset("Assets/Resources/Models/Locomotive.fbx",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        return "RicMar: "+count+" toon materials saved; projected UVs imported; original geometry unchanged.";
    }
}

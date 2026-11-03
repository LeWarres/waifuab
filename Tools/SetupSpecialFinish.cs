using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class SetupSpecialFinish
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string id in new[]{"hellstone","hellwood","trencharmor","trenchdeck"})
        {
            var t=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/RaceSurfaces/"+id+".png");
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;t.filterMode=FilterMode.Trilinear;t.anisoLevel=8;
            t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        var paths=new HashSet<string>();
        foreach(string folder in new[]{"Scenery/hell","Scenery/fortress"})foreach(var go in Resources.LoadAll<GameObject>(folder))foreach(var f in go.GetComponentsInChildren<MeshFilter>())
        {
            var r=f.GetComponent<Renderer>();if(!r)continue;bool needed=false;
            foreach(var m in r.sharedMaterials)if(m&&(m.name=="Basalt"||m.name=="Basalt_FFFFFF"||m.name=="InfernalIron"||m.name=="FortArmor"||m.name=="FortLight"))needed=true;
            if(needed)paths.Add(AssetDatabase.GetAssetPath(f.sharedMesh));
        }
        int changed=0;foreach(string p in paths)if(AssetImporter.GetAtPath(p) is ModelImporter m&&!m.isReadable){m.isReadable=true;m.SaveAndReimport();changed++;}
        AssetDatabase.SaveAssets();return "Four painted textures imported; readable models enabled: "+changed;
    }
}

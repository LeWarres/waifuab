using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class SetupCityFinish
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);int models=0;
        foreach(string id in new[]{"asphalt","concrete","steel","facade"})
        {
            var t=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/RaceSurfaces/"+id+".png");
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;
            t.filterMode=FilterMode.Trilinear;t.anisoLevel=8;t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        var paths=new HashSet<string>();
        foreach(string id in new[]{"barrier","gantry","lamp","arch","seats","car","hazard","tower1","tower2","tower3","tower4"})
            paths.Add("Assets/Resources/Models/Race_"+id+".fbx");
        foreach(var go in Resources.LoadAll<GameObject>("Scenery/city"))
        {
            var bounds=new Bounds();bool any=false;foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){if(any)bounds.Encapsulate(r.bounds);else bounds=r.bounds;any=true;}
            if(!any||bounds.size.y<=4f)continue;
            foreach(var f in go.GetComponentsInChildren<MeshFilter>())
            {
            var r=f.GetComponent<Renderer>();if(!r)continue;bool needed=false;
            foreach(var m in r.sharedMaterials)if(m&&(m.name=="CityConcrete"||m.name=="CityCream"||m.name=="CitySteel"||m.name=="CityBrick"))needed=true;
            if(needed)paths.Add(AssetDatabase.GetAssetPath(f.sharedMesh));
            }
        }
        foreach(string p in paths)if(AssetImporter.GetAtPath(p) is ModelImporter model&&!model.isReadable){model.isReadable=true;model.SaveAndReimport();models++;}
        AssetDatabase.SaveAssets();return "Four painted textures imported with mipmaps; readable model mapping enabled: "+models;
    }
}

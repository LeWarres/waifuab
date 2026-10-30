using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class ImportOniniFanArt
{
    public static string Run()
    {
        var paths=Directory.GetFiles("Assets/Resources/Story","onini_*.png");
        if(paths.Length!=15)throw new Exception("Expected 15 images");
        foreach(var path in paths)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture||Mathf.Abs((float)texture.width/texture.height-16f/9f)>.03f)throw new Exception("Invalid image "+path);
        }
        return "PASS: 15 fanmade pictures imported; existing GUIDs preserved.";
    }
}

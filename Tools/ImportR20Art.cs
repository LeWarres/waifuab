using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportR20Art
{
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Import in Edit Mode");
        int count=0;
        foreach(string path in Directory.GetFiles("Assets/Resources/Story","r20_*.png"))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;
            t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;
            t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;
            t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            if(!AssetDatabase.LoadAssetAtPath<Texture2D>(path))throw new Exception("Missing "+path);
            count++;
        }
        return $"Imported {count} corrected R20 illustrations, preserving asset GUIDs.";
    }
}

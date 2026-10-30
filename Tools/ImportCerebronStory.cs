using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ImportCerebronStory
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        int count=0;
        foreach(string raw in Directory.GetFiles("Assets/Resources/Story","cerebron_*.png"))
        {
            string path=raw.Replace('\\','/');
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;
            t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;
            t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;
            t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();count++;
        }
        AssetDatabase.SaveAssets();return LocalizationImport.Run()+" Imported "+count+" Cerebron illustrations.";
    }
}

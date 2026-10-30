using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class SetupGigerStory
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        int pictures=0,entries=0;
        foreach(var file in Directory.GetFiles("Assets/Resources/Story","giger_*.png"))
        {
            string p=file.Replace('\\','/');AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(p);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();pictures++;
        }
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(var line in lines)if(line.StartsWith("story.giger.")||line.StartsWith("giger.")||line.StartsWith("hero.character.giger\t")){var cells=line.Split('\t');table.AddEntry(cells[0],cells[c]);entries++;}
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);AssetDatabase.SaveAssets();return $"Imported {pictures} sketches and {entries} translations.";
    }
}

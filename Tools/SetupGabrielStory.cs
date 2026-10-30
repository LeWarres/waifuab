using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class SetupGabrielStory
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        int pictures=0,entries=0;
        foreach(var file in Directory.GetFiles("Assets/Resources/Story","gabriel_*.png"))
        {
            string p=file.Replace('\\','/');AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(p);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();pictures++;
        }
        string portrait="Assets/Resources/Menu/hero_gabriel.png";
        AssetDatabase.ImportAsset(portrait,ImportAssetOptions.ForceUpdate);
        var image=(TextureImporter)AssetImporter.GetAtPath(portrait);image.mipmapEnabled=false;image.wrapMode=TextureWrapMode.Clamp;image.npotScale=TextureImporterNPOTScale.None;image.SaveAndReimport();
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(var line in lines)if(line.StartsWith("story.gabriel.")||line.StartsWith("gabriel.")||line.StartsWith("hero.character.gabriel\t")){var cells=line.Split('\t');table.AddEntry(cells[0],cells[c]);entries++;}
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);AssetDatabase.SaveAssets();return $"Imported {pictures} sketches and {entries} translations.";
    }
}

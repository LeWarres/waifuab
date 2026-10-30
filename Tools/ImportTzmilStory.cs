using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportTzmilStory
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        int count=0,strings=0;
        foreach(string raw in Directory.GetFiles("Assets/Resources/Story","tzmil_*.png"))
        {
            string path=raw.Replace('\\','/');AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();count++;
        }
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(var line in lines)if(line.StartsWith("story.tzmil.")){var cells=line.Split('\t');table.AddEntry(cells[0],cells[c].Replace("\\n","\n"));strings++;}
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);AssetDatabase.SaveAssets();
        return $"Imported {count} Tzmil images and {strings} localized entries.";
    }
}

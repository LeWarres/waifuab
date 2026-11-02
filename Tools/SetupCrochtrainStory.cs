using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class SetupCrochtrainStory
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int pictures=0,entries=0;
        foreach(string id in new[]{"gift","book","service","escape","hell","heaven","sea","cafe","taken"})
        {
            string path="Assets/Resources/Story/crochtrain_"+id+".png";
            if(!File.Exists(path))throw new Exception("Missing "+path);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;
            t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!image||image.width<image.height)throw new Exception("Invalid landscape art: "+path);pictures++;
        }
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in lines)
            {
                var row=line.Split('\t');if(!row[0].StartsWith("story.crochtrain.")&&row[0]!="hero.character.crochtrain"&&row[0]!="story.giger.1.before.2"&&row[0]!="story.giger.1.after.1"&&row[0]!="story.giger.1.after.2")continue;
                if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid entry: "+row[0]);
                table.AddEntry(row[0],row[c]);if(table.GetEntry(row[0])?.LocalizedValue!=row[c])throw new Exception("Readback mismatch: "+row[0]);entries++;
            }
            EditorUtility.SetDirty(table);
        }
        if(entries!=216)throw new Exception("Wrong entry count: "+entries);
        EditorUtility.SetDirty(collection);EditorUtility.SetDirty(collection.SharedData);LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return $"Imported {pictures} landscape illustrations, 204 Crochtrain values and 12 Giger continuity values.";
    }
}

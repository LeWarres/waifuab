using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportOniniRevision
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');int count=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in lines)
            {
                if(!line.StartsWith("story.onini."))continue;
                var row=line.Split('\t');if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid Onini entry");
                table.AddEntry(row[0],row[c].Replace("\\n","\n"));count++;
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection);EditorUtility.SetDirty(collection.SharedData);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return "Imported Onini personality: "+count+" localized values. Artwork and route preserved.";
    }
}

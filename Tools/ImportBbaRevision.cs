using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportBbaRevision
{
    public static string Run()
    {
        if(Application.isPlaying) throw new Exception("Edit Mode required");
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        var lines=File.ReadAllLines("Tools/bba_story_revision.tsv");
        var header=lines[0].Split('\t');int count=0;
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            if(!table) throw new Exception("Missing locale: "+header[c]);
            for(int i=1;i<lines.Length;i++)
            {
                var row=lines[i].Split('\t');
                if(row.Length!=5 || string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid row "+i);
                table.AddEntry(row[0],row[c].Replace("\\n","\n"));count++;
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);EditorUtility.SetDirty(collection);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);
        AssetDatabase.SaveAssets();return "Imported BBA revision: "+count+" localized values.";
    }
}

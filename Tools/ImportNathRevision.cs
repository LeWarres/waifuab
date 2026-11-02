using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportNathRevision
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');int count=0;
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(var line in lines)
            {
                if(!line.StartsWith("story.nath.")&&!line.StartsWith("hero.character.nath\t"))continue;
                var row=line.Split('\t');if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid Nath row");
                table.AddEntry(row[0],row[c].Replace("\\n","\n"));count++;
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);EditorUtility.SetDirty(collection);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return "Imported Nath Moon: "+count+" localized values; original artwork preserved.";
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportFacelessRevision
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var keys=new HashSet<string>();foreach(var line in File.ReadAllLines("Tools/faceless_story_revision.tsv"))if(!line.StartsWith("key\t"))keys.Add(line.Split('\t')[0]);
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');int count=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in lines)
            {
                var row=line.Split('\t');if(!keys.Contains(row[0]))continue;
                if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid Faceless entry");
                string value=row[c].Replace("\\n","\n");table.AddEntry(row[0],value);
                if(table.GetEntry(row[0])?.LocalizedValue!=value)throw new Exception("Readback mismatch: "+row[0]);
                count++;
            }
            EditorUtility.SetDirty(table);
        }
        if(count!=keys.Count*4)throw new Exception("Missing localization rows");
        EditorUtility.SetDirty(collection);EditorUtility.SetDirty(collection.SharedData);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return "Imported and verified "+count+" Faceless personality values in four languages.";
    }
}

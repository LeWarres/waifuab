using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportGolemRevision
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
                if(!line.StartsWith("story.sentinel.")&&!line.StartsWith("golem.")&&!line.StartsWith("biome.robotheaven\t")&&!line.StartsWith("boss.name.robotheaven\t"))continue;
                var row=line.Split('\t');if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid Golem entry");
                string value=row[c].Replace("\\n","\n");table.AddEntry(row[0],value);
                if(table.GetEntry(row[0])?.LocalizedValue!=value)throw new Exception("Readback mismatch: "+row[0]);
                count++;
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection);EditorUtility.SetDirty(collection.SharedData);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return "Imported and verified "+count+" Golem values in four languages.";
    }
}

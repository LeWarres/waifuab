using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportCerebronRevision
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var shared=new HashSet<string>();foreach(string side in new[]{"before","after"})for(int n=1;n<=(side=="before"?4:2);n++)shared.Add($"story.faceless.1.{side}.{n}");
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');int count=0;
        var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in lines)
            {
                var row=line.Split('\t');if(!row[0].StartsWith("story.cerebron.")&&!shared.Contains(row[0]))continue;
                if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid Cerebron entry");
                string value=row[c].Replace("\\n","\n");table.AddEntry(row[0],value);
                if(table.GetEntry(row[0])?.LocalizedValue!=value)throw new Exception("Readback mismatch: "+row[0]);count++;
            }
            foreach(string side in new[]{"before","after"})for(int n=1;n<=(side=="before"?4:2);n++)
                if(table.GetEntry($"story.cerebron.2.{side}.{n}").LocalizedValue!=table.GetEntry($"story.faceless.1.{side}.{n}").LocalizedValue)throw new Exception("Mirrored encounter differs");
            EditorUtility.SetDirty(table);
        }
        if(count!=240)throw new Exception("Unexpected localized count: "+count);
        EditorUtility.SetDirty(collection);EditorUtility.SetDirty(collection.SharedData);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return "Imported and verified 216 Cerebron values and 24 mirrored Faceless values.";
    }
}

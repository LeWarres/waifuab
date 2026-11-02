using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class ImportGigerRevision
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
                var row=line.Split('\t');if(!row[0].StartsWith("story.giger.")&&!row[0].StartsWith("giger.")&&row[0]!="hero.character.giger")continue;
                if(row.Length!=5||string.IsNullOrWhiteSpace(row[c]))throw new Exception("Invalid Giger entry");
                table.AddEntry(row[0],row[c]);
                if(table.GetEntry(row[0])?.LocalizedValue!=row[c])throw new Exception("Readback mismatch: "+row[0]);count++;
            }
            EditorUtility.SetDirty(table);
        }
        if(count!=200)throw new Exception("Unexpected localized count: "+count);
        EditorUtility.SetDirty(collection);EditorUtility.SetDirty(collection.SharedData);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);AssetDatabase.SaveAssets();
        return "Imported and verified 200 Giger values in four languages.";
    }
}

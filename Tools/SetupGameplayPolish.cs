using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class SetupGameplayPolish
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');var collection=LocalizationEditorSettings.GetStringTableCollection("Game");int count=0;
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);
            foreach(string line in lines){var row=line.Split('\t');if(row[0]!="shop.chest"&&row[0]!="miniboss.coming")continue;table.AddEntry(row[0],row[c].Replace("\\n","\n"));count++;}
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);AssetDatabase.SaveAssets();return "Imported "+count+" localized values";
    }
}

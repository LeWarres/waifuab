using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
public static class ImportBalanceText
{
    public static string Run()
    {
        var keys=new[]{"hero.damage","hero.armor","hero.vitality","hero.fireRate","hero.regen","hero.amount"};
        var rows=File.ReadAllLines("Assets/Localization/strings.tsv");var locales=rows[0].Split('\t');var collection=LocalizationEditorSettings.GetStringTableCollection("Game");int count=0;
        for(int c=1;c<locales.Length;c++)
        {
            var table=(StringTable)collection.GetTable(locales[c]);
            foreach(var row in rows){var cells=row.Split('\t');if(Array.IndexOf(keys,cells[0])<0)continue;table.AddEntry(cells[0],cells[c]);count++;}
            EditorUtility.SetDirty(table);
        }
        AssetDatabase.SaveAssets();return $"Updated {count} upgrade descriptions";
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Tools > Localization > Import strings.tsv
// Rebuilds the "Game" string table from Assets/Localization/strings.tsv (one row per key, one
// column per locale; "\n" in a cell is a line break). Edit the TSV, run this, done.
public static class LocalizationImport
{
    const string Folder = "Assets/Localization", Source = Folder + "/strings.tsv", Collection = "Game";

    [MenuItem("Tools/Localization/Import strings.tsv")]
    static void Menu() => Run();

    public static string Run()
    {
        // Settings asset: reuse the project's one, or create it.
        LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (!settings)
        {
            string[] found = AssetDatabase.FindAssets("t:LocalizationSettings", new[] { "Assets" });
            if (found.Length > 0) settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(AssetDatabase.GUIDToAssetPath(found[0]));
            else
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, Folder + "/LocalizationSettings.asset");
            }
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        string[] lines = File.ReadAllLines(Source);
        string[] header = lines[0].Split('\t'); // key, then one locale code per column

        // Locales named in the header; the first one is the project's default language.
        Directory.CreateDirectory(Folder + "/Locales");
        for (int c = 1; c < header.Length; c++)
        {
            if (LocalizationEditorSettings.GetLocale(header[c]) != null) continue;
            Locale locale = Locale.CreateLocale(header[c]);
            AssetDatabase.CreateAsset(locale, $"{Folder}/Locales/{header[c]}.asset");
            LocalizationEditorSettings.AddLocale(locale);
        }
        LocalizationSettings.ProjectLocale = LocalizationEditorSettings.GetLocale(header[1]);

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(Collection)
            ?? LocalizationEditorSettings.CreateStringTableCollection(Collection, Folder + "/Tables");

        int entries = 0;
        for (int c = 1; c < header.Length; c++)
        {
            // Matched by locale code, never by position in the collection.
            var table = collection.GetTable(header[c]) as UnityEngine.Localization.Tables.StringTable
                ?? collection.AddNewTable(new LocaleIdentifier(header[c])) as UnityEngine.Localization.Tables.StringTable;
            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                string[] cells = lines[row].Split('\t');
                table.AddEntry(cells[0], cells[c].Replace("\\n", "\n"));
                entries++;
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(settings);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        AssetDatabase.SaveAssets();
        string result = $"Imported {entries} entries into '{Collection}' for {header.Length - 1} locales.";
        Debug.Log(result);
        return result;
    }
}

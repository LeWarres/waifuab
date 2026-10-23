using UnityEditor;

public static class ImportBbaStoryArt
{
    public static string Run()
    {
        var source = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Story/bba_desert_victory.png");
        var settings = new TextureImporterSettings();
        source.ReadTextureSettings(settings);
        string[] names = { "bba_alien_arrival", "bba_alien_victory", "bba_base_journey", "bba_fortress_arrival",
            "bba_ending_return", "bba_ending_truth", "bba_ending_silicon", "bba_ending_missing_ship", "bba_ending_reflection" };
        foreach (string name in names)
        {
            string path = "Assets/Resources/Story/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var target = (TextureImporter)AssetImporter.GetAtPath(path);
            target.SetTextureSettings(settings);
            target.maxTextureSize = source.maxTextureSize;
            target.textureCompression = source.textureCompression;
            target.SaveAndReimport();
        }
        return "Imported 9 BBA story illustrations with the existing story texture settings.";
    }
}

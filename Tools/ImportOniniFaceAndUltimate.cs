using UnityEditor;
using UnityEngine;
public static class ImportOniniFaceAndUltimate
{
    public static string Run()
    {
        foreach (string path in new[]{"Assets/Resources/Ultimate/onini_cut_in.png", "Assets/Resources/Menu/hero_onini_cut.png"})
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=4096;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.alphaIsTransparency=path.Contains("/Menu/");
            importer.SaveAndReimport();
        }
        return "Imported corrected Onini menu portrait and Ultimate banner";
    }
}

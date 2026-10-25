using UnityEditor;
using UnityEngine;

public static class ImportFacelessPrologue
{
    public static string Run()
    {
        foreach (string name in new[] { "champion", "orders", "legacy", "artist", "friend", "idea" })
        {
            string path = "Assets/Resources/Story/faceless_" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var texture = (TextureImporter)AssetImporter.GetAtPath(path);
            texture.textureType = TextureImporterType.Default;
            texture.sRGBTexture = true;
            texture.mipmapEnabled = false;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.maxTextureSize = 2048;
            texture.textureCompression = TextureImporterCompression.CompressedHQ;
            texture.SaveAndReimport();
        }
        return LocalizationImport.Run() + " Imported six Faceless illustrations.";
    }
}

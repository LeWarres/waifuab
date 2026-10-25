using UnityEditor;
using UnityEngine;

public static class ImportMenuHeroArt
{
    public static string Run()
    {
        string[] heroes = { "bba", "cerebron", "culu", "faceless", "onini", "sentinel", "skullmonk", "tzmil", "xiuhtecuhtli" };
        foreach (string id in heroes)
        {
            string path = "Assets/Resources/Menu/hero_" + id + "_cut.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        return "Imported " + heroes.Length + " transparent menu portraits";
    }
}

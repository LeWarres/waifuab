using UnityEditor;
using UnityEngine;
public static class ImportMachineMenuArt
{
    public static string Run()
    {
        foreach(string hero in new[]{"giger","crochtrain"})
        {
            string path="Assets/Resources/Menu/hero_"+hero+"_cut.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;
            importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        return "Imported Giger Machine and Crochtrain transparent menu art";
    }
}

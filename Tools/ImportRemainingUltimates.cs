using UnityEditor;
using UnityEngine;
public static class ImportRemainingUltimates
{
    public static string Run()
    {
        foreach(string hero in new[]{"xiuhtecuhtli","giger","crochtrain"})
        {
            string path="Assets/Resources/Ultimate/"+hero+"_cut_in.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=4096;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        return "Imported Xiuhtecuhtli, Giger Machine and Crochtrain Ultimate cards";
    }
}

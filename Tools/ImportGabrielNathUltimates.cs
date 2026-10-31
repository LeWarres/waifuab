using UnityEditor;
using UnityEngine;
public static class ImportGabrielNathUltimates
{
    public static string Run()
    {
        foreach(string hero in new[]{"gabriel","nath"})
        foreach(bool menu in new[]{false,true})
        {
            string path=menu ? "Assets/Resources/Menu/hero_"+hero+"_cut.png" : "Assets/Resources/Ultimate/"+hero+"_cut_in.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=4096;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.alphaIsTransparency=menu;
            importer.SaveAndReimport();
        }
        return "Imported Gabriel and Nath Ultimate banners and menu portraits";
    }
}

using System;
using UnityEditor;
using UnityEngine;
public static class ImportCuluArt
{
    public static string Run()
    {
        int count=0;
        foreach(string path in System.IO.Directory.GetFiles("Assets/Resources/Story","culu_*.png"))
        {
            string asset=path.Replace('\\','/');
            AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceUpdate);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            if(!texture || texture.width<1000 || texture.width<texture.height)throw new Exception("Invalid story art: "+asset);
            count++;
        }
        return "Imported and checked "+count+" Culu story illustrations.";
    }
}

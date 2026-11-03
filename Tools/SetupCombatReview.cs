using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class SetupCombatReview
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        AssetDatabase.Refresh();int count=0;
        foreach(string file in Directory.GetFiles("Assets/Resources/Icons","*_evo.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=256;importer.SaveAndReimport();count++;
        }
        return "Configured "+count+" evolved icons";
    }
}

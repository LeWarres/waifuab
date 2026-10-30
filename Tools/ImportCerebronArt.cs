using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ImportCerebronArt
{
    public static string Run()
    {
        int count=0;
        foreach(string raw in Directory.GetFiles("Assets/Resources/Story","cerebron_*.png"))
        {
            string path=raw.Replace('\\','/');
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture || texture.width<1000 || (float)texture.width/texture.height<1.7f || (float)texture.width/texture.height>1.85f)
                throw new Exception("Invalid landscape illustration: "+path);
            count++;
        }
        if(count!=13)throw new Exception("Expected thirteen Cerebron illustrations, got "+count);
        return "Imported thirteen landscape Cerebron illustrations with existing GUID metadata.";
    }
}

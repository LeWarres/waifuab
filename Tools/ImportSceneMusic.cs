using UnityEditor;
using UnityEngine;
public static class ImportSceneMusic
{
    public static string Run()
    {
        int count=0;
        foreach(string name in new[]{"selectMenu","desierto","desiertofinalboss","city","cityfinalboss"})
        {
            string path="Assets/Resources/Music/"+name+".ogg";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.Streaming;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;
            settings.quality=1f;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings;
            importer.forceToMono=false;
            importer.SaveAndReimport();
            count++;
        }
        AssetDatabase.SaveAssets();return "Imported "+count+" streaming music tracks";
    }
}

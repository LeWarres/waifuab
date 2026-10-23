using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportEyeBossAudio
{
    public static string Run()
    {
        int count = 0;
        foreach (string file in Directory.GetFiles("Assets/MusicSound/Resources/SFX", "eye_*.wav"))
        {
            string path = file.Replace('\\', '/');
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            count++;
        }
        return "Imported " + count + " eye-boss effects as mono PCM at 44.1 kHz.";
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportRaceAudio
{
    public static string Run()
    {
        int count = 0;
        foreach (string file in Directory.GetFiles("Assets/MusicSound/Resources/SFX", "race_*.wav"))
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
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip || clip.channels != 1 || clip.frequency != 44100)
                throw new System.Exception("Invalid race sound: " + path);
            count++;
        }
        return "Imported and checked " + count + " mono PCM race effects at 44.1 kHz.";
    }
}

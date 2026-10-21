using UnityEditor;
using UnityEngine;

public static class ImportAscentAudio
{
    public static string Run()
    {
        string[] names = { "ascent_propulsion", "ascent_pass_1", "ascent_pass_2", "ascent_pass_3", "ascent_impact_1", "ascent_impact_2", "ascent_impact_3" };
        foreach (string name in names)
        {
            string path = "Assets/MusicSound/Resources/SFX/" + name + ".wav";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            // Short synthetic effects stay PCM, preserving their transients and the engine's loop seam.
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var clip = Resources.Load<AudioClip>("SFX/" + name);
            if (!clip || clip.channels != 1 || clip.frequency != 44100)
                throw new System.Exception("Incorrect flight audio import: " + name);
        }
        return "Imported and checked 7 original mono PCM effects at 44.1 kHz.";
    }
}

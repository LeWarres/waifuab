using System.Collections.Generic;
using UnityEngine;

// The game's volume settings: music and effects apart, a master over both and a mute for everything.
// Kept between runs. An AudioSource joins by telling which kind it is; its own loudness stays as its base.
// The pause menu changes them (TrainSim's sound menu).
public static class Sound
{
    public static float Master { get; private set; } = PlayerPrefs.GetFloat("sound.master", 1f);
    public static float Music { get; private set; } = PlayerPrefs.GetFloat("sound.music", 0.8f);
    public static float Effects { get; private set; } = PlayerPrefs.GetFloat("sound.sfx", 1f);
    public static bool Muted { get; private set; } = PlayerPrefs.GetInt("sound.mute", 0) == 1;

    static readonly List<(AudioSource source, float loudness, bool music)> sources = new();
    static readonly Dictionary<string, AudioClip> clips = new();
    static readonly Dictionary<string, int> playedFrame = new();
    static AudioSource sharedEffects;

    // Keeps button sounds alive during scene changes and works while the game is paused.
    public static void Play(string clip, float loudness = 0.5f)
    {
        if (!Application.isPlaying) return;
        if (playedFrame.TryGetValue(clip, out int frame) && frame == Time.frameCount) return;
        if (!clips.TryGetValue(clip, out AudioClip audio) || !audio)
            clips[clip] = audio = Resources.Load<AudioClip>("SFX/" + clip);
        if (!audio) return;
        if (!sharedEffects)
        {
            var owner = new GameObject("GameplaySounds");
            Object.DontDestroyOnLoad(owner);
            sharedEffects = owner.AddComponent<AudioSource>();
            sharedEffects.playOnAwake = false;
            sharedEffects.ignoreListenerPause = true;
            AsEffect(sharedEffects);
        }
        playedFrame[clip] = Time.frameCount;
        sharedEffects.PlayOneShot(audio, loudness);
    }

    public static AudioSource AsEffect(AudioSource source, float loudness = 1f) => Join(source, loudness, false);
    public static AudioSource AsMusic(AudioSource source, float loudness = 1f) => Join(source, loudness, true);

    public static AudioSource Effect(GameObject owner, string clip, float loudness = 1f)
    {
        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = Resources.Load<AudioClip>("SFX/" + clip);
        return AsEffect(source, loudness);
    }

    static AudioSource Join(AudioSource source, float loudness, bool music)
    {
        sources.Add((source, loudness, music));
        Apply();
        return source;
    }

    // which: 0 master, 1 music, 2 effects. A step is a tenth; going past either end comes round to the other.
    public static void Step(int which, int steps)
    {
        float Next(float now) => Mathf.Repeat(Mathf.Round(now * 10f) + steps, 11f) / 10f;
        if (which == 0) Master = Next(Master);
        else if (which == 1) Music = Next(Music);
        else Effects = Next(Effects);
        Save();
    }

    public static void ToggleMute()
    {
        Muted = !Muted;
        Save();
    }

    static void Save()
    {
        PlayerPrefs.SetFloat("sound.master", Master);
        PlayerPrefs.SetFloat("sound.music", Music);
        PlayerPrefs.SetFloat("sound.sfx", Effects);
        PlayerPrefs.SetInt("sound.mute", Muted ? 1 : 0);
        Apply();
    }

    static void Apply()
    {
        AudioListener.volume = Muted ? 0f : Master; // the master and the mute act on everything the game plays
        sources.RemoveAll(entry => !entry.source);  // gone with a scene reload
        foreach (var entry in sources) entry.source.volume = entry.loudness * (entry.music ? Music : Effects);
    }
}

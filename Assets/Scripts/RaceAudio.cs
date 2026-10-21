using System.Collections.Generic;
using UnityEngine;

// Runs on real time: the race freezes the surrounding gameplay, not its own engine audio.
[DisallowMultipleComponent]
public class RaceAudio : MonoBehaviour
{
    readonly AudioSource[] loops = new AudioSource[6]; // player, air, turbo, drift, two nearby opponents
    readonly float[] levels = new float[6];
    readonly float[] gains = new float[6];
    readonly Dictionary<string, AudioClip> clips = new();
    AudioSource signals, contact;
    bool live, paused, boosted;
    int lastCount = -1, lastImpact;
    float contactAt, warningAt;

    void Awake()
    {
        string[] names = { "race_engine_loop", "race_wind_loop", "race_turbo_loop", "race_drift_loop", "race_engine_loop", "race_engine_loop" };
        float[] ceilings = { .26f, .11f, .14f, .17f, .1f, .1f };
        for (int i = 0; i < loops.Length; i++)
        {
            loops[i] = Sound.Effect(gameObject, names[i], ceilings[i]);
            loops[i].loop = true;
            loops[i].volume = 0f;
            loops[i].spatialBlend = 0f;
            loops[i].dopplerLevel = 0f;
        }
        signals = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), .42f);
        contact = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), .48f);
        signals.playOnAwake = contact.playOnAwake = false;
    }

    AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Resources.Load<AudioClip>("SFX/" + name);
        return clip;
    }

    public void Begin()
    {
        Stop();
        live = true;
        lastCount = -1;
        boosted = false;
        contactAt = 0f;
        warningAt = -10f;
        levels[0] = .14f;
    }

    public void Cue(string name, float gain = 1f)
    {
        if (!live || paused) return;
        var clip = Clip(name);
        if (clip) signals.PlayOneShot(clip, gain);
    }

    public void Countdown(int number)
    {
        if (number == lastCount) return;
        lastCount = number;
        Cue(number > 0 ? "race_countdown" : "race_go");
    }

    public void Drive(float speed, bool turbo, bool drift, bool brake, bool crashed, float heat)
    {
        speed = Mathf.Clamp01(speed);
        levels[0] = crashed ? 0f : .14f + .12f * speed;
        levels[1] = crashed ? 0f : .11f * speed * speed;
        levels[2] = !crashed && turbo ? heat >= .72f ? .1f : .14f : 0f;
        levels[3] = !crashed && speed > .2f ? drift ? .17f : brake ? .065f : 0f : 0f;
        loops[0].pitch = .62f + speed * 1.08f;
        loops[1].pitch = .85f + speed * .25f;
        loops[2].pitch = 1f + speed * .15f;
        loops[3].pitch = .9f + speed * .3f;
        if (turbo && !boosted && !crashed) Cue("race_boost", .85f);
        boosted = turbo && !crashed;
        float alarmInterval = Mathf.Lerp(.45f, .18f, Mathf.InverseLerp(.72f, .98f, heat));
        if (!crashed && turbo && heat >= .72f)
        {
            if (Time.unscaledTime - warningAt >= alarmInterval)
            {
                Cue("race_warning", .95f);
                warningAt = Time.unscaledTime;
            }
        }
        else warningAt = -10f; // releasing the turbo silences the alarm while it cools
    }

    public void StopDriving()
    {
        for (int i = 0; i < loops.Length; i++)
        {
            levels[i] = gains[i] = 0f;
            loops[i].Stop();
            loops[i].volume = 0f;
        }
        boosted = false;
    }

    public void Rival(int slot, float distance, float pan, float speed)
    {
        int i = 4 + slot;
        float near = Mathf.Clamp01(1f - distance / 45f);
        levels[i] = .1f * near * near;
        loops[i].panStereo = Mathf.Clamp(pan, -.9f, .9f);
        loops[i].pitch = (.7f + Mathf.Clamp01(speed) * 1.05f) * (slot == 0 ? .97f : 1.03f);
    }

    public void Bump(float pan, float gain = 1f, bool scrape = false)
    {
        if (!live || paused || Time.unscaledTime < contactAt) return;
        contactAt = Time.unscaledTime + (scrape ? .38f : .26f);
        int variant;
        do variant = Random.Range(1, 4); while (variant == lastImpact);
        lastImpact = variant;
        PlayContact(scrape ? "race_scrape" : "ascent_impact_" + variant, pan, gain * .7f);
    }

    public void Crash(float pan, float gain, bool player)
    {
        if (!live || paused) return;
        PlayContact("vagonroto", pan, gain);
    }

    void PlayContact(string name, float pan, float gain)
    {
        contact.panStereo = Mathf.Clamp(pan, -.85f, .85f);
        var clip = Clip(name);
        if (clip) contact.PlayOneShot(clip, Mathf.Clamp01(gain));
    }

    void LateUpdate()
    {
        if (!live || paused) return;
        for (int i = 0; i < loops.Length; i++)
        {
            var voice = loops[i];
            gains[i] = Mathf.MoveTowards(gains[i], levels[i], Time.unscaledDeltaTime * .7f);
            voice.volume = gains[i] * Sound.Effects;
            if (voice.volume > .001f && !voice.isPlaying && voice.clip)
            {
                voice.Play();
                if (i >= 4) voice.time = voice.clip.length * (i == 4 ? .23f : .61f);
            }
            else if (voice.volume <= .001f && levels[i] == 0f) voice.Stop();
        }
    }

    public void Pause(bool value)
    {
        if (paused == value) return;
        paused = value;
        foreach (var source in loops) if (source) { if (value) source.Pause(); else source.UnPause(); }
        if (value) { signals.Pause(); contact.Pause(); }
        else { signals.UnPause(); contact.UnPause(); }
    }

    public void Stop()
    {
        live = paused = false;
        for (int i = 0; i < loops.Length; i++)
        {
            levels[i] = 0f;
            gains[i] = 0f;
            if (loops[i]) { loops[i].Stop(); loops[i].volume = 0f; }
        }
        if (signals) signals.Stop();
        if (contact) contact.Stop();
    }

    void OnDisable() => Stop();
}

using System.Collections.Generic;
using UnityEngine;

// Separate voices keep rapid turret fire from covering the boss's warnings.
[DisallowMultipleComponent]
public class EyeBossAudio : MonoBehaviour
{
    AudioSource effects, shots, sweep, charge;
    readonly Dictionary<string, AudioClip> clips = new();
    readonly Dictionary<string, float> next = new();
    bool paused, sweeping;
    float sweepGain;

    void Awake()
    {
        effects = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), .6f);
        shots = Sound.Effect(gameObject, "machinegunsound", .6f);
        shots.loop = true;
        sweep = Sound.Effect(gameObject, "eye_sweep_loop", .18f);
        charge = Sound.Effect(gameObject, "eye_charge", .5f);
        sweep.loop = true;
        foreach (var voice in new[] { effects, shots, sweep, charge })
        {
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
        }
        sweep.volume = 0f;
    }

    public void Cue(string name, float gain = 1f, float interval = .1f)
    {
        if (paused || (next.TryGetValue(name, out float at) && Time.unscaledTime < at)) return;
        next[name] = Time.unscaledTime + interval;
        if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Resources.Load<AudioClip>("SFX/" + name);
        if (clip) effects.PlayOneShot(clip, gain);
    }

    public void Firing(bool firing)
    {
        if (paused || !shots.clip) return;
        if (firing && !shots.isPlaying) shots.Play();
        else if (!firing) shots.Stop();
    }

    public void Charge(float seconds)
    {
        if (paused || charge.isPlaying || !charge.clip) return;
        charge.pitch = charge.clip.length / Mathf.Max(.1f, seconds);
        charge.Play();
    }

    public void Phase(bool hasSweep)
    {
        shots.Stop();
        sweeping = hasSweep;
        charge.Stop();
    }

    void LateUpdate()
    {
        if (paused) return;
        sweepGain = Mathf.MoveTowards(sweepGain, sweeping ? .18f : 0f, Time.unscaledDeltaTime);
        sweep.volume = sweepGain * Sound.Effects;
        if (sweepGain > 0f && !sweep.isPlaying && sweep.clip) sweep.Play();
        else if (sweepGain == 0f) sweep.Stop();
    }

    public void Pause(bool value)
    {
        if (paused == value) return;
        paused = value;
        foreach (var voice in new[] { effects, shots, sweep, charge })
            if (value) voice.Pause(); else voice.UnPause();
    }

    public void Stop()
    {
        sweeping = paused = false;
        sweepGain = 0f;
        next.Clear();
        foreach (var voice in new[] { effects, shots, sweep, charge }) if (voice) voice.Stop();
    }

    void OnDisable() => Stop();
}

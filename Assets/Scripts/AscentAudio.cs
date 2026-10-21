using UnityEngine;

// Dedicated flight voices keep the engine, near passes and collisions independently balanced.
[DisallowMultipleComponent]
public class AscentAudio : MonoBehaviour
{
    AudioSource engine, passing, impact;
    readonly AudioClip[] passes = new AudioClip[3], impacts = new AudioClip[3];
    bool flying, paused;
    float gain, progress, steering, lastPass = -10f;
    int passVariant = -1, impactVariant = -1;

    void Awake()
    {
        engine = Sound.Effect(gameObject, "ascent_propulsion", .22f);
        engine.loop = true;
        passing = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), .34f);
        impact = Sound.AsEffect(gameObject.AddComponent<AudioSource>(), .55f);
        foreach (var source in new[] { engine, passing, impact })
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }
        for (int i = 0; i < 3; i++)
        {
            passes[i] = Resources.Load<AudioClip>("SFX/ascent_pass_" + (i + 1));
            impacts[i] = Resources.Load<AudioClip>("SFX/ascent_impact_" + (i + 1));
        }
    }

    public void Flight(bool active, float height, float turn = 0f)
    {
        flying = active;
        progress = Mathf.Clamp01(height);
        steering = Mathf.Abs(turn);
    }

    void Update()
    {
        if (paused) return;
        gain = Mathf.MoveTowards(gain, flying ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        // Sound owns master/mute; the local envelope follows the existing effects-volume setting.
        engine.volume = .22f * Sound.Effects * gain;
        engine.pitch = Mathf.MoveTowards(engine.pitch, 1f + progress * .12f + steering * .035f, Time.unscaledDeltaTime * .2f);
        if (gain > 0f && !engine.isPlaying && engine.clip) engine.Play();
        else if (gain == 0f && engine.isPlaying) engine.Stop();
    }

    static AudioClip Next(AudioClip[] clips, ref int previous)
    {
        int index;
        do index = Random.Range(0, clips.Length); while (index == previous);
        previous = index;
        return clips[index];
    }

    public void NearMiss(float horizontalDistance)
    {
        if (!flying || paused || Time.unscaledTime - lastPass < .2f) return;
        lastPass = Time.unscaledTime;
        passing.panStereo = Mathf.Clamp(horizontalDistance / 5f, -.85f, .85f);
        passing.pitch = Random.Range(.94f, 1.06f);
        var clip = Next(passes, ref passVariant);
        if (clip) passing.PlayOneShot(clip, Mathf.Lerp(1f, .25f, Mathf.Abs(horizontalDistance) / 5.5f));
    }

    public void Hit()
    {
        if (paused) return;
        impact.pitch = Random.Range(.96f, 1.04f);
        var clip = Next(impacts, ref impactVariant);
        if (clip) impact.PlayOneShot(clip);
    }

    public void Pause(bool value)
    {
        paused = value;
        foreach (var source in new[] { engine, passing, impact })
            if (value) source.Pause(); else source.UnPause();
    }

    void OnDisable()
    {
        flying = false;
        gain = 0f;
        if (engine) engine.Stop();
        if (passing) passing.Stop();
        if (impact) impact.Stop();
    }
}

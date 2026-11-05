using UnityEngine;

// Two things the fight shows that have nothing to do with its rules: the fireball when a robot is wrecked on a rock,
// and the light every shot of ours throws about (real point lights, from a small pool: the rest stay unlit).
public partial class Combat
{
    // ---------------- a robot wrecked on a rock ----------------

    struct Puff { public Vector3 at, velocity, axis; public float born, life, size, gravity; public Batch look; }
    readonly System.Collections.Generic.List<Puff> puffs = new();
    const int MaxPuffs = 280;
    Batch crashHot, crashFire, crashSmoke, crashBits;
    readonly Color crashLight = new(1f, 0.55f, 0.2f);

    // Out of the picture nothing is drawn: it is simply gone.
    void CrashBlast(Vector3 at, float size)
    {
        Vector3 seen = Camera.main.WorldToViewportPoint(at);
        if (seen.x < -0.1f || seen.x > 1.1f || seen.y < -0.1f || seen.y > 1.1f) return;
        if (crashHot == null)
        {
            Mesh ball = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            crashHot = new Batch(new Color(1f, 0.82f, 0.42f), false, ball, glow: 4f, plain: true);
            crashFire = new Batch(new Color(1f, 0.36f, 0.07f), false, ball, glow: 2.6f, plain: true);
            crashSmoke = new Batch(new Color(0.17f, 0.14f, 0.12f), false, ball, plain: true);
            crashBits = new Batch(new Color(0.3f, 0.24f, 0.2f), false);
        }
        Vector3 heart = new Vector3(at.x, 0.6f + size * 0.3f, at.z);
        float big = Mathf.Sqrt(Mathf.Max(size, 0.5f));
        void Add(Batch look, int count, Vector2 speed, Vector2 life, Vector2 scale, float delay, float lift, float gravity)
        {
            for (int k = 0; k < count && puffs.Count < MaxPuffs; k++)
            {
                Vector3 way = Random.onUnitSphere;
                way.y = Mathf.Abs(way.y) * 0.8f + lift;
                puffs.Add(new Puff
                {
                    at = heart + Random.insideUnitSphere * 0.25f * big, velocity = way * Random.Range(speed.x, speed.y) * big, axis = Random.onUnitSphere,
                    born = Time.time + Random.Range(0f, delay), life = Random.Range(life.x, life.y), size = Random.Range(scale.x, scale.y) * big, gravity = gravity, look = look,
                });
            }
        }
        Add(crashSmoke, 7, new Vector2(1.5f, 3f), new Vector2(0.8f, 1.25f), new Vector2(1f, 1.7f), 0.14f, 0.7f, -1.5f); // the smoke comes after, rising
        Add(crashFire, 9, new Vector2(3.5f, 7f), new Vector2(0.3f, 0.5f), new Vector2(0.9f, 1.5f), 0.04f, 0.2f, 0f);
        Add(crashHot, 5, new Vector2(1.5f, 4f), new Vector2(0.14f, 0.26f), new Vector2(0.7f, 1.2f), 0f, 0.2f, 0f);
        Add(crashBits, 10, new Vector2(6f, 12f), new Vector2(0.55f, 0.95f), new Vector2(0.14f, 0.34f), 0f, 0.5f, 24f);
        Ring(new Vector3(at.x, 0.25f, at.z), 2.4f + size, crashFire);
        Burst("grenade", heart, 1f + size);
        FlashLight(heart + Vector3.up * 2f, crashLight, 5f, 0.45f);
        train.Shake(0.1f);
    }

    // Each ball swells fast, then burns away; the bits are thrown and fall.
    void DrawCrashes()
    {
        float now = Time.time, dt = Time.deltaTime;
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            Puff p = puffs[i];
            float age = (now - p.born) / p.life;
            if (age >= 1f) { puffs[i] = puffs[^1]; puffs.RemoveAt(puffs.Count - 1); continue; }
            if (age < 0f) continue;
            p.velocity += Vector3.down * (p.gravity * dt);
            if (p.look != crashBits) p.velocity *= Mathf.Exp(-3.5f * dt); // the air stops a fireball, not a stone
            p.at += p.velocity * dt;
            if (p.at.y < 0.1f) { p.at.y = 0.1f; p.velocity = new Vector3(p.velocity.x * 0.5f, -p.velocity.y * 0.3f, p.velocity.z * 0.5f); }
            puffs[i] = p;
            float scale = p.size * Mathf.Sqrt(Mathf.Min(age / 0.25f, 1f)) * (1f - age * age * age);
            p.look.Add(p.at, Vector3.one * scale, Quaternion.AngleAxis(age * 300f, p.axis));
        }
        crashSmoke?.Flush();
        crashFire?.Flush();
        crashHot?.Flush();
        crashBits?.Flush();
    }

    // ---------------- the light of our shots ----------------

    class Lamp { public Light light; public Bullet shot; public float strength, power, fade; }
    readonly Lamp[] lamps = new Lamp[28];
    Transform lampRoot;
    [System.NonSerialized] public float shotLumen = 700000f;
    const float LampRange = 11f, ShotFlash = 1.2f;

    // A weapon's light is its colour at full strength, a little washed so it lights whatever it falls on: a dark red
    // round would otherwise light only what is red.
    static Color LightOf(Color c)
    {
        float top = Mathf.Max(c.r, Mathf.Max(c.g, c.b), 0.01f);
        return Color.Lerp(new Color(c.r / top, c.g / top, c.b / top), Color.white, 0.35f);
    }

    Lamp IdleLamp()
    {
        if (!lampRoot)
        {
            lampRoot = new GameObject("ShotLights").transform;
            lampRoot.SetParent(transform, false);
            for (int i = 0; i < lamps.Length; i++)
            {
                lamps[i] = new Lamp { light = Interior.Lamp(lampRoot, Vector3.zero, Color.white, 0f, LampRange, false) };
                lamps[i].light.enabled = false;
            }
        }
        foreach (Lamp lamp in lamps) if (lamp.shot == null && lamp.strength <= 0f) return lamp;
        return null;
    }

    // A flash that dies by itself: a muzzle, a beam, a blast. Gone at once with the flashes turned off (accessibility).
    public void FlashLight(Vector3 at, Color colour, float power, float seconds)
    {
        if (!Comfort.Flashes) return;
        Lamp lamp = IdleLamp();
        if (lamp == null) return;
        lamp.strength = 1f;
        lamp.power = power;
        lamp.fade = seconds;
        lamp.light.transform.position = at;
        lamp.light.color = colour;
        lamp.light.enabled = true;
    }

    void ShotLights()
    {
        if (lampRoot == null) IdleLamp();
        // Every shot of ours in the picture takes a lamp while there is one; it dims out when the shot is gone.
        if (train.Swarming) foreach (Lamp lamp in lamps) { lamp.shot = null; lamp.strength = 0f; }
        else
        {
            Vector3 focus = train.Focus;
            foreach (Bullet b in bullets)
            {
                if (b.lamp != null || b.weapon == null || b.weapon == Weapon.Truck || (b.position - focus).sqrMagnitude > 2500f) continue;
                Lamp lamp = IdleLamp();
                if (lamp == null) break;
                b.lamp = lamp;
                lamp.shot = b;
                lamp.power = 1f;
                lamp.fade = 0.12f;
                lamp.light.color = LightOf(b.weapon.color);
                lamp.light.enabled = true;
            }
        }
        foreach (Lamp lamp in lamps)
        {
            if (lamp.shot != null)
            {
                lamp.strength = Mathf.Min(1f, lamp.strength + Time.unscaledDeltaTime * 25f);
                lamp.light.transform.position = lamp.shot.position + Vector3.up * 1.8f; // above it: a pool of light on the ground, not a hot spot
            }
            else if (lamp.strength > 0f) lamp.strength -= Time.unscaledDeltaTime / lamp.fade;
            if (lamp.strength <= 0f) { lamp.strength = 0f; if (lamp.light.enabled) lamp.light.enabled = false; continue; }
            lamp.light.intensity = shotLumen * lamp.power * lamp.strength;
        }
    }
}

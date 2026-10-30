using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The race's other mode (Story.Part.trench), the last chapter's finale: uncoupled from its wagons, the locomotive
// runs alone down a narrow trench of the alien base, as Luke's run at the Death Star. Driven as in the race, with
// triangle (or F) to shoot the base's drones flying at it; crashes and drones cost health. At the end of the trench
// the view closes in on a ventilation hatch, the sight drifts and fights the hand, and she gets two throws of the
// rock at it. In: "Yuuuuujuuuu!" and the chapter is won; out of throws, of health, she has lost.
public partial class RaceEvent
{
    public float trenchLength = 5200f, droneEvery = 2.6f, droneSpeed = 12f, droneDamage = 8f, trenchFireRate = 6f;

    bool trench, aiming, trenchWon;
    int life, throws;
    float nextDrone, trenchFireIn, throwAt = -1f;
    Vector2 sightAt, sightVelocity;
    Vector3 hatch, throwFrom, throwTo;
    Transform rock;
    RawImage sightImage;
    Material droneMat, tracerMat2;
    Mesh droneMesh;
    readonly List<(Transform body, float lane, float height, float s)> drones = new();
    readonly List<(Transform t, float until)> trenchFlashes = new();
    // The trench's dangers: turrets on the wall tops firing bolts at her, and laser gates across it to slip through.
    readonly List<(Vector3 at, int side)> turrets = new();
    readonly List<(Transform body, Vector3 velocity, float until)> bolts = new();
    readonly List<(float s, float gapAt, float gapWide, Transform beam)> lasers = new();
    readonly List<float> turretReady = new();
    float nextBlast;
    Material boltMat;

    static void Tint(Material m, Color c) => TrainSim.Tint(m, c);

    // The trench's walls are modelled (Tools/make_race_kit.py: armour plates, pipes, ribs, its bands of light), bent
    // along it by RaceEvent.Barriers. Here: glowing reactor windows, bridges over it; turrets along the tops; laser
    // gates across it with one gap to steer through. Lit in Persona 3's blues like the city race; red for danger.
    void DressTrench()
    {
        Material cyan = Glow(new Color(0.45f, 1f, 1.35f)), pink = Glow(new Color(0.2f, 0.5f, 1.5f)), lime = Glow(new Color(0.9f, 1.2f, 1.5f)), red = Glow(new Color(2.4f, 0.3f, 0.2f));
        Material plateDark = TrainSim.Mat(new Color(0.13f, 0.15f, 0.2f)), pipe = TrainSim.Mat(new Color(0.5f, 0.54f, 0.62f));
        float w = roadWidth / 2f;
        Ribbon("Centre", -0.3f, 0.3f, 0.06f, lime, 4);                                   // a dashed lit line down the middle
        turrets.Clear();
        turretReady.Clear();
        lasers.Clear();
        int k = 0;
        for (float s = 30f; s < total - 60f; s += 14f, k++)
        {
            Vector3 c = PointAt(s, out Vector3 d), right = Vector3.Cross(Vector3.up, d);
            Quaternion rot = Quaternion.LookRotation(d);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 face = c + right * side * (w + 0.6f); // the wall's upper face
                if (k % 9 == 4 + side) // a reactor window, glowing behind bars
                {
                    Block(face + Vector3.up * 13f - right * side * 0.1f, rot, new Vector3(0.2f, 4f, 7f), side < 0 ? cyan : pink);
                    for (int b = -1; b <= 1; b++) Block(face + Vector3.up * 13f + d * b * 2.2f - right * side * 0.3f, rot, new Vector3(0.3f, 4.4f, 0.4f), plateDark);
                }
                if (k % 13 == 6 + side * 3) // a turret on the wall top
                {
                    Vector3 top = c + right * side * (w + 0.6f) + Vector3.up * 22.6f;
                    Block(top, rot, new Vector3(2.4f, 1.2f, 2.4f), plateDark);
                    Block(top + Vector3.up * 0.9f - right * side * 1.2f, rot * Quaternion.Euler(0f, 90f, 0f), new Vector3(2.2f, 0.4f, 0.4f), pipe);
                    Ball(top + Vector3.up * 1.1f, 0.6f, red);
                    turrets.Add((top + Vector3.up * 0.9f, side));
                    turretReady.Add(0f);
                }
            }
            if (k % 11 == 5) // a bridge over the trench
            {
                Block(c + Vector3.up * 19f, rot, new Vector3(roadWidth + 2f, 1.6f, 4f), plateDark);
                Block(c + Vector3.up * 18.1f, rot, new Vector3(roadWidth, 0.25f, 0.6f), k % 2 == 0 ? cyan : pink);
            }
        }
        // Laser gates: a low beam across the whole trench but for one gap, somewhere new each time.
        for (float s = 420f; s < total - 300f; s += Random.Range(420f, 560f))
        {
            Vector3 c = PointAt(s, out Vector3 d), right = Vector3.Cross(Vector3.up, d);
            Quaternion rot = Quaternion.LookRotation(d);
            float gapWide = Mathf.Lerp(20f, 14f, s / total), gapAt = Random.Range(-w + gapWide * 0.5f + 2f, w - gapWide * 0.5f - 2f);
            var beam = new GameObject("LaserGate").transform;
            beam.SetParent(course, false);
            // the beam in two parts, either side of the gap; posts at the gap's edges
            float left0 = -w, left1 = gapAt - gapWide / 2f, right0 = gapAt + gapWide / 2f, right1 = w;
            Block(beam, c + right * (left0 + left1) / 2f + Vector3.up * 1.6f, rot, new Vector3(left1 - left0, 2.2f, 0.3f), red);
            Block(beam, c + right * (right0 + right1) / 2f + Vector3.up * 1.6f, rot, new Vector3(right1 - right0, 2.2f, 0.3f), red);
            foreach (float x in new[] { left1, right0 }) Block(beam, c + right * x + Vector3.up * 2.5f, rot, new Vector3(0.6f, 5f, 0.6f), plateDark);
            Block(beam, c + right * gapAt + Vector3.up * 0.06f, rot, new Vector3(gapWide, 0.05f, 6f), lime); // the gap, lit on the floor
            lasers.Add((s, gapAt, gapWide, beam));
        }
        boltMat = Glow(new Color(2.6f, 0.25f, 0.15f));
        boltCore ??= Glow(new Color(4f, 3.6f, 3.4f));
    }

    // A bolt of plasma along its way: a red shell round a white-hot core, both drawn out long (capsules).
    Material boltCore;
    Transform Plasma(Vector3 at, Vector3 way, Material shell, float thick, float length)
    {
        var bolt = new GameObject("Bolt").transform;
        bolt.SetParent(course, false);
        bolt.SetPositionAndRotation(at, Quaternion.LookRotation(way));
        foreach (var (m, size) in new[] { (shell, new Vector3(thick, thick, length)), (boltCore, new Vector3(thick * 0.42f, thick * 0.42f, length * 0.72f)) })
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(bolt, false);
            part.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // a capsule stands along its y
            part.transform.localScale = new Vector3(size.x, size.z * 0.5f, size.y);
            var look = part.GetComponent<Renderer>();
            look.sharedMaterial = m;
            look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return bolt;
    }

    static Material Glow(Color c)
    {
        Material m = TrainSim.Mat(c, false);
        m.SetInt("_Culling", 0);
        return m;
    }

    // The turrets fire at where she will be; the laser gates crash her unless she is through the gap; and the
    // base blows up round her as she goes, for the show.
    void Dangers(float now, float dt)
    {
        for (int i = 0; i < turrets.Count && me.progress > 200f; i++) // a quiet first stretch to get going in
        {
            Vector3 to = me.body.position - turrets[i].at;
            float along = Vector3.Dot(to, dirs[Mathf.Clamp(me.seg, 0, dirs.Count - 1)]);
            if (along > -20f || along < -150f || now < turretReady[i]) continue; // ahead of her, in range
            turretReady[i] = now + Random.Range(2.8f, 4.4f);
            Vector3 lead = me.body.position + Quaternion.Euler(0f, me.way, 0f) * Vector3.forward * me.speed * 0.9f + Vector3.up * 1.5f;
            Vector3 way = (lead - turrets[i].at).normalized * 75f;
            Burst(turrets[i].at, now, FireMats[4], 1.4f); // the muzzle flash
            bolts.Add((Plasma(turrets[i].at, way, boltMat, 1.1f, 4.2f), way, now + 4f));
        }
        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            var b = bolts[i];
            b.body.position += b.velocity * dt;
            b.body.GetChild(1).localScale = new Vector3(0.45f, 0.45f, 3f) * (1f + 0.15f * Mathf.Sin(now * 40f + i)); // the core, fizzing
            if (Random.value < 0.6f) flares.Add((b.body.position - b.velocity.normalized * 2f, -b.velocity * 0.05f + Random.insideUnitSphere * 3f, now, now + 0.25f, 0.35f, FireMats[4])); // embers trailing it
            if ((b.body.position - (me.body.position + Vector3.up * 1.5f)).magnitude < 3.2f) { TrenchHurt(5); Fireball(b.body.position, now); Destroy(b.body.gameObject); bolts.RemoveAt(i); continue; }
            if (now > b.until || b.body.position.y < 0f) { Destroy(b.body.gameObject); bolts.RemoveAt(i); }
        }
        for (int i = lasers.Count - 1; i >= 0; i--)
        {
            var l = lasers[i];
            float pulse = 1f + 0.06f * Mathf.Sin(now * 14f + i);
            l.beam.GetChild(0).localScale = new Vector3(l.beam.GetChild(0).localScale.x, 2.2f * pulse, 0.3f);
            l.beam.GetChild(1).localScale = new Vector3(l.beam.GetChild(1).localScale.x, 2.2f * pulse, 0.3f);
            if (me.progress < l.s) continue;
            // just crossed it: through the gap, or into the beam
            Vector3 c = PointAt(l.s, out Vector3 d);
            float side = Vector3.Dot(me.pos - c, Vector3.Cross(Vector3.up, d));
            if (Mathf.Abs(side - l.gapAt) > l.gapWide / 2f - 1.5f && now >= me.recoverUntil) Crash(me, now);
            lasers.RemoveAt(i);
        }
        if (now >= nextBlast) // the base going up round her
        {
            nextBlast = now + Random.Range(0.5f, 1.1f);
            Vector3 c = PointAt(me.progress + Random.Range(40f, 140f), out Vector3 d);
            float side = Random.value < 0.5f ? -1f : 1f;
            Fireball(c + Vector3.Cross(Vector3.up, d) * side * (roadWidth / 2f - 1f) + Vector3.up * Random.Range(4f, 20f), now);
        }
    }

    // The trench run: as a race, alone, down the trench.
    public void BeginTrench(System.Action<bool> done)
    {
        trench = true;
        Begin(done);
    }

    // The end of the trench: a wall across it, and in it the ventilation hatch, glowing, ringed with warning light.
    void BuildVent()
    {
        DressTrench();
        Vector3 end = points[^1], fwd = dirs[^1];
        Quaternion look = Quaternion.LookRotation(fwd);
        Material wall = TrainSim.Mat(new Color(0.3f, 0.34f, 0.38f)), ring = TrainSim.Mat(new Color(3f, 0.6f, 0.2f), false), core = TrainSim.Mat(new Color(0.4f, 2.8f, 0.6f), false), dark = TrainSim.Mat(new Color(0.05f, 0.05f, 0.07f));
        Block(end + fwd * 40f + Vector3.up * 11f, look, new Vector3(roadWidth + 4f, 22f, 4f), wall);
        hatch = end + fwd * 37.8f + Vector3.up * 7f;
        Block(hatch, look * Quaternion.Euler(0f, 0f, 45f), new Vector3(6.4f, 6.4f, 0.2f), ring);
        Block(hatch - fwd * 0.1f, look * Quaternion.Euler(0f, 0f, 45f), new Vector3(5.2f, 5.2f, 0.2f), dark);
        Block(hatch - fwd * 0.2f, look, new Vector3(2.4f, 2.4f, 0.2f), core);
        for (int k = 1; k <= 6; k++) // chevrons on the floor pointing at it
        {
            Vector3 c = PointAt(total - k * 25f, out Vector3 d);
            Vector3 right = Vector3.Cross(Vector3.up, d);
            for (int s = -1; s <= 1; s += 2) Block(c + right * s * 3.4f + Vector3.up * 0.07f, Quaternion.LookRotation(d) * Quaternion.Euler(0f, -s * 38f, 0f), new Vector3(1.1f, 0.05f, 10f), ring);
        }
    }

    void TrenchBegin()
    {
        life = 100;
        throws = 2;
        aiming = trenchWon = false;
        throwAt = -1f;
        foreach (var d in drones) if (d.body) Destroy(d.body.gameObject);
        drones.Clear();
        foreach (var b in bolts) if (b.body) Destroy(b.body.gameObject);
        bolts.Clear();
        nextBlast = Time.unscaledTime + 2f;
        nextDrone = Time.unscaledTime + 6f;
        droneMesh = Resources.Load<Mesh>("Models/Enemy_warden");
        droneMat ??= TrainSim.Mat(Color.white);
        droneMat.SetFloat("_MVCOL", 1f);
        tracerMat2 ??= TrainSim.Mat(new Color(0.6f, 1.6f, 3f), false);
        if (!sightImage)
        {
            sightImage = UiKit.Picture(board.transform, "sight", Vector2.zero, 150f); // the gunner's scope (SwarmEvent)
        }
        sightImage.gameObject.SetActive(false);
        standings.text = "";
    }

    void TrenchHurt(int amount)
    {
        if (aiming || endsAt > 0f) return;
        life -= amount;
        shake = Mathf.Max(shake, 0.4f);
        Sound.Play("wagonbad");
        if (life <= 0) TrenchOver(false, "trench.down");
    }

    void TrenchOver(bool won, string key)
    {
        trenchWon = won;
        message.text = L10n.T(key);
        endsAt = Time.unscaledTime + (won ? 3.2f : 2.4f);
        if (won) Sound.Play("newcard");
    }

    void TrenchUpdate(float now, float dt)
    {
        for (int i = trenchFlashes.Count - 1; i >= 0; i--)
        {
            if (!trenchFlashes[i].t || now > trenchFlashes[i].until) { if (trenchFlashes[i].t) Destroy(trenchFlashes[i].t.gameObject); trenchFlashes.RemoveAt(i); continue; }
            Vector3 sc = trenchFlashes[i].t.localScale;
            trenchFlashes[i].t.localScale = new Vector3(sc.x * (1f - 9f * dt), sc.y * (1f - 9f * dt), sc.z); // the beam thinning out
        }
        place.text = Mathf.Max(0, life).ToString(); // her health, big where the place goes, its word small beside it
        placeOf.text = L10n.T("gates.life", "").Trim();
        standings.text = "";
        if (endsAt > 0f) { if (aiming) AimCamera(dt); return; }
        if (!aiming && me.progress >= total - 32f) { aiming = true; me.speed = 0f; sightAt = new Vector2(Screen.width * 0.35f, Screen.height * 0.35f); sightVelocity = Vector2.zero; message.text = L10n.T("trench.aim"); }
        if (aiming) { me.speed = 0f; Aim(now, dt); return; }
        Drones(now, dt);
        Dangers(now, dt);
        bool fire = (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed) || (Keyboard.current != null && Keyboard.current.fKey.isPressed) || (Mouse.current != null && Mouse.current.rightButton.isPressed);
        if (fire && (trenchFireIn -= dt) <= 0f) { trenchFireIn = 1f / trenchFireRate; Fire(now); }
    }

    // The base's drones: ahead in the trench, flying at her.
    void Drones(float now, float dt)
    {
        if (now >= nextDrone && me.progress < total - 260f)
        {
            nextDrone = now + droneEvery * Random.Range(0.7f, 1.3f);
            var go = new GameObject("Drone");
            go.transform.SetParent(course, false);
            go.AddComponent<MeshFilter>().sharedMesh = droneMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = droneMat;
            go.transform.localScale = Vector3.one * 3f;
            drones.Add((go.transform, Random.Range(-roadWidth * 0.38f, roadWidth * 0.38f), Random.Range(2.5f, 7f), me.progress + Random.Range(170f, 260f)));
        }
        for (int i = drones.Count - 1; i >= 0; i--)
        {
            var d = drones[i];
            float s = d.s - droneSpeed * dt;
            float lane = Mathf.MoveTowards(d.lane, Vector3.Dot(me.pos - PointAt(s, out _), Vector3.Cross(Vector3.up, dirs[Mathf.Clamp((int)(s / Step), 0, dirs.Count - 1)])), 3f * dt); // drifting into her lane
            drones[i] = (d.body, lane, d.height, s);
            Vector3 at = PointAt(s, out Vector3 dir) + Vector3.Cross(Vector3.up, dir) * lane + Vector3.up * (d.height + Mathf.Sin(now * 3f + i) * 0.5f);
            d.body.position = at;
            d.body.rotation = Quaternion.LookRotation(-dir);
            if ((at - (me.body.position + Vector3.up * 2f)).magnitude < 4.5f) { TrenchHurt(Mathf.RoundToInt(droneDamage)); Pop(i, now); continue; }
            if (s < me.progress - 30f) { Destroy(d.body.gameObject); drones.RemoveAt(i); }
        }
    }

    void Pop(int i, float now)
    {
        Fireball(drones[i].body.position, now);
        Destroy(drones[i].body.gameObject);
        drones.RemoveAt(i);
    }

    // Triangle: whatever drone is nearest straight ahead, within a narrow cone, goes down.
    void Fire(float now)
    {
        Vector3 from = me.body.position + Vector3.up * 2.4f, fwd = me.body.forward;
        int best = -1;
        float bestAt = 180f;
        for (int i = 0; i < drones.Count; i++)
        {
            Vector3 to = drones[i].body.position - from;
            if (to.magnitude < bestAt && Vector3.Angle(new Vector3(fwd.x, 0f, fwd.z), new Vector3(to.x, 0f, to.z)) < 9f) { best = i; bestAt = to.magnitude; }
        }
        Vector3 end = best >= 0 ? drones[best].body.position : from + fwd * 150f;
        // Her shot: a beam of her blue round a white core, thinning out over a tenth of a second; a flash at the gun
        // and, where it lands, a burst of blue.
        var beam = new GameObject("Shot").transform;
        beam.SetParent(course, false);
        beam.SetPositionAndRotation((from + end) * 0.5f, Quaternion.LookRotation(end - from));
        Block(beam, beam.position, beam.rotation, new Vector3(0.5f, 0.5f, (end - from).magnitude), tracerMat2);
        Block(beam, beam.position, beam.rotation, new Vector3(0.16f, 0.16f, (end - from).magnitude), boltCore);
        trenchFlashes.Add((beam, now + 0.1f));
        Burst(from + fwd * 2f, now, FireMats[5], 0.7f);
        if (best >= 0) { Burst(end, now, FireMats[5], 1.3f); Pop(best, now); }
    }

    // At the hatch: the sight drifts and answers the hand sluggishly; one press throws the rock where it is.
    void Aim(float now, float dt)
    {
        AimCamera(dt);
        sightImage.gameObject.SetActive(throwAt < 0f);
        if (throwAt >= 0f) { Throwing(now); return; }
        Vector2 stick = move.ReadValue<Vector2>();
        Vector2 mouse = Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.6f : Vector2.zero;
        Vector2 drift = new Vector2(Mathf.PerlinNoise(now * 0.7f, 1f) - 0.5f, Mathf.PerlinNoise(2f, now * 0.7f) - 0.5f) * 900f;
        sightVelocity += (stick * 900f + mouse * 30f + drift) * dt;
        sightVelocity *= 1f - 1.2f * dt; // heavy: it keeps going
        sightAt += sightVelocity * dt;
        sightAt = new Vector2(Mathf.Clamp(sightAt.x, 0f, Screen.width), Mathf.Clamp(sightAt.y, 0f, Screen.height));
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)board.transform, sightAt, null, out Vector2 local);
        sightImage.rectTransform.anchoredPosition = local;
        bool press = (Gamepad.current != null && (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame))
                     || (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                     || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!press) return;
        // the rock goes where the sight was, onto the end wall
        Ray ray = view.ScreenPointToRay(sightAt);
        Plane wallPlane = new Plane(-dirs[^1], hatch);
        throwTo = wallPlane.Raycast(ray, out float at) ? ray.GetPoint(at) : hatch + Vector3.up * 8f;
        throwFrom = view.transform.position + view.transform.forward * 1.5f - Vector3.up * 0.8f;
        throwAt = now;
        rock = Ball(throwFrom, 0.9f, TrainSim.Mat(new Color(0.9f, 0.85f, 0.4f)));
        Sound.Play("click");
    }

    void Throwing(float now)
    {
        float u = (now - throwAt) / 0.9f;
        rock.position = Vector3.Lerp(throwFrom, throwTo, u) + Vector3.up * Mathf.Sin(Mathf.Min(1f, u) * Mathf.PI) * 3f;
        rock.Rotate(400f * Time.unscaledDeltaTime, 260f * Time.unscaledDeltaTime, 0f);
        if (u < 1f) return;
        throwAt = -1f;
        Destroy(rock.gameObject);
        bool inside = (throwTo - hatch).magnitude < 2.2f; // into the hatch's middle
        Fireball(throwTo, now);
        if (inside)
        {
            for (int k = 0; k < 4; k++) Fireball(hatch + Random.insideUnitSphere * 4f, now);
            shake = 1f;
            TrenchOver(true, "trench.won");
            return;
        }
        throws--;
        message.text = L10n.T(throws > 0 ? "trench.miss" : "trench.missed");
        if (throws <= 0) TrenchOver(false, "trench.missed");
    }

    // Close in on the hatch, from the cab, the wall filling the picture.
    void AimCamera(float dt)
    {
        Vector3 want = hatch - dirs[^1] * 24f + Vector3.up * 1.5f;
        view.transform.position = Vector3.Lerp(view.transform.position, want, 1f - Mathf.Exp(-4f * dt));
        view.transform.rotation = Quaternion.Slerp(view.transform.rotation, Quaternion.LookRotation(hatch - view.transform.position), 1f - Mathf.Exp(-4f * dt));
        view.fieldOfView = Mathf.Lerp(view.fieldOfView, 40f, 3f * dt);
        if (shake > 0f) view.transform.position += Random.insideUnitSphere * shake * 0.4f;
    }

    Transform Block(Transform parent, Vector3 at, Quaternion turn, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(at, turn);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }
}

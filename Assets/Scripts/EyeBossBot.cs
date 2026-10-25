using System.Collections.Generic;
using UnityEngine;

// A test bot for the eye boss, like AutoPlay for the ride: set Botting from the editor and it plays the fight in
// place of the player (runs from the marks, jumps the sweep, ships, waves and gaps, takes the turret, aims with a
// hand's speed at the orbs and the open eyes) and notes in BotLog how each fight went and what hurt it. Its timing
// is jittered so it misses now and then, as a player would. Not part of the game.
public partial class EyeBossEvent
{
    public static bool Botting;
    public static readonly System.Text.StringBuilder BotLog = new();
    readonly Dictionary<string, int> hurtBy = new();
    float botX, botStarted, botShut;
    bool botJump;
    readonly List<int> shutPerSeat = new();

    Phase botLast;

    // what: the method the hurt came from (Ground a fall, Ships, Beams, Sweep, Hazards a patch or a wave, Gunner an orb)
    void BotNote(int halves, string what)
    {
        hurtBy.TryGetValue(what, out int n);
        hurtBy[what] = n + halves;
    }

    void BotBegin() { hurtBy.Clear(); shutPerSeat.Clear(); botStarted = Time.unscaledTime; }

    void BotEnd(bool won)
    {
        var why = new System.Text.StringBuilder();
        foreach (var pair in hurtBy) why.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
        BotLog.AppendLine((won ? "WON " : "LOST ") + (Time.unscaledTime - botStarted).ToString("0") + "s attacks=" + attack + " halvesLeft=" + left +
            " eyesLeft=" + OpenEyes + " shutPerSeat=" + string.Join(",", shutPerSeat) + " hurt: " + why);
    }

    float Metres(float a) => Mathf.DeltaAngle(angle * Mathf.Rad2Deg, a * Mathf.Rad2Deg) * Mathf.Deg2Rad * ringRadius; // signed, from her

    void BotThink(float now)
    {
        botX = 0f;
        botJump = false;
        if (phase != botLast)
        {
            if (phase == Phase.Gunner) botShut = eyes.Count - OpenEyes;
            if (botLast == Phase.Gunner) shutPerSeat.Add(eyes.Count - OpenEyes - (int)botShut);
            botLast = phase;
        }
        if (phase == Phase.Gunner) { BotAim(now); return; }
        // Where to stand: the nearest spot within a few steps clear of the beams' marks, the burning patches and
        // the wagons the rocks are coming down on, leaning toward the turret when it is up.
        float bestAt = 0f, bestCost = float.MaxValue;
        for (float d = -8f; d <= 8f; d += 0.5f)
        {
            float a = angle + d / ringRadius, cost = Mathf.Abs(d) * 0.05f;
            if (!Solid(a)) cost += 5f;
            foreach (Beam b in beams)
                if (now < b.fireAt + 0.05f && Mathf.Abs(Metres(Mathf.Atan2(b.to.z - Arena.z, b.to.x - Arena.x)) - d) < 2.5f) cost += 10f;
            foreach (Hazard h in hazards)
                if (!h.wave && Mathf.Abs(Metres(h.at) - d) < 1.3f) cost += 6f;
            int car = CarAt(a);
            if (rocks.Exists(r => r.car == car)) cost += 8f;
            if (phase == Phase.Turret) cost += Mathf.Abs(Metres(turretAngle) - d) * 0.3f;
            if (cost < bestCost) { bestCost = cost; bestAt = d; }
        }
        if (Mathf.Abs(bestAt) > 0.4f) botX = Mathf.Sign(bestAt);
        if (height > 0f || fallUntil > 0f) return;
        float look = Random.Range(0.12f, 0.5f); // how far ahead it sees a thing coming: off now and then
        if (botX != 0f && !Solid(angle + botX * 1.4f / ringRadius)) botJump = true;
        foreach (Hazard h in hazards) // a burning patch in the way: over it
            if (!h.wave && botX != 0f && Metres(h.at) * botX > 0f && Mathf.Abs(Metres(h.at)) < 1.6f) botJump = true;
        float her = -angle * Mathf.Rad2Deg + 90f;
        foreach (Transform s in new[] { sweep, sweep2 })
            if (s.gameObject.activeSelf && Mathf.Abs(Mathf.DeltaAngle(s.localEulerAngles.y, her)) < sweepSpeed * (flipAt > 0f && now < flipAt ? 0.25f : 1f) * look + 3f) botJump = true;
        foreach (var f in fleet)
            if (Mathf.Abs(Metres(f.angle)) < 1.1f + (shipSpeed + Mathf.Abs(spin) * ringRadius) * look) botJump = true;
        foreach (Hazard h in hazards)
            if (h.wave && h.body.gameObject.activeSelf && Mathf.Abs(Metres(h.at + h.way * 9f * (now - h.from) / ringRadius)) < 0.9f + 9f * look) botJump = true;
    }

    // In the seat: the orb nearest to landing first, else the open eye nearest the sights; turned to at a hand's speed.
    void BotAim(float now)
    {
        Vector3 seat = turret.position + Vector3.up * 2.2f;
        Quaternion ahead = Quaternion.LookRotation(boss.position - seat);
        Vector3 target = Vector3.zero;
        bool any = false;
        float soonest = 0.35f;
        foreach (Orb o in orbs)
        {
            float u = Mathf.InverseLerp(o.from0, o.land, now);
            if (u > soonest) { soonest = u; target = o.body.position; any = true; }
        }
        if (!any)
        {
            float bestTurn = float.MaxValue;
            foreach (Eye e in eyes)
            {
                if (e.Shut || e.lid.gameObject.activeSelf) continue;
                float turn = Vector3.Angle(view.transform.forward, e.root.position - seat);
                if (Vector3.Dot(e.normal, boss.InverseTransformDirection(seat - boss.position).normalized) < 0.1f) continue; // round the back of the ball
                if (turn < bestTurn) { bestTurn = turn; target = e.root.position; any = true; }
            }
        }
        if (!any) return;
        Vector3 local = Quaternion.Inverse(ahead) * (target - seat);
        float wantYaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, wantPitch = -Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
        float step = 110f * Mathf.Min(Time.unscaledDeltaTime, 0.05f); // a player's hand, not a machine's
        yaw = Mathf.Clamp(Mathf.MoveTowards(yaw, wantYaw + Random.Range(-2.5f, 2.5f), step), -45f, 45f);
        pitch = Mathf.Clamp(Mathf.MoveTowards(pitch, wantPitch + Random.Range(-2.5f, 2.5f), step), -35f, 30f);
    }
}

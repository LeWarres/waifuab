using UnityEngine;

// A test bot for the forest finale, like EyeBossBot for the eye boss: set Botting from the editor and it plays in the
// player's place at BotSpeed times the pace, going for the nearest drawing (a pinned one, or round behind a child),
// stepping out of any torch it is about to walk into, running only when no child is near, and with a player's
// slowness to react. It notes in BotLog how each night went. Not part of the game.
public partial class ForestChase
{
    public static bool Botting;
    public const int BotSpeed = 4;
    public static readonly System.Text.StringBuilder BotLog = new();
    float botYaw, botThink, botSeen, botPanic, botStuckAt;
    Vector2 botMove;
    bool botSprint, botTake;
    Vector3 botWant, botLastSpot, botDodge;

    // Would a child's torch light this spot?
    bool Lit(Vector3 at, float margin)
    {
        foreach (var t in thieves)
        {
            Vector3 to = at - t.root.position; to.y = 0f;
            float range = Range;
            if (to.magnitude < range + margin && Vector3.Angle(t.root.forward, to) < t.torch.spotAngle * .5f + margin * 6f && Clear(t.root.position, at)) return true;
            if (to.magnitude < 1.4f) return true;
        }
        return false;
    }

    void BotThink(float dt)
    {
        botTake = false;
        botThink -= dt;
        if (botThink <= 0f)
        {
            botThink = Random.Range(.3f, .55f); // how quickly a player takes in what changed (and they only half watch the torches)
            Vector3 me = player.position;
            // the nearest drawing: one on a tree, or the spot just behind a child that carries one
            Vector3 goal = me; float best = float.MaxValue; bool child = false;
            foreach (var paper in onTrees) { float d = Vector3.Distance(me, paper.position); if (d < best) { best = d; goal = paper.position; child = false; } }
            foreach (var t in thieves)
            {
                if (!t.carrying || now < t.scaredUntil) continue;
                Vector3 behind = t.root.position - t.root.forward * 2f;
                float d = Vector3.Distance(me, behind);
                if (d < best) { best = d; goal = behind; child = true; }
            }
            Vector3 way = goal - me; way.y = 0f;
            // the first of a fan of headings, nearest the goal's, that does not walk into a torch
            Vector3 pick = way.normalized;
            if (Lit(me, 0f))
            {
                // already in the light: straight out of it, sideways to the torch
                foreach (var t in thieves)
                {
                    Vector3 to = me - t.root.position; to.y = 0f;
                    if (to.magnitude < 16f) pick = Vector3.Cross(Vector3.up, t.root.forward) * Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, t.root.forward), to));
                }
            }
            else
                foreach (float turn in new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f })
                {
                    Vector3 h = Quaternion.Euler(0f, turn, 0f) * way.normalized;
                    if (!Lit(me + h * 2f, 0f)) { pick = h; break; }
                }
            // stuck on a trunk: a few steps to one side
            if (now > botStuckAt + 1f)
            {
                if ((me - botLastSpot).magnitude < .6f && way.magnitude > 2f) botDodge = Quaternion.Euler(0f, Random.value < .5f ? 90f : -90f, 0f) * pick;
                else botDodge = Vector3.zero;
                botLastSpot = me; botStuckAt = now;
            }
            if (botDodge != Vector3.zero) pick = botDodge;
            botWant = pick;
            bool near = false;
            foreach (var t in thieves) if (Vector3.Distance(me, t.root.position) < 9f) near = true;
            botSprint = !near && way.magnitude > 8f;
            botTake = way.magnitude < (child ? 1.2f : 2.2f) && !Lit(me, 0f);
        }
        if (botWant.sqrMagnitude > .01f) botYaw = Mathf.MoveTowardsAngle(botYaw, Mathf.Atan2(botWant.x, botWant.z) * Mathf.Rad2Deg, 400f * dt);
        botMove = new Vector2(0f, 1f);
    }

    void BotEnd(bool won)
    {
        BotLog.AppendLine((won ? "WON " : panic >= 1f ? "LOST(panic) " : "LOST(dawn) ") + "drawings " + recovered + "/" + Drawings + " time " + (seconds - remaining).ToString("0") +
            "s seen " + botSeen.ToString("0.0") + "s panic peak " + Mathf.RoundToInt(botPanic * 100f) + "%");
    }
}

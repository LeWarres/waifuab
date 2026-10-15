using UnityEngine;

// The Masamune wagon's swordsman (Turret): a character held in a samurai's stance, low and wide, the right hand on
// the hilt in front of the left hip and the blade back along it, as if still in its scabbard. Cut() is an iai draw:
// one horizontal stroke across his front from the left to the right, both hands on the hilt, the hips turning
// with it; he holds the finish a moment, then brings the sword back to his hip. Every frame the bones start from
// how Player.Stand left them and are pointed again: feet planted on the roof, hands on the hilt.
public class Samurai : MonoBehaviour
{
    Transform owner, blade, hips, spine, head;
    Transform[] arm, leg; // upper, lower, end: left then right
    Transform[] bones;
    Quaternion[] rest;
    Vector3 bodyRest;
    float cutAt = -99f;

    // owner: the turret (he faces its +z); rig: his bones, as Player.Stand posed them; sword: the katana, held here.
    public void Init(Transform owner, Animator rig, Transform sword)
    {
        this.owner = owner;
        blade = sword;
        Transform Bone(HumanBodyBones b) => rig.GetBoneTransform(b);
        hips = Bone(HumanBodyBones.Hips);
        spine = Bone(HumanBodyBones.Chest) ?? Bone(HumanBodyBones.Spine);
        head = Bone(HumanBodyBones.Head);
        arm = new[] { Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.LeftLowerArm), Bone(HumanBodyBones.LeftHand),
                      Bone(HumanBodyBones.RightUpperArm), Bone(HumanBodyBones.RightLowerArm), Bone(HumanBodyBones.RightHand) };
        leg = new[] { Bone(HumanBodyBones.LeftUpperLeg), Bone(HumanBodyBones.LeftLowerLeg), Bone(HumanBodyBones.LeftFoot),
                      Bone(HumanBodyBones.RightUpperLeg), Bone(HumanBodyBones.RightLowerLeg), Bone(HumanBodyBones.RightFoot) };
        bones = hips.GetComponentsInChildren<Transform>();
        rest = System.Array.ConvertAll(bones, b => b.localRotation);
        bodyRest = transform.localPosition;
    }

    public void Cut() => cutAt = Time.time;

    // Points in his own frame: x to his right, y up from the roof, z the way he faces.
    Vector3 At(Vector3 local) => owner.TransformPoint(local);
    Vector3 Way(Vector3 local) => owner.TransformDirection(local).normalized;
    static Vector3 Flat(float yaw) => new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));

    void LateUpdate()
    {
        if (!owner) return;
        for (int i = 0; i < bones.Length; i++) bones[i].localRotation = rest[i];

        // Where the stroke is: yaw of the blade (0 straight ahead, + to his right) and how far into it he is
        // (draw 0: hand on the hilt at the hip, 1: arms out with the blade).
        float t = Time.time - cutAt, yaw, draw;
        const float Sheathed = -150f, Finish = 110f;
        if (t < 0.2f) { draw = Mathf.SmoothStep(0f, 1f, t / 0.06f); yaw = Mathf.Lerp(Sheathed, Finish, Mathf.SmoothStep(0f, 1f, t / 0.2f)); }
        else if (t < 0.55f) { draw = 1f; yaw = Finish; }                                   // zanshin: the finish, held
        else if (t < 1.1f)
        {
            float k = (t - 0.55f) / 0.55f;
            yaw = Mathf.Lerp(Finish, Sheathed, Mathf.SmoothStep(0f, 1f, k));                // and back into the scabbard
            draw = 1f - Mathf.SmoothStep(0f, 1f, (k - 0.4f) / 0.6f);
        }
        else { draw = 0f; yaw = Sheathed; }

        // Low and wide, breathing; lower still in the stroke, the hips and chest turning with the blade.
        float crouch = 0.2f + 0.08f * draw + Mathf.Sin(Time.time * 2f) * 0.012f;
        transform.localPosition = bodyRest + Vector3.down * crouch;
        Turn(hips, 10f + 6f * draw, yaw * 0.18f * draw - 12f * (1f - draw));
        Turn(spine, 4f, yaw * 0.15f * draw);

        // Feet planted wide on the roof, left forward, right back; knees out over the toes.
        Reach(leg[0], leg[1], leg[2], At(new Vector3(-0.42f, 0.08f, 0.38f)), Way(new Vector3(-0.4f, 0f, 1f)));
        Reach(leg[3], leg[4], leg[5], At(new Vector3(0.38f, 0.08f, -0.42f)), Way(new Vector3(0.6f, 0f, 0.5f)));

        // The hilt: at rest in front of the left hip, blade back and down; in the stroke out at chest height along it.
        Vector3 restHilt = new Vector3(-0.2f, 1.0f - crouch, 0.28f), restBlade = new Vector3(-0.35f, -0.3f, -0.9f);
        Vector3 cutHilt = new Vector3(0f, 1.3f - crouch, 0f) + Flat(yaw) * 0.6f, cutBlade = Flat(yaw) + Vector3.down * 0.05f;
        Vector3 hilt = Vector3.Lerp(restHilt, cutHilt, draw), along = Vector3.Slerp(restBlade.normalized, cutBlade.normalized, draw);
        Vector3 hiltAt = At(hilt), bladeWay = Way(along);
        Reach(arm[3], arm[4], arm[5], hiltAt, Way(new Vector3(0.6f, -1f, -0.2f)));                           // right hand on the hilt
        Reach(arm[0], arm[1], arm[2], hiltAt - bladeWay * 0.16f * owner.lossyScale.x, Way(new Vector3(-0.6f, -1f, -0.2f))); // left just behind it
        if (head) head.rotation = Quaternion.FromToRotation(Way(Vector3.forward), Way(Flat(yaw * 0.25f * draw))) * head.rotation; // eyes after the blade

        blade.position = arm[5].position;
        blade.rotation = Quaternion.LookRotation(bladeWay, Way(Vector3.up));
    }

    // Lean forward (degrees about his right) and twist (about up) a bone, in his frame.
    void Turn(Transform bone, float lean, float twist)
    {
        if (!bone) return;
        Quaternion frame = owner.rotation;
        bone.rotation = frame * Quaternion.Euler(lean, twist, 0f) * Quaternion.Inverse(frame) * bone.rotation;
    }

    // Two bones reaching for a point: the middle joint bends towards hint.
    static void Reach(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint)
    {
        if (!upper || !lower || !end) return;
        Vector3 s = upper.position;
        float a = (lower.position - s).magnitude, b = (end.position - lower.position).magnitude;
        Vector3 to = target - s;
        float d = Mathf.Clamp(to.magnitude, 0.01f, a + b - 0.001f);
        Vector3 dir = to.normalized;
        float x = (a * a - b * b + d * d) / (2f * d), h = Mathf.Sqrt(Mathf.Max(0f, a * a - x * x));
        Vector3 bend = Vector3.ProjectOnPlane(hint, dir).normalized;
        Aim(upper, lower.position, s + dir * x + bend * h);
        Aim(lower, end.position, s + dir * d);
    }

    static void Aim(Transform bone, Vector3 child, Vector3 want) =>
        bone.rotation = Quaternion.FromToRotation(child - bone.position, want - bone.position) * bone.rotation;
}

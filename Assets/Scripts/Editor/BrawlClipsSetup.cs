using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Fills Resources/Brawl/BrawlClips.asset with the clips BrawlEvent plays, picked out of the two animation packs.
// Run again after changing the picks: Tools > Brawl > Build Clips.
public static class BrawlClipsSetup
{
    const string Fighter = "Assets/Grruzam_Fighter_Animation/M_Fighter/";
    const string Wrestler = "Assets/Wrestler_Finishers_Volume2/Art/Animations/Wrestling_";

    static AnimationClip Clip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (!clip) Debug.LogError("BrawlClips: no clip in " + path);
        return clip;
    }

    static AnimationClip F(string path) => Clip(Fighter + path + ".FBX");
    static AnimationClip W(string name) => Clip(Wrestler + name + ".fbx");

    // A wrestler clip set up to play once or loop; paired: set up as the pack's own demo has them (root position and
    // turn baked into the pose, based on the centre of mass), so the demo's spacing of the two bodies lines them up.
    static AnimationClip W(string name, bool loop, bool paired = false)
    {
        string path = Wrestler + name + ".fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
        bool changed = false;
        foreach (ModelImporterClipAnimation c in clips)
        {
            if (c.loopTime == loop && (!paired || (c.lockRootPositionXZ && !c.keepOriginalPositionXZ && c.lockRootRotation && c.keepOriginalOrientation))) continue;
            changed = true;
            c.loopTime = loop;
            if (!paired) continue;
            c.lockRootRotation = c.keepOriginalOrientation = true;
            c.lockRootHeightY = c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true;
            c.keepOriginalPositionXZ = false;
        }
        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
        return Clip(path);
    }

    [MenuItem("Tools/Brawl/Build Clips")]
    public static void Run()
    {
        const string asset = "Assets/Resources/Brawl/BrawlClips.asset";
        Directory.CreateDirectory("Assets/Resources/Brawl");
        var clips = AssetDatabase.LoadAssetAtPath<BrawlClips>(asset);
        if (!clips) AssetDatabase.CreateAsset(clips = ScriptableObject.CreateInstance<BrawlClips>(), asset);
        clips.idle = F("1_Movements/1__Idle/Fighter@Idle");
        clips.run = F("1_Movements/4__Run/Inplace/Fighter@Run");
        clips.dodge = F("1_Movements/8__Dodge/Inplace/Fighter@Dodge_Fwd");
        clips.heavy = F("2_Attacks/0__Melee_Attack/Inplace/Fighter@Attack_Right_High_Kick_A");
        clips.hit = F("4_Damages/1__Front/Fighter@Damage_Front_Small_ver_A");
        clips.down = F("4_Damages/6__Die/Fighter@Damage_Die");
        clips.combo = new[]
        {
            F("2_Attacks/1__3Combos/Inplace/Fighter@Attack_3combo_1"),
            F("2_Attacks/1__3Combos/Inplace/Fighter@Attack_3combo_2"),
            F("2_Attacks/1__3Combos/Inplace/Fighter@Attack_3combo_3"),
        };
        clips.boarder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Wrestler_Finishers_Volume2/Art/Models/Android_SkeletalMesh.fbx");
        clips.boarderIdle = W("Idle01");
        clips.boarderWalk = W("Walk_Forward");
        clips.boarderBack = W("Walk_Back");
        clips.boarderLeft = W("Walk_Left");
        clips.boarderRight = W("Walk_Right");
        clips.boarderDown = F("4_Damages/5__KnockDown/Fighter@KnockDown_Back_Big");
        clips.boarderAttacks = new[] { W("Attack_Punch01", false), W("Attack_Punch02", false), W("Attack_Kick01", false) };
        clips.boarderHits = new[] { W("HitReaction_01", false), W("HitReaction_02", false), W("HitReaction_03", false) };
        clips.guard = F("1_Movements/18__Blocking/Fighter@Blocking_Loop");
        clips.guardHit = F("1_Movements/18__Blocking/Fighter@Blocking_Accept");
        clips.getUp = W("LayOnBack_GetUp01", false);
        clips.lying = W("LayOnBack_Idle", true);
        string[] throws = { "BackControl_Suplex", "HipThrow", "PowerBomb", "Underarm_FlipToss", "BehindScoopSlam" };
        clips.throwBy = throws.Select(t => W("Paired_" + t + "_Att", false, true)).ToArray();
        clips.throwOf = throws.Select(t => W("Paired_" + t + "_Vic", false, true)).ToArray();
        clips.holdBy = W("Paired_HeadLock_Start_Att", false, true);
        clips.holdOf = W("Paired_HeadLock_Start_Vic", false, true);
        clips.heldBy = W("Paired_HeadLock_Loop_Att", true, true);
        clips.heldOf = W("Paired_HeadLock_Loop_Vic", true, true);
        clips.letGoBy = W("Paired_HeadLock_End_Att", false, true);
        clips.letGoOf = W("Paired_HeadLock_End_Vic", false, true);
        clips.slamBy = W("Paired_HeadLock_DDT_Att", false, true);
        clips.slamOf = W("Paired_HeadLock_DDT_Vic", false, true);
        EditorUtility.SetDirty(clips);
        AssetDatabase.SaveAssets();
        Debug.Log("BrawlClips built");
    }
}

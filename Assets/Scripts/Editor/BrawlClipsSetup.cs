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
        clips.boarderLeft = W("Walk_Left");
        clips.boarderRight = W("Walk_Right");
        clips.boarderDown = F("4_Damages/5__KnockDown/Fighter@KnockDown_Back_Big");
        clips.boarderAttacks = new[] { W("Attack_Punch01"), W("Attack_Punch02"), W("Attack_Kick01") };
        clips.boarderHits = new[] { W("HitReaction_01"), W("HitReaction_02"), W("HitReaction_03") };
        EditorUtility.SetDirty(clips);
        AssetDatabase.SaveAssets();
        Debug.Log("BrawlClips built");
    }
}

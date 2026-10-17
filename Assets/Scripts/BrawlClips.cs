using UnityEngine;

// What the fight inside a wagon (BrawlEvent) is played with: the hero's moves (Assets/Grruzam_Fighter_Animation)
// and the boarders' (Assets/Wrestler_Finishers_Volume2), all humanoid, so any of the heroes can wear them.
// Resources/Brawl/BrawlClips.asset holds them, so only these clips go into a build and not the whole packs.
public class BrawlClips : ScriptableObject
{
    public AnimationClip idle, run, dodge, heavy, hit, down;
    public AnimationClip[] combo;        // the three hits of the light combo, in order
    public GameObject boarder;           // the android
    public AnimationClip boarderIdle, boarderWalk, boarderLeft, boarderRight, boarderDown;
    public AnimationClip[] boarderAttacks, boarderHits;
}

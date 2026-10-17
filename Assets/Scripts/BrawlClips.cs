using UnityEngine;

// What the fight inside a wagon (BrawlEvent) is played with: the hero's moves (Assets/Grruzam_Fighter_Animation)
// and the boarders' (Assets/Wrestler_Finishers_Volume2), all humanoid, so any of the heroes can wear them.
// Resources/Brawl/BrawlClips.asset holds them, so only these clips go into a build and not the whole packs.
// The paired ones (By: the one doing it, Of: the one it is done to) are played with the two bodies spaced and
// turned as in the pack's demo scene (BrawlEvent.PairSpacing).
public class BrawlClips : ScriptableObject
{
    public AnimationClip idle, run, dodge, heavy, hit, down;
    public AnimationClip[] combo;        // the three hits of the light combo, in order
    public GameObject boarder;           // the android
    public AnimationClip boarderIdle, boarderWalk, boarderBack, boarderLeft, boarderRight, boarderDown;
    public AnimationClip[] boarderAttacks, boarderHits;
    public AnimationClip guard, guardHit;              // a boarder covering up, and a blow taken on the guard
    public AnimationClip getUp, lying;                 // back on the feet after a throw, and staying down
    public AnimationClip[] throwBy, throwOf;           // the hero's throws (the suplex among them), side by side
    public AnimationClip holdBy, holdOf, heldBy, heldOf, letGoBy, letGoOf, slamBy, slamOf; // a boarder's headlock: in, held, out or DDT
}

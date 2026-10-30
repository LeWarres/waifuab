// One knob for how big the numbers look. Every health pool and every damage source is designed in small
// single digits, then multiplied by this, so the floating damage numbers read big (60, 250...) while the
// fights play out exactly as before: same hits to kill, same resistance. Distances, rates, money, rewards
// and costs are not scaled.
public static class Balance
{
    public const float Scale = 10f;
    public const float DamagePerRank = 0.1f, CooldownPerRank = 0.08f;
    public const float WagonArmorPerRank = 0.05f, WagonVitalityPerRank = 0.1f;

    // Equal pressure at equal chapter progress, including Gabriel's longer opening shift. Like Vampire Survivors'
    // harder stages, every chapter opens as gently as the first (the build starts over, and the enemy types unlock
    // by difficulty: a later chapter starting at 3 met the alien saboteurs, which stun guns, at minute 0) but climbs
    // ChapterRamp steeper, plus one a chapter by its end: the further along the story, the harder, ahead of what
    // earlier chapters hand down (Story.Kept). Chapter 1 ends at 29, chapter 4 at 37, chapter 5 at 39 (0.1 lost chapter 4 with a good build).
    public const float ChapterRamp = 0.06f;
    public static int StoryDifficulty(float seconds, float duration, int chapter)
    {
        float progress = UnityEngine.Mathf.Clamp01(seconds / UnityEngine.Mathf.Max(1f, duration)), t = progress * 1200f;
        return (int)((t / 90f + 4f * (t / 600f) * (t / 600f)) * (1f + ChapterRamp * chapter) + chapter * progress);
    }
}

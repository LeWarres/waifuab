# Combat and UI review

Validated in the connected Unity Editor, Play Mode, on 2026-11-03.

- Kill milestones use the same large animated entrance banner as bosses, with a chest tag and the correct kill total, followed by the reward after two seconds. The interval was subsequently changed to 2500 kills: `PlayKillMilestones.cs` verifies no reward at 1000/2000/2499, queued 2500/5000 rewards and a 7500 reward interrupted by pause.
- The mini-boss taunt instruction uses the shared QTE button graphics through `Prompt`; both HUD toasts and nearby world prompts support button tokens. Screenshot checked with the PlayStation square button.
- Generated and imported 19 distinct evolved weapon icons using the existing `Tools/gen_icons.py` pipeline: Game Icon Institute v4 XL checkpoint, seed 1, 768px generation, 28 steps, CFG 6, Euler ancestral, normal scheduler, original style and transparent cutout. Output is 256px RGBA. Settings and subjects are recorded in `Tools/evolved_icons_settings.json`.
- Evolved icons appear in the hero HUD, arsenal strip, wagon placement, chest evolution rewards and evolution recipes. Asset loading and actual evolved hero HUD texture were verified.
- Regeneration stops in weapon/passive station menus and resumes on the road, for both train and hero.
- 160 controlled boss/shop chests included 48 multi-prize chests, six with five prizes. Verified no repeated weapon/passive level, including two copies of the same wagon weapon, and no weapon level-up followed by its evolution in the same chest. Evolution eligibility is captured when the chest opens; exhausted choices fall back to money.
- Downward view uses a forward composition offset and additional HUD clearance. Tested an eleven-wagon train; its focus projects at viewport y=0.430 and its rear remains below the reserved upper HUD area.
- Rival tracks are 20m apart, with the camera centered between the trains. Rival base HP is half its previous value; absolute normal/ultimate damage caps are preserved so the reduction actually shortens the fight.
- Forced simultaneous boarding/QTE deadlines: no overlap. An already active QTE blocks boarding, and returning from boarding gives a new phrase cooldown. A boarding win removes 12% of the rival's maximum HP and can finish it. Returning does not replay the boss introduction.

Production compilation: 0 errors, 0 warnings. Integrated probe: 0 console errors. Screenshots were inspected for banner, button glyph, downward long-train view, separated duel trains and evolved HUD icons.

These are accelerated encounter and reward tests, not a full campaign balance playthrough. Full-match pacing with a completed evolved arsenal still needs a normal playthrough. Player save, selection and scene are restored after testing.

# Gameplay polish validation

Validated in the connected Unity Editor on 2026-11-03 using `Tools/PlayGameplayPolish.cs`.

- Compilation: 0 errors, 0 warnings. Final Play Mode probe: 0 console errors.
- Story and gameplay dialogue measure the entire sentence before revealing characters, keeping its chosen font size stable. The tested story page selected 27 px.
- Central elapsed clock uses fixed 42 px type. Clock, kill counter, boss bar and boarding counter were visually checked for overlap.
- Boarding ground UVs advance with unscaled time while the exterior simulation is paused.
- Mini-boss entrance warning precedes its taunt instruction. Recurring interval reduced from 70 to 55 seconds; first spawn remains unchanged.
- Desert truck health is two thirds of its previous value; controlled base fixture measured 1000 instead of 1500.
- Station chest costs 800 coins. Purchases below that amount are disabled, and chest presentation blocks duplicate confirmation, including its closing frame.
- Shop chest retains 1/3/5 reward counts. Eligible evolutions share candidate weight with normal upgrades instead of receiving priority. A controlled two-candidate sample produced 59 evolution-first rewards in 128 seeded openings. Existing mini-boss evolution priority remains intact.
- Thirty simple chest fixtures produced one normal reward each, no evolutions. Fully upgraded inventory falls back to money. Kill milestones 1000 and 2000 triggered once each; 1001 did not duplicate a chest.
- Updated two localization keys in all four project locales (8 imported values).

The probe accelerated kill counts, encounter deadlines and encounter starts. It did not simulate a complete biome or kill 1000 enemies naturally. The 55-second interval permits roughly four additional spawns in a nominal 20-minute window; actual counts depend on fight duration, station time and encounter availability. Long-session pacing still needs a normal playthrough.

Evidence: `play.txt`, `story-fixed.png`, `hud-clock.png`, `mini-warning.png`, `shop-chest.png`, and `boarding-motion.png` in this folder. Original player save, selection and scene were restored after testing.

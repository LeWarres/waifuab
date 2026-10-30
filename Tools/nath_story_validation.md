# Nath Star / ReyMar — validation 2026-10-28

## Delivered

- 5 chapters: volcano/Tzmil; human inspection then city/race; heaven/Gora-P; hell/Onini; forest using existing jungle biome/Faceless and T.exe; Thanks.
- 14 story illustrations created with built-in imagegen. Simple amateur webcomic forms, exaggerated fighter expressions on human/demon faces, correct blank Faceless head, consistent warm brown Nath skin and purple sailor outfit.
- 57 text keys, 228 translations in Spanish, English, Portuguese and Japanese. Manual story advance and existing Start skip retained.
- Illustrated Nath portrait in locomotive selection and purple story palette. The supplied Nath reference is an image; no Nath FBX/VRM is present in the project. Existing runtime hero mesh remains until the actual model is supplied. Gabriel duel uses Gora-P without an invented humanoid model.
- Existing progression/save logic, questionnaire and race reused. Story scenes remain separate from gameplay.

## Verification

- SetupNathStory: imported 14 textures and 228 translations.
- CheckNathStory: PASS 5 chapter route, correct rivals, human questionnaire/race flags, 14 mapped artworks and all 228 translations; normal 1200-second duration.
- PlayNathStory: PASS selection to Nath prologue, X reveal and upgrade choice, human questionnaire arrest after three wrong answers and retry, five correct answers to city, normal race/launcher transition, four rival wins, all five story transitions, ending and Thanks. 3206 frames, zero console errors after lighting repair.
- Travel clocks and combat/race wins accelerated. This verifies flow, not full-length balance or difficulty.
- The first Play run exposed HDRP SetCookie(null) dereferencing null in TrainSim.Grade outside jungle. Clear the underlying Light.cookie directly instead; jungle still uses Canopy through HDRP. Recompile passed with zero warnings/errors; repeated full flow passed.
- CheckStoryPresentation: PASS 154 illustrations, 5 opening pages, 1312 localized narrative texts, no art/name/dialogue overlap. Prologue and ending screenshots visually reviewed.
- CheckGabrielStory and CheckGigerStory regression checks: PASS.
- All 14 selected PNG source hashes match imported project files and metadata exists. Targeted git diff whitespace check passed.
- Original save bytes and locomotive selection restored and verified; sandScenario in Edit Mode.

Prompts and image sources: Tools/nath_story_art_final.json. Full Spanish narrative: Tools/nath_story.md. Test logs/captures: ignored Tools/nath_test_session.

## Amateur simplification pass

- Redrew all 14 illustrations and the selector portrait with fewer lines, simpler color areas, crooked forms and sparse backgrounds. Preserved Nath's warm brown skin, purple uniform, exaggerated fighter faces and locomotive identities.
- Prior PNGs preserved in Tools/nath_art_trials/before_amateur_pass; exact new prompts/sources in amateur_pass.json and the final manifest.
- Imported via SetupNathStory. CheckNathStory passed all 14 artwork references, five chapters and 228 translations.
- PreviewNathAmateur displayed all 14 textures through the actual StoryIntro in Play Mode and verified texture identity/aspect ratio. Visually reviewed the fresh amateur-preview.png capture.
- CheckStoryPresentation passed 154 illustrations and 1312 localized texts without artwork/dialogue overlap. This pass checks presentation; campaign gameplay was not rerun for an art-only edit.
- Original save/selection/scene restored and verified after Play Mode.

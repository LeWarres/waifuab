# Nath Moon / ReyMar

## Revisión de voz y nombre — 2026-11-01

- Nombre público cambiado a Nath Moon en español, inglés y portugués; ナス・ムーン en japonés. El identificador interno `nath` se conserva para mantener partidas, modelos, arte y referencias.
- 52 claves revisadas: presentación, narración y conversaciones. Los cinco títulos existentes se conservan: 57 claves y 228 valores localizados.
- Voz ruda, peleonera e impulsiva: convierte las advertencias en desafíos, escucha las condiciones después de aceptar y oculta el afecto bajo protestas o invitaciones a una revancha. La narración se mantiene como una conversación en primera persona, con conocimientos limitados.
- Gabriel, Tzmil, Onini y Faceless conservan sus propias motivaciones. Nath reconoce el estado de BBA en la base destruida y deja de insistir, sin bromear sobre su pérdida ni conocer lo ocurrido fuera de su perspectiva.
- Se conserva el recorrido, las 14 ilustraciones, el diseño y las reglas. Su intervención antes de la carrera ahora se muestra mediante el diálogo específico de Nath, en vez de saltarse al anuncio genérico.
- El generador toma `nath_story_dialogues.json` como fuente, evitando reponer la versión antigua al regenerar. Guion de lectura: `nath_story.md`.
- Compilación: cero errores y advertencias. `CheckNathStory`: cinco capítulos, banderas de cuestionario/carrera, duración, 14 ilustraciones y 228 traducciones correctas.
- `PlayNathStory`: selector al prólogo; X para completar texto; nombres y alturas de texto en los cuadros reales en cuatro idiomas; cuestionario humano con arresto, reintento y aprobación; carrera de ciudad; diálogos anteriores a los cinco finales; progresión completa y Thanks. PASS, 2251 fotogramas y cero errores de consola.
- Relojes y victorias de combate/carrera acelerados; esto comprueba texto y flujo, no el balance completo de las partidas.

## Integración original — 2026-10-28

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

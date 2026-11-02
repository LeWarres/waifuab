# Gabriel story validation — 2026-10-27

## Revisión de personalidad — 2026-11-01

- 42 claves narrativas y de combate reescritas en ES/EN/PT/JA. Gabriel es consentida, berrinchuda y muy amorosa: quiere atención, descanso y agradecimientos; protesta con dramatismo, pero protege a la gente y atiende a sus rivales heridos antes de ocuparse de sí misma.
- El estado del hermano de Tzmil se trata con seriedad. Gabriel se preocupa, ayuda a dejarlos pasar y espera noticias; no lo confunde con alguien dormido por pereza. La noticia de su mejoría llega por un mensajero, sin conocimiento ajeno a su perspectiva.
- El reencuentro mantiene su personalidad exigente y cariñosa, con la promesa de comida y una cita concreta en Baratito sobre el mar. Se conservan las once ilustraciones, los encuentros, las mejoras y la duración.
- JSON, TSV y guion de lectura sincronizados. `build_gabriel_story.py` toma el JSON canónico para no regenerar la voz anterior. `ImportGabrielRevision.Run`: 208 valores importados y comprobados por lectura.
- `CheckGabrielStory.Run`: PASS, cuatro encuentros en cielo, 1800 segundos, once ilustraciones, retrato y 208 valores; las demás campañas mantienen 1200 segundos.
- `PlayGabrielStory.Run`: PASS, 386 fotogramas y 0 errores de consola. Se comprobaron 208 valores contra el TSV, altura real de narración/conversaciones en cuatro idiomas, hablantes y textos antes/después de cada rival y avance manual. Conservación del mismo TrainSim, tren y equipo entre los cuatro encuentros, X para elegir arma/pasiva, frontera 1798/1800 segundos, suerte +3, Gracias y recuperación de mejoras parciales/completas sin duplicación: PASS.
- Viajes y victorias acelerados para comprobar la campaña; no es una prueba de balance de treinta minutos. El montaje artístico permanece intacto.
- Revisión visual adicional con `PreviewGabrielArt.Run`: seis páginas con los textos nuevos renderizadas en StoryIntro, 127 fotogramas. La captura de la cita muestra el texto completo y legible. Partida, selección y escena originales restauradas; `TzmilPlaySession.Verify`: PASS, guardado idéntico al respaldo y editor fuera de Play Mode.

## Validación anterior de integración

Implemented Gora-P campaign: 30-minute heaven opening, three initial luck levels, then Onini, Tzmil, Giger Machine and ReyMar. Three intermediate dialogues retain the same TrainSim instance, scene, train and equipment; each gives one weapon and one passive choice before the next direct duel. Final reunion leads to Thanks.

11 imported illustrations follow the approved simple amateur pencil/pastel sample. 52 localization keys / 208 translations (ES, EN, PT, JA). Gabriel portrait available in selection. Her dedicated 3D model remains pending; existing gameplay avatar fallback retained.

## Checks

- CheckGabrielStory: PASS 4 encounters, 1800-second duration, 11 illustrations, portrait and 208 translations. Other campaigns retain 1200 seconds.
- PlayGabrielStory: PASS selector/prologue, 1798/1800-second finale boundary, +3 luck, all four opponents, three same-instance frozen interludes and real X weapon/passive confirmation, ending and Thanks.
- Save resume: PASS partially selected and completed upgrade pairs resume without duplicate rewards.
- Play run: 2323 frames, zero console errors. Travel time and combat victories accelerated; this verifies campaign flow, not a full 30-minute balance playthrough.
- CheckStoryPresentation: PASS 140 illustrations, 6 Gabriel opening pages, 1200 localized narrative texts; no art overlap. Screens reviewed visually.
- PreviewGabrielUpgrade: PASS final localized card labels, no literal newline escapes, three choices and frozen game; final screenshot reviewed.
- CheckGigerStory and CheckGolemStory: PASS regression checks.
- Targeted C# git diff whitespace check passed. Full repository check reports existing whitespace in unrelated localization strings.
- TzmilPlaySession.Restore / Verify: original save identical to backup; selection restored; sandScenario restored in Edit Mode.

Test captures and logs are under ignored Tools/gabriel_test_session. Dialogue script: Tools/gabriel_story.md. Art source manifest: Tools/gabriel_story_art_final.json.

## Character-art corrections

Six images replaced using built-in imagegen edits, preserving previous PNGs in Tools/gabriel_art_trials/before_model_corrections.
- wingless: foreground only Gabriel legs and shoes; distant angels retained.
- reymar: new user-supplied cat-eared black-haired green-eyed purple-uniform woman replaces masked driver.
- tzmil: skeletal outfit, bone markings, gold serpent belt, blue feather crown match actual model reference.
- tzmil/crush/reunion/date: consistent bird skull upper cranium, black beak, turquoise feathers, black body, red cloth and skull ornaments, translated to amateur pencil/pastel style.
Sources/prompts: model_corrections.json and brother_consistency_corrections.json, consolidated in gabriel_story_art_final.json.

Play Mode artwork review: PASS six live pages, 838 frames. Screens inspected for wingless/Tzmil/ReyMar. Presentation bounds regression: PASS 140 illustrations / 1200 localized texts. SHA-256 verifies six final imported source PNGs match selected outputs; existing Unity metadata retained. Original save and selection restored and verified after test, sandScenario in Edit Mode.

## Costume consistency follow-up

Compared all four brother appearances against the actual xiuhtecuhtli model render. Removed invented red chest bibs in crush/date and misplaced biceps cuff in date; chest and abdomen now black in all appearances, red loincloth stays below waist, wrist cuffs retained. Built-in imagegen edits and prompts saved in Tools/gabriel_art_trials/costume_fix.json; source-of-truth costume rules saved in xiuhtecuhtli_costume.md. Previous versions preserved.

Costume follow-up verification: final PNG edits inspected together with unchanged tzmil/reunion and actual model reference; Unity import completed and source hashes matched. The automated Play capture probe did not complete reliably in this follow-up (old captures rejected); no fresh six-page Play pass claimed. Original save restored and verified after closing test.

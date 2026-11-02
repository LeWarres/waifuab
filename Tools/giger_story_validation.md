# Giger Machine — validación

## Revisión de personalidad — 2026-11-02

- Revisadas las 40 entradas narrativas y de combate en ES/EN/PT/JA: 160 valores. Giger habla con agresividad, brusquedad e impulsividad, provoca a sus rivales y disfruta la resistencia y el ruido. La narración sigue sus acciones y percepciones, sin conocer planes ajenos; el engaño del ángel se descubre al volver a casa. Pscoworld permanece sin otros habitantes.
- Las respuestas de Culu conservan un tono correcto y práctico. Crochtrain y Gora-P mantienen sus propios rótulos y voces. Las cifras 35/40 permanecen en la interfaz, fuera de los diálogos. No se modificaron ilustraciones, rutas ni mecánicas.
- Fuente canónica: `giger_story_dialogues.json`; cambios de voz: `giger_story_revision.tsv`; lectura española: `giger_story.md`. `build_giger_story.py` exporta el guion aprobado sin restaurar el borrador anterior ni cambiar filas ajenas.
- `ImportGigerRevision.Run`: 200 valores importados y comprobados en Unity. `CheckGigerStory.Run`: cuatro capítulos, nueve ilustraciones, traducciones y límite 34/35 correctos.
- `PlayGigerStory.Run`: 3147 fotogramas, cero errores de consola. Conversaciones previas y posteriores de los cuatro capítulos verificadas con sus hablantes, avance manual y control X; recorrido hasta Gracias. Se comprobaron 200 valores en los cuatro idiomas y la altura de los textos narrativos y de gameplay sobre sus controles reales.
- El reto usó entrada R2, 40 resoluciones de colisión/omisión, derrota con 34/40, reintento limpio y victoria con 35/40. Trayectos y victorias de combate acelerados; las posiciones entre cajas se adelantaron para cubrir ambos límites. Esta prueba verifica integración y resultados, no el balance de una partida completa.
- Capturas actuales `giger_test_session/prologue.png` y `giger_test_session/ending.png` inspeccionadas: arte aprobado conservado, texto legible y final sin personajes añadidos en Pscoworld.
- Partida y selección originales restauradas y verificadas idénticas a la copia previa; escena `historiaDialogos`, Editor fuera de Play Mode.

## Registro anterior

Campaña conectada al selector: Pscoworld (Culu / Kairi Resha), Thulu (Crochtrain), infierno (romper 35 de 40 cajas), cielo (Gora-P), regreso a Pscoworld y pantalla Gracias.

## Arte y textos

- Nueve ilustraciones en `Assets/Resources/Story/giger_*.png`, generadas con la herramienta integrada de imágenes. Referencia aprobada: `Tools/giger_art_trials/giger_style_beginner.png`.
- Prompts e imágenes de origen: `Tools/giger_story_art_final.json`.
- Guion: `Tools/giger_story.md`; 50 claves y 200 traducciones en español, inglés, portugués y japonés.
- Importación realizada mediante el plugin de Unity / Unity CLI. Recursos existentes y metadatos conservados.

## Pruebas en Unity

- Compilación final: cero errores y advertencias.
- `CheckGigerStory`: cuatro capítulos, rivales y biomas correctos, nueve recursos, 200 traducciones, límite 34/35 correcto. Crochtrain y Gora-P no toman el piloto de BBA en sus duelos.
- `PlayGigerStory`: campaña completa en Play Mode, 3180 fotogramas y cero errores de consola. Entradas reales X/R2; colisiones y cajas omitidas contabilizadas sin duplicados; derrota con 34/40, reintento limpio, victoria con 35/40, diálogos posteriores y Gracias. Los trayectos largos y victorias de combate se aceleraron en esta prueba; las posiciones entre cajas se adelantaron para cubrir ambos límites.
- `PlayGigerDrive`: segunda prueba de conducción continua: 40/40 en 79,1 segundos, 12003 fotogramas, cero errores. Desde el inicio del recorrido solo se enviaron entradas de R2 y stick, sin turbo, teletransportes ni modificaciones de reloj o velocidad. Una primera conducción con turbo constante y otro ajuste del controlador de prueba consiguió 18/40; la conducción controlada comprobó que se puede completar sin alterar la dificultad del juego.
- `CheckGigerMenu`: seleccionar Giger abre su propio prólogo y crea la campaña guardada.
- `CheckStoryPresentation`: 129 ilustraciones y 1108 textos localizados comprobados; sin solapamiento del arte con nombres o diálogo.
- Copia de seguridad restaurada y verificada: partida idéntica, selección original, escena `sandScenario`, modo edición.

Las pruebas de combate confirman la conexión y los rivales, no el equilibrio de una partida completa de veinte minutos por bioma.

## Corrección de ilustraciones y narración

Cinco imágenes corregidas con la herramienta integrada: sin cajas en la boca en origen, Thulu, Crochtrain e infierno; Culu retirada del dibujo del encuentro, conservando Kairi Resha. La roca de la explosión sigue en su momento narrativo. Dos frases sustituidas en los cuatro idiomas: ningún diálogo menciona 35/40 cajas; las cifras permanecen en las instrucciones del reto. Importación y validación de nueve recursos y 200 traducciones correctas. No se modificó código de gameplay. Prompts de edición: `Tools/giger_art_trials/story_corrections.json`.

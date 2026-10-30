# Validación de la historia de Onini

Campaña conectada al selector: infierno → desierto → base del gobierno → cielo → Psicoworlds → Gracias por jugar.

- 15 ilustraciones propias, revisadas contra el modelo de Onini y su locomotora C-279. Estética de tinta negra, sombras grandes y colores planos inspirada en las referencias.
- 32 páginas narradas y diálogos de batalla. 72 entradas nuevas traducidas a español, inglés, portugués y japonés.
- Tres biomas añadidos al final del catálogo, sin alterar los índices existentes: infierno, cielo y Psicoworlds. Cuatro decorados y una estación por bioma, materiales RealToon y paletas propias. Las pinturas base de suelo/cielo y los modelos de enemigos se reutilizan de biomas existentes.
- Guardián celestial con tres anillos dorados y 36 plumas, conservando los ojos y las mecánicas del combate espacial.
- Rival final: Onini, locomotora, vagones, conductor y abordaje en negro. El jugador conserva sus colores.

## Pruebas ejecutadas en Unity Play Mode

`PlayOniniStory`: los cinco capítulos, rivales, transiciones, decorados de tres tamaños, entrada con X, movimiento y salto ante el guardián, rival de sombras y pantalla final. 3467 frames, cero errores de consola. Las victorias se aceleraron para revisar el flujo; no constituye una prueba completa del equilibrio de combate.

`CheckStoryPresentation`: 63 ilustraciones del proyecto, seis páginas del prólogo de Onini y 572 textos narrativos localizados. Sin solapamiento con el nombre ni el diálogo y sin desbordamiento de texto.

`CheckOniniStory`: cinco capítulos, rivales, 15 ilustraciones, 288 valores traducidos, alcance del rival de sombras y final. Sin pasos de cuestionario o ascenso no solicitados.

Regresión: `CheckR20Story` pasa; el jefe original del espacio conserva su modelo EyeBall. Inspección visual adicional del guardián celestial en combate y del tren rival negro.

La partida y la locomotora seleccionada antes de las pruebas se restauraron mediante `R20PlaySession.Restore`.

## Revisión del arte fanmade

Selección posterior del usuario: `home_feast`, `security` y `desert_chase` usan las versiones anteriores más simples que adjuntó explícitamente. Se recuperaron los originales de generación, evitando importar los bordes de las capturas. `onini_story_art_final.json` registra las tres fuentes elegidas; las otras doce imágenes se conservan.

Selección adicional: `shadow` y `deal` también usan los originales exactos de las capturas elegidas por el usuario. Importación en Unity correcta y archivos comprobados contra las fuentes mediante hash.

También se eligieron explícitamente las versiones anteriores de `bargain` (demonio con pergamino y núcleo rojo) y `heaven_entry` (arco dorado entre dos pilares lisos). Originales recuperados, importados y comprobados mediante hash; referencias de historia conservadas.

Las 15 imágenes activas fueron sustituidas conservando sus GUID. Las dos primeras son las pruebas aprobadas; las otras trece se generaron tomando únicamente esas dos como referencia de acabado, sin reutilizar las composiciones detalladas como referencia gráfica. Se revisaron humanos, demonios, ángeles, objetos y locomotoras para mantener contornos angulosos, colores planos y un nivel de detalle moderado. Se corrigieron además la dirección de la locomotora en la persecución y el relleno negro de todo el tren de sombras.

Fuentes y prompts finales: `onini_story_art_final.json` y `onini_story_art_unified.json`. Generación con la herramienta integrada imagegen. Respaldo de las 15 imágenes anteriores: `Tools/onini_art_backup_noir/`.

Validación posterior en Play Mode: `CheckStoryPresentation` pasó con las 15 imágenes en el componente real StoryIntro, 63 ilustraciones totales y 572 textos localizados sin solapamiento o desbordamiento. `CheckOniniStory` confirmó la ruta y las 15 referencias. Captura de revisión: `Temp/onini-fanmade-dialogue.png`. Se restauraron la partida, selección y escena originales, dejando Unity fuera de Play Mode como estaba antes de esta revisión. No se cambió la lógica de gameplay.

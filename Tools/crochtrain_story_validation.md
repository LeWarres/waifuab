# Crochtrain — Dorian

Integración y pruebas del 2026-11-02.

## Historia y presentación

- Ruta aprobada: Thulu contra Giger Machine, infierno contra Onini, cielo contra Gora-P, mar contra Cerebron. Cuatro capítulos y cierre en Gracias.
- Dorian fue humano, jovial y afectuoso; compró para su amada el libro que lo sometió a Thulu. Su voz actual conserva cortesía y expresiones antiguas, pero refleja agotamiento, tristeza y añoranza. Recuerda a Doris sin saber qué fue de ella hasta verla en Baratito.
- Sus líneas de gameplay están rotuladas como pensamientos. Los rivales reaccionan a sonidos y movimientos, nunca responden a sus pensamientos. Solo Culu los escucha en el cierre. La forma tejida representa para el jugador un cuerpo que otros perciben como una forma de guerra indefinida.
- Final: Doris anciana tomando café junto a Cerebron y el amigo gris, sobre el mar; Dorian se alegra de verla viva; el tentáculo lo recoge y Culu promete transmitir su mensaje. No se añade liberación ni reacción de Doris tras esa promesa.
- Nueve ilustraciones propias en `Assets/Resources/Story/crochtrain_*.png`: regalo, lectura, cautiverio, salida, infierno, cielo, mar, café y captura. Caricatura amateur en blanco y negro inspirada en los años treinta. Las fuentes y prompts están en `crochtrain_story_art_final.json`; hashes de las nueve copias comprobados.
- Se corrigió una taza sobrante de la ilustración del café. Permanecen tres clientes y tres tazas. El café se ve sobre una plataforma de madera en el mar.
- Fuente canónica de textos: `crochtrain_story_dialogues.tsv`; JSON exportado y lectura española `crochtrain_story.md`. Son 51 claves en ES/EN/PT/JA, 204 valores. El generador conserva filas ajenas y saltos de línea del archivo de localización.
- Continuidad con Giger: dos respuestas habladas de Crochtrain se sustituyeron por sonidos y se adaptó la reacción posterior. Fuente, revisión, guía y tablas en Unity sincronizadas en los cuatro idiomas. Los diálogos existentes de Cerebron y Culu ya contenían silencio o sonidos.

## Verificación en Unity

- `unity recompile`: cero errores y advertencias.
- `SetupCrochtrainStory.Run`: nueve ilustraciones horizontales importadas, 204 valores de campaña y 12 de continuidad con Giger, con comprobación posterior de cada valor.
- `CheckCrochtrainStory.Run`: ruta de cuatro capítulos, nueve recursos, 204 valores, colores de Crochtrain, rótulos de pensamientos y jugador con orbe sin piloto humano.
- `PlayCrochtrainStory.Run`: 4215 fotogramas, cero errores de consola. Desde el selector, confirmación X, prólogo propio, cuatro biomas y rivales correctos, conversaciones previas y posteriores comprobadas con sus hablantes, avance manual, progreso guardado en cada capítulo y cierre en Gracias.
- Los 204 valores se comprobaron contra la fuente y los textos narrativos y de gameplay caben en sus controles reales, en los cuatro idiomas. El retrato de los pensamientos resuelve a Crochtrain y conserva su color.
- El primer pase detectó una búsqueda incorrecta de la herramienta de prueba: el retrato vive en el Canvas de Talk, no bajo su componente. Se corrigió esa búsqueda en el guion de prueba; el siguiente recorrido completo pasó.
- Capturas inspeccionadas en el diseño real: `crochtrain_test_session/doris.png`, `crochtrain_test_session/culu-promise.png`, `crochtrain_test_session/talk-1.png`. Arte y texto legibles; las imágenes propias quedan en la pantalla narrativa y los retratos existentes en gameplay.
- Regresión: `CheckGigerStory.Run`, `CheckCerebronStory.Run` y `CheckCuluStory.Run` pasaron con sus rutas, recursos y traducciones.
- Los tiempos de bioma y victorias de combate se aceleraron para comprobar la campaña. No se reevaluó su dificultad ni se jugaron veinte minutos por bioma.
- Copia original restaurada y verificada: partida idéntica, locomotora seleccionada original, escena `historiaDialogos`, Editor fuera de modo de juego.

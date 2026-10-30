# Validación de Cerebron

## Revisión posterior del arte

Se aplicaron trece imágenes en el estilo aprobado de `cerebron_doris_indie_test.png`, revisando cuerpos, siluetas, pintura y fondos. Las versiones de horror y su manifiesto se conservaron en `Tools/cerebron_art_backup_horror/`. `ImportCerebronArt.Run` importó las trece imágenes sin alterar sus GUID y verificó su formato horizontal. Prompts y fuentes: `cerebron_story_art_indie.json` y manifiesto final actualizado.

La revisión nueva del encuadre en Play Mode quedó pendiente: el Editor perdió la conexión y posteriormente ya no había ninguna instancia de Unity abierta. Las comprobaciones de Play Mode listadas abajo corresponden a la implementación anterior de la campaña, no a esta última sustitución de arte.

Cambio final del café pedido por el usuario: se retiraron teléfono y cable; Doris reemplazó a la lagartija del centro conservando al alien gris, Cerebron y Culu. Archivo definitivo `Assets/Resources/Story/cerebron_baratito.png`, fuente y prompt `Tools/cerebron_art_trials/cafe_doris_edit.json`; copia contrastada por hash. Unity estaba cerrado, por lo que esta última sustitución se importará al abrir el proyecto.

- Unity recompiló los cambios sin errores ni advertencias.
- Se verificaron en el Editor el héroe `Characters/cerebron/cerebron`, su locomotora `Models/Locomotive_Armor`, el vínculo al índice 7 y su disponibilidad en el selector.
- `CheckCerebronStory.Run`: cuatro capítulos en espacio, planeta alienígena, fortaleza y Thulu; Roger, BBA, Faceless en Holo-Duelo y Crochtrain; trece ilustraciones; 54 claves en ES/EN/PT/JA (216 traducciones). Las seis líneas del encuentro de Faceless son idénticas a las de su campaña y sus bandos están invertidos correctamente. Final enlazado a Thanks.
- `CheckCuluStory.Run`: regresión correcta de los cinco capítulos, quince ilustraciones y 244 traducciones. El ajuste compartido de rivales sin conductor conserva a Giger Machine y Crochtrain sin un avatar de BBA prestado.
- `PlayCerebronStory.Run`: 3169 fotogramas, cero errores de consola. Prueba en Play Mode con control virtual: X revela texto, color violeta del héroe, disparador real del diálogo previo a cada batalla, cuatro finales activos, diálogo posterior y transiciones correctas. Se comprobó el rival Faceless en el tablero, el rótulo Crochtrain y su abordaje sin conductor. Se alcanzó la pantalla Thanks.
- Se aceleraron las victorias y se omitió la elección de mejora de los duelos de locomotoras. No se evaluó el balance de combate ni se jugaron veinte minutos completos por bioma.
- El primer pase del guion de prueba esperaba el diálogo del Holo-Duelo antes de que terminara su resultado de victoria de 1.8 segundos. Se corrigió la espera de la herramienta; el segundo recorrido completo pasó sin modificar el juego por ese motivo.
- `CheckStoryPresentation.Run`: 91 ilustraciones, siete páginas de prólogo y 796 textos localizados comprobados; ninguna ilustración invade el nombre o el cuadro de texto. Altura máxima del texto al tamaño mínimo: 109.
- Trece archivos finales copiados a Resources y contrastados por hash con sus fuentes imagegen. Prompts y procedencia en `cerebron_story_art_final.json`.

Capturas de inspección propias: `Temp/cerebron-prologue.png`, `Temp/cerebron-roger-finale.png`, `Temp/cerebron-faceless-holo.png`, `Temp/cerebron-crochet-finale.png`, `Temp/cerebron-baratito-ending.png`. Registro de recorrido: `Temp/cerebron-play-check.txt`.

La revisión se hizo con copia de seguridad de la partida y selección. Se devuelve el Editor a la escena original tras la prueba. Los cambios preexistentes de otros trabajos del proyecto se conservaron.

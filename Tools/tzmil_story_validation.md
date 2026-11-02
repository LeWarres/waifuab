# Validación de Tzmil

## Revisión de personalidad — 2026-11-01

- Voz linda, bondadosa, paciente y amorosa, con límites claros cuando alguien corre peligro. Narración conversacional en primera persona y conocimientos limitados a lo que presencia o le cuentan.
- 51 entradas narrativas/de combate revisadas en español, inglés, portugués y japonés; cuatro títulos conservados. Se eliminaron las burlas sobre el estado del hermano y se reforzaron la escucha, el agradecimiento a Onini y el respeto a Golem.
- Tres intervenciones de Tzmil en la campaña de Xiuhtecuhtli también revisadas: lo detiene con cariño, sin proponer venganza contra el gobierno. Fuente de esa revisión: `tzmil_guest_dialogues.tsv`; JSON de R-20 sincronizado.
- `ImportTzmilRevision.Run`: 232 valores importados con comprobación de lectura en las tablas reales (220 de su campaña y 12 de sus intervenciones en R-20).
- `PlayTzmilStory.Run`: PASS, 1953 fotogramas, 0 errores de consola. Comprobados los 220 valores, espacio de texto real en los cuatro idiomas, avance manual con X, paleta, continuidad de imagen y los cuatro capítulos con conversaciones antes/después y pantalla Gracias. Las tres intervenciones adicionales de R-20 se verificaron al importar; no se repitió su campaña completa.
- Las victorias se aceleraron para comprobar el recorrido; esta prueba no mide el balance del combate. Ilustraciones, rutas y rivales conservados.
- El generador `build_tzmil_story.py` ahora toma el JSON canónico para no regenerar los diálogos antiguos. Guion de lectura actualizado en `tzmil_story.md`.

## Validación anterior de integración

- Compilación Unity 6000.4.5f1: sin errores ni advertencias.
- `CheckTzmilStory.Run`: PASS. Selector Night Skull/Tzmil, cuatro capítulos, biomas 4/9/10/11, rivales 8/4/2/4; holoajedrez en volcán y ángel celestial en cielo. Sin cuestionarios ni ascenso adicionales.
- 13 ilustraciones originales en Resources/Story, incluidos la muestra aprobada, familia, volcán, gobierno, Golem, explosión, búsqueda en infierno, comida con Onini, viaje al cielo, ángel, reunión con padre, nueva misión y Carito.
- 55 claves de diálogo × español/inglés/portugués/japonés = 220 entradas importadas y comprobadas en las tablas reales de Unity.
- `PlayTzmilStory.Run`: PASS en Play Mode, 2660 fotogramas, 0 errores de consola. Se verificó X en mando, paleta de Tzmil, continuidad sin destello cuando no cambia la imagen, prólogo, disparador temporizado de las cuatro batallas, conversaciones antes/después, transiciones y pantalla Gracias.
- La prueba entra al holoajedrez contra Xiuhtecuhtli, duelo contra Golem/Sentinel, duelo contra Onini y batalla del cielo con el modelo HeavenGuardian existente. Las victorias se aceleran y se omite solamente la elección de mejora para recorrer la campaña: no es una validación de balance ni una partida completa de 20 minutos por bioma.
- Capturas revisadas: `tzmil_test_session/guardian.png`, `r20-holo.png`, `angel.png`, `carito-ending.png`.
- `CheckStoryPresentation.Run`: PASS. 104 ilustraciones y 916 textos localizados de todas las historias; sin deformación de aspecto ni solapamiento del nombre con el arte, sin texto fuera del área disponible. Altura máxima a tamaño mínimo: 109.
- Regresión: CheckCerebronStory y CheckOniniStory PASS después de ampliar los diálogos del jefe celestial para Tzmil.
- Se corrigieron dos ilustraciones donde el atuendo de huesos de Tzmil se había extendido al hermano y a Onini. Manifiesto de fuentes/prompts: `tzmil_story_art_final.json`.
- Partida y selección previas respaldadas en disco antes de detener la sesión autorizada; restauradas al finalizar. Copias de partida excluidas de Git en `tzmil_test_session/.gitignore`.

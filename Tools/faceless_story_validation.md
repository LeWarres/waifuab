# Validación de la historia de Faceless

Se comprobaron cinco capítulos, sus rivales y biomas, 18 ilustraciones y 272 textos localizados de la historia en español, inglés, portugués y japonés. La revisión del encuadre comprobó 34 ilustraciones de ambas historias y 320 textos de narración sin recortes ni superposición con el diálogo.

Pruebas en Play Mode mediante el plugin de Unity:

- Cuestionario con la oficial alienígena: tres errores producen arresto; OK regresa a la narración anterior. Cinco respuestas alienígenas correctas abren la renuncia rechazada y después la fortaleza.
- Batallas: BBA como rival en el planeta alienígena y en el encuentro de cueva; Cerebron en la fortaleza y la cueva; R-20 como rival en la selva; jefe de ojos activo en el espacio con Faceless.
- Descenso: X inicia, el stick mueve la locomotora, tres colisiones reales con chatarra provocan derrota, el reintento vuelve a la narración del espacio. La victoria cambia el fondo de espacio a atmósfera, abre la llegada a la Tierra y luego la selva.
- Bosque: X inicia, el stick mueve al héroe, el tiempo agotado causa derrota, la narración permite reintentar. Cinco interacciones reales de proximidad y X recuperan las hojas y abren las seis páginas del epílogo.
- El epílogo abre la pantalla Gracias existente. Las articulaciones de T.exe conservan sus referencias al recargar scripts.

Las victorias de los combates reutilizados se aceleraron con los controles de prueba existentes. No se jugó la duración completa de cada capítulo ni se calibró exhaustivamente la dificultad de la campaña. Las pruebas del descenso acortaron su duración y colocaron obstáculos para verificar las colisiones; las del bosque colocaron los objetivos cerca para verificar la interacción.

Ilustraciones nuevas: `Assets/Resources/Story/faceless_*.png`. Prompts finales y referencias: `Tools/faceless_story_art_final.json`; generadas con la herramienta integrada image_gen. Guion: `Tools/faceless_story.md` y traducciones en `Tools/faceless_story_dialogues.json` y las tablas de localización de Unity.

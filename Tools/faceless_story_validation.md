# Validación de la historia de Faceless

## Revisión de personalidad — 2026-11-01

- Voz respetuosa, muy amable y excéntrica de artista. Cambiada la narración del prólogo y los cinco capítulos, las conversaciones de combate y las frases narrativas del descenso/bosque: 70 claves revisadas en ES/EN/PT/JA (280 valores importados con lectura de comprobación).
- Faceless pide permiso y escucha sin someter su vida a las órdenes ajenas. Su atención al dibujo aparece en detalles concretos: la hoja, el retrato de Cerebron, la corbata y sus bocetos de T.exe. La relación con su locomotora respeta la voluntad de ella; su amabilidad no implica negar el daño de los combates.
- Se conservan la derrota fingida, la renuncia rechazada, la escapatoria con ayuda de Cerebron, el descenso, el encuentro con R-20, los dibujos y el trabajo de T.exe. La elección de corbata azul/roja permanece. Sin cambios de arte ni de mecánicas.
- Se sincronizaron el prólogo JSON/Markdown, el JSON completo, el TSV de Unity y la guía de lectura. El importador ahora conserva saltos de línea y exporta el guion completo. Los textos auxiliares antiguos del bosque se sincronizaron con los vigentes de ocho dibujos para no regenerar instrucciones obsoletas.
- En la conversación del jefe espacial se corrigió el nombre mostrado: ahora usa el jefe de ojos en lugar del robot del índice de reserva. La comprobación antigua de ruta ahora reconoce el holoajedrez ya existente contra Cerebron.
- `CheckFacelessStory.Run`: PASS, cinco capítulos, cuestionario alienígena/reintento, descenso/reintento, 18 ilustraciones y 276 valores de las 69 claves de campaña.
- Compilación Unity: 0 errores y 0 advertencias. `CheckFacelessRevision.Run`: PASS, 1036 fotogramas y 0 errores de consola; 300 valores verificados, altura real de los textos de narración y combate en cuatro idiomas, X y avance manual, conversaciones reales antes/después, holoajedrez con Cerebron, nombre de Moho, cuestionario con arresto/reintento/aprobación, descenso con derrota/reintento/llegada, bosque con ocho hojas, epílogo de seis páginas y Gracias.
- Viajes, victorias y recuperación de dibujos acelerados mediante los ganchos existentes: esta prueba verifica textos y conexiones, no el balance. La espera de carga del bosque se ajustó en el comprobador para no enviar X antes del bloqueo inicial de entrada. Captura final revisada: `faceless_revision_ending.png`.
- Partida, selección y escena originales restauradas. `TzmilPlaySession.Verify`: PASS, guardado idéntico al respaldo y editor fuera de Play Mode.

## Validación anterior de integración

Se comprobaron cinco capítulos, sus rivales y biomas, 18 ilustraciones y 272 textos localizados de la historia en español, inglés, portugués y japonés. La revisión del encuadre comprobó 34 ilustraciones de ambas historias y 320 textos de narración sin recortes ni superposición con el diálogo.

Pruebas en Play Mode mediante el plugin de Unity:

- Cuestionario con la oficial alienígena: tres errores producen arresto; OK regresa a la narración anterior. Cinco respuestas alienígenas correctas abren la renuncia rechazada y después la fortaleza.
- Batallas: BBA como rival en el planeta alienígena y en el encuentro de cueva; Cerebron en la fortaleza y la cueva; R-20 como rival en la selva; jefe de ojos activo en el espacio con Faceless.
- Descenso: X inicia, el stick mueve la locomotora, tres colisiones reales con chatarra provocan derrota, el reintento vuelve a la narración del espacio. La victoria cambia el fondo de espacio a atmósfera, abre la llegada a la Tierra y luego la selva.
- Bosque: X inicia, el stick mueve al héroe, el tiempo agotado causa derrota, la narración permite reintentar. Cinco interacciones reales de proximidad y X recuperan las hojas y abren las seis páginas del epílogo.
- El epílogo abre la pantalla Gracias existente. Las articulaciones de T.exe conservan sus referencias al recargar scripts.

Las victorias de los combates reutilizados se aceleraron con los controles de prueba existentes. No se jugó la duración completa de cada capítulo ni se calibró exhaustivamente la dificultad de la campaña. Las pruebas del descenso acortaron su duración y colocaron obstáculos para verificar las colisiones; las del bosque colocaron los objetivos cerca para verificar la interacción.

Ilustraciones nuevas: `Assets/Resources/Story/faceless_*.png`. Prompts finales y referencias: `Tools/faceless_story_art_final.json`; generadas con la herramienta integrada image_gen. Guion: `Tools/faceless_story.md` y traducciones en `Tools/faceless_story_dialogues.json` y las tablas de localización de Unity.

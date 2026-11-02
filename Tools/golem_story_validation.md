# Validación de la historia de Golem

## Revisión de personalidad — 2026-11-01

- 82 entradas revisadas en cuatro idiomas: 56 de narración, conversaciones y ascenso; 26 opciones de las trece preguntas. La puntuación, las preguntas, los rivales, las rutas, el arte y ambos finales se conservan.
- Golem habla con precisión y contención, evita bromas constantes y no da por demostrada su conciencia. Observa recuerdos que no quiere perder, la espera involuntaria de una orden y su resistencia a desconectar el núcleo. No presupone que los otros robots sean perezosos ni sabe qué sienten.
- Su decisión de dejar abierto el ducto sigue siendo deliberada y peligrosa. Volver a encontrarse con una abertura que puede abandonar enlaza la elección del portal con esa responsabilidad, sin absolverlo por sacrificarse después.
- El final alto permite elegir renacer con dudas. El bajo conserva su voz interna mientras pierde el control de su cuerpo; Culu no conoce esa situación. Baratito sigue situado sobre el mar.
- `golem_story_revision.tsv` conserva los cambios en ES/EN/PT/JA. `build_golem_story.py` exporta el JSON canónico sin restaurar el borrador anterior. Guion completo en `golem_story.md`.
- `ImportGolemRevision.Run`: 496 valores importados y comprobados por lectura de las tablas reales. Comprobación adicional de igualdad entre JSON y TSV: PASS.
- `CheckGolemStory.Run`: PASS en Unity, cuatro capítulos, ascenso/reintento, 16 imágenes, 496 traducciones, circuitos, frontera moral 6/7 y ambos finales.
- `PlayGolemStory.Run`: PASS, 2819 fotogramas, 0 errores de consola. Comprobados textos y hablantes antes/después de los cuatro retos, avance manual, 496 valores contra TSV y altura real de narración/conversaciones en ES/EN/PT/JA. Preguntas y opciones caben también en los rectángulos reales del juicio en los cuatro idiomas.
- Se probaron X, derrota/reintento del ascenso, montaje y rotación real del circuito, deshacer con Círculo y las trece respuestas en ambas ramas (7 altas y 6 altas), persistencia y pantallas Gracias. Viajes y victorias de duelos acelerados: no es una prueba de balance. Capturas del final y juicio revisadas visualmente.
- Partida y selección originales restauradas al terminar; comprobación de igualdad con el respaldo: PASS.

## Contenido

- Ruta del selector: locomotora Golem, héroe interno `sentinel` (índice 4).
- Base del gobierno (9), duelo Tzmil (3) → diálogos → ascenso existente, con narración propia de Golem y reintento → espacio (6), duelo Faceless (10) → Thulu World (13), ensamblaje de tres piezas y tablero de conexiones → Cielo Robótico (14), 13 decisiones morales.
- Dos finales: 7–13 respuestas altas ofrece descanso y Golem elige renacer; 0–6 devuelve su conciencia prisionera a un robot que trabaja con Culu en Baratito. Ambos terminan en Gracias.
- Resultado moral persistido en SaveGame; se reinicia al iniciar una historia nueva.
- 16 ilustraciones en blanco y una tinta por personaje, con fuentes y prompts en `golem_story_art_final.json`.
- 124 claves / 496 entradas ES, EN, PT y JA, incluidas las 13 preguntas y las instrucciones. Narración manual y colores propios del héroe.
- Cielo Robótico: terreno cerámico con circuitos, cielo, estación, nodos, nubes, arcos y monumento robótico con materiales toon; tablas de bioma/vías/jefes alineadas a 15 entradas. Usa los drones mecánicos del catálogo existente.

## Pruebas

- Unity 6000.4.5f1, compilación sin errores ni advertencias.
- `CheckGolemStory.Run`: PASS. Ruta, ascenso/reintento, 16 recursos, 496 traducciones, conectividad de entrada/salida, cuatro giros equivalentes, frontera moral 6/7, persistencia de resultados, finales y recursos del nuevo bioma.
- `PlayGolemStory.Run`: PASS, **6411 fotogramas**, **0 errores de consola**. Entrada X, derrota del ascenso a tres golpes y vuelta a sus textos, reintento y llegada orbital, dos duelos correctos, selección/cancelación de piezas, rechazo de alojamiento incorrecto, tablero resuelto con acciones reales de rotación, deshacer y activar núcleo. Se respondieron las 13 preguntas de ambos recorridos, revisando respuestas con Círculo sin duplicar puntuación. Se comprobaron los dos finales y sus pantallas Gracias.
- Las victorias de los dos duelos y los recorridos de viaje se aceleraron; no es una validación del balance ni de una partida completa de veinte minutos por bioma. Los dos minijuegos nuevos sí se completaron mediante sus acciones reales.
- `CheckStoryPresentation.Run`: PASS, 120 ilustraciones y 1020 textos localizados de todas las historias, sin deformación ni solapamiento del nombre con el arte; altura máxima de texto a tamaño mínimo 109.
- Revisión visual de prologo, tablero, preguntas, mundo y ambos finales. Los rótulos de entrada/núcleo se separaron después de detectar que quedaban demasiado cerca de las casillas; captura final `golem_test_session/circuits-final.png`.
- Regresión: Tzmil, Cerebron, Onini y Culu PASS. El chequeo antiguo de Culu exigía exactamente 14 biomas; se ajustó para aceptar biomas posteriores y seguir verificando la misma cantidad de paletas de vías.
- Se utiliza `TzmilPlaySession` como herramienta de respaldo/restauración: guarda la partida original antes de las pruebas y verifica igualdad al terminar; sus copias originales están excluidas de Git.

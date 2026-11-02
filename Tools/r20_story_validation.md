# Validación de la historia de R-20

## Revisión de personalidad — 2026-11-01

- 53 claves reescritas en ES/EN/PT/JA: narración y conversaciones con voz excéntrica, extrovertida y casi siempre enfadada. Xiuhtecuhtli presume, reclama atención, interpreta contratiempos como agravios y discute incluso consigo mismo. Tzmil conserva sus tres intervenciones pacientes y amorosas de la revisión anterior.
- Se mantiene el engaño a Culu, el cuestionario humano, la carrera ganada con lanzamiento denegado, BBA en el mar, la amenaza al volcán, el holoajedrez con Tzmil y el clon. El encuentro de BBA tras el lanzador lo desconcierta; los archivos de varias copias conectan después con lo que ha visto, sin conocimientos omniscientes.
- La verdad sobre el silicio y el proyecto de clones llega por archivos que él consulta. Arrojar la roca conserva consecuencias serias. El traslado al banquete se presenta como una laguna de memoria, sin afirmar que conoce lo ocurrido durante ella. Se mantiene literalmente «mesa larga larga» y el cariño por su hermana.
- JSON, TSV y guía `r20_story.md` sincronizados. Nuevo exportador `build_r20_story.py` usa el JSON canónico y conserva el resto de la localización. Comprobación de 65 claves, frase literal y tres líneas de Tzmil: PASS.
- `ImportR20Revision.Run`: 252 valores de campaña importados y comprobados por lectura. `CheckR20Story.Run`: PASS, cinco capítulos, cuestionario/reintento, lanzamiento denegado, holoajedrez, clon, siete páginas finales, 14 ilustraciones y 260 valores incluyendo las claves del bioma.
- `PlayR20Story.Run`: PASS, 847 fotogramas y 0 errores de consola. Comprobados 252 valores y altura real de narración/conversaciones en los cuatro idiomas, X, avance manual, hablantes y textos antes/después de los cuatro combates, briefing real de carrera, cuestionario con arresto/reintento/aprobación, R-20 ganador con BBA en la rampa, holo de Tzmil, clon y Gracias.
- Viajes y victorias acelerados mediante funciones de prueba existentes; no es una prueba completa de balance. Captura del banquete revisada visualmente: `r20_test_session/personality-ending.png`. Partida, selección y escena originales restauradas; `TzmilPlaySession.Verify`: PASS, guardado idéntico al respaldo y editor fuera de Play Mode.

## Validación anterior de integración

Comprobado en Unity 6000.4.5f1 con el plugin de Unity, en Play Mode.

- Cinco capítulos: selva/Culu, ciudad/carrera, mar/BBA, volcán/Tzmil en Holo-Duel y base del gobierno/clon propio.
- 31 páginas narradas, 14 ilustraciones de códice y 65 entradas nuevas en español, inglés, portugués y japonés.
- X del mando completa el texto; los diálogos siguen siendo manuales.
- Cuestionario humano: tres errores arrestan; OK repite la llegada a la ciudad; cinco respuestas correctas permiten continuar.
- Carrera: R-20 gana; su locomotora permanece abajo y BBA sube por el lanzador. El siguiente capítulo es el mar, sin ascenso.
- Modelos rivales comprobados por su índice: Culu 6, BBA 0, Tzmil 3 y clon R-20 8.
- Base gubernamental: terminal, suministros, control de seguridad, patrullas y hangar; enemigos mecánicos reutilizados de la ciudad. El modo infinito mantiene su ruta anterior.
- Epílogo de siete páginas y pantalla Gracias. Se conserva literalmente «mesa larga larga».
- La prueba recorrió 9245 fotogramas, sin errores de gameplay en consola. Se aceleraron las victorias mediante las funciones de prueba existentes; no verifica el balance de los combates completos.
- La comprobación visual midió 48 ilustraciones y 444 textos localizados de todas las historias: sin solapar los dibujos con el nombre o el diálogo.
- Partida, selección de locomotora, escena y ajuste de ejecución en segundo plano restaurados al terminar.

Repetición: ejecutar CheckR20Story.Run en Edit Mode. Para la prueba de campaña, R20PlaySession.Start respalda la partida y entra en Play Mode; PlayR20Story.Run ejecuta el recorrido. Al salir de Play Mode, R20PlaySession.Restore restaura el estado original.

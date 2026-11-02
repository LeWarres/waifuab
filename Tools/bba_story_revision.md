# Revisión de la campaña de BBA

Fuente de edición: `bba_story_revision.tsv`; el juego usa `Assets/Localization/strings.tsv` y las tablas Game de Unity.

## Personalidad de BBA

Se ha aplicado una tercera pasada a 49 intervenciones en los cuatro idiomas, conservando el ritmo conversacional. BBA es muy energética, infantil y burlona. Se entusiasma con su nave y las victorias, se pica cuando la retan, busca que alguien vea lo bien que lo hizo y protesta cuando la hacen esperar. A veces la emoción se adelanta a sus planes, pero conserva la competencia para cuidar su carga y su locomotora.

Se muestra esta personalidad con acciones y respuestas: quiere repetir su victoria por radio, hace una reverencia después de vencer a Moho y se burla de la excusa de Onini, sin dejar de pedirle que revise su caldera. Habla con RicMar como una cómplice. No se imponen bromas en cada página ni se añade habla de bebé. Las dudas sobre su hogar y el final trágico conservan su vulnerabilidad; al regresar pierde las ganas de hablar y presumir.

`bba_personality_copy.tsv` conserva esta pasada; está integrada en la fuente de edición y en el guion de lectura. Las pasadas anteriores se conservan como registro, no se reaplican encima de la vigente.

## Segunda pasada: voz de una conversación

La primera revisión seguía separando acciones y observaciones como un informe. Se han reescrito las 68 intervenciones activas para que BBA cuente la experiencia a alguien: conecta motivos y consecuencias, retoma lo que acaba de decir, explica por qué aceptó el encargo y deja que las conversaciones respondan al interlocutor. Las pausas y las interrupciones tienen una razón en la situación, en lugar de cortar cada idea con un punto.

Ejemplo: «Aun así acepté, y sé cómo suena después de lo que te he contado. Pero llevaba ochenta años sin conseguir que nadie me escuchara, y ese hombre estaba diciéndome que podía volver a casa. Le pedí la promesa por escrito y firmé.»

Se mantienen las páginas y el recorrido, con versiones equivalentes en los cuatro idiomas. `bba_conversational_copy.tsv` conserva esta pasada editorial; su contenido ya está incorporado a la fuente de edición. El guion de lectura se ha regenerado.

## Voz y continuidad

BBA lleva ochenta años intentando recuperar la nave y el mapa confiscados. Habla desde su experiencia de luchadora y transportista, con entusiasmo, competitividad e impaciencia. Sus burlas no sustituyen las reacciones ante pérdidas reales.

La promesa del gobierno resulta tentadora porque incluye algo concreto que necesita. Al principio sospecha y pide una copia; aún no sabe qué hace la roca ni puede afirmar que conoce el engaño. Las dudas aumentan con las respuestas evasivas de control. En el final diferencia lo que otros maquinistas le cuentan de lo que ella puede comprobar. No sabe cuántas personas había en la base que destruyó. El cierre permite el silencio y el fracaso de su objetivo sin añadir un chiste.

## Escenas conservadas

- Prólogo: llegada, confiscación, lucha libre, transporte, oferta y salida por el desierto. Se conservan las 16 páginas y sus imágenes.
- Desierto: Onini también necesita escapar; reconoce la voz de BBA por las arenas. Se conserva la burla desde la cabina y la llamada urgente hacia la ciudad.
- Ciudad: cuestionario humano, carrera y lanzamiento. El aviso de control abre una conversación con Cerebron; él desconfía de la carga, como en su campaña. No se le promete un cuerpo nuevo.
- Ascenso: peligro visible de los restos orbitales; al salir, BBA busca las balizas. No se afirma que ya está en el espacio antes de jugar el ascenso.
- Espacio: Moho habla como el jefe real. BBA pregunta por el permiso y reacciona a sus ataques. No se presenta a Golem como su rival ni se confirma que Moho haya muerto.
- Planeta alienígena: aterrizaje sobre la vía, Faceless y T.exe. Él cumple órdenes; BBA nota la exageración de su rendición, sin conocer su plan secreto ni su deseo de dibujar.
- Fortaleza: puertas y corredor de servicio, indicaciones de control y lanzamiento de la roca. Se elimina el anuncio de una batalla con Tzmil que aquí no ocurre. El chiste del ducto aparece al recibir la instrucción concreta.
- Regreso: explosión, silencio de control, base gubernamental destruida, testimonios sobre clones y silicio, búsqueda infructuosa de la nave y el mapa.

Las intervenciones de otros personajes son palabras que BBA escucha directamente; no hay narrador externo ni acceso a sus pensamientos. No se modifican imágenes, biomas, requisitos de victoria ni campañas ajenas.

Se actualizan también las claves antiguas de BBA que actualmente no muestra la ruta (`1.outro.2`, `1.after.*`, `2.after.2`, `4.after.*`), para evitar que una reutilización posterior recupere contradicciones. La carrera y el corredor van directamente al desenlace; esas claves no añaden una conversación inexistente después de ganar.

## Verificación en Unity — 2026-10-31

- Compilación: cero errores y cero advertencias.
- 74 claves importadas en español, inglés, portugués y japonés: 296 valores verificados contra las tablas Game.
- Altura del texto comprobada en los rectángulos reales: narración a tamaño mínimo 25, gameplay a 31. Ningún desbordamiento.
- Play Mode: cinco aperturas y desenlaces; se fuerza el reloj para activar el distribuidor real de conversaciones previas al jefe. Se comprueban el contenido y los hablantes de cada intervención.
- Conversaciones posteriores con Onini, Moho y Faceless; entrada y salida de las puertas de la fortaleza; inicio real de carrera, jefe orbital y corredor; pantalla Thanks. No hay avance automático durante la espera de prueba.
- La pasada final terminó con `consoleErrors=0`. Registro: `bba_revision_test.txt`.
- Las victorias se aceleraron con las ayudas existentes. No se validó el balance de combate ni se rejugaron los cuestionarios o el vuelo completos, cuyos controles y reglas no cambiaron.
- Se detectaron ajustes de importación previos incorrectos en `Race_barrier.fbx` y `Race_trenchwall.fbx`: `RaceEvent.Barriers` lee vértices, normales e índices en CPU. Se activó Read/Write mediante ModelImporter en esas dos mallas; la repetición confirmó que carrera y corredor ya no generan esos errores.
- Partida y locomotora originales restauradas y comparadas con el respaldo; escena restaurada a sandScenario, fuera de Play Mode.

Guion completo para lectura: `bba_story_revisada_es.md`.

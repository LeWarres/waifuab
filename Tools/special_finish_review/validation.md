# Acabado de los finales de infierno y trinchera

Se añadieron cuatro texturas pintadas: basalto, madera, armadura y cubierta metálica. El metal compartido de la carrera de ciudad se reutiliza para tuberías, refuerzos y estructuras. Los materiales se clonan únicamente en los recorridos; los prefabs de los biomas conservan sus materiales originales.

El infierno aplica basalto al camino, muros y decoración compatible, madera a las 40 cajas y metal a sus refuerzos. La trinchera aplica paneles al suelo, muros, obstáculos compatibles, puentes, torretas y muro final. Se mantienen las luces y señales de peligro originales. Las UV del suelo son continuas y las de los muros curvos se calculan antes de doblar cada sección.

Las texturas tienen mipmaps, compresión de alta calidad, filtrado trilineal, anisotropía 8 y tamaño máximo de 1024. Se activó lectura de malla únicamente en siete modelos adicionales que necesitan proyectar UV.

## Validación

- Compilación final: 0 errores y 0 advertencias.
- `ReviewSpecialRace.After`: modo de juego real, ambos recorridos, R2 mediante Input System, más de 150 cuadros por recorrido, movimiento verificado.
- Cobertura: infierno, 108 referencias de basalto y 40 de madera; trinchera, 421 de armadura y una cubierta del camino. Metal compartido presente en ambos.
- UV presentes y no colapsadas en caminos y muros; ninguna textura exclusiva del otro ambiente ni asfalto/fachadas de ciudad filtrados a estos finales.
- Colisión real contra una caja: destrucción y contador correctos. Se conservan 40 cajas y objetivo de 35. Daño de trinchera y presencia de puertas láser comprobados.
- Inspección visual de capturas del recorrido, cajas, pared lateral y objetivo final. Se amplió la escala de los paneles tras la primera revisión para reducir la repetición visual.
- Regresión `ReviewCityRace.After`: texturas y UV de ciudad correctas, conducción real con R2, 0 errores de consola.

Las pruebas fueron breves y enfocadas al acabado. No equivalen a una partida completa ni a una medición de rendimiento. La captura del objetivo final se obtuvo colocando la cámara de prueba frente a la escotilla, sin completar todo el trayecto.

Los prompts y archivos de origen están en `Tools/special_finish_textures.json`. Las capturas y registros de antes y después se conservan en esta carpeta. La partida y selección originales se restauran con `TzmilPlaySession.Restore` y se verifican con `TzmilPlaySession.Verify` al finalizar la sesión.

Restauración final confirmada: partida idéntica al respaldo, selección original recuperada, escena historiaDialogos y editor fuera de Play Mode.

# Validación de la historia de R-20

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

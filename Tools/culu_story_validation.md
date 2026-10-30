# Validación de la historia de Culu

## Contenido

- Cinco capítulos conectados al selector de Kairi Resha/Culu: mar contra Onini; selva contra R-20; desierto contra BBA; Psicoworlds contra Giger Machine; Thulu World contra Crochetrain. Termina en la pantalla Gracias existente.
- 29 páginas narradas en primera persona. 61 entradas nuevas, 244 traducciones en español, inglés, portugués y japonés. Se conservan la piedra perdida, las indicaciones falsas del pollo, el cactus morado/verde de Onini, el comentario sobre el hijo fallecido de otro dios y el café Baratito.
- 15 ilustraciones propias: `Assets/Resources/Story/culu_*.png`. Generadas con imagegen usando las referencias geométricas del usuario y los diseños reales de personajes y trenes. Culu conserva cara humana, ojos rojos y pelo de tentáculos; el padre mantiene ojos rojos, alas y barba de tentáculos. Se corrigió un brazo extra en el desierto y un emblema inventado en la locomotora de Onini.
- Prompts y fuentes: `culu_story_art_final.json`. Los 17 archivos importados coinciden mediante hash con las fuentes revisadas.
- Thulu World añade el índice 13 al catálogo, sin mover los anteriores: cuatro prefabs de decorado (ojos, tentáculos jóvenes, arcos y tentáculos grandes), estación, materiales RealToon, mallas orgánicas y dos pinturas propias de suelo y fondo. Se reutilizan los enemigos del planeta alienígena y el edificio base de su estación.
- Giger Machine y Crochetrain usan sus propios nombres en diálogos y HUD. Sus turnos de piloto envían abordadores en vez de tomar a BBA como piloto de reserva. Los demás rivales conservan sus turnos.

## Comprobaciones ejecutadas

- Compilación: cero errores y advertencias.
- `CheckCuluStory`: cinco capítulos/rivales, las 15 referencias de arte, los cuatro idiomas, transiciones, recursos de Thulu World, final y ausencia de minijuegos no solicitados.
- `PlayCuluStory` en Play Mode: cinco biomas y finales, confirmación con X, tres tamaños de decorado en Thulu World, abordadores de las máquinas, transiciones narrativas y pantalla Gracias. 3887 frames, cero errores de consola. Las victorias se aceleraron; no es una prueba completa del equilibrio de dificultad.
- Revisión adicional en Play Mode tras corregir el nombre de reserva del HUD: Crochetrain muestra su nombre sin BBA. Selector: Culu ya no figura como historia pendiente.
- `CheckStoryPresentation`: 78 ilustraciones totales y 688 textos narrativos localizados; sin solapamiento con nombres o cuadro de texto y sin desbordamiento. Prólogo de seis páginas revisado visualmente con la paleta de Culu.
- Regresión: `CheckOniniStory` y `CheckR20Story` pasan. No se volvió a jugar íntegramente sus campañas.
- Partida, selección, escena y ajuste de ejecución en segundo plano originales restaurados con `R20PlaySession.Restore`; Unity queda fuera de Play Mode, como estaba inicialmente.

Capturas locales: `Temp/culu-thulu-gameplay.png`, `Temp/culu-crochetrain-finale.png` y `Temp/culu-prologue-review.png`. La segunda corresponde a la primera prueba, anterior a corregir la etiqueta del HUD.
# Corrección de locomotoras e interior de cabina

Revisión posterior de proporciones: `culu_bba` se recompuso para mostrar cuerpos largos, con las protagonistas más pequeñas delante; `culu_crochet` aumenta la máquina verde hasta una escala comparable a Kairi Resha en el mismo plano. Fuentes y prompts: `Tools/culu_proportion_corrections.json` (imagegen integrado). Importación de ambos PNG y revisión visual en Play Mode: `Temp/culu-bba-proportions.png` y `Temp/culu-crochet-proportions.png`. Versiones previas: `Tools/culu_art_trials/before_proportion_fix/`. Sin cambios de mecánicas ni diálogos.

- Revisados los seis modelos asignados en `Locomotives.Models` desde Unity; referencias de silueta en `Temp/culu-models/` y colores en los retratos del selector.
- Once ilustraciones corregidas con imagegen integrado; prompts y fuentes en `Tools/culu_locomotive_corrections.json`. Versiones anteriores conservadas en `Tools/culu_art_trials/before_locomotive_fix/`.
- Kairi Resha conserva cuerpo de tiburón bajo, tres aletas y accesorios amarillos. RicMar, C-279, R-20, Giger Machine y Crochtrain siguen sus modelos asignados. R-20 usa la máquina oxidada con turbinas y musgo; se retiró el diseño inventado de pájaro negro.
- En `culu_desert_lost` se eliminó la locomotora duplicada tras la ventanilla: Culu está dentro y por las ventanas se ve únicamente el desierto.
- Importadas las 15 ilustraciones de Culu, manteniendo sus archivos `.meta`. Verificados hashes de las once fuentes aplicadas y actualizado el manifiesto final.
- Revisión en Play Mode con la interfaz `StoryIntro`: `CheckStoryPresentation` pasó para 78 ilustraciones, 11 páginas de revisión y 688 textos localizados, sin solapamiento del arte con nombre o diálogo. Capturas: `Temp/culu-cabin-corrected.png` y `Temp/culu-r20-corrected.png`.
- Cambio únicamente de arte y herramientas de revisión; no se alteraron reglas de combate ni rutas narrativas. Partida y escena originales restauradas tras revisar.


# Culu: continuidad visual

Base: las cuatro ilustraciones geométricas del usuario, con un acabado de aficionado que intenta imitarlas. Grandes siluetas angulosas, manchas amplias de color, colores marinos/malvas/crema y fondos sencillos. Mantener el mismo grado de simplificación en humanos, dioses, trenes y utilería; evitar insertar rostros realistas, maquinaria detallada o telas complejas.

Culu: mujer adulta, piel verde, anatomía facial humana, nariz y boca normales, ojos rojos y orejas puntiagudas. Los tentáculos son el pelo, jamás la boca. Vestido negro, delantal/cofia/puños blancos y lazo verde. Usar unas pocas formas para el encaje. Su locomotora es Kairi Resha: tiburón azul con franjas amarillas, aletas y centros de rueda rojos.

Padre: enorme dios verde con alas, ojos rojos, crestas angulosas y barba de tentáculos. Consultar `culu_origin.png` para conservarlo en todas las apariciones.

R-20: cabeza de cráneo de pájaro con pico marfil/punta negra, sin ojos visibles ni cara humana debajo; penacho turquesa. Onini: cara roja, dos cuernos negros, melena blanca. BBA: pelo rosa y sombrero rojo. Dibujarlos con el mismo acabado geométrico que Culu.

El cactus comparte forma y colores con el de Onini: espiral gruesa morada y verde, espinas triangulares amarillas. Baratito conserva el nombre y el pequeño toldo rosa. Las pinturas para el gameplay mantienen el acabado toon de sandScenario; no aplicar la textura del cuento a los modelos 3D.

## Locomotoras: referencia obligatoria del juego

Antes de ilustrar, consultar `Locomotives.Models` y el modelo asignado en Unity. `InspectCuluLocomotives.Run()` captura los seis modelos de esta historia en `Temp/culu-models/`; los retratos de `Resources/Menu/loco_*.png` muestran sus colores. Simplificar el dibujo sin inventar otra máquina.

- Culu: `Locomotive_Shark`, Kairi Resha. Cuerpo azul bajo y redondeado, tres aletas, dos tubos amarillos, ventilación lateral amarilla, pequeña boca roja con dientes amarillos, ruedas grises con centros rojos.
- BBA: `Locomotive`, RicMar. Diésel azul violeta y amarillo, cabina frontal alta con dos parabrisas, capó delantero corto, luces blancas, franja amarilla baja y ruedas grises.
- Onini: `Locomotive_Steam`, C-279. Caldera gris oscura con frente gris claro, cabina trasera con marco rojo, ruedas grises y biela roja, apartavías gris.
- R-20: `Locomotive_Jet`. Máquina oxidada marrón/cobre, paneles claros, dos turbinas frontales, musgo verde en el techo y patas laterales grises. No inventar una locomotora negra con pico por la apariencia del conductor.
- Giger Machine: `Locomotive_Beast`. Máquina ocre y gris, cúpula biomecánica con dos cuernos claros y parrilla de dientes cuadrados.
- Crochtrain: `Locomotive_Crochet`. Diésel verde tejido, capó y franja amarillos, ventanas blancas y ruedas de botón blancas.

Interior de cabina: por las ventanas se ve el paisaje. Nunca dibujar una segunda copia completa de la propia locomotora fuera de la cabina.

Proporciones: evitar acortar o comprimir los cuerpos para encajarlos junto a los personajes. Mostrar los laterales largos con perspectiva moderada y reducir a los personajes en el encuadre si hace falta. RicMar y Kairi Resha deben sentirse locomotoras de tamaño real. En la escena con Crochtrain, ambas máquinas comparten plano de apoyo y escala comparable; no dibujar Crochtrain como un juguete pequeño frente a Kairi Resha.

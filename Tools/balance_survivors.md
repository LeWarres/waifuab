# Balance del tren inspirado en Vampire Survivors — 29 de octubre de 2026

Primera calibración aplicada y probada en Unity 6000.4.5f1. Fuente: el archivo local `Vampire_Survivors_analisis_mecanicas.txt` proporcionado por el usuario, versión observada 1.16.111. Se usaron especialmente las secciones 2–7 y 9; sus cifras declaradas y aproximaciones no se trataron como una simulación completa del juego original.

## Criterio

Vampire Survivors combina potencia propia del arma, bonificaciones aditivas, intervalo entre ataques y composición de oleadas. Copiar sus HP o DPS literalmente aquí sería engañoso: el tren tiene varias armas y reservas de vida separadas, avanza por una vía y no puede esquivar como un personaje libre. La progresión de equipo de este proyecto llega por estaciones/cofres y tiene cinco niveles de arma; no se añadió un sistema de experiencia ni se cambiaron los formatos del guardado.

## Cambios aplicados

| Sistema | Antes | Ahora |
|---|---|---|
| Daño pasivo por rango | +20% al héroe; nada a las torretas | +10% aditivo al héroe y las torretas; +50% al máximo |
| Cadencia | +20 puntos de frecuencia por rango; 2x al máximo | -8 puntos del intervalo base por rango; intervalo 0,6x y frecuencia 1,667x al máximo |
| Resistencia | -10 puntos de daño recibido al héroe por rango | Conserva el efecto del héroe y añade -5 puntos por rango a los vagones; máximo -25% en el tren |
| Vitalidad | +100 HP por rango solo al héroe | +50 HP al héroe y +10% de la vida base a cada vagón armado por rango; los futuros vagones reciben el mismo bonus |
| Regeneración | 1,5 HP/s por rango **por vagón**, más la mitad al héroe | 1 HP/s por rango compartido entre los vagones dañados, más 0,5 HP/s al héroe; no revive vagones destruidos |
| Cantidad | Cinco rangos; solo los rangos 1 y 4 cambiaban los disparos | Dos rangos útiles, +1 disparo cada uno; menú, cofres, borrador de Gabriel y HUD respetan ese máximo |
| Vida enemiga por dificultad | +13% lineal | +10% lineal |
| Daño enemigo por dificultad | +6% lineal, sin techo | +2,5% lineal, hasta +100% en dificultad 40; el enjambre usa el mismo techo |
| Variantes gigantes / élites | 6x / 14x vida base | 4,5x / 9x; conservan sus diferencias de tamaño y recompensa |
| Hordas y estampidas | Una tirada podía convertir todo un grupo en variantes grandes; podían exceder el límite | Grupos de enemigos básicos; paquetes, reposición y descendencia respetan el límite de población normal |
| Capítulos largos | La curva de 20 minutos seguía acelerando hasta dificultad 56 a los 30 minutos | La curva se normaliza por progreso del capítulo: 0, 4, 10, 19, 29 al 0/25/50/75/100%, más el índice de capítulo |
| Taller | Su mejora de daño se aplicaba al héroe | También alcanza a las torretas, como indica su descripción |
| Enjambre controlado | Solo miraba el nivel del arma | También recibe daño y cadencia de la build |

Las 20 armas conservan sus identidades, cifras base y evoluciones. Se verificó su crecimiento real de niveles 1–5 y se exportó `balance_weapons.csv`. El daño por impacto llega a 3x y la frecuencia a aproximadamente 1,524x: 4,572x de DPS teórico de un impacto antes de pasivas, críticos y cobertura. Con Daño y Cadencia al máximo, ese componente llega a aproximadamente 11,43x del nivel inicial. Este cálculo excluye penetración, área, disparos adicionales y aciertos reales; no es el DPS observado de todas las armas.

Las seis descripciones modificadas se importaron a español, inglés, portugués y japonés. Los tokens antiguos de Cantidad siguen produciendo sus dos disparos adicionales; se evita ofrecer rangos que ya no aportan nada.

## Mediciones de combate real

Arnés `ProbeSurvivorBalance.cs`: desierto, BBA, semilla inicial 741, bot de movimiento, configuraciones fijas y 60 segundos de recorrido por muestra. Reloj acelerado a 4x; estaciones, eventos y miniboss desactivados para aislar combate. Sin ultimate en esta comparación inicial.

| Dificultad | Configuración | Pico de enemigos antes | Pico después | Vida final antes | Vida final después |
|---|---|---:|---:|---:|---:|
| 0 | 2 vagones, armas nivel 1 | 61 | 60 | 800/800 | 800/800 |
| 12 | 4 vagones, armas nivel 3, pasivas nivel 2 | 203 | 177 | 1524/1600 | 1600/1600 |
| 29 | 6 vagones, armas nivel 5, pasivas nivel 4 | 305 | 220 | 2339/2400 | 2400/2400 |
| 29 | 2 vagones, armas nivel 1, sin pasivas | — | 220 | — | Derrota a los 11,8 segundos |

Estos son ensayos de configuraciones ya construidas, no partidas completas obteniendo mejoras. Muestran que el límite funciona y que mejorar el arsenal sigue siendo necesario; no prueban una dificultad óptima para todos los jugadores. La regeneración anterior, al multiplicarse por seis vagones, también ocultaba mucho daño recibido.

## Ultimates incluidas

`ProbeUltimateBalance.cs` ejecutó seis muestras adicionales de 120 segundos de recorrido: las mismas tres configuraciones, cada una sin ultimate y con pulsaciones reales de R al estar lista. La pausa de la tarjeta se ejecutó normalmente. Archivo reproducible: `balance_ultimates.csv`.

| Dificultad | Usos | Primera activación | Bajas directas de ultimate | Daño efectivo de ultimate | Porcentaje del daño total de la muestra con ultimate |
|---|---:|---:|---:|---:|---:|
| 0 | 2 | 55,02 s | 20 | 144 | 2,87% |
| 12 | 2 | 55,02 s | 56 | 1598,16 | 3,08% |
| 29 | 2 | 55,02 s | 53 | 4089,60 | 1,70% |

El daño se cuenta después de armadura y sin daño sobrante. Se añadieron contadores separados de ultimate a `RunStats`, sin cambiar el guardado ni añadir interfaz. Las muestras usaron BBA; los héroes comparten actualmente el mismo ataque de cinco meteoritos con presentación/color distintos. No se presentan como seis ultimates mecánicamente independientes.

Los meteoritos conservan su daño y cobertura actuales. Los datos no justifican multiplicar la vida de todos los enemigos para compensarlos. Una misma semilla inicial no produce una trayectoria idéntica después de activar una ultimate: consume azar, cambia bajas y modifica los encuentros posteriores. Por eso las diferencias de vida final y población media de una sola pareja no se interpretan como efectos causales; hacen falta más semillas y partidas completas para estimar tasas de victoria.

### Corrección en los duelos contra locomotoras

Se forzaron los cinco impactos sobre el cuerpo real de un rival, con sus armas normales desactivadas. Antes, 17.550 puntos contabilizados por el combate solo quitaban 1,488 HP de 31.742,44 (0,00469%) por el límite por fotograma. Esa ultimate prácticamente desaparecía dentro del techo de DPS.

Ahora el rival acepta el daño de ultimate por una ruta separada, con armadura y un máximo del **1% de su vida por meteorito**. Cinco impactos pueden quitar hasta el 5% por lanzamiento, independientemente del framerate; un impacto pequeño sigue siendo pequeño. Las armas normales conservan su techo de daño por segundo.

Prueba posterior: 400 HP contabilizados y 400 HP realmente perdidos sobre 8000 HP, exactamente 5%. Se verificaron también impactos pequeños, HP cero, ausencia de daño sobrante, techo de armas normales y limpieza del callback al reutilizar enemigos del pool. La recarga sigue siendo de 55 segundos de recorrido, las bajas no la aceleran y no carga en pausa. El ensayo forzado mide daño aceptado, no garantiza que los cinco meteoritos acierten en una pelea normal.

Las batallas con controles propios, como el jefe de ojos, no se modelaron como combates donde el jugador pueda lanzar libremente esta ultimate.

## Verificación y límites

- Compilación del código sin errores ni advertencias.
- Comprobaciones en Play Mode: daño compartido, armadura, vitalidad en vagones existentes/nuevos, regeneración durante Update sin revivir restos, límites de rangos, hordas básicas y límites de paquetes/estampidas.
- Se probaron listas de aparición de todos los biomas en dificultades 0 y 29, además de las curvas de las 20 armas y los capítulos de 20/30 minutos.
- Las muestras completas de combate y ultimate terminaron con cero errores de consola.
- Regeneración medida durante 90 fotogramas: coincide con 5 HP/s de presupuesto total al rango máximo.
- No se alteraron precios, recompensas, daño base de todas las armas ni mecánicas particulares de todos los finales a partir de un DPS teórico. Esta es una calibración inicial, pendiente de tasas de victoria por personaje/build en campañas completas.

## Segunda pasada — 29 de octubre de 2026

Problema medido en la primera pasada: con builds razonables (dificultad 12: 4 vagones nivel 3, pasivas 2; dificultad 29: 6 vagones nivel 5, pasivas 4) el tren terminaba 120 s **sin perder vida**. El poder del jugador crecía ~34x (armas 4,6x, Daño+Cadencia 2,5x, vagones 3x) contra ~14x de vida enemiga total (vida 3,9x, población 3,6x). En VS la presión media/tardía viene de especies, densidad y TimeMods de vida **y velocidad** (+2–2,5% velocidad por minuto en Bone Zone / Castle Dracula). Para un tren que no esquiva, la velocidad acorta la ventana en que las torretas matan antes del contacto: es la palanca correcta.

| Sistema | Antes | Ahora |
|---|---|---|
| Velocidad enemiga por dificultad | ninguna | +0,8% por nivel, máximo +32% en dificultad 40 (≈ +1,2%/min en un capítulo de 20 min) |
| Vida enemiga por dificultad | +10% lineal | +12% lineal (dificultad 29: 3,9x → 4,5x; se probó +11% antes del apuntado por arma). También escala abordajes y el daño del meteorito |
| Meteoritos de ultimate | Cuatro esquinas y centro de pantalla, donde casi no hay enemigos (llegan desde fuera) | Con 20+ enemigos en campo, cada meteorito cae sobre uno de los 30 más cercanos al tren; con pocos, el patrón de pantalla anterior |

Sin cambios, a propósito: pasivas (ya equivalen a Spinach, Empty Tome, Hollow Heart y Duplicator), cadencia de estaciones (≈2 elecciones cada 30–60 s, ritmo de level-up de VS a mitad de partida; el build se completa cerca del final de un capítulo de 20 min), recarga de 55 s de la ultimate y curva de dificultad del capítulo.

Primero probé +1,2% de velocidad y +12% de vida. Los muestreos aislados de abajo usan esos valores. En el capítulo completo resultaron demasiado: ver la tabla del capítulo.

Muestras (`ProbeUltimateBalance.cs`, misma semilla y configuraciones; la ultimate ahora se lanza por código, porque las teclas inyectadas se pierden con el Editor sin foco):

| Dificultad | Sin ultimate antes | Sin ultimate ahora | Con ultimate ahora | Bajas por ultimate (2 usos) |
|---|---:|---:|---:|---:|
| 0 | 772/800 | 784/800 | 780/800 | 19 |
| 12 | 1600/1600 | 1322/1600 | 1583/1600 | 49 |
| 29 | 2400/2400 | 2153/2400 | 2390/2400 | 43 |

Ahora el inicio sigue tranquilo y la mitad/final del capítulo cuesta 10–17% de vida en 2 minutos sin ultimate. La ultimate deja de ser decorativa: evita casi todo ese daño porque limpia lo que está por golpear el tren. Su porcentaje del daño total sigue bajo (1–2%), así que su valor es defensivo y no de bajas. Una semilla por configuración: es una tendencia, no una tasa de victoria.

### Capítulo completo

`ProbeChapterBalance.cs`: BBA, capítulo 1 de historia (20 min de reloj, desierto), AutoPlay a 5x con estaciones, mejoras, compra de vagones y reparaciones reales; eventos desactivados. Se corta justo antes del final del capítulo. El bot elige cartas casi al azar (prioriza subir lo que ya tiene), así que sus builds son peores que las de un jugador atento. CSV por minuto: `balance_test_session/chapter-*.csv`.

| Valores | Ultimate | Resultado | Vida mínima | Build final del bot |
|---|---|---|---:|---|
| Primera pasada (+10% vida, sin velocidad) | no | Llega al final, 92% de vida | 52% | Daño 5, armadura 5, crítico 5, suerte, codicia |
| +12% vida, +1,2% velocidad | no | **Destruido** en el minuto 17,5 | 1% | Vitalidad, velocidad, codicia, cantidad, duración |
| +12% vida, +1,2% velocidad | sí | **Destruido** en el minuto 15,5 | 0% | Daño 5, área, regeneración, vitalidad, crítico |
| **+11% vida, +0,8% velocidad (final)** | no | Llega al final, 20% de vida | 1% | Área 5, codicia 5, suerte 5, **daño 1**, crítico |
| **+11% vida, +0,8% velocidad (final)** | sí | Llega al final, 67% de vida | 30% | Codicia, área, velocidad, regeneración, suerte; **sin daño** |

Lectura: en la primera pasada, el último tercio del capítulo casi no rozaba al tren y sobraban 3279 de dinero sin gastar. Con los valores finales, una build floja sobrevive al límite y la ultimate es lo que la salva, como en VS, donde una mala build pasa apuros a partir de la mitad. Ningún valor final mató al bot. Con +12%/+1,2% murió en dos de dos intentos, así que el punto medio es deliberado. Son tres muestras de un solo capítulo y héroe; los capítulos posteriores empiezan con dificultad +1 por capítulo y conservan una mejora.

### Apuntado por arma y retroceso por arma

Después se añadieron modos de apuntado de VS a las armas de vagón (`Weapon.aim`: aleatorio para cohete y tormenta, el que está por golpear al tren para escopeta y lanzallamas, el más fuerte para el francotirador) y un multiplicador de retroceso por arma (`Weapon.push`). En `ProbeSurvivorBalance` subieron las bajas a dificultad 29 de 1766 a 2129 (+20%) con la misma build. Capítulo completo con los mismos valores enemigos finales (+11% vida, +0,8% velocidad):

| Ultimate | Antes del apuntado | Con apuntado |
|---|---|---|
| no | Llega al final con 20% de vida; mínima 1% | Llega al final con 83% de vida; mínima 47% |
| sí | Llega al final con 67% de vida; mínima 30% | Llega al final con 99,5% de vida; mínima 64% |

Ninguna de las dos builds del bot tenía la pasiva de daño. El apuntado recuperó casi toda la holgura de la primera pasada (mínima 52%). CSV: `balance_test_session/chapter-aim.csv`.

Recalibrado: vida enemiga **+12% por nivel** (velocidad sigue en +0,8%). Capítulo completo, de nuevo sin pasiva de daño en ninguna build del bot:

| Ultimate | Resultado | Vida mínima | Build del bot |
|---|---|---:|---|
| no | Llega al final con 50% de vida | 1% | Crítico 5, cantidad 2, codicia 5, área 5, suerte 5 |
| sí | Llega al final con 80% de vida | 46% | Cantidad 2, regeneración 5, suerte 5, área 5, codicia 4 |

Vuelve a la presión buscada: una build floja sin ultimate llega al límite, y con ultimate pasa apurada pero segura. CSV: `balance_test_session/chapter-aim-012.csv`. Valores finales: `healthPerLevel = 0.12f`, `speedPerLevel = 0.008f`.

### Bot que elige bien

`AutoPlay.smart` prioriza el arma del héroe y luego daño, cadencia, críticos, armadura y vitalidad; `AutoPlay.events` deja los eventos encendidos. Con ultimate, valores finales:

- **Sin eventos:** tren destruido en el minuto 12,5 (dificultad 14). El arma del héroe **nunca se ofreció**: blaster nivel 1 durante 12 minutos (cada estación muestra 3 de unas 14 cartas del héroe; ~9% de probabilidad de no verla en 10 estaciones). Solo 4 vagones, 3 armas. La derrota vino del azar de las cartas, no de malas decisiones. CSV: `balance_test_session/chapter-smart.csv`.
- **Con eventos:** destruido en el primer minuto. El bot no juega los minijuegos y el enjambre (cada robot que llega golpea 50 al vagón si nadie usa el cañón) basta para acabar con el tren. No mide la dificultad real de los eventos. CSV: `balance_test_session/chapter-smart-events.csv`.

Conclusión: el resultado de un capítulo depende mucho del sorteo de cartas. VS lo suaviza con reroll, saltar y desterrar cartas.

**Regla de compensación** (`TrainSim.RollOffers`): si el nivel del arma del héroe faltó en dos estaciones seguidas, aparece en la tercera. Con ella, el mismo bot (sin eventos):

| Ultimate | Resultado | Vida mínima | Arma del héroe |
|---|---|---:|---|
| sí | Llega al final con 100% de vida | 76% | nivel 5 en el minuto 5 |
| no | Llega al final con 99% de vida | 71% | nivel 5 en el minuto 6, evolucionada en el 7 |

Una build bien elegida, con su arma garantizada, no corre peligro en el capítulo 1 (sin eventos). El reto del capítulo 1 queda para builds flojas (vida mínima 1% sin ultimate). CSV: `balance_test_session/chapter-smart-pity.csv`.

### Progresión de la historia

Antes, cada capítulo ganado heredaba una mejora elegida y una pasiva al azar **con su nivel completo**, mientras la dificultad solo sumaba +1 por capítulo (0→29 más el número de capítulo). Los capítulos avanzados empezaban con 15–20 rangos gratis y se volvían más fáciles que el primero.

Cambios:
- `Story.Kept`: la pasiva heredada llega a la **mitad de su nivel** (redondeado hacia arriba).
- `Balance.StoryDifficulty`: todo capítulo **empieza en 0** y su curva es un 10% más empinada por capítulo (`ChapterRamp`), más +1 por capítulo al final. Capítulo 1: 0→29; capítulo 4: 0→40; capítulo 5: 0→44.
  - Primero se probó +3 por capítulo desde el inicio: el capítulo 4 empezaba en 9 y el tren moría antes de la primera estación.
  - Luego "+capítulo al inicio y curva más empinada": seguía muriendo en 2 minutos. El bestiario alienígena saca el **Saboteador** (aturde torretas 5 s) desde dificultad 3, y el capítulo 4 empezaba en 3. Los tipos de enemigo se desbloquean por dificultad absoluta, así que cualquier capítulo avanzado los soltaba en el minuto 0. Ya pasaba antes de esta sesión.

Capítulo 4 (planeta alienígena), heredando de 3 capítulos cohete, daño, armadura, codicia 2, cadencia 2 y vitalidad 2:

| Bot | Ultimate | Resultado | Vida mínima |
|---|---|---|---:|
| al azar | no | Aguanta con 70% hasta el minuto 19 y es **destruido justo antes del final** (dificultad 38→40) | 2% |
| elige bien | sí | **Destruido en el minuto 14,5** (dificultad 24) | 0% |

El bot que elige bien tuvo solo 13 estaciones a los 14 minutos frente a 16 del otro (el largo de cada tramo es al azar entre 600 y 1200), y sus armas de vagón seguían en nivel 1–3. Hay mucho azar en estaciones y cartas. CSV: `balance_test_session/chapter4-*.csv`.

Se suavizó a `ChapterRamp = 0.06` (capítulo 4: 0→37) y se añadió **inmunidad al aturdimiento** (`TrainSim.Stun`: 3 s libres al terminar uno; antes cada saboteador lo reiniciaba). Capítulo 4:

| Ajuste | Bot al azar, sin ultimate | Bot que elige bien, con ultimate |
|---|---|---|
| 0.06 | destruido en el minuto 14,5 | destruido en el minuto 18,5, con la build al máximo |
| 0.06 + inmunidad | destruido en el minuto 19,5 (dificultad 35) | destruido en el minuto 15 (dificultad 25) |

En el capítulo 4 hay un **precipicio**: trenes con todo al máximo pasan de 70–90% de vida a 0 en menos de un minuto (2183→0, 2665→0). En el desierto del capítulo 1, el mismo bot terminaba al 100% en dificultad 29. El bioma alienígena (Pelusa, Hexa, Hexágono mayor, Colmena que suelta larvas, Saboteador) es mucho más letal a igual dificultad. Falta medir qué tipo hace el daño. CSV: `balance_test_session/chapter4-*-06.csv`, `chapter4-*-grace.csv`.

Errores de consola vistos en estos ensayos, ajenos al balance: `IndexOutOfRangeException` en `TrainSim.ShapeTrack` (línea 1169) y `TrainSim.KneePoint` (línea 752, desde `ZigzagEvent.LateUpdate`), al cambiar de escena entre corridas.

Herramientas: `CheckSurvivorBalance.cs`, `ProbeSurvivorBalance.cs`, `ProbeUltimateBalance.cs`, `CheckUltimateBoss.cs`, `ImportBalanceText.cs`. Registros temporales y copias previas en `balance_test_session/` (ignorados por Git).

Partida, selección y escena originales restauradas; Verify confirmó igualdad del guardado y Editor fuera de Play Mode. Las comprobaciones de regresión de Gabriel y Nath pasaron.

## Diagnóstico capítulo 4: daño al tren por tipo (bot listo, ramp 0.06 + stun grace)

Combat.TrainHurtBy (DIAG) suma el daño bruto (antes de armadura) por tipo y tier (+1 grande, +2 gigante, +3 élite); el probe lo vuelca por minuto. CSV: Tools/balance_test_session/chapter4-hurt.csv.

- Sin ultimate: destruido a 2 s del final (dificultad 37). Con ultimate: sobrevive al 50% (mínimo 45%). Mismo seed que antes: la varianza entre corridas es alta.
- Minutos 7–17: **Larva** (de la Colmena) es la fuente nº 1 casi cada minuto (210, 816, 522, 812 por minuto). Daño 2, velocidad 7, sin tier, cada Colmena suelta una cada 2.5 s.
- Minutos 16–fin: **Hexágono mayor +1/+2/+3** y nubes de **Pelusa** de todos los tiers. Último minuto sin ultimate: Hexágono mayor+1 = 873, +3 = 545, Pelusa (todos) ≈ 1240.
- Pelusa, Hexa y Hexágono mayor tienen las mismas cifras que Normal, Rápido y Tanque del desierto. Lo que mata al final es la escala de tiers con dificultad > 29, que solo alcanzan los capítulos 4–5 (ChapterRamp), no el bioma.
- Lo único propio del bioma es la Colmena y sus larvas.
- Saboteador: con la gracia de aturdimiento ya no aparece como causa.

### Colmena: larva cada 4 s (antes 2.5)

Los tiers ya topan solos (élite 4.4% en dificultad 29, 5% desde 33; gigante y grande topados desde 20), así que ponerles tope no cambiaría nada. De 29 a 37 crecen vida (+21%), daño (+12%) y enemigos en campo (189 a 220): es la curva por capítulo.
CSV: Tools/balance_test_session/chapter4-hurt-colmena4.csv.
- Sin ultimate: sobrevive con 92% (mínimo 35%).
- Con ultimate: destruido al minuto 13.5 (dificultad 21) con build débil (rocket2, storm1, flame). En los últimos ~30 s: Larva 676, Pelusa (todos los tiers) ≈ 1080, Saboteador 190. Pasa de 1312 a 0.
- El precipicio sigue: cuando una multitud llega a los vagones, cae uno, se pierden sus armas y el resto cae en cadena. Con una corrida por modo la varianza tapa todo: hacen falta varias semillas.

### Capítulo 4, 3 semillas × 2 modos (bot listo, Colmena 4 s)

CSV: Tools/balance_test_session/chapter4-3seeds.csv. Todas las builds terminan al máximo (todo nivel 5 o 6).

| semilla | ultimate | resultado | dificultad final | mínimo |
|---|---|---|---|---|
| 741 | no | muere min 17 | 30 | 0 |
| 741 | sí | sobrevive 42% | 37 | 28% |
| 1203 | no | muere min 17 | 30 | 0 |
| 1203 | sí | muere min 19 | 35 | 0 |
| 58 | no | sobrevive 23% | 37 | 10% |
| 58 | sí | muere min 17 | 30 | 0 |

4 de 6 muertes, todas en los últimos 3 minutos con build completa, entre dificultad 30 y 35. El capítulo 1 (termina en 29) lo pasa el mismo bot al 100%. El muro está en dificultad ~30: por encima, una build completa no aguanta. Los supervivientes acaban al 10–28% de mínimo.

### Capítulo 4 con herencia coherente (una sola build) y bot que busca evoluciones

Herencia de la prueba: wagon:rocket, critChance, amount (elegidas) + luck, damage, critChance (al azar) = blaster, storm, turret y rocket pueden evolucionar. AutoPlay.Best toma primero la pasiva que evoluciona un arma que lleva. CSV: chapter4-coherent-part1.csv y part2.csv.

| semilla | ultimate | resultado | dificultad | mínimo | evolucionadas |
|---|---|---|---|---|---|
| 741 | no | muere min 13 | 21 | 1% | blaster |
| 1203 | no | muere min 18 | 34 | 0 | blaster, turret, rocket, masamune |
| 1203 | sí | sobrevive 69% | 37 | 22% | blaster, turret, rocket, storm |
| 58 | no | muere min 19 | 36 | 6% | blaster, turret, rocket, masamune |
| 58 | sí | sobrevive 90% | 37 | 66% | blaster, turret, rocket, storm, minigun |

741 con ultimate no corrió: Unity salió del modo Play a mitad.
- Con 4–5 evoluciones y la ultimate se pasa. Sin la ultimate se muere en los últimos minutos aunque haya 4 evoluciones.
- La herencia ofensiva deja huecos sin vitality ni armor (vida 2000 en vez de 3000): elegir solo daño tiene un costo real.
- 15 errores en consola: UiKit.Symbol IndexOutOfRange (StationMenu.cs:579). Los símbolos △○□✕ de StationMenu.cs están con codificación rota (mojibake) en los cambios sin commitear; SelectLocomotive.cs también.

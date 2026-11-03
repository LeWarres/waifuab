# Acabado de la carrera de ciudad — 2026-11-02

Primera pasada aplicada al entorno real de la final de ciudad: asfalto, barreras, estructuras metálicas, fachadas del skyline y superficies compatibles de edificios cercanos. Se conserva la geometría y la dirección artística de anime con azules y luces de neón.

## Recursos e integración

- Cuatro texturas creadas con la herramienta integrada de imágenes: `Assets/Resources/RaceSurfaces/asphalt.png`, `concrete.png`, `steel.png`, `facade.png`. Fuentes y prompts completos en `Tools/city_finish_textures.json`; copias contrastadas por hash.
- Importación a un máximo de 1024 píxeles, compresión alta, repetición, mipmaps, filtrado trilineal y anisotropía 8.
- `RaceSurfaces.cs`: materiales compartidos y mapeado por superficie de los submeshes pintados. Los submeshes de ventanas conservan sus coordenadas originales. Los materiales de los prefabs en disco y los del bioma normal no se sustituyen.
- Las barreras reconstruidas junto a la pista reciben coordenadas basadas en cada sección original, evitando deformarlas según las curvas. No se guardan copias adicionales de esas barreras en la caché de mallas.
- El asfalto usa coordenadas continuas a lo largo de la pista, con un ciclo de 80 metros para sus marcas pintadas. Las fachadas usan la textura de marcos, reflejos y ventanas iluminadas con intensidad más contenida.
- Se habilitó lectura de mallas solo en los modelos que necesitan el nuevo mapeado. Se restituyeron los ajustes originales de hidrante y jardinera, que quedaron fuera de esta pasada.

## Pruebas

- `unity recompile`: cero errores y advertencias.
- `ReviewCityRace.Before`: carrera original en Play Mode, entrada R2 con movimiento comprobado y cero errores de consola. La captura inicial conserva el cuadro de controles; las posteriores se tomaron con el HUD oculto para inspeccionar las superficies.
- `ReviewCityRace.After`: carrera real con 7921 renderers. Comprobaciones de cobertura: 1208 referencias de hormigón, 2511 de metal y 571 de fachada; 26 materiales de superficie compartidos. Cantidad de coordenadas igual a los vértices y rango no colapsado en ambas barreras y la carretera.
- Con R2, el tren recorrió 37,8 metros durante el tramo comprobado. Se inspeccionaron parrilla, barrera y conducción con el HUD, donde se alcanzan 158 km/h. Cero errores de consola.
- `ReviewCityRace.Regression`: retos de cajas del infierno y trinchera alienígena abiertos en Play Mode; conservan sus carreteras originales, no usan los materiales nuevos de ciudad y avanzan fotogramas. Cero errores. No se repitieron los combates ni la carrera completa: esta revisión cubre presentación e integración, no balance ni una medición formal de rendimiento.
- Capturas inspeccionadas: `after-grid.png`, `after-barrier.png` y `after-driving.png`. Asfalto y marcas legibles, paneles de barrera visibles, fachadas con marcos y reflejos, sin estirar las ventanas.
- Partida, selección y escena originales restauradas. Verificación: partida idéntica a la copia previa, escena `historiaDialogos`, Editor fuera de Play Mode.

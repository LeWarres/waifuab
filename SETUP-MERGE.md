# Smart Merge de Unity (una vez por equipo)

El repo ya indica en `.gitattributes` que escenas, prefabs y assets se fusionan con
`UnityYAMLMerge`. Cada persona tiene que decirle a su git dónde está esa herramienta.
Se hace una sola vez, dentro de la carpeta del proyecto.

## Windows

Ajusta la versión de Unity si la tuya es otra (el proyecto usa 6000.4.5f1):

```bash
git config merge.unityyamlmerge.name "Unity Smart Merge"
```

```bash
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A"
```

```bash
git config merge.unityyamlmerge.recursive binary
```

## macOS

```bash
git config merge.unityyamlmerge.name "Unity Smart Merge"
```

```bash
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.4.5f1/Unity.app/Contents/Tools/UnityYAMLMerge' merge -h -p --force %O %B %A %A"
```

```bash
git config merge.unityyamlmerge.recursive binary
```

## Comprobar

```bash
git config --get merge.unityyamlmerge.driver
```

Debe imprimir la ruta. Si no está configurado, git no falla: simplemente fusiona esos
archivos como texto normal, igual que antes.

Smart Merge resuelve solo los cambios que no se pisan (objetos distintos, propiedades
distintas). Si dos personas cambian la misma propiedad del mismo objeto, sigue habiendo
conflicto y hay que elegir a mano.

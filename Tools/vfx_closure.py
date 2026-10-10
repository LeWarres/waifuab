# Lists what can be deleted from Gabriel Aguiar's VFX packs while keeping Resources/Vfx working.
# VFX graphs point at shader graphs, subgraphs and textures in ways AssetDatabase.GetDependencies does not
# report, but the GUIDs are in the files as text: follow them from the kept prefabs until nothing new turns up.
#   python Tools/vfx_closure.py   (run from the project root; writes Tools/vfx_delete.txt)
import collections, os, re

ROOT = "Assets/GabrielAguiarProductions"
KEPT = "Assets/Resources/Vfx"
BINARY = (".png", ".tga", ".psd", ".fbx", ".exr", ".jpg", ".tif", ".tiff", ".hdr")

guid_path = {}
for folder, _, files in os.walk(ROOT):
    for name in files:
        if name.endswith(".meta"):
            text = open(os.path.join(folder, name), encoding="utf-8", errors="ignore").read()
            found = re.search(r"guid: ([0-9a-f]{32})", text)
            if found:
                guid_path[found.group(1)] = os.path.join(folder, name[:-5]).replace(os.sep, "/")

todo = [KEPT + "/" + name for name in os.listdir(KEPT) if name.endswith(".prefab")]
keep = set()
guid = re.compile(rb"[0-9a-f]{32}")
while todo:
    path = todo.pop()
    if not os.path.isfile(path) or path.lower().endswith(BINARY):
        continue
    for hit in set(guid.findall(open(path, "rb").read())):
        target = guid_path.get(hit.decode())
        if target and target not in keep:
            keep.add(target)
            todo.append(target)

files = [p for p in guid_path.values() if os.path.isfile(p)]
gone = [p for p in files if p not in keep]
open("Tools/vfx_delete.txt", "w", encoding="utf-8").write("\n".join(gone))
print("keep", len(keep), "delete", len(gone), "of", len(files))
print(collections.Counter(os.path.splitext(p)[1] for p in keep).most_common(12))
print(sum(os.path.getsize(p) for p in keep if os.path.isfile(p)) // 1048576, "MB kept")

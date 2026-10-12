# The paint of every character in Assets/Resources/Characters, out of its FBX and beside it as <name>.png:
# these models carry their texture inside the file, where the game cannot reach it.
#   blender -b --python Tools/extract_character_textures.py
import bpy, os, glob, shutil, tempfile
root = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Characters")
work = tempfile.mkdtemp(prefix="paint_")
for path in sorted(glob.glob(root + "/*/*.fbx")):
    target = path[:-4] + ".png"
    if os.path.exists(target) or glob.glob(os.path.join(os.path.dirname(path), "*.mat")):
        continue  # done already, or it came with a material of its own
    bpy.ops.wm.read_factory_settings(use_empty=True)
    copy = os.path.join(work, "model.fbx")
    shutil.copyfile(path, copy)  # so nothing is unpacked into the project
    bpy.ops.import_scene.fbx(filepath=copy)
    paints = [n.image for o in bpy.context.scene.objects if o.type == "MESH" for s in o.material_slots if s.material and s.material.node_tree
              for n in s.material.node_tree.nodes if n.type == "TEX_IMAGE" and n.image and n.outputs[0].is_linked]
    colour = [i for i in paints if i.colorspace_settings.name == "sRGB"] or paints
    if not colour:
        print("PAINT none", path); continue
    image = colour[0]
    image.file_format = "PNG"
    image.save(filepath=target)
    print("PAINT", os.path.basename(target), tuple(image.size), len(paints), "images")
shutil.rmtree(work, ignore_errors=True)

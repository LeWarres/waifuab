# Turns a VRoid character (.vrm) into a seated gunner for the turret: poses it sitting with the arms
# forward, bakes the pose, thins the mesh, shrinks the textures and exports FBX + PNGs for Unity.
#   blender -b --python Tools/make_gunner_vrm.py -- path/to/character.vrm
# The result shares the turret's origin and aims along -Y, like Tools/make_wagon.py.
import bpy, math, os, shutil, sys, tempfile
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = sys.argv[-1]
OUT = os.path.join(ROOT, "Assets", "Resources", "Models", "GunnerVrm")
KEEP = 0.3          # share of the triangles kept
TEXTURE = 512       # texture side, pixels
SEAT = Vector((0.0, 0.16, 0.1))  # where the hips go, in turret space
HEIGHT = 0.95       # hips to the top of the head once seated

bpy.ops.wm.read_factory_settings(use_empty=True)
glb = os.path.join(tempfile.mkdtemp(), "character.glb")  # a .vrm is a .glb with extras
shutil.copy(SOURCE, glb)
bpy.ops.import_scene.gltf(filepath=glb)

rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="POSE")

def bone(suffix):
    return next(b for b in rig.pose.bones if b.name.endswith(suffix))

def world(b):
    return rig.matrix_world @ b.head

# Turns a bone (and what hangs from it) around a world axis through its own head.
def turn(suffix, axis, degrees):
    b = bone(suffix) if isinstance(suffix, str) else suffix
    pivot = b.head.copy()
    spin = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(degrees), 4, rig.matrix_world.inverted().to_3x3() @ Vector(axis)) @ Matrix.Translation(-pivot)
    b.matrix = spin @ b.matrix
    bpy.context.view_layer.update()

front = 1.0 if world(bone("L_ToeBase")).y > world(bone("L_Foot")).y else -1.0  # which way along Y she faces
for side in ("L", "R"):
    out = 1.0 if world(bone(side + "_UpperArm")).x > 0 else -1.0
    turn(side + "_UpperLeg", (1, 0, 0), front * 85)
    turn(side + "_LowerLeg", (1, 0, 0), -front * 80)
    turn(side + "_UpperArm", (0, 0, 1), out * front * 78)
    turn(side + "_UpperArm", (1, 0, 0), -front * 28)
# The skirt has its own bones and does not follow the legs: lift its front (and half as much its sides).
skirt = [b for b in rig.pose.bones if "Skirt" in b.name and "Skirt" not in b.parent.name]
print("SKIRT", [b.name for b in skirt])
for b in skirt:
    lift = 80 if "Front" in b.name else 40 if "Side" in b.name else 0
    if lift:
        turn(b, (1, 0, 0), front * lift)
hips = world(bone("_Hips")).copy()
bpy.ops.object.mode_set(mode="OBJECT")

# Bake the pose into the meshes (shape keys would block it), then make one object of them.
for ob in meshes:
    bpy.context.view_layer.objects.active = ob
    if ob.data.shape_keys:
        ob.shape_key_clear()
    for mod in list(ob.modifiers):
        if mod.type == "ARMATURE":
            bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.parent = None
bpy.data.objects.remove(rig)
bpy.ops.object.select_all(action="DESELECT")
for ob in meshes:
    ob.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
girl = bpy.context.object
girl.name = "GunnerVrm"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

thin = girl.modifiers.new("thin", "DECIMATE")
thin.ratio = KEEP
bpy.ops.object.modifier_apply(modifier="thin")

# Into turret space: facing -Y, hips on the seat, sized by the seated height.
top = max((v.co.z for v in girl.data.vertices))
fit = Matrix.Scale(HEIGHT / (top - hips.z), 4) @ Matrix.Rotation(math.pi if front > 0 else 0.0, 4, "Z") @ Matrix.Translation(-hips)
girl.data.transform(Matrix.Translation(SEAT) @ fit)
girl.data.update()

os.makedirs(OUT, exist_ok=True)
for i, image in enumerate(list(bpy.data.images)):
    if not image.has_data and image.size[0] == 0:
        continue
    image.scale(TEXTURE, TEXTURE)
    image.name = "gunner_%02d" % i
    image.filepath_raw = os.path.join(OUT, image.name + ".png")
    image.file_format = "PNG"
    image.save()

bpy.ops.object.select_all(action="DESELECT")
girl.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "GunnerVrm.fbx"), use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, object_types={"MESH"}, mesh_smooth_type="OFF", path_mode="RELATIVE")
lo = [min(v.co[i] for v in girl.data.vertices) for i in range(3)]
hi = [max(v.co[i] for v in girl.data.vertices) for i in range(3)]
print("MODEL GunnerVrm tris", sum(len(p.vertices) - 2 for p in girl.data.polygons), "front", front,
      "lo", [round(v, 2) for v in lo], "hi", [round(v, 2) for v in hi], "mats", len(girl.data.materials))

# Preview.
s = bpy.context.scene
cam = bpy.data.objects.new("c", bpy.data.cameras.new("c"))
s.collection.objects.link(cam)
s.camera = cam
w = bpy.data.worlds.new("w")
w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (1, 1, 1, 1)
s.world = w
s.render.engine = "CYCLES"
s.cycles.samples = 16
s.render.resolution_x, s.render.resolution_y = 900, 800
s.view_settings.view_transform = "Standard"
cam.location = (1.6, -2.4, 1.2)
cam.rotation_euler = (Vector((0, 0, 0.45)) - cam.location).to_track_quat("-Z", "Y").to_euler()
s.render.filepath = os.path.join(ROOT, "Tools", "gunner_vrm_view.png")
bpy.ops.render.render(write_still=True)

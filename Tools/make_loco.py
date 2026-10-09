# Builds the low-poly diesel locomotive, renders a preview and exports the FBX for Unity.
#   blender -b --python Tools/make_loco.py
# Front is +X, up is +Z, the rail top is z = 0. Sized in "design units" and scaled to the game's 5-long wagon.
import bpy, math, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCALE = 5.0 / 8.4

bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, rgb, rough=0.6, metal=0.0, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emit:
        b.inputs["Emission Color"].default_value = (*rgb, 1)
        b.inputs["Emission Strength"].default_value = emit
    m.diffuse_color = (*rgb, 1)
    return m

BLUE = mat("LocoBlue", (0.10, 0.14, 0.62))
NAVY = mat("LocoNavy", (0.05, 0.07, 0.30))
YELLOW = mat("LocoYellow", (0.90, 0.55, 0.08))
ORANGE = mat("LocoOrange", (0.75, 0.33, 0.06))
DARK = mat("LocoDark", (0.035, 0.04, 0.05), 0.5, 0.3)
STEEL = mat("LocoSteel", (0.42, 0.44, 0.5), 0.35, 0.8)
GLASS = mat("LocoGlass", (0.30, 0.62, 0.90), 0.1)
BRONZE = mat("LocoBronze", (0.30, 0.17, 0.09), 0.5, 0.4)
LAMP = mat("LocoLamp", (1.0, 0.95, 0.75), 0.3, 0.0, 3.0)
RED = mat("LocoRed", (0.9, 0.05, 0.05), 0.3, 0.0, 2.0)

parts = []

def mesh(name, verts, faces, m):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    parts.append(ob)
    return ob

def box(x0, x1, y0, y1, z0, z1, m, name="box"):
    return prism([(x0, z0), (x1, z0), (x1, z1), (x0, z1)], y0, y1, m, name)

# A side profile (x, z), counter-clockwise seen from -Y, extruded across the width.
def prism(profile, y0, y1, m, name="prism"):
    n = len(profile)
    verts = [(x, y0, z) for x, z in profile] + [(x, y1, z) for x, z in profile]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((j, i, n + i, n + j))
    return mesh(name, verts, faces, m)

# Same on both sides of the locomotive.
def both(fn, y0, y1, *args):
    fn(y0, y1, *args)
    fn(-y1, -y0, *args)

def cyl(center, radius, depth, axis, m, sides=14, name="cyl"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=center,
                                        rotation=(math.radians(90), 0, 0) if axis == "Y" else (0, math.radians(90), 0) if axis == "X" else (0, 0, 0))
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(m)
    parts.append(ob)
    return ob

# ---- running gear ----
box(-4.0, 4.0, -1.22, 1.22, 0.62, 0.86, DARK, "frame")
box(-1.1, 1.1, -0.95, 0.95, 0.18, 0.62, DARK, "tank")
for bx in (-2.5, 2.3):
    box(bx - 1.25, bx + 1.25, -1.12, 1.12, 0.22, 0.62, DARK, "bogie")
    for ax in (-0.8, 0.0, 0.8):
        cyl((bx + ax, 0, 0.36), 0.36, 2.3, "Y", STEEL, name="axle")
        cyl((bx + ax, 0, 0.36), 0.14, 2.44, "Y", DARK, sides=8, name="hub")
    both(lambda y0, y1: box(bx - 1.2, bx + 1.2, y0, y1, 0.3, 0.46, DARK, "bogie_beam"), 1.15, 1.2)
box(-4.25, -4.0, -0.25, 0.25, 0.5, 0.8, STEEL, "coupler")

# ---- long hood (engine), behind the cab ----
box(-3.9, 0.6, -1.12, 1.12, 0.86, 1.30, ORANGE, "hood_base")
box(-3.9, 0.6, -1.13, 1.13, 1.30, 1.42, YELLOW, "hood_stripe")
box(-3.9, 0.6, -1.14, 1.14, 1.42, 1.52, DARK, "hood_line")
box(-3.9, 0.6, -1.12, 1.12, 1.52, 2.95, BLUE, "hood")
# Roof with its shoulders cut off.
mesh("hood_roof", [(-3.9, -1.12, 2.95), (0.6, -1.12, 2.95), (0.6, 1.12, 2.95), (-3.9, 1.12, 2.95),
                   (-3.9, -0.75, 3.25), (0.6, -0.75, 3.25), (0.6, 0.75, 3.25), (-3.9, 0.75, 3.25)],
     [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (1, 2, 6, 5), (3, 0, 4, 7)], NAVY)
for vx in (-3.3, -2.3, -1.3):
    box(vx - 0.38, vx + 0.38, -0.55, 0.55, 3.25, 3.36, DARK, "vent")
cyl((-0.3, 0, 3.4), 0.2, 0.36, "Z", DARK, name="exhaust")
# Panels along the sides: doors below, windows above.
def side_panels(y0, y1):
    for i in range(5):
        x = -3.7 + i * 0.84
        box(x, x + 0.7, y0, y1, 1.62, 2.15, NAVY, "door")
    for i in range(3):
        x = -1.9 + i * 0.8
        box(x, x + 0.62, y0, y1, 2.3, 2.8, NAVY, "window_frame")
        box(x + 0.07, x + 0.55, y0 - 0.01 if y0 < 0 else y0, y1 + 0.01 if y1 > 0 else y1, 2.37, 2.73, GLASS, "window")
    box(-3.8, 0.5, y0, y1, 2.02 + 0.2, 2.02 + 0.24, STEEL, "handrail")
both(side_panels, 1.12, 1.16)

# ---- cab, with the windscreen leaning back ----
CAB = [(0.6, 0.86), (2.25, 0.86), (2.25, 2.25), (1.9, 3.5), (0.6, 3.5)]
prism(CAB, -1.25, 1.25, BLUE, "cab")
box(0.5, 2.0, -1.3, 1.3, 3.5, 3.6, NAVY, "cab_roof")
# Yellow wedge low on the cab sides, as on the reference.
both(lambda y0, y1: prism([(0.6, 0.86), (2.25, 0.86), (2.25, 2.0)], y0, y1, YELLOW, "cab_wedge"), 1.25, 1.27)
both(lambda y0, y1: box(0.6, 2.25, y0, y1, 0.86, 1.0, ORANGE, "cab_skirt"), 1.25, 1.28)
# Side windows of the cab.
both(lambda y0, y1: box(0.85, 1.75, y0, y1, 2.45, 3.2, BRONZE, "cab_window_frame"), 1.25, 1.28)
both(lambda y0, y1: box(0.92, 1.68, y0, y1, 2.52, 3.13, GLASS, "cab_window"), 1.25, 1.30)

# Windscreen: two panes lying on the sloped face, each in a bronze frame.
def on_slope(z, out):  # point on the cab's sloped front at height z, pushed out along the normal
    t = (z - 2.25) / (3.5 - 2.25)
    return 2.25 + (1.9 - 2.25) * t + out * 0.963, z + out * 0.27
def pane(y0, y1, z0, z1, out, m, name):
    (xa, za), (xb, zb) = on_slope(z0, out), on_slope(z1, out)
    (xc, zc), (xd, zd) = on_slope(z0, 0), on_slope(z1, 0)
    mesh(name, [(xa, y0, za), (xa, y1, za), (xb, y1, zb), (xb, y0, zb), (xc, y0, zc), (xc, y1, zc), (xd, y1, zd), (xd, y0, zd)],
         [(0, 1, 2, 3), (4, 0, 3, 7), (1, 5, 6, 2), (3, 2, 6, 7), (4, 5, 1, 0)], m)
for y0, y1 in ((-1.12, -0.07), (0.07, 1.12)):
    pane(y0, y1, 2.42, 3.36, 0.03, BRONZE, "screen_frame")
    pane(y0 + 0.08, y1 - 0.08, 2.5, 3.28, 0.05, GLASS, "screen")
    # wiper
    mid = (y0 + y1) * 0.5
    pane(mid - 0.02, mid + 0.02, 2.5, 3.1, 0.07, DARK, "wiper")

# Big lamp on the roof.
box(1.55, 2.0, -0.3, 0.3, 3.6, 4.05, BRONZE, "lamp_box")
cyl((2.01, 0, 3.83), 0.17, 0.06, "X", LAMP, name="lamp")

# ---- short nose ----
box(2.25, 3.45, -1.0, 1.0, 0.86, 2.1, YELLOW, "nose")
box(2.2, 3.5, -1.04, 1.04, 2.1, 2.2, BLUE, "nose_top")
box(2.25, 3.47, -1.02, 1.02, 0.86, 1.0, ORANGE, "nose_skirt")
cyl((2.7, -0.5, 2.3), 0.09, 0.22, "Z", DARK, name="horn")
# Blue light board on the nose front.
box(3.45, 3.55, -0.85, 0.85, 0.95, 1.45, BLUE, "board")
box(3.45, 3.55, -0.45, 0.45, 1.45, 2.0, BLUE, "board_top")
for y, z in ((-0.62, 1.2), (0.62, 1.2), (0.0, 1.62), (0.0, 1.85)):
    cyl((3.56, y, z), 0.15, 0.06, "X", STEEL, name="light_rim")
    cyl((3.58, y, z), 0.11, 0.06, "X", LAMP, name="light")
for y in (-0.5, 0.5):  # grab irons
    for z in (1.6, 1.8, 2.0):
        box(3.45, 3.49, y - 0.14 if y > 0 else y - 0.14, y + 0.14, z, z + 0.03, YELLOW, "grab")

# ---- front deck, pilot and rails ----
PILOT = [(3.5, 0.12), (4.35, 0.12), (4.15, 0.95), (3.5, 0.95)]
prism(PILOT, -1.25, 1.25, DARK, "pilot")
both(lambda y0, y1: prism([(3.9, 0.12), (4.6, 0.12), (4.3, 0.95), (3.9, 0.95)], y0, y1, DARK, "pilot_wing"), 0.55, 1.25)
box(4.2, 4.3, -0.42, 0.42, 0.3, 0.9, STEEL, "pilot_plate")
cyl((4.31, 0, 0.6), 0.22, 0.04, "X", LAMP, sides=16, name="pilot_disc").scale = (1, 0.75, 1.25)
both(lambda y0, y1: cyl((4.32, (y0 + y1) * 0.5, 0.88), 0.06, 0.05, "X", RED, name="tail"), 1.0, 1.2)
box(3.5, 4.15, -1.22, 1.22, 0.86, 0.95, STEEL, "deck")
def rails(y0, y1):
    ym = (y0 + y1) * 0.5
    for x in (2.4, 3.3, 4.1):
        cyl((x, ym, 1.4), 0.03, 0.95, "Z", STEEL, sides=6, name="post")
    box(2.4, 4.1, y0, y1, 1.84, 1.9, STEEL, "rail")
    for i in range(3):  # steps down to the ground
        box(2.55 + i * 0.05, 3.3, y0 - 0.02 if y0 < 0 else y0, y1 + 0.02 if y1 > 0 else y1, 0.2 + i * 0.2, 0.26 + i * 0.2, DARK, "step")
both(rails, 1.17, 1.23)
both(lambda y0, y1: box(4.07, 4.13, y0, y1, 1.84, 1.9, STEEL, "rail_front"), 0.45, 1.2)

# ---- one object, flat shaded, scaled to the game ----
bpy.ops.object.select_all(action="DESELECT")
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
loco = bpy.context.object
loco.name = "Locomotive"
loco.scale = (SCALE,) * 3
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.shade_flat()
print("TRIS", sum(len(p.vertices) - 2 for p in loco.data.polygons), "SIZE", tuple(round(v, 2) for v in loco.dimensions))

os.makedirs(os.path.join(ROOT, "Assets", "Resources", "Models"), exist_ok=True)
bpy.ops.export_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Locomotive.fbx"), use_selection=True,
                         apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                         object_types={"MESH"}, mesh_smooth_type="FACE")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "Tools", "locomotive.blend"))

# ---- preview render ----
scene = bpy.context.scene
bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, -0.01))
bpy.context.object.data.materials.append(mat("Ground", (0.45, 0.47, 0.55)))
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 4.0
sun.rotation_euler = (math.radians(50), 0, math.radians(120))
scene.collection.objects.link(sun)
world = bpy.data.worlds.new("World")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.45, 0.65, 1.0, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
scene.world = world
cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.lens = 45
scene.render.resolution_x, scene.render.resolution_y = 1280, 800
scene.render.engine = "CYCLES"
scene.cycles.samples = 48
scene.view_settings.view_transform = "Standard"
target = Vector((0.2, 0, 1.0))
for name, pos in (("loco_front", (6.5, -5.5, 2.6)), ("loco_back", (-6.0, 6.0, 4.0))):
    cam.location = Vector(pos)
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(ROOT, "Tools", name + ".png")
    bpy.ops.render.render(write_still=True)

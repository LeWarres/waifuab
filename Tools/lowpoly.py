# Shared helpers for the low-poly model scripts (run inside Blender).
# Convention: up is +Z, sizes are in game units, the model's origin is where Unity places it.
import bpy, math, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
parts = []  # everything built since the last finish()

def reset():
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

# A side profile (x, z), counter-clockwise seen from -Y, extruded across y0..y1.
def prism(profile, y0, y1, m, name="prism"):
    n = len(profile)
    verts = [(x, y0, z) for x, z in profile] + [(x, y1, z) for x, z in profile]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((j, i, n + i, n + j))
    return mesh(name, verts, faces, m)

# fn(y0, y1) on both sides of the model.
def both(fn, y0, y1):
    fn(y0, y1)
    fn(-y1, -y0)

def _added(m, name, smooth):
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(m)
    for p in ob.data.polygons:
        p.use_smooth = smooth
    parts.append(ob)
    return ob

AXIS = {"X": (0, math.radians(90), 0), "Y": (math.radians(90), 0, 0), "Z": (0, 0, 0)}

def cyl(center, radius, depth, axis, m, sides=14, name="cyl", top=None):
    if top is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=center, rotation=AXIS[axis])
    else:  # cone: radius at the bottom, top at the top
        bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius, radius2=top, depth=depth, location=center, rotation=AXIS[axis])
    return _added(m, name, False)

def ball(center, radius, m, name="ball", scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=9, radius=radius, location=center)
    ob = _added(m, name, True)
    ob.scale = scale
    return ob

def ring(center, radius, thick, m, name="ring"):
    bpy.ops.mesh.primitive_torus_add(major_segments=18, minor_segments=6, major_radius=radius, minor_radius=thick, location=center)
    return _added(m, name, True)

# Joins everything built so far into one object and exports it for Unity.
def finish(name):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in parts:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    parts.clear()
    folder = os.path.join(ROOT, "Assets", "Resources", "Models")
    os.makedirs(folder, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(folder, name + ".fbx"), use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, object_types={"MESH"}, mesh_smooth_type="OFF")
    print("MODEL", name, "tris", sum(len(p.vertices) - 2 for p in ob.data.polygons), "size", tuple(round(v, 2) for v in ob.dimensions))
    return ob

# Ground, sun and sky, then one picture per (file name, camera position, look-at point).
def render(shots, lens=45):
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
    scene.world = world
    cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.lens = lens
    scene.render.resolution_x, scene.render.resolution_y = 1280, 800
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.view_settings.view_transform = "Standard"
    for name, pos, look in shots:
        cam.location = Vector(pos)
        cam.rotation_euler = (Vector(look) - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(ROOT, "Tools", name + ".png")
        bpy.ops.render.render(write_still=True)

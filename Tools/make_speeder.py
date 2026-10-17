# The speeder bike the boarders ride in on (BrawlEvent): a pale grey futuristic cowling with a saddle and a tall pair of
# handlebars, a boxy engine at the back with black cable runs under it, two long black forks out front ending in
# a pair of steering vanes edged in cyan light, foot pedals and a small cannon slung underneath.
#   blender -b --python Tools/make_speeder.py           -> Assets/Resources/Models/Speeder.fbx
#   blender -b --python Tools/make_speeder.py -- shot   -> also a preview picture, Tools/speeder_preview.png
# About a unit long, centred on the origin, facing -Y (Unity's forward), +Z up; the saddle top at (0, 0.14, 0.22).
# Vertex colours, drawn untinted (as Tools/make_gunship.py).
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
_mat = mat
def mat(name, rgb):  # vertex colours are taken as they are by the toon shader: baked pre-darkened
    return _mat(name, tuple(c ** 2.2 for c in rgb))

RUST = mat("Hull", (0.6, 0.63, 0.68))         # pale grey hull
RUST_DARK = mat("HullDark", (0.3, 0.32, 0.38))  # dark grey panels
GRIME = mat("Glow", (0.35, 0.92, 1.0))         # cyan light strips
BLACK = mat("Black", (0.1, 0.1, 0.11))
GUNMETAL = mat("Gunmetal", (0.2, 0.2, 0.23))
YELLOW = mat("Cyan", (0.35, 0.92, 1.0))
RED = mat("Light", (0.6, 1.0, 1.0))

# A side profile (y, z), extruded across x0..x1.
def side(profile, x0, x1, m, name="side"):
    n = len(profile)
    verts = [(x0, y, z) for y, z in profile] + [(x1, y, z) for y, z in profile]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    return mesh(name, verts, faces, m)

def bar(a, b, thick, m):  # a square rod from a to b
    a, b = Vector(a), Vector(b)
    ob = box(-thick, thick, 0, (b - a).length, -thick, thick, m, "bar")
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector((0, 1, 0)).rotation_difference(b - a)
    ob.location = a
    return ob

# ---- the cowling: a long tapering nose, the saddle behind, a dark scuffed tip
side([(0.3, 0.02), (0.3, 0.17), (0.2, 0.2), (0.0, 0.19), (-0.14, 0.15), (-0.3, 0.07), (-0.33, 0.035), (-0.05, -0.02)], -0.075, 0.075, RUST, "cowl")
side([(-0.2, 0.125), (-0.3, 0.075), (-0.335, 0.04), (-0.27, 0.03), (-0.17, 0.08)], -0.078, 0.078, RUST_DARK, "nose")
for y in (-0.26, -0.22):
    box(-0.079, 0.079, y, y + 0.025, 0.06, 0.08, GRIME)                            # scuffs on the nose
box(-0.06, 0.06, 0.06, 0.22, 0.19, 0.225, BLACK)                                  # saddle
box(-0.055, 0.055, 0.2, 0.24, 0.2, 0.26, BLACK)                                   # its back rest
for x in (-0.08, 0.08):
    box(x - 0.004, x + 0.004, -0.02, 0.06, 0.1, 0.14, BLACK)                       # side vents
    box(x - 0.005, x + 0.005, 0.22, 0.27, 0.15, 0.18, RUST_DARK)                   # stripes
box(-0.02, 0.02, -0.1, -0.08, 0.185, 0.2, RED)                                    # the red light on top
# ---- the engine at the back, with fins and cables under it
side([(0.26, -0.06), (0.26, 0.17), (0.36, 0.15), (0.4, 0.0), (0.36, -0.07)], -0.1, 0.1, RUST, "engine")
for x in (-0.11, 0.11):
    side([(0.28, 0.17), (0.4, 0.13), (0.42, -0.04), (0.33, -0.09)], x - 0.01, x + 0.01, BLACK, "fin")
    box(x - 0.012, x + 0.012, 0.3, 0.32, 0.02, 0.06, YELLOW)                       # warning stripe
box(-0.065, 0.065, 0.0, 0.26, -0.09, -0.02, BLACK)                                # the engine block under the seat
for x in (-0.05, -0.017, 0.017, 0.05):
    cyl((x, 0.13, -0.065), 0.016, 0.26, "Y", GUNMETAL, sides=8)                    # cable runs
cyl((0, -0.06, -0.06), 0.035, 0.12, "Y", BLACK, sides=10)                         # a turbine up front
# ---- handlebars: two tall black arms, bent, with a grip each
for x in (-0.055, 0.055):
    bar((x, -0.12, 0.17), (x, -0.2, 0.3), 0.008, BLACK)
    bar((x, -0.2, 0.3), (x, -0.13, 0.36), 0.008, BLACK)
    cyl((x, -0.13, 0.365), 0.012, 0.03, "X", BLACK, sides=8)
# ---- the forks: two long black rods each side, out to the steering vanes
for x in (-0.03, 0.03):
    bar((x, -0.25, 0.04), (x, -0.78, 0.045), 0.009, BLACK)
    bar((x, -0.2, 0.0), (x, -0.7, 0.03), 0.006, BLACK)
box(-0.04, 0.04, -0.79, -0.76, 0.02, 0.07, BLACK)                                 # the bracket at the tip
for x in (-0.06, 0.06):                                                           # the vanes, weathered
    side([(-0.86, -0.03), (-0.72, -0.01), (-0.72, 0.11), (-0.82, 0.12), (-0.9, 0.05)], x - 0.008, x + 0.008, RUST, "vane")
    side([(-0.85, 0.07), (-0.77, 0.09), (-0.77, 0.05), (-0.84, 0.03)], x - 0.009, x + 0.009, GRIME, "weather")
    box(x - 0.01, x + 0.01, -0.75, -0.73, 0.08, 0.1, YELLOW)
# ---- pedals and the cannon underneath
for x in (-0.09, 0.09):
    bar((x * 0.7, 0.05, -0.05), (x, 0.15, -0.15), 0.008, BLACK)
    side([(0.08, -0.15), (0.24, -0.15), (0.22, -0.13), (0.1, -0.13)], x - 0.03, x + 0.03, BLACK, "pedal")
cyl((0, -0.32, -0.04), 0.012, 0.22, "Y", BLACK, sides=8)
cyl((0, -0.23, -0.04), 0.02, 0.06, "Y", GUNMETAL, sides=8)
finish("Speeder", paint=True)

if "shot" in sys.argv:
    reset()
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Speeder.fbx"))
    ob = bpy.context.selected_objects[0]
    vc = bpy.data.materials.new("VC")
    vc.use_nodes = True
    nodes = vc.node_tree.nodes
    attr = nodes.new("ShaderNodeVertexColor")
    gamma = nodes.new("ShaderNodeGamma")  # undo the pre-darkening, to see the colours as the game will
    gamma.inputs["Gamma"].default_value = 1 / 2.2
    vc.node_tree.links.new(attr.outputs["Color"], gamma.inputs["Color"])
    vc.node_tree.links.new(gamma.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    ob.data.materials.clear()
    ob.data.materials.append(vc)
    ob.location = (0, 0, 0.5)
    render([("speeder_preview", (2.0, -0.4, 0.9), (0, -0.2, 0.5))], lens=50)

# The gunship: the rank-and-file enemy (Combat "Normal"), a dieselpunk propeller craft in its own colours.
#   blender -b --python Tools/make_gunship.py           -> Assets/Resources/Models/Enemy_gunship.fbx
#   blender -b --python Tools/make_gunship.py -- shot   -> also a preview picture, Tools/gunship_preview.png
# Unit sized like the other enemies (Tools/make_enemies.py), centred on the origin, facing -Y (Unity's forward).
# Drawn instanced with one material, so the colours are vertex colours (finish paint=True) and the type's tint is
# white (EnemyType.painted).
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
# Unity's toon shader takes the vertex colours as they are, without undoing sRGB: they are baked pre-darkened.
_mat = mat
def mat(name, rgb):
    return _mat(name, tuple(c ** 2.2 for c in rgb))

OLIVE = mat("Olive", (0.46, 0.43, 0.2))
OLIVE_DARK = mat("OliveDark", (0.27, 0.26, 0.13))
KHAKI = mat("Khaki", (0.66, 0.6, 0.36))
STEEL = mat("Steel", (0.5, 0.53, 0.57))
GUNMETAL = mat("Gunmetal", (0.13, 0.14, 0.16))
GLASS = mat("Glass", (0.25, 0.85, 0.95))
GLASS_DARK = mat("GlassDark", (0.08, 0.3, 0.38))
ORANGE = mat("Orange", (1.0, 0.45, 0.08))
CREAM = mat("Cream", (0.92, 0.87, 0.72))
RED = mat("Red", (0.82, 0.14, 0.1))

def turned(ob, rx=0, ry=0, rz=0):
    ob.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    return ob

# ---- fuselage: a stout olive hull, khaki belly, steel panel bands, glass all round its nose
ball((0, -0.02, 0.0), 0.17, OLIVE, scale=(1, 2.35, 0.92))
ball((0, -0.02, -0.06), 0.15, KHAKI, scale=(1, 2.2, 0.6))                         # belly
for y in (-0.2, 0.08, 0.28):
    turned(ring((0, y, 0), 0.165 * (1 - abs(y) * 0.9), 0.012, STEEL), 90)          # panel bands
ball((0, -0.4, -0.02), 0.108, GLASS_DARK, scale=(1, 0.8, 0.85))                   # glass nose
turned(ring((0, -0.35, -0.02), 0.118, 0.022, GUNMETAL), 90)                       # its frame
for x in (-0.06, 0.06):
    box(x - 0.012, x + 0.012, -0.44, -0.3, -0.06, 0.02, GUNMETAL)                  # nose glazing bars
# cockpit: a slanted windscreen and the bubble turret on top
for x0, x1 in ((-0.1, -0.035), (-0.03, 0.03), (0.035, 0.1)):                      # framed windscreen panes
    turned(box(x0, x1, -0.008, 0.008, -0.045, 0.045, GLASS), 50).location = (0, -0.25, 0.11)
for x in (-0.1, -0.032, 0.032, 0.1):
    turned(box(x - 0.007, x + 0.007, -0.012, 0.012, -0.05, 0.05, GUNMETAL), 50).location = (0, -0.25, 0.11)
box(-0.105, 0.105, -0.22, -0.13, 0.13, 0.155, OLIVE_DARK)                          # cockpit roof
ball((0, -0.08, 0.16), 0.075, GLASS, scale=(1, 1.1, 0.85))                        # bubble
turned(ring((0, -0.08, 0.15), 0.075, 0.012, STEEL), 0)
for y in (-0.12, -0.02, 0.06):                                                    # side windows
    for x in (-0.168, 0.168):
        box(x - 0.006, x + 0.006, y - 0.03, y + 0.03, 0.02, 0.07, GLASS)
box(-0.04, 0.04, 0.0, 0.3, 0.12, 0.17, OLIVE_DARK)                                # spine
box(-0.01, 0.01, 0.12, 0.3, 0.17, 0.27, OLIVE_DARK)                               # dorsal fin
box(-0.012, 0.012, 0.14, 0.26, 0.22, 0.26, ORANGE)                                # its tip
box(-0.172, 0.172, -0.05, 0.03, -0.01, 0.04, CREAM)                               # cream fuselage stripe
for x in (-0.05, 0.05):                                                           # chin guns
    cyl((x, -0.38, -0.13), 0.018, 0.22, "Y", GUNMETAL, sides=8)
    cyl((x, -0.48, -0.13), 0.025, 0.03, "Y", STEEL, sides=8)
# ---- tail: a ducted fan, steel duct with an orange band, dark blades inside
cyl((0, 0.4, 0), 0.14, 0.12, "Y", STEEL, sides=16)
turned(ring((0, 0.4, 0), 0.142, 0.016, ORANGE), 90)
cyl((0, 0.47, 0), 0.12, 0.02, "Y", GUNMETAL, sides=16)
for a in range(6):
    turned(box(-0.012, 0.012, 0.45, 0.47, -0.11, 0.11, STEEL), 0, a * 30)
cyl((0, 0.49, 0), 0.03, 0.04, "Y", RED, sides=8, top=0.0)
# ---- wings, out to fins leaning outwards; a nacelle on each wing and one low on each fin
for side in (-1, 1):
    x0, x1 = sorted((0.12 * side, 0.43 * side))
    box(x0, x1, -0.1, 0.12, -0.03, 0.03, OLIVE)                                   # wing
    box(x0, x1, -0.12, -0.1, -0.025, 0.025, GUNMETAL)                             # leading edge
    box(x0, x1, 0.1, 0.13, -0.02, 0.02, OLIVE_DARK)                               # flap
    roundel = cyl((0.33 * side, 0.01, 0.032), 0.05, 0.006, "Z", CREAM, sides=12)  # roundel on the wing
    cyl((0.33 * side, 0.01, 0.036), 0.028, 0.006, "Z", RED, sides=12)
    # The fin: a swept plate (its profile drawn across y and z) with an orange band near its top.
    for shape, m, w in (([(-0.16, -0.26), (0.2, -0.22), (0.24, 0.13), (-0.05, 0.16)], OLIVE_DARK, 0.028),
                        ([(-0.07, 0.08), (0.225, 0.08), (0.232, 0.115), (-0.055, 0.125)], ORANGE, 0.032)):
        fin = turned(prism(shape, -w, w, m, "fin"), 0, 18 * side, 90)
        fin.location = (0.45 * side, 0, 0)
    for x, z in ((0.25 * side, 0.0), (0.48 * side, -0.17)):
        cyl((x, -0.12, z), 0.062, 0.3, "Y", OLIVE, sides=14)                      # nacelle
        cyl((x, -0.12, z), 0.064, 0.05, "Y", OLIVE_DARK, sides=14)                # its seam
        cyl((x, -0.27, z), 0.074, 0.05, "Y", STEEL, sides=14)                     # cowl ring
        cyl((x, -0.27, z), 0.05, 0.052, "Y", GUNMETAL, sides=14)                  # the engine inside
        cyl((x, 0.04, z), 0.045, 0.05, "Y", GUNMETAL, sides=10, top=0.025)        # exhaust
        turned(cyl((x, -0.31, z), 0.04, 0.08, "Y", RED, sides=12, top=0.0), 0, 0)  # spinner
        for k in range(3):                                                        # three tapered blades, orange tips
            for shape, m in (([(0.03, -0.024), (0.13, -0.016), (0.13, 0.016), (0.03, 0.024)], GUNMETAL),
                             ([(0.13, -0.016), (0.17, -0.011), (0.17, 0.011), (0.13, 0.016)], ORANGE)):
                blade = turned(prism(shape, -0.302, -0.29, m, "blade"), 8, k * 120 + (40 if z else 0))
                blade.location = (x, 0, z)
finish("Enemy_gunship", paint=True)

if "shot" in sys.argv:
    reset()
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Enemy_gunship.fbx"))
    ob = bpy.context.selected_objects[0]
    vc = bpy.data.materials.new("VC")
    vc.use_nodes = True
    nodes = vc.node_tree.nodes
    attr = nodes.new("ShaderNodeVertexColor")
    vc.node_tree.links.new(attr.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    ob.data.materials.clear()
    ob.data.materials.append(vc)
    ob.location = (0, 0, 0.6)
    render([("gunship_preview", (1.6, -1.9, 1.7), (0, 0, 0.55))], lens=50)

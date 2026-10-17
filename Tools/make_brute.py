# The brute: the heavy enemy (Combat "Tanque"), a horned one-eyed mech with huge fists and an arm cannon, in its
# own colours.
#   blender -b --python Tools/make_brute.py           -> Assets/Resources/Models/Enemy_brute.fbx
#   blender -b --python Tools/make_brute.py -- shot   -> also a preview picture, Tools/brute_preview.png
# Unit sized like the other enemies, centred on the origin, facing -Y (Unity's forward), +Z up. Vertex colours,
# drawn untinted (EnemyType.model), as Tools/make_gunship.py.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from mathutils import Euler

reset()
_mat = mat
def mat(name, rgb):  # vertex colours are taken as they are by the toon shader: baked pre-darkened
    return _mat(name, tuple(c ** 2.2 for c in rgb))

ORANGE = mat("Orange", (1.0, 0.52, 0.12))
ORANGE_DARK = mat("OrangeDark", (0.78, 0.3, 0.08))
PURPLE = mat("Purple", (0.45, 0.2, 0.62))
PURPLE_DARK = mat("PurpleDark", (0.24, 0.1, 0.36))
STEEL = mat("Steel", (0.55, 0.57, 0.62))
GUNMETAL = mat("Gunmetal", (0.14, 0.14, 0.17))
YELLOW = mat("Yellow", (1.0, 0.85, 0.2))
EYE = mat("Eye", (1.0, 0.25, 0.6))
CYAN = mat("Cyan", (0.3, 0.95, 0.95))

def turned(ob, rx=0, ry=0, rz=0):
    ob.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    return ob

def placed(ob, at, rx=0, ry=0, rz=0):  # built round the origin, turned about it, then carried to at
    e = Euler((math.radians(rx), math.radians(ry), math.radians(rz)))
    offset = Vector(ob.location)  # where it was built (a cylinder's centre): it turns with the rest
    ob.rotation_euler = e
    ob.location = Vector(at) + e.to_matrix() @ offset
    return ob

# ---- torso: a round armoured hull, a purple chest plate with vents, a steel belly
ball((0, 0, 0.1), 0.25, ORANGE, scale=(1.05, 0.9, 1))
ball((0, -0.03, -0.1), 0.17, STEEL, scale=(1.1, 0.9, 0.7))                         # belly
prism([(-0.16, -0.02), (0.16, -0.02), (0.12, 0.2), (-0.12, 0.2)], -0.25, -0.2, PURPLE, "chest")
for x in (-0.09, -0.03, 0.03, 0.09):                                                  # vents
    box(x - 0.018, x + 0.018, -0.262, -0.25, 0.02, 0.12, ORANGE_DARK)
turned(ring((0, 0, 0.0), 0.245, 0.018, PURPLE_DARK), 0)                             # waist band
for x, z in ((-0.2, 0.18), (0.2, 0.18), (-0.14, -0.02), (0.14, -0.02), (-0.2, 0.06), (0.2, 0.06)):
    cyl((x, -0.2 + abs(x) * 0.45, z), 0.016, 0.03, "Y", STEEL, sides=6)              # rivets
box(-0.012, 0.012, -0.2, 0.2, 0.3, 0.36, ORANGE_DARK)                                 # crest down the head
# ---- head: sunk into the hull, a dark visor and one big eye, two curved horns
ball((0, -0.06, 0.3), 0.13, ORANGE, scale=(1.1, 1, 0.8))
box(-0.11, 0.11, -0.2, -0.15, 0.27, 0.34, GUNMETAL)                                  # visor
cyl((0, -0.2, 0.305), 0.045, 0.03, "Y", EYE, sides=14)                               # the eye
turned(ring((0, -0.21, 0.305), 0.05, 0.01, STEEL), 90)
for side in (-1, 1):                                                                  # horns: a socket, then three bends
    at = Vector((0.085 * side, -0.05, 0.33))
    ring(tuple(at), 0.042, 0.014, PURPLE_DARK)
    radius = 0.042
    for k, (rx, ry, h) in enumerate(((-5, 28, 0.08), (-15, 10, 0.08), (-32, -8, 0.07))):
        top = radius * 0.66 if k < 2 else 0.0
        placed(cyl((0, 0, h / 2), radius, h, "Z", YELLOW if k else ORANGE_DARK, sides=10, top=top), tuple(at), rx, ry * side)
        at = at + Euler((math.radians(rx), math.radians(ry * side), 0)).to_matrix() @ Vector((0, 0, h * 0.92))
        radius = top
# ---- back: two exhaust stacks and a cyan power cell
for x in (-0.09, 0.09):
    cyl((x, 0.17, 0.28), 0.04, 0.18, "Z", STEEL, sides=10)
    cyl((x, 0.17, 0.38), 0.045, 0.03, "Z", GUNMETAL, sides=10)
box(-0.06, 0.06, 0.19, 0.24, 0.0, 0.16, CYAN)
# ---- arms: huge shoulders, steel upper arms, big armoured forearms; a fist on the left, a cannon on the right
for side in (-1, 1):
    x = 0.32 * side
    ball((x, 0, 0.2), 0.12, ORANGE)                                                   # shoulder
    turned(ring((x, 0, 0.2), 0.115, 0.018, PURPLE), 0, 90)
    cyl((x, 0, 0.06), 0.045, 0.16, "Z", STEEL, sides=10)                              # upper arm
    ball((x, 0, -0.03), 0.05, GUNMETAL)                                               # elbow
    ball((x, -0.02, -0.15), 0.1, ORANGE_DARK, scale=(0.95, 1, 1.3))                    # forearm
    box(x - 0.1 * side, x - 0.07 * side, -0.08, 0.04, -0.24, -0.06, PURPLE)           # forearm plate
    if side < 0:
        ball((x, -0.03, -0.31), 0.075, GUNMETAL, scale=(1, 1.1, 0.9))                 # fist
        for k in range(4):
            box(x - 0.06 + k * 0.035, x - 0.035 + k * 0.035, -0.12, -0.08, -0.36, -0.3, STEEL)  # knuckles
    else:
        cyl((x, -0.12, -0.2), 0.04, 0.22, "Y", GUNMETAL, sides=12)                    # arm cannon
        cyl((x, -0.24, -0.2), 0.05, 0.04, "Y", STEEL, sides=12)
        cyl((x, -0.262, -0.2), 0.03, 0.01, "Y", EYE, sides=12)                        # its glowing muzzle
# ---- legs: steel hips, short thick legs, broad dark feet
box(-0.13, 0.13, -0.08, 0.08, -0.26, -0.16, GUNMETAL)
for side in (-1, 1):
    x = 0.11 * side
    cyl((x, 0, -0.28), 0.05, 0.1, "Z", STEEL, sides=10)                               # thigh
    ball((x, -0.01, -0.36), 0.075, ORANGE, scale=(1, 1.05, 1.2))                      # shin armour
    ball((x, -0.06, -0.33), 0.03, PURPLE)                                             # knee light
    box(x - 0.07, x + 0.07, -0.13, 0.07, -0.5, -0.44, GUNMETAL)                       # foot
    box(x - 0.06, x + 0.06, -0.15, -0.12, -0.5, -0.46, YELLOW)                        # toe cap
finish("Enemy_brute", paint=True)

if "shot" in sys.argv:
    reset()
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Enemy_brute.fbx"))
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
    render([("brute_preview", (1.3, -2.1, 1.1), (0, 0, 0.5))], lens=50)

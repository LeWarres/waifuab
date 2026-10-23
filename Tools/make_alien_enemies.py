# The alien planet's own creatures, as the chapter's opening has them: "from fluffy things you want to hug to
# shapes I still struggle to describe… hexagons or something".
#   fluff     a fluffy ball of fur tufts with two big shiny eyes, little feet and a bulb on a stalk (walks)
#   hexa      a flying hexagon: a thin six-sided disc with a rim, one great eye and little fins
#   hexgiant  a walker of stacked hexagons, an eye in the middle one and hexagon fists
#   blender -b --python Tools/make_alien_enemies.py           -> Assets/Resources/Models/Enemy_<name>.fbx
#   blender -b --python Tools/make_alien_enemies.py -- shot   -> also Tools/alien_enemies_preview.png
# Same conventions as the mechs: unit sized, centred, facing -Y, +Z up, walkers with their feet at -0.5,
# vertex colours (pre-darkened, drawn untinted). Bright, to stand out on the planet's dark ground.
import math, os, random, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

def colours(**p):
    return {k: mat(k, tuple(c ** 2.2 for c in v)) for k, v in p.items()}

def eye(at, r, c, look=-1):
    # white, iris, pupil and a shine, looking out along -Y
    ball(at, r, c["white"], scale=(1, 0.6, 1))
    ball((at[0], at[1] + look * r * 0.45, at[2]), r * 0.62, c["iris"], scale=(1, 0.5, 1))
    ball((at[0], at[1] + look * r * 0.62, at[2]), r * 0.34, c["pupil"], scale=(1, 0.5, 1))
    ball((at[0] + r * 0.25, at[1] + look * r * 0.72, at[2] + r * 0.3), r * 0.13, c["white"])

def fluff():
    random.seed(1)
    c = colours(fur=(1.0, 0.55, 0.8), fur2=(0.95, 0.75, 0.95), white=(1, 1, 1), iris=(0.3, 0.8, 1.0), pupil=(0.05, 0.05, 0.08), foot=(0.55, 0.25, 0.6), bulb=(1.0, 1.0, 0.4))
    ball((0, 0, 0.0), 0.36, c["fur"])
    for k in range(18):  # tufts all over (few: it comes in crowds)
        z = random.uniform(-1, 1); a = random.uniform(0, math.tau); r = math.sqrt(1 - z * z)
        ball((math.cos(a) * r * 0.36, math.sin(a) * r * 0.36, z * 0.36), random.uniform(0.13, 0.18), c["fur2"] if k % 3 == 0 else c["fur"])
    for s in (-1, 1):
        eye((0.13 * s, -0.33, 0.08), 0.11, c)
        ball((0.16 * s, 0.0, -0.42), 0.09, c["foot"], scale=(1.2, 1.5, 0.6))
    cyl((0, 0.05, 0.46), 0.015, 0.2, "Z", c["foot"], sides=6)
    ball((0, 0.05, 0.58), 0.06, c["bulb"])

def hexa():
    c = colours(face=(0.35, 1.0, 0.85), rim=(0.15, 0.45, 0.6), white=(1, 1, 1), iris=(1.0, 0.4, 0.7), pupil=(0.05, 0.05, 0.08), fin=(1.0, 0.9, 0.3))
    cyl((0, 0, 0), 0.42, 0.14, "Y", c["face"], sides=6)              # the hexagon, face to -Y
    cyl((0, 0.02, 0), 0.46, 0.08, "Y", c["rim"], sides=6)            # its rim
    eye((0, -0.08, 0), 0.17, c)
    for k in range(6):                                                # a fin at each corner
        a = k * math.pi / 3
        ball((math.cos(a) * 0.48, 0.06, math.sin(a) * 0.48), 0.06, c["fin"], scale=(1, 1.6, 1))

def hexgiant():
    c = colours(shell=(1.0, 0.82, 0.25), shell2=(0.85, 0.55, 0.2), core=(0.65, 0.35, 0.9), white=(1, 1, 1), iris=(0.4, 1.0, 0.4), pupil=(0.05, 0.05, 0.08), glow=(0.5, 1.0, 0.9))
    cyl((0, 0, -0.38), 0.28, 0.24, "Z", c["shell2"], sides=6)        # the lowest hexagon, its legs
    for s in (-1, 1):
        cyl((0.15 * s, 0, -0.46), 0.1, 0.1, "Z", c["core"], sides=6)  # feet
    cyl((0, 0, -0.05), 0.36, 0.42, "Z", c["shell"], sides=6)         # the middle one, with the eye
    cyl((0, 0, 0.3), 0.26, 0.26, "Z", c["shell2"], sides=6)          # the top one
    cyl((0, 0, 0.47), 0.12, 0.08, "Z", c["glow"], sides=6)
    eye((0, -0.3, -0.03), 0.14, c)
    for s in (-1, 1):                                                 # hexagon fists on short arms
        cyl((0.42 * s, 0, 0.0), 0.05, 0.16, "X", c["core"], sides=6)
        cyl((0.55 * s, -0.02, -0.06), 0.14, 0.14, "Y", c["core"], sides=6)
        ball((0.55 * s, -0.1, -0.06), 0.05, c["glow"])

for name, build in (("fluff", fluff), ("hexa", hexa), ("hexgiant", hexgiant)):
    reset()
    build()
    finish("Enemy_" + name, paint=True)

if "shot" in sys.argv:
    reset()
    vc = bpy.data.materials.new("VC")
    vc.use_nodes = True
    nodes = vc.node_tree.nodes
    attr, gamma = nodes.new("ShaderNodeVertexColor"), nodes.new("ShaderNodeGamma")
    gamma.inputs["Gamma"].default_value = 1 / 2.2
    vc.node_tree.links.new(attr.outputs["Color"], gamma.inputs["Color"])
    vc.node_tree.links.new(gamma.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    for i, name in enumerate(("fluff", "hexa", "hexgiant")):
        bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Enemy_" + name + ".fbx"))
        ob = bpy.context.selected_objects[0]
        ob.data.materials.clear()
        ob.data.materials.append(vc)
        ob.location = (i * 1.4 - 1.4, 0, 0.55)
    render([("alien_enemies_preview", (1.2, -4.0, 2.4), (0.0, 0.0, 0.45))], lens=40)

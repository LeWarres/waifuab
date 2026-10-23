# The alien base's scenery, in place of the stone kit and torii gates it borrowed: glowing tech pylons, armoured
# wall pieces, a radar dish, an energy core in a cage, stacked supply crates and floor vents. One low-poly model
# each, named fortress__<name>, into Assets/Scenery/Props; Tools/bake_fortress_props.cs turns them into
# Resources/Scenery/fortress. Standing on z = 0, centred. Materials named Glow... come out bright.
#   blender -b --python Tools/make_fortress_props.py           (-- shot: also Tools/fortress_props_preview.png)
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

OUT = os.path.join(ROOT, "Assets", "Scenery", "Props")
os.makedirs(OUT, exist_ok=True)

def m(name, rgb, emit=0.0):
    return bpy.data.materials.get(name) or mat(name, rgb, emit=emit)

ARMOR = lambda: m("FortArmor", (0.24, 0.28, 0.32)); LIGHT = lambda: m("FortLight", (0.45, 0.5, 0.55))
GREEN = lambda: m("GlowFortGreen", (0.4, 1.0, 0.3), emit=3.0); RED = lambda: m("GlowFortRed", (1.0, 0.25, 0.2), emit=3.0)

def pylon():
    cyl((0, 0, 0.3), 1.0, 0.6, "Z", ARMOR(), sides=6)
    cyl((0, 0, 3.6), 0.45, 6.0, "Z", LIGHT(), sides=6, top=0.25)
    for k in range(4):
        cyl((0, 0, 1.4 + k * 1.4), 0.5 - k * 0.05, 0.18, "Z", GREEN(), sides=6)
    ball((0, 0, 6.8), 0.35, RED())

def wall():
    box(-3, 3, -0.5, 0.5, 0, 2.8, ARMOR())
    box(-3.1, 3.1, -0.6, 0.6, 2.8, 3.1, LIGHT())
    box(-2.6, 2.6, -0.52, -0.5, 1.2, 1.4, GREEN())
    for x in (-3, 3):
        box(x - 0.35, x + 0.35, -0.65, 0.65, 0, 3.4, LIGHT())

def radar():
    cyl((0, 0, 1.0), 0.3, 2.0, "Z", ARMOR(), sides=8)
    box(-0.9, 0.9, -0.9, 0.9, 0, 0.4, LIGHT())
    dish = ball((0, -0.3, 2.6), 1.4, LIGHT(), scale=(1, 0.3, 1))
    dish.rotation_euler = (math.radians(-30), 0, 0)
    cyl((0, -0.9, 2.9), 0.06, 1.2, "Y", ARMOR(), sides=6)
    ball((0, -1.5, 3.0), 0.16, RED())

def core():
    cyl((0, 0, 0.2), 1.3, 0.4, "Z", ARMOR(), sides=8)
    ball((0, 0, 1.6), 0.8, GREEN())
    for k in range(6):
        a = k * math.pi / 3
        cyl((math.cos(a) * 1.1, math.sin(a) * 1.1, 1.6), 0.08, 2.6, "Z", LIGHT(), sides=6)
    cyl((0, 0, 3.0), 1.3, 0.3, "Z", ARMOR(), sides=8)

def crates():
    box(-1.2, 0.2, -0.7, 0.7, 0, 1.4, ARMOR())
    box(0.3, 1.5, -0.6, 0.6, 0, 1.2, LIGHT())
    box(-0.8, 0.6, -0.5, 0.7, 1.4, 2.5, LIGHT())
    for x0, x1, z in ((-1.2, 0.2, 0.7), (0.3, 1.5, 0.6)):
        box(x0 + 0.1, x1 - 0.1, -0.72, -0.7, z - 0.1, z + 0.1, GREEN())

def vent():
    box(-0.9, 0.9, -0.9, 0.9, 0, 0.15, ARMOR())
    for k in range(5):
        box(-0.75, 0.75, -0.7 + k * 0.32, -0.6 + k * 0.32, 0.15, 0.2, GREEN() if k == 2 else LIGHT())

BUILDS = [("pylon", pylon), ("wall", wall), ("radar", radar), ("core", core), ("crates", crates), ("vent", vent)]
for name, build in BUILDS:
    reset()
    build()
    finish("fortress__" + name, OUT)

if "shot" in sys.argv:
    reset()
    for i, (name, _) in enumerate(BUILDS):
        bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "fortress__" + name + ".fbx"))
        for ob in bpy.context.selected_objects:
            ob.location = (i * 7 - 17, 0, 0)
    render([("fortress_props_preview", (0, -34, 14), (0, 0, 2.5))], lens=35)

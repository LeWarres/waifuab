# The alien planet's scenery, in place of the jungle trees and flower patches it borrowed: glowing mushroom
# clusters, hexagon crystal pillars, spongy coral towers, pods with a glowing slit, a hex rock arch and a few
# small hex shards. One low-poly model each, named alien__<name>, into Assets/Scenery/Props; Tools/bake_alien_props.cs
# turns them into Resources/Scenery/alien. Standing on z = 0, centred. Materials named Glow... come out bright.
#   blender -b --python Tools/make_alien_props.py           (-- shot: also Tools/alien_props_preview.png)
import math, os, random, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

OUT = os.path.join(ROOT, "Assets", "Scenery", "Props")
os.makedirs(OUT, exist_ok=True)

def m(name, rgb, emit=0.0):
    return bpy.data.materials.get(name) or mat(name, rgb, emit=emit)

def shrooms():
    random.seed(2)
    for k, (x, y, h, r) in enumerate(((0, 0, 3.2, 1.2), (1.3, 0.5, 2.0, 0.8), (-1.1, 0.6, 1.5, 0.6), (0.4, -1.1, 1.1, 0.45))):
        cyl((x, y, h / 2), 0.14 + r * 0.08, h, "Z", m("StemPale", (0.85, 0.8, 0.95)), sides=8)
        ball((x, y, h), r, m("GlowCapCyan" if k % 2 == 0 else "GlowCapPink", (0.3, 1.0, 0.9) if k % 2 == 0 else (1.0, 0.4, 0.85), emit=3.0), scale=(1, 1, 0.45))
        for d in range(4):
            a = d * math.pi / 2 + k
            ball((x + math.cos(a) * r * 0.55, y + math.sin(a) * r * 0.55, h + r * 0.28), r * 0.12, m("CapSpot", (1, 1, 1)))

def crystals():
    for k, (x, y, h, r, tilt) in enumerate(((0, 0, 7, 0.9, 0), (1.1, 0.4, 4.5, 0.6, 14), (-0.9, 0.7, 3.6, 0.5, -16), (0.3, -1.0, 2.4, 0.4, 9))):
        ob = cyl((x, y, h / 2), r, h, "Z", m("GlowCrystalLime" if k % 2 == 0 else "GlowCrystalViolet", (0.6, 1.0, 0.3) if k % 2 == 0 else (0.7, 0.4, 1.0), emit=3.0), sides=6, top=0.08)
        ob.rotation_euler = (math.radians(tilt), math.radians(tilt * 0.5), 0)
    cyl((0, 0, 0.15), 1.6, 0.3, "Z", m("RockIndigo", (0.25, 0.22, 0.4)), sides=6)

def coral():
    random.seed(5)
    for k in range(9):
        z = 0.6 + k * 0.6
        r = 1.0 - k * 0.07
        ball((math.sin(k) * 0.3, math.cos(k * 1.3) * 0.3, z), r, m("SpongePink", (1.0, 0.5, 0.7)), scale=(1, 1, 0.7))
        for h in range(3):
            a = random.uniform(0, math.tau)
            ball((math.cos(a) * r * 0.85, math.sin(a) * r * 0.85, z), 0.18, m("SpongeHole", (0.55, 0.2, 0.4)), scale=(1, 1, 0.8))

def pod():
    ball((0, 0, 1.4), 1.2, m("PodTeal", (0.25, 0.6, 0.65)), scale=(1, 1, 1.25))
    box(-0.12, 0.12, -1.22, -1.0, 0.6, 2.4, m("GlowSlit", (1.0, 0.9, 0.3), emit=3.0))
    for k in range(5):
        a = k * math.tau / 5
        placed = cyl((math.cos(a) * 1.0, math.sin(a) * 1.0, 0.35), 0.18, 0.7, "Z", m("PodRoot", (0.18, 0.35, 0.4)), sides=6, top=0.05)

def arch():
    for s in (-1, 1):
        for k in range(5):
            cyl((s * (2.4 - k * 0.15), 0, 0.5 + k * 0.95), 0.75 - k * 0.06, 0.95, "Z", m("RockHex", (0.38, 0.32, 0.55)), sides=6)
    for k in range(5):
        cyl((-1.6 + k * 0.8, 0, 5.0 + math.sin(k / 4 * math.pi) * 0.4), 0.55, 0.6, "X", m("RockHex", (0.38, 0.32, 0.55)), sides=6)
    ball((0, -0.4, 4.6), 0.3, m("GlowArch", (0.3, 1.0, 0.85), emit=3.0))

def shards():
    random.seed(8)
    for k in range(5):
        h = random.uniform(0.3, 0.8)
        ob = cyl((random.uniform(-0.7, 0.7), random.uniform(-0.7, 0.7), h / 2), random.uniform(0.08, 0.16), h, "Z", m("GlowShard", (0.4, 1.0, 0.8), emit=3.0), sides=6, top=0.02)
        ob.rotation_euler = (math.radians(random.uniform(-25, 25)), math.radians(random.uniform(-25, 25)), 0)

BUILDS = [("shrooms", shrooms), ("crystals", crystals), ("coral", coral), ("pod", pod), ("arch", arch), ("shards", shards)]
for name, build in BUILDS:
    reset()
    build()
    finish("alien__" + name, OUT)

if "shot" in sys.argv:
    reset()
    for i, (name, _) in enumerate(BUILDS):
        bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "alien__" + name + ".fbx"))
        for ob in bpy.context.selected_objects:
            ob.location = (i * 7 - 17, 0, 0)
    render([("alien_props_preview", (0, -34, 14), (0, 0, 2.5))], lens=35)

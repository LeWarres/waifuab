# Props for the two closed rooms: the wagon a fight happens in (BrawlEvent) and Faceless's cave (CaveEvent).
# Wagon: a crate with steel corners and bands, a barrel with hoops, a pile of sacks, a hanging lamp, a crate stack
# under a tarp. Cave: three irregular boulders, a stalactite, a stalagmite, a crystal cluster, a chunk of wall.
# One low-poly model each, named kit__<name>, into Assets/Scenery/Props; Tools/bake_interior_props.cs turns them
# into Resources/Interior. Standing on z = 0, centred. Materials named Glow... come out bright.
#   blender -b --python Tools/make_interior_props.py           (-- shot: also Tools/interior_props_preview.png)
import math, os, random, sys
import bmesh
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

OUT = os.path.join(ROOT, "Assets", "Scenery", "Props")
os.makedirs(OUT, exist_ok=True)

def m(name, rgb, emit=0.0):
    return bpy.data.materials.get(name) or mat(name, rgb, emit=emit)

WOOD, WOOD_DARK, STEEL, IRON = (0.62, 0.4, 0.22), (0.42, 0.26, 0.14), (0.55, 0.58, 0.64), (0.22, 0.23, 0.27)

def crate(s=1.0, z0=0.0, x0=0.0, turn=0.0):
    h = 1.1 * s
    w = 1.1 * s
    # the boards, a little inset between the frame
    box(x0 - w / 2 + 0.06, x0 + w / 2 - 0.06, -w / 2 + 0.06, w / 2 - 0.06, z0 + 0.06, z0 + h - 0.06, m("KitWood", WOOD))
    t = 0.09 * s
    for (ax, ay) in ((-1, -1), (-1, 1), (1, -1), (1, 1)):  # the frame's posts
        box(x0 + ax * w / 2 - (t if ax > 0 else 0), x0 + ax * w / 2 + (t if ax < 0 else 0), ay * w / 2 - (t if ay > 0 else 0), ay * w / 2 + (t if ay < 0 else 0),
            z0, z0 + h, m("KitWoodDark", WOOD_DARK))
    for z in (z0, z0 + h - t):  # top and bottom frame
        for ay in (-1, 1):
            box(x0 - w / 2, x0 + w / 2, ay * w / 2 - (t if ay > 0 else 0), ay * w / 2 + (t if ay < 0 else 0), z, z + t, m("KitWoodDark", WOOD_DARK))
        for ax in (-1, 1):
            box(x0 + ax * w / 2 - (t if ax > 0 else 0), x0 + ax * w / 2 + (t if ax < 0 else 0), -w / 2, w / 2, z, z + t, m("KitWoodDark", WOOD_DARK))
    for ay in (-1, 1):  # a board across the middle of two faces
        box(x0 - w / 2, x0 + w / 2, ay * (w / 2 + 0.01) - 0.03, ay * (w / 2 + 0.01) + 0.03, z0 + h / 2 - t / 2, z0 + h / 2 + t / 2, m("KitWoodDark", WOOD_DARK))
    for (ax, ay) in ((-1, -1), (-1, 1), (1, -1), (1, 1)):  # steel corner caps
        for z in (z0, z0 + h - 0.16 * s):
            box(x0 + ax * (w / 2 + 0.01) - 0.08 * s, x0 + ax * (w / 2 + 0.01) + 0.08 * s, ay * (w / 2 + 0.01) - 0.08 * s, ay * (w / 2 + 0.01) + 0.08 * s,
                z, z + 0.16 * s, m("KitSteel", STEEL))

def barrel():
    h, r = 1.25, 0.48
    staves = 14
    for k in range(staves):
        a = k * math.tau / staves
        ob = box(-0.11, 0.11, -0.035, 0.035, 0, h, m("KitWood" if k % 2 else "KitWoodDark", WOOD if k % 2 else WOOD_DARK))
        ob.location = (math.cos(a) * r, math.sin(a) * r, 0)
        ob.rotation_euler = (0, 0, a + math.pi / 2)
    cyl((0, 0, h / 2), r - 0.02, h - 0.04, "Z", m("KitWoodDark", WOOD_DARK), sides=staves)
    for z in (0.12, h / 2, h - 0.12):
        ring((0, 0, z), r + 0.04, 0.035, m("KitIron", IRON))

def sacks():
    random.seed(4)
    for k, (x, y, z, sx) in enumerate(((0, 0, 0.28, 1.0), (0.85, 0.15, 0.28, 0.95), (0.4, 0.05, 0.75, 0.9), (-0.7, -0.1, 0.26, 0.85))):
        ball((x, y, z), 0.45, m("KitSack" if k % 2 == 0 else "KitSackDark", (0.82, 0.72, 0.52) if k % 2 == 0 else (0.66, 0.56, 0.38)), scale=(sx * 1.25, 0.8, 0.62))
        ball((x + sx * 0.55, y, z + 0.08), 0.12, m("KitSackDark", (0.66, 0.56, 0.38)), scale=(1, 1, 0.8))  # the tied end

def lamp():
    cyl((0, 0, 1.0), 0.02, 2.0, "Z", m("KitIron", IRON), sides=6)                       # the cable
    cyl((0, 0, -0.05), 0.4, 0.06, "Z", m("KitShade", (0.2, 0.32, 0.28)), sides=16, top=0.12)  # the shade
    cyl((0, 0, 0.05), 0.14, 0.18, "Z", m("KitShade", (0.2, 0.32, 0.28)), sides=12)
    ball((0, 0, -0.14), 0.14, m("GlowBulb", (1.0, 0.85, 0.55), emit=4.0))

def stack():
    crate(1.0, 0, -0.6)
    crate(0.9, 0, 0.6)
    crate(0.8, 1.1, -0.3)
    # a tarp thrown over the top, tied down
    ob = ball((-0.1, 0, 1.75), 1.0, m("KitTarp", (0.28, 0.42, 0.36)), scale=(1.15, 0.72, 0.42))
    for x in (-0.8, 0.6):
        box(x - 0.03, x + 0.03, -0.62, 0.62, 0.0, 1.9, m("KitRope", (0.75, 0.62, 0.38)))

# ---- the cave ----
def rock(name, size, seed, squash=0.8, rgb=(0.33, 0.29, 0.42)):
    random.seed(seed)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=size, location=(0, 0, size * squash * 0.8))
    ob = bpy.context.object
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    for v in bm.verts:
        n = v.co.normalized()
        v.co += n * random.uniform(-0.22, 0.22) * size
        v.co.z *= squash
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.materials.append(m(name, rgb))
    parts.append(ob)
    return ob

def boulder_a():
    rock("CaveRock", 1.3, 11)
    rock("CaveRockDark", 0.6, 12, rgb=(0.2, 0.18, 0.27)).location = (1.0, 0.4, 0.0)

def boulder_b():
    ob = rock("CaveRock", 1.2, 21, squash=1.05)
    ob.scale = (1.2, 0.9, 1.0)
    rock("CaveRockDark", 0.5, 22, rgb=(0.2, 0.18, 0.27)).location = (-0.9, -0.5, 0.0)

def boulder_c():
    rock("CaveRock", 1.0, 31, squash=1.3)
    rock("CaveRock", 0.75, 32, squash=1.1).location = (0.9, 0.2, 0.0)
    rock("CaveRockDark", 0.45, 33, rgb=(0.2, 0.18, 0.27)).location = (-0.7, 0.5, 0.0)

def spike(name, up, h, r, seed, rgb):
    random.seed(seed)
    for k in range(3):
        hh = h * random.uniform(0.6, 1.0) if k else h
        rr = r * random.uniform(0.5, 0.8) if k else r
        ob = cyl((0, 0, 0), rr, hh, "Z", m(name, rgb), sides=7, top=0.02)
        ob.location = (random.uniform(-0.5, 0.5) * r * 2 if k else 0, random.uniform(-0.5, 0.5) * r * 2 if k else 0, hh / 2 if up else -hh / 2)
        if not up:
            ob.rotation_euler = (math.pi, 0, 0)
            ob.location.z = -hh / 2

def stalactite():
    spike("CaveRockDark", False, 2.4, 0.35, 41, (0.2, 0.18, 0.27))

def stalagmite():
    spike("CaveRock", True, 1.8, 0.4, 51, (0.33, 0.29, 0.42))

def crystals():
    random.seed(61)
    for k in range(6):
        h = random.uniform(0.5, 1.6)
        ob = cyl((0, 0, 0), random.uniform(0.1, 0.22), h, "Z", m("GlowCaveCrystal" if k % 2 == 0 else "GlowCaveCrystalPink", (0.3, 1.0, 0.85) if k % 2 == 0 else (0.95, 0.35, 0.85), emit=3.0), sides=6, top=0.02)
        ob.location = (random.uniform(-0.5, 0.5), random.uniform(-0.4, 0.4), h / 2)
        ob.rotation_euler = (math.radians(random.uniform(-25, 25)), math.radians(random.uniform(-25, 25)), 0)
    rock("CaveRockDark", 0.55, 62, squash=0.5, rgb=(0.2, 0.18, 0.27))

def wall():
    random.seed(71)
    for k in range(7):
        ob = rock("CaveRock" if k % 3 else "CaveRockDark", random.uniform(1.6, 2.6), 70 + k, squash=random.uniform(0.9, 1.4), rgb=(0.33, 0.29, 0.42) if k % 3 else (0.2, 0.18, 0.27))
        ob.location = (random.uniform(-3, 3), random.uniform(-0.8, 0.8), random.uniform(0, 3))

BUILDS = [("crate", lambda: crate()), ("barrel", barrel), ("sacks", sacks), ("lamp", lamp), ("stack", stack),
          ("boulder_a", boulder_a), ("boulder_b", boulder_b), ("boulder_c", boulder_c), ("stalactite", stalactite),
          ("stalagmite", stalagmite), ("crystals", crystals), ("wall", wall)]
for name, build in BUILDS:
    reset()
    build()
    finish("kit__" + name, OUT)

if "shot" in sys.argv:
    reset()
    for i, (name, _) in enumerate(BUILDS):
        bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "kit__" + name + ".fbx"))
        for ob in bpy.context.selected_objects:
            ob.location = ((i % 6) * 4.5 - 11, (i // 6) * 6, 0)
    render([("interior_props_preview", (0, -24, 12), (0, 3, 1.5))], lens=35)

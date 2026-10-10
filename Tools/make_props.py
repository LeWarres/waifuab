# Scenery the asset packs do not have, one small low-poly model each, named <biome>__<name>:
# cacti, snowy pines, a snowman, ice, lava, craters, antennas, glowing mushrooms, fortress pylons...
#   blender -b --python Tools/make_props.py
# They land in Assets/Scenery/Props; Tools/bake_scenery.cs turns them into game scenery (RealToon materials,
# prefab under Resources/Scenery/<biome>). A material whose name starts with Glow comes out bright enough to bloom.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
OUT = os.path.join(ROOT, "Assets", "Scenery", "Props")
os.makedirs(OUT, exist_ok=True)

def done(name):
    finish(name, OUT)

def cone(center, radius, height, m, sides=8, tip=0.0, tilt=(0, 0)):
    ob = cyl((center[0], center[1], center[2] + height * 0.5), radius, height, "Z", m, sides=sides, top=tip)
    ob.rotation_euler = (math.radians(tilt[0]), math.radians(tilt[1]), 0)
    return ob

GREEN = mat("CactusGreen", (0.25, 0.55, 0.28))
PINK = mat("CactusBloom", (1.0, 0.45, 0.6))
DRY = mat("DryBrush", (0.55, 0.42, 0.22))
BONE = mat("Bone", (0.92, 0.88, 0.78))
PINE = mat("PineGreen", (0.12, 0.38, 0.3))
SNOW = mat("Snow", (0.95, 0.97, 1.0))
BARK = mat("Bark", (0.33, 0.22, 0.14))
ICE = mat("Ice", (0.6, 0.85, 1.0), 0.15)
CARROT = mat("Carrot", (1.0, 0.5, 0.1))
COAL = mat("Coal", (0.06, 0.06, 0.07))
CAP = mat("MushroomRed", (0.9, 0.18, 0.15))
STEM = mat("MushroomStem", (0.95, 0.9, 0.8))
MOSS = mat("Moss", (0.3, 0.5, 0.18))
BASALT = mat("Basalt", (0.16, 0.12, 0.12))
LAVA = mat("GlowLava", (1.0, 0.4, 0.05), emit=3.0)
EMBER = mat("GlowEmber", (1.0, 0.65, 0.1), emit=3.0)
MOON = mat("MoonRock", (0.5, 0.52, 0.62))
MOONDARK = mat("MoonShade", (0.22, 0.23, 0.32))
METAL = mat("PanelMetal", (0.75, 0.78, 0.84), 0.35, 0.6)
BLUE = mat("GlowBlue", (0.3, 0.75, 1.0), emit=3.0)
VIOLET = mat("AlienViolet", (0.5, 0.25, 0.65))
TEAL = mat("GlowTeal", (0.2, 1.0, 0.8), emit=3.0)
MAGENTA = mat("GlowMagenta", (1.0, 0.3, 0.8), emit=3.0)
PALE = mat("AlienPale", (0.8, 0.75, 0.9))
HULL = mat("FortHull", (0.12, 0.15, 0.15), 0.4, 0.5)
HULL2 = mat("FortPlate", (0.22, 0.27, 0.27), 0.4, 0.5)
LIME = mat("GlowLime", (0.4, 1.0, 0.25), emit=3.0)

# ---------------- desert ----------------
cyl((0, 0, 1.5), 0.3, 3.0, "Z", GREEN, sides=10); ball((0, 0, 3.0), 0.3, GREEN)
for side, z, up in ((1, 1.3, 1.1), (-1, 1.8, 0.8)):
    cyl((side * 0.5, 0, z), 0.2, 0.7, "X", GREEN, sides=8)
    cyl((side * 0.8, 0, z + up * 0.5), 0.2, up, "Z", GREEN, sides=8); ball((side * 0.8, 0, z + up), 0.2, GREEN)
ball((0, 0, 3.3), 0.14, PINK)
done("desert__cactus_tall")

for x, y, r in ((0, 0, 0.5), (0.55, 0.2, 0.36), (-0.4, 0.35, 0.3), (0.1, -0.5, 0.28)):
    ball((x, y, r * 0.8), r, GREEN, scale=(1, 1, 0.85)); ball((x, y, r * 1.6), r * 0.2, PINK)
done("desert__cactus_round")

for i in range(7):
    a = i * 0.9
    cone((math.cos(a) * 0.25, math.sin(a) * 0.25, 0), 0.07, 0.9 + 0.2 * (i % 3), DRY, sides=5, tilt=(math.sin(a) * 28, -math.cos(a) * 28))
done("desert__dry_bush")

# ribs of something big, half buried
for i in range(5):
    y = -1.2 + i * 0.6
    for side in (-1, 1):
        cone((side * 0.55, y, 0), 0.09, 1.3 - abs(i - 2) * 0.15, BONE, sides=6, tilt=(0, side * 22))
cyl((0, 0, 0.12), 0.12, 3.0, "Y", BONE, sides=6)
done("desert__ribs")

# ---------------- snow ----------------
def pine(height, name):
    k = height / 4.5
    cyl((0, 0, 0.45 * k), 0.22 * k, 0.9 * k, "Z", BARK, sides=8)
    for i, (z, r, h) in enumerate(((0.7, 1.35, 1.7), (1.7, 1.05, 1.5), (2.7, 0.75, 1.4))):
        cone((0, 0, z * k), r * k, h * k, PINE, sides=9)
        cone((0, 0, (z + h * 0.45) * k), r * 0.6 * k, h * 0.58 * k, SNOW, sides=9)  # snow resting on each tier
    done(name)
pine(4.5, "snow__pine")
pine(2.8, "snow__pine_small")

ball((0, 0, 0.55), 0.6, SNOW); ball((0, 0, 1.35), 0.42, SNOW); ball((0, 0, 1.95), 0.3, SNOW)
cyl((0, -0.38, 1.95), 0.06, 0.3, "Y", CARROT, sides=6, top=0.0)
for x in (-0.11, 0.11):
    ball((x, -0.26, 2.05), 0.04, COAL)
cyl((0, 0, 2.25), 0.26, 0.05, "Z", COAL, sides=10); cyl((0, 0, 2.42), 0.17, 0.3, "Z", COAL, sides=10)
for side in (-1, 1):
    cyl((side * 0.65, 0, 1.45), 0.03, 0.6, "X", BARK, sides=5)
done("snow__snowman")

for x, y, h, t in ((0, 0, 2.4, (0, 0)), (0.45, 0.1, 1.6, (8, 16)), (-0.4, 0.2, 1.3, (-6, -18)), (0.05, -0.45, 1.0, (20, 2))):
    cone((x, y, 0), 0.28, h, ICE, sides=6, tilt=t)
done("snow__ice")

ball((0, 0, 0.1), 1.3, SNOW, scale=(1.2, 1, 0.35)); ball((0.9, 0.5, 0.05), 0.7, SNOW, scale=(1, 1, 0.35))
done("snow__drift")

# ---------------- jungle ----------------
cyl((0, 0, 0.6), 0.2, 1.2, "Z", STEM, sides=8); ball((0, 0, 1.25), 0.75, CAP, scale=(1, 1, 0.55))
for a in range(6):
    ball((math.cos(a) * 0.45, math.sin(a) * 0.45, 1.5), 0.09, STEM)
cyl((0.7, 0.3, 0.3), 0.1, 0.6, "Z", STEM, sides=8); ball((0.7, 0.3, 0.62), 0.35, CAP, scale=(1, 1, 0.55))
done("jungle__mushrooms")

cyl((0, 0, 0.45), 0.45, 3.4, "X", BARK, sides=10)
for x in (-1.0, 0.5):
    ball((x, 0.1, 0.85), 0.4, MOSS, scale=(1.5, 1, 0.35))
cone((1.2, 0.2, 0.4), 0.09, 0.7, BARK, sides=5, tilt=(-30, 20))
done("jungle__log")

# ---------------- volcano ----------------
for x, y, r, h, t in ((0, 0, 1.0, 5.0, (0, 0)), (1.0, 0.4, 0.6, 2.8, (6, 12)), (-0.8, -0.5, 0.5, 2.0, (-8, -10))):
    cone((x, y, 0), r, h, BASALT, sides=7, tip=0.08, tilt=t)
done("volcano__spire")

cyl((0, 0, 0.03), 2.2, 0.06, "Z", LAVA, sides=14)
for a in range(9):
    ang = a * 0.7
    ball((math.cos(ang) * 2.2, math.sin(ang) * 2.2, 0.1), 0.35 + 0.2 * (a % 3), BASALT, scale=(1.2, 1, 0.6))
done("volcano__lava_pool")

cone((0, 0, 0), 0.25, 3.0, COAL, sides=7, tip=0.06)
for z, t in ((1.2, (0, 55)), (1.8, (0, -50)), (2.2, (45, 10))):
    cone((0, 0, z), 0.08, 1.0, COAL, sides=5, tilt=t)
done("volcano__burnt_tree")

for x, y, h, t in ((0, 0, 1.6, (0, 0)), (0.35, 0.1, 1.0, (10, 20)), (-0.3, 0.15, 0.8, (-8, -22))):
    cone((x, y, 0), 0.22, h, EMBER, sides=6, tilt=t)
ball((0, 0, 0.1), 0.6, BASALT, scale=(1.2, 1, 0.4))
done("volcano__ember_crystal")

# ---------------- space ----------------
ring((0, 0, 0.12), 2.2, 0.35, MOON); cyl((0, 0, 0.03), 2.0, 0.06, "Z", MOONDARK, sides=16)
done("space__crater")

cyl((0, 0, 0.2), 0.7, 0.4, "Z", METAL, sides=10); cyl((0, 0, 1.6), 0.09, 2.6, "Z", METAL, sides=6)
cone((0, 0, 2.6), 0.9, 0.5, METAL, sides=12, tip=0.12).rotation_euler = (math.radians(200), 0, 0)
ball((0, 0, 3.1), 0.14, BLUE)
done("space__antenna")

ball((0, 0, 0), 1.6, METAL, scale=(1, 1, 0.75)); cyl((0, 0, 0.12), 1.75, 0.24, "Z", MOONDARK, sides=16)
box(-0.35, 0.35, -1.75, -1.4, 0.0, 0.9, MOONDARK); box(-0.2, 0.2, -1.78, -1.74, 0.15, 0.7, BLUE)
done("space__dome")

for x, y, h, t in ((0, 0, 2.0, (0, 0)), (0.4, 0.1, 1.3, (10, 18)), (-0.35, 0.2, 1.0, (-8, -20)), (0.1, -0.4, 0.8, (22, 0))):
    cone((x, y, 0), 0.24, h, BLUE, sides=6, tilt=t)
done("space__crystal")

# ---------------- alien ----------------
cyl((0, 0, 0.9), 0.16, 1.8, "Z", PALE, sides=8); ball((0, 0, 1.85), 0.8, TEAL, scale=(1, 1, 0.5))
cyl((0.8, 0.2, 0.45), 0.1, 0.9, "Z", PALE, sides=8); ball((0.8, 0.2, 0.95), 0.42, TEAL, scale=(1, 1, 0.5))
cyl((-0.5, -0.4, 0.3), 0.08, 0.6, "Z", PALE, sides=8); ball((-0.5, -0.4, 0.62), 0.3, MAGENTA, scale=(1, 1, 0.5))
done("alien__glowshroom")

for i in range(9):  # a stalk leaning over, a light at its tip
    t = i / 8.0
    ball((math.sin(t * 1.4) * 1.1, 0, 0.2 + t * 3.0 - t * t * 0.6), 0.34 - t * 0.2, VIOLET)
ball((math.sin(1.4) * 1.1, 0, 2.75), 0.26, MAGENTA)
done("alien__stalk")

ball((0, 0, 0.7), 0.9, VIOLET, scale=(1, 1, 0.85))
for a in range(7):
    ang = a * 0.9
    ball((math.cos(ang) * 0.7, math.sin(ang) * 0.7, 0.75 + 0.3 * math.sin(a * 2.0)), 0.2, TEAL)
done("alien__pod")

# ---------------- fortress ----------------
box(-0.6, 0.6, -0.6, 0.6, 0.0, 0.5, HULL2); box(-0.4, 0.4, -0.4, 0.4, 0.5, 4.6, HULL)
box(-0.42, -0.4, -0.12, 0.12, 0.8, 4.2, LIME); box(0.4, 0.42, -0.12, 0.12, 0.8, 4.2, LIME)
box(-0.7, 0.7, -0.7, 0.7, 4.6, 4.9, HULL2); ball((0, 0, 5.2), 0.3, LIME)
done("fortress__pylon")

box(-3.0, 3.0, -0.4, 0.4, 0.0, 2.2, HULL); box(-3.05, 3.05, -0.45, 0.45, 2.2, 2.45, HULL2)
both(lambda y0, y1: box(-2.8, 2.8, y0, y1, 1.0, 1.15, LIME), 0.4, 0.42)
for x in (-3.0, 0.0, 3.0):
    box(x - 0.3, x + 0.3, -0.55, 0.55, 0.0, 2.7, HULL2)
done("fortress__wall")

cyl((0, 0, 0.6), 0.9, 1.2, "Z", HULL, sides=10); cyl((0, 0, 1.5), 0.7, 0.6, "Z", HULL2, sides=10)
cyl((0, -0.9, 1.5), 0.12, 1.4, "Y", HULL, sides=6); ball((0, -1.6, 1.5), 0.14, LIME)
done("fortress__gun_post")

for x in (-0.9, 0.9):
    prism([(x - 0.5, 0), (x + 0.5, 0), (x + 0.12, 1.1), (x - 0.12, 1.1)], -0.5, 0.5, HULL2)
box(-1.5, 1.5, -0.12, 0.12, 0.35, 0.55, HULL); box(-1.5, 1.5, -0.14, 0.14, 0.75, 0.85, LIME)
done("fortress__barrier")

# ---------------- ocean: islands and things that float ----------------
SAND = mat("IslandSand", (0.95, 0.85, 0.6)); PALMTRUNK = mat("PalmTrunk", (0.5, 0.36, 0.2)); FROND = mat("PalmFrond", (0.2, 0.62, 0.3))
SEAROCK = mat("SeaRock", (0.45, 0.47, 0.52)); BUOYRED = mat("BuoyRed", (0.9, 0.15, 0.12)); HULLWOOD = mat("BoatWood", (0.55, 0.36, 0.2))
SAIL = mat("BoatSail", (0.97, 0.96, 0.9)); BEACON = mat("GlowBeacon", (1.0, 0.9, 0.5), emit=3.0); CORAL = mat("Coral", (1.0, 0.45, 0.5))
CORAL2 = mat("CoralOrange", (1.0, 0.65, 0.25)); PILE = mat("PierPile", (0.4, 0.28, 0.17)); TOWERRED = mat("TowerRed", (0.85, 0.2, 0.18))

def palm(x, y, lean, height=3.2):
    for i in range(6):  # a trunk that bends
        t = i / 5.0
        cyl((x + lean * t * t * 1.2, y, 0.5 + t * height), 0.16 - t * 0.05, height / 5 + 0.15, "Z", PALMTRUNK, sides=6)
    top = (x + lean * 1.2, y, 0.55 + height)
    for a in range(7):
        ang = a * math.tau / 7
        ob = cone(top, 0.28, 1.7, FROND, sides=4, tilt=(0, 0))
        bpy.context.scene.cursor.location = top
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        ob.rotation_euler = (math.radians(105), 0, ang)
    ball((top[0], top[1], top[2] - 0.1), 0.2, PALMTRUNK)

ball((0, 0, 0.0), 3.4, SAND, scale=(1.25, 1, 0.22)); ball((0.6, 0.3, 0.25), 1.6, SAND, scale=(1.2, 1, 0.3))
palm(-0.6, 0.2, 0.9); palm(1.2, -0.5, -0.7, 2.5)
ball((-2.2, -1.0, 0.25), 0.55, SEAROCK, scale=(1.2, 1, 0.7)); ball((2.6, 1.2, 0.2), 0.4, SEAROCK, scale=(1.2, 1, 0.7))
done("ocean__island_palms")

ball((0, 0, 0.0), 2.0, SAND, scale=(1.2, 1, 0.2)); palm(0.1, 0.0, 0.6, 2.6)
ball((0.9, 0.6, 0.2), 0.35, SEAROCK, scale=(1.2, 1, 0.7))
done("ocean__islet")

for x, y, r, h in ((0, 0, 1.3, 1.9), (1.4, 0.5, 0.8, 1.1), (-1.2, -0.6, 0.7, 0.8), (0.3, -1.3, 0.5, 0.5)):
    cone((x, y, -0.2), r, h + 0.2, SEAROCK, sides=7, tip=r * 0.35)
done("ocean__rocks")

cyl((0, 0, 0.25), 0.45, 0.5, "Z", BUOYRED, sides=10); cyl((0, 0, 0.55), 0.46, 0.12, "Z", SAIL, sides=10)
cone((0, 0, 0.6), 0.34, 0.9, BUOYRED, sides=10, tip=0.08); ball((0, 0, 1.6), 0.13, BEACON)
done("ocean__buoy")

prism([(-1.4, 0.55), (-1.0, 0.0), (1.0, 0.0), (1.6, 0.55)], -0.5, 0.5, HULLWOOD, "hull")
box(-1.3, 1.45, -0.56, 0.56, 0.5, 0.58, PALMTRUNK); cyl((0.1, 0, 1.6), 0.05, 2.2, "Z", PALMTRUNK, sides=6)
prism([(0.18, 0.75), (1.3, 0.8), (0.18, 2.6)], -0.03, 0.03, SAIL, "sail"); prism([(0.02, 0.9), (-0.9, 0.95), (0.02, 2.2)], -0.03, 0.03, SAIL, "jib")
done("ocean__boat")

ball((0, 0, 0.0), 1.9, SEAROCK, scale=(1.2, 1, 0.45))
cyl((0, 0, 2.3), 0.62, 3.6, "Z", SAIL, sides=10, top=0.45)
for z in (1.4, 2.6):
    cyl((0, 0, z), 0.6 - (z - 0.5) * 0.045, 0.5, "Z", TOWERRED, sides=10)
cyl((0, 0, 4.2), 0.62, 0.12, "Z", PILE, sides=10); cyl((0, 0, 4.55), 0.36, 0.6, "Z", BEACON, sides=8)
cone((0, 0, 4.85), 0.5, 0.55, TOWERRED, sides=10)
done("ocean__lighthouse")

for i, (x, y, h) in enumerate(((0, 0, 1.6), (0.9, 0.1, 1.2), (-0.8, 0.3, 1.9), (0.3, 0.9, 0.9))):
    cyl((x, y, h * 0.5 - 0.2), 0.14, h + 0.4, "Z", PILE, sides=7); cyl((x, y, h + 0.02), 0.17, 0.08, "Z", SAIL, sides=7)
done("ocean__piles")

for i in range(9):
    a = i * 0.8
    cone((math.cos(a) * 0.5 * (i % 3), math.sin(a) * 0.5 * (i % 3), -0.1), 0.12, 0.6 + 0.25 * (i % 4), CORAL if i % 2 else CORAL2, sides=5,
         tilt=(math.sin(a) * 22, -math.cos(a) * 22))
ball((0, 0, -0.05), 0.75, SEAROCK, scale=(1.3, 1, 0.3))
done("ocean__coral")

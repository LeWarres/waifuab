# One station per biome: Station_desert, Station_snow, Station_jungle, Station_volcano, Station_city,
# Station_space, Station_alien, Station_fortress. Each is a 34-long platform beside the track with its hall
# behind it, in the biome's own style.
#   blender -b --python Tools/make_stations.py
# Written in the game's axes: x along the track, z away from it (the platform starts 3 from the rails), y up.
# Materials carry their real colours; one whose name starts with Glow is made bright in the game.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()

def ubox(x0, x1, z0, z1, y0, y1, m, name="box"):
    return box(-x1, -x0, -z1, -z0, y0, y1, m, name)

def ucyl(x, z, y0, y1, radius, m, sides=10, top=None):
    return cyl((-x, -z, (y0 + y1) * 0.5), radius, y1 - y0, "Z", m, sides=sides, top=top)

def uball(x, z, y, radius, m, scale=(1, 1, 1)):
    return ball((-x, -z, y), radius, m, scale=(scale[0], scale[2], scale[1]))

# A ridge roof along the track: eaves at y0 over z0..z1, ridge at y1 in the middle.
def roof(x0, x1, z0, z1, y0, y1, m):
    zm = (z0 + z1) * 0.5
    v = [(-x0, -z0, y0), (-x0, -z1, y0), (-x0, -zm, y1), (-x1, -z0, y0), (-x1, -z1, y0), (-x1, -zm, y1)]
    return mesh("roof", v, [(0, 1, 2), (5, 4, 3), (0, 2, 5, 3), (2, 1, 4, 5), (1, 0, 3, 4)], m)

YELLOW = mat("SafetyYellow", (1.0, 0.78, 0.1))
DARK = mat("TrimDark", (0.08, 0.08, 0.1))
WHITE = mat("PaintWhite", (0.92, 0.92, 0.9))
PANE = mat("WindowBlue", (0.35, 0.65, 0.95), 0.1)
LAMP = mat("GlowLamp", (1.0, 0.92, 0.7), emit=3.0)

# What every station has: the platform with its safety line, lamps, a name board and benches.
def platform(floor, accent):
    ubox(-17, 17, 3, 9.4, 0, 0.6, floor, "platform")
    ubox(-17, 17, 2.94, 3.06, 0, 0.62, DARK, "edge")
    ubox(-17, 17, 3.2, 3.45, 0.6, 0.63, YELLOW, "line")
    for x in (-14, -5, 5, 14):
        ucyl(x, 8.9, 0.6, 3.6, 0.07, DARK, sides=6); ubox(x - 0.08, x + 0.08, 8.1, 8.95, 3.5, 3.6, DARK)
        uball(x, 8.1, 3.45, 0.16, LAMP)
    for x in (-11.5, 11.5):  # name boards
        ubox(x - 1.5, x + 1.5, 8.6, 8.72, 1.9, 2.7, accent); ubox(x - 1.3, x + 1.3, 8.58, 8.6, 2.1, 2.5, WHITE)
        for px in (x - 1.3, x + 1.3):
            ubox(px - 0.06, px + 0.06, 8.6, 8.72, 0.6, 1.9, DARK)
    for x in (-8.5, 8.5):  # benches
        ubox(x - 0.9, x + 0.9, 8.0, 8.45, 1.0, 1.08, accent); ubox(x - 0.9, x + 0.9, 8.4, 8.48, 1.08, 1.5, accent)
        for px in (x - 0.8, x + 0.8):
            ubox(px - 0.05, px + 0.05, 8.02, 8.45, 0.6, 1.0, DARK)

def windows(xs, z, y0, y1, m=PANE, half=0.55):
    for x in xs:
        ubox(x - half - 0.08, x + half + 0.08, z - 0.03, z, y0 - 0.08, y1 + 0.08, DARK)
        ubox(x - half, x + half, z - 0.06, z - 0.03, y0, y1, m)

# ---------------- desert: adobe hall, timber awning, water tower ----------------
ADOBE = mat("Adobe", (0.86, 0.66, 0.42)); ADOBE2 = mat("AdobeDark", (0.66, 0.45, 0.28)); TIMBER = mat("Timber", (0.42, 0.27, 0.15))
SANDSTONE = mat("Sandstone", (0.78, 0.68, 0.5))
platform(SANDSTONE, TIMBER)
ubox(-8, 8, 9.6, 14.2, 0.6, 4.8, ADOBE); ubox(-8.3, 8.3, 9.3, 14.5, 4.8, 5.2, ADOBE2)
for x in (-6, -2, 2, 6):
    ubox(x - 0.18, x + 0.18, 9.0, 9.6, 4.3, 4.6, TIMBER)  # beam ends
ubox(-0.9, 0.9, 9.5, 9.6, 0.6, 3.0, DARK); windows((-5.5, -3, 3, 5.5), 9.6, 2.0, 3.2)
ubox(-8.5, 8.5, 6.4, 9.7, 3.5, 3.65, TIMBER)
for x in (-8, -4, 0, 4, 8):
    ubox(x - 0.1, x + 0.1, 6.5, 6.7, 0.6, 3.5, TIMBER)
for lx, lz in ((11, 11), (13.4, 11), (11, 13.4), (13.4, 13.4)):
    ubox(lx - 0.1, lx + 0.1, lz - 0.1, lz + 0.1, 0, 4.2, TIMBER)
ucyl(12.2, 12.2, 4.2, 6.6, 1.7, TIMBER, sides=12); ucyl(12.2, 12.2, 6.6, 7.6, 1.85, ADOBE2, sides=12, top=0.2)
finish("Station_desert")

# ---------------- snow: log cabin under a steep snowy roof ----------------
LOG = mat("CabinLog", (0.45, 0.28, 0.16)); REDROOF = mat("RoofRed", (0.7, 0.2, 0.18)); SNOW = mat("RoofSnow", (0.96, 0.98, 1.0))
ICESTONE = mat("FrostStone", (0.8, 0.86, 0.94))
platform(ICESTONE, REDROOF)
ubox(-7.5, 7.5, 9.6, 14.2, 0.6, 4.2, LOG)
for y in (1.4, 2.2, 3.0, 3.8):
    ubox(-7.6, 7.6, 9.55, 9.6, y, y + 0.08, DARK)
roof(-8.3, 8.3, 8.6, 15.2, 4.1, 7.4, REDROOF); roof(-8.0, 8.0, 9.4, 14.4, 4.75, 7.6, SNOW)
ubox(3.5, 4.7, 11.2, 12.4, 5.5, 8.4, ICESTONE); ubox(3.3, 4.9, 11.0, 12.6, 8.4, 8.7, SNOW)
ubox(-0.9, 0.9, 9.5, 9.6, 0.6, 3.0, REDROOF); windows((-5, -2.8, 2.8, 5), 9.6, 2.0, 3.1, LAMP)
uball(-12, 12, 0.3, 2.2, SNOW, scale=(1.3, 0.4, 1)); uball(13, 11.5, 0.2, 1.6, SNOW, scale=(1.2, 0.4, 1))
finish("Station_snow")

# ---------------- jungle: a hut on stilts under thatch ----------------
BAMBOO = mat("BambooWall", (0.72, 0.62, 0.3)); THATCH = mat("Thatch", (0.5, 0.42, 0.2)); PLANK = mat("Plank", (0.5, 0.33, 0.18))
MOSSY = mat("MossStone", (0.5, 0.56, 0.42))
platform(MOSSY, PLANK)
for x in (-7, -2.4, 2.4, 7):
    for z in (10, 14):
        ucyl(x, z, 0, 2.0, 0.22, PLANK, sides=8)
ubox(-7.8, 7.8, 9.4, 14.6, 1.9, 2.2, PLANK); ubox(-7.2, 7.2, 10, 14, 2.2, 4.6, BAMBOO)
for x in range(-7, 8, 2):
    ubox(x - 0.07, x + 0.07, 9.95, 10, 2.2, 4.6, PLANK)
roof(-8.6, 8.6, 8.8, 15.2, 4.5, 7.8, THATCH); roof(-8.9, 8.9, 8.5, 15.5, 4.3, 4.9, PLANK)
for i in range(5):  # steps down to the platform
    ubox(-1.2, 1.2, 9.4 - (i + 1) * 0.35, 9.4 - i * 0.35, 0.6, 1.9 - i * 0.28, PLANK)
ubox(-0.8, 0.8, 9.95, 10, 2.2, 4.0, DARK); windows((-5, -3, 3, 5), 10, 3.0, 3.9)
finish("Station_jungle")

# ---------------- ocean: a pier on piles, a boathouse and a lighthouse ----------------
DECKWOOD = mat("PierDeck", (0.62, 0.45, 0.27)); PILEWOOD = mat("PierPost", (0.38, 0.26, 0.16)); SEAWHITE = mat("BoathouseWhite", (0.95, 0.96, 0.98))
SEABLUE = mat("BoathouseBlue", (0.15, 0.45, 0.85)); TOWER = mat("LightRed", (0.85, 0.2, 0.18)); SEALAMP = mat("GlowLight", (1.0, 0.9, 0.5), emit=3.0)
platform(DECKWOOD, SEABLUE)
for x in range(-16, 17, 4):
    for z in (3.4, 9.0, 14.0):
        ucyl(x, z, -0.2, 0.9, 0.2, PILEWOOD, sides=7)
ubox(-10, 12, 9.4, 14.6, 0.3, 0.6, DECKWOOD)
ubox(-8, 4, 9.8, 14.2, 0.6, 4.0, SEAWHITE)
for x in range(-8, 5, 2):
    ubox(x - 0.5, x + 0.5, 9.74, 9.8, 0.6, 4.0, SEABLUE if (x // 2) % 2 == 0 else SEAWHITE)
roof(-8.6, 4.6, 9.2, 14.8, 3.9, 6.4, SEABLUE)
ubox(-2.9, -1.1, 9.7, 9.8, 0.6, 3.0, DARK); windows((-6, 1.5), 9.72, 2.0, 3.1)
ucyl(9, 12, 0.6, 7.2, 1.25, SEAWHITE, sides=12, top=0.85)
for y0 in (1.8, 4.2):
    ucyl(9, 12, y0, y0 + 1.0, 1.25 - (y0 - 0.6) * 0.06, TOWER, sides=12)
ucyl(9, 12, 7.2, 7.45, 1.25, PILEWOOD, sides=12); ucyl(9, 12, 7.45, 8.5, 0.7, SEALAMP, sides=8); ucyl(9, 12, 8.5, 9.5, 0.95, TOWER, sides=12, top=0.05)
finish("Station_ocean")

# ---------------- volcano: a basalt bunker with furnace stacks ----------------
BASALT = mat("Bunker", (0.2, 0.16, 0.16)); BASALT2 = mat("BunkerTrim", (0.33, 0.24, 0.22)); LAVA = mat("GlowFurnace", (1.0, 0.42, 0.06), emit=3.0)
ASH = mat("AshStone", (0.36, 0.3, 0.3))
platform(ASH, BASALT2)
ubox(-8.5, 8.5, 9.6, 14.6, 0.6, 4.4, BASALT); ubox(-9, 9, 9.2, 15, 4.4, 4.9, BASALT2)
for x in (-8.5, -4.25, 0, 4.25, 8.5):
    ubox(x - 0.35, x + 0.35, 9.2, 9.6, 0.6, 4.4, BASALT2)
windows((-6.4, -2.1, 2.1, 6.4), 9.6, 2.2, 2.9, LAVA, half=1.2)
ubox(-1.0, 1.0, 9.5, 9.6, 0.6, 1.9, DARK)
for x in (-5.5, 5.5):
    ucyl(x, 12.5, 4.9, 8.6, 0.9, BASALT2, sides=10, top=0.65); ucyl(x, 12.5, 8.6, 8.75, 0.6, LAVA, sides=10)
finish("Station_volcano")

# ---------------- city: glass hall, flat roof, clock and sign ----------------
GLASS = mat("HallGlass", (0.4, 0.7, 0.95), 0.1); CONCRETE = mat("Concrete", (0.82, 0.83, 0.86)); SIGN = mat("SignBlue", (0.15, 0.35, 0.85))
TILE = mat("PlatformTile", (0.74, 0.75, 0.8))
platform(TILE, SIGN)
ubox(-9, 9, 9.6, 14.2, 0.6, 5.2, GLASS)
for x in range(-9, 10, 3):
    ubox(x - 0.18, x + 0.18, 9.5, 9.7, 0.6, 5.2, CONCRETE)
ubox(-9.2, 9.2, 9.5, 9.7, 2.9, 3.1, CONCRETE)
ubox(-10, 10, 6.6, 14.8, 5.2, 5.6, CONCRETE); ubox(-10, 10, 6.6, 6.8, 4.9, 5.2, SIGN)
for x in (-9.5, -3.2, 3.2, 9.5):
    ucyl(x, 6.9, 0.6, 5.2, 0.14, CONCRETE, sides=8)
ubox(-4.5, 4.5, 11, 11.3, 5.6, 7.4, SIGN); ubox(-4.1, 4.1, 10.97, 11, 5.9, 7.1, WHITE)
cyl((0, -9.45, 4.2), 0.75, 0.12, "Y", WHITE, sides=16); cyl((0, -9.38, 4.2), 0.82, 0.06, "Y", DARK, sides=16)
ubox(-0.04, 0.04, 9.3, 9.36, 4.2, 4.75, DARK); ubox(0, 0.4, 9.3, 9.36, 4.16, 4.24, DARK)
ubox(-1.2, 1.2, 9.5, 9.6, 0.6, 2.9, DARK)
finish("Station_city")

# ---------------- space: a dome with a mast ----------------
HULL = mat("DomeHull", (0.86, 0.88, 0.93), 0.3, 0.5); HULL2 = mat("DomeDark", (0.3, 0.33, 0.42)); BLUE = mat("GlowDock", (0.3, 0.75, 1.0), emit=3.0)
DECK = mat("DeckPlate", (0.5, 0.54, 0.66))
platform(DECK, HULL2)
ubox(-17, 17, 3.5, 3.7, 0.6, 0.64, BLUE)
uball(0, 12.2, 0.6, 5.2, HULL, scale=(1.25, 0.85, 0.72)); ring((0, -12.2, 0.9), 5.6, 0.25, HULL2).scale = (1.16, 0.72, 1)
ring((0, -12.2, 2.6), 4.9, 0.1, BLUE).scale = (1.19, 0.72, 1)
ubox(-1.6, 1.6, 8.6, 10.4, 0.6, 3.4, HULL2); ubox(-1.1, 1.1, 8.55, 8.6, 0.6, 2.9, BLUE)
for x in (-10.5, 10.5):
    uball(x, 12, 0.6, 2.4, HULL, scale=(1, 0.8, 1)); ucyl(x, 12, 2.4, 5.2, 0.08, HULL2, sides=6); uball(x, 12, 5.3, 0.2, BLUE)
ucyl(5.5, 13.5, 3.6, 8.2, 0.1, HULL2, sides=6); cyl((-5.5, -13.5, 8.0), 1.1, 0.5, "Z", HULL, sides=12, top=0.15).rotation_euler = (math.radians(150), 0, 0)
finish("Station_space")

# ---------------- alien: three living pods ----------------
POD = mat("PodViolet", (0.5, 0.24, 0.66)); POD2 = mat("PodDeep", (0.3, 0.12, 0.42)); TEAL = mat("GlowPod", (0.2, 1.0, 0.8), emit=3.0)
MEMBRANE = mat("Membrane", (0.62, 0.36, 0.72))
platform(MEMBRANE, POD2)
ubox(-17, 17, 3.5, 3.7, 0.6, 0.64, TEAL)
for x, z, r in ((0, 12.5, 4.4), (-7.5, 11.8, 3.0), (7.8, 12.0, 3.3)):
    uball(x, z, 0.6 + r * 0.55, r, POD, scale=(1, 0.95, 0.85))
    uball(x, z, 0.4, r * 1.05, POD2, scale=(1, 0.3, 0.9))
    for a in range(6):
        ang = a * 1.05 + x
        uball(x + math.cos(ang) * r * 0.78, z - abs(math.sin(ang)) * r * 0.6, 0.9 + r * (0.5 + 0.35 * math.sin(a * 1.7)), r * 0.13, TEAL)
uball(0, 9.4, 2.0, 1.3, POD2, scale=(1, 1.3, 0.4)); uball(0, 9.1, 2.0, 0.9, TEAL, scale=(1, 1.3, 0.3))
for x in (-13, 13):
    for i in range(7):
        t = i / 6.0
        uball(x + math.sin(t * 1.3) * (1.2 if x < 0 else -1.2), 11, 0.6 + t * 4.0, 0.4 - t * 0.22, POD)
    uball(x + math.sin(1.3) * (1.2 if x < 0 else -1.2), 11, 4.9, 0.32, TEAL)
finish("Station_alien")

# ---------------- fortress: black blockhouse, green lights, two pylons ----------------
ARMOR = mat("FortArmor", (0.13, 0.16, 0.16), 0.4, 0.5); ARMOR2 = mat("FortTrim", (0.24, 0.29, 0.29), 0.4, 0.5); LIME = mat("GlowFort", (0.4, 1.0, 0.25), emit=3.0)
GRATE = mat("FortDeck", (0.3, 0.35, 0.35))
platform(GRATE, ARMOR2)
ubox(-17, 17, 3.5, 3.7, 0.6, 0.64, LIME)
ubox(-9, 9, 9.6, 14.8, 0.6, 5.6, ARMOR); ubox(-9.4, 9.4, 9.2, 15.2, 5.6, 6.0, ARMOR2)
for x in range(-9, 10, 3):
    ubox(x - 0.6, x + 0.6, 9.2, 9.9, 6.0, 6.9, ARMOR2)
for y in (2.0, 4.4):
    ubox(-8.6, 8.6, 9.54, 9.6, y, y + 0.16, LIME)
ubox(-1.6, 1.6, 9.4, 9.6, 0.6, 3.6, ARMOR2); ubox(-1.2, 1.2, 9.36, 9.4, 0.6, 3.2, DARK)
for x in (-12.5, 12.5):
    ubox(x - 0.9, x + 0.9, 10.6, 12.4, 0.6, 1.4, ARMOR2); ubox(x - 0.6, x + 0.6, 10.9, 12.1, 1.4, 8.6, ARMOR)
    ubox(x - 0.62, x + 0.62, 10.86, 10.9, 2.0, 8.0, LIME); ubox(x - 0.9, x + 0.9, 10.6, 12.4, 8.6, 9.0, ARMOR2); uball(x, 11.5, 9.4, 0.4, LIME)
finish("Station_fortress")

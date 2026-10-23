# The alien base's station on its own (the rest are in Tools/make_stations.py, whose helpers this repeats): a dark
# armoured bunker under a slanted roof with green light strips, a landing pad and two antenna masts, a gate frame
# of light over the platform.
#   blender -b --python Tools/make_fortress_station.py
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


# ---------------- fortress: an armoured bunker ----------------
ARMOR = mat("BaseArmor", (0.22, 0.26, 0.3), 0.4, 0.5); ARMOR2 = mat("BaseArmorLight", (0.4, 0.46, 0.5), 0.4, 0.5)
PLATE = mat("BasePlate", (0.3, 0.34, 0.36)); GREEN = mat("GlowBaseGreen", (0.4, 1.0, 0.3), emit=3.0); RED = mat("GlowBaseRed", (1.0, 0.25, 0.2), emit=3.0)
platform(PLATE, ARMOR)
ubox(-17, 17, 3.5, 3.7, 0.6, 0.64, GREEN)                                        # a light strip along the edge
ubox(-9, 9, 10, 15, 0, 5.2, ARMOR)                                               # the bunker
mesh("roof", [(-9.5, -9.6, 5.2), (9.5, -9.6, 5.2), (9.5, -15.4, 6.6), (-9.5, -15.4, 6.6),
              (-9.5, -9.6, 5.6), (9.5, -9.6, 5.6), (9.5, -15.4, 7.0), (-9.5, -15.4, 7.0)],
     [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], ARMOR2)
for x in (-6, -2, 2, 6):                                                          # slit windows lit green
    ubox(x - 1.2, x + 1.2, 9.94, 10.0, 2.6, 3.2, GREEN)
ubox(-1.4, 1.4, 9.9, 10.0, 0.6, 2.4, ARMOR2); ubox(-1.2, 1.2, 9.88, 9.9, 0.7, 2.3, RED)  # blast door
for x in (-9.3, 9.3):                                                             # buttresses
    ubox(x - 0.5, x + 0.5, 9.4, 15.2, 0, 4.2, ARMOR2)
ucyl(13, 12, 0, 0.4, 3.4, ARMOR, sides=8); ucyl(13, 12, 0.4, 0.46, 3.0, GREEN, sides=8)  # landing pad
ubox(12.7, 13.3, 11.7, 12.3, 0.46, 0.5, ARMOR2)
for x in (-14, -11.5):                                                            # antenna masts
    ucyl(x, 13, 0, 9 - (x + 14) * 0.6, 0.18, ARMOR2, sides=6)
    uball(x, 13, 9.1 - (x + 14) * 0.6, 0.3, RED)
for x in (-4.5, 4.5):                                                             # the gate of light over the platform
    ubox(x - 0.3, x + 0.3, 4.0, 4.6, 0.6, 6.0, ARMOR2); ubox(x - 0.12, x + 0.12, 3.95, 4.0, 0.8, 5.8, GREEN)
ubox(-4.8, 4.8, 4.0, 4.6, 6.0, 6.6, ARMOR2); ubox(-4.3, 4.3, 3.95, 4.0, 6.15, 6.45, GREEN)
finish("Station_fortress")

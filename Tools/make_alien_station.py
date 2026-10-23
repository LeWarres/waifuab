# The alien planet's station on its own (the rest are in Tools/make_stations.py, whose helpers this repeats):
# a honeycomb of hexagonal pods behind a hex-tiled platform, a glowing hex arch over it, crystal spires at its ends.
#   blender -b --python Tools/make_alien_station.py
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


# ---------------- alien: a honeycomb of hexagons ----------------
HEXA = mat("HexShell", (0.28, 0.8, 0.72)); HEXDARK = mat("HexDark", (0.12, 0.28, 0.32)); FLOORHEX = mat("HexFloor", (0.36, 0.32, 0.52))
HEXGLOW = mat("GlowHex", (0.6, 1.0, 0.3), emit=3.0); PINKGLOW = mat("GlowPinkHex", (1.0, 0.35, 0.8), emit=3.0)
platform(FLOORHEX, HEXDARK)
for i in range(-8, 9):  # hex tiles set into the platform, every third lit
    ucyl(i * 2.0, 6.3, 0.6, 0.64, 0.72, HEXGLOW if i % 3 == 0 else HEXDARK, sides=6)
for x, z, r, h in ((0, 13, 3.2, 6), (-5.6, 12.4, 2.6, 4.5), (5.6, 12.4, 2.6, 4.5), (-2.8, 16.2, 2.4, 8), (2.8, 16.2, 2.4, 5.5), (-9.6, 13, 2, 3.4), (9.6, 13, 2, 3.4)):
    ucyl(x, z, 0, h, r, HEXA, sides=6)                      # a pod of the honeycomb
    ucyl(x, z, h, h + 0.3, r * 1.06, HEXDARK, sides=6)      # its rim
    ucyl(x, z, h + 0.3, h + 0.55, r * 0.55, HEXGLOW, sides=6)  # and a lit cap
    ubox(x - 0.55, x + 0.55, z - r * 0.87 - 0.06, z - r * 0.87, h * 0.42, h * 0.66, PINKGLOW)  # a window to the track
for x in (-3.3, 3.3):  # the hex arch over the platform's middle
    ucyl(x, 4.2, 0.6, 6.2, 0.36, HEXA, sides=6)
ubox(-3.8, 3.8, 3.9, 4.5, 6.0, 6.6, HEXDARK); ubox(-3.1, 3.1, 3.85, 3.9, 6.12, 6.48, HEXGLOW)
for x in (-14.6, 14.6):  # crystal spires at the ends
    ucyl(x, 12, 0, 7.5, 0.85, HEXGLOW, sides=6, top=0.05)
    ucyl(x + (0.9 if x < 0 else -0.9), 12.7, 0, 4.2, 0.5, PINKGLOW, sides=6, top=0.05)
finish("Station_alien")

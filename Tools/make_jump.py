# Builds the jump: a ravine cutting across the track, a launch ramp before it and a landing ramp after it.
#   blender -b --python Tools/make_jump.py
# Written in the game's own axes (x along the track with the ravine starting at 0, y up, z across) and
# converted on the way in, since the export mirrors x. Materials are placeholders Unity swaps by name.
import os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
VOID = mat("JumpVoid", (0.01, 0.01, 0.02))
ROCK = mat("JumpRock", (0.5, 0.42, 0.3))
STEEL = mat("JumpSteel", (0.5, 0.52, 0.56), 0.35, 0.8)
HAZARD = mat("JumpHazard", (1.0, 0.72, 0.05))
DARK = mat("JumpDark", (0.04, 0.04, 0.05))

LENGTH = 9.0   # of the ravine where the track crosses it
TOP = 0.27     # just over the rails, so the track vanishes under it
RAMP = 1.3     # height of the launch lip
RAIL = 0.24    # top of the rails

def ubox(x0, x1, z0, z1, y0, y1, m, name="box"):
    return box(-x1, -x0, -z1, -z0, y0, y1, m, name)

def uprism(profile, z0, z1, m, name="prism"):  # profile: (x, y) points
    return prism([(-x, y) for x, y in profile], -z1, -z0, m, name)

def slope(x):  # height of the launch ramp's surface at x
    return RAIL + (RAMP - RAIL) * (x + 3.6) / 3.6

# ---- the ravine: widest under the track, narrowing as it runs off to both sides ----
stations = [(-15, 3.4, 5.4), (-10, 2.2, 6.6), (-6, 1.0, 8.2), (-2.4, 0, LENGTH), (2.4, 0, LENGTH), (5.5, 0.7, 8.4), (10, 2.0, 7.0), (15, 3.6, 5.6)]
verts, faces = [], []
for z, x0, x1 in stations:
    verts += [(-x0, -z, TOP), (-x1, -z, TOP)]
for i in range(len(stations) - 1):
    a = i * 2
    faces.append((a, a + 1, a + 3, a + 2))
mesh("ravine", verts, faces, VOID)

# Rocks along both lips, except where the ramps stand.
sizes = [0.55, 0.8, 0.45, 0.7, 0.95, 0.5, 0.65, 0.85, 0.4, 0.75]
count = 0
for i in range(len(stations) - 1):
    (za, a0, a1), (zb, b0, b1) = stations[i], stations[i + 1]
    steps = max(1, int(abs(zb - za) / 1.5))
    for k in range(steps):
        t = (k + 0.5) / steps
        z = za + (zb - za) * t
        if abs(z) < 2.0:
            continue
        for x in (a0 + (b0 - a0) * t, a1 + (b1 - a1) * t):
            r = sizes[count % len(sizes)]
            count += 1
            ball((-x, -z, TOP * 0.5), r, ROCK, "rock", (1.0, 1.25, 0.7))

# ---- launch ramp: a steel wedge with rails on it, yellow sides with black ribs, two marker posts ----
uprism([(-3.6, RAIL), (0, RAMP), (0.35, RAMP), (0.35, 0.0), (-3.6, 0.0)], -1.25, 1.25, STEEL, "ramp")
for z in (-0.8, 0.8):
    uprism([(-3.6, RAIL), (0.35, RAMP + 0.02), (0.35, RAMP + 0.1), (-3.6, RAIL + 0.08)], z - 0.07, z + 0.07, DARK, "ramp_rail")
for side in (-1, 1):
    z0, z1 = sorted((side * 1.25, side * 1.31))
    uprism([(-3.6, 0.0), (-3.6, RAIL), (0, RAMP), (0.35, RAMP), (0.35, 0.0)], z0, z1, HAZARD, "ramp_side")
    z0, z1 = sorted((side * 1.31, side * 1.35))
    for x in (-2.7, -1.8, -0.9, 0.0):
        ubox(x - 0.09, x + 0.09, z0, z1, 0.0, slope(x), DARK, "rib")
    # Marker post at the lip: black and yellow bands, a yellow board on top.
    zp = side * 1.75
    for band in range(5):
        ubox(-0.08, 0.2, zp - 0.14, zp + 0.14, band * 0.55, band * 0.55 + 0.55, HAZARD if band % 2 else DARK, "post")
    ubox(-0.14, 0.26, zp - 0.45, zp + 0.45, 2.75, 3.35, HAZARD, "board")
    ubox(-0.16, 0.28, zp - 0.3, zp + 0.3, 2.95, 3.15, DARK, "board_mark")

# ---- landing ramp on the far lip ----
uprism([(LENGTH - 0.3, 0.0), (LENGTH - 0.3, 1.05), (LENGTH, 1.05), (LENGTH + 3.0, RAIL), (LENGTH + 3.0, 0.0)], -1.25, 1.25, STEEL, "landing")
for side in (-1, 1):
    z0, z1 = sorted((side * 1.25, side * 1.31))
    uprism([(LENGTH - 0.3, 0.0), (LENGTH - 0.3, 1.05), (LENGTH, 1.05), (LENGTH + 3.0, RAIL), (LENGTH + 3.0, 0.0)], z0, z1, HAZARD, "landing_side")

finish("Jump")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "Tools", "jump.blend"))
render([("jump_view", (-9.0, -13.0, 10.0), (-4.0, 0, 0.5))])

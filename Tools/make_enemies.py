# Enemy robots, one small mesh per shape: Enemy_saucer, Enemy_diamond, Enemy_mine, Enemy_drone, Enemy_walker,
# Enemy_dart, Enemy_hive, Enemy_orb.
#   blender -b --python Tools/make_enemies.py
# Each fits a unit cube centred on the origin (the game scales it by the enemy's size) and faces -Y, which the
# export turns into Unity's forward. They are drawn instanced with ONE material in the enemy type's colour, so the
# parts are told apart by vertex colour only: BODY takes the full colour, MID a duller one, DARK nearly black.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
BODY = mat("Body", (1.0, 1.0, 1.0))
MID = mat("Mid", (0.55, 0.6, 0.7))
DARK = mat("Dark", (0.12, 0.12, 0.16))

def spike(center, radius, height, m, sides=6, turn=(0, 0, 0)):  # a cone standing on center, then turned about it
    ob = cyl((center[0], center[1], center[2] + height * 0.5), radius, height, "Z", m, sides=sides, top=0.0)
    bpy.context.scene.cursor.location = center
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    ob.rotation_euler = tuple(math.radians(a) for a in turn)
    return ob

# ---- flying saucer with its robot pilot ----
ball((0, 0, -0.1), 0.5, BODY, scale=(1, 1, 0.26)); ring((0, 0, -0.1), 0.45, 0.05, DARK)
ball((0, 0, 0.0), 0.27, MID, scale=(1, 1, 0.8))
box(-0.11, 0.11, -0.1, 0.1, 0.16, 0.36, BODY); box(-0.09, 0.09, -0.12, -0.1, 0.24, 0.31, DARK)
cyl((0, 0, 0.42), 0.015, 0.12, "Z", DARK, sides=5); ball((0, 0, 0.49), 0.04, BODY)
for a in range(5):
    ball((math.cos(a * 1.257) * 0.3, math.sin(a * 1.257) * 0.3, -0.2), 0.06, DARK)
finish("Enemy_saucer", paint=True)

# ---- diamond: two pyramids base to base, a band round the middle ----
spike((0, 0, 0), 0.36, 0.5, BODY, sides=4); spike((0, 0, 0), 0.36, 0.5, MID, sides=4, turn=(180, 0, 0))
ring((0, 0, 0), 0.4, 0.035, DARK); box(-0.07, 0.07, -0.3, -0.24, -0.05, 0.05, DARK)
finish("Enemy_diamond", paint=True)

# ---- mine: a ball bristling with spikes ----
ball((0, 0, 0), 0.3, MID); ring((0, 0, 0), 0.31, 0.04, DARK)
for turn in ((0, 0, 0), (180, 0, 0), (90, 0, 0), (-90, 0, 0), (0, 90, 0), (0, -90, 0), (55, 0, 45), (55, 0, 135), (55, 0, 225), (55, 0, 315)):
    spike((0, 0, 0), 0.09, 0.5, BODY, sides=5, turn=turn)
finish("Enemy_mine", paint=True)

# ---- drone: a flat body on four rotors ----
box(-0.17, 0.17, -0.26, 0.26, -0.08, 0.08, BODY); box(-0.1, 0.1, -0.28, -0.26, -0.03, 0.04, DARK)
for x in (-1, 1):
    for y in (-1, 1):
        box(min(0, x * 0.36), max(0, x * 0.36), y * 0.3 - 0.03, y * 0.3 + 0.03, -0.02, 0.03, DARK)
        cyl((x * 0.36, y * 0.3, 0.07), 0.15, 0.03, "Z", MID, sides=10); cyl((x * 0.36, y * 0.3, 0.02), 0.03, 0.1, "Z", DARK, sides=5)
ball((0, 0, -0.12), 0.1, MID)
finish("Enemy_drone", paint=True)

# ---- walker: a heavy robot on two legs ----
for x in (-0.22, 0.22):
    box(x - 0.1, x + 0.1, -0.12, 0.12, -0.5, -0.12, DARK); box(x - 0.13, x + 0.13, -0.2, 0.14, -0.5, -0.4, MID)
box(-0.36, 0.36, -0.24, 0.24, -0.12, 0.28, BODY); box(-0.2, 0.2, -0.27, -0.24, -0.02, 0.14, DARK)
box(-0.17, 0.17, -0.2, 0.12, 0.28, 0.48, MID); box(-0.14, 0.14, -0.22, -0.2, 0.35, 0.42, DARK)
for x in (-0.42, 0.42):
    cyl((x, -0.12, 0.15), 0.07, 0.5, "Y", DARK, sides=7); box(x - 0.08, x + 0.08, -0.05, 0.2, 0.06, 0.24, MID)
finish("Enemy_walker", paint=True)

# ---- dart: a wedge that runs nose first ----
mesh("hull", [(0, -0.5, -0.38), (-0.36, 0.42, -0.5), (0.36, 0.42, -0.5), (0, 0.3, 0.02), (0, 0.42, -0.5)],
     [(0, 1, 3), (0, 3, 2), (1, 4, 3), (4, 2, 3), (0, 2, 4, 1)], BODY)
box(-0.14, 0.14, 0.38, 0.5, -0.46, -0.2, DARK)
both(lambda y0, y1: None, 0, 0)
for x in (-0.3, 0.3):
    cyl((x, 0.25, -0.4), 0.1, 0.08, "X", DARK, sides=8)
box(-0.05, 0.05, -0.2, 0.0, -0.22, -0.14, MID)
finish("Enemy_dart", paint=True)

# ---- hive: stacked six-sided rings, where the small ones come from ----
for z, r, h, m in ((-0.36, 0.46, 0.28, BODY), (-0.08, 0.38, 0.26, MID), (0.18, 0.28, 0.24, BODY)):
    cyl((0, 0, z), r, h, "Z", m, sides=6)
    for a in range(6):
        ang = a * math.pi / 3 + math.pi / 6
        ball((math.cos(ang) * r * 0.88, math.sin(ang) * r * 0.88, z), 0.07, DARK)
spike((0, 0, 0.3), 0.2, 0.2, DARK, sides=6)
finish("Enemy_hive", paint=True)

# ---- orb: a core inside two rings ----
ball((0, 0, 0), 0.26, MID); ball((0, -0.22, 0.02), 0.09, DARK)
ring((0, 0, 0), 0.4, 0.035, BODY).rotation_euler = (math.radians(70), 0, 0)
ring((0, 0, 0), 0.46, 0.03, BODY).rotation_euler = (math.radians(70), 0, math.radians(90))
finish("Enemy_orb", paint=True)

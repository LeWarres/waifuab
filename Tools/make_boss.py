# The biome bosses: one big war-rig per biome, sharing one heavy-truck body. The desert one is the
# reference (cargo truck with a rocket rack). Drawn as a normal model (not instanced), so parts keep
# their own materials; TrainSim.Toon() swaps them for the game's toon look.
#   blender -b --python Tools/make_boss.py
# Built nose towards -Y and standing on z=0, like the enemies, so the export turns the nose into Unity's forward.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()

RUST = mat("BossRust", (0.40, 0.19, 0.07), rough=0.85, metal=0.1)
STEEL = mat("BossSteel", (0.5, 0.52, 0.56), rough=0.45, metal=0.7)
DARK = mat("BossDark", (0.10, 0.10, 0.12), rough=0.6, metal=0.5)
GLASS = mat("BossGlass", (0.04, 0.07, 0.11), rough=0.15, metal=0.0)
WAR = mat("BossWarhead", (0.62, 0.10, 0.08), rough=0.4, metal=0.3)
HOT = mat("BossHazard", (0.85, 0.58, 0.10), rough=0.5, metal=0.3)

AXIS = {"X": (0, math.radians(90), 0), "Y": (math.radians(90), 0, 0), "Z": (0, 0, 0)}


def oring(center, radius, thick, axis, m, major=12, minor=5, name="ring"):
    bpy.ops.mesh.primitive_torus_add(major_segments=major, minor_segments=minor, major_radius=radius,
                                     minor_radius=thick, location=center, rotation=AXIS[axis])
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(m)
    parts.append(ob)
    return ob


def spike(x, z, m=STEEL, sides=8):
    # A cone pointing towards -Y (the nose), base sitting just in front of the bumper.
    return cyl((x, -5.05, z), 0.13, 1.0, "Y", m, sides=sides, top=0.0)


# ---- rolling stock: three axles of very big wheels, chained ----
for y in (-3.4, 0.4, 3.6):
    for side in (-1, 1):
        x = side * 1.75
        cyl((x, y, 1.15), 1.15, 0.80, "X", RUST, sides=16, name="tyre")
        cyl((x + side * 0.05, y, 1.15), 0.42, 0.86, "X", STEEL, sides=12, name="hub")
        cyl((x + side * 0.12, y, 1.15), 0.14, 0.92, "X", DARK, sides=8, name="cap")
        oring((x, y, 1.15), 1.22, 0.07, "X", STEEL)  # snow chain round the tyre

# ---- chassis ----
box(-1.15, 1.15, -4.6, 4.9, 1.70, 2.10, STEEL)
box(-1.00, 1.00, -4.2, 4.7, 1.50, 1.72, RUST)

# ---- cargo box, with armour slats and a top deck ----
box(-1.62, 1.62, 0.30, 4.90, 2.10, 4.30, RUST)
both(lambda y0, y1: box(1.62, 1.74, y0, y1, 2.25, 4.15, DARK), 0.10, 4.90)
box(-1.70, 1.70, 0.20, 5.00, 4.30, 4.48, RUST)
for y in (-0.5, 1.0, 2.5, 4.0):
    box(-1.78, 1.78, y - 0.06, y + 0.06, 2.25, 4.20, STEEL)
box(-1.40, 1.40, 5.00, 5.12, 2.10, 4.20, DARK)  # rear door

# ---- cab, hood and engine nose ----
box(-1.55, 1.55, -2.90, 0.30, 2.10, 4.05, RUST)
box(-1.45, 1.45, -2.80, 0.20, 4.05, 4.22, RUST)
box(-1.40, 1.40, -4.65, -2.90, 1.95, 3.40, RUST)
box(-1.30, 1.30, -4.75, -2.90, 3.40, 3.60, RUST)
# windows: dark glass set into a dark frame, standing slightly proud of the cab
box(-1.34, 1.34, -2.98, -2.86, 3.42, 4.02, DARK)
box(-1.26, 1.26, -3.02, -2.88, 3.50, 3.96, GLASS)
both(lambda x0, x1: box(x0, x1, -2.55, -0.10, 3.45, 4.00, DARK), 1.52, 1.62)
both(lambda x0, x1: box(x0, x1, -2.48, -0.18, 3.52, 3.94, GLASS), 1.60, 1.68)
# grille, headlights, bull bar
box(-1.22, 1.22, -4.72, -4.62, 1.95, 3.05, DARK)
for side in (-1, 1):
    cyl((side * 0.9, -4.70, 2.75), 0.22, 0.14, "Y", STEEL, sides=10, name="light")
box(-1.55, 1.55, -5.00, -4.86, 2.55, 2.75, HOT)
box(-1.55, 1.55, -5.00, -4.86, 1.65, 1.85, HOT)
for side in (-1, 1):
    box(side * 0.9 - 0.09, side * 0.9 + 0.09, -5.00, -4.86, 1.85, 2.55, HOT)

# ---- a front of spikes ----
for z in (1.55, 2.35, 3.15):
    for i in range(-3, 4):
        spike(i * 0.42, z)
for i in range(-2, 3):
    spike(i * 0.44, 3.65)

# ---- rocket launcher on the roof of the cargo ----
box(-1.15, 1.15, -1.30, 1.70, 4.48, 4.72, DARK)
box(-1.08, 1.08, -1.22, 1.62, 4.72, 5.05, STEEL)
box(-0.95, 0.95, 1.62, 1.85, 4.40, 5.35, DARK)
for z in (4.78, 5.12):
    for x in (-0.66, 0.0, 0.66):
        cyl((x, 0.20, z), 0.22, 2.4, "Y", STEEL, sides=12, name="tube")
        cyl((x, -1.35, z), 0.19, 0.45, "Y", WAR, sides=12, top=0.02, name="warhead")

# ---- two exhaust stacks ----
for side in (-1, 1):
    cyl((side * 1.45, -2.70, 3.70), 0.17, 2.6, "Z", DARK, sides=10, name="stack")
    cyl((side * 1.45, -2.70, 5.05), 0.22, 0.25, "Z", STEEL, sides=10, name="stacktip")

# ---- chains hanging off the tail ----
for side in (-1, 1):
    for k in range(5):
        z = 2.6 - k * 0.32
        ax = "X" if k % 2 == 0 else "Z"
        oring((side * 0.8, 5.2, z), 0.14, 0.045, ax, STEEL, major=10, minor=4, name="chain")

finish("Boss")  # nose towards -Y: Unity turns that into forward

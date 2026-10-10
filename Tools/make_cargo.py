# Cargo wagons in the armed wagon's style (yellow, white, black trim, hazard stripes) and the loot balloons.
#   Cargo_wood, Cargo_rock, Cargo_arms, Cargo_container   -> Assets/Resources/Models
#   Balloon_0 (wood), Balloon_1 (rock), Balloon_2 (arms)  -> the same folder
#   blender -b --python Tools/make_cargo.py
# Wagons: 5 long (x), 2 wide (y), standing on the rail (z = 0), real colours in their materials.
# Balloons: inside a unit cube centred on the origin, painted with vertex colours and drawn instanced in the
# cargo's colour, like the enemy robots (Tools/make_enemies.py).
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
YELLOW = mat("CargoYellow", (0.95, 0.62, 0.04))
WHITE = mat("CargoWhite", (0.88, 0.88, 0.85))
BLACK = mat("CargoBlack", (0.03, 0.03, 0.035), 0.5, 0.2)
STEEL = mat("CargoSteel", (0.35, 0.37, 0.42), 0.35, 0.8)
LOGS = mat("CargoLog", (0.5, 0.3, 0.15)); LOGEND = mat("CargoLogEnd", (0.85, 0.68, 0.42))
STONE = mat("CargoStone", (0.55, 0.56, 0.6)); STONE2 = mat("CargoStoneDark", (0.4, 0.41, 0.46))
OLIVE = mat("CargoOlive", (0.3, 0.38, 0.2)); OLIVE2 = mat("CargoOliveDark", (0.2, 0.26, 0.14))
BLUE = mat("CargoBlue", (0.12, 0.32, 0.7)); RED = mat("CargoRed", (0.8, 0.12, 0.1))

# The same running gear as the armed wagon, and a deck on it at z = 0.6..0.75.
def flatcar():
    box(-2.5, 2.5, -0.95, 0.95, 0.42, 0.6, BLACK, "chassis")
    for end in (-1, 1):
        box(end * 2.5 - 0.12, end * 2.5 + 0.12, -0.2, 0.2, 0.36, 0.56, STEEL, "coupler")
        for ax in (-0.45, 0.45):
            cyl((end * 1.5 + ax, 0, 0.3), 0.3, 1.84, "Y", STEEL, name="axle")
            cyl((end * 1.5 + ax, 0, 0.3), 0.11, 1.96, "Y", BLACK, sides=8, name="hub")
        both(lambda y0, y1: box(end * 1.5 - 0.85, end * 1.5 + 0.85, y0, y1, 0.24, 0.38, BLACK, "bogie_beam"), 0.92, 0.97)
    box(-2.42, 2.42, -1.0, 1.0, 0.6, 0.75, YELLOW, "deck")
    box(-2.44, 2.44, -1.02, 1.02, 0.6, 0.66, BLACK, "deck_trim")
    for start in (-2.3, 1.45):  # hazard stripes on the deck's edge
        for i in range(3):
            x = start + i * 0.28
            both(lambda y0, y1: prism([(x, 0.66), (x + 0.1, 0.66), (x + 0.2, 0.75), (x + 0.1, 0.75)], y0, y1, BLACK, "hazard"), 1.0, 1.02)

# ---- wood: logs between stakes ----
flatcar()
for x in (-2.0, -0.7, 0.7, 2.0):
    both(lambda y0, y1: box(x - 0.07, x + 0.07, y0, y1, 0.75, 2.0, BLACK, "stake"), 0.9, 1.0)
rows = [(0.99, (-0.66, -0.22, 0.22, 0.66)), (1.37, (-0.44, 0.0, 0.44)), (1.75, (-0.22, 0.22))]
for z, ys in rows:
    for y in ys:
        cyl((0, y, z), 0.22, 4.5, "X", LOGS, sides=8, name="log")
        for end in (-1, 1):
            cyl((end * 2.26, y, z), 0.17, 0.02, "X", LOGEND, sides=8, name="log_end")
for x in (-1.3, 1.3):
    box(x - 0.05, x + 0.05, -0.92, 0.92, 1.96, 2.0, STEEL, "chain")
finish("Cargo_wood")

# ---- rock: an open gondola heaped with stone ----
flatcar()
both(lambda y0, y1: box(-2.4, 2.4, y0, y1, 0.75, 1.5, YELLOW, "wall"), 0.92, 1.0)
for end in (-1, 1):
    x0, x1 = sorted((end * 2.32, end * 2.4))
    box(x0, x1, -1.0, 1.0, 0.75, 1.5, YELLOW, "end_wall")
box(-2.43, 2.43, -1.03, 1.03, 1.45, 1.55, WHITE, "rim"); box(-2.3, 2.3, -0.9, 0.9, 1.44, 1.56, BLACK, "rim_inside")
for x in (-2.38, -1.2, 0.0, 1.2, 2.32):
    both(lambda y0, y1: box(x, x + 0.06, y0, y1, 0.75, 1.45, BLACK, "rib"), 1.0, 1.02)
heap = [(-1.7, -0.3, 0.5), (-1.0, 0.35, 0.6), (-0.2, -0.25, 0.68), (0.6, 0.3, 0.6), (1.4, -0.2, 0.55), (1.9, 0.35, 0.42), (-0.6, 0.1, 0.5), (0.9, -0.4, 0.45)]
for i, (x, y, r) in enumerate(heap):
    ball((x, y, 1.45 + r * 0.25), r, STONE if i % 2 else STONE2, "stone", (1.15, 1, 0.75))
box(-2.3, 2.3, -0.9, 0.9, 1.3, 1.5, STONE2, "stone_bed")
finish("Cargo_rock")

# ---- arms: ammunition crates and two rockets, strapped down ----
flatcar()
for x, y, sx, sy, h in ((-1.6, -0.4, 0.75, 0.5, 0.7), (-1.6, 0.48, 0.75, 0.42, 0.55), (-0.1, 0.0, 0.6, 0.85, 0.8), (1.5, -0.45, 0.8, 0.45, 0.6), (1.5, 0.45, 0.8, 0.45, 0.6)):
    box(x - sx, x + sx, y - sy, y + sy, 0.75, 0.75 + h, OLIVE, "crate")
    box(x - sx - 0.01, x + sx + 0.01, y - sy - 0.01, y + sy + 0.01, 0.75 + h * 0.42, 0.75 + h * 0.58, WHITE, "stencil")
    box(x - sx - 0.02, x + sx + 0.02, y - 0.05, y + 0.05, 0.75, 0.75 + h + 0.02, BLACK, "strap")
    box(x - sx, x + sx, y - sy, y + sy, 0.75 + h, 0.75 + h + 0.03, OLIVE2, "lid")
for y in (-0.22, 0.22):  # rockets on the middle crate
    cyl((-0.1, y, 1.72), 0.14, 0.95, "X", WHITE, sides=8, name="rocket")
    ob = cyl((0.5, y, 1.72), 0.14, 0.3, "X", RED, sides=8, name="rocket_tip", top=0.0)
    ob.rotation_euler = (0, math.radians(-90), 0)
    box(-0.62, -0.5, y - 0.2, y + 0.2, 1.7, 1.74, BLACK, "fin"); box(-0.62, -0.5, y - 0.02, y + 0.02, 1.52, 1.92, BLACK, "fin")
finish("Cargo_arms")

# ---- container: a shipping container on the deck ----
flatcar()
box(-2.3, 2.3, -0.95, 0.95, 0.75, 2.1, BLUE, "container")
box(-2.32, 2.32, -0.97, 0.97, 1.3, 1.6, WHITE, "band")
for i in range(12):
    x = -2.2 + i * 0.4
    both(lambda y0, y1: box(x, x + 0.06, y0, y1, 0.78, 2.07, BLACK, "corrugation"), 0.95, 0.97)
for end in (-1, 1):
    x0, x1 = sorted((end * 2.3, end * 2.33))
    box(x0, x1, -0.85, 0.85, 0.85, 2.0, WHITE, "door"); box(min(x0, x1) - 0.0, max(x0, x1) + 0.01 if end > 0 else max(x0, x1), -0.02, 0.02, 0.85, 2.0, BLACK, "door_seam")
    for y in (-0.5, 0.5):
        box(x0 - (0.02 if end < 0 else 0), x1 + (0.02 if end > 0 else 0), y - 0.03, y + 0.03, 0.9, 1.95, STEEL, "lock_bar")
for x in (-2.3, 2.24):
    both(lambda y0, y1: box(x, x + 0.06, y0, y1, 0.75, 2.12, BLACK, "post"), 0.93, 0.98)
box(-2.32, 2.32, -0.97, 0.97, 2.08, 2.12, BLACK, "top_rail")
finish("Cargo_container")

# ================= loot balloons =================
BODY = mat("Body", (1.0, 1.0, 1.0)); MID = mat("Mid", (0.55, 0.6, 0.7)); DARK = mat("Dark", (0.12, 0.12, 0.16))
LIGHT = mat("Light", (1.0, 0.95, 0.8))

def balloon():
    ball((0, 0, 0.17), 0.33, BODY, "envelope", (1, 1, 1.0))
    ring((0, 0, 0.17), 0.33, 0.025, LIGHT); ring((0, 0, 0.3), 0.27, 0.022, MID); ring((0, 0, 0.04), 0.27, 0.022, MID)
    cyl((0, 0, -0.12), 0.12, 0.1, "Z", MID, sides=8, top=0.2)  # the mouth
    for x in (-0.09, 0.09):
        for y in (-0.09, 0.09):
            cyl((x, y, -0.26), 0.006, 0.22, "Z", DARK, sides=4)
    box(-0.12, 0.12, -0.12, 0.12, -0.5, -0.36, DARK, "basket"); box(-0.13, 0.13, -0.13, 0.13, -0.38, -0.35, LIGHT, "basket_rim")

balloon()  # wood: logs sticking out of the basket
for y in (-0.06, 0.0, 0.06):
    cyl((0, y, -0.33), 0.035, 0.42, "X", LIGHT, sides=6)
finish("Balloon_0", paint=True)

balloon()  # rock: a boulder
ball((0, 0, -0.3), 0.13, MID, "rock", (1.1, 1, 0.8)); ball((0.08, 0.05, -0.26), 0.07, LIGHT)
finish("Balloon_1", paint=True)

balloon()  # arms: a crate with a rocket across it
box(-0.1, 0.1, -0.1, 0.1, -0.36, -0.24, BODY, "crate"); box(-0.11, 0.11, -0.11, 0.11, -0.32, -0.29, LIGHT)
cyl((0, 0, -0.2), 0.03, 0.34, "X", LIGHT, sides=6)
finish("Balloon_2", paint=True)

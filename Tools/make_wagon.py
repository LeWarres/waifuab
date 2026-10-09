# Builds the armed wagon (yellow, white, black trim) and the turret with its gunner, renders previews
# and exports both FBX files for Unity.
#   blender -b --python Tools/make_wagon.py
import bpy, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()

YELLOW = mat("WagonYellow", (0.95, 0.62, 0.04))
WHITE = mat("WagonWhite", (0.88, 0.88, 0.85))
BLACK = mat("WagonBlack", (0.03, 0.03, 0.035), 0.5, 0.2)
STEEL = mat("WagonSteel", (0.35, 0.37, 0.42), 0.35, 0.8)
PANE = mat("WagonGlass", (0.25, 0.55, 0.85), 0.1)

# ---------------- wagon: 5 long (x), 2 wide (y), roof at z = 2.1, origin on the rail ----------------
box(-2.5, 2.5, -0.95, 0.95, 0.42, 0.6, BLACK, "chassis")
for end in (-1, 1):
    box(end * 2.5 - 0.12, end * 2.5 + 0.12, -0.2, 0.2, 0.36, 0.56, STEEL, "coupler")
    for ax in (-0.45, 0.45):
        cyl((end * 1.5 + ax, 0, 0.3), 0.3, 1.84, "Y", STEEL, name="axle")
        cyl((end * 1.5 + ax, 0, 0.3), 0.11, 1.96, "Y", BLACK, sides=8, name="hub")
    both(lambda y0, y1: box(end * 1.5 - 0.85, end * 1.5 + 0.85, y0, y1, 0.24, 0.38, BLACK, "bogie_beam"), 0.92, 0.97)

box(-2.4, 2.4, -1.0, 1.0, 0.6, 1.25, YELLOW, "body_low")
box(-2.42, 2.42, -1.02, 1.02, 1.25, 1.33, BLACK, "belt")
box(-2.4, 2.4, -1.0, 1.0, 1.33, 1.95, WHITE, "body_high")
box(-2.42, 2.42, -1.02, 1.02, 1.89, 1.95, BLACK, "eave")
mesh("roof", [(-2.4, -1.0, 1.95), (2.4, -1.0, 1.95), (2.4, 1.0, 1.95), (-2.4, 1.0, 1.95),
              (-2.3, -0.8, 2.1), (2.3, -0.8, 2.1), (2.3, 0.8, 2.1), (-2.3, 0.8, 2.1)],
     [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (1, 2, 6, 5), (3, 0, 4, 7)], YELLOW)
cyl((0, 0, 2.1), 0.72, 0.03, "Z", BLACK, sides=20, name="turret_pad")
for x in (-1.7, 1.7):
    box(x - 0.3, x + 0.3, -0.45, 0.45, 2.1, 2.16, BLACK, "roof_vent")

def side(y0, y1):
    out = y1 if y1 > 0 else y0          # the face away from the body
    far = out + (0.02 if out > 0 else -0.02)
    for x in (-2.36, -1.3, 1.24, 2.3):  # ribs
        box(x, x + 0.06, y0, y1, 0.6, 1.95, BLACK, "rib")
    # Sliding door in the middle: black frame, yellow leaf, a window and a handle.
    box(-0.78, 0.78, y0, y1, 0.64, 1.86, BLACK, "door_frame")
    box(-0.7, 0.7, min(out, far), max(out, far), 0.72, 1.78, YELLOW, "door")
    box(-0.4, 0.4, min(out, far), max(out, far) + (0.01 if out > 0 else 0), 1.38, 1.68, PANE, "door_window") if out > 0 else \
        box(-0.4, 0.4, far - 0.01, out, 1.38, 1.68, PANE, "door_window")
    box(0.5, 0.58, min(out, far) - (0.02 if out < 0 else 0), max(out, far) + (0.02 if out > 0 else 0), 1.0, 1.25, BLACK, "handle")
    # Windows in the white band.
    for x in (-2.05, 1.5):
        box(x, x + 0.55, y0, y1, 1.45, 1.78, BLACK, "window_frame")
        box(x + 0.05, x + 0.5, min(out, far), max(out, far), 1.5, 1.73, PANE, "window")
    # Hazard stripes low on both ends.
    for start in (-2.25, 1.35):
        for i in range(3):
            x = start + i * 0.28
            prism([(x, 0.62), (x + 0.13, 0.62), (x + 0.4, 1.22), (x + 0.27, 1.22)], y0, y1, BLACK, "hazard")
both(side, 1.0, 1.02)

for end in (-1, 1):  # ends: black frame, white door
    x0, x1 = (2.4, 2.43) if end > 0 else (-2.43, -2.4)
    box(x0, x1, -0.5, 0.5, 0.64, 1.86, BLACK, "end_frame")
    box(x0 + end * 0.02, x1 + end * 0.02, -0.42, 0.42, 0.72, 1.78, WHITE, "end_door")
    both(lambda y0, y1: box(x0, x1 + end * 0.01 if end > 0 else x1, y0, y1, 0.6, 1.95, BLACK, "corner"), 0.9, 1.02)

wagon = finish("Wagon")

# ---------------- turret with its gunner: origin 0.25 above its base, aims along -Y ----------------
SHELL = mat("TurretWhite", (0.88, 0.88, 0.85))
ACCENT = mat("TurretAccent", (0.95, 0.62, 0.04))  # Unity swaps this one for the weapon's colour
DARK = mat("TurretDark", (0.05, 0.05, 0.06), 0.45, 0.4)
SKIN = mat("GirlSkin", (1.0, 0.78, 0.66), 0.7)
HAIR = mat("GirlHair", (1.0, 0.42, 0.62), 0.5)
SHIRT = mat("GirlShirt", (0.95, 0.95, 0.97), 0.7)
NAVY = mat("GirlNavy", (0.08, 0.11, 0.32), 0.7)
RED = mat("GirlRed", (0.85, 0.08, 0.12), 0.6)
EYE = mat("GirlEye", (0.05, 0.12, 0.35), 0.3)
HALO = mat("GirlHalo", (0.2, 0.8, 1.0), 0.3, 0.0, 1.2)

cyl((0, 0, -0.2), 0.62, 0.1, "Z", DARK, sides=18, name="base")
cyl((0, 0, -0.11), 0.54, 0.08, "Z", SHELL, sides=18, name="platform")
box(-0.16, 0.16, 0.05, 0.34, -0.07, 0.05, DARK, "seat")
box(-0.16, 0.16, 0.28, 0.34, 0.05, 0.4, DARK, "seat_back")
# Gun: pedestal, body in the weapon's colour, two barrels.
box(-0.09, 0.09, -0.42, -0.24, -0.07, 0.22, DARK, "pedestal")
box(-0.15, 0.15, -0.64, -0.2, 0.2, 0.4, ACCENT, "gun")
box(-0.11, 0.11, -0.6, -0.26, 0.4, 0.45, DARK, "gun_top")
for x in (-0.075, 0.075):
    cyl((x, -0.98, 0.3), 0.045, 0.7, "Y", DARK, sides=8, name="barrel")
    cyl((x, -1.31, 0.3), 0.065, 0.1, "Y", ACCENT, sides=8, name="muzzle")
# Shields: low in front (the gunner shows over them) and along both sides.
for x0, x1 in ((-0.54, -0.2), (0.2, 0.54)):
    box(x0, x1, -0.5, -0.44, -0.07, 0.4, SHELL, "shield_front")
    box(x0, x1, -0.51, -0.43, 0.4, 0.46, ACCENT, "shield_trim")
    box(x0 + 0.06, x1 - 0.06, -0.52, -0.5, 0.08, 0.16, DARK, "shield_mark")
for x0, x1 in ((-0.56, -0.5), (0.5, 0.56)):
    prism([(x0, -0.07), (x1, -0.07), (x1, 0.4), (x0, 0.4)], -0.5, 0.0, SHELL, "shield_side")
    box(x0 - 0.01, x1 + 0.01, -0.5, 0.0, 0.12, 0.18, DARK, "shield_stripe")

# Gunner: chibi, sitting, hands on the gun.
box(-0.15, 0.15, 0.02, 0.3, 0.05, 0.15, NAVY, "skirt")
for s in (-1, 1):
    a, b = sorted((s * 0.03, s * 0.13))
    box(a, b, -0.12, 0.08, 0.05, 0.13, SKIN, "thigh")
    box(a, b, -0.19, -0.11, 0.02, 0.1, SKIN, "shin")
    box(a, b, -0.19, -0.11, -0.03, 0.03, SHIRT, "sock")
    box(a, b, -0.24, -0.11, -0.07, -0.02, DARK, "shoe")
    a, b = sorted((s * 0.12, s * 0.19))
    box(a, b, -0.16, 0.22, 0.3, 0.38, SHIRT, "arm")
    box(a, b, -0.24, -0.16, 0.3, 0.37, SKIN, "hand")
    a, b = sorted((s * 0.045, s * 0.1))
    box(a, b, -0.075, -0.035, 0.57, 0.65, EYE, "eye")
    cyl((s * 0.27, 0.27, 0.5), 0.085, 0.42, "Z", HAIR, sides=8, name="tail", top=0.015).rotation_euler[1] += s * 0.25 + 3.14159
    ball((s * 0.22, 0.24, 0.72), 0.05, RED, "hair_tie")
box(-0.12, 0.12, 0.1, 0.26, 0.14, 0.42, SHIRT, "torso")
box(-0.135, 0.135, 0.085, 0.275, 0.37, 0.43, NAVY, "collar")
box(-0.035, 0.035, 0.07, 0.1, 0.28, 0.37, RED, "ribbon")
ball((0, 0.14, 0.62), 0.2, SKIN, "head")
ball((0, 0.21, 0.69), 0.225, HAIR, "hair")
box(-0.17, 0.17, -0.07, 0.02, 0.7, 0.8, HAIR, "fringe")
ring((0, 0.17, 1.0), 0.17, 0.018, HALO, "halo")

turret = finish("TurretGirl")

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "Tools", "wagon.blend"))
turret.location = (0, 0, 2.35)
turret.rotation_euler[2] = 0.6
render([("wagon_view", (5.5, -6.0, 5.0), (0, 0, 1.5)), ("turret_view", (1.6, -2.6, 3.9), (0, 0, 2.6))])

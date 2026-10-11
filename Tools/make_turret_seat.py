# The turret with nobody in it (the gunner is a character sat on it by the game): Models/TurretSeat.fbx.
#   blender -b --python Tools/make_turret_seat.py
import bpy, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()

YELLOW = mat("WagonYellow", (0.95, 0.62, 0.04))
WHITE = mat("WagonWhite", (0.88, 0.88, 0.85))
BLACK = mat("WagonBlack", (0.03, 0.03, 0.035), 0.5, 0.2)
STEEL = mat("WagonSteel", (0.35, 0.37, 0.42), 0.35, 0.8)
PANE = mat("WagonGlass", (0.25, 0.55, 0.85), 0.1)

# ---------------- turret, empty: origin 0.25 above its base, aims along -Y ----------------
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

finish("TurretSeat")

# What the hero carries: Models/HeroRifle.fbx and Models/HeroJetpack.fbx.
#   blender -b --python Tools/make_hero_gear.py
# Sized for a hero two units tall. Both point along -Y here, which is forward (+z) in the game.
#   rifle:   its origin is the grip, where the hand goes
#   jetpack: its origin is the middle of the face that lies against the back; the nozzles open at
#            (+-0.2, 0.22, -0.52) here, which Player.cs knows as (+-0.2, -0.52, -0.22)
import bpy, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()

DARK = mat("GearDark", (0.06, 0.06, 0.08), 0.45, 0.4)
STEEL = mat("GearSteel", (0.42, 0.44, 0.5), 0.35, 0.8)
WHITE = mat("GearWhite", (0.9, 0.9, 0.88))
RED = mat("GearRed", (0.85, 0.1, 0.14))
GLOW = mat("GlowGear", (0.3, 0.9, 1.0), emit=1.0)

# ---------------- rifle ----------------
box(-0.05, 0.05, -0.75, 0.2, 0.04, 0.2, WHITE, "body")
box(-0.055, 0.055, -0.6, -0.1, 0.09, 0.15, RED, "stripe")
box(-0.04, 0.04, -0.04, 0.06, -0.16, 0.04, DARK, "grip")
box(-0.035, 0.035, -0.42, -0.28, -0.2, 0.04, DARK, "magazine")
prism([(0.2, 0.04), (0.62, -0.04), (0.62, 0.16), (0.2, 0.2)], -0.04, 0.04, DARK, "stock").rotation_euler[2] = math.radians(90)
cyl((0, -1.1, 0.13), 0.035, 0.7, "Y", DARK, sides=8, name="barrel")
cyl((0, -1.48, 0.13), 0.055, 0.12, "Y", STEEL, sides=8, name="muzzle")
box(-0.045, 0.045, -1.0, -0.75, 0.05, 0.2, DARK, "handguard")
cyl((0, -0.3, 0.27), 0.045, 0.34, "Y", DARK, sides=8, name="scope")
cyl((0, -0.475, 0.27), 0.035, 0.02, "Y", GLOW, sides=8, name="scope_lens")
box(-0.015, 0.015, -0.4, -0.2, 0.2, 0.24, DARK, "scope_mount")
for ob in parts:                                     # drawn big, worn at two thirds
    ob.scale = Vector(ob.scale) * 0.65
    ob.location = Vector(ob.location) * 0.65
finish("HeroRifle")

# ---------------- jetpack ----------------
box(-0.2, 0.2, 0.0, 0.16, -0.3, 0.3, DARK, "frame")
box(-0.12, 0.12, 0.16, 0.24, -0.12, 0.2, RED, "core")
cyl((0, 0.25, 0.04), 0.06, 0.03, "Y", GLOW, sides=10, name="core_light")
for x in (-0.2, 0.2):
    cyl((x, 0.22, 0.02), 0.15, 0.62, "Z", WHITE, sides=12, name="tank")
    ball((x, 0.22, 0.33), 0.15, WHITE, "tank_top", (1, 1, 0.7))
    cyl((x, 0.22, 0.12), 0.155, 0.08, "Z", RED, sides=12, name="tank_band")
    cyl((x, 0.22, -0.38), 0.1, 0.18, "Z", STEEL, sides=10, name="nozzle_neck")
    cyl((x, 0.22, -0.48), 0.09, 0.12, "Z", DARK, sides=10, name="nozzle", top=0.15)
    box(x - 0.04, x + 0.04, -0.02, 0.02, -0.2, 0.34, DARK, "strap")
    prism([(0.3, 0.1), (0.52, -0.05), (0.52, 0.02), (0.3, 0.24)], x - 0.015, x + 0.015, RED, "fin").rotation_euler[2] = math.radians(90)
finish("HeroJetpack")

# ---------------- katana (the Masamune) ----------------
# Same frame as the rifle: origin in the middle of the grip, the blade along -Y.
GOLD = mat("GearGold", (0.95, 0.72, 0.2), 0.35, 0.8)
box(-0.022, 0.022, -0.14, 0.14, -0.025, 0.025, DARK, "handle")
for y in (-0.1, -0.03, 0.04, 0.11):
    box(-0.025, 0.025, y - 0.018, y + 0.018, -0.028, 0.028, WHITE, "wrap")
cyl((0, 0.15, 0), 0.03, 0.03, "Y", GOLD, sides=10, name="pommel")
cyl((0, -0.155, 0), 0.075, 0.025, "Y", GOLD, sides=12, name="guard")
box(-0.012, 0.012, -1.0, -0.17, -0.004, 0.05, STEEL, "blade")
box(-0.006, 0.006, -1.0, -0.17, -0.016, -0.004, GLOW, "edge")
mesh("tip", [(-0.012, -1.0, -0.016), (0.012, -1.0, -0.016), (0.012, -1.0, 0.05), (-0.012, -1.0, 0.05), (0, -1.14, 0.0)],
     [(0, 1, 2, 3), (0, 4, 1), (1, 4, 2), (2, 4, 3), (3, 4, 0)], GLOW)
finish("HeroSword")

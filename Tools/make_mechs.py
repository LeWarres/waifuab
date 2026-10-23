# Every enemy but the gunship (Tools/make_gunship.py) and the brute (Tools/make_brute.py): six mech designs,
# each built in the colours of the enemies that wear it (Combat.Types: model = "Enemy_<name>").
#   blender -b --python Tools/make_mechs.py           -> Assets/Resources/Models/Enemy_<name>.fbx, one per entry in MECHS
#   blender -b --python Tools/make_mechs.py -- shot   -> also a contact sheet of them all, Tools/mechs_preview.png
#   blender -b --python Tools/make_mechs.py -- only invader,skimmer -> just those
# Unit sized like the other enemies, centred on the origin, facing -Y (Unity's forward), +Z up; walkers stand
# with their feet at -0.5. Vertex colours, drawn untinted (EnemyType.model).
#   tripod  a round body on three legs, missile pods on its shoulders, a porthole ("Fully Loaded")
#   crab    a gun pod with a glass dome, gatling barrels and four crab legs
#   alien   a flying mecha-alien head: dome, open face of machinery, fans for ears, a grilled snout
#   dragon  a flying dragon-mech head: long snout, glowing eyes, crystal blades behind
#   gorilla a hunched mech with long arms, a fist and an arm cannon, spikes on its back
#   horned  a tall mech with two great horns, side drums with rods, heavy hands
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from mathutils import Euler

def palette(main, dark, accent, accent2, metal, gun, glow, extra):
    return dict(main=main, dark=dark, accent=accent, accent2=accent2, metal=metal, gun=gun, glow=glow, extra=extra)

STEEL, GUN = (0.55, 0.57, 0.62), (0.13, 0.13, 0.16)
MECHS = [  # name, design, colours
    ("bomb",      "tripod",  palette((0.85, 0.15, 0.12), (0.5, 0.06, 0.06), (1.0, 0.82, 0.15), (0.25, 0.25, 0.3), STEEL, GUN, (1.0, 0.95, 0.4), (1.0, 0.5, 0.1))),
    ("spitter",   "tripod",  palette((0.45, 0.8, 0.2), (0.2, 0.42, 0.1), (0.2, 0.75, 0.75), (0.4, 0.2, 0.55), STEEL, GUN, (0.8, 1.0, 0.3), (1.0, 0.6, 0.2))),
    ("buoy",      "tripod",  palette((0.95, 0.93, 0.88), (0.8, 0.15, 0.15), (0.85, 0.2, 0.2), (0.2, 0.4, 0.7), STEEL, GUN, (1.0, 0.85, 0.3), (0.2, 0.5, 0.8))),
    ("sentinel",  "tripod",  palette((0.35, 0.38, 0.42), (0.18, 0.2, 0.24), (0.3, 1.0, 0.4), (0.15, 0.15, 0.2), STEEL, GUN, (0.4, 1.0, 0.5), (0.3, 1.0, 0.4))),
    ("nest",      "crab",    palette((0.25, 0.55, 0.2), (0.12, 0.3, 0.1), (0.95, 0.55, 0.15), (0.55, 0.25, 0.6), STEEL, GUN, (0.6, 1.0, 0.4), (0.3, 0.85, 0.9))),
    ("hive",      "crab",    palette((0.4, 0.2, 0.6), (0.22, 0.1, 0.36), (1.0, 0.6, 0.12), (0.7, 0.25, 0.55), STEEL, GUN, (0.3, 0.9, 0.95), (0.35, 0.85, 0.95))),
    ("gang",      "crab",    palette((0.45, 0.85, 0.3), (0.2, 0.12, 0.3), (0.65, 0.3, 0.9), (0.25, 0.2, 0.32), STEEL, GUN, (0.6, 1.0, 0.4), (1.0, 0.35, 0.8))),  # the city's invaders
    ("larva",     "crab",    palette((0.7, 0.9, 0.3), (0.4, 0.6, 0.15), (0.9, 0.5, 0.7), (0.4, 0.6, 0.15), STEEL, GUN, (1.0, 1.0, 0.6), (0.5, 0.9, 0.9))),
    ("ghost",     "alien",   palette((0.9, 0.95, 1.0), (0.55, 0.65, 0.8), (0.3, 0.9, 1.0), (0.3, 0.35, 0.5), STEEL, GUN, (0.4, 1.0, 1.0), (0.6, 0.9, 1.0))),
    ("healer",    "alien",   palette((1.0, 0.55, 0.75), (0.75, 0.3, 0.5), (0.95, 0.95, 0.95), (0.35, 0.3, 0.45), STEEL, GUN, (0.5, 1.0, 0.6), (1.0, 1.0, 1.0))),
    ("saboteur",  "alien",   palette((0.1, 0.6, 0.6), (0.05, 0.3, 0.32), (0.95, 0.85, 0.2), (0.2, 0.2, 0.28), STEEL, GUN, (1.0, 0.9, 0.3), (0.95, 0.4, 0.2))),
    ("fast",      "dragon",  palette((0.6, 0.2, 0.55), (0.35, 0.1, 0.35), (0.25, 0.85, 0.7), (0.4, 0.42, 0.5), STEEL, GUN, (0.3, 1.0, 0.8), (0.25, 0.85, 0.7))),
    ("wolf",      "dragon",  palette((0.55, 0.6, 0.68), (0.32, 0.36, 0.42), (0.6, 0.85, 1.0), (0.25, 0.28, 0.35), STEEL, GUN, (0.5, 0.9, 1.0), (0.9, 0.95, 1.0))),
    ("fish",      "dragon",  palette((0.2, 0.75, 0.9), (0.08, 0.4, 0.55), (1.0, 0.55, 0.15), (0.95, 0.95, 0.9), STEEL, GUN, (1.0, 0.95, 0.4), (1.0, 0.55, 0.15))),
    ("imp",       "dragon",  palette((0.85, 0.12, 0.1), (0.25, 0.05, 0.05), (0.15, 0.12, 0.12), (0.4, 0.1, 0.08), STEEL, GUN, (1.0, 0.7, 0.1), (1.0, 0.5, 0.05))),
    ("charger",   "gorilla", palette((1.0, 0.5, 0.12), (0.75, 0.28, 0.08), (0.5, 0.22, 0.65), (0.3, 0.12, 0.42), STEEL, GUN, (1.0, 0.3, 0.6), (0.5, 0.22, 0.65))),
    ("armored",   "gorilla", palette((0.72, 0.75, 0.82), (0.3, 0.2, 0.42), (0.45, 1.0, 0.35), (0.5, 0.3, 0.7), STEEL, GUN, (0.5, 1.0, 0.4), (0.9, 0.4, 1.0))),  # the city's invaders
    ("colossus",  "gorilla", palette((0.18, 0.16, 0.17), (0.08, 0.07, 0.08), (1.0, 0.35, 0.05), (0.5, 0.12, 0.08), STEEL, GUN, (1.0, 0.55, 0.1), (1.0, 0.35, 0.05))),
    ("yeti",      "horned",  palette((0.92, 0.95, 1.0), (0.62, 0.72, 0.85), (0.35, 0.65, 1.0), (0.5, 0.55, 0.7), STEEL, GUN, (0.5, 0.9, 1.0), (0.75, 0.9, 1.0))),
    ("golem",     "horned",  palette((0.3, 0.12, 0.08), (0.16, 0.06, 0.05), (1.0, 0.45, 0.05), (0.2, 0.18, 0.18), STEEL, GUN, (1.0, 0.65, 0.1), (0.95, 0.85, 0.75))),
    # the city's invaders: acid green, violet and chrome (with gang and armored above)
    ("invader",   "alien",   palette((0.45, 0.85, 0.3), (0.25, 0.12, 0.38), (1.0, 0.35, 0.8), (0.3, 0.25, 0.4), STEEL, GUN, (0.7, 1.0, 0.3), (0.9, 0.4, 1.0))),
    ("skimmer",   "dragon",  palette((0.55, 0.25, 0.85), (0.2, 0.1, 0.3), (0.45, 1.0, 0.35), (0.72, 0.75, 0.82), STEEL, GUN, (0.6, 1.0, 0.4), (1.0, 0.4, 0.8))),
    ("crusher",   "horned",  palette((0.7, 0.73, 0.8), (0.28, 0.18, 0.4), (0.45, 1.0, 0.35), (0.55, 0.25, 0.85), STEEL, GUN, (0.5, 1.0, 0.4), (1.0, 0.35, 0.8))),
    # space's own: bright enough to stand out on the black (no dark parts to vanish)
    ("probe",     "tripod",  palette((1.0, 0.55, 0.12), (0.6, 0.6, 0.7), (0.2, 0.95, 1.0), (0.95, 0.95, 1.0), STEEL, (0.45, 0.45, 0.5), (1.0, 1.0, 0.5), (0.2, 0.95, 1.0))),
    ("comet",     "dragon",  palette((0.3, 0.95, 1.0), (0.9, 0.95, 1.0), (1.0, 0.4, 0.85), (0.95, 0.95, 1.0), STEEL, (0.45, 0.45, 0.5), (1.0, 1.0, 0.6), (1.0, 0.4, 0.85))),
    ("titan",     "gorilla", palette((1.0, 0.3, 0.75), (0.65, 0.6, 0.75), (1.0, 0.9, 0.2), (0.95, 0.95, 1.0), STEEL, (0.45, 0.45, 0.5), (0.5, 1.0, 0.4), (1.0, 0.9, 0.2))),
    # the alien base's guards: white armour, lime and red lights, bright on the dark plates
    ("warden",    "alien",   palette((0.92, 0.94, 0.96), (0.3, 0.34, 0.4), (0.5, 1.0, 0.3), (0.3, 0.34, 0.4), STEEL, (0.45, 0.45, 0.5), (1.0, 0.3, 0.25), (0.5, 1.0, 0.3))),
    ("hunter",    "dragon",  palette((0.5, 1.0, 0.3), (0.92, 0.94, 0.96), (1.0, 0.3, 0.25), (0.92, 0.94, 0.96), STEEL, (0.45, 0.45, 0.5), (1.0, 0.9, 0.3), (1.0, 0.3, 0.25))),
    ("guardian",  "horned",  palette((0.92, 0.94, 0.96), (0.5, 0.55, 0.62), (1.0, 0.3, 0.25), (0.5, 1.0, 0.3), STEEL, (0.45, 0.45, 0.5), (0.5, 1.0, 0.3), (1.0, 0.3, 0.25))),
    ("miniboss",  "horned",  palette((1.0, 0.75, 0.15), (0.6, 0.4, 0.05), (0.15, 0.14, 0.16), (0.7, 0.1, 0.1), STEEL, GUN, (1.0, 0.25, 0.2), (0.95, 0.95, 0.95))),
]

_mat = mat
def colours(p):  # vertex colours are taken as they are by the toon shader: baked pre-darkened
    return {k: _mat(k, tuple(c ** 2.2 for c in v)) for k, v in p.items()}

def placed(ob, at, rx=0, ry=0, rz=0):  # built round the origin, turned about it, then carried to at
    e = Euler((math.radians(rx), math.radians(ry), math.radians(rz)))
    offset = Vector(ob.location)  # where it was built (a cylinder's centre): it turns with the rest
    ob.rotation_euler = e
    ob.location = Vector(at) + e.to_matrix() @ offset
    return ob

def ahead(at, rx, ry, rz, length):  # where a part built along +Z from at, so turned, ends
    return Vector(at) + Euler((math.radians(rx), math.radians(ry), math.radians(rz))).to_matrix() @ Vector((0, 0, length))

def horn(c, base, bends, radius, side, colour):  # a curved horn from a socket, bend by bend
    at = Vector(base)
    ring(tuple(at), radius, radius * 0.3, c["dark"])
    for k, (rx, ry, h) in enumerate(bends):
        top = radius * 0.66 if k < len(bends) - 1 else 0.0
        placed(cyl((0, 0, h / 2), radius, h, "Z", colour, sides=8, top=top), tuple(at), rx, ry * side)
        at = ahead(at, rx, ry * side, 0, h * 0.92)
        radius = top

def tripod(c):
    ball((0, 0, 0.05), 0.2, c["main"], scale=(0.95, 0.88, 1.15))                      # body
    ball((0, 0.02, -0.12), 0.14, c["dark"], scale=(1, 1, 0.5))                         # lower hull
    cyl((0, -0.17, 0.1), 0.07, 0.03, "Y", c["glow"], sides=14)                         # porthole
    placed(ring((0, 0, 0), 0.075, 0.014, c["metal"]), (0, -0.18, 0.1), 90)
    box(-0.04, 0.04, -0.19, -0.16, -0.05, -0.02, c["gun"])                             # hatch handle
    for k in range(7):                                                                 # rivets round the body
        a = math.radians(-60 + k * 20)
        cyl((math.sin(a) * 0.19, -math.cos(a) * 0.16, 0.24), 0.012, 0.02, "Y", c["metal"], sides=6)
    cyl((0, 0, 0.29), 0.05, 0.06, "Z", c["dark"], sides=10)                            # top cap
    cyl((0.03, 0.02, 0.36), 0.006, 0.12, "Z", c["gun"], sides=5)                       # antenna
    for side in (-1, 1):
        x = 0.2 * side
        cyl((x, 0, 0.12), 0.06, 0.14, "X", c["dark"], sides=10)                        # shoulder
        box(min(0.24 * side, 0.4 * side), max(0.24 * side, 0.4 * side), -0.08, 0.08, 0.14, 0.46, c["accent2"])  # missile pod
        box(min(0.24 * side, 0.4 * side), max(0.24 * side, 0.4 * side), -0.09, -0.08, 0.42, 0.46, c["metal"])
        for i in range(2):
            for j in range(4):
                xx = (0.28 + i * 0.07) * side
                box(xx - 0.025, xx + 0.025, -0.095, -0.08, 0.17 + j * 0.06, 0.21 + j * 0.06, c["extra"])  # warheads
        cyl((0.27 * side, -0.06, -0.04), 0.06, 0.1, "Y", c["accent"], sides=10)          # gatling pod
        for k in range(4):
            a = k * math.pi / 2 + math.pi / 4
            cyl((0.27 * side + math.cos(a) * 0.03, -0.115, -0.04 + math.sin(a) * 0.03), 0.012, 0.02, "Y", c["gun"], sides=6)
    cyl((0, 0, -0.18), 0.12, 0.05, "Z", c["metal"], sides=12)                          # hip ring
    for k in range(3):                                                                 # three legs
        a = math.radians(90 + k * 120)
        hip = (math.cos(a) * 0.1, math.sin(a) * 0.1, -0.18)
        rz = math.degrees(a) - 90
        placed(cyl((0, 0, -0.15), 0.045, 0.3, "Z", c["main"], sides=10, top=0.035), hip, 35, 0, rz)
        knee = ahead(hip, 35, 0, rz, -0.3)
        ball(tuple(knee), 0.045, c["metal"])
        cyl((knee.x, knee.y, (knee.z - 0.5) / 2), 0.012, knee.z + 0.5, "Z", c["gun"], sides=8, top=0.035)

def crab(c):
    ball((0, 0.03, 0.04), 0.24, c["main"], scale=(1.15, 1, 0.72))                      # hull
    placed(ring((0, 0, 0), 0.27, 0.022, c["accent"]), (0, 0.03, 0.04), 0, 0, 0).scale = (1.12, 1, 0.6)
    ball((0, 0.06, -0.08), 0.18, c["dark"], scale=(1.1, 1, 0.5))                       # belly
    ball((0, -0.1, 0.13), 0.13, c["extra"])                                            # glass dome
    placed(ring((0, 0, 0), 0.13, 0.016, c["accent"]), (0, -0.1, 0.12), 0)
    ball((0, -0.12, 0.13), 0.055, c["accent2"])                                        # the pilot's head
    for side in (-1, 1):                                                               # its horns
        placed(cyl((0, 0, 0.03), 0.016, 0.06, "Z", c["accent"], sides=6, top=0.0), (0.035 * side, -0.13, 0.17), 0, 30 * side)
    for side in (-1, 1):
        for z in (0.0, 0.1):                                                           # gatling barrels
            cyl((0.3 * side, -0.12, z), 0.045, 0.34, "Y", c["accent2"], sides=10)
            cyl((0.3 * side, -0.29, z), 0.05, 0.03, "Y", c["accent"], sides=10)
            cyl((0.3 * side, -0.305, z), 0.03, 0.01, "Y", c["glow"], sides=10)
        cyl((0.25 * side, 0.02, 0.05), 0.07, 0.1, "X", c["dark"], sides=10)            # gun mount
        placed(cyl((0, 0, 0.12), 0.035, 0.24, "Z", c["accent2"], sides=8), (0.12 * side, 0.18, 0.12), -40, 0, 15 * side)  # back tubes
        for y in (-0.1, 0.12):                                                         # crab legs
            root = (0.2 * side, y, -0.05)
            placed(cyl((0, 0, 0.12), 0.042, 0.24, "Z", c["accent2"], sides=8), root, 0, 70 * side)
            knee = ahead(root, 0, 70 * side, 0, 0.24)
            ball(tuple(knee), 0.045, c["accent"])
            placed(cyl((0, 0, -0.27), 0.012, 0.54, "Z", c["main"], sides=8, top=0.055), tuple(knee), 0, -15 * side)

def alien(c):
    ball((0, 0.02, 0.1), 0.3, c["main"], scale=(0.92, 0.85, 1.0))                     # dome
    ball((0, -0.12, 0.12), 0.21, c["gun"], scale=(0.78, 0.55, 0.85))                  # open face
    for side in (-1, 1):                                                               # its machinery
        placed(box(-0.02, 0.02, -0.01, 0.01, -0.13, 0.13, c["metal"]), (0.07 * side, -0.24, 0.13), 0, 15 * side)
        for z in (0.04, 0.12, 0.2):
            cyl((0.07 * side, -0.255, z), 0.014, 0.02, "Y", c["accent2"], sides=6)
    box(-0.03, 0.03, -0.25, -0.23, 0.0, 0.08, c["metal"])
    for k in range(6):                                                                 # rim of holes round the dome
        a = math.radians(-75 + k * 30)
        cyl((math.sin(a) * 0.27, -math.cos(a) * 0.2, 0.25), 0.02, 0.03, "Y", c["dark"], sides=8)
    for side in (-1, 1):                                                               # fans for ears
        x = 0.29 * side
        cyl((x, 0, 0.02), 0.13, 0.07, "X", c["metal"], sides=16)
        cyl((x * 1.04, 0, 0.02), 0.1, 0.07, "X", c["gun"], sides=16)
        for k in range(4):
            placed(box(-0.005, 0.005, -0.09, 0.09, -0.01, 0.01, c["metal"]), (x * 1.13, 0, 0.02), k * 45, 0, 0)
        placed(cyl((0, 0, 0.09), 0.035, 0.18, "Z", c["metal"], sides=6, top=0.0), (0.24 * side, -0.05, -0.18), 0, 125 * side)  # spikes
        placed(ring((0, 0, 0), 0.06, 0.016, c["metal"]), (0.2 * side, -0.05, -0.3), 0, 90)  # hose loops
    ball((0, -0.2, -0.13), 0.11, c["metal"], scale=(0.85, 0.8, 1.15))                  # snout
    for k in range(5):
        z = -0.21 + k * 0.035
        box(-0.07, 0.07, -0.3, -0.28, z, z + 0.015, c["gun"])                          # its grille
    placed(cyl((0, 0, 0), 0.04, 0.02, "Y", c["glow"], sides=3), (0, -0.27, 0.0), 0, 0, 0)   # the eye
    box(-0.15, -0.07, -0.18, -0.06, -0.3, -0.18, c["dark"])                           # jaw armour
    box(0.07, 0.15, -0.18, -0.06, -0.3, -0.18, c["dark"])

def dragon(c):
    ball((0, -0.04, 0.05), 0.16, c["main"], scale=(0.85, 1.55, 0.75))                  # head
    ball((0, -0.34, -0.01), 0.08, c["dark"], scale=(0.95, 1.1, 0.75))                  # snout
    for side in (-1, 1):
        cyl((0.03 * side, -0.42, 0.01), 0.012, 0.02, "Y", c["glow"], sides=6)          # nostrils
        placed(box(-0.04, 0.04, -0.01, 0.01, -0.015, 0.015, c["glow"]), (0.075 * side, -0.25, 0.08), 0, 25 * side, -25 * side)  # eyes
        for k, (x, rz, colour) in enumerate(((0.17, 20, "dark"), (0.25, 32, "accent"))):   # crystal blades behind
            placed(box(-0.035, 0.035, -0.035, 0.035, 0.0, 0.42 - k * 0.06, c[colour]), (x * side, 0.1, -0.12), 15, 28 * side, rz * side)
        placed(box(-0.01, 0.01, -0.12, 0.12, -0.05, 0.05, c["accent2"]), (0.13 * side, 0.05, 0.02), 0, 0, 0)  # cheek plates
    ball((0, 0.06, 0.14), 0.14, c["accent"], scale=(1, 1.25, 0.6))                     # helmet cap
    box(-0.05, 0.05, -0.04, 0.06, 0.205, 0.23, c["glow"])                              # its glowing chip
    cyl((0, 0.26, 0.0), 0.08, 0.16, "Y", c["metal"], sides=12)                         # neck
    placed(ring((0, 0, 0), 0.08, 0.016, c["glow"]), (0, 0.3, 0.0), 90)
    cyl((0, 0.36, 0.0), 0.05, 0.06, "Y", c["gun"], sides=10, top=0.02)                 # thruster
    box(-0.012, 0.012, -0.32, 0.1, 0.0, 0.07, c["dark"])                               # crest

def gorilla(c):
    ball((0, 0.03, 0.1), 0.24, c["main"], scale=(1.1, 0.9, 0.85))                      # hunched torso
    ball((0, 0.12, 0.2), 0.17, c["dark"], scale=(1, 0.9, 0.7))                         # back hump
    for k in range(3):                                                                 # spikes on the back
        placed(cyl((0, 0, 0.06), 0.035, 0.12, "Z", c["metal"], sides=6, top=0.0), (0, 0.06 + k * 0.08, 0.32 - k * 0.04), -40)
    box(-0.13, 0.13, -0.22, -0.18, -0.02, 0.14, c["accent"])                           # chest plate
    for x in (-0.07, 0.0, 0.07):
        box(x - 0.02, x + 0.02, -0.235, -0.22, 0.02, 0.1, c["accent2"])
    ball((0, -0.2, 0.17), 0.085, c["dark"])                                             # small head, forward
    cyl((0, -0.27, 0.18), 0.035, 0.03, "Y", c["glow"], sides=12)                       # its eye
    placed(ring((0, 0, 0), 0.04, 0.009, c["metal"]), (0, -0.285, 0.18), 90)
    for side in (-1, 1):
        x = 0.28 * side
        ball((x, -0.02, 0.18), 0.1, c["accent"])                                       # shoulder pad
        cyl((x, -0.04, 0.03), 0.04, 0.2, "Z", c["metal"], sides=8)                     # upper arm
        ball((x, -0.06, -0.08), 0.04, c["gun"])                                        # elbow
        ball((x, -0.08, -0.25), 0.095, c["main"], scale=(1, 1, 1.5))                   # long forearm
        box(x - 0.1 * side, x - 0.07 * side, -0.14, -0.02, -0.34, -0.16, c["accent"])  # forearm plate
        if side < 0:
            ball((x, -0.1, -0.42), 0.07, c["gun"])                                     # fist
            for k in range(3):
                box(x - 0.05 + k * 0.035, x - 0.025 + k * 0.035, -0.17, -0.13, -0.46, -0.4, c["metal"])
        else:
            cyl((x, -0.17, -0.38), 0.04, 0.16, "Y", c["gun"], sides=10)                # arm cannon
            cyl((x, -0.26, -0.38), 0.03, 0.01, "Y", c["glow"], sides=10)
    box(-0.12, 0.12, -0.05, 0.1, -0.2, -0.1, c["gun"])                                 # hips
    for side in (-1, 1):
        x = 0.1 * side
        cyl((x, 0.02, -0.25), 0.045, 0.12, "Z", c["metal"], sides=8)
        box(x - 0.055, x + 0.055, -0.04, 0.08, -0.43, -0.3, c["main"])                 # shins
        box(x - 0.065, x + 0.065, -0.1, 0.09, -0.5, -0.43, c["gun"])                   # feet

def horned(c):
    ball((0, 0, 0.1), 0.23, c["main"], scale=(0.95, 0.8, 1.15))                        # tall torso
    ball((0, -0.06, -0.12), 0.16, c["dark"], scale=(1, 0.8, 0.7))                      # lower plate
    for x in (-0.09, -0.03, 0.03, 0.09):                                               # face slots
        box(x - 0.018, x + 0.018, -0.19, -0.17, 0.18, 0.26, c["glow"])
    box(-0.012, 0.012, -0.2, 0.2, 0.33, 0.37, c["dark"])                               # seam over the top
    for x, z in ((-0.14, 0.05), (0.14, 0.05), (-0.1, -0.04), (0.1, -0.04)):
        cyl((x, -0.18 + abs(x) * 0.3, z), 0.022, 0.03, "Y", c["accent"], sides=8)     # bolts
    for side in (-1, 1):
        horn(c, (0.08 * side, -0.02, 0.33), ((-5, 15, 0.09), (-12, 8, 0.09), (-25, -5, 0.08)), 0.045, side, c["extra"])  # great horns
        x = 0.25 * side
        cyl((x, 0.06, 0.25), 0.1, 0.12, "X", c["accent"], sides=14)                    # side drum
        cyl((x * 1.2, 0.06, 0.25), 0.065, 0.01, "X", c["glow"], sides=14)
        cyl((0.38 * side, 0.06, 0.25), 0.015, 0.14, "X", c["metal"], sides=6)          # its rod
        ball((0.46 * side, 0.06, 0.25), 0.03, c["glow"])
        ball((0.23 * side, -0.02, 0.08), 0.07, c["accent2"])                          # shoulder joint
        box(min(0.2 * side, 0.32 * side), max(0.2 * side, 0.32 * side), -0.08, 0.06, -0.2, 0.06, c["main"])  # arm
        cyl((0.27 * side, -0.07, -0.05), 0.04, 0.02, "Y", c["accent"], sides=10)      # elbow disc
        ball((0.27 * side, -0.03, -0.27), 0.07, c["gun"], scale=(1, 1.1, 1))           # heavy hand
        for k in range(3):
            box(0.27 * side - 0.05 + k * 0.035, 0.27 * side - 0.025 + k * 0.035, -0.11, -0.07, -0.34, -0.27, c["gun"])
        x = 0.1 * side                                                                 # legs
        box(x - 0.06, x + 0.06, -0.06, 0.06, -0.42, -0.2, c["main"])
        cyl((x, -0.07, -0.28), 0.035, 0.02, "Y", c["accent"], sides=10)               # knee disc
        box(x - 0.075, x + 0.075, -0.11, 0.08, -0.5, -0.42, c["gun"])                  # feet

DESIGNS = dict(tripod=tripod, crab=crab, alien=alien, dragon=dragon, gorilla=gorilla, horned=horned)
only = sys.argv[sys.argv.index("only") + 1].split(",") if "only" in sys.argv else None
for name, design, p in MECHS:
    if only and name not in only: continue
    reset()
    DESIGNS[design](colours(p))
    finish("Enemy_" + name, paint=True)

if "shot" in sys.argv:
    # Every one side by side, seen as the game will (undoing the pre-darkening).
    reset()
    vc = bpy.data.materials.new("VC")
    vc.use_nodes = True
    nodes = vc.node_tree.nodes
    attr, gamma = nodes.new("ShaderNodeVertexColor"), nodes.new("ShaderNodeGamma")
    gamma.inputs["Gamma"].default_value = 1 / 2.2
    vc.node_tree.links.new(attr.outputs["Color"], gamma.inputs["Color"])
    vc.node_tree.links.new(gamma.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    for i, (name, _, _) in enumerate(MECHS):
        bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Enemy_" + name + ".fbx"))
        ob = bpy.context.selected_objects[0]
        ob.data.materials.clear()
        ob.data.materials.append(vc)
        ob.location = ((i % 7) * 1.4 - 4.2, (i // 7) * 2.2, 0.55)
    render([("mechs_preview", (0.0, -8.5, 5.5), (0.0, 2.2, 0.3))], lens=30)

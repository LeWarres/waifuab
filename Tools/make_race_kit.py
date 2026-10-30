# The city race's roadside kit (RaceEvent): a concrete barrier, a truss gantry over the road and a street lamp.
# One model each into Assets/Resources/Models/Race_<name>.fbx, with its materials kept apart: RaceEvent turns them
# into toon ones (Glow... bright enough to bloom) and gives GlowAccent the side's neon, cyan or pink.
#   barrier: one 20 m stretch (RaceEvent.Step) along +y, its road face at x = 0, running out to x = 0.8; RaceEvent
#            bends it along the course, one per stretch, so it must tile end to end.
#   gantry:  centred on the road, x across it (52 m between the towers' outsides), y along it.
#   lamp:    its pole at the origin, its arm reaching out over the road towards -x.
#   blender -b --python Tools/make_race_kit.py           (-- shot: also Tools/race_kit_preview.png)
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from lowpoly import _added

M = dict(
    concrete=("RaceConcrete", (0.42, 0.44, 0.5)), joint=("RaceJoint", (0.14, 0.15, 0.18)), kerb=("RaceKerb", (0.13, 0.13, 0.15)),
    steel=("RaceSteel", (0.55, 0.58, 0.66)), dark=("RaceDark", (0.1, 0.11, 0.14)), stripe=("RaceStripe", (1.0, 0.75, 0.1)),
    accent=("GlowAccent", (1.0, 1.0, 1.0)), amber=("GlowAmber", (1.0, 0.55, 0.12)), light=("GlowLamp", (1.0, 0.95, 0.85)),
)
def m(key):
    name, rgb = M[key]
    return bpy.data.materials.get(name) or mat(name, rgb, emit=3.0 if name.startswith("Glow") else 0.0)

# A round bar from p to q.
def strut(p, q, r, mk, sides=6):
    p, q = Vector(p), Vector(q)
    d = q - p
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=r, depth=d.length, location=(p + q) / 2)
    bpy.context.object.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    return _added(m(mk), "strut", False)

L = 20.0  # one stretch of the course
JERSEY = [(0, 0), (0.8, 0), (0.8, 0.1), (0.62, 0.34), (0.52, 1.02), (0.28, 1.02), (0.18, 0.34), (0, 0.1)]

def barrier():
    # Jersey profile, road face at x = 0: a kerb, the sloped lower face, the steep upper one, the top, and the back.
    for k in range(5):  # cast in 4 m sections, a dark joint between them
        y0, y1 = k * 4.0, (k + 1) * 4.0 - 0.06
        prism(JERSEY, y0, y1, m("concrete"))
        prism([(x + (0.03 if x < 0.4 else -0.03), z) for x, z in JERSEY], y1, y1 + 0.06, m("joint"))  # set back from the faces
        box(-0.01, 0.02, y0, y1, 0.0, 0.1, m("kerb"))                          # the scuffed kerb along its foot
        for r in (0.9, 2.9):                                                   # two reflectors on each section
            box(0.12, 0.21, y0 + r - 0.3, y0 + r + 0.3, 0.4, 0.62, m("amber"))  # standing proud of the slope
    # Steel cap with the neon running along its road side, and a hazard stripe under it.
    box(0.22, 0.58, 0, L, 1.02, 1.1, m("steel"))
    box(0.18, 0.24, 0, L, 1.04, 1.2, m("accent"))
    prism([(0.17, 0.74), (0.21, 0.74), (0.25, 0.9), (0.21, 0.9)], 0, L, m("stripe"))
    return finish("Race_barrier")

def tower(x):
    # A square lattice tower on a concrete foot, 1.6 m across, up to the beam.
    box(x - 1.3, x + 1.3, -1.3, 1.3, 0, 0.6, m("concrete"))
    c = [(x - 0.7, -0.7), (x + 0.7, -0.7), (x + 0.7, 0.7), (x - 0.7, 0.7)]
    for cx, cy in c:
        box(cx - 0.12, cx + 0.12, cy - 0.12, cy + 0.12, 0.6, 17.6, m("steel"))
    for z in range(1, 9):
        z0, z1 = 0.6 + (z - 1) * 2.0, 0.6 + z * 2.0
        for i in range(4):
            (ax, ay), (bx, by) = c[i], c[(i + 1) % 4]
            strut((ax, ay, z1), (bx, by, z1), 0.05, "steel")
            strut((ax, ay, z0) if z % 2 else (bx, by, z0), (bx, by, z1) if z % 2 else (ax, ay, z1), 0.04, "steel")
    box(x - 0.9, x + 0.9, -0.9, 0.9, 17.6, 17.8, m("dark"))

def gantry():
    span = 25.0  # the towers' centres, either side of the road (RaceEvent: roadWidth / 2 + 3)
    tower(-span)
    tower(span)
    # The beam: a box truss 1.8 high, 1.4 deep, from tower to tower.
    zb, zt, d = 15.8, 17.6, 0.7
    for y in (-d, d):
        for z in (zb, zt):
            box(-span - 0.8, span + 0.8, y - 0.1, y + 0.1, z - 0.1, z + 0.1, m("steel"))
    n = 26
    for i in range(n + 1):
        x = -span + i * (2 * span / n)
        for y in (-d, d):
            strut((x, y, zb), (x, y, zt), 0.05, "steel")
            if i < n:
                x1 = x + 2 * span / n
                strut((x, y, zb) if i % 2 else (x, y, zt), (x1, y, zt) if i % 2 else (x1, y, zb), 0.04, "steel")
        strut((x, -d, zt), (x, d, zt), 0.04, "steel")
    # Under it, a light bar facing both ways, and a hazard band along the beam's faces.
    box(-span + 2, span - 2, -0.25, 0.25, 15.25, 15.6, m("dark"))
    box(-span + 2.2, span - 2.2, -0.3, 0.3, 15.12, 15.28, m("accent"))
    for y in (-d - 0.12, d + 0.1):
        box(-span + 1.2, span - 1.2, y, y + 0.02, zb + 0.15, zb + 0.45, m("stripe"))
    return finish("Race_gantry")

def lamp():
    box(-0.45, 0.45, -0.45, 0.45, 0, 0.5, m("concrete"))
    cyl((0, 0, 4.75), 0.2, 8.5, "Z", m("steel"), sides=10, top=0.12)
    box(-0.18, 0.18, -0.18, 0.18, 1.2, 1.5, m("dark"))                        # its service hatch
    # The arm out over the road, its head lit underneath, and the side's neon along it.
    strut((0, 0, 8.8), (-2.6, 0, 9.3), 0.09, "steel", sides=8)
    strut((0, 0, 7.6), (-1.2, 0, 9.0), 0.06, "steel", sides=8)
    box(-3.9, -2.3, -0.32, 0.32, 9.1, 9.42, m("dark"))
    box(-3.8, -2.4, -0.26, 0.26, 9.04, 9.1, m("light"))
    box(-2.6, 0.0, -0.05, 0.05, 9.42, 9.5, m("accent"))
    return finish("Race_lamp")

reset()
made = [barrier(), gantry(), lamp()]  # finish() exports each one on its own

if "--" in sys.argv and "shot" in sys.argv[sys.argv.index("--") + 1:]:
    b, g, l = made
    for side, x in ((1, 22.0), (-1, -22.0)):  # both sides of a 44 m road, three stretches each
        for s in range(3):
            c = b if (side, s) == (1, 0) else b.copy()
            if c is not b:
                bpy.context.scene.collection.objects.link(c)
            c.location = (x, s * L - 10, 0)
            c.scale = (side, 1, 1)
    g.location = (0, 22, 0)
    l.location = (24.2, 4, 0)
    render([("race_kit_preview", (30, -34, 12), (0, 14, 5)), ("race_kit_close", (14, -8, 2.2), (23, 8, 0.8))], lens=30)

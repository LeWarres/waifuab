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
    paint=("CarPaint", (0.8, 0.8, 0.82)), glass=("CarGlass", (0.08, 0.12, 0.2)), tail=("GlowTail", (1.0, 0.1, 0.08)),
    cone=("RaceCone", (1.0, 0.38, 0.05)), facade=("Facade", (1.0, 1.0, 1.0)), podium=("RacePodium", (0.2, 0.22, 0.3)),
    plate=("TrenchPlate", (0.2, 0.23, 0.3)), plateDark=("TrenchDark", (0.08, 0.09, 0.13)), pipe=("TrenchPipe", (0.36, 0.4, 0.48)),
    accent=("GlowAccent", (1.0, 1.0, 1.0)), white=("RaceWhite", (0.92, 0.93, 0.96)), seat=("RaceSeat", (0.1, 0.45, 0.95)),
    shirt=("Shirt", (0.9, 0.9, 0.9)), skin=("Skin", (0.95, 0.75, 0.6)), hair=("Hair", (0.12, 0.08, 0.06)), pants=("Pants", (0.16, 0.18, 0.28)), amber=("GlowAmber", (1.0, 0.55, 0.12)), light=("GlowLamp", (1.0, 0.95, 0.85)),
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

# A low ball for the crowd (an icosphere: 20 faces at level 1, 80 at 2).
def nub(center, radius, mk, level=1, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=level, radius=radius, location=center)
    ob = _added(m(mk), "nub", False)
    ob.scale = scale
    return ob

# A fan in the stands, facing -x (the road), feet at the origin: arms both up, or one waving (wave=True).
def fan(name, wave=False):
    box(-0.16, 0.16, -0.26, -0.03, 0, 0.8, m("pants"))
    box(-0.16, 0.16, 0.03, 0.26, 0, 0.8, m("pants"))
    prism([(-0.17, 0.8), (0.17, 0.8), (0.15, 1.42), (-0.15, 1.42)], -0.3, 0.3, m("shirt"))   # torso, a little narrower up top
    nub((0, 0, 1.66), 0.2, "skin", 2, scale=(1, 0.95, 1.1))
    nub((0.03, 0, 1.73), 0.2, "hair", 1, scale=(1.02, 1.02, 0.75))
    for y, up in ((-0.36, True), (0.36, not wave)):
        hand = (-0.12, y * 1.25, 2.15) if up else (0, y * 1.05, 0.75)
        strut((0, y * 0.9, 1.36), hand, 0.075, "shirt", sides=5)
        nub(hand, 0.09, "skin")
    return finish(name)

# A row of stadium seats, 10 m along y, its front edge at x = 0 and the stand behind it towards +x.
def seats():
    box(0, 2.4, 0, 10, -0.12, 0, m("concrete"))
    for k in range(16):
        y = 0.32 + k * 0.62
        box(0.9, 1.35, y - 0.24, y + 0.24, 0, 0.42, m("seat"))                  # the pan
        box(1.3, 1.42, y - 0.24, y + 0.24, 0.42, 0.95, m("seat"))               # its back
    box(0.02, 0.08, 0, 10, 0.0, 1.0, m("steel"))                                 # the rail along the front
    for k in range(6):
        box(0.0, 0.1, k * 2.0 - 0.04 + (0.04 if k == 0 else 0), k * 2.0 + 0.04 + (0.04 if k == 0 else 0), 0, 1.0, m("steel"))
    return finish("Race_seats")

# The start arch over the grid: two octagonal pylons in white with light bands and a dark beam housing the start
# lights (RaceCity puts them in, at 19 m, on its front). Centred on the road, x across, y along (front: -y).
def arch():
    span = 24.5
    octa = lambda r: [(r * math.cos(math.radians(22.5 + 45 * k)), r * math.sin(math.radians(22.5 + 45 * k))) for k in range(8)]
    for s in (-1, 1):
        cx = s * span
        box(cx - 1.8, cx + 1.8, -1.8, 1.8, 0, 0.8, m("concrete"))
        for k in range(6):  # sections, each a little narrower, a band of light between them
            z0, z1, r = 0.8 + k * 3.0, 0.8 + (k + 1) * 3.0 - 0.25, 1.45 - k * 0.06
            ring_ = octa(r)
            verts = [(cx + x, y, z0) for x, y in ring_] + [(cx + x, y, z1) for x, y in ring_]
            faces = [tuple(range(8))[::-1], tuple(range(8, 16))] + [(i, (i + 1) % 8, 8 + (i + 1) % 8, 8 + i) for i in range(8)]
            mesh("pylon", verts, faces, m("white"))
            lit = octa(r - 0.05)
            verts = [(cx + x, y, z1) for x, y in lit] + [(cx + x, y, z1 + 0.25) for x, y in lit]
            mesh("band", verts, faces, m("accent"))
    box(-span - 1.6, span + 1.6, -1.2, 1.2, 17.8, 20.2, m("dark"))
    box(-span - 1.7, span + 1.7, -1.3, 1.3, 20.2, 20.5, m("steel"))
    box(-span + 1.6, span - 1.6, -1.3, -1.2, 17.6, 17.85, m("accent"))      # a line of light under its front
    box(-12, 12, -1.32, -1.2, 18.0, 20.0, m("kerb"))                          # the lights' black panel
    return finish("Race_arch")

# A car smashed on the road, along +x (its nose), wheels on the ground. CarPaint takes a random colour in the game.
def car():
    body = [(-2.25, 0.32), (2.2, 0.32), (2.3, 0.62), (2.05, 0.9), (0.95, 1.0), (0.38, 1.46), (-1.05, 1.5), (-1.7, 1.06), (-2.25, 0.98), (-2.35, 0.62)]
    prism(body, -0.92, 0.92, m("paint"))
    prism([(0.86, 1.03), (0.42, 1.41), (-1.0, 1.44), (-1.56, 1.08)], -0.94, 0.94, m("glass"))      # the windows all round
    box(-0.3, -0.22, -0.95, 0.95, 1.03, 1.44, m("paint"))                                          # the pillar between them
    for x in (-1.45, 1.45):
        for y in (-0.82, 0.82):
            cyl((x, y, 0.36), 0.36, 0.28, "Y", m("kerb"), sides=10)
    box(2.18, 2.34, -0.98, 0.98, 0.3, 0.55, m("kerb"))                                            # bumpers
    box(-2.4, -2.24, -0.98, 0.98, 0.3, 0.55, m("kerb"))
    for y in (-0.62, 0.62):
        box(2.22, 2.3, y - 0.2, y + 0.2, 0.66, 0.8, m("light"))
        box(-2.38, -2.3, y - 0.24, y + 0.24, 0.72, 0.86, m("tail"))
    # The crash: a crumpled bonnet and a black scorch along its flank.
    prism([(1.2, 0.95), (2.0, 0.92), (1.7, 1.18)], -0.6, 0.5, m("paint"))
    box(-1.6, 0.4, 0.925, 0.94, 0.45, 0.9, m("kerb"))
    return finish("Race_car")

# Round a wreck: a ring of cones and, on the side the racers come from (-y), a warning sign on a stand.
def hazard():
    for k in range(10):
        a = math.radians(k * 36 + 18)
        x, y = 4.4 * math.cos(a), 4.4 * math.sin(a)
        box(x - 0.32, x + 0.32, y - 0.32, y + 0.32, 0, 0.08, m("kerb"))
        cyl((x, y, 0.5), 0.26, 0.85, "Z", m("cone"), sides=8, top=0.05)
        cyl((x, y, 0.55), 0.19, 0.16, "Z", m("white"), sides=8, top=0.15)
    # The sign on one pole just in front of the car, inside its crash radius (RaceEvent: 3.4 m), high over it.
    sy, sz = -2.8, 5.6
    cyl((0, sy, 3.0), 0.09, 6.0, "Z", m("steel"), sides=8)
    d = [(0, sz - 1.5), (1.5, sz), (0, sz + 1.5), (-1.5, sz)]                     # a diamond, yellow edged in black
    prism([(x, z) for x, z in d], sy - 0.08, sy - 0.02, m("kerb"))
    prism([(x * 0.86, sz + (z - sz) * 0.86) for x, z in d], sy - 0.12, sy - 0.08, m("stripe"))
    box(-0.15, 0.15, sy - 0.14, sy - 0.12, sz + 0.1, sz + 0.85, m("kerb"))       # !
    box(-0.15, 0.15, sy - 0.14, sy - 0.12, sz - 0.6, sz - 0.3, m("kerb"))
    box(-0.3, 0.3, sy - 0.3, sy + 0.3, sz + 1.6, sz + 1.75, m("dark"))             # the beacon's cap (RaceEvent puts the light on it)
    return finish("Race_hazard")

# Windows for the towers: every face in Facade gets UVs from its place in the world, one tile of the game's window
# texture (eight windows a side) every 24 m, so all towers share one material and their windows match in size.
def facade_uvs():
    for ob in parts:
        if not ob.data.materials or ob.data.materials[0].name != "Facade":
            continue
        layer = ob.data.uv_layers.new(name="UV")
        mw = ob.matrix_world
        for poly in ob.data.polygons:
            n = (mw.to_3x3() @ poly.normal).normalized()
            for li in poly.loop_indices:
                co = mw @ ob.data.vertices[ob.data.loops[li].vertex_index].co
                if abs(n.z) > 0.7:
                    u, v = co.x, co.y
                elif abs(n.x) > abs(n.y):
                    u, v = co.y * (1 if n.x > 0 else -1), co.z
                else:
                    u, v = co.x * (-1 if n.y > 0 else 1), co.z
                layer.data[li].uv = (u / 24.0, v / 24.0)

def octo(r, z0, z1, mk, chamfer=0.22, sx=1.0, sy=1.0, name="octo"):
    c = r * chamfer
    pts = [(r - c, -r), (r, -r + c), (r, r - c), (r - c, r), (-r + c, r), (-r, r - c), (-r, -r + c), (-r + c, -r)]
    pts = [(x * sx, y * sy) for x, y in pts]
    verts = [(x, y, z0) for x, y in pts] + [(x, y, z1) for x, y in pts]
    faces = [tuple(range(8))[::-1], tuple(range(8, 16))] + [(i, (i + 1) % 8, 8 + (i + 1) % 8, 8 + i) for i in range(8)]
    return mesh(name, verts, faces, m(mk))

# Four towers for the skyline, about 100 m, on a dark podium: setbacks and fins, bands of light, a sky bridge.
def towers():
    made = []
    # 1: chamfered square, two setbacks, a crown of fins and a mast.
    octo(14, 0, 8, "podium")
    octo(11, 8, 62, "facade")
    octo(8.5, 62, 86, "facade")
    octo(6, 86, 100, "facade")
    for z in (62, 86, 100):
        octo({62: 11.4, 86: 8.9, 100: 6.4}[z], z - 0.6, z + 0.6, "accent")
    for k in range(8):
        a = math.radians(k * 45)
        box(math.cos(a) * 6 - 0.3, math.cos(a) * 6 + 0.3, math.sin(a) * 6 - 0.3, math.sin(a) * 6 + 0.3, 100, 106, m("steel"))
    cyl((0, 0, 112), 0.5, 24, "Z", m("steel"), sides=6)
    facade_uvs(); made.append(finish("Race_tower1"))
    # 2: an octagonal shaft ringed with light every 18 m, a slanted top.
    octo(15, 0, 6, "podium", chamfer=0.4)
    octo(10, 6, 96, "facade", chamfer=0.4)
    for z in range(24, 96, 18):
        octo(10.3, z - 0.5, z + 0.5, "accent", chamfer=0.4)
    prism([(-10, 96), (10, 96), (10, 100), (-10, 108)], -7, 7, m("steel"))
    facade_uvs(); made.append(finish("Race_tower2"))
    # 3: twins joined by a sky bridge, each with its neon edges.
    for x in (-11, 11):
        box(x - 7, x + 7, -8, 8, 0, 6, m("podium"))
        box(x - 6, x + 6, -6, 6, 6, 92 if x < 0 else 80, m("facade"))
        for cx in (x - 6.1, x + 5.7):
            for cy in (-6.1, 5.7):
                box(cx, cx + 0.4, cy, cy + 0.4, 6, 92 if x < 0 else 80, m("accent"))
    box(-5, 5, -3, 3, 52, 58, m("facade"))
    box(-5.2, 5.2, -3.2, 3.2, 51.4, 52, m("accent"))
    facade_uvs(); made.append(finish("Race_tower3"))
    # 4: a stepped block, a slim top and a spire.
    box(-18, 18, -14, 14, 0, 24, m("facade"))
    box(-13, 13, -11, 11, 24, 46, m("facade"))
    box(-8, 8, -8, 8, 46, 90, m("facade"))
    for (w, d, z) in ((18.3, 14.3, 24), (13.3, 11.3, 46)):
        box(-w, w, -d, d, z - 0.5, z + 0.5, m("accent"))
    cyl((0, 0, 98), 4, 16, "Z", m("steel"), sides=8, top=0.4)
    facade_uvs(); made.append(finish("Race_tower4"))
    return made

# The trench run's wall: one 20 m stretch, 22 m high, its face to the road at x = 0 and its bulk towards +x, like the
# barrier (RaceEvent bends it along the course). A sloped skirt, armour plates in two tiers with dark seams, a deep
# groove with a light strip at 2.8 m and another at 16 m, a run of pipes, a rib at each end and a coping on top.
def trench_wall():
    H = 22.0
    prism([(0, 0), (1.6, 0), (1.6, 2.2), (0.6, 2.2)], 0, L, m("plateDark"))                # the skirt, sloped back
    box(0.6, 1.6, 0, L, 2.2, 2.6, m("plateDark"))
    box(0.7, 1.6, 0, L, 2.6, 3.0, m("accent"))                                             # the low strip, in its groove
    box(0.6, 1.6, 0, L, 3.0, 3.4, m("plateDark"))
    for z0, z1 in ((3.4, 9.4), (10.2, 15.6)):                                              # two tiers of armour plates
        for k in range(4):
            y0, y1 = k * 5.0 + 0.12, (k + 1) * 5.0 - 0.12
            prism([(0.75, z0), (0.95, z0), (0.95, z1), (0.75, z1 - 0.3)], y0, y1, m("plate"))
            box(0.95, 1.6, y0 - 0.12, y1 + 0.12, z0, z1, m("plateDark"))                   # the seam behind them
    box(0.95, 1.6, 0, L, 9.4, 10.2, m("plateDark"))
    for y in (0.4, 0.75):                                                                  # pipes along the seam
        cyl((0.65, L / 2, 9.8 + (y - 0.55)), 0.18, L, "Y", m("pipe"), sides=8)
    box(0.95, 1.6, 0, L, 15.6, 16.0, m("plateDark"))
    box(1.05, 1.6, 0, L, 16.0, 16.3, m("accent"))                                          # the high strip
    box(0.6, 1.6, 0, L, 16.3, H, m("plate"))                                              # the upper wall
    for k in range(5):
        y = k * 4.0 + 2.0
        box(0.5, 0.6, y - 0.6, y + 0.6, 17.2, 20.8, m("plateDark"))                        # vents up top
    box(0.3, 1.6, 0, L, H - 0.4, H, m("pipe"))                                             # the coping
    for y in (0.0, L - 0.9):
        box(0.2, 1.6, y, y + 0.9, 0, H - 0.4, m("plateDark"))                              # a rib at each end
    return finish("Race_trenchwall")

reset()
made = [barrier(), gantry(), lamp(), fan("Race_fan"), fan("Race_fan2", wave=True), seats(), arch(), car(), hazard(), *towers(), trench_wall()]  # finish() exports each one on its own

if "--" in sys.argv and "shot" in sys.argv[sys.argv.index("--") + 1:]:
    b, g, l, f1, f2, st, ar, cr, hz, *tw = made
    tr = tw.pop()
    tr.location = (60, 0, 0)
    cr.location = (-6, -6, 0); cr.rotation_euler = (0, 0, 0.6)
    hz.location = (-6, -6, 0)
    for k, o in enumerate(tw):
        o.location = (-120 + k * 60, 200, 0)
    ar.location = (0, 60, 0)
    st.location = (-30, -4, 2.2)
    for k in range(10):
        c = (f1 if k % 2 == 0 else f2).copy()
        bpy.context.scene.collection.objects.link(c)
        c.location = (-29.6, -3.5 + k * 0.9, 2.2)
    for side, x in ((1, 22.0), (-1, -22.0)):  # both sides of a 44 m road, three stretches each
        for s in range(3):
            c = b if (side, s) == (1, 0) else b.copy()
            if c is not b:
                bpy.context.scene.collection.objects.link(c)
            c.location = (x, s * L - 10, 0)
            c.scale = (side, 1, 1)
    g.location = (0, 22, 0)
    l.location = (24.2, 4, 0)
    render([("race_kit_preview", (30, -34, 12), (0, 14, 5)), ("race_kit_close", (14, -8, 2.2), (23, 8, 0.8)), ("race_kit_crowd", (-22, -6, 4.2), (-30, 0.5, 3.0)), ("race_kit_wreck", (4, -18, 6), (-6, -6, 1.5)), ("race_kit_towers", (-30, 30, 50), (-30, 200, 55)), ("race_kit_trench", (48, -14, 9), (61, 10, 9))], lens=30)

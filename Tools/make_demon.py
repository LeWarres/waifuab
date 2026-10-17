# The demon: a mini boss look (Combat "MiniDemon", beside the mech one): a hunched crimson brute, muscles in
# separate masses with dark striations, bone plates down its chest and belly, a long draconic head with brow
# ridges, curled horns and a gaping double row of fangs, huge clawed fists, digitigrade legs in teal greaves, an
# armoured tail ending in a bone blade, wide bat wings with curved finger bones, sagging membranes and torn,
# spiked trailing edges, spines down its back and cyan sparks crawling over it.
#   blender -b --python Tools/make_demon.py           -> Assets/Resources/Models/Enemy_demon.fbx
#   blender -b --python Tools/make_demon.py -- shot   -> also a preview picture, Tools/demon_preview.png
# Unit sized like the other enemies (the wings reach well past it), centred on the origin, facing -Y (Unity's
# forward), +Z up. Vertex colours, drawn untinted (EnemyType.model), as Tools/make_gunship.py. The fleshy masses
# are subdivided and smooth shaded so it reads as a creature, not a pile of balls.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
_mat = mat
def mat(name, rgb):  # vertex colours are taken as they are by the toon shader: baked pre-darkened
    return _mat(name, tuple(c ** 2.2 for c in rgb))

CRIMSON = mat("Crimson", (0.78, 0.14, 0.3))
CRIMSON_LIGHT = mat("CrimsonLight", (0.95, 0.3, 0.42))
CRIMSON_DARK = mat("CrimsonDark", (0.45, 0.07, 0.22))
PURPLE = mat("Purple", (0.34, 0.15, 0.4))
PURPLE_DARK = mat("PurpleDark", (0.2, 0.08, 0.26))
STRIATION = mat("Striation", (0.25, 0.03, 0.12))
BONE = mat("Bone", (0.92, 0.86, 0.72))
BONE_DARK = mat("BoneDark", (0.7, 0.62, 0.5))
TEAL = mat("Teal", (0.55, 0.82, 0.84))
TEAL_DARK = mat("TealDark", (0.28, 0.5, 0.55))
WING = mat("Wing", (0.88, 0.34, 0.44))
WING_DARK = mat("WingDark", (0.6, 0.18, 0.32))
WING_EDGE = mat("WingEdge", (1.0, 0.25, 0.3))
CYAN = mat("Cyan", (0.45, 0.97, 1.0))
TONGUE = mat("Tongue", (0.98, 0.35, 0.4))
GUM = mat("Gum", (0.55, 0.05, 0.12))
FANG = mat("Fang", (0.98, 0.96, 0.9))
DARK = mat("Dark", (0.1, 0.03, 0.06))

flesh = []  # the parts to round off

def blob(center, radius, m, scale=(1, 1, 1), rot=(0, 0, 0)):  # a smooth mass of muscle
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=radius, location=center)
    ob = bpy.context.object
    ob.name = "flesh"
    ob.data.materials.append(m)
    for p in ob.data.polygons: p.use_smooth = True
    ob.scale = scale
    ob.rotation_euler = tuple(math.radians(a) for a in rot)
    parts.append(ob)
    flesh.append(ob)
    return ob

def cone(a, b, r0, r1, m, sides=10, smooth=True):  # a tapering round piece from a (radius r0) to b (radius r1)
    a, b = Vector(a), Vector(b)
    ob = cyl((0, 0, 0), r0, (b - a).length, "Z", m, sides=sides, top=r1)
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(b - a)
    ob.location = (a + b) / 2
    if smooth:
        for p in ob.data.polygons: p.use_smooth = True
    return ob

def limb(a, b, r0, r1, m):  # a muscled limb segment: a smooth tapered piece, a round joint at each end
    ob = cone(a, b, r0, r1, m, 14)
    blob(a, r0 * 0.98, m)
    blob(b, r1 * 0.98, m)
    return ob

def chain(points, r0, r1, m, sides=10):  # a bent horn, claw, finger or tail: cones through the points, thinning
    for i in range(len(points) - 1):
        k0, k1 = i / (len(points) - 1), (i + 1) / (len(points) - 1)
        cone(points[i], points[i + 1], r0 + (r1 - r0) * k0, r0 + (r1 - r0) * k1, m, sides)
        if 0 < i: ball(points[i], r0 + (r1 - r0) * k0, m)

def curve(a, b, bulge, n=5):  # points along a gentle arc from a to b, bowed by bulge
    a, b, bulge = Vector(a), Vector(b), Vector(bulge)
    return [tuple(a.lerp(b, i / n) + bulge * math.sin(math.pi * i / n)) for i in range(n + 1)]

def strands(points, thick=0.006):  # dark striations over a muscle
    for p, q in zip(points, points[1:]):
        cone(p, q, thick, thick * 0.7, STRIATION, 5, False)

def slab(points, m, thick=0.007):  # a thin flat piece through these points (membranes, plates)
    pts = [Vector(p) for p in points]
    n = len(pts)
    verts = [tuple(p) for p in pts] + [tuple(p + Vector((0, thick, 0))) for p in pts]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))] + [((j + 1) % n, j, n + j, n + (j + 1) % n) for j in range(n)]
    return mesh("slab", verts, faces, m)

# ---- the torso, hunched forward: chest, pecs, traps, lats, a narrow waist, bone plates down the front
blob((0, -0.02, 0.12), 0.16, CRIMSON, (1.3, 0.85, 1.05), (12, 0, 0))
for s in (-1, 1):
    blob((s * 0.08, -0.12, 0.16), 0.085, CRIMSON_LIGHT, (1.2, 0.6, 0.9), (0, 0, s * 15))        # pec
    blob((s * 0.12, 0.02, 0.24), 0.08, CRIMSON, (1.3, 0.9, 0.7))                               # trap
    blob((s * 0.15, 0.03, 0.06), 0.08, CRIMSON_DARK, (0.8, 0.9, 1.5))                          # lat
    strands(curve((s * 0.03, -0.17, 0.2), (s * 0.15, -0.13, 0.12), (s * 0.01, -0.02, 0.03)))
    strands(curve((s * 0.04, -0.17, 0.14), (s * 0.15, -0.1, 0.06), (0, -0.01, 0.02)))
blob((0, -0.03, -0.06), 0.1, CRIMSON_DARK, (1.05, 0.85, 1.2))                                 # belly
for z, w in ((0.21, 0.12), (0.16, 0.13), (0.11, 0.115)):                                      # bone ribs
    for s in (-1, 1):
        cone((s * 0.01, -0.175, z), (s * w, -0.15, z + 0.025), 0.011, 0.006, BONE, 8)
for i, z in enumerate((0.03, -0.02, -0.07, -0.12)):                                          # ab plates
    w = 0.05 - i * 0.006
    for s in (-1, 1):
        slab([(s * 0.006, -0.135, z - 0.022), (s * w, -0.13, z - 0.018), (s * w, -0.128, z + 0.018), (s * 0.006, -0.133, z + 0.022)], BONE)
cone((0, -0.18, 0.24), (0, -0.115, -0.15), 0.006, 0.004, DARK, 6, False)                     # the seam
slab([(-0.07, -0.1, -0.13), (0.07, -0.1, -0.13), (0.045, -0.095, -0.27), (0, -0.09, -0.31), (-0.045, -0.095, -0.27)], BONE)  # loin plate
slab([(-0.05, -0.105, -0.15), (0.05, -0.105, -0.15), (0.03, -0.1, -0.25), (-0.03, -0.1, -0.25)], BONE_DARK)
for i in range(6):                                                                           # spines down the back
    z = 0.28 - i * 0.07
    cone((0, 0.1 + i * 0.006, z), (0, 0.17 + i * 0.01, z + 0.05), 0.02, 0.0, BONE, 6)
# ---- arms: huge shoulders with a spike, bulging biceps, thick forearms, clawed fists
for s in (-1, 1):
    blob((s * 0.22, -0.02, 0.21), 0.1, CRIMSON, (1.0, 1.0, 0.95))                              # deltoid
    cone((s * 0.27, 0.0, 0.27), (s * 0.33, 0.04, 0.36), 0.025, 0.0, BONE, 6)                   # shoulder spike
    limb((s * 0.26, -0.03, 0.15), (s * 0.32, -0.07, 0.01), 0.07, 0.055, CRIMSON)               # upper arm
    blob((s * 0.29, -0.09, 0.09), 0.055, CRIMSON_LIGHT, (0.9, 0.8, 1.2))                       # bicep
    strands(curve((s * 0.27, -0.13, 0.14), (s * 0.32, -0.11, 0.03), (s * 0.02, -0.02, 0)))
    cone((s * 0.34, 0.0, 0.01), (s * 0.38, 0.06, -0.02), 0.02, 0.0, BONE, 6)                   # elbow spike
    limb((s * 0.32, -0.07, 0.01), (s * 0.35, -0.12, -0.15), 0.065, 0.05, CRIMSON_DARK)        # forearm
    strands(curve((s * 0.3, -0.13, -0.0), (s * 0.33, -0.17, -0.13), (s * 0.02, -0.01, 0)))
    blob((s * 0.355, -0.135, -0.19), 0.065, CRIMSON_DARK, (1.0, 1.05, 0.9))                   # fist
    for k in range(4):                                                                       # knuckles and claws
        x = s * (0.32 + k * 0.023)
        ball((x, -0.18, -0.17), 0.016, CRIMSON)
        chain([(x, -0.19, -0.18), (x, -0.235, -0.21), (x, -0.23, -0.265)], 0.012, 0.002, FANG, 6)
    chain([(s * 0.31, -0.15, -0.21), (s * 0.29, -0.19, -0.24), (s * 0.3, -0.2, -0.27)], 0.013, 0.002, FANG, 6)  # thumb claw
# ---- the head: a long snout thrust forward, brow ridges, horns, a frill, the jaw hanging open
blob((0, -0.06, 0.27), 0.06, CRIMSON_DARK, (1.2, 1.1, 1.0))                                  # neck
blob((0, -0.12, 0.33), 0.062, CRIMSON, (1.1, 1.0, 0.85))                                     # skull
blob((0, -0.21, 0.32), 0.045, CRIMSON_DARK, (0.85, 1.9, 0.55), (8, 0, 0))                     # snout
for s in (-1, 1):
    blob((s * 0.035, -0.17, 0.355), 0.025, BONE_DARK, (1.0, 1.6, 0.5), (0, 0, s * 20))       # brow ridge
    ball((s * 0.03, -0.19, 0.345), 0.011, CYAN)                                              # glowing eye
    chain([(s * 0.04, -0.1, 0.37), (s * 0.08, -0.08, 0.44), (s * 0.085, -0.03, 0.51), (s * 0.06, 0.02, 0.55), (s * 0.04, 0.04, 0.56)], 0.026, 0.003, BONE)  # horns
    for k in range(3):                                                                       # ridges on the horn
        cone((s * (0.05 + k * 0.012), -0.09 + k * 0.012, 0.39 + k * 0.04), (s * (0.065 + k * 0.012), -0.085 + k * 0.012, 0.395 + k * 0.04), 0.024, 0.024, BONE_DARK, 10)
    slab([(s * 0.05, -0.08, 0.3), (s * 0.12, -0.04, 0.34), (s * 0.11, -0.02, 0.27), (s * 0.06, -0.05, 0.25)], CRIMSON_DARK, 0.005)  # frill
blob((0, -0.2, 0.25), 0.04, CRIMSON_DARK, (0.9, 2.0, 0.45), (-30, 0, 0))                      # lower jaw, dropped
blob((0, -0.2, 0.278), 0.033, GUM, (0.9, 1.5, 0.3))                                           # inside the mouth
blob((0, -0.21, 0.275), 0.02, TONGUE, (1.0, 1.8, 0.4), (-15, 0, 0))
for k in range(7):                                                                           # fangs, two rows
    x = -0.03 + k * 0.01
    y = -0.25 + abs(k - 3) * 0.012
    cone((x, y, 0.305), (x, y - 0.004, 0.27 - (0.012 if k in (1, 5) else 0)), 0.005, 0.0, FANG, 5, False)
    cone((x, y + 0.01, 0.245), (x, y + 0.006, 0.275), 0.0045, 0.0, FANG, 5, False)
# ---- legs: thick striated thighs, shins bent back, teal greaves and knee guards, clawed feet
for s in (-1, 1):
    limb((s * 0.09, 0.0, -0.14), (s * 0.13, -0.04, -0.3), 0.085, 0.06, PURPLE)                 # thigh
    blob((s * 0.11, -0.04, -0.2), 0.065, PURPLE, (0.9, 0.8, 1.3))
    strands(curve((s * 0.07, -0.1, -0.15), (s * 0.12, -0.09, -0.29), (s * 0.02, -0.01, 0)))
    strands(curve((s * 0.13, -0.08, -0.14), (s * 0.15, -0.07, -0.28), (s * 0.01, -0.01, 0)))
    limb((s * 0.13, -0.04, -0.3), (s * 0.15, 0.05, -0.39), 0.055, 0.045, PURPLE_DARK)          # shin, bent back
    blob((s * 0.135, -0.06, -0.305), 0.045, TEAL, (1.0, 0.8, 1.1))                            # knee guard
    cone((s * 0.15, 0.05, -0.39), (s * 0.15, -0.01, -0.47), 0.05, 0.04, TEAL_DARK, 12)        # greave
    slab([(s * 0.12, 0.0, -0.4), (s * 0.18, 0.0, -0.4), (s * 0.17, -0.02, -0.46), (s * 0.13, -0.02, -0.46)], TEAL, 0.012)
    blob((s * 0.15, -0.04, -0.485), 0.05, TEAL, (1.0, 1.4, 0.4))                             # foot
    for k in (-1, 0, 1):                                                                     # toe claws
        chain([(s * 0.15 + k * 0.028, -0.09, -0.49), (s * 0.15 + k * 0.032, -0.13, -0.5), (s * 0.15 + k * 0.034, -0.15, -0.515)], 0.012, 0.002, TEAL_DARK, 6)
# ---- the tail: teal armoured segments curling out behind, a bone blade at the tip
tail = curve((0, 0.09, -0.16), (0.42, 0.24, -0.16), (0.06, 0.12, -0.2), 7)
for i in range(len(tail) - 1):
    r = 0.05 - i * 0.0045
    limb(tail[i], tail[i + 1], r, r - 0.004, TEAL if i % 2 == 0 else TEAL_DARK)
slab([(0.42, 0.24, -0.16), (0.47, 0.25, -0.12), (0.52, 0.24, 0.02), (0.46, 0.23, -0.04)], BONE, 0.012)
# ---- the wings: curved bone arms from the shoulder blades, finger bones fanning out, sagging torn membranes
for s in (-1, 1):
    root, elbow, wrist = Vector((s * 0.12, 0.08, 0.26)), Vector((s * 0.44, 0.15, 0.5)), Vector((s * 0.64, 0.17, 0.68))
    chain([tuple(root), tuple(elbow), tuple(wrist)], 0.032, 0.02, BONE, 12)
    ball(tuple(wrist), 0.026, BONE_DARK)
    chain(curve(tuple(wrist), (s * 0.61, 0.16, 0.82), (s * -0.02, 0, 0), 2), 0.02, 0.0, BONE, 6)        # thumb spike
    tips = [Vector(t) for t in ((s * 1.0, 0.21, 0.74), (s * 0.95, 0.21, 0.44), (s * 0.76, 0.19, 0.17), (s * 0.45, 0.15, 0.02))]
    for tip in tips:                                                                          # curved finger bones
        chain(curve(tuple(wrist), tuple(tip), (0, 0, 0.04), 3), 0.017, 0.004, BONE, 8)
        cone(tuple(tip), tuple(tip + (tip - wrist).normalized() * 0.05), 0.006, 0.0, BONE_DARK, 5)     # claw on the tip
    # Between each pair of bones a membrane, its trailing edge sagging in towards the body and torn into spikes.
    anchors = [tips[0], tips[1], tips[2], tips[3], Vector((s * 0.16, 0.1, 0.05))]
    for i in range(len(anchors) - 1):
        a, b = anchors[i], anchors[i + 1]
        mid = (a + b) / 2
        sag = mid + (wrist - mid) * 0.32
        edge = []
        for k in range(7):  # along the torn edge, from a to b through the sag, spiking in and out
            t = k / 6
            p = (a * (1 - t) + b * t) * (1 - math.sin(math.pi * t) * 0.6) + sag * math.sin(math.pi * t) * 0.6
            pull = (wrist - p) * (0.08 if k % 2 else -0.02)
            edge.append(p + pull)
        slab([wrist] + edge, WING if i % 2 == 0 else WING_DARK)
        for p, q in zip(edge, edge[1:]):                                                      # a red rim on the tear
            cone(tuple(p), tuple(q), 0.004, 0.004, WING_EDGE, 4, False)
    slab([root, elbow, wrist, anchors[4]], WING_DARK)                                         # the inner panel
    for hole in ((0.55, 0.17, 0.33), (0.8, 0.2, 0.52), (0.68, 0.18, 0.2), (0.38, 0.14, 0.2)):  # tears through it
        c = Vector((s * hole[0], hole[1] - 0.004, hole[2]))
        slab([c + Vector((s * -0.03, 0, 0.0)), c + Vector((0, 0, 0.035)), c + Vector((s * 0.025, 0, 0.005)), c + Vector((0, 0, -0.03))], DARK, 0.016)
# ---- cyan sparks crawling over the shoulders, arms and hips
import random
random.seed(7)
def spark(start, toward, steps=6, jag=0.035):
    p, d = Vector(start), (Vector(toward) - Vector(start)) / steps
    for _ in range(steps):
        q = p + d + Vector((random.uniform(-jag, jag), random.uniform(-jag, jag) * 0.4, random.uniform(-jag, jag)))
        cone(tuple(p), tuple(q), 0.0055, 0.004, CYAN, 4, False)
        p = q
for s in (-1, 1):
    spark((s * 0.15, -0.14, 0.3), (s * 0.3, -0.14, 0.0))
    spark((s * 0.24, -0.1, 0.26), (s * 0.4, -0.12, 0.1), 5)
    spark((s * 0.33, -0.15, 0.0), (s * 0.36, -0.18, -0.16), 4)
    spark((s * 0.05, -0.13, -0.1), (s * 0.16, -0.1, -0.28), 5)
    spark((s * 0.18, 0.05, 0.3), (s * 0.35, 0.12, 0.45), 4)

# Round off the fleshy masses, then everything goes out as one mesh.
for ob in flesh:
    mod = ob.modifiers.new("Round", "SUBSURF")
    mod.levels = 1
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier="Round")
finish("Enemy_demon", paint=True)

if "shot" in sys.argv:
    reset()
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Resources", "Models", "Enemy_demon.fbx"))
    ob = bpy.context.selected_objects[0]
    vc = bpy.data.materials.new("VC")
    vc.use_nodes = True
    nodes = vc.node_tree.nodes
    attr = nodes.new("ShaderNodeVertexColor")
    gamma = nodes.new("ShaderNodeGamma")  # undo the pre-darkening, to see the colours as the game will
    gamma.inputs["Gamma"].default_value = 1 / 2.2
    vc.node_tree.links.new(attr.outputs["Color"], gamma.inputs["Color"])
    vc.node_tree.links.new(gamma.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    ob.data.materials.clear()
    ob.data.materials.append(vc)
    ob.location = (0, 0, 0.55)
    render([("demon_preview", (0.9, -2.6, 0.9), (0, 0, 0.6)), ("demon_side", (2.4, -1.2, 0.8), (0, 0, 0.55))], lens=40)

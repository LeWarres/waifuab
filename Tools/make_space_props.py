# The far things of space: planets (ringed, banded, cratered) and tumbling asteroids. One low-poly model each,
# named space__<name>, into Assets/Scenery/Props; Tools/bake_space_props.cs turns them into the space scenery
# under Resources/Scenery/space (replacing the moon-base props there). Standing on z = 0, centred.
#   blender -b --python Tools/make_space_props.py           (-- shot: also Tools/space_props_preview.png)
import math, os, random, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

OUT = os.path.join(ROOT, "Assets", "Scenery", "Props")
os.makedirs(OUT, exist_ok=True)
random.seed(4)

def m(name, rgb, emit=0.0):
    return bpy.data.materials.get(name) or mat(name, rgb, emit=emit)

def lumpy(center, radius, colour, seed, rough=0.22, sub=2):
    # an asteroid: an icosphere pushed in and out at random
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=radius, location=center)
    ob = bpy.context.object
    rng = random.Random(seed)
    for v in ob.data.vertices:
        v.co *= 1.0 + rng.uniform(-rough, rough)
    ob.data.materials.append(colour)
    parts.append(ob)
    return ob

def banded(center, radius, colours, bands):
    # a gas giant: stacked slices of a sphere, each band its own colour
    for k in range(bands):
        z0 = -radius + 2 * radius * k / bands
        z1 = -radius + 2 * radius * (k + 1) / bands
        r0 = math.sqrt(max(radius * radius - z0 * z0, 0.0))
        r1 = math.sqrt(max(radius * radius - z1 * z1, 0.0))
        cyl((center[0], center[1], center[2] + (z0 + z1) / 2), max(r0, r1, 0.05), z1 - z0, "Z", colours[k % len(colours)], sides=24, top=None)
        parts[-1].scale = (1, 1, 1)
        ob = parts[-1]
        # taper each slice to the sphere's curve
        for v in ob.data.vertices:
            rr = r1 if v.co.z > 0 else r0
            f = rr / max(r0, r1, 0.05)
            v.co.x *= f
            v.co.y *= f

def ringed():
    ball((0, 0, 7), 6, m("PlanetCoral", (0.95, 0.5, 0.35)))
    for k, (r, w, c) in enumerate(((9.5, 0.5, (0.95, 0.85, 0.6)), (11, 0.35, (0.8, 0.65, 0.45)), (12.2, 0.25, (0.9, 0.8, 0.7)))):
        ob = ring((0, 0, 7), r, w, m("PlanetRing%d" % k, c))
        ob.scale = (1, 1, 0.12)
        ob.rotation_euler = (math.radians(18), math.radians(8), 0)

def giant():
    banded((0, 0, 10), 10, [m("GiantA", (0.85, 0.6, 0.35)), m("GiantB", (0.95, 0.85, 0.65)), m("GiantC", (0.7, 0.4, 0.3)), m("GiantD", (0.9, 0.75, 0.5))], 9)
    ball((3.5, -9.2, 9), 1.6, m("GiantSpot", (0.75, 0.3, 0.2)), scale=(1.6, 0.4, 1))

def moon():
    ball((0, 0, 4), 4, m("MoonGrey", (0.7, 0.72, 0.78)))
    for k in range(7):
        a, b = random.uniform(0, math.tau), random.uniform(-0.9, 0.9)
        p = (math.cos(a) * math.cos(b) * 3.9, math.sin(a) * math.cos(b) * 3.9, 4 + math.sin(b) * 3.9)
        ball(p, random.uniform(0.5, 1.0), m("MoonCrater", (0.5, 0.52, 0.58)), scale=(1, 1, 0.45))

def icy():
    ball((0, 0, 5), 5, m("IcyBlue", (0.45, 0.75, 1.0)))
    ball((0, 0, 5), 5.15, m("IcyCloud", (0.9, 0.97, 1.0)), scale=(1, 1, 0.35))

def asteroid_a():
    lumpy((0, 0, 1.6), 1.6, m("RockDark", (0.42, 0.38, 0.36)), 1)
    lumpy((1.8, 0.6, 0.8), 0.7, m("RockDark", (0.42, 0.38, 0.36)), 2)

def asteroid_b():
    lumpy((0, 0, 2.4), 2.4, m("RockRed", (0.55, 0.4, 0.32)), 3, rough=0.28)
    lumpy((-2.6, -1.0, 1.0), 0.9, m("RockRed", (0.55, 0.4, 0.32)), 4)
    lumpy((2.2, 1.6, 0.6), 0.5, m("RockRed", (0.55, 0.4, 0.32)), 5)

BUILDS = [("ringed", ringed), ("giant", giant), ("moon", moon), ("icy", icy), ("asteroid_a", asteroid_a), ("asteroid_b", asteroid_b)]
for name, build in BUILDS:
    reset()
    build()
    finish("space__" + name, OUT)

if "shot" in sys.argv:
    reset()
    for i, (name, _) in enumerate(BUILDS):
        bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "space__" + name + ".fbx"))
        for ob in bpy.context.selected_objects:
            ob.location = (i * 26 - 65, 0, 0)
    render([("space_props_preview", (0, -110, 40), (0, 0, 8))], lens=35)

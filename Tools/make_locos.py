# More locomotives to choose from, one low-poly model each, plus the picture of it the selection screen shows:
#   Locomotive_Steam   classic steam engine no. 279: black boiler, silver smokebox, red trim
#   Locomotive_Dark    a ghost steam engine: black iron, horned skull on the smokebox, red eyes, skulls in its smoke
#   Locomotive_Future  a heavy orange machine: turbine face, white armour, pipes, antennas
#   Locomotive_Cat     white steam engine with a cat's head: brown and black markings, blue eyes, whiskers, paws and tail
#   Locomotive_Shark   a hot rod with a shark's head: deep blue, flames, gold teeth and engine, red wheels
#   Locomotive_Armor   grey armoured war train: slab sides, wedge nose, plates, ribs, guns
#   Locomotive_Jet     abandoned jet train: rusty hull, two turbines on the nose, moss on the roof, landing legs
#   Locomotive_Beast   rusted iron relic: a ribbed monster skull with tusks and teeth riding a low boiler, spoked wheels
#   Locomotive_Crochet the first diesel again, in greens, as if crocheted: soft corners, stitches, button wheels
#   Locomotive_Spider  violet tank engine with a mask for a face and a pale abdomen; its eight legs are
#                      SpiderLeg_Upper / SpiderLeg_Lower, one bone each, which the game moves (SpiderLegs.cs)
#   blender -b --python Tools/make_locos.py
# Like the first locomotive: nose towards +X, standing on the rail (z = 0), about 5.3 long and 1.5 wide.
# Models go to Assets/Resources/Models, pictures to Assets/Resources/Menu/loco_<name>.png.
# Materials carry their real colours; one whose name starts with Glow comes out bright in the game.
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()

def tilt(ob, x=0, y=0, z=0):
    ob.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))
    return ob

def cone(center, radius, height, m, sides=8, tip=0.0, axis="Z"):
    return cyl(center, radius, height, axis, m, sides=sides, top=tip)

# Raised lettering: a text object turned into a mesh. facing: "X" reads from the front, "Y-" / "Y+" from a side.
def text(body, at, size, m, facing="X"):
    bpy.ops.object.text_add(location=at)
    ob = bpy.context.object
    ob.data.body = body
    ob.data.size = size
    ob.data.extrude = 0.02
    ob.data.align_x = "CENTER"
    ob.data.align_y = "CENTER"
    ob.rotation_euler = {"X": (math.radians(90), 0, math.radians(90)), "Y-": (math.radians(90), 0, 0), "Y+": (math.radians(90), 0, math.radians(180))}[facing]
    bpy.ops.object.convert(target="MESH")
    ob.data.materials.append(m)
    parts.append(ob)
    return ob

def wheel(x, y, radius, rim, hub):
    cyl((x, y, radius), radius, 0.12, "Y", rim, sides=14)
    cyl((x, y + (0.07 if y > 0 else -0.07), radius), radius * 0.45, 0.04, "Y", hub, sides=8)

def picture(ob, name):
    scene = bpy.context.scene
    if "IconCamera" not in bpy.data.objects:
        scene.render.engine = "BLENDER_EEVEE"
        scene.render.film_transparent = True
        scene.render.resolution_x, scene.render.resolution_y = 512, 320
        scene.render.image_settings.file_format = "PNG"
        scene.render.image_settings.color_mode = "RGBA"
        scene.view_settings.view_transform = "Standard"
        world = bpy.data.worlds.new("IconWorld")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs[0].default_value = (0.6, 0.7, 0.9, 1)
        scene.world = world
        light = bpy.data.objects.new("IconLight", bpy.data.lights.new("IconLight", "AREA"))
        scene.collection.objects.link(light)
        light.location = (2, -4, 7)
        light.data.energy = 1000
        light.data.size = 5
        light.rotation_euler = (Vector((0, 0, 1)) - light.location).to_track_quat("-Z", "Y").to_euler()
        camera = bpy.data.objects.new("IconCamera", bpy.data.cameras.new("IconCamera"))
        scene.collection.objects.link(camera)
        camera.location = (6, -6, 4)
        camera.rotation_euler = (Vector((0, 0, 1.4)) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = 7.2
        scene.camera = camera
    for other in bpy.data.objects:
        if other.type == "MESH":
            other.hide_render = other is not ob
    scene.render.filepath = os.path.join(ROOT, "Assets", "Resources", "Menu", name + ".png")
    bpy.ops.render.render(write_still=True)

# ====================== steam engine 279 ======================
BLACK = mat("SteamBlack", (0.05, 0.05, 0.06), 0.45, 0.3)
SILVER = mat("SteamSilver", (0.72, 0.74, 0.78), 0.3, 0.8)
STEAMRED = mat("SteamRed", (0.8, 0.12, 0.1))
BRASS = mat("SteamBrass", (0.85, 0.62, 0.2), 0.3, 0.8)
IRON = mat("SteamIron", (0.22, 0.23, 0.26), 0.4, 0.6)
CABWHITE = mat("SteamCream", (0.9, 0.9, 0.86))
LENS = mat("GlowSteamLamp", (1.0, 0.95, 0.75), emit=1.0)
PANE = mat("SteamGlass", (0.25, 0.55, 0.85), 0.1)

box(-2.6, 2.3, -0.5, 0.5, 0.5, 0.68, IRON, "frame")
for x in (-0.55, 0.35, 1.25):                      # three driving wheels a side, red centres
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.44, IRON, STEAMRED), 0.56, 0.68)
both(lambda y0, y1: box(-0.75, 1.45, y0, y1, 0.36, 0.46, STEAMRED, "rod"), 0.7, 0.76)   # coupling rods
for x in (1.95, -1.6, -2.2):                       # pilot and trailing wheels
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.24, IRON, SILVER), 0.56, 0.68)
both(lambda y0, y1: box(1.6, 2.2, y0, y1, 0.5, 0.95, IRON, "cylinder"), 0.5, 0.82)       # steam chests
cyl((0.55, 0, 1.4), 0.62, 2.9, "X", BLACK, sides=16, name="boiler")
for x in (-0.6, 0.3, 1.2):
    cyl((x, 0, 1.4), 0.64, 0.07, "X", SILVER, sides=16, name="band")
cyl((2.1, 0, 1.4), 0.66, 0.35, "X", SILVER, sides=16, name="smokebox")
cyl((2.3, 0, 1.4), 0.6, 0.06, "X", SILVER, sides=16, name="door")
cyl((2.34, 0, 1.4), 0.2, 0.05, "X", IRON, sides=10, name="door_hub")
for a in range(8):                                  # bolts round the smokebox door
    ball((2.33, math.cos(a * 0.785) * 0.5, 1.4 + math.sin(a * 0.785) * 0.5), 0.035, IRON)
cyl((1.75, 0, 2.25), 0.2, 0.55, "Z", BLACK, sides=10, name="stack"); cyl((1.75, 0, 2.55), 0.27, 0.12, "Z", BLACK, sides=10, name="stack_lip")
ball((0.9, 0, 2.0), 0.28, BLACK, "dome"); ball((0.1, 0, 2.0), 0.24, BRASS, "sand_dome")
# Headlight on a bracket ahead of the stack, and the bell with its red mouth.
box(2.0, 2.35, -0.22, 0.22, 2.0, 2.08, IRON); cyl((2.25, 0, 2.3), 0.22, 0.34, "X", IRON, sides=12); cyl((2.43, 0, 2.3), 0.18, 0.03, "X", LENS, sides=12)
cone((1.3, 0, 2.2), 0.16, 0.26, BRASS, sides=10, tip=0.05); cyl((1.3, 0, 2.08), 0.17, 0.04, "Z", STEAMRED, sides=10); box(1.27, 1.33, -0.02, 0.02, 2.33, 2.5, IRON)
# Cab: black, cream band, red window frames, the number on its sides.
box(-2.5, -0.9, -0.74, 0.74, 0.68, 2.25, BLACK, "cab"); box(-2.62, -0.78, -0.8, 0.8, 2.25, 2.36, IRON, "cab_roof")
box(-2.52, -0.88, -0.75, 0.75, 1.05, 1.15, CABWHITE, "cab_line")
both(lambda y0, y1: box(-2.2, -1.2, y0, y1, 1.45, 2.05, STEAMRED, "window_frame"), 0.74, 0.76)
both(lambda y0, y1: box(-2.12, -1.28, y0 - (0.01 if y0 < 0 else 0), y1 + (0.01 if y1 > 0 else 0), 1.52, 1.98, PANE, "window"), 0.74, 0.77)
text("279", (-1.7, -0.775, 1.28), 0.3, CABWHITE, "Y-"); text("279", (-1.7, 0.775, 1.28), 0.3, CABWHITE, "Y+")
# Running boards, handrails and the ladder up the front.
both(lambda y0, y1: box(-0.9, 2.3, y0, y1, 0.86, 0.92, IRON, "running_board"), 0.62, 0.84)
both(lambda y0, y1: box(-0.8, 2.0, y0, y1, 1.75, 1.79, SILVER, "handrail"), 0.66, 0.69)
box(2.3, 2.62, -0.8, 0.8, 0.62, 0.72, IRON, "buffer_beam")
# The red plate with the number, low on the front, and a red lamp beside it.
box(2.62, 2.66, -0.36, 0.36, 0.74, 1.02, STEAMRED, "plate"); text("279", (2.67, 0, 0.88), 0.24, CABWHITE, "X")
ball((2.64, 0.6, 0.9), 0.09, STEAMRED)
# Cowcatcher: a row of silver bars sloping forward, red tips at both ends.
for i in range(9):
    y = -0.72 + i * 0.18
    prism([(2.62, 0.56), (2.62, 0.48), (3.0, 0.06), (3.06, 0.12)], y - 0.035, y + 0.035, SILVER, "pilot_bar")
box(2.96, 3.08, -0.8, 0.8, 0.04, 0.12, SILVER, "pilot_foot")
both(lambda y0, y1: box(2.9, 3.1, y0, y1, 0.02, 0.2, STEAMRED, "pilot_tip"), 0.72, 0.84)
picture(finish("Locomotive_Steam"), "loco_steam")

# ====================== the dark one ======================
# A ghost of a steam engine: black iron, a horned skull for a smokebox door with burning red eyes, a red lamp
# on the stack, spikes and a long cage of bars for a cowcatcher. The game adds the smoke.
NIGHT = mat("DarkBlack", (0.03, 0.03, 0.04), 0.4, 0.4)
COAL = mat("DarkIron", (0.13, 0.13, 0.15), 0.4, 0.6)
RUST = mat("DarkRust", (0.2, 0.17, 0.16), 0.5, 0.5)
BONE = mat("DarkBone", (0.4, 0.38, 0.33))            # old bone, grey and dirty
EYES = mat("GlowDarkEyes", (1.0, 0.08, 0.05), emit=1.0)
EMBER = mat("GlowDarkEmber", (1.0, 0.25, 0.05), emit=1.0)

# A skull looking along +X: cranium, cheekbones, a jaw of long teeth, deep sockets with a glow inside.
def skull(x, y, z, s, glow=True):
    ball((x, y, z + 0.1 * s), 0.6 * s, BONE, "cranium", (0.85, 1.0, 0.95))
    box(x + 0.05 * s, x + 0.5 * s, y - 0.4 * s, y + 0.4 * s, z - 0.75 * s, z - 0.3 * s, BONE, "jaw")
    for side in (-1, 1):
        ball((x + 0.2 * s, y + side * 0.42 * s, z - 0.25 * s), 0.16 * s, BONE, "cheek")
        ball((x + 0.44 * s, y + side * 0.23 * s, z + 0.08 * s), 0.2 * s, NIGHT, "socket", (0.5, 1, 1.15))
        if glow:
            ball((x + 0.5 * s, y + side * 0.23 * s, z + 0.06 * s), 0.09 * s, EYES, "eye")
    prism([(x + 0.52 * s, z - 0.3 * s), (x + 0.54 * s, z - 0.3 * s), (x + 0.52 * s, z - 0.1 * s)], y - 0.07 * s, y + 0.07 * s, NIGHT, "nose_hole")
    box(x + 0.5 * s, x + 0.53 * s, y - 0.36 * s, y + 0.36 * s, z - 0.72 * s, z - 0.36 * s, NIGHT, "mouth")
    for k in range(6):                                # fangs, top and bottom
        ty = y + (-0.3 + k * 0.12) * s
        prism([(x + 0.5 * s, z - 0.36 * s), (x + 0.56 * s, z - 0.36 * s), (x + 0.53 * s, z - 0.55 * s)], ty - 0.045 * s, ty + 0.045 * s, BONE, "fang")
        prism([(x + 0.5 * s, z - 0.72 * s), (x + 0.53 * s, z - 0.56 * s), (x + 0.56 * s, z - 0.72 * s)], ty - 0.045 * s, ty + 0.045 * s, BONE, "fang")

# A horn curving out and up: a string of shrinking balls along an arc.
def horn(x, y, z, side, size):
    for k in range(8):
        a = k / 7.0
        ball((x - 0.08 * a, y + side * (0.1 + 0.42 * math.sin(a * 1.5)) * size, z + (0.75 * a + 0.12 * math.sin(a * 3.1)) * size), (0.15 - 0.125 * a) * size, COAL, "horn")

box(-2.6, 2.3, -0.5, 0.5, 0.5, 0.68, COAL, "frame")
for x in (-0.55, 0.35, 1.25):                       # spoked driving wheels
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.44, NIGHT, RUST), 0.56, 0.68)
both(lambda y0, y1: box(-0.75, 1.45, y0, y1, 0.36, 0.46, RUST, "rod"), 0.7, 0.76)
for x in (1.95, -1.6, -2.2):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.24, NIGHT, RUST), 0.56, 0.68)
both(lambda y0, y1: box(1.55, 2.25, y0, y1, 0.45, 1.0, COAL, "cylinder"), 0.5, 0.86)
both(lambda y0, y1: ball((1.9, (y0 + y1) * 0.5 + (0.2 if y0 > 0 else -0.2), 0.72), 0.1, EYES, "side_lamp"), 0.5, 0.86)
cyl((0.55, 0, 1.4), 0.62, 2.9, "X", NIGHT, sides=16, name="boiler")
for x in (-0.6, 0.0, 0.6, 1.2):                     # riveted bands, a spike on top of each
    cyl((x, 0, 1.4), 0.65, 0.08, "X", COAL, sides=16, name="band")
    cone((x, 0, 2.02), 0.07, 0.3, COAL, sides=5)
cyl((2.1, 0, 1.4), 0.7, 0.4, "X", COAL, sides=16, name="smokebox")
cyl((2.3, 0, 1.4), 0.72, 0.06, "X", RUST, sides=16, name="door_rim")
for a in range(12):
    ball((2.33, math.cos(a * 0.524) * 0.66, 1.4 + math.sin(a * 0.524) * 0.66), 0.035, NIGHT)
skull(2.3, 0, 1.45, 0.82)
horn(2.2, 0.45, 1.75, 1, 1.25); horn(2.2, -0.45, 1.75, -1, 1.25)
# Tall stack with a crown, the red lamp ahead of it, two spiked domes.
cyl((1.7, 0, 2.3), 0.22, 0.7, "Z", NIGHT, sides=10, name="stack"); cyl((1.7, 0, 2.7), 0.32, 0.22, "Z", COAL, sides=10, name="crown")
for a in range(6):
    cone((1.7 + math.cos(a * 1.047) * 0.3, math.sin(a * 1.047) * 0.3, 2.8), 0.05, 0.2, COAL, sides=4)
box(1.95, 2.3, -0.16, 0.16, 2.1, 2.16, COAL); cyl((2.2, 0, 2.38), 0.2, 0.3, "X", COAL, sides=10); cyl((2.36, 0, 2.38), 0.16, 0.04, "X", EYES, sides=10)
for x in (0.95, 0.2):
    ball((x, 0, 2.0), 0.26, COAL, "dome"); cone((x, 0, 2.2), 0.06, 0.3, COAL, sides=5)
# (Its smoke is real: the game pours it out of the stack, see Smoke.cs and Locomotives.cs.)
# Cab like a crypt: arched windows glowing like embers, spiked roof.
box(-2.5, -0.9, -0.74, 0.74, 0.68, 2.25, NIGHT, "cab"); prism([(-2.65, 2.25), (-0.75, 2.25), (-1.0, 2.5), (-2.4, 2.5)], -0.8, 0.8, COAL, "cab_roof")
both(lambda y0, y1: box(-2.2, -1.2, y0, y1, 1.35, 2.05, RUST, "window_frame"), 0.74, 0.76)
both(lambda y0, y1: prism([(-2.1, 1.42), (-1.3, 1.42), (-1.3, 1.85), (-1.7, 2.02), (-2.1, 1.85)], y0 - (0.01 if y0 < 0 else 0), y1 + (0.01 if y1 > 0 else 0), EMBER, "window"), 0.74, 0.77)
for x in (-2.3, -1.7, -1.1):
    cone((x, 0, 2.5), 0.07, 0.35, COAL, sides=5)
box(-2.5, -0.9, -0.75, 0.75, 0.68, 0.78, RUST, "cab_foot")
both(lambda y0, y1: box(-0.9, 2.3, y0, y1, 0.86, 0.92, COAL, "running_board"), 0.62, 0.84)
both(lambda y0, y1: box(-0.8, 1.9, y0, y1, 1.75, 1.79, RUST, "handrail"), 0.66, 0.69)
# Ladders up both sides of the smokebox, a wide deck and a long cage of a cowcatcher under it.
for side in (-1, 1):
    y = side * 0.5
    for dy in (-0.12, 0.12):
        prism([(2.42, 0.72), (2.48, 0.72), (2.4, 1.9), (2.34, 1.9)], y + dy - 0.02, y + dy + 0.02, RUST, "ladder_rail")
    for z in (0.95, 1.2, 1.45, 1.7):
        box(2.36, 2.46, y - 0.12, y + 0.12, z, z + 0.03, RUST, "rung")
box(2.3, 2.75, -0.86, 0.86, 0.62, 0.72, COAL, "deck")
for y in (-0.8, 0.8):
    cone((2.7, y, 0.72), 0.05, 0.35, COAL, sides=4)
for i in range(11):
    y = -0.8 + i * 0.16
    prism([(2.72, 0.62), (2.72, 0.54), (3.25, 0.05), (3.31, 0.11)], y - 0.03, y + 0.03, RUST, "pilot_bar")
box(3.2, 3.33, -0.86, 0.86, 0.03, 0.12, COAL, "pilot_foot")
for i in range(5):
    tilt(cone((3.3, -0.64 + i * 0.32, 0.08), 0.05, 0.28, BONE, sides=4), y=90)
picture(finish("Locomotive_Dark"), "loco_dark")

# ====================== the one from the future ======================
# A heavy machine from another world: a huge orange drum with a turbine for a face, white armour plates,
# pipes and tanks all over it, antennas on the roof.
ORANGE = mat("TitanOrange", (0.92, 0.36, 0.1))
ORANGE2 = mat("TitanRust", (0.62, 0.2, 0.07))
PLATE = mat("TitanWhite", (0.86, 0.85, 0.8))
PIPE = mat("TitanPipe", (0.5, 0.52, 0.56), 0.35, 0.8)
MACHINE = mat("TitanDark", (0.1, 0.1, 0.12), 0.45, 0.5)
CORE = mat("GlowTitanCore", (1.0, 0.6, 0.15), emit=1.0)

box(-2.6, 2.4, -0.62, 0.62, 0.42, 0.72, MACHINE, "frame")
for x in (-1.9, -1.1, 0.3):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.36, MACHINE, ORANGE2), 0.6, 0.74)
both(lambda y0, y1: wheel(1.5, (y0 + y1) * 0.5, 0.55, MACHINE, ORANGE), 0.62, 0.78)        # one great driving wheel
both(lambda y0, y1: box(-2.3, 2.2, y0, y1, 0.68, 0.8, ORANGE2, "side_skirt"), 0.62, 0.8)
# The drum: orange, a white armour plate over its upper front, and a second, smaller white module behind.
cyl((0.9, 0, 1.6), 0.9, 2.6, "X", ORANGE, sides=18, name="drum")
cyl((1.3, 0, 1.66), 0.93, 1.1, "X", PLATE, sides=18, name="armour")
box(0.4, 2.0, -0.95, 0.95, 0.75, 1.3, ORANGE, "belly")                                       # hides the plate's lower half
cyl((-1.55, 0, 1.45), 0.72, 1.9, "X", ORANGE, sides=16, name="rear_module")
for x in (-2.35, -1.55, -0.75):
    cyl((x, 0, 1.45), 0.76, 0.16, "X", PLATE, sides=16, name="rear_ring")
cyl((-0.4, 0, 1.55), 0.8, 0.28, "X", ORANGE2, sides=16, name="joint")
# The face: a thick orange ring, a white ring inside it, and the dark turbine with its blades and hub.
cyl((2.25, 0, 1.6), 1.02, 0.42, "X", ORANGE, sides=20, name="face_ring")
cyl((2.46, 0, 1.6), 0.86, 0.08, "X", PLATE, sides=20, name="face_white")
cyl((2.5, 0, 1.6), 0.72, 0.06, "X", MACHINE, sides=20, name="turbine")
cyl((2.54, 0, 1.6), 0.4, 0.05, "X", PIPE, sides=16, name="turbine_plate")
cyl((2.58, 0, 1.6), 0.14, 0.06, "X", CORE, sides=10, name="hub")
for k in range(10):
    blade = box(2.53, 2.57, -0.035, 0.035, 0.4, 0.7, PIPE, "blade")   # built round the hub, then turned about it
    blade.location = (0, 0, 1.6)
    blade.rotation_euler = (k * math.tau / 10, 0, 0)
both(lambda y0, y1: box(2.2, 2.5, y0, y1, 1.5, 1.7, ORANGE2, "face_lug"), 1.0, 1.1)
# Round ports on the armour.
for x in (1.1, 1.5):
    both(lambda y0, y1: cyl((x, (y0 + y1) * 0.5, 2.0), 0.16, 0.08, "Y", MACHINE, sides=12), 0.8, 0.88)
# Pipes: along both flanks, up into the drum, and over the roof.
for z, y in ((1.05, 0.98), (1.25, 1.04), (0.9, 0.9)):
    both(lambda y0, y1: cyl((-0.3, (y0 + y1) * 0.5, z), 0.07, 4.0, "X", PIPE, sides=8), y - 0.02, y + 0.02)
for x in (-0.9, 0.0, 0.6):
    both(lambda y0, y1: cyl((x, (y0 + y1) * 0.5, 1.0), 0.07, 0.75, "Z", PIPE, sides=8), 0.94, 1.0)
    both(lambda y0, y1: ball((x, (y0 + y1) * 0.5, 1.38), 0.1, ORANGE2), 0.94, 1.0)
cyl((-0.2, 0.3, 2.42), 0.09, 3.2, "X", PIPE, sides=8)
cyl((-0.4, -0.25, 2.38), 0.07, 2.6, "X", PIPE, sides=8)
# Tanks and antennas on the roof; a railing along the back of it.
for x, y, r, h in ((0.3, -0.3, 0.2, 0.5), (0.75, 0.25, 0.16, 0.6), (-1.0, 0.25, 0.22, 0.45), (-1.6, -0.2, 0.18, 0.55), (-0.3, -0.1, 0.14, 0.4)):
    z = 2.4 if x > -0.4 else 2.1
    cyl((x, y, z + h * 0.5), r, h, "Z", PIPE, sides=10)
    ball((x, y, z + h), r, ORANGE, "tank_cap", (1, 1, 0.5))
for x, y, h in ((0.5, 0.0, 1.0), (-0.6, 0.3, 0.75), (-1.3, -0.3, 0.9), (-2.0, 0.1, 0.6)):
    cyl((x, y, 2.3 + h * 0.5), 0.02, h, "Z", MACHINE, sides=5)
    ball((x, y, 2.3 + h), 0.05, CORE)
both(lambda y0, y1: box(-2.4, -0.8, y0, y1, 2.5, 2.54, PIPE, "railing"), 0.5, 0.54)
for x in (-2.4, -1.6, -0.8):
    both(lambda y0, y1: box(x - 0.02, x + 0.02, y0, y1, 2.0, 2.52, PIPE, "railing_post"), 0.5, 0.54)
# Orange machinery boxes low on the flanks, a heavy bumper with lamps.
for x in (-1.6, -0.4):
    both(lambda y0, y1: box(x - 0.4, x + 0.4, y0, y1, 0.72, 1.0, ORANGE, "machine_box"), 0.8, 1.0)
box(2.4, 2.8, -0.9, 0.9, 0.3, 0.75, MACHINE, "bumper")
box(2.78, 2.84, -0.7, 0.7, 0.42, 0.62, ORANGE2, "bumper_plate")
both(lambda y0, y1: ball((2.82, (y0 + y1) * 0.5, 0.52), 0.09, CORE), 0.5, 0.7)
picture(finish("Locomotive_Future"), "loco_future")

# ====================== the cat ======================
# A white steam engine with a cat's head for a smokebox: pointed ears, blue eyes, whiskers, front paws over
# the pilot wheels and a tail curling up over the cab.
FUR = mat("CatWhite", (0.9, 0.9, 0.88))
FUR2 = mat("CatGrey", (0.6, 0.62, 0.68))
PINK = mat("CatPink", (1.0, 0.55, 0.65))
CATDARK = mat("CatDark", (0.12, 0.13, 0.17), 0.45, 0.4)
CATEYE = mat("GlowCatEyes", (0.3, 0.6, 1.0), emit=1.0)
CATBROWN = mat("CatBrown", (0.4, 0.26, 0.16))
CATBLACK = mat("CatBlack", (0.07, 0.06, 0.07))

# A marking on the fur: a shell just outside a round part, cut down to the points keep(x, y, z) says yes to.
def patch(center, radius, scale, m, keep, name="patch", fine=1):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32 * fine, ring_count=20 * fine, radius=radius, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    me = ob.data
    me.materials.append(m)
    parts.append(ob)
    gone = [v.index for v in me.vertices if not keep(*(center[i] + v.co[i] * scale[i] for i in range(3)))]
    import bmesh
    bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.verts[i] for i in gone], context="VERTS")
    bm.to_mesh(me); bm.free()
    return ob

CATLAMP = mat("GlowCatLamp", (1.0, 0.9, 0.6), emit=1.0)

box(-2.6, 2.3, -0.5, 0.5, 0.5, 0.68, CATDARK, "frame")
for x in (-0.55, 0.35, 1.25):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.44, CATDARK, PINK), 0.56, 0.68)
both(lambda y0, y1: box(-0.75, 1.45, y0, y1, 0.36, 0.46, FUR2, "rod"), 0.7, 0.76)
for x in (-1.6, -2.2):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.24, CATDARK, FUR2), 0.56, 0.68)
cyl((0.55, 0, 1.4), 0.62, 2.9, "X", FUR, sides=16, name="boiler")
for x in (-0.6, 0.3, 1.2):
    cyl((x, 0, 1.4), 0.64, 0.07, "X", FUR2, sides=16, name="band")
for x in (1.2, 0.5, -0.2):                           # three low stacks along the back
    cyl((x, 0, 2.15), 0.17, 0.3, "Z", FUR2, sides=10)
    cyl((x, 0, 2.3), 0.2, 0.06, "Z", CATDARK, sides=10)
# The head: round, wider than the boiler, with a muzzle and cheeks.
ball((2.3, 0, 1.65), 0.82, FUR, "head", (0.9, 1.05, 0.92))
ball((2.85, 0, 1.38), 0.36, FUR, "muzzle", (0.8, 1.15, 0.75))
both(lambda y0, y1: ball((2.65, (y0 + y1) * 0.5, 1.35), 0.3, FUR, "cheek", (0.8, 1, 0.8)), 0.3, 0.5)
# Its markings: brown over the brow on both sides of a white blaze, black behind that round the ears,
# and a black streak from between the eyes down to the nose.
patch((2.3, 0, 1.65), 0.835, (0.9, 1.05, 0.92), CATBROWN, lambda x, y, z: z > 1.84 and abs(y) > 0.09 + (x - 2.3) * 0.12 and x > 2.1, "brow")
patch((2.3, 0, 1.65), 0.845, (0.9, 1.05, 0.92), CATBLACK, lambda x, y, z: z > 2.0 and x < 2.42 and abs(y) > 0.2, "crown")
patch((2.3, 0, 1.65), 0.85, (0.9, 1.05, 0.92), CATBLACK, lambda x, y, z: x > 2.5 and 1.5 < z < 1.95 and abs(y) < 0.13 + (1.95 - z) * 0.35, "streak")
patch((2.85, 0, 1.38), 0.375, (0.8, 1.15, 0.75), CATBLACK, lambda x, y, z: z > 1.47 and abs(y) < 0.16 and x > 2.85, "streak_muzzle")
for side in (-1, 1):
    # Ears: tall, pointed, black outside, pink inside.
    tilt(cone((2.25, side * 0.5, 2.4), 0.42, 1.0, CATBLACK, sides=4), x=-side * 16)
    tilt(cone((2.42, side * 0.5, 2.4), 0.26, 0.7, PINK, sides=4), x=-side * 16)
    # Big blue eyes with dark pupils, rimmed dark.
    ball((2.9, side * 0.36, 1.76), 0.19, CATDARK, "socket", (0.45, 1.0, 0.9))
    ball((2.95, side * 0.36, 1.76), 0.15, CATEYE, "eye", (0.5, 1.0, 0.95))
    ball((3.0, side * 0.365, 1.76), 0.07, CATBLACK, "pupil", (0.45, 0.8, 1.2))
    for k in (-1, 0, 1):                             # whiskers, fanning out from the cheek
        whisker = box(0, 0.02, 0, 0.62, -0.009, 0.009, CATDARK, "whisker")
        whisker.location = (2.96, side * 0.34, 1.32 + k * 0.03)
        whisker.scale = (1, side, 1)
        whisker.rotation_euler = (math.radians(side * k * 12), 0, 0)
    # Front paws: rounded white fenders over the pilot wheels, a lamp on each, pink toes.
    ball((2.45, side * 0.62, 0.62), 0.42, FUR, "paw", (1.25, 0.8, 1.0))
    cyl((2.95, side * 0.62, 0.66), 0.14, 0.08, "X", CATDARK, sides=10)
    cyl((3.0, side * 0.62, 0.66), 0.1, 0.03, "X", CATLAMP, sides=10)
    for k in (-1, 0, 1):
        ball((2.9, side * 0.62 + k * 0.14, 0.34), 0.07, PINK, "toe")
prism([(3.11, 1.56), (3.17, 1.56), (3.14, 1.44)], -0.1, 0.1, CATBLACK, "nose")
box(3.12, 3.15, -0.012, 0.012, 1.32, 1.44, CATDARK, "mouth_line")
box(3.1, 3.135, -0.14, 0.14, 1.3, 1.325, CATDARK, "mouth")
both(lambda y0, y1: wheel(2.0, (y0 + y1) * 0.5, 0.24, CATDARK, FUR2), 0.56, 0.68)
box(2.6, 2.9, -0.5, 0.5, 0.45, 0.62, CATDARK, "buffer_beam")
for k in range(7):                                   # grey cowcatcher
    y = -0.42 + k * 0.14
    prism([(2.9, 0.5), (2.9, 0.44), (3.2, 0.06), (3.26, 0.12)], y - 0.035, y + 0.035, FUR2, "pilot_bar")
box(3.16, 3.28, -0.48, 0.48, 0.04, 0.12, FUR2, "pilot_foot")
# Cab: white, dark roof, pink window frames and a paw print. The tail curls up from its roof.
box(-2.5, -0.9, -0.74, 0.74, 0.68, 2.25, FUR, "cab")
box(-2.62, -0.78, -0.8, 0.8, 2.25, 2.36, CATDARK, "cab_roof")
both(lambda y0, y1: box(-2.2, -1.2, y0, y1, 1.45, 2.05, PINK, "window_frame"), 0.74, 0.76)
both(lambda y0, y1: box(-2.12, -1.28, y0, y1, 1.52, 1.98, CATDARK, "window"), 0.75, 0.775)
both(lambda y0, y1: ball((-1.7, (y0 + y1) * 0.5, 1.12), 0.15, PINK, "paw_print", (1, 0.2, 1)), 0.72, 0.78)
for k in range(12):
    a = k / 11.0
    ball((-2.3 + 0.4 * math.sin(a * 2.6), 0, 2.3 + 0.85 * a), 0.2 - 0.07 * a, FUR if k < 10 else FUR2, "tail")
both(lambda y0, y1: box(-0.9, 2.2, y0, y1, 0.86, 0.92, FUR2, "running_board"), 0.62, 0.84)
# Canaries riding along on the running boards, a yellow one and an orange one on each side.
CANARY = mat("CanaryYellow", (1.0, 0.85, 0.15))
CANARY2 = mat("CanaryOrange", (1.0, 0.5, 0.1))
BEAK = mat("CanaryBeak", (0.85, 0.3, 0.05))
def canary(x, y, m):
    ball((x, y, 1.0), 0.09, m, "canary", (1.3, 1, 1))
    ball((x + 0.1, y, 1.12), 0.065, m, "canary_head")
    cone((x + 0.19, y, 1.12), 0.025, 0.07, BEAK, sides=4, axis="X")
    prism([(x - 0.24, 1.06), (x - 0.08, 1.0), (x - 0.08, 1.04), (x - 0.24, 1.1)], y - 0.025, y + 0.025, m, "canary_tail")
    both(lambda y0, y1: ball((x + 0.14, y + (y0 + y1) * 0.5, 1.14), 0.015, CATDARK, "canary_eye"), 0.04, 0.06)
for side in (-1, 1):
    canary(0.95, side * 0.75, CANARY if side > 0 else CANARY2)
    canary(0.1, side * 0.75, CANARY2 if side > 0 else CANARY)
for ob in parts:                                     # the head sticks out in front: slide it all back to keep the length
    ob.location.x -= 0.35
picture(finish("Locomotive_Cat"), "loco_cat")

# ====================== the shark ======================
# A hot rod: one long deep blue shark, flames down its flanks, gold teeth, narrow mean eyes; gold engines
# bolted low on both flanks with their exhausts running back along the body, fat tyres on red wheels.
SHARK = mat("SharkBlue", (0.05, 0.14, 0.62), 0.3, 0.4)
SHARKBACK = mat("SharkBack", (0.03, 0.08, 0.38), 0.3, 0.4)
GOLD = mat("SharkGold", (0.95, 0.7, 0.18), 0.3, 0.8)
GOLD2 = mat("SharkGoldDark", (0.6, 0.4, 0.08), 0.35, 0.8)
FLAME = mat("SharkFlame", (1.0, 0.82, 0.15))
FLAME2 = mat("SharkFlameCore", (1.0, 0.42, 0.08))
MOUTH = mat("SharkMouth", (0.45, 0.05, 0.08))
REDWHEEL = mat("SharkRed", (0.85, 0.08, 0.1))
SHARKDARK = mat("SharkDark", (0.06, 0.06, 0.08), 0.5, 0.2)
SHARKEYE = mat("GlowSharkEye", (1.0, 0.8, 0.1), emit=1.0)
MID, HALF = (-0.3, 0, 1.45), (3.0, 0.8, 0.85)       # the body: an egg this big round this point

# Where the body's skin is, towards the nose, at this y and z (k: how far outside it).
def skin(y, z, k=1.0):
    return MID[0] + HALF[0] * math.sqrt(max(0.0, k * k - (y / HALF[1]) ** 2 - ((z - MID[2]) / HALF[2]) ** 2))

def hotwheel(x, y, radius):
    side = 1 if y > 0 else -1
    cyl((x, y, radius), radius, 0.24, "Y", SHARKDARK, sides=16, name="tyre")
    cyl((x, y + side * 0.03, radius), radius * 0.62, 0.24, "Y", REDWHEEL, sides=12, name="wheel")
    cyl((x, y + side * 0.07, radius), radius * 0.2, 0.2, "Y", SHARKDARK, sides=8, name="wheel_cap")

box(-2.6, 2.0, -0.5, 0.5, 0.42, 0.72, SHARKDARK, "frame")
for x, r in ((-2.1, 0.5), (0.55, 0.45), (1.5, 0.38)):
    both(lambda y0, y1: hotwheel(x, (y0 + y1) * 0.5, r), 0.56, 0.8)
# The body, whole, its mouth wide open.
patch(MID, 1.0, HALF, SHARK, lambda x, y, z: x > -2.9, "body")
patch(MID, 1.008, HALF, SHARKBACK, lambda x, y, z: z > 1.98 and x > -2.9, "back")
patch(MID, 1.02, HALF, MOUTH, lambda x, y, z: 1.03 < z < 1.34 and x > 1.45, "mouth")
collar = cyl((-2.45, 0, 1.45), 1.0, 0.6, "X", SHARKBACK, sides=18, name="collar")    # closes the body off at the back
collar.scale = (0.7, 0.66, 1)
for k in range(9):                                   # big gold teeth: a row hanging from the upper lip, a row standing on the lower
    y = -0.6 + k * 0.15
    tilt(cone((skin(y, 1.31, 1.02) + 0.02, y, 1.21), 0.075, 0.22, GOLD, sides=4), x=180)
    if k < 8:
        cone((skin(y + 0.075, 1.06, 1.02) + 0.02, y + 0.075, 1.14), 0.065, 0.18, GOLD, sides=4)
# Flames licking back along the flanks, yellow with an orange heart.
lick = lambda x, z: abs(math.sin((x + z * 0.7) * 5.0))
patch(MID, 1.012, HALF, FLAME, lambda x, y, z: abs(y) > 0.25 and 0.0 < x < 1.5 and 1.58 < z < 1.72 + 0.3 * lick(x, z), "flame", fine=7)
patch(MID, 1.02, HALF, FLAME2, lambda x, y, z: abs(y) > 0.25 and 0.15 < x < 1.35 and 1.58 < z < 1.62 + 0.2 * lick(x, z), "flame_core", fine=7)
for side in (-1, 1):
    # Eyes: narrow yellow slits slanting down to the nose, a thin pupil, under a heavy brow.
    tilt(ball((1.78, side * 0.52, 1.63), 0.1, SHARKEYE, "eye", (1.8, 0.8, 0.4)), y=20)
    ball((1.84, side * 0.585, 1.61), 0.05, SHARKDARK, "pupil", (0.35, 0.6, 0.85))
    prism([(1.4, 1.84), (2.08, 1.66), (2.08, 1.76), (1.4, 2.0)], side * 0.5 - 0.13, side * 0.5 + 0.13, SHARKBACK, "brow")
    # A gold engine low on the flank, ribbed; four stubs out of it into the exhaust.
    box(-1.45, -0.05, side * 0.72 - 0.2, side * 0.72 + 0.2, 0.74, 1.22, GOLD, "engine")
    for x in (-1.25, -0.9, -0.55, -0.2):
        box(x - 0.05, x + 0.05, side * 0.72 - 0.23, side * 0.72 + 0.23, 0.8, 1.26, GOLD2, "engine_rib")
        cyl((x, side * 0.9, 1.32), 0.07, 0.3, "Z", GOLD, sides=8, name="header")
    # Exhausts: a thick one along the flank and a thinner one above it, open at the back.
    cyl((-1.2, side * 0.92, 1.45), 0.11, 2.8, "X", GOLD, sides=10, name="exhaust")
    ball((0.2, side * 0.92, 1.45), 0.11, GOLD, "exhaust_end")
    cyl((-2.61, side * 0.92, 1.45), 0.075, 0.02, "X", SHARKDARK, sides=10, name="exhaust_mouth")
    cyl((-1.5, side * 0.84, 1.74), 0.07, 2.2, "X", GOLD, sides=8, name="exhaust_thin")
    ball((-0.4, side * 0.84, 1.74), 0.07, GOLD, "exhaust_thin_end")
    cyl((-2.61, side * 0.84, 1.74), 0.045, 0.02, "X", SHARKDARK, sides=8, name="exhaust_thin_mouth")
    # Tail fins.
    prism([(-2.0, 1.85), (-2.6, 1.8), (-3.0, 2.6), (-2.65, 2.38)], side * 0.35 - 0.04, side * 0.35 + 0.04, SHARK, "tail_fin")
prism([(0.4, 2.2), (-0.7, 2.2), (-1.05, 3.1), (-0.6, 2.82)], -0.05, 0.05, SHARK, "dorsal_fin")
# An air scoop on the back, behind the fin.
box(-1.9, -1.2, -0.2, 0.2, 2.1, 2.3, GOLD2, "intake")
box(-2.0, -1.1, -0.26, 0.26, 2.3, 2.52, GOLD, "scoop")
box(-1.12, -1.08, -0.2, 0.2, 2.33, 2.49, SHARKDARK, "scoop_mouth")
box(1.9, 2.3, -0.5, 0.5, 0.35, 0.7, SHARKDARK, "bumper")
picture(finish("Locomotive_Shark"), "loco_shark")

# ====================== the armoured one ======================
# A grey war machine: two tall armoured casemates, a lower gun deck, a wedge for a nose. Plates, ribs and
# vents all over it, twin guns on both cheeks, a turret, an amber visor on the slope.
ARMOR = mat("ArmorGrey", (0.4, 0.43, 0.47), 0.5, 0.3)
ARMOR2 = mat("ArmorDark", (0.24, 0.26, 0.3), 0.5, 0.3)
ARMOR3 = mat("ArmorLight", (0.56, 0.59, 0.63), 0.5, 0.3)
GUN = mat("ArmorGun", (0.08, 0.08, 0.1), 0.45, 0.5)
VISOR = mat("GlowArmorVisor", (1.0, 0.55, 0.1), emit=1.0)

# A cross-section (y, z) run from x0 to x1.
def hull(x0, x1, section, m, name="hull"):
    n = len(section)
    verts = [(x0, y, z) for y, z in section] + [(x1, y, z) for y, z in section]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))] + [((i + 1) % n, i, n + i, n + (i + 1) % n) for i in range(n)]
    return mesh(name, verts, faces, m)

TALL = [(-0.78, 0.42), (0.78, 0.42), (0.78, 1.7), (0.55, 2.2), (-0.55, 2.2), (-0.78, 1.7)]
LOW = [(-0.74, 0.42), (0.74, 0.42), (0.74, 1.5), (0.5, 1.85), (-0.5, 1.85), (-0.74, 1.5)]
box(-2.6, 2.6, -0.5, 0.5, 0.25, 0.5, GUN, "frame")
for x in (-2.2, -1.6, -0.8, -0.2, 0.6, 1.3, 2.0):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.26, GUN, ARMOR2), 0.52, 0.64)
for x0, x1 in ((-2.65, -1.25), (-1.2, 0.2)):         # the two casemates, a dark seam between them
    hull(x0, x1, TALL, ARMOR, "casemate")
    for side in (-1, 1):
        y0, y1 = (0.78, 0.83) if side > 0 else (-0.83, -0.78)
        box(x0 + 0.08, x0 + 0.62, y0, y1, 0.95, 1.62, ARMOR3, "plate")
        box(x0 + 0.7, x1 - 0.08, y0, y1, 1.25, 1.62, ARMOR2, "plate")
        box(x0 + 0.7, x1 - 0.3, y0, y1, 0.95, 1.18, ARMOR3, "plate")
        box(x0 + 0.16, x0 + 0.54, y0 * 1.02, y1 * 1.02, 1.08, 1.3, ARMOR2, "plate_inset")
        for k in range(6):                           # ribs along the skirt
            x = x0 + 0.12 + k * 0.22
            prism([(x, 0.42), (x + 0.12, 0.42), (x + 0.12, 0.88), (x, 0.88)], min(y0, side * 0.9), max(y1, side * 0.9), ARMOR2, "rib")
        # Plates on the sloping shoulder: thin slabs lying on it.
        slab = [(x0 + 0.1, side * 0.75, 1.78), (x1 - 0.1, side * 0.75, 1.78), (x1 - 0.1, side * 0.6, 2.12), (x0 + 0.1, side * 0.6, 2.12)]
        mesh("shoulder_plate", slab + [(x, y + side * 0.04, z + 0.02) for x, y, z in slab],
             [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], ARMOR2)
    box(x0 + 0.15, x1 - 0.15, -0.3, 0.3, 2.2, 2.27, ARMOR2, "roof_plate")
    for k in range(4):
        box(x0 + 0.3 + k * 0.2, x0 + 0.38 + k * 0.2, -0.22, 0.22, 2.27, 2.31, GUN, "roof_vent")
    for k in range(4):
        cyl((x1 - 0.5 + k * 0.11, 0.42, 2.23), 0.04, 0.08, "Z", ARMOR3, sides=8, name="roof_stud")
box(-1.26, -1.19, -0.74, 0.74, 0.5, 2.12, GUN, "seam")
box(-2.72, -2.65, -0.6, 0.6, 0.6, 1.9, ARMOR2, "back_plate")
box(-2.4, -2.0, 0.15, 0.4, 2.27, 2.5, ARMOR2, "mast_box")
cyl((-2.2, 0.28, 2.85), 0.02, 0.7, "Z", GUN, sides=5)
# The gun deck: lower, a round hatch on each side, a turret on top.
hull(0.2, 1.2, LOW, ARMOR, "gun_deck")
box(0.17, 0.24, -0.7, 0.7, 0.5, 1.8, GUN, "seam")
cyl((0.6, 0, 1.95), 0.3, 0.22, "Z", ARMOR2, sides=10, name="turret")
box(0.5, 0.95, -0.14, 0.14, 1.92, 2.08, ARMOR3, "turret_mantle")
for y in (-0.07, 0.07):
    cyl((1.25, y, 2.0), 0.035, 0.9, "X", GUN, sides=6, name="turret_barrel")
for side in (-1, 1):
    cyl((0.7, side * 0.76, 1.02), 0.36, 0.1, "Y", ARMOR2, sides=14, name="hatch")
    cyl((0.7, side * 0.8, 1.02), 0.27, 0.08, "Y", ARMOR3, sides=14, name="hatch_plate")
    cyl((0.7, side * 0.83, 1.02), 0.09, 0.08, "Y", GUN, sides=8, name="hatch_hub")
    # Twin guns on the cheek, in an armoured mount.
    box(0.95, 1.75, side * 0.86 - 0.14, side * 0.86 + 0.14, 0.72, 1.16, ARMOR2, "gun_mount")
    box(1.05, 1.6, side * 0.92 - 0.1, side * 0.92 + 0.1, 0.8, 1.08, ARMOR3, "gun_mount_plate")
    for z in (0.86, 1.03):
        cyl((2.2, side * 0.86, z), 0.045, 1.1, "X", GUN, sides=6, name="barrel")
        cyl((2.7, side * 0.86, z), 0.065, 0.16, "X", GUN, sides=6, name="muzzle")
    # Cheek armour beside the wedge, and a small fin near the tip.
    prism([(1.2, 0.42), (2.6, 0.42), (2.6, 0.68), (1.2, 1.5)], side * 0.67 - 0.07, side * 0.67 + 0.07, ARMOR2, "cheek")
    prism([(2.2, 1.24), (2.55, 1.02), (2.3, 1.5)], side * 0.5 - 0.03, side * 0.5 + 0.03, ARMOR3, "fin")
# The nose: a wedge sloping down to the plough, an amber visor and plates on the slope.
prism([(1.2, 0.42), (2.9, 0.42), (2.9, 0.8), (1.2, 1.85)], -0.6, 0.6, ARMOR, "nose")
rise = lambda x, up=0.0: 1.85 - (x - 1.2) * 1.05 / 1.7 + up
prism([(1.42, rise(1.42)), (1.75, rise(1.75)), (1.75, rise(1.75, 0.05)), (1.42, rise(1.42, 0.05))], -0.42, 0.42, VISOR, "visor")
prism([(1.32, rise(1.32)), (1.4, rise(1.4)), (1.4, rise(1.4, 0.08)), (1.32, rise(1.32, 0.08))], -0.5, 0.5, ARMOR2, "visor_brow")
both(lambda y0, y1: prism([(1.9, rise(1.9)), (2.7, rise(2.7)), (2.7, rise(2.7, 0.04)), (1.9, rise(1.9, 0.04))], y0, y1, ARMOR3, "nose_plate"), 0.08, 0.5)
prism([(2.75, rise(2.75)), (2.9, rise(2.9)), (2.9, rise(2.9, 0.07)), (2.75, rise(2.75, 0.07))], -0.55, 0.55, ARMOR2, "nose_lip")
prism([(2.7, 0.08), (3.15, 0.08), (2.92, 0.5), (2.7, 0.5)], -0.72, 0.72, ARMOR2, "plough")
both(lambda y0, y1: box(2.9, 2.94, y0, y1, 0.52, 0.66, VISOR, "lamp"), 0.3, 0.5)
picture(finish("Locomotive_Armor"), "loco_armor")

# ====================== the forgotten jet train ======================
# Left to rust in a forest and running again: a brown hull with pale panels, a jet turbine on each side of
# the cab, a glass dome on the roof, moss and grass all over the top, landing legs folded out of the flanks.
RUST = mat("JetRust", (0.5, 0.24, 0.12))
RUST2 = mat("JetRustDark", (0.33, 0.15, 0.08))
PALE = mat("JetPale", (0.68, 0.58, 0.45))
MOSS = mat("JetMoss", (0.36, 0.56, 0.14))
MOSS2 = mat("JetMossDark", (0.2, 0.38, 0.1))
STEEL = mat("JetSteel", (0.17, 0.18, 0.21), 0.45, 0.5)
BRASS = mat("JetBrass", (0.62, 0.5, 0.3), 0.35, 0.8)
JETGLASS = mat("JetGlass", (0.05, 0.07, 0.09), 0.2, 0.2)
JETLAMP = mat("GlowJetLamp", (0.3, 0.85, 1.0), emit=1.0)

BODY = [(-0.5, 0.55), (0.5, 0.55), (0.64, 0.9), (0.64, 1.6), (0.45, 1.95), (-0.45, 1.95), (-0.64, 1.6), (-0.64, 0.9)]
box(-2.5, 2.1, -0.45, 0.45, 0.3, 0.58, STEEL, "frame")
for x in (-2.0, -1.4, 0.9, 1.5):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.3, STEEL, RUST2), 0.46, 0.58)
hull(-2.65, 1.9, BODY, RUST, "hull")
# The cab: a blunt face, one wide window with a pillar, a mossy sill under it.
hull(1.9, 2.35, [(-0.46, 0.7), (0.46, 0.7), (0.56, 0.95), (0.56, 1.6), (0.4, 1.85), (-0.4, 1.85), (-0.56, 1.6), (-0.56, 0.95)], PALE, "cab")
box(2.33, 2.38, -0.44, 0.44, 1.08, 1.6, JETGLASS, "window")
box(2.36, 2.4, -0.03, 0.03, 1.05, 1.63, RUST2, "window_pillar")
box(2.34, 2.4, -0.48, 0.48, 1.6, 1.68, RUST2, "window_brow")
box(2.3, 2.5, -0.5, 0.5, 0.96, 1.08, MOSS, "sill")
cyl((2.38, 0, 1.76), 0.07, 0.05, "X", JETLAMP, sides=10, name="badge")
box(2.1, 2.45, -0.5, 0.5, 0.4, 0.7, STEEL, "buffer")
for side in (-1, 1):
    # A turbine: rusty barrel, a paler lip, the fan and its brass spinner inside.
    at = (1.75, side * 0.98, 1.45)
    cyl(at, 0.44, 1.5, "X", RUST, sides=16, name="turbine")
    cyl((2.48, at[1], at[2]), 0.48, 0.14, "X", PALE, sides=16, name="turbine_lip")
    cyl((2.53, at[1], at[2]), 0.39, 0.06, "X", JETGLASS, sides=16, name="turbine_dark")
    for k in range(12):
        blade = box(2.55, 2.58, -0.025, 0.025, 0.1, 0.38, STEEL, "blade")
        blade.location = (0, at[1], at[2])
        blade.rotation_euler = (k * math.tau / 12, 0, 0)
    cone((2.62, at[1], at[2]), 0.13, 0.22, BRASS, sides=10, axis="X")
    cyl((1.0, at[1], at[2]), 0.34, 0.12, "X", RUST2, sides=14, name="turbine_tail")
    cyl((1.3, at[1], at[2]), 0.455, 0.1, "X", RUST2, sides=16, name="turbine_band")
    box(1.3, 2.2, side * 0.6 - 0.12, side * 0.6 + 0.12, 1.2, 1.7, RUST2, "pylon")
    # Pale panels down the flank, a rust post between each two.
    for k in range(7):
        x = -2.5 + k * 0.48
        box(x, x + 0.4, side * 0.66 - 0.03, side * 0.66 + 0.03, 0.95, 1.55, PALE if k % 3 else RUST2, "panel")
    box(-2.6, 0.9, side * 0.66 - 0.05, side * 0.66 + 0.05, 1.55, 1.63, RUST2, "rail")
    for x in (-2.0, -1.0):                           # small dark windows
        box(x, x + 0.3, side * 0.68 - 0.03, side * 0.68 + 0.03, 1.2, 1.42, JETGLASS, "porthole")
    # Landing legs: a strut out and down from the flank, a rusty foot at the end.
    for x in (0.2, -1.3, -2.3):
        beam = [(x - 0.08, side * 0.6, 1.0), (x + 0.08, side * 0.6, 1.0), (x - 0.12, side * 1.1, 0.22), (x - 0.28, side * 1.1, 0.22)]
        mesh("leg", beam + [(px, py + side * 0.07, pz + 0.09) for px, py, pz in beam],
             [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], STEEL)
        box(x - 0.34, x - 0.06, side * 1.1 - 0.1, side * 1.1 + 0.1, 0.14, 0.24, RUST2, "foot")
        box(x - 0.12, x + 0.12, side * 0.62 - 0.06, side * 0.62 + 0.06, 0.9, 1.1, RUST2, "leg_hinge")
# The dome on the roof: dark glass under rusty ribs.
ball((0.9, 0, 1.92), 0.5, JETGLASS, "dome", (1.25, 0.95, 0.55))
for k in range(-2, 3):
    rib = box(-0.03, 0.03, -0.5, 0.5, 0.0, 0.04, RUST2, "dome_rib")
    rib.location = (0.9 + k * 0.24, 0, 1.92 + 0.27 * math.cos(k * 0.5))
    rib.scale = (1, math.cos(k * 0.42), 1)
box(0.25, 1.55, -0.03, 0.03, 2.17, 2.22, RUST2, "dome_spine")
# A red hump with a lamp at the back, and a mast.
box(-2.3, -1.6, -0.25, 0.25, 1.95, 2.2, RUST2, "hump")
cyl((-1.95, 0, 2.28), 0.1, 0.16, "Z", BRASS, sides=8, name="hump_lamp")
cyl((-0.9, 0.3, 2.3), 0.02, 0.7, "Z", STEEL, sides=5)
# Moss: a carpet along the roof, lumps over the cab and the turbines, grass standing in it, strands hanging.
box(-1.5, 0.2, -0.42, 0.42, 1.95, 2.01, MOSS2, "moss_carpet")
for x, y, z, r in ((1.9, 0.0, 1.88, 0.34), (1.6, 0.35, 1.9, 0.3), (1.7, -0.4, 1.9, 0.28), (2.0, 0.85, 1.86, 0.26), (1.5, -1.0, 1.86, 0.3),
                   (1.2, 1.0, 1.84, 0.24), (-0.3, 0.2, 1.98, 0.3), (-0.9, -0.2, 1.98, 0.26), (-1.4, 0.25, 1.98, 0.22), (0.1, -0.35, 1.96, 0.24)):
    ball((x, y, z), r, MOSS if (x * 7 + y * 3) % 2 < 1 else MOSS2, "moss", (1.2, 1.0, 0.45))
for x, y in ((1.8, 0.1), (1.65, 0.4), (1.75, -0.35), (-0.2, 0.25), (-0.5, -0.1), (-1.0, -0.25), (-1.3, 0.2), (0.0, -0.3), (1.95, -0.2)):
    for dx, dy in ((0, 0), (0.07, 0.05), (-0.05, 0.07)):
        cone((x + dx, y + dy, 2.2), 0.03, 0.34, MOSS, sides=4)
for side in (-1, 1):
    for x, drop in ((-1.2, 0.3), (-0.4, 0.45), (0.1, 0.25), (0.6, 0.38)):
        box(x, x + 0.14, side * 0.66 - 0.05, side * 0.66 + 0.05, 1.63 - drop, 1.75, MOSS2, "moss_strand")
picture(finish("Locomotive_Jet"), "loco_jet")

# ====================== the iron beast ======================
# A relic dug out of the ground: a low black boiler on spoked wheels, and riding on its front a long ribbed
# monster skull in rusted bronze, tusks sweeping forward, jaws full of pale teeth, embers in its sockets.
IRON = mat("BeastIron", (0.1, 0.09, 0.09), 0.5, 0.5)
BRONZE = mat("BeastBronze", (0.38, 0.22, 0.09), 0.5, 0.2)
ORUST = mat("BeastRust", (0.68, 0.27, 0.05))
BONE = mat("BeastBone", (0.9, 0.88, 0.78))
MAW = mat("BeastMaw", (0.03, 0.02, 0.02))
EMBER = mat("GlowBeastEmber", (1.0, 0.4, 0.08), emit=1.0)

def spoked(x, y, radius):
    rim = ring((x, y, radius), radius - 0.05, 0.055, BRONZE, "rim")
    rim.rotation_euler = (math.radians(90), 0, 0)
    for k in range(4):
        spoke = box(-radius + 0.06, radius - 0.06, -0.025, 0.025, -0.025, 0.025, IRON, "spoke")
        spoke.location = (x, y, radius)
        spoke.rotation_euler = (0, k * math.pi / 4, 0)
    cyl((x, y, radius), radius * 0.24, 0.16, "Y", ORUST, sides=8, name="hub")

box(-2.5, 2.4, -0.42, 0.42, 0.45, 0.7, IRON, "frame")
for x, r in ((-1.9, 0.45), (-0.85, 0.45), (0.2, 0.45), (1.6, 0.36)):
    both(lambda y0, y1: spoked(x, (y0 + y1) * 0.5, r), 0.5, 0.62)
for side in (-1, 1):                                 # the rod that ties the wheels, with its fat joints
    cyl((-0.85, side * 0.7, 0.4), 0.06, 2.3, "X", BRONZE, sides=8, name="rod")
    for x in (-1.9, -0.85, 0.2):
        ball((x, side * 0.7, 0.4), 0.1, ORUST, "rod_joint")
    box(-1.7, 1.3, side * 0.62 - 0.14, side * 0.62 + 0.14, 0.86, 1.2, BRONZE, "side_tank")
    box(-0.4, -0.25, side * 0.62 - 0.16, side * 0.62 + 0.16, 0.84, 1.22, IRON, "tank_strap")
    box(0.6, 0.75, side * 0.62 - 0.16, side * 0.62 + 0.16, 0.84, 1.22, IRON, "tank_strap")
# The boiler: low, black, banded, ending in a great drum at the back.
cyl((-0.5, 0, 1.2), 0.5, 3.6, "X", IRON, sides=16, name="boiler")
for x in (-1.6, -0.8, 0.0):
    cyl((x, 0, 1.2), 0.52, 0.08, "X", ORUST if x == -0.8 else BRONZE, sides=16, name="band")
cyl((-2.4, 0, 1.15), 0.68, 0.55, "X", BRONZE, sides=18, name="drum")
cyl((-2.69, 0, 1.15), 0.56, 0.04, "X", IRON, sides=18, name="drum_end")
# The skull: a long cranium swept back over the boiler, ribbed in black iron, blotched with rust.
SKULL, LONG = (1.0, 0, 2.02), (1.4, 0.72, 0.6)
patch(SKULL, 1.0, LONG, BRONZE, lambda x, y, z: True, "cranium")
patch(SKULL, 1.03, LONG, IRON, lambda x, y, z: z > 1.95 and x < 1.9 and (x * 3.2) % 1.0 < 0.42, "cranium_rib", fine=3)
patch(SKULL, 1.035, LONG, IRON, lambda x, y, z: z > 1.95 and abs(y) < 0.06, "cranium_ridge", fine=3)
patch(SKULL, 1.015, LONG, ORUST, lambda x, y, z: math.sin(x * 9.0) * math.sin(z * 11.0 + y * 7.0) > 0.5, "rust", fine=3)
# Snout and jaws, the maw black between two rows of teeth.
prism([(1.5, 1.5), (2.75, 1.52), (2.8, 1.78), (2.3, 2.02), (1.5, 2.02)], -0.42, 0.42, BRONZE, "snout")
prism([(1.4, 1.02), (2.6, 1.06), (2.68, 1.24), (1.4, 1.3)], -0.4, 0.4, IRON, "jaw")
box(1.45, 2.62, -0.36, 0.36, 1.2, 1.55, MAW, "maw")
box(1.3, 2.05, -0.36, 0.36, 0.7, 1.05, ORUST, "chin_plate")
box(1.0, 1.6, -0.4, 0.4, 1.2, 1.7, IRON, "neck")
for k in range(7):                                   # front teeth
    y = -0.36 + k * 0.12
    box(2.7, 2.78, y - 0.045, y + 0.045, 1.38, 1.54, BONE, "tooth")
    box(2.6, 2.68, y - 0.045, y + 0.045, 1.2, 1.34, BONE, "tooth")
for side in (-1, 1):
    for k in range(6):                               # and down both sides
        x = 1.95 + k * 0.13
        box(x - 0.05, x + 0.05, side * 0.4 - 0.04, side * 0.4 + 0.04, 1.38, 1.54, BONE, "tooth")
        box(x - 0.1, x, side * 0.38 - 0.04, side * 0.38 + 0.04, 1.2, 1.34, BONE, "tooth")
    # A deep socket with an ember in it.
    ball((2.2, side * 0.4, 1.82), 0.17, MAW, "socket", (1.2, 0.5, 0.8))
    ball((2.22, side * 0.44, 1.81), 0.06, EMBER, "ember")
    # Tusks: a long one sweeping forward from behind the skull, a short one under it.
    ball((0.65, side * 0.72, 2.12), 0.2, IRON, "tusk_root")
    cone((1.6, side * 0.74, 2.12), 0.17, 1.9, BONE, sides=8, axis="X")
    cone((2.3, side * 0.5, 1.42), 0.07, 0.9, IRON, sides=6, axis="X")
    # Straps holding the skull down onto the boiler.
    box(0.2, 0.36, side * 0.5 - 0.06, side * 0.5 + 0.06, 1.2, 1.9, IRON, "strap")
    box(-0.25, -0.1, side * 0.42 - 0.06, side * 0.42 + 0.06, 1.2, 1.85, IRON, "strap")
cone((3.02, 0, 1.68), 0.08, 0.5, IRON, sides=6, axis="X")                                    # a spike on the nose
tilt(cone((-0.3, 0, 2.2), 0.1, 0.7, IRON, sides=6, axis="X"), y=-90 - 25)                    # and one off the back of the skull
cyl((0.2, 0, 2.6), 0.14, 0.2, "Z", IRON, sides=8, name="blowhole")
# Front beam with its knob.
box(2.2, 2.5, -0.5, 0.5, 0.4, 0.66, IRON, "beam")
ball((2.62, 0, 0.53), 0.13, BRONZE, "knob")
picture(finish("Locomotive_Beast"), "loco_beast")

# ====================== the spider ======================
# A small violet tank engine that grew legs: a pale mask with red eyes and a yellow grin on a neck out of the
# smokebox, a white abdomen swelling behind the cab, eight hip sockets down its flanks. The legs are not part
# of this model: the game builds them from the two bones below and walks them.
VIOLET = mat("SpiderViolet", (0.42, 0.16, 0.7))
VIOLET2 = mat("SpiderVioletDark", (0.24, 0.08, 0.44))
BONEW = mat("SpiderBone", (0.92, 0.9, 0.82))
SPBLACK = mat("SpiderBlack", (0.07, 0.07, 0.09), 0.45, 0.4)
SPRED = mat("SpiderRed", (0.8, 0.1, 0.12))
GRIN = mat("SpiderGrin", (1.0, 0.85, 0.1))
SPEYE = mat("GlowSpiderEye", (1.0, 0.08, 0.08), emit=1.0)
HIPS = [(x, side * 0.72, 1.3) for x in (1.5, 0.5, -0.5, -1.5) for side in (1, -1)]

box(-2.0, 2.3, -0.5, 0.5, 0.45, 0.7, SPBLACK, "frame")
box(-2.0, 2.5, -0.72, 0.72, 0.7, 0.8, SPRED, "running_board")
box(2.4, 2.6, -0.72, 0.72, 0.45, 0.8, SPRED, "buffer_beam")
both(lambda y0, y1: cyl((2.66, (y0 + y1) * 0.5, 0.62), 0.09, 0.12, "X", SPBLACK, sides=8, name="buffer"), 0.35, 0.55)
for x in (-1.2, -0.2, 0.8):
    both(lambda y0, y1: wheel(x, (y0 + y1) * 0.5, 0.42, VIOLET2, VIOLET), 0.52, 0.64)
both(lambda y0, y1: box(-1.3, 0.9, y0, y1, 0.36, 0.44, SPBLACK, "rod"), 0.66, 0.7)
cyl((0.7, 0, 1.35), 0.52, 2.4, "X", VIOLET, sides=16, name="boiler")
cyl((2.0, 0, 1.35), 0.55, 0.5, "X", SPBLACK, sides=16, name="smokebox")
for x in (0.2, 1.0):
    cyl((x, 0, 1.35), 0.54, 0.07, "X", GRIN, sides=16, name="band")
both(lambda y0, y1: box(-0.6, 1.4, y0, y1, 0.8, 1.5, VIOLET, "side_tank"), 0.45, 0.7)
cyl((1.9, 0, 2.05), 0.12, 0.5, "Z", SPBLACK, sides=10, name="funnel")
cyl((1.9, 0, 2.3), 0.16, 0.06, "Z", SPBLACK, sides=10, name="funnel_cap")
ball((0.6, 0, 1.88), 0.22, GRIN, "dome")
box(-1.8, -0.6, -0.7, 0.7, 0.8, 2.1, VIOLET, "cab")
box(-1.9, -0.5, -0.76, 0.76, 2.1, 2.22, SPBLACK, "cab_roof")
for side in (-1, 1):
    cyl((-1.2, side * 0.7, 1.7), 0.22, 0.06, "Y", GRIN, sides=12, name="window_ring")
    cyl((-1.2, side * 0.72, 1.7), 0.16, 0.06, "Y", SPBLACK, sides=12, name="window")
    cyl((-0.57, side * 0.35, 1.75), 0.14, 0.06, "X", GRIN, sides=10, name="cab_eye_ring")
    cyl((-0.55, side * 0.35, 1.75), 0.09, 0.06, "X", SPBLACK, sides=10, name="cab_eye")
ball((-2.2, 0, 1.5), 0.78, BONEW, "abdomen", (1.0, 1.0, 0.95))
for at in HIPS:
    ball(at, 0.17, BONEW, "hip")
# The face: a mask on a neck, a ruff of white round it.
cyl((2.45, 0, 1.55), 0.15, 0.6, "X", BONEW, sides=8, name="neck")
for k in range(6):
    ball((2.3, 0.26 * math.cos(k * math.tau / 6), 1.5 + 0.26 * math.sin(k * math.tau / 6)), 0.17, BONEW, "ruff")
ball((2.85, 0, 1.75), 0.46, BONEW, "mask", (0.35, 1.0, 1.1))
for side in (-1, 1):
    ball((2.95, side * 0.17, 1.9), 0.15, SPRED, "eye_ring", (0.4, 1, 1))
    ball((2.98, side * 0.17, 1.9), 0.1, SPEYE, "eye", (0.5, 1, 1))
for k in range(-3, 4):                               # the grin: a row of yellow teeth, its ends turned up
    z = 1.5 + 0.1 * (k / 3.0) ** 2
    box(2.96, 3.02, k * 0.07 - 0.03, k * 0.07 + 0.03, z - 0.06, z + 0.06, GRIN, "grin")
spider = finish("Locomotive_Spider")

# The two leg bones: each one unit long, from x = 0 to x = 1, so the game can stretch it to the length it needs
# (their knobs are squashed along x to come out round once stretched).
cyl((0.5, 0, 0), 0.1, 1.0, "X", BONEW, sides=8, name="thigh")
for x, r in ((0.0, 0.17), (0.5, 0.13), (1.0, 0.16)):
    ball((x, 0, 0), r, BONEW, "knuckle", (0.5, 1, 1))
finish("SpiderLeg_Upper")
cyl((0.5, 0, 0), 0.1, 1.0, "X", BONEW, sides=8, name="shin", top=0.02)
for x, r in ((0.0, 0.16), (0.55, 0.11), (0.78, 0.08)):
    ball((x, 0, 0), r, BONEW, "knuckle", (0.5, 1, 1))
finish("SpiderLeg_Lower")

# For the picture only: the legs standing as the game stands them, joined to the engine.
def limb(a, b, radius):
    a, b = Vector(a), Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=(b - a).length, location=(a + b) / 2)
    ob = bpy.context.object
    ob.rotation_euler = (b - a).to_track_quat("Z", "Y").to_euler()
    ob.data.materials.append(BONEW)
    parts.append(ob)

for k, hip in enumerate(HIPS):
    hip = Vector(hip)
    foot = Vector(((2.7, 1.05, -0.75, -2.4)[k // 2], (1.75, 2.15, 2.15, 1.8)[k // 2] * (1 if hip.y > 0 else -1), 0))
    reach = foot - hip
    near = (1.75 ** 2 - 2.1 ** 2 + reach.length ** 2) / (2 * reach.length)
    up = Vector((0, 0, 1)) - reach.normalized() * reach.normalized().z
    knee = hip + reach.normalized() * near + up.normalized() * math.sqrt(1.75 ** 2 - near ** 2)
    limb(hip, knee, 0.11); limb(knee, foot, 0.08); ball(knee, 0.17, BONEW, "knee")
import tempfile
legs = finish("SpiderPortraitLegs", folder=tempfile.gettempdir())
bpy.ops.object.select_all(action="DESELECT")
spider.select_set(True); legs.select_set(True)
bpy.context.view_layer.objects.active = spider
bpy.ops.object.join()
picture(spider, "loco_spider")

# ====================== the crocheted one ======================
# The first diesel (Tools/make_loco.py: long hood, cab with a leaning windscreen, short nose, pilot), in greens,
# made of wool: every big piece is rebuilt as a fine skin, its corners softened and rows of stitches raised
# on it; windows and doors are felt patches sewn on, the wheels are buttons, a pompom sits on the roof.
# Built in that script's design units and shrunk to the game's size like it.
SHRINK = 5.0 / 8.4
WOOL = mat("WoolGreen", (0.2, 0.6, 0.25), 0.9)
WOOL2 = mat("WoolDark", (0.07, 0.34, 0.16), 0.9)
LIME = mat("WoolLime", (0.66, 0.84, 0.24), 0.9)
DEEP = mat("WoolDeep", (0.04, 0.15, 0.09), 0.9)
CREAM = mat("WoolCream", (0.95, 0.92, 0.78), 0.9)
WLAMP = mat("GlowWoolLamp", (1.0, 0.93, 0.6), emit=1.0)

box(-4.0, 4.0, -1.22, 1.22, 0.62, 0.9, DEEP, "frame")
box(-1.1, 1.1, -0.95, 0.95, 0.18, 0.62, DEEP, "tank")
for bx in (-2.5, 2.3):
    box(bx - 1.25, bx + 1.25, -1.1, 1.1, 0.22, 0.62, DEEP, "bogie")
    for ax in (-0.8, 0.0, 0.8):
        for side in (-1, 1):                         # a button for a wheel, its four holes
            cyl((bx + ax, side * 1.2, 0.4), 0.4, 0.3, "Y", CREAM, sides=14, name="button")
            for dx, dz in ((-0.12, -0.12), (0.12, -0.12), (-0.12, 0.12), (0.12, 0.12)):
                cyl((bx + ax + dx, side * 1.34, 0.4 + dz), 0.055, 0.06, "Y", DEEP, sides=6, name="button_hole")
box(-3.9, 0.6, -1.12, 1.12, 0.86, 1.3, WOOL2, "hood_base")
box(-3.9, 0.6, -1.18, 1.18, 1.3, 1.56, LIME, "hood_stripe")
box(-3.9, 0.6, -1.12, 1.12, 1.5, 2.95, WOOL, "hood")
mesh("hood_roof", [(-3.9, -1.12, 2.95), (0.6, -1.12, 2.95), (0.6, 1.12, 2.95), (-3.9, 1.12, 2.95),
                   (-3.9, -0.75, 3.25), (0.6, -0.75, 3.25), (0.6, 0.75, 3.25), (-3.9, 0.75, 3.25)],
     [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (1, 2, 6, 5), (3, 0, 4, 7)], WOOL2)
for vx in (-3.3, -2.3, -1.3):
    box(vx - 0.38, vx + 0.38, -0.55, 0.55, 3.25, 3.4, DEEP, "vent")
ball((-0.3, 0, 3.6), 0.4, LIME, "pompom")
def patches(y0, y1):
    for i in range(5):
        box(-3.7 + i * 0.84, -3.0 + i * 0.84, y0, y1, 1.68, 2.18, WOOL2, "door")
    for i in range(3):
        box(-1.9 + i * 0.8, -1.28 + i * 0.8, y0, y1, 2.35, 2.8, CREAM, "window")
both(patches, 1.1, 1.2)
prism([(0.6, 0.86), (2.25, 0.86), (2.25, 2.25), (1.9, 3.5), (0.6, 3.5)], -1.25, 1.25, WOOL, "cab")
box(0.45, 2.05, -1.32, 1.32, 3.5, 3.75, WOOL2, "cab_roof")
both(lambda y0, y1: prism([(0.6, 0.86), (2.25, 0.86), (2.25, 2.0)], y0, y1, LIME, "cab_wedge"), 1.22, 1.31)
both(lambda y0, y1: box(0.85, 1.75, y0, y1, 2.45, 3.2, CREAM, "cab_window"), 1.22, 1.32)
# Windscreen: two cream panes sewn onto the cab's leaning face, a dark hem round each.
def leaning(z, out):
    t = (z - 2.25) / (3.5 - 2.25)
    return 2.25 + (1.9 - 2.25) * t + out * 0.963, z + out * 0.27
def pane(y0, y1, z0, z1, out, m, name):
    (xa, za), (xb, zb), (xc, zc), (xd, zd) = leaning(z0, out), leaning(z1, out), leaning(z0, -0.05), leaning(z1, -0.05)
    mesh(name, [(xa, y0, za), (xa, y1, za), (xb, y1, zb), (xb, y0, zb), (xc, y0, zc), (xc, y1, zc), (xd, y1, zd), (xd, y0, zd)],
         [(0, 1, 2, 3), (4, 0, 3, 7), (1, 5, 6, 2), (3, 2, 6, 7), (4, 5, 1, 0)], m)
for y0, y1 in ((-1.12, -0.07), (0.07, 1.12)):
    pane(y0, y1, 2.42, 3.36, 0.04, WOOL2, "screen_hem")
    pane(y0 + 0.09, y1 - 0.09, 2.51, 3.27, 0.08, CREAM, "screen")
box(1.5, 2.0, -0.32, 0.32, 3.75, 4.15, WOOL2, "lamp_box")
cyl((2.02, 0, 3.95), 0.19, 0.08, "X", WLAMP, name="lamp")
box(2.25, 3.45, -1.0, 1.0, 0.86, 2.1, LIME, "nose")
box(2.2, 3.5, -1.04, 1.04, 2.1, 2.32, WOOL, "nose_top")
box(3.45, 3.6, -0.85, 0.85, 0.95, 1.5, WOOL2, "board")
box(3.45, 3.6, -0.45, 0.45, 1.5, 2.02, WOOL2, "board_top")
for y, z in ((-0.62, 1.22), (0.62, 1.22), (0.0, 1.62), (0.0, 1.87)):
    cyl((3.62, y, z), 0.16, 0.08, "X", CREAM, name="light_button")
    cyl((3.66, y, z), 0.09, 0.06, "X", WLAMP, name="light")
prism([(3.5, 0.12), (4.35, 0.12), (4.15, 0.95), (3.5, 0.95)], -1.25, 1.25, DEEP, "pilot")
both(lambda y0, y1: prism([(3.9, 0.12), (4.6, 0.12), (4.3, 0.95), (3.9, 0.95)], y0, y1, DEEP, "pilot_wing"), 0.55, 1.25)
box(3.5, 4.15, -1.22, 1.22, 0.86, 1.02, WOOL2, "deck")

STITCH = 0.16
for ob in list(parts):
    ob.location = Vector(ob.location) * SHRINK
    ob.scale = Vector(ob.scale) * SHRINK
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if min(ob.dimensions) >= 0.12:                   # thick enough to knit: a fine skin, soft corners, stitches
        knit = ob.modifiers.new("knit", "REMESH")
        knit.mode, knit.voxel_size = "VOXEL", 0.07
        soft = ob.modifiers.new("soft", "SMOOTH")
        soft.factor, soft.iterations = 0.5, 10
        bpy.ops.object.modifier_apply(modifier="knit")
        bpy.ops.object.modifier_apply(modifier="soft")
        ob.data.update()
        for v in ob.data.vertices:
            n, (x, y, z) = v.normal.copy(), v.co
            # Rows run round the piece; on a top they run across it. Each row is half a stitch off the last.
            a, b = (x, y) if abs(n.z) > 0.7 else (z, x if abs(n.y) > abs(n.x) else y)
            v.co += n * 0.035 * abs(math.sin(math.pi * a / STITCH)) * abs(math.sin(math.pi * (b / STITCH + 0.5 * (math.floor(a / STITCH) % 2))))
    for face in ob.data.polygons:
        face.use_smooth = True
picture(finish("Locomotive_Crochet"), "loco_crochet")

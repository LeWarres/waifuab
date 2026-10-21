# More of the city than the Mega City pack left us (Tools/bake_scenery.cs): towers, flats, shops, a kiosk, parked
# cars, a hydrant, road barriers, a street planter. One low-poly model each, named city__<name>, into
# Assets/Scenery/Props; Tools/bake_city_props.cs turns them into game scenery under Resources/Scenery/city.
# Built along x (the long side), standing on z = 0, centred. Materials named Glow... come out bright enough to bloom.
#   blender -b --python Tools/make_city_props.py           (-- shot: also Tools/city_props_preview.png)
import math, os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

OUT = os.path.join(ROOT, "Assets", "Scenery", "Props")
os.makedirs(OUT, exist_ok=True)

M = dict(
    glass=("CityGlass", (0.35, 0.6, 0.85)), glass2=("CityGlassTeal", (0.3, 0.72, 0.75)), lit=("GlowWindow", (1.0, 0.85, 0.5)),
    frame=("CityFrame", (0.82, 0.84, 0.88)), concrete=("CityConcrete", (0.62, 0.62, 0.66)), dark=("CityDark", (0.16, 0.17, 0.2)),
    brick=("CityBrick", (0.72, 0.38, 0.28)), cream=("CityCream", (0.93, 0.88, 0.76)), roof=("CityRoof", (0.3, 0.31, 0.35)),
    red=("CityRed", (0.85, 0.15, 0.12)), white=("CityWhite", (0.94, 0.94, 0.96)), yellow=("CityYellow", (1.0, 0.8, 0.15)),
    green=("CityLeaf", (0.3, 0.62, 0.28)), wood=("CityWood", (0.5, 0.34, 0.2)), steel=("CitySteel", (0.6, 0.62, 0.68)),
    tyre=("CityTyre", (0.08, 0.08, 0.09)), sign=("GlowSign", (1.0, 0.3, 0.6)), sign2=("GlowSignCyan", (0.2, 0.9, 1.0)),
    car1=("CarRed", (0.8, 0.12, 0.14)), car2=("CarBlue", (0.18, 0.35, 0.75)), car3=("CarMint", (0.45, 0.8, 0.65)),
    stripe=("CityAwning", (0.95, 0.35, 0.3)), orange=("CityOrange", (1.0, 0.45, 0.1)),
)
def m(key):
    name, rgb = M[key]
    found = bpy.data.materials.get(name)
    return found or mat(name, rgb, emit=3.0 if name.startswith("Glow") else 0.0)

def windows(x0, x1, z0, z1, y, cols, rows, key, inset=0.08, lit_share=0.14, facing=-1):
    # a grid of window panes on the face at y (facing -1: towards -y, 1: towards +y), a few lit
    w, h = (x1 - x0) / cols, (z1 - z0) / rows
    for i in range(cols):
        for j in range(rows):
            k = "lit" if (i * 7 + j * 3) % 11 < lit_share * 11 else key
            a0, a1 = x0 + i * w + inset, x0 + (i + 1) * w - inset
            b0, b1 = z0 + j * h + inset * 1.4, z0 + (j + 1) * h - inset
            box(a0, a1, y - 0.03 if facing < 0 else y, y if facing < 0 else y + 0.03, b0, b1, m(k))

def side_windows(y0, y1, z0, z1, x, cols, rows, key, inset=0.08, facing=-1):
    w, h = (y1 - y0) / cols, (z1 - z0) / rows
    for i in range(cols):
        for j in range(rows):
            k = "lit" if (i * 5 + j * 3) % 11 < 1.5 else key
            a0, a1 = y0 + i * w + inset, y0 + (i + 1) * w - inset
            b0, b1 = z0 + j * h + inset * 1.4, z0 + (j + 1) * h - inset
            box(x - 0.03 if facing < 0 else x, x if facing < 0 else x + 0.03, a0, a1, b0, b1, m(k))

def all_round(lx, ly, z0, z1, rows, key, colsx, colsy):
    for f in (-1, 1):
        windows(-lx, lx, z0, z1, f * ly, colsx, rows, key, facing=f)
        side_windows(-ly, ly, z0, z1, f * lx, colsy, rows, key, facing=f)

def tower():  # an office tower in two steps, glass all round, a crown and an antenna
    box(-4, 4, -3, 3, 0, 1.2, m("dark"))                                    # lobby
    box(-3.6, 3.6, -2.7, 2.7, 1.2, 13, m("frame"))
    all_round(3.6, 2.7, 1.4, 12.8, 9, "glass", 6, 4)
    box(-2.6, 2.6, -2, 2, 13, 17, m("frame"))
    all_round(2.6, 2.0, 13.2, 16.8, 3, "glass", 4, 3)
    box(-2.8, 2.8, -2.2, 2.2, 17, 17.4, m("roof"))
    box(-1, 0.4, -1, 0.5, 17.4, 18.2, m("concrete"))                       # plant room
    cyl((1.4, 0.8, 17.9), 0.4, 1.0, "Z", m("steel"), sides=10)
    cyl((0, 0, 19.6), 0.06, 3.6, "Z", m("steel"), sides=6)
    ball((0, 0, 21.4), 0.18, m("red"))

def tower2():  # a slim teal tower with a slanted top
    box(-2.6, 2.6, -2.6, 2.6, 0, 14, m("concrete"))
    all_round(2.6, 2.6, 0.6, 13.6, 10, "glass2", 4, 4)
    prism([(-2.6, 14), (2.6, 14), (2.6, 15.4), (-2.6, 17.2)], -2.6, 2.6, m("roof"))
    for k in range(4):
        box(-2.7, 2.7, -2.7, 2.7, 3.4 * k + 0.3, 3.4 * k + 0.5, m("frame"))

def flats():  # a brick block of flats with balconies and a water tank on the roof
    box(-5, 5, -2.5, 2.5, 0, 9, m("brick"))
    windows(-5, 5, 0.8, 8.6, -2.5, 7, 4, "glass", inset=0.3)
    windows(-5, 5, 0.8, 8.6, 2.5, 7, 4, "glass", inset=0.3, facing=1)
    side_windows(-2.5, 2.5, 0.8, 8.6, -5, 3, 4, "glass", inset=0.3)
    side_windows(-2.5, 2.5, 0.8, 8.6, 5, 3, 4, "glass", inset=0.3, facing=1)
    for j in range(1, 4):
        for i in range(7):
            x = -5 + (i + 0.5) * 10 / 7
            box(x - 0.55, x + 0.55, -2.9, -2.5, 0.8 + j * 1.95, 0.9 + j * 1.95, m("cream"))   # balcony floor
            box(x - 0.55, x + 0.55, -2.93, -2.88, 0.9 + j * 1.95, 1.35 + j * 1.95, m("dark"))  # its rail
    box(-5.2, 5.2, -2.7, 2.7, 9, 9.3, m("cream"))
    box(-0.9, 0.9, -1.2, 0.8, 0, 1.6, m("dark"))                            # entrance
    for x in (-3, 3):
        for y in (-1, 1):
            cyl((x + 0.4 * (y > 0), y * 0.6, 10.2), 0.07, 1.8, "Z", m("wood"), sides=5)
    cyl((3.2, 0, 10.8), 0.9, 1.4, "Z", m("wood"), sides=12)                 # water tank
    cyl((3.2, 0, 11.6), 0.95, 0.4, "Z", m("roof"), sides=12, top=0.1)

def shop(awning, sign):  # a low shop: big windows, a striped awning, a glowing sign
    box(-3.5, 3.5, -2, 2, 0, 3.6, m("cream"))
    box(-3.6, 3.6, -2.1, 2.1, 3.6, 3.9, m("roof"))
    box(-3.1, 3.1, -2.04, -2.0, 0.3, 2.2, m("glass"))
    box(-0.6, 0.6, -2.06, -2.0, 0.0, 2.1, m("dark"))                        # door
    for k in range(7):                                                      # striped awning, sloping out
        x = -3.5 + k
        prism([(x, 2.3), (x + 1, 2.3), (x + 1, 2.9), (x, 2.9)], -3.0, -2.0, m(awning if k % 2 == 0 else "white"))
    box(-2.4, 2.4, -2.12, -2.04, 2.95, 3.5, m(sign))                        # sign
    box(2.2, 3.2, -1.5, -0.5, 3.9, 4.5, m("steel"))                         # air conditioner

def kiosk():  # a newspaper kiosk
    box(-1.2, 1.2, -0.9, 0.9, 0, 2.2, m("car3"))
    box(-1.0, 1.0, -0.92, -0.9, 0.9, 1.9, m("glass"))
    prism([(-1.4, 2.2), (1.4, 2.2), (0, 2.7)], -1.1, 1.1, m("stripe"))
    for k in range(3):
        box(-0.9 + k * 0.6, -0.5 + k * 0.6, -1.05, -0.9, 0.5, 0.85, m(("yellow", "white", "red")[k]))

def car(paint):  # a parked city car
    box(-1.9, 1.9, -0.85, 0.85, 0.25, 0.85, m(paint))
    prism([(-1.0, 0.85), (1.2, 0.85), (0.8, 1.4), (-0.8, 1.4)], -0.75, 0.75, m(paint))
    box(-0.7, 0.9, -0.77, -0.74, 0.92, 1.32, m("glass"))
    box(-0.7, 0.9, 0.74, 0.77, 0.92, 1.32, m("glass"))
    for x in (-1.25, 1.25):
        for y in (-0.82, 0.82):
            cyl((x, y, 0.3), 0.3, 0.2, "Y", m("tyre"), sides=10)
    box(1.88, 1.92, -0.7, -0.4, 0.55, 0.7, m("yellow"))                     # lights
    box(1.88, 1.92, 0.4, 0.7, 0.55, 0.7, m("yellow"))
    box(-1.92, -1.88, -0.7, -0.4, 0.55, 0.7, m("red"))
    box(-1.92, -1.88, 0.4, 0.7, 0.55, 0.7, m("red"))

def hydrant():
    cyl((0, 0, 0.3), 0.14, 0.6, "Z", m("red"), sides=10)
    ball((0, 0, 0.62), 0.14, m("red"))
    cyl((0, 0, 0.04), 0.2, 0.08, "Z", m("red"), sides=10)
    cyl((0, 0, 0.42), 0.06, 0.4, "Y", m("steel"), sides=8)
    cyl((0, 0, 0.78), 0.04, 0.08, "Z", m("steel"), sides=6)

def barrier():  # a striped road barrier with two warning lights
    for x in (-1.1, 1.1):
        box(x - 0.06, x + 0.06, -0.3, 0.3, 0, 0.08, m("dark"))
        box(x - 0.04, x + 0.04, -0.04, 0.04, 0, 1.0, m("white"))
        cyl((x, 0, 1.08), 0.08, 0.12, "Z", m("orange"), sides=8)
    for k in range(6):
        x = -1.2 + k * 0.4
        box(x, x + 0.4, -0.05, 0.05, 0.6, 0.9, m("red" if k % 2 == 0 else "white"))
    for x in (1.7, 2.1, 2.5):                                               # traffic cones beside it
        cyl((x, 0.2, 0.3), 0.18, 0.6, "Z", m("orange"), sides=8, top=0.03)
        box(x - 0.22, x + 0.22, 0.0, 0.4, 0, 0.04, m("orange"))

def planter():  # a street tree in a box
    box(-0.8, 0.8, -0.8, 0.8, 0, 0.6, m("concrete"))
    box(-0.7, 0.7, -0.7, 0.7, 0.6, 0.62, m("wood"))
    cyl((0, 0, 1.3), 0.1, 1.4, "Z", m("wood"), sides=6)
    ball((0, 0, 2.4), 0.9, m("green"), scale=(1, 1, 0.85))
    ball((0.35, 0.2, 2.9), 0.55, m("green"))

BUILDS = [("tower", tower), ("tower2", tower2), ("flats", flats), ("shop_red", lambda: shop("stripe", "sign")),
          ("shop_blue", lambda: shop("car2", "sign2")), ("kiosk", kiosk), ("car_red", lambda: car("car1")),
          ("car_blue", lambda: car("car2")), ("car_mint", lambda: car("car3")), ("hydrant", hydrant),
          ("barrier", barrier), ("planter", planter)]
for name, build in BUILDS:
    reset()
    build()
    finish("city__" + name, OUT)

if "shot" in sys.argv:
    reset()
    for i, (name, _) in enumerate(BUILDS):
        bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "city__" + name + ".fbx"))
        for ob in bpy.context.selected_objects:
            ob.location = ((i % 6) * 11 - 27, (i // 6) * 12, 0)
    render([("city_props_preview", (0, -60, 40), (0, 6, 3))], lens=35)

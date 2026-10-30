"""The city race's sky: blue hour in Persona 3's blues, painted by hand (no ComfyUI), for RaceEvent's sky shell.

Deep navy overhead fading to a pale blue haze at the horizon (the bottom edge), a scatter of stars that thins out
lower down, a big pale moon with a soft halo, and a few long thin clouds lit from below. The game wraps it mirrored
round the shell, so its ends need not match. Saved to Assets/Resources/Sky/race.png.

    python Tools/make_race_sky.py          (the city race: Sky/race.png)
    python Tools/make_race_sky.py trench   (the trench run: open space over the base, Sky/trench.png)
"""
import os, random, sys
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 1536, 768
STORM = "trench" in sys.argv[1:]  # the trench: deep space, no moon
TOP, MID, LOW = ((1, 2, 8), (4, 8, 24), (14, 26, 60)) if STORM else ((6, 12, 40), (18, 48, 120), (110, 160, 220))  # overhead, two thirds down, the horizon

def lerp(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))

sky = Image.new("RGB", (W, H))
px = sky.load()
for y in range(H):
    t = y / (H - 1)
    c = lerp(TOP, MID, t / 0.65) if t < 0.65 else lerp(MID, LOW, ((t - 0.65) / 0.35) ** 1.4)
    for x in range(W):
        px[x, y] = c

rng = random.Random(3)
stars = Image.new("L", (W, H))
d = ImageDraw.Draw(stars)
for _ in range(900):
    x, y = rng.randrange(W), int(H * rng.random() ** 1.8 * 0.75)  # most of them high up
    b = rng.randint(90, 255) * (1 - y / (H * 0.8))
    r = 1 if rng.random() < 0.9 else 2
    d.ellipse((x - r / 2, y - r / 2, x + r / 2, y + r / 2), fill=int(b))
sky.paste((235, 242, 255), mask=stars)

# The moon and its halo, up and to one side (hidden by the storm).
mx, my, mr = int(W * 0.7), int(H * 0.28), 0 if STORM else 70
halo = Image.new("L", (W, H))
if not STORM: ImageDraw.Draw(halo).ellipse((mx - mr * 3, my - mr * 3, mx + mr * 3, my + mr * 3), fill=70)
halo = halo.filter(ImageFilter.GaussianBlur(70))
sky.paste((150, 190, 255), mask=halo)
moon = Image.new("L", (W, H))
md = ImageDraw.Draw(moon)
md.ellipse((mx - mr, my - mr, mx + mr, my + mr), fill=255)
moon = moon.filter(ImageFilter.GaussianBlur(1.5))
if not STORM: sky.paste((236, 242, 255), mask=moon)
for _ in range(0 if STORM else 9):  # a few faint maria
    cx, cy, cr = mx + rng.randint(-40, 40), my + rng.randint(-40, 40), rng.randint(8, 20)
    if (cx - mx) ** 2 + (cy - my) ** 2 < (mr - cr) ** 2:
        spot = Image.new("L", (W, H))
        ImageDraw.Draw(spot).ellipse((cx - cr, cy - cr, cx + cr, cy + cr), fill=60)
        sky.paste((180, 196, 228), mask=spot.filter(ImageFilter.GaussianBlur(4)))

# Long thin clouds low down, dark undersides lit by the city.
clouds = Image.new("L", (W, H))
cd = ImageDraw.Draw(clouds)
for _ in range(0 if STORM else 14):  # none in space
    y = int(H * rng.uniform(0.45, 0.85))
    x = rng.randrange(-200, W)
    cd.ellipse((x, y - rng.randint(6, 14), x + rng.randint(260, 620), y + rng.randint(6, 14)), fill=rng.randint(60, 120))
clouds = clouds.filter(ImageFilter.GaussianBlur(9))
sky.paste((60, 90, 160), mask=clouds)

if STORM:
    # Space seen up out of the trench: the Milky Way as a band of haze and dense stars across it, blue nebulae, a
    # far ringed planet, and stars down to the horizon (no air to hide them), some big with a cross of light.
    band = Image.new("L", (W, H))
    bd = ImageDraw.Draw(band)
    for k in range(120):  # along a diagonal, lumpy
        u = k / 119
        x, y = int(-200 + u * (W + 400)), int(H * (0.15 + 0.45 * u)) + rng.randint(-40, 40)
        r = rng.randint(50, 120)
        bd.ellipse((x - r * 2, y - r, x + r * 2, y + r), fill=rng.randint(30, 70))
    band = band.filter(ImageFilter.GaussianBlur(40))
    sky.paste((70, 100, 170), mask=band)
    for _ in range(5):  # nebulae, blue to violet
        neb = Image.new("L", (W, H))
        x, y, r = rng.randrange(W), int(H * rng.uniform(0.1, 0.6)), rng.randint(90, 180)
        ImageDraw.Draw(neb).ellipse((x - r * 2, y - r, x + r * 2, y + r), fill=rng.randint(50, 90))
        sky.paste(rng.choice([(40, 110, 220), (90, 70, 200), (30, 160, 220)]), mask=neb.filter(ImageFilter.GaussianBlur(60)))
    dense = Image.new("L", (W, H))
    dd = ImageDraw.Draw(dense)
    for _ in range(4500):  # many more stars, thickest along the band
        x = rng.randrange(W)
        y = int(H * (0.15 + 0.45 * (x + 200) / (W + 400))) + int(rng.gauss(0, 90)) if rng.random() < 0.6 else rng.randrange(H)
        if 0 <= y < H:
            dd.point((x, y), fill=rng.randint(80, 255))
    sky.paste((225, 235, 255), mask=dense)
    for _ in range(40):  # the bright ones, each with a soft glow and a cross of light
        x, y = rng.randrange(W), rng.randrange(int(H * 0.9))
        glow = Image.new("L", (W, H))
        gd = ImageDraw.Draw(glow)
        gd.ellipse((x - 6, y - 6, x + 6, y + 6), fill=90)
        sky.paste((180, 210, 255), mask=glow.filter(ImageFilter.GaussianBlur(4)))
        cross = Image.new("L", (W, H))
        cd2 = ImageDraw.Draw(cross)
        cd2.line((x - 9, y, x + 9, y), fill=200)
        cd2.line((x, y - 9, x, y + 9), fill=200)
        cd2.ellipse((x - 1.5, y - 1.5, x + 1.5, y + 1.5), fill=255)
        sky.paste((240, 246, 255), mask=cross.filter(ImageFilter.GaussianBlur(0.6)))
    # A far ringed planet, low on one side, lit from the left: its ring drawn behind it, then the planet, then the
    # ring's near half in front.
    px_, py_, pr = int(W * 0.18), int(H * 0.42), 56
    ring = Image.new("L", (W, H))
    ImageDraw.Draw(ring).ellipse((px_ - pr * 2.1, py_ - pr * 0.45, px_ + pr * 2.1, py_ + pr * 0.45), outline=200, width=5)
    ring = ring.filter(ImageFilter.GaussianBlur(1))
    sky.paste((170, 200, 250), mask=ring)
    planet = Image.new("L", (W, H))
    ImageDraw.Draw(planet).ellipse((px_ - pr, py_ - pr, px_ + pr, py_ + pr), fill=255)
    sky.paste((50, 80, 150), mask=planet)
    lit = Image.new("L", (W, H))
    ImageDraw.Draw(lit).ellipse((px_ - pr, py_ - pr, px_ + pr * 0.4, py_ + pr), fill=255)
    lit = Image.composite(lit, Image.new("L", (W, H)), planet)
    sky.paste((140, 180, 245), mask=lit.filter(ImageFilter.GaussianBlur(6)))
    near = Image.new("L", (W, H))
    ImageDraw.Draw(near).rectangle((0, py_, W, H), fill=255)
    sky.paste((170, 200, 250), mask=Image.composite(ring, Image.new("L", (W, H)), near))
out = os.path.join(ROOT, "Assets", "Resources", "Sky", "trench.png" if STORM else "race.png")
sky.save(out)
print("SKY", out, sky.size)

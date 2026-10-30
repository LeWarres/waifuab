"""The city race's sky: blue hour in Persona 3's blues, painted by hand (no ComfyUI), for RaceEvent's sky shell.

Deep navy overhead fading to a pale blue haze at the horizon (the bottom edge), a scatter of stars that thins out
lower down, a big pale moon with a soft halo, and a few long thin clouds lit from below. The game wraps it mirrored
round the shell, so its ends need not match. Saved to Assets/Resources/Sky/race.png.

    python Tools/make_race_sky.py          (the city race: Sky/race.png)
    python Tools/make_race_sky.py trench   (the trench run: the same blues under a storm, no moon: Sky/trench.png)
"""
import os, random, sys
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 1536, 768
STORM = "trench" in sys.argv[1:]
TOP, MID, LOW = ((4, 8, 26), (14, 30, 76), (60, 90, 150)) if STORM else ((6, 12, 40), (18, 48, 120), (110, 160, 220))  # overhead, two thirds down, the horizon

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
ImageDraw.Draw(halo).ellipse((mx - mr * 3, my - mr * 3, mx + mr * 3, my + mr * 3), fill=70)
halo = halo.filter(ImageFilter.GaussianBlur(70))
sky.paste((150, 190, 255), mask=halo)
moon = Image.new("L", (W, H))
md = ImageDraw.Draw(moon)
md.ellipse((mx - mr, my - mr, mx + mr, my + mr), fill=255)
moon = moon.filter(ImageFilter.GaussianBlur(1.5))
sky.paste((236, 242, 255), mask=moon)
for _ in range(9):  # a few faint maria
    cx, cy, cr = mx + rng.randint(-40, 40), my + rng.randint(-40, 40), rng.randint(8, 20)
    if (cx - mx) ** 2 + (cy - my) ** 2 < (mr - cr) ** 2:
        spot = Image.new("L", (W, H))
        ImageDraw.Draw(spot).ellipse((cx - cr, cy - cr, cx + cr, cy + cr), fill=60)
        sky.paste((180, 196, 228), mask=spot.filter(ImageFilter.GaussianBlur(4)))

# Long thin clouds low down, dark undersides lit by the city.
clouds = Image.new("L", (W, H))
cd = ImageDraw.Draw(clouds)
for _ in range(14):
    y = int(H * rng.uniform(0.45, 0.85))
    x = rng.randrange(-200, W)
    cd.ellipse((x, y - rng.randint(6, 14), x + rng.randint(260, 620), y + rng.randint(6, 14)), fill=rng.randint(60, 120))
clouds = clouds.filter(ImageFilter.GaussianBlur(9))
sky.paste((60, 90, 160), mask=clouds)

if STORM:
    # Heavy banks of cloud over most of it, lit from inside here and there by lightning.
    bank = Image.new("L", (W, H))
    bd = ImageDraw.Draw(bank)
    for _ in range(60):
        y, x = int(H * rng.uniform(0.0, 0.8)), rng.randrange(-300, W)
        bd.ellipse((x, y - rng.randint(20, 60), x + rng.randint(300, 800), y + rng.randint(20, 60)), fill=rng.randint(90, 200))
    sky.paste((22, 32, 62), mask=bank.filter(ImageFilter.GaussianBlur(22)))
    for _ in range(4):
        glow = Image.new("L", (W, H))
        x, y, r = rng.randrange(W), int(H * rng.uniform(0.15, 0.6)), rng.randint(70, 140)
        ImageDraw.Draw(glow).ellipse((x - r * 2, y - r, x + r * 2, y + r), fill=rng.randint(50, 90))
        sky.paste((150, 180, 255), mask=glow.filter(ImageFilter.GaussianBlur(50)))
out = os.path.join(ROOT, "Assets", "Resources", "Sky", "trench.png" if STORM else "race.png")
sky.save(out)
print("SKY", out, sky.size)

# Draws the alien planet's ground, Assets/Resources/Ground/alien.png: a dark indigo soil with a faint honeycomb of
# hexagons in it, a few glowing spores and small dark craters. Low contrast, so the creatures and the train stand
# out. It tiles (hexagons laid on a grid that repeats exactly).
#   python Tools/make_alien_ground.py
import math, os, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

random.seed(31)
SIZE = 1024
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground", "alien.png")
img = Image.new("RGB", (SIZE, SIZE), (44, 36, 74))
pen = ImageDraw.Draw(img)

def everywhere(draw):
    for dx in (-SIZE, 0, SIZE):
        for dy in (-SIZE, 0, SIZE):
            draw(dx, dy)

for _ in range(60):  # soft patches of lighter and darker soil
    x, y, r = random.randrange(SIZE), random.randrange(SIZE), random.randrange(60, 180)
    c = random.choice(((50, 42, 84), (40, 32, 66), (46, 40, 80)))
    everywhere(lambda dx, dy: pen.ellipse((x + dx - r, y + dy - r, x + dx + r, y + dy + r), fill=c))
img = img.filter(ImageFilter.GaussianBlur(30))
pen = ImageDraw.Draw(img)

# the honeycomb: flat-topped hexagons on a grid that divides the tile exactly
cols, rows = 8, 8
w = SIZE / cols                       # column spacing (1.5 * side)
side = w / 1.5
h = math.sqrt(3) * side
rows = round(SIZE / h); h = SIZE / rows
for cx in range(cols + 1):
    for cy in range(rows + 1):
        x0 = cx * w
        y0 = cy * h + (h / 2 if cx % 2 else 0)
        pts = [(x0 + side * math.cos(a), y0 + h / math.sqrt(3) * math.sin(a)) for a in (k * math.pi / 3 for k in range(7))]
        everywhere(lambda dx, dy: pen.line([(px + dx, py + dy) for px, py in pts], fill=(58, 50, 96), width=3))
for _ in range(30):  # small dark craters
    x, y, r = random.randrange(SIZE), random.randrange(SIZE), random.randrange(6, 16)
    everywhere(lambda dx, dy: pen.ellipse((x + dx - r, y + dy - r, x + dx + r, y + dy + r), fill=(32, 26, 54), outline=(60, 52, 96), width=2))
glow = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
gpen = ImageDraw.Draw(glow)
for _ in range(45):  # glowing spores
    x, y = random.randrange(SIZE), random.randrange(SIZE)
    c = random.choice(((60, 200, 170), (200, 80, 170), (150, 220, 80)))
    everywhere(lambda dx, dy: gpen.ellipse((x + dx - 3, y + dy - 3, x + dx + 3, y + dy + 3), fill=c))
glow = glow.filter(ImageFilter.GaussianBlur(2))
img = ImageChops.lighter(img, glow)
img.save(OUT, optimize=True)
print("alien drawn", OUT)

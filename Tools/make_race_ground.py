# Draws the city race's ground, Assets/Resources/Ground/race.png (and the trench run's deck, below): one 54 m repeat of a night plaza round the course
# (RaceEvent tiles it every 54 m). Dark cool concrete in 3 m slabs, each a shade of its own, between pale granite
# bands every 18 m that cross in lit squares, and a drain cover here and there. Low contrast, like the ride's ground,
# so the racers and the barriers stay what the eye goes to. It tiles: the bands meet at the edges.
#   python Tools/make_race_ground.py
import os, random
from PIL import Image, ImageChops, ImageDraw, ImageFilter

random.seed(23)
SIZE = 1024
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground", "race.png")
M = SIZE / 54.0                     # pixels per metre
SLAB, BAND = 3.0, 18.0              # slab and band spacing, in metres
CONCRETE, JOINT = (70, 78, 98), (52, 58, 76)
GRANITE, GRANITE_JOINT = (118, 126, 146), (96, 104, 124)
LAMP = (190, 214, 245)

img = Image.new("RGB", (SIZE, SIZE), CONCRETE)
pen = ImageDraw.Draw(img)

# The slabs, each a little lighter or darker, with a dark joint round it.
n = int(54 / SLAB)
for i in range(n):
    for j in range(n):
        x0, y0 = round(i * SLAB * M), round(j * SLAB * M)
        x1, y1 = round((i + 1) * SLAB * M), round((j + 1) * SLAB * M)
        k = random.randint(-6, 6)
        pen.rectangle((x0, y0, x1, y1), fill=tuple(c + k for c in CONCRETE), outline=JOINT, width=2)

# Granite bands every 18 m (both ways), a joint either side, lit squares where they cross.
w = round(1.2 * M)
for b in range(int(54 / BAND)):
    c = round(b * BAND * M)
    for lo, hi in ((c - w // 2, c + w // 2),):
        for (x0, x1, y0, y1) in ((lo, hi, 0, SIZE), (0, SIZE, lo, hi)):
            pen.rectangle((x0, y0, x1, y1), fill=GRANITE)
        for t in range(0, SIZE, round(1.2 * M)):  # the band's own small setts
            pen.line((lo, t, hi, t), fill=GRANITE_JOINT, width=1)
            pen.line((t, lo, t, hi), fill=GRANITE_JOINT, width=1)
        for e in (lo, hi):
            pen.line((e, 0, e, SIZE), fill=JOINT, width=2)
            pen.line((0, e, SIZE, e), fill=JOINT, width=2)
for bx in range(int(54 / BAND)):
    for by in range(int(54 / BAND)):
        cx, cy, r = round(bx * BAND * M), round(by * BAND * M), round(0.35 * M)
        for ox in (0, -SIZE, SIZE):  # wrapped round the edges so the tile meets itself
            for oy in (0, -SIZE, SIZE):
                pen.rectangle((cx + ox - r, cy + oy - r, cx + ox + r, cy + oy + r), fill=LAMP)

# A few drain covers on the slabs.
for _ in range(5):
    x, y = random.uniform(2, 52) * M, random.uniform(2, 52) * M
    r = 0.4 * M
    pen.ellipse((x - r, y - r, x + r, y + r), fill=(46, 52, 68), outline=(40, 45, 60))
    for k in range(-2, 3):
        pen.line((x - r * 0.7, y + k * r * 0.3, x + r * 0.7, y + k * r * 0.3), fill=(60, 66, 84), width=2)

# Grain, so the slabs read as concrete and not as flat paint.
grain = Image.effect_noise((SIZE, SIZE), 10)
img = ImageChops.add(img, Image.merge("RGB", [grain] * 3), scale=1.0, offset=-128).filter(ImageFilter.SMOOTH)
img.save(OUT)
print("GROUND", OUT, img.size)

# The trench run's deck, Assets/Resources/Ground/trench.png, laid on its road once across (44 m) and every 40 m along
# (RaceEvent): dark steel plates with seams and rivets, hazard stripes along both edges, a row of guide lights down
# each lane line.
D = 1024
T = D / 44.0                         # pixels per metre across
deck = Image.new("RGB", (D, D), (44, 50, 66))
dp = ImageDraw.Draw(deck)
plate = round(4 * T)
for i in range(0, D, plate):
    for j in range(0, D, plate):
        k = random.randint(-5, 5)
        dp.rectangle((i, j, i + plate, j + plate), fill=(44 + k, 50 + k, 66 + k), outline=(30, 34, 46), width=3)
        for (rx, ry) in ((i + 8, j + 8), (i + plate - 8, j + 8), (i + 8, j + plate - 8), (i + plate - 8, j + plate - 8)):
            dp.ellipse((rx - 3, ry - 3, rx + 3, ry + 3), fill=(70, 78, 96))
edge = round(1.6 * T)
for x0 in (0, D - edge):                                   # hazard stripes along both edges, black on a muted yellow
    dp.rectangle((x0, 0, x0 + edge, D), fill=(150, 128, 52))
    for y in range(-edge, D, edge):
        dp.polygon([(x0, y), (x0 + edge, y + edge // 2), (x0 + edge, y + edge), (x0, y + edge // 2)], fill=(28, 28, 34))
for lane in (0.25, 0.75):                                  # guide lights down the lane lines
    x = round(lane * D)
    for y in range(32, D, 128):
        dp.rectangle((x - 6, y - 14, x + 6, y + 14), fill=(170, 205, 245))
deck = ImageChops.add(deck, Image.merge("RGB", [Image.effect_noise((D, D), 8)] * 3), scale=1.0, offset=-128).filter(ImageFilter.SMOOTH)
out = os.path.join(os.path.dirname(OUT), "trench.png")
deck.save(out)
print("DECK", out, deck.size)

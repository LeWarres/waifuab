# Draws the city's ground, Assets/Resources/Ground/city.png: one block of a street grid per repeat (TrainSim.groundTile
# on the ground). Paved sidewalks round the edges, a crossroads in the middle with lane lines, kerbs, zebra
# crossings and manholes. It tiles: the sidewalks meet at the edges.
#   python Tools/make_city_ground.py
import os, random
from PIL import Image, ImageDraw, ImageFilter

random.seed(11)
SIZE = 1024
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground", "city.png")
M = SIZE / 54.0                       # pixels per metre (TrainSim.GroundTile: three times groundTile in the city)
ROAD = int(7.0 * M)                   # width of each street
KERB = int(0.25 * M)
C = SIZE // 2
lo, hi = C - ROAD // 2, C + ROAD // 2

# Low contrast on purpose: the ground rushes by under a fast train, and strong lines drowned out the enemies and the hero.
PAVE, PAVE2, JOINT = (168, 170, 178), (163, 165, 173), (156, 158, 166)
ASPHALT, PATCH, KERBC = (112, 115, 124), (107, 110, 119), (182, 184, 191)
WHITE, YELLOW = (146, 149, 158), (166, 150, 112)

img = Image.new("RGB", (SIZE, SIZE), PAVE)
pen = ImageDraw.Draw(img)

# sidewalk slabs, a metre each, a few a shade darker
step = int(M)
for x in range(0, SIZE, step):
    for y in range(0, SIZE, step):
        if random.random() < 0.18:
            pen.rectangle((x, y, x + step, y + step), fill=PAVE2)
for k in range(0, SIZE + 1, step):
    pen.line((k, 0, k, SIZE), fill=JOINT, width=2)
    pen.line((0, k, SIZE, k), fill=JOINT, width=2)

# the two streets, kerbs first
pen.rectangle((lo - KERB, 0, hi + KERB, SIZE), fill=KERBC)
pen.rectangle((0, lo - KERB, SIZE, hi + KERB), fill=KERBC)
pen.rectangle((lo, 0, hi, SIZE), fill=ASPHALT)
pen.rectangle((0, lo, SIZE, hi), fill=ASPHALT)
for _ in range(40):                   # worn patches in the asphalt
    w, h = random.randint(30, 120), random.randint(20, 80)
    if random.random() < 0.5:
        x, y = random.randint(lo, hi - w), random.randint(0, SIZE - h)
    else:
        x, y = random.randint(0, SIZE - w), random.randint(lo, hi - h)
    pen.ellipse((x, y, x + w, y + h), fill=PATCH)
img = img.filter(ImageFilter.GaussianBlur(0.8))
pen = ImageDraw.Draw(img)

# lane lines: a double yellow down the middle of each street, white dashes between the lanes, stopping at the crossroads
gap = int(0.12 * M)
cross = (lo - int(2.4 * M), hi + int(2.4 * M))  # the crossroads and its zebra crossings
def along(a0, a1, draw):
    draw(a0, min(a1, cross[0])) if a0 < cross[0] else None
    draw(max(a0, cross[1]), a1) if a1 > cross[1] else None
for off in (-gap, gap):
    along(0, SIZE, lambda a, b: pen.rectangle((C + off - 3, a, C + off + 3, b), fill=YELLOW))
    along(0, SIZE, lambda a, b: pen.rectangle((a, C + off - 3, b, C + off + 3), fill=YELLOW))
dash, space = int(1.6 * M), int(1.4 * M)
for lane in (C - ROAD // 4, C + ROAD // 4):
    for a in range(0, SIZE, dash + space):
        along(a, a + dash, lambda p, q: pen.rectangle((lane - 3, p, lane + 3, q), fill=WHITE))
        along(a, a + dash, lambda p, q: pen.rectangle((p, lane - 3, q, lane + 3), fill=WHITE))

# manholes and drains
for _ in range(4):
    if random.random() < 0.5:
        x, y = random.randint(lo + 40, hi - 40), random.choice((random.randint(40, cross[0] - 80), random.randint(cross[1] + 40, SIZE - 80)))
    else:
        y, x = random.randint(lo + 40, hi - 40), random.choice((random.randint(40, cross[0] - 80), random.randint(cross[1] + 40, SIZE - 80)))
    r = int(0.45 * M)
    pen.ellipse((x - r, y - r, x + r, y + r), fill=(100, 103, 112), outline=(122, 125, 134), width=3)
    for k in range(-2, 3):
        pen.line((x - r + 6, y + k * 5, x + r - 6, y + k * 5), fill=(110, 113, 122), width=2)

# small planters on the sidewalk corners
for cx in (lo - KERB - int(1.2 * M), hi + KERB + int(1.2 * M)):
    for cy in (int(2 * M), SIZE - int(2 * M)):
        pen.rectangle((cx - 22, cy - 22, cx + 22, cy + 22), fill=(150, 140, 126))
        pen.ellipse((cx - 18, cy - 18, cx + 18, cy + 18), fill=(120, 146, 112))

img.save(OUT, optimize=True)
print("city drawn", OUT)

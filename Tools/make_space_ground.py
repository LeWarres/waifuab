# Draws the space ground, Assets/Resources/Ground/space.png: deep black with far-off stars, a few brighter and a
# few tinted, and nothing else, so the train and the enemies stand out. It tiles (TrainSim.GroundTile: four times
# groundTile in space, so the stars do not repeat in sight).
#   python Tools/make_space_ground.py
import os, random
from PIL import Image, ImageChops, ImageDraw, ImageFilter

random.seed(21)
SIZE = 1024
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground", "space.png")
img = Image.new("RGB", (SIZE, SIZE), (5, 5, 10))
glow = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
pen, halo = ImageDraw.Draw(img), ImageDraw.Draw(glow)
TINTS = [(255, 255, 255), (255, 255, 255), (255, 255, 255), (200, 220, 255), (255, 235, 200), (255, 210, 220)]

def everywhere(draw):
    for dx in (-SIZE, 0, SIZE):
        for dy in (-SIZE, 0, SIZE):
            draw(dx, dy)

for _ in range(900):  # faint far ones
    x, y = random.randrange(SIZE), random.randrange(SIZE)
    v = random.randint(60, 150)
    pen.point((x, y), fill=(v, v, v + 10))
for _ in range(160):  # brighter ones, a pixel or two
    x, y, r = random.randrange(SIZE), random.randrange(SIZE), random.choice((0.6, 1, 1, 1.5))
    c = random.choice(TINTS)
    everywhere(lambda dx, dy: pen.ellipse((x + dx - r, y + dy - r, x + dx + r, y + dy + r), fill=c))
for _ in range(14):  # a few big ones with a soft halo and a cross of light
    x, y = random.randrange(SIZE), random.randrange(SIZE)
    c = random.choice(TINTS)
    everywhere(lambda dx, dy: halo.ellipse((x + dx - 7, y + dy - 7, x + dx + 7, y + dy + 7), fill=tuple(v // 3 for v in c)))
    everywhere(lambda dx, dy: pen.ellipse((x + dx - 2, y + dy - 2, x + dx + 2, y + dy + 2), fill=c))
    everywhere(lambda dx, dy: pen.line((x + dx - 6, y + dy, x + dx + 6, y + dy), fill=tuple(v // 2 for v in c)))
    everywhere(lambda dx, dy: pen.line((x + dx, y + dy - 6, x + dx, y + dy + 6), fill=tuple(v // 2 for v in c)))
glow = glow.filter(ImageFilter.GaussianBlur(4))
img = ImageChops.lighter(img, glow)
img.save(OUT, optimize=True)
print("space drawn", OUT)

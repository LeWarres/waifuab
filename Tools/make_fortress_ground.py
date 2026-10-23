# Draws the alien base's ground, Assets/Resources/Ground/fortress.png: dark armour plates in a grid, seams, a few
# rivets and hazard-free green light strips now and then. Low contrast, so the train and the enemies stand out.
# It tiles (the plate grid divides the tile exactly).
#   python Tools/make_fortress_ground.py
import os, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

random.seed(41)
SIZE = 1024
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground", "fortress.png")
img = Image.new("RGB", (SIZE, SIZE), (38, 46, 50))
pen = ImageDraw.Draw(img)
N = 8
step = SIZE // N
for gx in range(N):
    for gy in range(N):
        v = random.randint(-4, 4)
        pen.rectangle((gx * step + 3, gy * step + 3, (gx + 1) * step - 3, (gy + 1) * step - 3), fill=(40 + v, 48 + v, 52 + v))
        for cx, cy in ((10, 10), (step - 10, 10), (10, step - 10), (step - 10, step - 10)):  # rivets
            x, y = gx * step + cx, gy * step + cy
            pen.ellipse((x - 3, y - 3, x + 3, y + 3), fill=(58, 66, 70))
for k in range(N + 1):  # seams
    pen.line((k * step, 0, k * step, SIZE), fill=(26, 32, 36), width=4)
    pen.line((0, k * step, SIZE, k * step), fill=(26, 32, 36), width=4)
glow = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
gpen = ImageDraw.Draw(glow)
for _ in range(7):  # a few light strips set in the plates
    gx, gy = random.randrange(N), random.randrange(N)
    x, y = gx * step + 20, gy * step + step // 2
    gpen.rectangle((x, y - 2, x + step - 40, y + 2), fill=(50, 150, 40))
glow = glow.filter(ImageFilter.GaussianBlur(2))
img = ImageChops.lighter(img, glow)
img.save(OUT, optimize=True)
print("fortress drawn", OUT)

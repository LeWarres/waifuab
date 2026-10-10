# Paints the ground texture of every biome into Assets/Resources/Ground/<biome>.png.
# Source: the albedo maps inside the "Stylized Terrain Textures" package in the Asset Store cache (read straight
# from the .unitypackage, nothing is imported). Each one is the biome's own colour carrying the texture's light
# and dark, with some of the painted original mixed back in so it does not look like a flat tint.
#   python Tools/make_ground.py
import io, os, tarfile
from PIL import Image, ImageChops, ImageEnhance, ImageStat

PACKAGE = os.path.expandvars(r"%APPDATA%\Unity\Asset Store-5.x\Raygeas\Textures MaterialsNature\Stylized Terrain Textures.unitypackage")
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground")
# biome: (texture in the pack, ground colour, how much of the painted original shows through)
BIOMES = {
    "desert": ("Sand_1", (0.80, 0.69, 0.45), 0.35),
    "snow": ("Snow_1", (0.90, 0.93, 0.97), 0.35),
    "jungle": ("Grass_1", (0.36, 0.62, 0.25), 0.45),
    "volcano": ("Dark_rock_1", (0.32, 0.14, 0.11), 0.20),
    "city": ("Tile_1", (0.62, 0.64, 0.70), 0.40),
    "space": ("Rock_2", (0.11, 0.11, 0.21), 0.15),
    "alien": ("Dark_grass_1", (0.42, 0.17, 0.52), 0.10),
    "fortress": ("Dark_tile_1", (0.20, 0.25, 0.25), 0.20),
}
SIZE = 1024

def brightness(image):
    return ImageStat.Stat(image.convert("L")).mean[0]

def paint(albedo, colour, keep):
    albedo = albedo.convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)
    grey = albedo.convert("L")
    mean = ImageStat.Stat(grey).mean[0]
    detail = grey.point(lambda v: max(0, min(255, int(v / mean * 200))))  # 200 = this pixel is of average brightness
    solid = Image.new("RGB", (SIZE, SIZE), tuple(int(c * 255) for c in colour))
    tinted = ImageEnhance.Brightness(ImageChops.multiply(solid, detail.convert("RGB"))).enhance(255 / 200)
    original = ImageEnhance.Brightness(albedo).enhance(brightness(tinted) / max(brightness(albedo), 1))
    return Image.blend(tinted, original, keep)

wanted = {texture + "_Albedo.png": biome for biome, (texture, _, _) in BIOMES.items()}
held, made = {}, 0
with tarfile.open(PACKAGE, "r:gz") as tar:
    for member in tar:
        folder, _, leaf = member.name.rpartition("/")
        if leaf == "asset" and member.size < 60_000_000:
            held = {folder: tar.extractfile(member).read()}
        elif leaf == "pathname":
            name = os.path.basename(tar.extractfile(member).read().decode().split("\n")[0].strip())
            if name in wanted and folder in held:
                biome = wanted[name]
                _, colour, keep = BIOMES[biome]
                paint(Image.open(io.BytesIO(held[folder])), colour, keep).save(os.path.join(OUT, biome + ".png"), optimize=True)
                made += 1
                print(biome, name)
            if made == len(wanted):
                break
print(made, "of", len(wanted))

# The ocean has no texture in the pack: calm blue water, drawn here. Soft lighter and darker patches with a few
# short glints, with no direction to them (lines all one way turned into stripes on screen). It tiles: every
# stroke is repeated across the edges.
import random
from PIL import ImageDraw, ImageFilter
random.seed(7)
BASE = (44, 142, 216)
water = Image.new("RGB", (SIZE, SIZE), BASE)
pen = ImageDraw.Draw(water)
def everywhere(draw):
    for dx in (-SIZE, 0, SIZE):
        for dy in (-SIZE, 0, SIZE):
            draw(dx, dy)
for _ in range(70):
    x, y, w, h = random.randrange(SIZE), random.randrange(SIZE), random.randrange(90, 260), random.randrange(60, 180)
    shade = random.choice(((52, 152, 224), (38, 132, 208), (58, 160, 228)))
    everywhere(lambda dx, dy: pen.ellipse((x + dx, y + dy, x + dx + w, y + dy + h), fill=shade))
water = water.filter(ImageFilter.GaussianBlur(28))
pen = ImageDraw.Draw(water)
for _ in range(46):
    x, y, w = random.randrange(SIZE), random.randrange(SIZE), random.randrange(18, 46)
    start = random.randrange(360)
    everywhere(lambda dx, dy: pen.arc((x + dx, y + dy, x + dx + w, y + dy + w * 0.6), start, start + 120, fill=(120, 196, 240), width=3))
water.filter(ImageFilter.GaussianBlur(1.6)).save(os.path.join(OUT, "ocean.png"), optimize=True)
print("ocean drawn")

# Paints the desert's ground into Assets/Resources/Ground/desert.png: loose sand drifting over sheets of dry, cracked
# earth, with long fissures across them. Drawn from nothing, and it tiles. One repeat is 54 units on the ground
# (TrainSim.GroundTile): wider than the screen, so the eye never catches it repeating.
#   python Tools/make_desert_ground.py [out.png]
import os, sys
import numpy as np
from PIL import Image

SIZE, UNITS = 2048, 54.0
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Ground", "desert.png")
rng = np.random.default_rng(11)
SAND = np.array((0.84, 0.68, 0.42))      # loose, in the light
SAND_DARK = np.array((0.66, 0.49, 0.28)) # loose, in its hollows
CLAY = np.array((0.74, 0.55, 0.36))      # the dry sheets
CLAY_PALE = np.array((0.86, 0.72, 0.53)) # their sun-bleached plates
CRACK = np.array((0.24, 0.15, 0.10))
STONE = np.array((0.47, 0.37, 0.28))

# Smooth noise that tiles: random values on a grid of `cells` a side, blown up with wrap-round.
def lattice(cells):
    grid = rng.random((cells, cells), dtype=np.float32)
    at = (np.arange(SIZE, dtype=np.float32) + 0.5) * cells / SIZE
    i = at.astype(int)
    f = at - i
    f = f * f * (3 - 2 * f)
    i2 = (i + 1) % cells
    a = grid[i][:, i] * (1 - f)[None, :] + grid[i][:, i2] * f[None, :]
    b = grid[i2][:, i] * (1 - f)[None, :] + grid[i2][:, i2] * f[None, :]
    return a * (1 - f)[:, None] + b * f[:, None]

def fbm(cells, octaves, fall=0.55):
    total, weight, out = 0.0, 1.0, np.zeros((SIZE, SIZE), dtype=np.float32)
    for _ in range(octaves):
        out += lattice(cells) * weight
        total += weight
        weight *= fall
        cells *= 2
    return out / total

def stretch(a):
    return (a - a.min()) / (a.max() - a.min())

def smooth(lo, hi, a):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)

# Plates: for each pixel the distance to its nearest two seeds, wrapping round, its coordinates bent first so the
# edges wander. d2 - d1 is small along the edges between plates; `plate` numbers them. Returns both in pixels.
def plates(count, bend):
    ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
    xs += (fbm(14, 4) - 0.5) * bend
    ys += (fbm(14, 4) - 0.5) * bend
    seeds = rng.random((count, 2), dtype=np.float32) * SIZE
    edge = np.empty((SIZE, SIZE), dtype=np.float32)
    plate = np.empty((SIZE, SIZE), dtype=np.int32)
    for row in range(0, SIZE, 128):
        dy = np.abs(ys[row:row + 128, :, None] - seeds[:, 1])
        dx = np.abs(xs[row:row + 128, :, None] - seeds[:, 0])
        dy = np.minimum(dy, SIZE - dy % SIZE)
        dx = np.minimum(dx, SIZE - dx % SIZE)
        d = dx * dx + dy * dy
        two = np.argpartition(d, 1, axis=2)[..., :2]
        near = np.take_along_axis(d, two, 2) ** 0.5
        edge[row:row + 128] = np.abs(near[..., 1] - near[..., 0])
        first = near[..., 0] <= near[..., 1]
        plate[row:row + 128] = np.where(first, two[..., 0], two[..., 1])
    return edge, plate

sheets = smooth(0.46, 0.56, stretch(fbm(5, 4)))        # where the dry earth lies bare (1) and where sand covers it (0)
drift = fbm(9, 5)                                      # the sand's lumps
grain = rng.random((SIZE, SIZE), dtype=np.float32)
mottle = fbm(60, 3)

small, plate = plates(int((UNITS / 2.6) ** 2), 90)     # plates about 2.6 units across
large, _ = plates(int((UNITS / 13) ** 2), 220)         # the long fissures, about 13 apart
tone = rng.random(plate.max() + 1, dtype=np.float32)[plate]
gaps = fbm(18, 3)                                      # where a long fissure peters out

# How high the ground stands: lit from the top left and painted in, since the toon shader won't show so small a relief.
crack_small = (1 - smooth(1.5, 5.0, small)) * sheets
crack_large = (1 - smooth(2.0, 7.0, large)) * smooth(0.42, 0.52, gaps)
height = drift * 26 + mottle * 5 + sheets * 4 + (smooth(0, 60, small) * sheets) * 3
height -= crack_small * 3 + crack_large * 7
shade = np.roll(height, (2, 2), (0, 1)) - np.roll(height, (-2, -2), (0, 1))
light = np.clip(1 + shade * 0.085, 0.72, 1.24)[..., None]

sand = SAND_DARK + (SAND - SAND_DARK) * smooth(0.3, 0.7, drift)[..., None]
clay = CLAY + (CLAY_PALE - CLAY) * (tone * 0.75 + mottle * 0.25)[..., None]
image = sand + (clay - sand) * sheets[..., None]
pebbles = smooth(0.82, 0.86, fbm(180, 2)) * (1 - sheets * 0.7)
image += (STONE - image) * pebbles[..., None] * 0.7
image *= light
crack = np.maximum(crack_small * 0.9, crack_large)[..., None]
image += (CRACK - image) * crack
image *= (0.975 + grain * 0.05)[..., None]
Image.fromarray((np.clip(image, 0, 1) * 255).astype(np.uint8)).save(OUT, optimize=True)
print(OUT)

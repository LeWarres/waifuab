# Paints the surfaces that are not any biome's ground into Assets/Resources/Surfaces. Both tile, and both are near white,
# since what wears them tints them.
#   rock.png: fractured stone, chipped into facets, its relief painted in (the toon shader would not show it).
#   metal.png: brushed sheet metal.
#   python Tools/make_surfaces.py
import os
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Surfaces")
SIZE = 1024
rng = np.random.default_rng(23)

def lattice(cx, cy):
    grid = rng.random((cy, cx), dtype=np.float32)
    def axis(cells):
        at = (np.arange(SIZE, dtype=np.float32) + 0.5) * cells / SIZE
        i = at.astype(int)
        f = at - i
        return i, (i + 1) % cells, f * f * (3 - 2 * f)
    y0, y1, fy = axis(cy)
    x0, x1, fx = axis(cx)
    a = grid[y0][:, x0] * (1 - fx)[None, :] + grid[y0][:, x1] * fx[None, :]
    b = grid[y1][:, x0] * (1 - fx)[None, :] + grid[y1][:, x1] * fx[None, :]
    return a * (1 - fy)[:, None] + b * fy[:, None]

def fbm(cx, cy, octaves, fall=0.55):
    total, weight, out = 0.0, 1.0, np.zeros((SIZE, SIZE), dtype=np.float32)
    for _ in range(octaves):
        out += lattice(cx, cy) * weight
        total += weight
        weight *= fall
        cx *= 2
        cy *= 2
    return out / total

def stretch(a):
    return (a - a.min()) / (a.max() - a.min())

def smooth(lo, hi, a):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)

# Cells that wrap round: for each pixel d2 - d1 (small along the cell's edges) and the cell it is in.
def cells(count, bend):
    ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
    xs += (fbm(9, 9, 3) - 0.5) * bend
    ys += (fbm(9, 9, 3) - 0.5) * bend
    seeds = rng.random((count, 2), dtype=np.float32) * SIZE
    edge = np.empty((SIZE, SIZE), dtype=np.float32)
    cell = np.empty((SIZE, SIZE), dtype=np.int32)
    for row in range(0, SIZE, 64):
        dy = np.abs(ys[row:row + 64, :, None] - seeds[:, 1])
        dx = np.abs(xs[row:row + 64, :, None] - seeds[:, 0])
        dy = np.minimum(dy % SIZE, SIZE - dy % SIZE)
        dx = np.minimum(dx % SIZE, SIZE - dx % SIZE)
        d = dx * dx + dy * dy
        two = np.argpartition(d, 1, axis=2)[..., :2]
        near = np.take_along_axis(d, two, 2) ** 0.5
        edge[row:row + 64] = np.abs(near[..., 1] - near[..., 0])
        cell[row:row + 64] = np.where(near[..., 0] <= near[..., 1], two[..., 0], two[..., 1])
    return edge, cell

def save(name, value):
    Image.fromarray((np.clip(value, 0, 1) * 255).astype(np.uint8)).save(os.path.join(OUT, name), optimize=True)
    print(name)

# ---- rock: big blocks, chipped into small facets each tilted its own way, fine grit over it, lit from the top left ----
ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
big_edge, big = cells(26, 90)
small_edge, small = cells(210, 40)
def tilted(cell, amount):
    tx, ty, up = (rng.random((3, cell.max() + 1), dtype=np.float32) - 0.5)
    # a slope per chip, made tile-safe by using the wrapped sine of the position
    return (tx[cell] * np.sin(xs / SIZE * 2 * np.pi * 8) + ty[cell] * np.sin(ys / SIZE * 2 * np.pi * 8) + up[cell]) * amount
fissure = 1 - smooth(1.0, 5.0, big_edge)
chip = 1 - smooth(0.6, 2.6, small_edge)
grit = fbm(120, 120, 2)
height = tilted(big, 34) + tilted(small, 16) + stretch(fbm(6, 6, 5)) * 28 + grit * 5 - fissure * 22 - chip * 6
light = np.clip(1 + (np.roll(height, (2, 2), (0, 1)) - np.roll(height, (-2, -2), (0, 1))) * 0.075, 0.5, 1.3)
tone = (rng.random(big.max() + 1, dtype=np.float32)[big] * 0.12 + 0.8) * (0.9 + stretch(fbm(5, 5, 4)) * 0.2)
shade = tone * light * (1 - fissure * 0.7) * (1 - chip * 0.3)
save("rock.png", np.stack((shade, shade * 0.975, shade * 0.94), 2))

# ---- metal: the grain of the brush along one way, a few long scratches, soft cloudy wear ----
grain = stretch(fbm(5, 220, 3, 0.7))
wear = stretch(fbm(6, 6, 4))
scratches = smooth(0.9, 0.97, stretch(fbm(9, 300, 2))) * smooth(0.45, 0.6, stretch(fbm(5, 5, 2)))
value = 0.8 + grain * 0.18 + wear * 0.04 - scratches * 0.2
save("metal.png", np.repeat(value[..., None], 3, 2))

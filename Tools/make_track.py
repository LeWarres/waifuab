# Builds the track pieces and exports them for Unity:
#   Track_rail  one unit of both rails on their bed; Unity stretches it along the track
#   Sleeper_0   wooden tie with plates        (desert, snow, jungle)
#   Sleeper_1   twin concrete blocks and bar  (volcano, city)
#   Sleeper_2   slim metal beam with a light  (space, alien, fortress)
#   blender -b --python Tools/make_track.py
# The rails run along Blender's x (the game's x); materials are placeholders, Unity swaps them per biome by name.
import os, sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *

reset()
RAIL = mat("TrackRail", (0.45, 0.47, 0.5), 0.35, 0.8)
BED = mat("TrackBallast", (0.6, 0.52, 0.38))
TIE = mat("TrackSleeper", (0.4, 0.27, 0.15))
GAUGE = 0.8  # each rail's distance from the middle of the track

# ---- rails and bed: from x = -1 to 0 here, which the export turns into 0 to 1 in Unity ----
X0, X1 = -1.0, 0.0
# Bed with sloping shoulders.
mesh("bed", [(X0, -1.9, 0), (X0, 1.9, 0), (X0, 1.5, 0.07), (X0, -1.5, 0.07), (X1, -1.9, 0), (X1, 1.9, 0), (X1, 1.5, 0.07), (X1, -1.5, 0.07)],
     [(0, 1, 2, 3), (7, 6, 5, 4), (3, 2, 6, 7), (0, 3, 7, 4), (2, 1, 5, 6)], BED)
for side in (-1, 1):
    y = side * GAUGE
    box(X0, X1, y - 0.09, y + 0.09, 0.13, 0.155, RAIL, "foot")
    box(X0, X1, y - 0.03, y + 0.03, 0.155, 0.2, RAIL, "web")
    box(X0, X1, y - 0.055, y + 0.055, 0.2, 0.235, RAIL, "head")
finish("Track_rail")

# ---- wooden tie ----
box(-0.17, 0.17, -1.45, 1.45, 0.05, 0.135, TIE, "tie")
both(lambda y0, y1: box(-0.21, 0.21, y0, y1, 0.135, 0.15, RAIL, "plate"), GAUGE - 0.2, GAUGE + 0.2)
finish("Sleeper_0")

# ---- twin blocks ----
both(lambda y0, y1: box(-0.23, 0.23, y0, y1, 0.04, 0.145, TIE, "block"), GAUGE - 0.38, GAUGE + 0.42)
box(-0.05, 0.05, -0.5, 0.5, 0.07, 0.11, RAIL, "bar")
finish("Sleeper_1")

# ---- slim beam ----
box(-0.08, 0.08, -1.3, 1.3, 0.07, 0.12, TIE, "beam")
both(lambda y0, y1: box(-0.2, 0.2, y0, y1, 0.04, 0.14, TIE, "clamp"), GAUGE - 0.16, GAUGE + 0.16)
box(-0.13, 0.13, -0.28, 0.28, 0.12, 0.14, RAIL, "light")
finish("Sleeper_2")

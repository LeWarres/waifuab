# The four icons of the sound menu, in the same sticker look as Tools/make_icons.py's symbols:
# sound (speaker), music (note), sfx (burst), mute (speaker crossed out).
#   python Tools/make_sound_icons.py
import math, os
from PIL import Image, ImageDraw

S, OUT, W = 512, 256, 16
NAVY, WHITE = (86, 84, 132, 255), (255, 255, 255, 255)
SKY, GOLD, ROSE, MINT = (150, 215, 255, 255), (255, 215, 60, 255), (240, 80, 80, 255), (150, 235, 180, 255)
FOLDER = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Resources", "Icons")

def new():
    image = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)

def save(image, name):
    image.resize((OUT, OUT), Image.LANCZOS).save(os.path.join(FOLDER, name + ".png"))

def speaker(d, fill):
    d.polygon([(90, 200), (180, 200), (290, 110), (290, 402), (180, 312), (90, 312)], fill=fill, outline=NAVY, width=W)
    d.line([(180, 200), (180, 312)], fill=NAVY, width=W)

image, d = new()
speaker(d, SKY)
for r in (70, 130):
    d.arc((300 - r, 256 - r, 300 + r, 256 + r), -50, 50, fill=NAVY, width=W + 8)
    d.arc((300 - r, 256 - r, 300 + r, 256 + r), -46, 46, fill=WHITE, width=W - 4)
save(image, "sound")

image, d = new()
speaker(d, (200, 205, 220, 255))
d.line([(330, 190), (450, 322)], fill=NAVY, width=W + 22); d.line([(450, 190), (330, 322)], fill=NAVY, width=W + 22)
d.line([(330, 190), (450, 322)], fill=ROSE, width=W + 4); d.line([(450, 190), (330, 322)], fill=ROSE, width=W + 4)
save(image, "mute")

image, d = new()
for x, y in ((150, 360), (340, 320)):
    d.ellipse((x - 62, y - 46, x + 62, y + 46), fill=GOLD, outline=NAVY, width=W)
d.polygon([(190, 130), (400, 90), (400, 150), (190, 190)], fill=GOLD, outline=NAVY, width=W)
d.line([(200, 150), (200, 360)], fill=NAVY, width=W + 6); d.line([(390, 110), (390, 320)], fill=NAVY, width=W + 6)
save(image, "music")

image, d = new()
points = []
for i in range(16):
    a, r = i * math.pi / 8, 200 if i % 2 == 0 else 105
    points.append((256 + math.cos(a) * r, 256 + math.sin(a) * r))
d.polygon(points, fill=MINT, outline=NAVY, width=W)
d.ellipse((206, 206, 306, 306), fill=WHITE, outline=NAVY, width=W)
save(image, "sfx")

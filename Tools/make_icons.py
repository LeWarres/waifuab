"""Draws the plain SYMBOL icons (arrows, check, cross, play...) as pastel stickers, to sit next to the
generated object icons from Tools/gen_icons.py. Only the names in SYMBOLS are written.

Originally: draws the game's UI icons into Assets/Resources/Icons (transparent PNG).

Run from the project root:  python Tools/make_icons.py
Style: a rounded glossy tile per category, a white glyph with a navy outline and one accent colour.
Everything is drawn 4x and scaled down, so edges come out smooth. Add an icon by adding a function
to ICONS; the file name is the dictionary key, which is the name the game asks for.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

S, OUT, W = 512, 256, 14          # working size, final size, outline width
NAVY = (86, 84, 132, 255)          # soft violet-grey line, like the generated icons
SYMBOLS = ('skip', 'back', 'yes', 'no', 'stay', 'resume', 'restart')
WHITE = (255, 255, 255, 255)
TILES = {                          # category: (top colour, bottom colour)
    'wagon': ((120, 200, 255), (50, 130, 230)),
    'hero': ((205, 160, 255), (130, 90, 225)),
    'stat': ((150, 235, 170), (50, 175, 110)),
    'gold': ((255, 232, 130), (245, 170, 40)),
    'cargo': ((255, 200, 140), (230, 125, 60)),
    'shop': ((140, 235, 225), (40, 165, 175)),
    'system': ((215, 225, 240), (140, 160, 190)),
    'danger': ((255, 170, 170), (225, 80, 90)),
}
RED, ORANGE, YELLOW = (240, 80, 80, 255), (255, 150, 40, 255), (255, 215, 60, 255)
GREEN, CYAN, BLUE = (90, 210, 110, 255), (110, 225, 250, 255), (70, 140, 240, 255)
SKY, MINT, ROSE = (150, 215, 255, 255), (150, 235, 180, 255), (255, 160, 175, 255)
PINK, PURPLE, GREY, BROWN = (255, 130, 170, 255), (160, 110, 240, 255), (150, 165, 185, 255), (170, 110, 60, 255)


class Icon:
    def __init__(self, tile):
        self.im = Image.new('RGBA', (S, S), (0, 0, 0, 0)) # no tile: the glyph itself becomes a sticker
        self.d = ImageDraw.Draw(self.im)

    # --- shapes: all filled, all outlined in navy ---
    def rrect(self, box, r, fill=WHITE, w=W):
        self.d.rounded_rectangle(box, r, fill=fill, outline=NAVY, width=w)

    def ell(self, box, fill=WHITE, w=W):
        self.d.ellipse(box, fill=fill, outline=NAVY, width=w)

    def circle(self, x, y, r, fill=WHITE, w=W):
        self.ell((x - r, y - r, x + r, y + r), fill, w)

    def poly(self, pts, fill=WHITE, w=W):
        self.d.polygon(pts, fill=fill)
        self.d.line(pts + [pts[0], pts[1]], fill=NAVY, width=w, joint='curve')

    def line(self, pts, fill=WHITE, w=30):
        for p in pts:                                                        # round caps and joints
            self.d.ellipse((p[0] - w / 2 - W, p[1] - w / 2 - W, p[0] + w / 2 + W, p[1] + w / 2 + W), fill=NAVY)
        self.d.line(pts, fill=NAVY, width=w + 2 * W, joint='curve')
        for p in pts:
            self.d.ellipse((p[0] - w / 2, p[1] - w / 2, p[0] + w / 2, p[1] + w / 2), fill=fill)
        self.d.line(pts, fill=fill, width=w, joint='curve')

    def text(self, s, size, y=256, fill=WHITE):
        font = ImageFont.truetype('arialbd.ttf', size)
        self.d.text((256, y), s, font=font, fill=fill, anchor='mm', stroke_width=W, stroke_fill=NAVY)

    def star(self, x, y, r, points=5, inner=0.45, fill=YELLOW, rot=-90):
        pts = []
        for i in range(points * 2):
            a = math.radians(rot + i * 180 / points)
            rr = r if i % 2 == 0 else r * inner
            pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
        self.poly(pts, fill)

    def bolt(self, x, y, s=1.0, fill=YELLOW):
        pts = [(30, -130), (-70, 20), (-5, 20), (-40, 130), (75, -30), (10, -30)]
        self.poly([(x + px * s, y + py * s) for px, py in pts], fill)

    def save(self, name, folder):
        # white sticker border around whatever was drawn, then crop to it
        alpha = self.im.split()[3]
        border = alpha.filter(ImageFilter.MaxFilter(31)).filter(ImageFilter.GaussianBlur(2)).point(lambda v: 255 if v > 110 else int(v * 2.3))
        sticker = Image.new('RGBA', (S, S), (255, 255, 255, 0))
        sticker.putalpha(border)
        out = Image.alpha_composite(sticker, self.im)
        box = out.getbbox()
        obj = out.crop(box)
        side = int(max(obj.size) * 1.06)
        square = Image.new('RGBA', (side, side), (0, 0, 0, 0))
        square.alpha_composite(obj, ((side - obj.width) // 2, (side - obj.height) // 2))
        square.resize((OUT, OUT), Image.LANCZOS).save(os.path.join(folder, name + '.png'))


# ---------------------------------------------------------------- wagon weapons
def turret():
    i = Icon('wagon'); i.line([(256, 270), (370, 150)], GREY, 44); i.rrect((140, 300, 372, 400), 30); i.circle(256, 290, 78, CYAN); return i

def rocket():
    i = Icon('wagon'); i.poly([(170, 380), (120, 420), (150, 330)], ORANGE); i.poly([(330, 220), (392, 250), (300, 300)], RED)
    i.poly([(160, 250), (190, 160), (250, 190)], RED); i.poly([(150, 362), (190, 250), (300, 150), (390, 120), (362, 212), (262, 322)], WHITE)
    i.circle(300, 212, 34, CYAN); return i

def minigun():
    i = Icon('wagon')
    for y in (190, 256, 322): i.line([(200, y), (400, y)], GREY, 30)
    i.rrect((110, 160, 240, 352), 34, CYAN); return i

def shotgun():
    i = Icon('wagon'); i.poly([(120, 230), (250, 190), (250, 322), (120, 282)], BROWN)
    for a in (-32, -16, 0, 16, 32):
        r = math.radians(a); i.circle(300 + math.cos(r) * 100, 256 + math.sin(r) * 100, 30, YELLOW, 12)
    return i

def sniper():
    i = Icon('wagon'); i.circle(256, 256, 130, CYAN); i.circle(256, 256, 62, WHITE)
    for a, b in (((256, 100), (256, 190)), ((256, 322), (256, 412)), ((100, 256), (190, 256)), ((322, 256), (412, 256))): i.line([a, b], RED, 16)
    return i

def pulse():
    i = Icon('wagon'); i.circle(256, 256, 150, (255, 215, 235, 255), 22); i.circle(256, 256, 100, WHITE, 22); i.circle(256, 256, 48, PINK); return i

def storm():
    i = Icon('wagon'); i.circle(190, 200, 70); i.circle(300, 180, 86); i.circle(350, 230, 60); i.rrect((130, 200, 400, 280), 40)
    i.d.rectangle((160, 190, 380, 250), fill=WHITE); i.bolt(262, 350, 0.8); return i

def flame():
    i = Icon('wagon'); i.poly([(256, 90), (350, 230), (372, 320), (320, 400), (256, 420), (192, 400), (140, 320), (170, 220), (212, 250)], ORANGE)
    i.poly([(256, 250), (300, 320), (290, 380), (256, 396), (222, 380), (212, 320)], YELLOW, 12); return i

def freeze():
    i = Icon('wagon')
    for a in (0, 60, 120):
        r = math.radians(a); dx, dy = math.cos(r) * 150, math.sin(r) * 150
        i.line([(256 - dx, 256 - dy), (256 + dx, 256 + dy)], CYAN, 22)
    i.circle(256, 256, 40, WHITE); return i

def chain():
    i = Icon('wagon'); i.line([(130, 370), (220, 230), (300, 300), (390, 140)], YELLOW, 22)
    for p in ((130, 370), (220, 230), (300, 300), (390, 140)): i.circle(p[0], p[1], 34, CYAN, 14)
    return i

def laser():
    i = Icon('wagon'); i.line([(110, 400), (400, 110)], RED, 26)
    for p in ((180, 330), (256, 256), (330, 180)): i.circle(p[0], p[1], 36, WHITE, 14)
    return i

# ---------------------------------------------------------------- hero weapons
def blaster():
    i = Icon('hero'); i.rrect((120, 170, 400, 260), 34); i.rrect((150, 240, 240, 400), 30, CYAN); i.circle(360, 215, 22, YELLOW, 12); return i

def grenade():
    i = Icon('hero'); i.rrect((216, 110, 296, 180), 16, GREY); i.circle(256, 290, 120, GREEN); i.line([(296, 140), (370, 150), (380, 220)], YELLOW, 14); return i

def smg():
    i = Icon('hero'); i.rrect((110, 180, 330, 260), 26); i.line([(330, 220), (410, 220)], GREY, 26); i.rrect((190, 250, 250, 400), 20, CYAN); i.rrect((120, 250, 180, 340), 20, WHITE); return i

def scatter():
    i = Icon('hero'); i.circle(150, 256, 46, WHITE)
    for a in (-35, 0, 35):
        r = math.radians(a); i.line([(190 + math.cos(r) * 20, 256 + math.sin(r) * 20), (190 + math.cos(r) * 210, 256 + math.sin(r) * 210)], ORANGE, 22)
    return i

def rail():
    i = Icon('hero'); i.rrect((100, 216, 412, 296), 30)
    for x in (170, 236, 302): i.rrect((x, 190, x + 36, 322), 12, PURPLE, 12)
    i.circle(400, 256, 30, CYAN, 12); return i

def nova():
    i = Icon('hero'); i.star(256, 256, 170, 8, 0.5, PINK); i.circle(256, 256, 52, WHITE); return i

def spark():
    i = Icon('hero'); i.bolt(256, 256, 1.35, CYAN); return i

# ---------------------------------------------------------------- hero stats
def speed():
    i = Icon('stat')
    for x in (120, 215, 310): i.poly([(x, 150), (x + 90, 256), (x, 362), (x - 10, 322), (x + 46, 256), (x - 10, 190)], WHITE, 14)
    return i

def armor():
    i = Icon('stat'); i.poly([(256, 100), (392, 150), (380, 300), (256, 420), (132, 300), (120, 150)], CYAN)
    i.poly([(256, 160), (336, 190), (330, 286), (256, 356), (182, 286), (176, 190)], WHITE, 12); return i

def heart(i, x=256, y=262, s=1.0, fill=PINK):
    pts = []
    for k in range(40):
        t = math.radians(k * 9)
        px = 16 * math.sin(t) ** 3
        py = -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))
        pts.append((x + px * 9.5 * s, y + py * 9.5 * s))
    i.poly(pts, fill)

def vitality():
    i = Icon('stat'); heart(i); i.line([(256, 205), (256, 305)], WHITE, 20); i.line([(206, 255), (306, 255)], WHITE, 20); return i

# ---------------------------------------------------------------- money and choices
def money():
    i = Icon('gold'); i.circle(256, 256, 150, YELLOW); i.circle(256, 256, 108, (255, 235, 140, 255), 12); i.text('$', 170, 262, WHITE); return i

def skip():
    i = Icon('system'); i.poly([(120, 150), (240, 256), (120, 362)], SKY); i.poly([(240, 150), (360, 256), (240, 362)], SKY); i.rrect((366, 150, 406, 362), 12, SKY); return i

def back():
    i = Icon('system'); i.poly([(110, 256), (240, 140), (240, 216), (400, 216), (400, 296), (240, 296), (240, 372)], SKY); return i

def yes():
    i = Icon('stat'); i.line([(130, 266), (226, 356), (386, 160)], MINT, 56); return i

def no():
    i = Icon('danger'); i.line([(150, 150), (362, 362)], ROSE, 56); i.line([(362, 150), (150, 362)], ROSE, 56); return i

# ---------------------------------------------------------------- cargo
def wood():
    i = Icon('cargo')
    for x, y in ((180, 330), (332, 330), (256, 200)):
        i.circle(x, y, 82, BROWN); i.circle(x, y, 40, (235, 190, 130, 255), 10)
    return i

def rock():
    i = Icon('cargo'); i.poly([(120, 340), (160, 200), (270, 130), (380, 210), (400, 350), (290, 400), (180, 400)], GREY)
    i.d.line([(270, 130), (260, 250), (180, 300)], fill=NAVY, width=10); i.d.line([(260, 250), (340, 290)], fill=NAVY, width=10); return i

def arms():
    i = Icon('cargo'); i.rrect((110, 170, 402, 392), 26, (120, 160, 110, 255)); i.d.line([(110, 245), (402, 245)], fill=NAVY, width=12)
    i.circle(256, 318, 46, WHITE, 12); i.line([(256, 290), (256, 346)], RED, 8); i.line([(228, 318), (284, 318)], RED, 8); return i

def container():
    i = Icon('cargo'); i.rrect((96, 170, 416, 380), 22, BLUE)
    for x in (160, 224, 288, 352): i.d.line([(x, 190), (x, 360)], fill=NAVY, width=12)
    return i

def slot():
    i = Icon('cargo'); i.rrect((130, 200, 382, 330), 24, (255, 255, 255, 90), 14); i.line([(256, 225), (256, 305)], WHITE, 20); i.line([(216, 265), (296, 265)], WHITE, 20)
    i.circle(180, 370, 30, GREY, 12); i.circle(332, 370, 30, GREY, 12); return i

# ---------------------------------------------------------------- shop
def wagon():
    i = Icon('shop'); i.rrect((100, 160, 412, 330), 26, RED); i.rrect((140, 195, 220, 265), 12, CYAN, 12); i.rrect((292, 195, 372, 265), 12, CYAN, 12)
    i.circle(170, 360, 40, GREY); i.circle(342, 360, 40, GREY); return i

def repair():
    i = Icon('shop'); i.line([(170, 350), (330, 190)], GREY, 40); i.circle(350, 170, 66, GREY); i.d.pieslice((300, 110, 420, 230), -80, 10, fill=TILES['shop'][0] + (255,))
    i.circle(160, 360, 30, WHITE, 12); return i

def heal():
    i = Icon('shop'); heart(i, fill=RED); return i

def medkit():
    i = Icon('shop'); i.rrect((206, 130, 306, 200), 20, (255, 255, 255, 0), 16); i.rrect((104, 180, 408, 392), 34)
    i.line([(256, 232), (256, 340)], RED, 30); i.line([(202, 286), (310, 286)], RED, 30); return i

# ---------------------------------------------------------------- route and system
def stay():
    i = Icon('system'); i.poly([(256, 110), (380, 240), (300, 240), (300, 400), (212, 400), (212, 240), (132, 240)], MINT); return i

def biome():
    i = Icon('gold'); i.poly([(100, 380), (220, 170), (300, 300), (340, 240), (420, 380)], GREEN); i.poly([(190, 222), (220, 170), (252, 222), (222, 240)], WHITE, 10)
    i.circle(366, 150, 40, WHITE, 12); return i

def language():
    i = Icon('system'); i.circle(256, 256, 150, CYAN); i.d.ellipse((186, 106, 326, 406), outline=NAVY, width=12)
    i.d.line([(106, 256), (406, 256)], fill=NAVY, width=12); i.d.line([(256, 106), (256, 406)], fill=NAVY, width=12); return i

def resume():
    i = Icon('stat'); i.poly([(180, 130), (390, 256), (180, 382)], MINT); return i

def restart():
    i = Icon('system'); i.d.arc((120, 120, 392, 392), 30, 300, fill=NAVY, width=74); i.d.arc((134, 134, 378, 378), 33, 297, fill=SKY, width=46)
    i.poly([(300, 96), (420, 150), (322, 230)], SKY); return i

def quit_():
    i = Icon('danger'); i.rrect((120, 110, 300, 402), 20, (255, 255, 255, 110), 16); i.poly([(250, 216), (340, 216), (340, 160), (430, 256), (340, 352), (340, 296), (250, 296)]); return i


ICONS = {
    'turret': turret, 'rocket': rocket, 'minigun': minigun, 'shotgun': shotgun, 'sniper': sniper, 'pulse': pulse, 'storm': storm,
    'flame': flame, 'freeze': freeze, 'chain': chain, 'laser': laser,
    'blaster': blaster, 'grenade': grenade, 'smg': smg, 'scatter': scatter, 'rail': rail, 'nova': nova, 'spark': spark,
    'speed': speed, 'armor': armor, 'vitality': vitality,
    'money': money, 'skip': skip, 'back': back, 'yes': yes, 'no': no,
    'wood': wood, 'rock': rock, 'arms': arms, 'container': container, 'slot': slot,
    'wagon': wagon, 'repair': repair, 'heal': heal, 'medkit': medkit,
    'stay': stay, 'biome': biome, 'language': language, 'resume': resume, 'restart': restart, 'quit': quit_,
}

if __name__ == '__main__':
    folder = os.path.join('Assets', 'Resources', 'Icons')
    os.makedirs(folder, exist_ok=True)
    for name in SYMBOLS:
        ICONS[name]().save(name, folder)
    print(len(SYMBOLS), 'symbols ->', folder)

"""Generates the UI icons with the local ComfyUI and cuts out the background.

Model: the "Game Icon Institute v4 XL" checkpoint (models/checkpoints/gameIconInstitute_v4XL.safetensors),
which in side-by-side tests gave one clean, centred, pastel object per image; no LoRA needed.

Usage, from the project root, with ComfyUI running on http://127.0.0.1:8188:
    python Tools/gen_icons.py                 # every icon
    python Tools/gen_icons.py rocket medkit   # only these
    python Tools/gen_icons.py --seed 7 rocket # another take of one

Each icon is rendered on a flat pastel background, which is flood-filled away from the edges, then the
object is cropped, squared and saved as a 256x256 transparent PNG in Assets/Resources/Icons/<name>.png
(the name the game asks for). Raw renders go to Tools/icons_raw/, a contact sheet to Tools/icons_preview.png.
"""
import io
import json
import os
import sys
import time
import urllib.parse
import urllib.request

from PIL import Image, ImageDraw, ImageFilter

HOST = 'http://127.0.0.1:8188'
CHECKPOINT = 'gameIconInstitute_v4XL.safetensors'
STYLE = ('game icon, {subject}, single object, centered, anime style, cel shading, flat color, white outline, glossy, '
         'pastel colors, simple background, green background, masterpiece, best quality')
NEGATIVE = ('1girl, 1boy, human, face, hands, text, letters, watermark, signature, logo, blurry, lowres, jpeg artifacts, photo, realistic, '
            '3d, sketch, monochrome, multiple views, multiple objects, many, pile, pattern, repeating, collage, sheet, frame, border, '
            'ground, scenery, cast shadow, gradient background, cropped, out of frame')

# name -> what to draw (danbooru-style tags)
ICONS = {
    # wagon weapons
    'turret': 'gun turret, mounted cannon, single barrel, blue metal',
    'rocket': 'rocket launcher, bazooka, missile, red warhead',
    'minigun': 'gatling gun, minigun, rotary barrels, steel',
    'shotgun': 'shotgun, pump action shotgun, wooden stock',
    'sniper': 'sniper rifle, long barrel, scope',
    'pulse': 'energy orb, glowing pink sphere, shockwave rings',
    'storm': 'thunder cloud, lightning bolt, white cloud, yellow lightning',
    'flame': 'flamethrower, fire, flame, orange fire',
    'freeze': 'ice crystal, snowflake, blue ice, frost',
    'chain': 'tesla coil, electricity, blue lightning arcs',
    'laser': 'laser cannon, red laser beam, sci-fi gun',
    # hero weapons
    'blaster': 'sci-fi pistol, ray gun, blue handgun',
    'grenade': 'hand grenade, green grenade',
    'smg': 'submachine gun, compact gun',
    'scatter': 'sawed-off shotgun, short double barrel shotgun',
    'rail': 'railgun, purple energy rifle, sci-fi',
    'nova': 'star burst, pink explosion star, sparkle',
    'spark': 'lightning bolt, cyan electricity, single bolt',
    # hero stats
    'speed': 'winged boot, sneaker with wings, speed',
    'armor': 'shield, blue shield, metal trim',
    'vitality': 'heart, pink heart, plus sign',
    # money (the choice symbols come from Tools/make_icons.py: the model cannot draw a clean arrow or check mark)
    'money': 'gold coin, shiny coin, star emblem',
    # cargo
    'wood': 'wooden logs, stack of logs, lumber',
    'rock': 'grey rock, boulder, stone ore',
    'arms': 'weapon crate, military wooden crate, ammo box',
    'container': 'shipping container, blue cargo container',
    'slot': 'flatbed train car, empty railway wagon, plus sign',
    # shop
    'wagon': 'train car, red railway wagon, cute train carriage',
    'repair': 'wrench, spanner, silver tool',
    'heal': 'red heart, heart',
    'medkit': 'first aid kit, white medical box, red cross',
    # route and system
    'biome': 'mountain landscape emblem, map, snowy mountains, sun',
    'language': 'globe, earth, blue globe',
    'quit': 'exit door, open door, arrow',
}


def post(path, data):
    req = urllib.request.Request(HOST + path, json.dumps(data).encode(), {'Content-Type': 'application/json'})
    return json.load(urllib.request.urlopen(req))


def get(path):
    return urllib.request.urlopen(HOST + path).read()


def render(name, subject, seed):
    graph = {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': CHECKPOINT}},
        '2': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': STYLE.format(subject=subject)}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': NEGATIVE}},
        '4': {'class_type': 'EmptyLatentImage', 'inputs': {'width': 768, 'height': 768, 'batch_size': 1}},
        '5': {'class_type': 'KSampler', 'inputs': {'model': ['1', 0], 'positive': ['2', 0], 'negative': ['3', 0], 'latent_image': ['4', 0],
                                                    'seed': seed, 'steps': 28, 'cfg': 6.0, 'sampler_name': 'euler_ancestral',
                                                    'scheduler': 'normal', 'denoise': 1.0}},
        '6': {'class_type': 'VAEDecode', 'inputs': {'samples': ['5', 0], 'vae': ['1', 2]}},
        '7': {'class_type': 'SaveImage', 'inputs': {'images': ['6', 0], 'filename_prefix': 'waifutren_icon_' + name}},
    }
    queued = post('/prompt', {'prompt': graph, 'client_id': 'waifutren-icons'})
    if queued.get('node_errors'):
        raise RuntimeError(queued['node_errors'])
    while True:
        history = json.loads(get('/history/' + queued['prompt_id']))
        entry = history.get(queued['prompt_id'])
        if entry and entry['status'].get('completed'):
            image = entry['outputs']['7']['images'][0]
            return Image.open(io.BytesIO(get('/view?' + urllib.parse.urlencode(image)))).convert('RGBA')
        time.sleep(1)


def cut_out(image):
    """Removes the flat background by flood-filling in from the edges, then crops to the object and squares it."""
    rgb = image.convert('RGB')
    w, h = rgb.size
    marker = (255, 0, 255)
    for x in range(0, w, 24):
        for xy in ((x, 0), (x, h - 1), (0, min(x, h - 1)), (w - 1, min(x, h - 1))):
            if rgb.getpixel(xy) != marker:
                ImageDraw.floodfill(rgb, xy, marker, thresh=44)
    mask = Image.new('L', (w, h), 255)
    mask.putdata([0 if p == marker else 255 for p in rgb.getdata()])
    mask = mask.filter(ImageFilter.MinFilter(7)).filter(ImageFilter.MaxFilter(5))        # drop specks the fill left behind, shave the fringe
    mask = mask.filter(ImageFilter.GaussianBlur(1))                                    # soften the edge
    image = image.convert('RGBA')
    image.putalpha(mask)
    box = mask.point(lambda v: 255 if v > 40 else 0).getbbox()
    if not box:
        return image
    obj = image.crop(box)
    side = int(max(obj.size) * 1.06)
    square = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    square.alpha_composite(obj, ((side - obj.width) // 2, (side - obj.height) // 2))
    return square.resize((256, 256), Image.LANCZOS)


if __name__ == '__main__':
    args = sys.argv[1:]
    seed = 1
    if '--seed' in args:
        at = args.index('--seed')
        seed = int(args[at + 1])
        del args[at:at + 2]
    names = args or list(ICONS)
    out, raw = os.path.join('Assets', 'Resources', 'Icons'), os.path.join('Tools', 'icons_raw')
    os.makedirs(out, exist_ok=True)
    os.makedirs(raw, exist_ok=True)
    for name in names:
        started = time.time()
        image = render(name, ICONS[name], seed)
        image.save(os.path.join(raw, name + '.png'))
        cut_out(image).save(os.path.join(out, name + '.png'))
        print(f'{name}: {time.time() - started:.0f}s', flush=True)

    present = [n for n in ICONS if os.path.exists(os.path.join(out, n + '.png'))]
    cols = 8
    sheet = Image.new('RGBA', (cols * 140, ((len(present) + cols - 1) // cols) * 140), (60, 70, 90, 255))
    for i, name in enumerate(present):
        tile = Image.open(os.path.join(out, name + '.png')).convert('RGBA').resize((128, 128), Image.LANCZOS)
        sheet.alpha_composite(tile, (i % cols * 140 + 6, i // cols * 140 + 6))
    sheet.save(os.path.join('Tools', 'icons_preview.png'))

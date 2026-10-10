"""Paints one anime sky per biome with the local ComfyUI, for the shell seen from the gunner's seat (the swarm).

Model: Illustrious-XL (anime). Each sky is a wide painting of nothing but sky, horizon at the bottom edge, saved to
Assets/Resources/Sky/<biome>.png. The game wraps it mirrored round the upper half of the shell, so its two ends
never have to match. Raw renders go to Tools/sky_raw/.

Usage, from the project root, with ComfyUI running on http://127.0.0.1:8188:
    python Tools/gen_sky.py                # every biome
    python Tools/gen_sky.py ocean volcano  # only these
    python Tools/gen_sky.py --seed 7 ocean # another take
"""
import io, json, os, sys, time, urllib.parse, urllib.request
from PIL import Image

HOST = 'http://127.0.0.1:8188'
CHECKPOINT = 'Illustrious-XL-v2.0.safetensors'
STYLE = ('masterpiece, best quality, absurdres, anime screencap, anime background, scenery, no humans, sky, {subject}, '
         'cel shading, soft painted clouds, clean gradient sky, vibrant colors, wide shot, blue archive')
NEGATIVE = ('1girl, 1boy, human, character, text, watermark, signature, logo, building, city, tree, ground, land, mountain, sea, '
            'water, bird, airplane, frame, border, lowres, blurry, jpeg artifacts, photo, realistic, 3d, sketch, monochrome, noise')
WIDTH, HEIGHT = 1536, 768
SKIES = {
    'desert': 'clear hot blue sky, a few thin white clouds, warm yellow haze low on the horizon, bright sun, midday',
    'snow': 'pale winter sky, soft grey white overcast clouds, light blue gaps, cold light',
    'jungle': 'deep blue summer sky, towering cumulonimbus clouds, white fluffy clouds, sunlight, tropical',
    'ocean': 'brilliant blue summer sky, huge cumulonimbus cloud, white clouds, sun rays, seaside summer',
    'volcano': 'dark red sky, black smoke clouds, orange glow from below, embers, ash, volcanic eruption sky, dramatic',
    'city': 'evening sky, orange and pink sunset clouds, blue sky above, golden hour',
    'space': 'night sky, starry sky, milky way, nebula, large ringed planet, deep space, dark blue and purple',
    'alien': 'purple and pink sky, two moons, stars, glowing teal clouds, alien planet sky, fantasy',
    'fortress': 'dark green stormy sky, ominous black clouds, green lightning glow, sinister',
}


def post(path, data):
    request = urllib.request.Request(HOST + path, data=json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
    return json.loads(urllib.request.urlopen(request).read())


def get(path):
    return urllib.request.urlopen(HOST + path).read()


def render(name, subject, seed):
    graph = {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': CHECKPOINT}},
        '2': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': STYLE.format(subject=subject)}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': NEGATIVE}},
        '4': {'class_type': 'EmptyLatentImage', 'inputs': {'width': WIDTH, 'height': HEIGHT, 'batch_size': 1}},
        '5': {'class_type': 'KSampler', 'inputs': {'model': ['1', 0], 'positive': ['2', 0], 'negative': ['3', 0], 'latent_image': ['4', 0],
                                                    'seed': seed, 'steps': 28, 'cfg': 6.0, 'sampler_name': 'euler_ancestral',
                                                    'scheduler': 'normal', 'denoise': 1.0}},
        '6': {'class_type': 'VAEDecode', 'inputs': {'samples': ['5', 0], 'vae': ['1', 2]}},
        '7': {'class_type': 'SaveImage', 'inputs': {'images': ['6', 0], 'filename_prefix': 'sky_' + name}},
    }
    job = post('/prompt', {'prompt': graph, 'client_id': 'gen_sky'})['prompt_id']
    while True:
        history = json.loads(get('/history/' + job))
        if job in history and history[job].get('status', {}).get('completed'):
            break
        time.sleep(1)
    saved = history[job]['outputs']['7']['images'][0]
    query = urllib.parse.urlencode({'filename': saved['filename'], 'subfolder': saved['subfolder'], 'type': saved['type']})
    return Image.open(io.BytesIO(get('/view?' + query))).convert('RGB')


if __name__ == '__main__':
    args = sys.argv[1:]
    seed = 11
    if '--seed' in args:
        seed = int(args[args.index('--seed') + 1])
        del args[args.index('--seed'):args.index('--seed') + 2]
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    raw, out = os.path.join(root, 'Tools', 'sky_raw'), os.path.join(root, 'Assets', 'Resources', 'Sky')
    os.makedirs(raw, exist_ok=True)
    for name in args or SKIES:
        picture = render(name, SKIES[name], seed)
        picture.save(os.path.join(raw, name + '.png'))
        picture.resize((WIDTH * 2, HEIGHT * 2), Image.LANCZOS).save(os.path.join(out, name + '.png'), optimize=True)
        print(name, flush=True)

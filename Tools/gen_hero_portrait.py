# A hero's picture in the Blue Archive look, redrawn by ComfyUI (Illustrious + the blue_archive LoRA) from a picture of its model.
#   python Tools/gen_hero_portrait.py <image in ComfyUI/input> <output prefix> <denoise,...> <what she looks like and does> [<background>]
import json, sys, time, urllib.request, random
image, prefix, denoises, extra = sys.argv[1], sys.argv[2], [float(x) for x in sys.argv[3].split(',')], sys.argv[4] if len(sys.argv) > 4 else ''
backdrop = sys.argv[5] if len(sys.argv) > 5 else 'simple background, white background'
def post(prompt):
    r = urllib.request.Request('http://127.0.0.1:8188/prompt', json.dumps({'prompt': prompt, 'client_id': 'waifutren'}).encode(), {'Content-Type': 'application/json'})
    return json.loads(urllib.request.urlopen(r).read())
pos = 'masterpiece, best quality, absurdres, official art, blue archive, 1girl, solo, ' + extra + ', looking at viewer, clean lineart, anime coloring, ' + backdrop
neg = 'lowres, worst quality, bad quality, bad anatomy, bad hands, extra fingers, text, watermark, signature, 3d, realistic, photo, blurry, jpeg artifacts, multiple girls'
ids = []
for d in denoises:
    g = {
     '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': 'Illustrious-XL-v2.0.safetensors'}},
     '2': {'class_type': 'LoraLoader', 'inputs': {'model': ['1', 0], 'clip': ['1', 1], 'lora_name': 'blue_archive_official_7-000032.safetensors', 'strength_model': 0.85, 'strength_clip': 0.85}},
     '3': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['2', 1], 'text': pos}},
     '4': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['2', 1], 'text': neg}},
     '5': {'class_type': 'LoadImage', 'inputs': {'image': image}},
     '6': {'class_type': 'VAEEncode', 'inputs': {'pixels': ['5', 0], 'vae': ['1', 2]}},
     '7': {'class_type': 'KSampler', 'inputs': {'model': ['2', 0], 'positive': ['3', 0], 'negative': ['4', 0], 'latent_image': ['6', 0], 'seed': random.randint(1, 2**31), 'steps': 28, 'cfg': 6.0, 'sampler_name': 'euler_ancestral', 'scheduler': 'normal', 'denoise': d}},
     '8': {'class_type': 'VAEDecode', 'inputs': {'samples': ['7', 0], 'vae': ['1', 2]}},
     '9': {'class_type': 'SaveImage', 'inputs': {'images': ['8', 0], 'filename_prefix': '%s_d%02d' % (prefix, round(d * 100))}},
    }
    r = post(g)
    if r.get('node_errors'): print('ERR', r['node_errors']); sys.exit(1)
    ids.append(r['prompt_id'])
for pid in ids:
    while True:
        h = json.loads(urllib.request.urlopen('http://127.0.0.1:8188/history/' + pid).read())
        if pid in h and h[pid]['status'].get('completed'): break
        if pid in h and h[pid]['status'].get('status_str') == 'error': print('FAILED', h[pid]['status']); sys.exit(1)
        time.sleep(2)
    for o in h[pid]['outputs'].values():
        for im in o.get('images', []): print('OUT', im['filename'])

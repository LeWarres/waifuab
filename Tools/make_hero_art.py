# The main menu's character illustrations, one per hero, from their in-game models: Tools/render_hero_poses.cs renders
# each model posed on white (Tools/hero_renders/<id>.png); here ComfyUI repaints each as anime key art over that render
# (img2img, Illustrious XL, the render keeps the design and the pose), takes the background off (rembg), and the
# figure is trimmed, scaled and stood at the foot of an 832 x 1216 picture: Assets/Resources/Menu/hero_<id>_cut.png.
# Needs the ComfyUI server up (E:/AI/ComfyUI_windows_portable). Run with its python (numpy, scipy):
#   E:/AI/ComfyUI_windows_portable/python_embeded/python.exe -I Tools/make_hero_art.py [ids...]
import json, os, shutil, sys, time, urllib.request
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
COMFY = r"E:/AI/ComfyUI_windows_portable/ComfyUI"
OUT = os.path.join(HERE, "..", "Assets", "Resources", "Menu")
STYLE = "masterpiece, best quality, newest, anime coloring, cel shading, clean lineart, game character key art, full body, standing, white background, simple background, "
NEGATIVE = "lowres, worst quality, bad anatomy, bad hands, extra limbs, deformed, text, watermark, signature, blurry, cropped, scenery, background clutter"
DENOISE = float(os.environ.get("DENOISE", "0.7"))
HEROES = {
    "cerebron": "1boy, cyborg, giant exposed pink brain for a head, single huge purple eye in the brain, dark olive armored power suit, claws, menacing",
    "culu": "1girl, green skin, green tentacle hair, red eyes, black and white maid dress, white apron, frills, black stockings, black gloves, smug",
    "faceless": "1other, faceless, featureless smooth white head, no eyes no mouth, slender white humanoid body, glossy white skin, eerie",
    "onini": "1girl, oni, short spiky white hair, red horns, red and white jacket, black track pants with white stripes, red sneakers, confident",
    "sentinel": "mecha, giant armored robot, black armor, orange armor plates, glowing orange visor, bulky shoulders, heavy fists",
    "skullmonk": "1boy, skull mask, long dark feathered hair, white robe, long red hakama, prayer beads, monk",
    "tzmil": "1girl, dark green spiky hair, skulls on head, skeletal ribcage top, gold ornaments, black stockings, boots, death goddess",
    "xiuhtecuhtli": "1boy, aztec god, warrior, huge turquoise fire feather headdress, gold skull ornaments, red loincloth, dark skin, jaguar details",
}

def post(prompt):
    data = json.dumps({"prompt": prompt, "client_id": "hero_art"}).encode()
    r = json.loads(urllib.request.urlopen(urllib.request.Request("http://127.0.0.1:8188/prompt", data=data, headers={"Content-Type": "application/json"})).read())
    if r.get("node_errors"): raise SystemExit(r["node_errors"])
    pid = r["prompt_id"]
    for _ in range(300):
        h = json.loads(urllib.request.urlopen("http://127.0.0.1:8188/history/" + pid).read())
        if h:
            h = h[pid]
            if h["status"]["status_str"] != "success": raise SystemExit(h["status"])
            return h["outputs"]
        time.sleep(1)
    raise SystemExit("timed out")

def make(hid, seed=int(os.environ.get("SEED", "7"))):
    shutil.copy(os.path.join(HERE, "hero_renders", hid + ".png"), os.path.join(COMFY, "input", "pose_" + hid + ".png"))
    prompt = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "Illustrious-XL-v2.0.safetensors"}},
        "2": {"class_type": "LoadImage", "inputs": {"image": "pose_" + hid + ".png"}},
        "3": {"class_type": "VAEEncode", "inputs": {"pixels": ["2", 0], "vae": ["1", 2]}},
        "4": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": STYLE + HEROES[hid]}},
        "5": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": NEGATIVE}},
        "6": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "positive": ["4", 0], "negative": ["5", 0], "latent_image": ["3", 0],
              "seed": seed, "steps": 28, "cfg": 6.0, "sampler_name": "euler_ancestral", "scheduler": "normal", "denoise": DENOISE}},
        "7": {"class_type": "VAEDecode", "inputs": {"samples": ["6", 0], "vae": ["1", 2]}},
        "8": {"class_type": "Image Remove Background Rembg (mtb)", "inputs": {"image": ["7", 0], "alpha_matting": True, "alpha_matting_foreground_threshold": 240,
              "alpha_matting_background_threshold": 10, "alpha_matting_erode_size": 10, "post_process_mask": True, "bgcolor": "#000000"}},
        "9": {"class_type": "InvertMask", "inputs": {"mask": ["8", 1]}},
        "10": {"class_type": "JoinImageWithAlpha", "inputs": {"image": ["7", 0], "alpha": ["9", 0]}},
        "11": {"class_type": "SaveImage", "inputs": {"images": ["10", 0], "filename_prefix": "hero_" + hid}},
    }
    name = post(prompt)["11"]["images"][0]["filename"]
    art = np.array(Image.open(os.path.join(COMFY, "output", name)).convert("RGBA"))
    # only the figure: its biggest piece (and what touches it), trimmed, scaled to the frame's height, feet at the foot
    solid = art[:, :, 3] > 40
    parts, n = ndimage.label(solid)
    if n:
        sizes = ndimage.sum(solid, parts, range(1, n + 1))
        keep = ndimage.binary_dilation(parts == np.argmax(sizes) + 1, iterations=3)
        art[:, :, 3] = np.where(keep, art[:, :, 3], 0)
        # holes inside the figure (a white body on the white page loses bits of itself) are filled back in
        inside = ndimage.binary_fill_holes(keep) & ~keep | (ndimage.binary_fill_holes(art[:, :, 3] > 40) & (art[:, :, 3] <= 40))
        art[:, :, 3] = np.where(inside, 255, art[:, :, 3])
    ys, xs = np.nonzero(art[:, :, 3] > 40)
    figure = Image.fromarray(art).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    scale = min(1180 / figure.height, 800 / figure.width)
    figure = figure.resize((max(1, int(figure.width * scale)), max(1, int(figure.height * scale))), Image.LANCZOS)
    frame = Image.new("RGBA", (832, 1216), (0, 0, 0, 0))
    frame.alpha_composite(figure, ((832 - figure.width) // 2, 1216 - figure.height - 8))
    frame.save(os.path.join(OUT, "hero_" + hid + "_cut.png"))
    print("made", hid, figure.size)

for hid in (sys.argv[1:] or HEROES):
    make(hid)

"""Uses the original icon checkpoint, prompt style, sampler and seed for distinct evolved weapon art."""
import json
import time
from pathlib import Path
from PIL import Image, ImageDraw
import gen_icons as original

SUBJECTS = {
    'turret': 'heavy artillery turret, twin blue cannon barrels, gold armor, muzzle blast',
    'rocket': 'advanced missile pod, red tipped rockets, gold and white launcher',
    'minigun': 'heavy gatling gun, oversized rotary barrels, silver and gold metal, cyan energy core',
    'shotgun': 'golden double barrel shotgun, ornate golden receiver, dark wooden stock',
    'sniper': 'advanced sniper rifle, golden hawk shaped scope, white and gold barrel',
    'pulse': 'glowing magenta energy core, golden ring, bright pink shockwave sphere',
    'storm': 'dark purple thundercloud, large golden lightning bolt, cyan electric glow',
    'flame': 'circular ring of orange flames, golden fire vortex, blazing fire halo',
    'freeze': 'large blue ice crystal, sharp ice crown, white frost, cyan aura',
    'chain': 'advanced tesla coil, golden tower, cyan lightning network, electric arcs',
    'laser': 'triangular glass prism, golden frame, rainbow laser beam, energy crystal',
    'blaster': 'bright blue plasma comet, round glowing energy core, long cyan flame tail, golden spark',
    'grenade': 'advanced cluster grenade, green shell, golden bands, glowing orange core',
    'smg': 'small futuristic combat drone, four white rotors, gold trim, cyan camera lens',
    'scatter': 'golden chakram, circular throwing blade, sharp orange rim, blue center gem',
    'rail': 'advanced ion cannon, purple energy barrel, white and gold sci-fi metal, glowing violet core',
    'nova': 'golden solar heart, pink star shaped energy core, bright orange sun rays',
    'spark': 'cyan orb shield, bright blue energy sphere surrounded by golden protective rings',
    'masamune': 'blue dragon head, flowing cyan dragon, golden horns, swirling blue magic',
}
ROOT = Path(__file__).resolve().parents[1]

if __name__ == '__main__':
    out = ROOT / 'Assets/Resources/Icons'
    raw = ROOT / 'Tools/icons_raw/evolved'
    raw.mkdir(parents=True, exist_ok=True)
    manifest = {'checkpoint': original.CHECKPOINT, 'seed': 1, 'steps': 28, 'cfg': 6.0,
                'sampler': 'euler_ancestral', 'scheduler': 'normal', 'subjects': SUBJECTS}
    (ROOT / 'Tools/evolved_icons_settings.json').write_text(json.dumps(manifest, indent=2), encoding='utf8')
    for name, subject in SUBJECTS.items():
        destination = out / (name + '_evo.png')
        if destination.exists():
            continue
        started = time.time()
        render = original.render(name + '_evo', subject, 1)
        render.save(raw / (name + '_evo.png'))
        original.cut_out(render).save(destination)
        print(f'{name}_evo: {time.time() - started:.1f}s', flush=True)
    sheet = Image.new('RGBA', (760, 4 * 160), (54, 63, 80, 255))
    draw = ImageDraw.Draw(sheet)
    for index, name in enumerate(SUBJECTS):
        x, y = index % 5 * 152, index // 5 * 160
        tile = Image.open(out / (name + '_evo.png')).resize((132, 132))
        sheet.alpha_composite(tile, (x + 10, y))
        draw.text((x + 10, y + 136), name, fill='white')
    sheet.save(ROOT / 'Tools/evolved_icons_preview.png')

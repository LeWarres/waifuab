"""Export Xiuhtecuhtli's canonical campaign and preserve unrelated localization."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = json.loads((root / 'Tools/r20_story_dialogues.json').read_text(encoding='utf-8'))
rows = data['rows']
assert data['locales'] == ['es', 'en', 'pt', 'ja']
assert len(rows) == 65 and len({r[0] for r in rows}) == len(rows)
assert all(len(r) == 5 and all(r) and all('\t' not in value for value in r) for r in rows)
source = root / 'Assets/Localization/strings.tsv'
raw = source.read_bytes()
newline = '\r\n' if b'\r\n' in raw else '\n'
lines = raw.decode('utf-8-sig').splitlines()
updates = {r[0]: '\t'.join(value.replace('\n', r'\n') for value in r) for r in rows}
for i, line in enumerate(lines):
    key = line.split('\t', 1)[0]
    if key in updates:
        lines[i] = updates.pop(key)
lines.extend(updates.values())
source.write_bytes((newline.join(lines) + newline).encode('utf-8'))
lookup = {r[0]: r[1] for r in rows}
report = ['# Historia de R-20: Xiuhtecuhtli', '',
          'Voz excéntrica, extrovertida y habitualmente enfadada. Se presenta, reclama atención y convierte los contratiempos en agravios personales; discute también consigo mismo. Su ira tiene consecuencias y no borra el afecto por Tzmil. Primera persona conversacional, sin conocer pensamientos ajenos ni información que no ha visto.', '',
          'Ruta: selva/Culu → cuestionario humano → ciudad/carrera y lanzamiento denegado → mar/BBA → volcán/holoajedrez con Tzmil → base del gobierno/clon → epílogo/Gracias. Arte y mecánicas conservados. «mesa larga larga» permanece literal.', '']
for c in range(5):
    report += [f'## {c+1}. '+lookup[f'story.xiuhtecuhtli.{c}.title'], '']
    for side in ['intro','before','after','outro']:
        group = sorted((r for r in rows if r[0].startswith(f'story.xiuhtecuhtli.{c}.{side}.')), key=lambda r:int(r[0].split('.')[-1]))
        report += [f'### {side}', ''] + [r[1]+'\n' for r in group]
(root / 'Tools/r20_story.md').write_text('\n'.join(report).rstrip()+'\n', encoding='utf-8')
print(f'{len(rows)} keys in four languages; unrelated localization rows preserved.')

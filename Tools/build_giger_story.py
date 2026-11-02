"""Export the approved localized Giger campaign without restoring the old draft."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = json.loads((root / 'Tools/giger_story_dialogues.json').read_text(encoding='utf-8'))
columns = ['key', 'es', 'en', 'pt', 'ja']
assert len(rows) == 50 and len({r['key'] for r in rows}) == len(rows)
assert all(set(r) == set(columns) and all(r.values()) and all('\t' not in v and '\n' not in v for v in r.values()) for r in rows)
source = root / 'Assets/Localization/strings.tsv'
raw = source.read_bytes()
newline = '\r\n' if b'\r\n' in raw else '\n'
lines = raw.decode('utf-8-sig').splitlines()
updates = {r['key']: '\t'.join(r[c] for c in columns) for r in rows}
for i, line in enumerate(lines):
    key = line.split('\t', 1)[0]
    if key in updates:
        lines[i] = updates.pop(key)
lines.extend(updates.values())
source.write_bytes((newline.join(lines) + newline).encode('utf-8'))
lookup = {r['key']: r['es'] for r in rows}
report = ['# Historia de Giger Machine', '',
          'Giger es agresivo, brusco, impulsivo y alocado. Busca ruido, resistencia y velocidad; se enfurece cuando lo frenan y se entusiasma hasta con los golpes que recibe. Narra lo que hace, oye y siente en primera persona, sin conocer planes ajenos. La curiosidad lo mete en problemas antes de pensar. Las voces de sus rivales conservan sus diferencias.', '',
          'Ruta: Pscoworld/Culu → Thulu/Crochtrain → infierno/reto de destrucción → cielo/Gora-P → regreso a Pscoworld → Gracias. Pscoworld no tiene otros habitantes; los visitantes llegan de fuera. Las cifras del reto permanecen exclusivamente en su interfaz. Arte y mecánicas conservados.', '',
          'Arte aprobado: boceto principiante con papel blanco, línea negra insegura y construcción roja/azul; fondos escasos, sin rellenos ni sombreado elaborado.', '']
for c in range(4):
    report += [f'## {c+1}. '+lookup[f'story.giger.{c}.title'], '']
    for side in ['intro', 'before', 'after', 'outro']:
        group = sorted((r for r in rows if r['key'].startswith(f'story.giger.{c}.{side}.')), key=lambda r:int(r['key'].split('.')[-1]))
        report += [f'### {side}', ''] + [r['es']+'\n' for r in group]
(root / 'Tools/giger_story.md').write_text('\n'.join(report).rstrip()+'\n', encoding='utf-8')
print(f'{len(rows)} keys in four languages; unrelated localization rows preserved.')

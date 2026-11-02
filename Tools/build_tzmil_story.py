"""Export Tzmil's canonical dialogue without restoring the old sarcastic draft."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = json.loads((root / 'Tools/tzmil_story_dialogues.json').read_text(encoding='utf-8'))
columns = ['key', 'es', 'en', 'pt', 'ja']
assert len(rows) == 55 and len({r['key'] for r in rows}) == len(rows)
for row in rows:
    assert set(row) == set(columns) and all(row.values())
    assert all('\t' not in value for value in row.values())
source = root / 'Assets/Localization/strings.tsv'
raw = source.read_bytes()
newline = '\r\n' if b'\r\n' in raw else '\n'
lines = raw.decode('utf-8-sig').splitlines()
updates = {r['key']: '\t'.join(r[c].replace('\n', r'\n') for c in columns) for r in rows}
for i, line in enumerate(lines):
    key = line.split('\t', 1)[0]
    if key in updates:
        lines[i] = updates.pop(key)
lines.extend(updates.values())
source.write_bytes((newline.join(lines) + newline).encode('utf-8'))
report = ['# Historia de Tzmil', '', 'Voz amable, bondadosa, paciente y amorosa. Escucha, cuida y pone límites cuando alguien corre peligro; también encuentra tiempo para sí misma. Narración conversacional en primera persona.', '', 'Ruta: volcán (holoajedrez R-20) → base del gobierno (Golem) → infierno (Onini) → cielo (ángel) → Gracias. Se conservan las ilustraciones existentes.', '']
lookup = {r['key']: r['es'] for r in rows}
for c in range(4):
    report += [f'## {c + 1}. ' + lookup[f'story.tzmil.{c}.title'], '']
    for side in ['intro', 'before', 'after', 'outro']:
        report += [f'### {side}', '']
        report += [r['es'] + '\n' for r in rows if r['key'].startswith(f'story.tzmil.{c}.{side}.')]
(root / 'Tools/tzmil_story.md').write_text('\n'.join(report) + '\n', encoding='utf-8')
print(f'{len(rows)} keys in four languages; unrelated localization rows preserved.')

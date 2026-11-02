"""Export Golem's canonical dialogue, including both endings and puzzle text."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = json.loads((root / 'Tools/golem_story_dialogues.json').read_text(encoding='utf-8'))
columns = ['key', 'es', 'en', 'pt', 'ja']
assert len(rows) == 124 and len({r['key'] for r in rows}) == len(rows)
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
report = ['# Historia de Golem', '',
          'Voz fría, precisa y contenida. Se observa como una máquina sin poder determinar si sus dudas, recuerdos y deseos son conciencia. No conoce el pensamiento de otros robots. Su decisión en el ducto tiene consecuencias; proteger a otros después no la borra.', '',
          'Ruta: base del gobierno (Tzmil) → ascenso → espacio (Faceless) → Thulu (circuitos) → Cielo Robótico (13 decisiones). 7–13 respuestas altas: renacimiento; 0–6: conciencia atrapada en Baratito. Se conservan las ilustraciones.', '']
for row in rows:
    report += [row['key'], row['es'], '']
(root / 'Tools/golem_story.md').write_text('\n'.join(report), encoding='utf-8')
print(f'{len(rows)} keys in four languages; unrelated localization rows preserved.')

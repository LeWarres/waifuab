"""Apply the canonical Nath Moon script without resetting it to the original draft."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = json.loads((root / 'Tools/nath_story_dialogues.json').read_text(encoding='utf-8'))
columns = ['key', 'es', 'en', 'pt', 'ja']
assert len({r['key'] for r in rows}) == len(rows)
for row in rows:
    assert set(row) == set(columns) and all(row.values())
    assert all('\n' not in value and '\t' not in value for value in row.values())
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
header = '# Nath Moon / ReyMar\n\nVolcán (Tzmil) → cuestionario humano → ciudad/carrera → cielo (Gabriel/Gora-P) → infierno (Onini) → bosque/selva (Faceless/T.exe) → Gracias.\n\n'
header += 'Nath es ruda, peleonera e impulsiva. Se adelanta a las explicaciones, entiende los desafíos como una invitación y expresa afecto a regañadientes. Narra en primera persona, como una conversación, sin saber lo que otros piensan. Conserva su piel morena, uniforme morado, ojos verdes y las ilustraciones existentes.\n\n'
(root / 'Tools/nath_story.md').write_text(header + '\n\n'.join(r['key'] + '\n' + r['es'] for r in rows) + '\n', encoding='utf-8')
print(f'{len(rows)} Nath Moon keys in four languages; unrelated rows preserved.')

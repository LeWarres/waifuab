"""Export Culu's canonical localized campaign without restoring the old draft."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = json.loads((root / 'Tools/culu_story_dialogues.json').read_text(encoding='utf-8'))
columns = ['key', 'es', 'en', 'pt', 'ja']
assert len(rows) == 61 and len({r['key'] for r in rows}) == len(rows)
assert all(set(r) == set(columns) and all(r.values()) and all('\t' not in v for v in r.values()) for r in rows)
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
lookup = {r['key']: r['es'] for r in rows}
report = ['# Historia de Culu', '',
          'Culu es amistosa, inteligente y correcta al hablar. Escucha, pregunta con precisión, ofrece ayuda y reconoce sus errores sin menospreciar a los demás. Le ilusionan los maid cafés y sabe pensar en cómo administrarlos. La información falsa, el calor y el cansancio explican sus dificultades sin convertirla en ingenua. Primera persona conversacional con conocimientos limitados.', '',
          'Ruta: mar/Onini → selva/R-20 → desierto/BBA → Psicoworlds/Giger Machine → Thulu/Crochtrain → Baratito sobre el mar → Gracias. Arte, cactus, piedra perdida y mecánicas conservados.', '']
for c in range(5):
    report += [f'## {c+1}. '+lookup[f'story.culu.{c}.title'], '']
    for side in ['intro','before','after','outro']:
        group = sorted((r for r in rows if r['key'].startswith(f'story.culu.{c}.{side}.')), key=lambda r:int(r['key'].split('.')[-1]))
        report += [f'### {side}', ''] + [r['es']+'\n' for r in group]
(root / 'Tools/culu_story.md').write_text('\n'.join(report).rstrip()+'\n', encoding='utf-8')
print(f'{len(rows)} keys in four languages; unrelated localization rows preserved.')

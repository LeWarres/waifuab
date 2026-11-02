"""Export Dorian's canonical localized campaign and Spanish reading guide."""
import csv
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
columns = ['key', 'es', 'en', 'pt', 'ja']
with (root / 'Tools/crochtrain_story_dialogues.tsv').open(encoding='utf-8', newline='') as f:
    rows = list(csv.DictReader(f, delimiter='\t'))
assert len(rows) == 51 and len({r['key'] for r in rows}) == len(rows)
assert all(set(r) == set(columns) and all(r.values()) and all('\t' not in v and '\n' not in v for v in r.values()) for r in rows)
for r in rows:
    assert r['ja'].count('?') < 3, r['key']
(root / 'Tools/crochtrain_story_dialogues.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
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
source.write_bytes((newline.join(lines)+newline).encode('utf-8'))
lookup = {r['key']: r['es'] for r in rows}
report = ['# Historia de Crochtrain — Dorian', '',
          'Ruta aprobada: Thulu/Giger Machine → infierno/Onini → cielo/Gora-P → mar/Cerebron → Doris en Baratito → captura y promesa de Culu → Gracias.', '',
          'Dorian fue jovial, cariñoso y entusiasta. Conserva expresiones antiguas y cortesía; transformado habla con agotamiento, tristeza y nostalgia. Sus intervenciones en gameplay son pensamientos rotulados explícitamente. Los rivales reaccionan a sonidos y movimientos; no lo entienden. Solo Culu escucha sus pensamientos al final. La forma tejida es su representación comprensible para el jugador; otros ven una forma de guerra indefinida.', '',
          'Arte: caricatura amateur en blanco y negro inspirada en los años treinta, líneas torpes, cuerpos deformes, fondos sencillos. Nueve ilustraciones propias. Café sobre el mar, Doris anciana junto a Cerebron y el amigo gris. El final termina con la promesa de Culu, sin añadir una liberación ni una reacción de Doris.', '']
for c in range(4):
    report += [f'## {c+1}. '+lookup[f'story.crochtrain.{c}.title'], '']
    for side in ['intro', 'before', 'after', 'outro']:
        group = sorted((r for r in rows if r['key'].startswith(f'story.crochtrain.{c}.{side}.')), key=lambda r:int(r['key'].split('.')[-1]))
        report += [f'### {side}', '']+[r['es']+'\n' for r in group]
(root / 'Tools/crochtrain_story.md').write_text('\n'.join(report).rstrip()+'\n', encoding='utf-8')
print(f'{len(rows)} keys exported in four languages.')

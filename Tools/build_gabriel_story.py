"""Export Gabriel's canonical localized campaign without restoring the old draft."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = json.loads((root / 'Tools/gabriel_story_dialogues.json').read_text(encoding='utf-8'))
columns = ['key', 'es', 'en', 'pt', 'ja']
assert len(rows) == 52 and len({r['key'] for r in rows}) == len(rows)
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
report = ['# Gabriel', '',
          'Gabriel es consentida, berrinchuda y muy amorosa. Quiere atención, agradecimientos y tiempo para ella; protesta con dramatismo, pero ayuda y protege antes de atender su propio disgusto. Su preocupación por el hermano de Tzmil conserva la seriedad del momento. Voz conversacional en primera persona y conocimientos limitados a lo que ve o le cuentan.', '',
          'Gora-P; cielo, apertura de 30 minutos y suerte +3. Onini → arma/pasiva → Tzmil → arma/pasiva → Giger Machine → arma/pasiva → Nath Moon/ReyMar. Mismo tren y equipo entre encuentros. Reencuentro con el conductor de R-20, cita en Baratito sobre el mar y Gracias. Arte conservado.', '']
for c in range(4):
    report += [f'## {c+1}. '+next(r['es'] for r in rows if r['key']==f'story.gabriel.{c}.title'), '']
    for side in ['intro','before','after','outro']:
        group = sorted((r for r in rows if r['key'].startswith(f'story.gabriel.{c}.{side}.')), key=lambda r:int(r['key'].split('.')[-1]))
        report += [f'### {side}', ''] + [r['es']+'\n' for r in group]
(root / 'Tools/gabriel_story.md').write_text('\n'.join(report).rstrip()+'\n', encoding='utf-8')
print(f'{len(rows)} keys in four languages; unrelated localization rows preserved.')

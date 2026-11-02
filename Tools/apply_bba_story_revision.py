"""Merge the reviewed BBA copy without rewriting unrelated localization rows."""
from pathlib import Path
import csv

root = Path(__file__).resolve().parents[1]
source = root / 'Assets/Localization/strings.tsv'
revision = Path(__file__).with_name('bba_story_revision.tsv')
with revision.open(encoding='utf-8', newline='') as f:
    records = list(csv.reader(f, delimiter='\t'))
assert records[0] == ['key', 'es', 'en', 'pt', 'ja']
updates = {}
for row in records[1:]:
    assert len(row) == 5 and all(row), row[0]
    assert row[0] not in updates, row[0]
    updates[row[0]] = '\t'.join(row)
raw = source.read_bytes()
newline = '\r\n' if b'\r\n' in raw else '\n'
lines = raw.decode('utf-8-sig').splitlines()
keys = [line.split('\t')[0] for line in lines[1:] if line]
assert len(keys) == len(set(keys)), 'Duplicate source keys'
merged = [updates.get(line.split('\t')[0], line) for line in lines]
merged += [value for key, value in updates.items() if key not in keys]
source.write_bytes((newline.join(merged) + newline).encode('utf-8'))
print(f'Applied {len(updates)} entries in four languages; {sum(k not in keys for k in updates)} new keys.')

spanish = {row[0]: row[1] for row in records[1:]}
script = ['# Historia revisada de BBA', '', 'Guion de lectura. Los interlocutores hablan por radio o ante BBA; toda la narración pertenece a ella.', '', '## Prólogo', '']
script += [spanish[f'story.page.{n:02}'] + '\n' for n in range(1, 17)]
for c, (title, before, after, rival) in enumerate([
    ('Desierto', 'hrhrh', 'rh', 'Onini'),
    ('Ciudad', 'rhrh', '', 'Cerebron'),
    ('Espacio', 'rhrh', 'h', 'Moho'),
    ('Planeta alienígena', 'rhrhr', 'rh', 'Faceless'),
    ('Fortaleza', 'rhrh', '', 'Control'),
]):
    script += [f'## {title}', '']
    if c == 2:
        script += ['### Ascenso', ''] + [spanish['ascent.' + suffix] + '\n' for suffix in ('intro', 'arrived')]
    if c:
        script += [spanish[f'story.bba.{c}.intro.{n}'] + '\n' for n in (1, 2)]
    if c == 4:
        script += ['### Entrada por las puertas', '', spanish['story.bba.gates.before'], '', spanish['story.bba.gates.after'], '']
    for side, pattern in [('before', before), ('after', after)]:
        if not pattern:
            continue
        script += ['### ' + ('Antes de la batalla' if side == 'before' else 'Después de la batalla'), '']
        for i, speaker in enumerate(pattern, 1):
            name = 'BBA' if speaker == 'h' else 'Control' if c == 1 and i == 1 else rival
            script += [f'**{name}:** ' + spanish[f'story.bba.{c}.{side}.{i}'], '']
    script += ['### Desenlace', '']
    count = (2, 1, 2, 2, 5)[c]
    script += [spanish[f'story.bba.{c}.outro.{n}'] + '\n' for n in range(1, count + 1)]
Path(__file__).with_name('bba_story_revisada_es.md').write_text('\n'.join(script), encoding='utf-8')

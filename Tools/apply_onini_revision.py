"""Keep Onini's script, localization source and reading copy in sync."""
import csv
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
with (root / 'Tools/onini_story_revision.tsv').open(encoding='utf-8', newline='') as f:
    edited = list(csv.reader(f, delimiter='\t'))
assert edited[0] == ['key', 'es', 'en', 'pt', 'ja']
updates = {row[0]: row for row in edited[1:]}
assert len(updates) == len(edited) - 1
assert all(len(row) == 5 and all(row) for row in edited[1:])
script = root / 'Tools/onini_story_dialogues.json'
data = json.loads(script.read_text(encoding='utf-8'))
assert data['locales'] == ['es', 'en', 'pt', 'ja']
assert set(updates) <= {row[0] for row in data['rows']}
data['rows'] = [updates.get(row[0], row) for row in data['rows']]
script.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
source = root / 'Assets/Localization/strings.tsv'
raw = source.read_bytes()
newline = '\r\n' if b'\r\n' in raw else '\n'
lines = raw.decode('utf-8-sig').splitlines()
all_rows = {row[0]: '\t'.join(row) for row in data['rows'] if row[0].startswith('story.onini.')}
known = {line.split('\t', 1)[0] for line in lines}
assert set(all_rows) <= known
lines = [all_rows.get(line.split('\t', 1)[0], line) for line in lines]
source.write_bytes((newline.join(lines) + newline).encode('utf-8'))
header = '# Historia de Onini\n\nVoz rebelde, despreocupada y aventurera. Narración conversacional en primera persona. Le atraen los caminos nuevos y se resiste al control; la comida y las peleas forman parte de la aventura, no sustituyen su personalidad.\n\n'
header += 'Ruta: infierno/BBA; desierto/BBA (daños impiden seguirla); base del gobierno/Golem; cielo/custodio; Psicoworlds/sombra; regreso al infierno y Gracias.\n\n'
(root / 'Tools/onini_story.md').write_text(header + '\n\n'.join(row[0] + '\n\n' + row[1] for row in data['rows']) + '\n', encoding='utf-8')
print(f'Updated {len(updates)} Onini entries in four languages; {len(data["rows"])} total keys. Other heroes preserved.')
